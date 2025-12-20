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
        ILogger<MatchingService> logger)
    {
        _context = context;
        _matchRepository = matchRepository;
        _fuzzyMatchingService = fuzzyMatchingService;
        _vendorAliasService = vendorAliasService;
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

        // Get unmatched transactions within date range (pre-filter for performance)
        var transactions = await _context.Transactions
            .Where(t => t.UserId == userId
                && t.MatchStatus == MatchStatus.Unmatched
                && t.TransactionDate >= minDate
                && t.TransactionDate <= maxDate)
            .ToListAsync();

        _logger.LogInformation("Found {ReceiptCount} receipts and {TransactionCount} transactions to match (date range: {MinDate} to {MaxDate})",
            receipts.Count, transactions.Count, minDate, maxDate);

        if (!transactions.Any())
        {
            _logger.LogInformation("No transactions in date range for user {UserId}", userId);
            return new AutoMatchResult(0, receipts.Count, 0, stopwatch.ElapsedMilliseconds, new List<ReceiptTransactionMatch>());
        }

        // Pre-compute alias matches for all unique transaction descriptions (O(m) instead of O(n*m))
        var aliasCache = await BuildAliasCacheAsync(transactions);
        _logger.LogDebug("Built alias cache with {CacheSize} entries", aliasCache.Count);

        var proposals = new List<ReceiptTransactionMatch>();
        var ambiguousCount = 0;

        foreach (var receipt in receipts)
        {
            // Filter transactions by amount tolerance before detailed scoring
            var candidateTransactions = transactions
                .Where(t => Math.Abs(receipt.AmountExtracted!.Value - Math.Abs(t.Amount)) <= AmountNearTolerance)
                .ToList();

            if (!candidateTransactions.Any())
            {
                continue;
            }

            var matchResult = FindBestMatchWithCache(receipt, candidateTransactions, userId, aliasCache);

            if (matchResult.IsAmbiguous)
            {
                ambiguousCount++;
                _logger.LogDebug("Receipt {ReceiptId} has ambiguous matches", receipt.Id);
                continue;
            }

            if (matchResult.Match != null)
            {
                proposals.Add(matchResult.Match);

                // Update receipt status to Proposed
                receipt.MatchStatus = MatchStatus.Proposed;

                // Remove transaction from available list to prevent duplicate matches
                transactions.RemoveAll(t => t.Id == matchResult.Match.TransactionId);
            }
        }

        // Save all proposals in a single batch
        if (proposals.Any())
        {
            await _matchRepository.AddRangeAsync(proposals);
            // Note: AddRangeAsync already calls SaveChangesAsync, no duplicate save needed
        }

        stopwatch.Stop();

        _logger.LogInformation("Auto-match completed: {ProposedCount} proposed, {AmbiguousCount} ambiguous, {ProcessedCount} processed in {Duration}ms",
            proposals.Count, ambiguousCount, receipts.Count, stopwatch.ElapsedMilliseconds);

        return new AutoMatchResult(
            proposals.Count,
            receipts.Count,
            ambiguousCount,
            stopwatch.ElapsedMilliseconds,
            proposals);
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

        // Link receipt and transaction using navigation properties (BUG-005 fix)
        // The match was loaded with Include(), so Receipt and Transaction are already tracked
        var receipt = match.Receipt ?? throw new InvalidOperationException("Receipt not found");
        var transaction = match.Transaction ?? throw new InvalidOperationException("Transaction not found");

        receipt.MatchedTransactionId = transaction.Id;
        receipt.MatchStatus = MatchStatus.Matched;
        transaction.MatchedReceiptId = receipt.Id;
        transaction.MatchStatus = MatchStatus.Matched;

        // Explicitly mark Receipt and Transaction as modified to ensure changes are persisted
        // This fixes BUG-005 where these entities were not being updated after match confirmation
        _context.Entry(receipt).State = EntityState.Modified;
        _context.Entry(transaction).State = EntityState.Modified;

        // Create or update vendor alias
        var pattern = ExtractVendorPattern(transaction.OriginalDescription);
        if (!string.IsNullOrWhiteSpace(pattern))
        {
            var existingAlias = await _vendorAliasService.FindMatchingAliasAsync(pattern);
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
        }

        // UpdateAsync already calls SaveChangesAsync, which saves all tracked changes
        // including receipt and transaction updates
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

        _logger.LogInformation("Manual match created by user {UserId}: Receipt {ReceiptId} -> Transaction {TransactionId}",
            userId, receiptId, transactionId);

        return match;
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

    private record MatchFindResult(ReceiptTransactionMatch? Match, bool IsAmbiguous);
    private record ScoreResult(decimal TotalScore, string Reason, Guid? MatchedAliasId);
}
