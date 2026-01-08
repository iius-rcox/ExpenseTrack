using System.Diagnostics;
using System.Diagnostics;
using System.Text.RegularExpressions;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for receipt-to-transaction matching operations.
/// Implements confidence scoring with amount (40pts), date (35pts), vendor (25pts).
/// </summary>
public partial class MatchingService : IMatchingService
{
    private readonly ExpenseFlowDbContext _context;
    private readonly IMatchRepository _matchRepository;
    private readonly IFuzzyMatchingService _fuzzyMatchingService;
    private readonly IVendorAliasService _vendorAliasService;
    private readonly IExpensePredictionService _predictionService;
    private readonly ILogger<MatchingService> _logger;

    // Scoring constants
    private const decimal AmountExactPoints = 40m;
    private const decimal AmountNearPoints = 20m;
    private const decimal DateSameDayPoints = 35m;
    private const decimal DateOneDayPoints = 30m;
    private const decimal DateTwoThreeDaysPoints = 25m;
    private const decimal DateFourSevenDaysPoints = 10m;
    private const decimal VendorAliasPoints = 25m;
    private const decimal VendorFuzzyPoints = 15m;
    private const decimal MinimumConfidenceThreshold = 70m;
    private const decimal AmbiguousThreshold = 5m;

    // Amount tolerances
    private const decimal AmountExactTolerance = 0.10m;
    private const decimal AmountNearTolerance = 1.00m;

    // Fuzzy matching threshold
    private const double FuzzyMatchThreshold = 0.70;

    public MatchingService(
        ExpenseFlowDbContext context,
        IMatchRepository matchRepository,
        IFuzzyMatchingService fuzzyMatchingService,
        IVendorAliasService vendorAliasService,
        IExpensePredictionService predictionService,
        ILogger<MatchingService> logger)
    {
        _context = context;
        _matchRepository = matchRepository;
        _fuzzyMatchingService = fuzzyMatchingService;
        _vendorAliasService = vendorAliasService;
        _predictionService = predictionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AutoMatchResult> RunAutoMatchAsync(Guid userId, List<Guid>? receiptIds = null)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting auto-match for user {UserId} with {ReceiptCount} specific receipts",
            userId, receiptIds?.Count ?? 0);

        // Get unmatched receipts with extracted data
        var receiptsQuery = _context.Receipts
            .Where(r => r.UserId == userId
                && r.MatchStatus == MatchStatus.Unmatched
                && r.AmountExtracted.HasValue
                && r.DateExtracted.HasValue);

        if (receiptIds != null && receiptIds.Any())
        {
            receiptsQuery = receiptsQuery.Where(r => receiptIds.Contains(r.Id));
        }

        var receipts = await receiptsQuery.ToListAsync();

        if (!receipts.Any())
        {
            _logger.LogInformation("No receipts to match for user {UserId}", userId);
            return new AutoMatchResult(0, 0, 0, stopwatch.ElapsedMilliseconds, new List<ReceiptTransactionMatch>());
        }

        // Calculate date range from receipts (±7 days from min/max receipt dates)
        var receiptDates = receipts.Select(r => r.DateExtracted!.Value).ToList();
        var minDate = receiptDates.Min().AddDays(-7);
        var maxDate = receiptDates.Max().AddDays(7);

        // T017: Query transactions and groups in parallel for performance
        var transactionsTask = _context.Transactions
            .Where(t => t.UserId == userId
                && t.MatchStatus == MatchStatus.Unmatched
                && t.GroupId == null  // T018: Exclude grouped transactions
                && t.TransactionDate >= minDate
                && t.TransactionDate <= maxDate)
            .ToListAsync();

        var groupsTask = GetUnmatchedGroupsAsync(userId, minDate, maxDate);

        await Task.WhenAll(transactionsTask, groupsTask);

        var transactions = await transactionsTask;
        var groups = await groupsTask;

        _logger.LogInformation(
            "Found {ReceiptCount} receipts, {TransactionCount} transactions, and {GroupCount} groups to match (date range: {MinDate} to {MaxDate})",
            receipts.Count, transactions.Count, groups.Count, minDate, maxDate);

        // Log group details for debugging
        foreach (var g in groups)
        {
            _logger.LogInformation(
                "Group candidate: {GroupId} '{GroupName}' Amount=${Amount} Date={Date} Status={Status}",
                g.Id, g.Name, g.CombinedAmount, g.DisplayDate, g.MatchStatus);
        }

        if (!transactions.Any() && !groups.Any())
        {
            _logger.LogInformation("No transactions or groups in date range for user {UserId}", userId);
            return new AutoMatchResult(0, receipts.Count, 0, stopwatch.ElapsedMilliseconds, new List<ReceiptTransactionMatch>());
        }

        // Pre-compute alias matches for all unique transaction descriptions (O(m) instead of O(n*m))
        var aliasCache = await BuildAliasCacheAsync(transactions);

        // Also add group names to alias cache for vendor matching
        foreach (var group in groups)
        {
            var vendorPattern = ExtractVendorFromGroupName(group.Name);
            if (!string.IsNullOrEmpty(vendorPattern) && !aliasCache.ContainsKey(vendorPattern))
            {
                // Look up any alias that might match the extracted vendor pattern
                var alias = await _context.VendorAliases
                    .FirstOrDefaultAsync(a => a.CanonicalName == vendorPattern);
                aliasCache[vendorPattern] = alias;
            }
        }

        _logger.LogDebug("Built alias cache with {CacheSize} entries", aliasCache.Count);

        var proposals = new List<ReceiptTransactionMatch>();
        var ambiguousCount = 0;
        var transactionMatchCount = 0;
        var groupMatchCount = 0;

        // Track consumed candidates to prevent duplicate matches
        var consumedTransactionIds = new HashSet<Guid>();
        var consumedGroupIds = new HashSet<Guid>();

        foreach (var receipt in receipts)
        {
            // T019: Create unified candidate pool from transactions and groups
            var candidates = new List<MatchCandidate>();

            // Add transaction candidates (filtered by amount tolerance)
            foreach (var t in transactions.Where(t => !consumedTransactionIds.Contains(t.Id)))
            {
                if (Math.Abs(receipt.AmountExtracted!.Value - Math.Abs(t.Amount)) <= AmountNearTolerance)
                {
                    candidates.Add(new MatchCandidate
                    {
                        Id = t.Id,
                        Type = MatchCandidateType.Transaction,
                        Amount = Math.Abs(t.Amount),
                        Date = t.TransactionDate,
                        VendorPattern = ExtractVendorPattern(t.Description),
                        DisplayName = t.Description,
                        TransactionCount = null
                    });
                }
            }

            // Add group candidates (filtered by amount tolerance)
            foreach (var g in groups.Where(g => !consumedGroupIds.Contains(g.Id)))
            {
                if (Math.Abs(receipt.AmountExtracted!.Value - Math.Abs(g.CombinedAmount)) <= AmountNearTolerance)
                {
                    candidates.Add(new MatchCandidate
                    {
                        Id = g.Id,
                        Type = MatchCandidateType.Group,
                        Amount = Math.Abs(g.CombinedAmount),
                        Date = g.DisplayDate,
                        VendorPattern = ExtractVendorFromGroupName(g.Name),
                        DisplayName = g.Name,
                        TransactionCount = g.TransactionCount
                    });
                }
            }

            if (!candidates.Any())
            {
                _logger.LogInformation(
                    "Receipt {ReceiptId} (${Amount}, {Vendor}) has no candidates within amount tolerance",
                    receipt.Id, receipt.AmountExtracted, receipt.VendorExtracted);
                continue;
            }

            _logger.LogInformation(
                "Receipt {ReceiptId} (${Amount}) has {CandidateCount} candidates",
                receipt.Id, receipt.AmountExtracted, candidates.Count);

            // T020: Find best match from unified candidate pool
            var (match, isAmbiguous, bestCandidate) = FindBestMatchFromCandidates(
                receipt, candidates, userId, aliasCache);

            if (match == null && !isAmbiguous)
            {
                _logger.LogInformation(
                    "Receipt {ReceiptId} candidates all scored below threshold ({Threshold})",
                    receipt.Id, MinimumConfidenceThreshold);
            }

            if (isAmbiguous)
            {
                ambiguousCount++;
                _logger.LogDebug("Receipt {ReceiptId} has ambiguous matches", receipt.Id);
                continue;
            }

            if (match != null && bestCandidate != null)
            {
                proposals.Add(match);

                // Update receipt status to Proposed
                receipt.MatchStatus = MatchStatus.Proposed;

                // Track which candidate type was matched and mark as consumed
                if (bestCandidate.Type == MatchCandidateType.Transaction)
                {
                    transactionMatchCount++;
                    consumedTransactionIds.Add(bestCandidate.Id);

                    _logger.LogDebug(
                        "Receipt {ReceiptId} matched to transaction {TransactionId} with score {Score}",
                        receipt.Id, bestCandidate.Id, match.ConfidenceScore);
                }
                else
                {
                    groupMatchCount++;
                    consumedGroupIds.Add(bestCandidate.Id);

                    // T022: Update group MatchStatus to Proposed
                    var matchedGroup = groups.First(g => g.Id == bestCandidate.Id);
                    matchedGroup.MatchStatus = MatchStatus.Proposed;

                    _logger.LogDebug(
                        "Receipt {ReceiptId} matched to group {GroupId} ({GroupName}) with score {Score}",
                        receipt.Id, bestCandidate.Id, bestCandidate.DisplayName, match.ConfidenceScore);
                }
            }
        }

        // Save all proposals in a single batch
        if (proposals.Any())
        {
            await _matchRepository.AddRangeAsync(proposals);
            // Note: AddRangeAsync already calls SaveChangesAsync, no duplicate save needed
        }

        stopwatch.Stop();

        // T023: Structured logging with group match breakdown
        _logger.LogInformation(
            "Auto-match completed: {ProposedCount} proposed ({TransactionMatches} transactions, {GroupMatches} groups), " +
            "{AmbiguousCount} ambiguous, {ProcessedCount} processed in {Duration}ms",
            proposals.Count, transactionMatchCount, groupMatchCount, ambiguousCount, receipts.Count, stopwatch.ElapsedMilliseconds);

        return new AutoMatchResult(
            proposals.Count,
            receipts.Count,
            ambiguousCount,
            stopwatch.ElapsedMilliseconds,
            proposals)
        {
            TransactionMatchCount = transactionMatchCount,
            GroupMatchCount = groupMatchCount
        };
    }

    /// <summary>
    /// Pre-builds a cache of alias matches for all transaction descriptions.
    /// Reduces O(n*m) alias lookups to O(m) by computing once per unique description.
    /// </summary>
    private async Task<Dictionary<string, VendorAlias?>> BuildAliasCacheAsync(List<Transaction> transactions)
    {
        var cache = new Dictionary<string, VendorAlias?>(StringComparer.OrdinalIgnoreCase);
        var uniqueDescriptions = transactions
            .Select(t => t.Description)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var description in uniqueDescriptions)
        {
            if (!cache.ContainsKey(description))
            {
                cache[description] = await _vendorAliasService.FindMatchingAliasAsync(description);
            }
        }

        return cache;
    }

    /// <summary>
    /// Finds the best match using pre-computed alias cache (no async DB calls).
    /// </summary>
    private MatchFindResult FindBestMatchWithCache(
        Receipt receipt,
        List<Transaction> transactions,
        Guid userId,
        Dictionary<string, VendorAlias?> aliasCache)
    {
        var candidates = new List<(Transaction Transaction, decimal Score, string Reason, Guid? AliasId)>();

        foreach (var transaction in transactions)
        {
            var scoreResult = CalculateConfidenceScoreWithCache(receipt, transaction, aliasCache);

            if (scoreResult.TotalScore >= MinimumConfidenceThreshold)
            {
                candidates.Add((transaction, scoreResult.TotalScore, scoreResult.Reason, scoreResult.MatchedAliasId));
            }
        }

        if (!candidates.Any())
        {
            return new MatchFindResult(null, false);
        }

        // Sort by score descending
        candidates = candidates.OrderByDescending(c => c.Score).ToList();

        // Check for ambiguous matches (multiple matches within threshold)
        if (candidates.Count > 1)
        {
            var topScore = candidates[0].Score;
            var secondScore = candidates[1].Score;

            if (topScore - secondScore <= AmbiguousThreshold)
            {
                return new MatchFindResult(null, true);
            }
        }

        // Create match proposal
        var best = candidates[0];
        var match = new ReceiptTransactionMatch
        {
            ReceiptId = receipt.Id,
            TransactionId = best.Transaction.Id,
            UserId = userId,
            Status = MatchProposalStatus.Proposed,
            ConfidenceScore = best.Score,
            AmountScore = CalculateAmountScore(receipt.AmountExtracted!.Value, best.Transaction.Amount),
            DateScore = CalculateDateScore(receipt.DateExtracted!.Value, best.Transaction.TransactionDate),
            VendorScore = CalculateVendorScoreWithCache(receipt.VendorExtracted, best.Transaction.Description, aliasCache).Score,
            MatchReason = best.Reason,
            MatchedVendorAliasId = best.AliasId,
            IsManualMatch = false
        };

        return new MatchFindResult(match, false);
    }

    /// <summary>
    /// Calculates confidence score using pre-computed alias cache.
    /// </summary>
    private ScoreResult CalculateConfidenceScoreWithCache(
        Receipt receipt,
        Transaction transaction,
        Dictionary<string, VendorAlias?> aliasCache)
    {
        var amountScore = CalculateAmountScore(receipt.AmountExtracted!.Value, transaction.Amount);
        var dateScore = CalculateDateScore(receipt.DateExtracted!.Value, transaction.TransactionDate);
        var (vendorScore, aliasId) = CalculateVendorScoreWithCache(receipt.VendorExtracted, transaction.Description, aliasCache);

        var totalScore = amountScore + dateScore + vendorScore;
        var reason = BuildMatchReason(receipt, transaction, amountScore, dateScore, vendorScore);

        return new ScoreResult(totalScore, reason, aliasId);
    }

    /// <summary>
    /// Scores a MatchCandidate against a receipt using pre-computed alias cache.
    /// Works with both transaction and group candidates.
    /// </summary>
    private (decimal TotalScore, string Reason, Guid? AliasId) ScoreCandidate(
        Receipt receipt,
        MatchCandidate candidate,
        Dictionary<string, VendorAlias?> aliasCache)
    {
        var amountScore = CalculateAmountScore(receipt.AmountExtracted!.Value, candidate.Amount);
        var dateScore = CalculateDateScore(receipt.DateExtracted!.Value, candidate.Date);
        var (vendorScore, aliasId) = CalculateVendorScoreWithCache(receipt.VendorExtracted, candidate.VendorPattern, aliasCache);

        var totalScore = amountScore + dateScore + vendorScore;

        _logger.LogDebug(
            "Score breakdown for {CandidateType} '{Name}': Amount={AmountScore} (${ReceiptAmt} vs ${CandidateAmt}), " +
            "Date={DateScore} ({ReceiptDate} vs {CandidateDate}), Vendor={VendorScore} ('{ReceiptVendor}' vs '{CandidateVendor}')",
            candidate.Type, candidate.DisplayName,
            amountScore, receipt.AmountExtracted, candidate.Amount,
            dateScore, receipt.DateExtracted, candidate.Date,
            vendorScore, receipt.VendorExtracted, candidate.VendorPattern);

        // Build match reason based on candidate type
        var reason = candidate.Type == MatchCandidateType.Group
            ? BuildGroupMatchReason(receipt, candidate, amountScore, dateScore, vendorScore)
            : BuildMatchReasonForCandidate(receipt, candidate, amountScore, dateScore, vendorScore);

        return (totalScore, reason, aliasId);
    }

    /// <summary>
    /// Builds a match reason string for a group candidate.
    /// </summary>
    private static string BuildGroupMatchReason(
        Receipt receipt,
        MatchCandidate candidate,
        decimal amountScore,
        decimal dateScore,
        decimal vendorScore)
    {
        var reasons = new List<string>();

        if (amountScore >= AmountExactPoints)
        {
            reasons.Add($"Group total ${candidate.Amount:F2} exact match");
        }
        else if (amountScore > 0)
        {
            reasons.Add($"Group total ${candidate.Amount:F2} close match (receipt: ${receipt.AmountExtracted:F2})");
        }

        if (dateScore >= DateSameDayPoints)
        {
            reasons.Add("same day");
        }
        else if (dateScore > 0)
        {
            var daysDiff = Math.Abs(receipt.DateExtracted!.Value.DayNumber - candidate.Date.DayNumber);
            reasons.Add($"within {daysDiff} days");
        }

        if (vendorScore > 0)
        {
            reasons.Add("vendor pattern match");
        }

        if (candidate.TransactionCount.HasValue)
        {
            reasons.Add($"{candidate.TransactionCount} transactions");
        }

        return string.Join(", ", reasons);
    }

    /// <summary>
    /// Builds a match reason string for a transaction candidate.
    /// </summary>
    private static string BuildMatchReasonForCandidate(
        Receipt receipt,
        MatchCandidate candidate,
        decimal amountScore,
        decimal dateScore,
        decimal vendorScore)
    {
        var reasons = new List<string>();

        if (amountScore >= AmountExactPoints)
        {
            reasons.Add($"Amount ${candidate.Amount:F2} exact match");
        }
        else if (amountScore > 0)
        {
            reasons.Add($"Amount ${candidate.Amount:F2} close match (receipt: ${receipt.AmountExtracted:F2})");
        }

        if (dateScore >= DateSameDayPoints)
        {
            reasons.Add("same day");
        }
        else if (dateScore > 0)
        {
            var daysDiff = Math.Abs(receipt.DateExtracted!.Value.DayNumber - candidate.Date.DayNumber);
            reasons.Add($"within {daysDiff} days");
        }

        if (vendorScore > 0)
        {
            reasons.Add("vendor match");
        }

        return string.Join(", ", reasons);
    }

    /// <summary>
    /// Finds the best match from a unified candidate pool (transactions + groups).
    /// Returns the best candidate with score and match record.
    /// </summary>
    private (ReceiptTransactionMatch? Match, bool IsAmbiguous, MatchCandidate? BestCandidate) FindBestMatchFromCandidates(
        Receipt receipt,
        List<MatchCandidate> candidates,
        Guid userId,
        Dictionary<string, VendorAlias?> aliasCache)
    {
        var scoredCandidates = new List<(MatchCandidate Candidate, decimal Score, string Reason, Guid? AliasId)>();

        foreach (var candidate in candidates)
        {
            var (score, reason, aliasId) = ScoreCandidate(receipt, candidate, aliasCache);

            _logger.LogInformation(
                "Candidate {CandidateType} {CandidateId} '{Name}' scored {Score} (threshold: {Threshold}) - {Reason}",
                candidate.Type, candidate.Id, candidate.DisplayName, score, MinimumConfidenceThreshold, reason);

            if (score >= MinimumConfidenceThreshold)
            {
                scoredCandidates.Add((candidate, score, reason, aliasId));
            }
        }

        if (!scoredCandidates.Any())
        {
            return (null, false, null);
        }

        // Sort by score descending
        scoredCandidates = scoredCandidates.OrderByDescending(c => c.Score).ToList();

        // Check for ambiguous matches
        if (scoredCandidates.Count > 1)
        {
            var topScore = scoredCandidates[0].Score;
            var secondScore = scoredCandidates[1].Score;

            if (topScore - secondScore <= AmbiguousThreshold)
            {
                return (null, true, null);
            }
        }

        // Create match proposal
        var best = scoredCandidates[0];
        var amountScore = CalculateAmountScore(receipt.AmountExtracted!.Value, best.Candidate.Amount);
        var dateScore = CalculateDateScore(receipt.DateExtracted!.Value, best.Candidate.Date);
        var (vendorScore, _) = CalculateVendorScoreWithCache(receipt.VendorExtracted, best.Candidate.VendorPattern, aliasCache);

        var match = new ReceiptTransactionMatch
        {
            ReceiptId = receipt.Id,
            TransactionId = best.Candidate.Type == MatchCandidateType.Transaction ? best.Candidate.Id : null,
            TransactionGroupId = best.Candidate.Type == MatchCandidateType.Group ? best.Candidate.Id : null,
            UserId = userId,
            Status = MatchProposalStatus.Proposed,
            ConfidenceScore = best.Score,
            AmountScore = amountScore,
            DateScore = dateScore,
            VendorScore = vendorScore,
            MatchReason = best.Reason,
            MatchedVendorAliasId = best.AliasId,
            IsManualMatch = false
        };

        return (match, false, best.Candidate);
    }

    /// <summary>
    /// Calculates vendor score using pre-computed alias cache.
    /// </summary>
    private (decimal Score, Guid? AliasId) CalculateVendorScoreWithCache(
        string? receiptVendor,
        string transactionDescription,
        Dictionary<string, VendorAlias?> aliasCache)
    {
        if (string.IsNullOrWhiteSpace(receiptVendor))
        {
            return (0m, null);
        }

        // Look up alias from cache
        aliasCache.TryGetValue(transactionDescription, out var alias);

        if (alias != null)
        {
            // Check if the alias canonical name matches the receipt vendor
            var similarity = _fuzzyMatchingService.CalculateSimilarity(
                receiptVendor,
                alias.CanonicalName);

            if (similarity >= FuzzyMatchThreshold)
            {
                return (VendorAliasPoints, alias.Id);
            }
        }

        // Fall back to fuzzy matching between receipt vendor and transaction description
        var extractedPattern = ExtractVendorPattern(transactionDescription);
        var fuzzySimilarity = _fuzzyMatchingService.CalculateSimilarity(receiptVendor, extractedPattern);

        if (fuzzySimilarity >= FuzzyMatchThreshold)
        {
            return (VendorFuzzyPoints, null);
        }

        return (0m, null);
    }

    /// <summary>
    /// Finds the best matching transaction for a receipt.
    /// </summary>
    private async Task<MatchFindResult> FindBestMatchAsync(Receipt receipt, List<Transaction> transactions, Guid userId)
    {
        var candidates = new List<(Transaction Transaction, decimal Score, string Reason, Guid? AliasId)>();

        foreach (var transaction in transactions)
        {
            var scoreResult = await CalculateConfidenceScoreAsync(receipt, transaction);

            if (scoreResult.TotalScore >= MinimumConfidenceThreshold)
            {
                candidates.Add((transaction, scoreResult.TotalScore, scoreResult.Reason, scoreResult.MatchedAliasId));
            }
        }

        if (!candidates.Any())
        {
            return new MatchFindResult(null, false);
        }

        // Sort by score descending
        candidates = candidates.OrderByDescending(c => c.Score).ToList();

        // Check for ambiguous matches (multiple matches within threshold)
        if (candidates.Count > 1)
        {
            var topScore = candidates[0].Score;
            var secondScore = candidates[1].Score;

            if (topScore - secondScore <= AmbiguousThreshold)
            {
                return new MatchFindResult(null, true);
            }
        }

        // Create match proposal
        var best = candidates[0];
        var match = new ReceiptTransactionMatch
        {
            ReceiptId = receipt.Id,
            TransactionId = best.Transaction.Id,
            UserId = userId,
            Status = MatchProposalStatus.Proposed,
            ConfidenceScore = best.Score,
            AmountScore = CalculateAmountScore(receipt.AmountExtracted!.Value, best.Transaction.Amount),
            DateScore = CalculateDateScore(receipt.DateExtracted!.Value, best.Transaction.TransactionDate),
            VendorScore = await CalculateVendorScoreAsync(receipt.VendorExtracted, best.Transaction.Description),
            MatchReason = best.Reason,
            MatchedVendorAliasId = best.AliasId,
            IsManualMatch = false
        };

        return new MatchFindResult(match, false);
    }

    /// <summary>
    /// Calculates the confidence score for a receipt-transaction pair.
    /// </summary>
    private async Task<ScoreResult> CalculateConfidenceScoreAsync(Receipt receipt, Transaction transaction)
    {
        var amountScore = CalculateAmountScore(receipt.AmountExtracted!.Value, transaction.Amount);
        var dateScore = CalculateDateScore(receipt.DateExtracted!.Value, transaction.TransactionDate);
        var (vendorScore, aliasId) = await CalculateVendorScoreWithAliasAsync(receipt.VendorExtracted, transaction.Description);

        var totalScore = amountScore + dateScore + vendorScore;

        var reason = BuildMatchReason(receipt, transaction, amountScore, dateScore, vendorScore);

        return new ScoreResult(totalScore, reason, aliasId);
    }

    /// <summary>
    /// Calculates amount match score.
    /// Exact (±$0.10) = 40 pts, Near (±$1.00) = 20 pts
    /// </summary>
    private decimal CalculateAmountScore(decimal receiptAmount, decimal transactionAmount)
    {
        var difference = Math.Abs(receiptAmount - Math.Abs(transactionAmount));

        if (difference <= AmountExactTolerance)
        {
            return AmountExactPoints;
        }

        if (difference <= AmountNearTolerance)
        {
            return AmountNearPoints;
        }

        return 0m;
    }

    /// <summary>
    /// Calculates date match score.
    /// Same day = 35 pts, ±1 day = 30 pts, ±2-3 days = 25 pts, ±4-7 days = 10 pts
    /// </summary>
    private decimal CalculateDateScore(DateOnly receiptDate, DateOnly transactionDate)
    {
        var daysDiff = Math.Abs(receiptDate.DayNumber - transactionDate.DayNumber);

        return daysDiff switch
        {
            0 => DateSameDayPoints,
            1 => DateOneDayPoints,
            <= 3 => DateTwoThreeDaysPoints,
            <= 7 => DateFourSevenDaysPoints,
            _ => 0m
        };
    }

    /// <summary>
    /// Calculates vendor match score.
    /// Alias match = 25 pts, Fuzzy >70% = 15 pts
    /// </summary>
    private async Task<decimal> CalculateVendorScoreAsync(string? receiptVendor, string transactionDescription)
    {
        var (score, _) = await CalculateVendorScoreWithAliasAsync(receiptVendor, transactionDescription);
        return score;
    }

    /// <summary>
    /// Calculates vendor match score and returns matched alias ID.
    /// </summary>
    private async Task<(decimal Score, Guid? AliasId)> CalculateVendorScoreWithAliasAsync(string? receiptVendor, string transactionDescription)
    {
        if (string.IsNullOrWhiteSpace(receiptVendor))
        {
            return (0m, null);
        }

        // First, try exact alias match
        var alias = await _vendorAliasService.FindMatchingAliasAsync(transactionDescription);
        if (alias != null)
        {
            // Check if the alias canonical name matches the receipt vendor
            var similarity = _fuzzyMatchingService.CalculateSimilarity(
                receiptVendor,
                alias.CanonicalName);

            if (similarity >= FuzzyMatchThreshold)
            {
                return (VendorAliasPoints, alias.Id);
            }
        }

        // Fall back to fuzzy matching between receipt vendor and transaction description
        var extractedPattern = ExtractVendorPattern(transactionDescription);
        var fuzzySimilarity = _fuzzyMatchingService.CalculateSimilarity(receiptVendor, extractedPattern);

        if (fuzzySimilarity >= FuzzyMatchThreshold)
        {
            return (VendorFuzzyPoints, null);
        }

        return (0m, null);
    }

    /// <summary>
    /// Extracts vendor pattern from transaction description.
    /// Removes trailing reference numbers, keeps first 3 words.
    /// </summary>
    public static string ExtractVendorPattern(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        // Handle special patterns first
        if (description.StartsWith("AMAZON.COM", StringComparison.OrdinalIgnoreCase))
        {
            return "AMAZON";
        }

        if (description.StartsWith("SQ *", StringComparison.OrdinalIgnoreCase))
        {
            // Square payments: SQ *MERCHANT NAME
            var afterSq = description[4..].Trim();
            var sqWords = afterSq.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return "SQ " + string.Join(" ", sqWords.Take(2)).ToUpperInvariant();
        }

        if (description.StartsWith("PAYPAL *", StringComparison.OrdinalIgnoreCase))
        {
            // PayPal: PAYPAL *MERCHANT NAME
            var afterPaypal = description[8..].Trim();
            var ppWords = afterPaypal.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return "PAYPAL " + string.Join(" ", ppWords.Take(2)).ToUpperInvariant();
        }

        // Remove trailing numbers/codes (likely reference numbers)
        var cleaned = TrailingReferenceRegex().Replace(description, "").Trim();

        // Take first 3 words max (vendor name)
        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Take(3)).ToUpperInvariant();
    }

    [GeneratedRegex(@"\s*[\d#]+[\dA-Z]*$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingReferenceRegex();

    /// <summary>
    /// Extracts vendor name from a transaction group name.
    /// Removes the "(N charges)" suffix if present.
    /// </summary>
    /// <example>
    /// "TWILIO (3 charges)" -> "TWILIO"
    /// "THE HOME DEPOT (2 charges)" -> "THE HOME DEPOT"
    /// "Simple Name" -> "Simple Name"
    /// </example>
    public static string ExtractVendorFromGroupName(string? groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return string.Empty;
        }

        // Match pattern: "VENDOR NAME (N charge[s])"
        var match = GroupChargesRegex().Match(groupName);
        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        return groupName.Trim();
    }

    [GeneratedRegex(@"^(.+?)\s*\(\s*\d+\s*charges?\s*\)$", RegexOptions.IgnoreCase)]
    private static partial Regex GroupChargesRegex();

    /// <summary>
    /// Builds a human-readable match reason.
    /// </summary>
    private string BuildMatchReason(Receipt receipt, Transaction transaction, decimal amountScore, decimal dateScore, decimal vendorScore)
    {
        var parts = new List<string>();

        // Amount reason
        if (amountScore == AmountExactPoints)
        {
            parts.Add($"Amount: exact match (${receipt.AmountExtracted:F2})");
        }
        else if (amountScore == AmountNearPoints)
        {
            parts.Add($"Amount: near match (${receipt.AmountExtracted:F2} vs ${Math.Abs(transaction.Amount):F2})");
        }
        else
        {
            parts.Add("Amount: no match");
        }

        // Date reason
        var daysDiff = Math.Abs(receipt.DateExtracted!.Value.DayNumber - transaction.TransactionDate.DayNumber);
        if (daysDiff == 0)
        {
            parts.Add("Date: same day");
        }
        else if (dateScore > 0)
        {
            parts.Add($"Date: +{daysDiff} days");
        }
        else
        {
            parts.Add("Date: no match");
        }

        // Vendor reason
        if (vendorScore == VendorAliasPoints)
        {
            var pattern = ExtractVendorPattern(transaction.Description);
            parts.Add($"Vendor: alias match ({pattern})");
        }
        else if (vendorScore == VendorFuzzyPoints)
        {
            parts.Add("Vendor: fuzzy match");
        }
        else if (!string.IsNullOrWhiteSpace(receipt.VendorExtracted))
        {
            parts.Add("Vendor: no match");
        }

        return string.Join(", ", parts);
    }

    /// <inheritdoc />
    public async Task<(List<ReceiptTransactionMatch> Items, int TotalCount)> GetProposalsAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        return await _matchRepository.GetProposedByUserIdAsync(userId, page, pageSize);
    }

    /// <inheritdoc />
    public async Task<ReceiptTransactionMatch?> GetMatchAsync(Guid matchId, Guid userId)
    {
        return await _matchRepository.GetByIdAsync(matchId, userId);
    }

    /// <inheritdoc />
    public async Task<ReceiptTransactionMatch> ConfirmMatchAsync(Guid matchId, Guid userId, string? vendorDisplayName = null, string? defaultGLCode = null, string? defaultDepartment = null)
    {
        var match = await _matchRepository.GetByIdAsync(matchId, userId)
            ?? throw new InvalidOperationException("Match not found");

        if (match.Status != MatchProposalStatus.Proposed)
        {
            throw new InvalidOperationException("Can only confirm proposed matches");
        }

        // Update match status
        match.Status = MatchProposalStatus.Confirmed;
        match.ConfirmedAt = DateTime.UtcNow;
        match.ConfirmedByUserId = userId;

        // Link receipt (required for both match types)
        var receipt = match.Receipt ?? throw new InvalidOperationException("Receipt not found");
        receipt.MatchStatus = MatchStatus.Matched;

        string? vendorPattern = null;

        // T040: Handle group matches vs transaction matches
        if (match.TransactionGroupId.HasValue)
        {
            // Group match - update group status
            var group = await _context.TransactionGroups.FindAsync(match.TransactionGroupId.Value)
                ?? throw new InvalidOperationException("Transaction group not found");

            group.MatchedReceiptId = receipt.Id;
            group.MatchStatus = MatchStatus.Matched;
            _context.Entry(group).State = EntityState.Modified;

            vendorPattern = ExtractVendorFromGroupName(group.Name);

            _logger.LogDebug("Confirmed group match: Receipt {ReceiptId} -> Group {GroupId}", receipt.Id, group.Id);
        }
        else
        {
            // Transaction match - existing logic
            var transaction = match.Transaction ?? throw new InvalidOperationException("Transaction not found");

            receipt.MatchedTransactionId = transaction.Id;
            transaction.MatchedReceiptId = receipt.Id;
            transaction.MatchStatus = MatchStatus.Matched;
            _context.Entry(transaction).State = EntityState.Modified;

            vendorPattern = ExtractVendorPattern(transaction.OriginalDescription);

            // Auto-create reimbursable prediction when match is confirmed
            await _predictionService.MarkTransactionReimbursableAsync(userId, transaction.Id);
        }

        // Explicitly mark Receipt as modified
        _context.Entry(receipt).State = EntityState.Modified;

        // Create or update vendor alias
        if (!string.IsNullOrWhiteSpace(vendorPattern))
        {
            var existingAlias = await _vendorAliasService.FindMatchingAliasAsync(vendorPattern);
            if (existingAlias != null)
            {
                // Update existing alias
                existingAlias.MatchCount++;
                existingAlias.LastMatchedAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(defaultGLCode))
                    existingAlias.DefaultGLCode = defaultGLCode;
                if (!string.IsNullOrWhiteSpace(defaultDepartment))
                    existingAlias.DefaultDepartment = defaultDepartment;
                await _vendorAliasService.AddOrUpdateAsync(existingAlias);
                match.MatchedVendorAliasId = existingAlias.Id;
            }
            else
            {
                // Create new alias
                var newAlias = new VendorAlias
                {
                    CanonicalName = receipt.VendorExtracted ?? vendorPattern,
                    AliasPattern = vendorPattern,
                    DisplayName = vendorDisplayName ?? receipt.VendorExtracted ?? vendorPattern,
                    DefaultGLCode = defaultGLCode,
                    DefaultDepartment = defaultDepartment,
                    MatchCount = 1,
                    LastMatchedAt = DateTime.UtcNow,
                    Confidence = 1.0m
                };
                var savedAlias = await _vendorAliasService.AddOrUpdateAsync(newAlias);
                match.MatchedVendorAliasId = savedAlias.Id;
            }
        }

        // UpdateAsync already calls SaveChangesAsync, which saves all tracked changes
        await _matchRepository.UpdateAsync(match);

        _logger.LogInformation("Match {MatchId} confirmed by user {UserId}", matchId, userId);

        return match;
    }

    /// <inheritdoc />
    public async Task<ReceiptTransactionMatch> RejectMatchAsync(Guid matchId, Guid userId)
    {
        var match = await _matchRepository.GetByIdAsync(matchId, userId)
            ?? throw new InvalidOperationException("Match not found");

        if (match.Status != MatchProposalStatus.Proposed)
        {
            throw new InvalidOperationException("Can only reject proposed matches");
        }

        // Update match status
        match.Status = MatchProposalStatus.Rejected;
        match.ConfirmedAt = DateTime.UtcNow;
        match.ConfirmedByUserId = userId;

        // Reset receipt status to Unmatched
        var receipt = await _context.Receipts.FindAsync(match.ReceiptId);
        if (receipt != null)
        {
            receipt.MatchStatus = MatchStatus.Unmatched;
        }

        // T038-T039: Handle group matches - reset group status
        if (match.TransactionGroupId.HasValue)
        {
            var group = await _context.TransactionGroups.FindAsync(match.TransactionGroupId.Value);
            if (group != null)
            {
                group.MatchStatus = MatchStatus.Unmatched;
                group.MatchedReceiptId = null;
                _logger.LogDebug("Reset group {GroupId} status to Unmatched on match rejection", group.Id);
            }
        }

        // UpdateAsync already calls SaveChangesAsync, which saves all tracked changes
        await _matchRepository.UpdateAsync(match);

        _logger.LogInformation("Match {MatchId} rejected by user {UserId}", matchId, userId);

        return match;
    }

    /// <inheritdoc />
    public async Task<ReceiptTransactionMatch> CreateManualMatchAsync(Guid userId, Guid receiptId, Guid transactionId, string? vendorDisplayName = null, string? defaultGLCode = null, string? defaultDepartment = null)
    {
        // Validate receipt exists and is unmatched
        var receipt = await _context.Receipts
            .FirstOrDefaultAsync(r => r.Id == receiptId && r.UserId == userId)
            ?? throw new InvalidOperationException("Receipt not found");

        if (receipt.MatchStatus == MatchStatus.Matched)
        {
            throw new InvalidOperationException("Receipt is already matched");
        }

        // Validate transaction exists and is unmatched
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId)
            ?? throw new InvalidOperationException("Transaction not found");

        if (transaction.MatchStatus == MatchStatus.Matched)
        {
            throw new InvalidOperationException("Transaction is already matched");
        }

        // Create confirmed match
        var match = new ReceiptTransactionMatch
        {
            ReceiptId = receiptId,
            TransactionId = transactionId,
            UserId = userId,
            Status = MatchProposalStatus.Confirmed,
            ConfidenceScore = 100m, // Manual matches are 100% confident
            AmountScore = 0m,
            DateScore = 0m,
            VendorScore = 0m,
            MatchReason = "Manual match by user",
            IsManualMatch = true,
            ConfirmedAt = DateTime.UtcNow,
            ConfirmedByUserId = userId
        };

        // Link receipt and transaction
        receipt.MatchedTransactionId = transaction.Id;
        receipt.MatchStatus = MatchStatus.Matched;
        transaction.MatchedReceiptId = receipt.Id;
        transaction.MatchStatus = MatchStatus.Matched;

        // Create vendor alias
        var pattern = ExtractVendorPattern(transaction.OriginalDescription);
        if (!string.IsNullOrWhiteSpace(pattern))
        {
            var newAlias = new VendorAlias
            {
                CanonicalName = receipt.VendorExtracted ?? pattern,
                AliasPattern = pattern,
                DisplayName = vendorDisplayName ?? receipt.VendorExtracted ?? pattern,
                DefaultGLCode = defaultGLCode,
                DefaultDepartment = defaultDepartment,
                MatchCount = 1,
                LastMatchedAt = DateTime.UtcNow,
                Confidence = 1.0m
            };
            var savedAlias = await _vendorAliasService.AddOrUpdateAsync(newAlias);
            match.MatchedVendorAliasId = savedAlias.Id;
        }

        // AddAsync already calls SaveChangesAsync, which saves all tracked changes
        await _matchRepository.AddAsync(match);

        // Auto-create reimbursable prediction when manual match is created
        // (matching a receipt to a transaction implies it's a business expense)
        await _predictionService.MarkTransactionReimbursableAsync(userId, transactionId);

        _logger.LogInformation("Manual match created by user {UserId}: Receipt {ReceiptId} -> Transaction {TransactionId}",
            userId, receiptId, transactionId);

        return match;
    }

    /// <inheritdoc />
    public async Task<ReceiptTransactionMatch> CreateManualGroupMatchAsync(
        Guid userId,
        Guid receiptId,
        Guid transactionGroupId,
        string? vendorDisplayName = null)
    {
        // Validate receipt exists and is unmatched
        var receipt = await _context.Receipts
            .FirstOrDefaultAsync(r => r.Id == receiptId && r.UserId == userId)
            ?? throw new InvalidOperationException("Receipt not found");

        if (receipt.MatchStatus == MatchStatus.Matched)
        {
            throw new InvalidOperationException("Receipt is already matched");
        }

        // Validate group exists and is unmatched
        var group = await _context.TransactionGroups
            .FirstOrDefaultAsync(g => g.Id == transactionGroupId && g.UserId == userId)
            ?? throw new InvalidOperationException("Transaction group not found");

        if (group.MatchStatus == MatchStatus.Matched)
        {
            throw new InvalidOperationException("Transaction group is already matched");
        }

        // Create confirmed match with group
        var match = new ReceiptTransactionMatch
        {
            ReceiptId = receiptId,
            TransactionId = null, // No individual transaction
            TransactionGroupId = transactionGroupId,
            UserId = userId,
            Status = MatchProposalStatus.Confirmed,
            ConfidenceScore = 100m, // Manual matches are 100% confident
            AmountScore = 0m,
            DateScore = 0m,
            VendorScore = 0m,
            MatchReason = $"Manual match to group: {group.Name}",
            IsManualMatch = true,
            ConfirmedAt = DateTime.UtcNow,
            ConfirmedByUserId = userId
        };

        // Update statuses (the link is through the ReceiptTransactionMatch record)
        receipt.MatchStatus = MatchStatus.Matched;
        group.MatchedReceiptId = receipt.Id;
        group.MatchStatus = MatchStatus.Matched;

        // Create vendor alias from group name
        var vendorPattern = ExtractVendorFromGroupName(group.Name);
        if (!string.IsNullOrWhiteSpace(vendorPattern))
        {
            var newAlias = new VendorAlias
            {
                CanonicalName = receipt.VendorExtracted ?? vendorPattern,
                AliasPattern = vendorPattern,
                DisplayName = vendorDisplayName ?? receipt.VendorExtracted ?? vendorPattern,
                MatchCount = 1,
                LastMatchedAt = DateTime.UtcNow,
                Confidence = 1.0m
            };
            var savedAlias = await _vendorAliasService.AddOrUpdateAsync(newAlias);
            match.MatchedVendorAliasId = savedAlias.Id;
        }

        await _matchRepository.AddAsync(match);

        _logger.LogInformation(
            "Manual group match created by user {UserId}: Receipt {ReceiptId} -> Group {GroupId} ({GroupName})",
            userId, receiptId, transactionGroupId, group.Name);

        return match;
    }

    /// <inheritdoc />
    public async Task<List<MatchCandidate>> GetCandidatesAsync(Guid userId, Guid receiptId, int limit = 10)
    {
        // Validate limit
        if (limit <= 0) limit = 10;
        if (limit > 50) limit = 50;

        // Get the receipt with extracted data
        var receipt = await _context.Receipts
            .FirstOrDefaultAsync(r => r.Id == receiptId && r.UserId == userId)
            ?? throw new InvalidOperationException("Receipt not found");

        if (!receipt.AmountExtracted.HasValue || !receipt.DateExtracted.HasValue)
        {
            _logger.LogWarning("Receipt {ReceiptId} missing extracted data for matching", receiptId);
            return new List<MatchCandidate>();
        }

        // Calculate date range (±7 days)
        var minDate = receipt.DateExtracted.Value.AddDays(-7);
        var maxDate = receipt.DateExtracted.Value.AddDays(7);

        // Query transactions and groups in parallel (T035: exclude grouped transactions)
        var transactionsTask = _context.Transactions
            .Where(t => t.UserId == userId
                && t.MatchStatus == MatchStatus.Unmatched
                && t.GroupId == null  // Exclude grouped transactions
                && t.TransactionDate >= minDate
                && t.TransactionDate <= maxDate)
            .ToListAsync();

        var groupsTask = GetUnmatchedGroupsAsync(userId, minDate, maxDate);

        await Task.WhenAll(transactionsTask, groupsTask);

        var transactions = await transactionsTask;
        var groups = await groupsTask;

        // Build alias cache for vendor scoring
        var aliasCache = await BuildAliasCacheAsync(transactions);

        // Also add group vendor patterns to cache
        foreach (var group in groups)
        {
            var vendorPattern = ExtractVendorFromGroupName(group.Name);
            if (!string.IsNullOrEmpty(vendorPattern) && !aliasCache.ContainsKey(vendorPattern))
            {
                var alias = await _context.VendorAliases
                    .FirstOrDefaultAsync(a => a.CanonicalName == vendorPattern);
                aliasCache[vendorPattern] = alias;
            }
        }

        // Create candidates and score them
        var candidates = new List<MatchCandidate>();

        // Add transaction candidates (filter by amount tolerance)
        foreach (var t in transactions)
        {
            if (Math.Abs(receipt.AmountExtracted!.Value - Math.Abs(t.Amount)) <= AmountNearTolerance)
            {
                var candidate = new MatchCandidate
                {
                    Id = t.Id,
                    Type = MatchCandidateType.Transaction,
                    Amount = Math.Abs(t.Amount),
                    Date = t.TransactionDate,
                    VendorPattern = ExtractVendorPattern(t.Description),
                    DisplayName = t.Description,
                    TransactionCount = null
                };

                // Calculate scores
                var (score, reason, _) = ScoreCandidate(receipt, candidate, aliasCache);
                candidate.ConfidenceScore = score;
                candidate.AmountScore = CalculateAmountScore(receipt.AmountExtracted!.Value, candidate.Amount);
                candidate.DateScore = CalculateDateScore(receipt.DateExtracted!.Value, candidate.Date);
                candidate.VendorScore = CalculateVendorScoreWithCache(receipt.VendorExtracted, candidate.VendorPattern, aliasCache).Score;
                candidate.MatchReason = reason;

                candidates.Add(candidate);
            }
        }

        // Add group candidates (filter by amount tolerance)
        foreach (var g in groups)
        {
            if (Math.Abs(receipt.AmountExtracted!.Value - Math.Abs(g.CombinedAmount)) <= AmountNearTolerance)
            {
                var candidate = new MatchCandidate
                {
                    Id = g.Id,
                    Type = MatchCandidateType.Group,
                    Amount = Math.Abs(g.CombinedAmount),
                    Date = g.DisplayDate,
                    VendorPattern = ExtractVendorFromGroupName(g.Name),
                    DisplayName = g.Name,
                    TransactionCount = g.TransactionCount
                };

                // Calculate scores
                var (score, reason, _) = ScoreCandidate(receipt, candidate, aliasCache);
                candidate.ConfidenceScore = score;
                candidate.AmountScore = CalculateAmountScore(receipt.AmountExtracted!.Value, candidate.Amount);
                candidate.DateScore = CalculateDateScore(receipt.DateExtracted!.Value, candidate.Date);
                candidate.VendorScore = CalculateVendorScoreWithCache(receipt.VendorExtracted, candidate.VendorPattern, aliasCache).Score;
                candidate.MatchReason = reason;

                candidates.Add(candidate);
            }
        }

        // Sort by confidence score descending and take top candidates
        return candidates
            .OrderByDescending(c => c.ConfidenceScore)
            .Take(limit)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<(List<Receipt> Items, int TotalCount)> GetUnmatchedReceiptsAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        var query = _context.Receipts
            .Where(r => r.UserId == userId
                && r.MatchStatus == MatchStatus.Unmatched
                && r.AmountExtracted.HasValue
                && r.DateExtracted.HasValue);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<(List<Transaction> Items, int TotalCount)> GetUnmatchedTransactionsAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        var query = _context.Transactions
            .Where(t => t.UserId == userId && t.MatchStatus == MatchStatus.Unmatched);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.TransactionDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    /// <summary>
    /// Gets unmatched transaction groups within a date range for auto-matching.
    /// Groups are filtered by DisplayDate within the specified range.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="minDate">Minimum date (inclusive)</param>
    /// <param name="maxDate">Maximum date (inclusive)</param>
    /// <returns>List of unmatched transaction groups</returns>
    private async Task<List<TransactionGroup>> GetUnmatchedGroupsAsync(Guid userId, DateOnly minDate, DateOnly maxDate)
    {
        return await _context.TransactionGroups
            .Where(g => g.UserId == userId
                && g.MatchStatus == MatchStatus.Unmatched
                && g.DisplayDate >= minDate
                && g.DisplayDate <= maxDate)
            .OrderByDescending(g => g.DisplayDate)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<MatchingStats> GetStatsAsync(Guid userId)
    {
        var matchCounts = await _matchRepository.GetStatusCountsAsync(userId);
        var matchedCount = matchCounts.GetValueOrDefault(MatchStatus.Matched, 0);
        var proposedCount = matchCounts.GetValueOrDefault(MatchStatus.Proposed, 0);

        var unmatchedReceiptsCount = await _context.Receipts
            .CountAsync(r => r.UserId == userId && r.MatchStatus == MatchStatus.Unmatched);

        var unmatchedTransactionsCount = await _context.Transactions
            .CountAsync(t => t.UserId == userId && t.MatchStatus == MatchStatus.Unmatched);

        var totalReceipts = matchedCount + proposedCount + unmatchedReceiptsCount;
        var autoMatchRate = totalReceipts > 0
            ? (matchedCount + proposedCount) * 100m / totalReceipts
            : 0m;

        var averageConfidence = await _matchRepository.GetAverageConfidenceAsync(userId);

        return new MatchingStats(
            matchedCount,
            proposedCount,
            unmatchedReceiptsCount,
            unmatchedTransactionsCount,
            Math.Round(autoMatchRate, 2),
            Math.Round(averageConfidence, 2));
    }

    /// <inheritdoc />
    public async Task<BatchApproveResult> BatchApproveAsync(Guid userId, decimal? minConfidence = null, List<Guid>? matchIds = null)
    {
        _logger.LogInformation("Batch approve requested by user {UserId} with minConfidence={MinConfidence}, matchIds={MatchIdCount}",
            userId, minConfidence, matchIds?.Count ?? 0);

        // Get proposed matches that meet the criteria
        IQueryable<ReceiptTransactionMatch> query = _context.Set<ReceiptTransactionMatch>()
            .Include(m => m.Receipt)
            .Include(m => m.Transaction)
            .Where(m => m.UserId == userId && m.Status == MatchProposalStatus.Proposed);

        if (matchIds != null && matchIds.Any())
        {
            // Filter by specific IDs
            query = query.Where(m => matchIds.Contains(m.Id));
        }
        else if (minConfidence.HasValue)
        {
            // Filter by confidence threshold
            query = query.Where(m => m.ConfidenceScore >= minConfidence.Value);
        }
        else
        {
            // Default: 90% threshold if nothing specified
            query = query.Where(m => m.ConfidenceScore >= 90m);
        }

        var matchesToApprove = await query.ToListAsync();

        if (!matchesToApprove.Any())
        {
            _logger.LogInformation("No matches found to batch approve for user {UserId}", userId);
            return new BatchApproveResult(0, 0);
        }

        var approvedCount = 0;
        var skippedCount = 0;

        foreach (var match in matchesToApprove)
        {
            try
            {
                // Update match status
                match.Status = MatchProposalStatus.Confirmed;
                match.ConfirmedAt = DateTime.UtcNow;
                match.ConfirmedByUserId = userId;

                // Link receipt and transaction
                var receipt = match.Receipt;
                var transaction = match.Transaction;

                if (receipt == null || transaction == null)
                {
                    _logger.LogWarning("Match {MatchId} has null receipt or transaction, skipping", match.Id);
                    skippedCount++;
                    continue;
                }

                receipt.MatchedTransactionId = transaction.Id;
                receipt.MatchStatus = MatchStatus.Matched;
                transaction.MatchedReceiptId = receipt.Id;
                transaction.MatchStatus = MatchStatus.Matched;

                // Mark entities as modified
                _context.Entry(receipt).State = EntityState.Modified;
                _context.Entry(transaction).State = EntityState.Modified;

                // Create vendor alias if pattern is available
                var pattern = ExtractVendorPattern(transaction.OriginalDescription);
                if (!string.IsNullOrWhiteSpace(pattern))
                {
                    var existingAlias = await _vendorAliasService.FindMatchingAliasAsync(pattern);
                    if (existingAlias != null)
                    {
                        existingAlias.MatchCount++;
                        existingAlias.LastMatchedAt = DateTime.UtcNow;
                        await _vendorAliasService.AddOrUpdateAsync(existingAlias);
                        match.MatchedVendorAliasId = existingAlias.Id;
                    }
                    else
                    {
                        var newAlias = new VendorAlias
                        {
                            CanonicalName = receipt.VendorExtracted ?? pattern,
                            AliasPattern = pattern,
                            DisplayName = receipt.VendorExtracted ?? pattern,
                            MatchCount = 1,
                            LastMatchedAt = DateTime.UtcNow,
                            Confidence = 1.0m
                        };
                        var savedAlias = await _vendorAliasService.AddOrUpdateAsync(newAlias);
                        match.MatchedVendorAliasId = savedAlias.Id;
                    }
                }

                // Mark transaction as reimbursable
                await _predictionService.MarkTransactionReimbursableAsync(userId, transaction.Id);

                approvedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve match {MatchId}", match.Id);
                skippedCount++;
            }
        }

        // Save all changes in a single batch
        await _context.SaveChangesAsync();

        _logger.LogInformation("Batch approve completed for user {UserId}: {ApprovedCount} approved, {SkippedCount} skipped",
            userId, approvedCount, skippedCount);

        return new BatchApproveResult(approvedCount, skippedCount);
    }

    private record MatchFindResult(ReceiptTransactionMatch? Match, bool IsAmbiguous);
    private record ScoreResult(decimal TotalScore, string Reason, Guid? MatchedAliasId);
}
