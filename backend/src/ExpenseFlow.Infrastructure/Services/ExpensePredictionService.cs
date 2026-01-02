using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for expense prediction and pattern learning.
/// Learns from approved expense reports to predict which future transactions
/// are likely business expenses.
/// </summary>
public class ExpensePredictionService : IExpensePredictionService
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly IExpensePatternRepository _patternRepository;
    private readonly ITransactionPredictionRepository _predictionRepository;
    private readonly IVendorAliasService _vendorAliasService;
    private readonly ILogger<ExpensePredictionService> _logger;

    // Confidence calculation weights (must sum to 1.0)
    private const decimal FrequencyWeight = 0.40m;
    private const decimal RecencyWeight = 0.25m;
    private const decimal AmountConsistencyWeight = 0.20m;
    private const decimal FeedbackWeight = 0.15m;

    // Recency decay half-life in months
    private const double RecencyHalfLifeMonths = 6.0;

    // Minimum occurrences to generate predictions
    private const int MinOccurrencesForPrediction = 2;

    // Confidence thresholds
    private const decimal HighConfidenceThreshold = 0.75m;
    private const decimal MediumConfidenceThreshold = 0.50m;

    public ExpensePredictionService(
        ExpenseFlowDbContext dbContext,
        IExpensePatternRepository patternRepository,
        ITransactionPredictionRepository predictionRepository,
        IVendorAliasService vendorAliasService,
        ILogger<ExpensePredictionService> logger)
    {
        _dbContext = dbContext;
        _patternRepository = patternRepository;
        _predictionRepository = predictionRepository;
        _vendorAliasService = vendorAliasService;
        _logger = logger;
    }

    #region Pattern Learning

    /// <inheritdoc />
    public async Task<int> LearnFromReportAsync(Guid userId, Guid reportId)
    {
        _logger.LogInformation("Learning expense patterns from report {ReportId} for user {UserId}", reportId, userId);

        var report = await _dbContext.ExpenseReports
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == reportId && r.UserId == userId);

        if (report == null)
        {
            _logger.LogWarning("Report {ReportId} not found for user {UserId}", reportId, userId);
            return 0;
        }

        var patternsUpdated = 0;

        foreach (var line in report.Lines)
        {
            var vendorName = line.VendorName ?? line.OriginalDescription;
            var normalized = await NormalizeVendorAsync(vendorName);
            var pattern = await _patternRepository.GetByNormalizedVendorAsync(userId, normalized);

            if (pattern == null)
            {
                // Create new pattern
                pattern = new ExpensePattern
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    NormalizedVendor = normalized,
                    DisplayName = line.VendorName ?? line.OriginalDescription,
                    Category = line.GLCode, // Use GL code as category
                    AverageAmount = line.Amount,
                    MinAmount = line.Amount,
                    MaxAmount = line.Amount,
                    OccurrenceCount = 1,
                    LastSeenAt = report.CreatedAt,
                    DefaultGLCode = line.GLCode,
                    DefaultDepartment = line.DepartmentCode,
                    ConfirmCount = 0,
                    RejectCount = 0,
                    IsSuppressed = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _patternRepository.AddAsync(pattern);
                _logger.LogDebug("Created new pattern for vendor {Vendor}", normalized);
            }
            else
            {
                // Update existing pattern with exponential decay weighting
                var decayWeight = CalculateDecayWeight(report.CreatedAt);
                UpdatePatternWithNewOccurrence(pattern, line, decayWeight);
                await _patternRepository.UpdateAsync(pattern);
                _logger.LogDebug("Updated pattern for vendor {Vendor}, weight {Weight:F3}", normalized, decayWeight);
            }

            patternsUpdated++;
        }

        await _patternRepository.SaveChangesAsync();
        _logger.LogInformation("Learned {Count} patterns from report {ReportId}", patternsUpdated, reportId);

        return patternsUpdated;
    }

    /// <inheritdoc />
    public async Task<int> LearnFromReportsAsync(Guid userId, IEnumerable<Guid> reportIds)
    {
        var totalPatterns = 0;

        foreach (var reportId in reportIds)
        {
            totalPatterns += await LearnFromReportAsync(userId, reportId);
        }

        return totalPatterns;
    }

    /// <inheritdoc />
    public async Task<int> RebuildPatternsAsync(Guid userId)
    {
        _logger.LogInformation("Rebuilding all patterns for user {UserId}", userId);

        // Get all submitted reports for the user
        var approvedReportIds = await _dbContext.ExpenseReports
            .Where(r => r.UserId == userId && r.Status == ReportStatus.Submitted)
            .OrderBy(r => r.CreatedAt)
            .Select(r => r.Id)
            .ToListAsync();

        // Clear existing patterns
        var existingPatterns = await _patternRepository.GetActiveAsync(userId);
        foreach (var pattern in existingPatterns)
        {
            await _patternRepository.DeleteAsync(pattern);
        }
        await _patternRepository.SaveChangesAsync();

        // Rebuild from historical reports
        return await LearnFromReportsAsync(userId, approvedReportIds);
    }

    #endregion

    #region Prediction Generation

    /// <inheritdoc />
    public async Task<int> GeneratePredictionsAsync(Guid userId, IEnumerable<Guid> transactionIds)
    {
        var patterns = await _patternRepository.GetActiveAsync(userId);

        if (patterns.Count == 0)
        {
            _logger.LogDebug("No active patterns for user {UserId}, skipping prediction", userId);
            return 0;
        }

        var transactions = await _dbContext.Transactions
            .Where(t => transactionIds.Contains(t.Id) && t.UserId == userId)
            .ToListAsync();

        var predictionsGenerated = 0;

        foreach (var transaction in transactions)
        {
            // Skip if prediction already exists
            if (await _predictionRepository.ExistsForTransactionAsync(transaction.Id))
                continue;

            var prediction = await MatchTransactionToPatternAsync(transaction, patterns);
            if (prediction != null)
            {
                await _predictionRepository.AddAsync(prediction);
                predictionsGenerated++;
            }
        }

        await _predictionRepository.SaveChangesAsync();

        // T085: Enhanced logging with pattern context
        _logger.LogInformation(
            "Generated {Count} predictions for user {UserId} from {TransactionCount} transactions using {PatternCount} patterns",
            predictionsGenerated, userId, transactions.Count, patterns.Count);

        return predictionsGenerated;
    }

    /// <inheritdoc />
    public async Task<int> GenerateAllPendingPredictionsAsync(Guid userId)
    {
        // Get transactions without predictions
        var transactionsWithoutPredictions = await _dbContext.Transactions
            .Where(t => t.UserId == userId)
            .Where(t => !_dbContext.TransactionPredictions.Any(p => p.TransactionId == t.Id))
            .Select(t => t.Id)
            .ToListAsync();

        return await GeneratePredictionsAsync(userId, transactionsWithoutPredictions);
    }

    /// <inheritdoc />
    public async Task<PredictionSummaryDto?> GetPredictionForTransactionAsync(Guid transactionId)
    {
        var prediction = await _predictionRepository.GetByTransactionIdAsync(transactionId);

        if (prediction == null || prediction.ConfidenceLevel < PredictionConfidence.Medium)
            return null;

        return MapToPredictionSummaryDto(prediction);
    }

    /// <inheritdoc />
    public async Task<Dictionary<Guid, PredictionSummaryDto>> GetPredictionsForTransactionsAsync(IEnumerable<Guid> transactionIds)
    {
        var predictions = await _predictionRepository.GetByTransactionIdsAsync(transactionIds);

        return predictions
            .Where(kvp => kvp.Value.ConfidenceLevel >= PredictionConfidence.Medium)
            .ToDictionary(kvp => kvp.Key, kvp => MapToPredictionSummaryDto(kvp.Value));
    }

    #endregion

    #region Prediction Actions

    /// <inheritdoc />
    public async Task<PredictionActionResponseDto> ConfirmPredictionAsync(Guid userId, ConfirmPredictionRequestDto request)
    {
        var prediction = await _predictionRepository.GetByIdAsync(userId, request.PredictionId);

        if (prediction == null)
        {
            return new PredictionActionResponseDto
            {
                Success = false,
                NewStatus = PredictionStatus.Pending,
                Message = "Prediction not found"
            };
        }

        prediction.Status = PredictionStatus.Confirmed;
        prediction.ResolvedAt = DateTime.UtcNow;
        await _predictionRepository.UpdateAsync(prediction);

        // Update pattern confirm count
        var pattern = await _patternRepository.GetByIdAsync(userId, prediction.PatternId);
        if (pattern != null)
        {
            pattern.ConfirmCount++;
            pattern.UpdatedAt = DateTime.UtcNow;
            await _patternRepository.UpdateAsync(pattern);
        }

        // Record feedback for observability
        var feedback = new PredictionFeedback
        {
            Id = Guid.NewGuid(),
            PredictionId = prediction.Id,
            UserId = userId,
            FeedbackType = FeedbackType.Confirmed,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.PredictionFeedback.Add(feedback);

        await _dbContext.SaveChangesAsync();

        // T086: Enhanced logging with pattern context for feedback observability
        _logger.LogInformation(
            "Prediction {PredictionId} confirmed for user {UserId}, pattern {PatternId} ({VendorName}) confirmCount now {ConfirmCount}",
            request.PredictionId, userId, pattern?.Id, pattern?.DisplayName, pattern?.ConfirmCount);

        return new PredictionActionResponseDto
        {
            Success = true,
            NewStatus = PredictionStatus.Confirmed,
            Message = "Prediction confirmed successfully"
        };
    }

    /// <inheritdoc />
    public async Task<PredictionActionResponseDto> RejectPredictionAsync(Guid userId, RejectPredictionRequestDto request)
    {
        var prediction = await _predictionRepository.GetByIdAsync(userId, request.PredictionId);

        if (prediction == null)
        {
            return new PredictionActionResponseDto
            {
                Success = false,
                NewStatus = PredictionStatus.Pending,
                Message = "Prediction not found"
            };
        }

        prediction.Status = PredictionStatus.Rejected;
        prediction.ResolvedAt = DateTime.UtcNow;
        await _predictionRepository.UpdateAsync(prediction);

        // Update pattern reject count
        var pattern = await _patternRepository.GetByIdAsync(userId, prediction.PatternId);
        var patternSuppressed = false;
        if (pattern != null)
        {
            pattern.RejectCount++;
            pattern.UpdatedAt = DateTime.UtcNow;

            // T060: Auto-suppress pattern if >3 rejects and <30% confirm rate
            var totalFeedback = pattern.ConfirmCount + pattern.RejectCount;
            var confirmRate = totalFeedback > 0 ? (decimal)pattern.ConfirmCount / totalFeedback : 0m;

            if (pattern.RejectCount > 3 && confirmRate < 0.30m && !pattern.IsSuppressed)
            {
                pattern.IsSuppressed = true;
                patternSuppressed = true;
                _logger.LogInformation(
                    "Pattern {PatternId} ({VendorName}) auto-suppressed: {RejectCount} rejects, {ConfirmRate:P0} confirm rate",
                    pattern.Id, pattern.DisplayName, pattern.RejectCount, confirmRate);
            }

            await _patternRepository.UpdateAsync(pattern);
        }

        // Record feedback for observability
        var feedback = new PredictionFeedback
        {
            Id = Guid.NewGuid(),
            PredictionId = prediction.Id,
            UserId = userId,
            FeedbackType = FeedbackType.Rejected,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.PredictionFeedback.Add(feedback);

        await _dbContext.SaveChangesAsync();

        // T086: Enhanced logging with pattern context for feedback observability
        _logger.LogInformation(
            "Prediction {PredictionId} rejected for user {UserId}, pattern {PatternId} ({VendorName}) rejectCount now {RejectCount}",
            request.PredictionId, userId, pattern?.Id, pattern?.DisplayName, pattern?.RejectCount);

        var message = patternSuppressed
            ? "Prediction rejected. Pattern has been auto-suppressed due to low accuracy."
            : "Prediction rejected successfully";

        return new PredictionActionResponseDto
        {
            Success = true,
            NewStatus = PredictionStatus.Rejected,
            Message = message,
            PatternSuppressed = patternSuppressed
        };
    }

    /// <inheritdoc />
    public async Task<BulkPredictionActionResponseDto> BulkActionAsync(Guid userId, BulkPredictionActionRequestDto request)
    {
        var successCount = 0;
        var failedIds = new List<Guid>();

        foreach (var predictionId in request.PredictionIds)
        {
            PredictionActionResponseDto result;

            if (request.Action == FeedbackType.Confirmed)
            {
                result = await ConfirmPredictionAsync(userId, new ConfirmPredictionRequestDto { PredictionId = predictionId });
            }
            else
            {
                result = await RejectPredictionAsync(userId, new RejectPredictionRequestDto { PredictionId = predictionId });
            }

            if (result.Success)
                successCount++;
            else
                failedIds.Add(predictionId);
        }

        // T086: Log bulk action summary for observability
        _logger.LogInformation(
            "Bulk {Action} completed for user {UserId}: {SuccessCount}/{TotalCount} succeeded, {FailedCount} failed",
            request.Action, userId, successCount, request.PredictionIds.Count, failedIds.Count);

        return new BulkPredictionActionResponseDto
        {
            SuccessCount = successCount,
            FailedCount = failedIds.Count,
            FailedIds = failedIds,
            Message = $"Processed {successCount} of {request.PredictionIds.Count} predictions"
        };
    }

    #endregion

    #region Pattern Management

    /// <inheritdoc />
    public async Task<PatternListResponseDto> GetPatternsAsync(Guid userId, int page, int pageSize, bool includeSuppressed = false)
    {
        var (patterns, totalCount) = await _patternRepository.GetPagedAsync(userId, page, pageSize, includeSuppressed);
        var (activeCount, suppressedCount) = await _patternRepository.GetCountsAsync(userId);

        return new PatternListResponseDto
        {
            Patterns = patterns.Select(MapToPatternSummaryDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            ActiveCount = activeCount,
            SuppressedCount = suppressedCount
        };
    }

    /// <inheritdoc />
    public async Task<PatternDetailDto?> GetPatternAsync(Guid userId, Guid patternId)
    {
        var pattern = await _patternRepository.GetByIdAsync(userId, patternId);
        return pattern != null ? MapToPatternDetailDto(pattern) : null;
    }

    /// <inheritdoc />
    public async Task<bool> UpdatePatternSuppressionAsync(Guid userId, UpdatePatternSuppressionRequestDto request)
    {
        var pattern = await _patternRepository.GetByIdAsync(userId, request.PatternId);

        if (pattern == null)
            return false;

        pattern.IsSuppressed = request.IsSuppressed;
        pattern.UpdatedAt = DateTime.UtcNow;
        await _patternRepository.UpdateAsync(pattern);
        await _patternRepository.SaveChangesAsync();

        _logger.LogInformation("Pattern {PatternId} suppression set to {IsSuppressed}", request.PatternId, request.IsSuppressed);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeletePatternAsync(Guid userId, Guid patternId)
    {
        var pattern = await _patternRepository.GetByIdAsync(userId, patternId);

        if (pattern == null)
            return false;

        await _patternRepository.DeleteAsync(pattern);
        await _patternRepository.SaveChangesAsync();

        _logger.LogInformation("Pattern {PatternId} deleted for user {UserId}", patternId, userId);
        return true;
    }

    #endregion

    #region Prediction Queries

    /// <inheritdoc />
    public async Task<PredictionListResponseDto> GetPredictionsAsync(
        Guid userId,
        int page,
        int pageSize,
        PredictionStatus? status = null,
        PredictionConfidence? minConfidence = null)
    {
        var (predictions, totalCount) = await _predictionRepository.GetPagedAsync(userId, page, pageSize, status, minConfidence);
        var confidenceCounts = await _predictionRepository.GetPendingConfidenceCountsAsync(userId);

        return new PredictionListResponseDto
        {
            Predictions = predictions.Select(MapToPredictionSummaryDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            PendingCount = confidenceCounts.Values.Sum(),
            HighConfidenceCount = confidenceCounts.GetValueOrDefault(PredictionConfidence.High, 0)
        };
    }

    /// <inheritdoc />
    public async Task<PredictionDetailDto?> GetPredictionAsync(Guid userId, Guid predictionId)
    {
        var prediction = await _predictionRepository.GetByIdAsync(userId, predictionId);
        return prediction != null ? MapToPredictionDetailDto(prediction) : null;
    }

    /// <inheritdoc />
    public async Task<PredictionDashboardDto> GetDashboardAsync(Guid userId)
    {
        var confidenceCounts = await _predictionRepository.GetPendingConfidenceCountsAsync(userId);
        var (activeCount, _) = await _patternRepository.GetCountsAsync(userId);
        var stats = await GetAccuracyStatsAsync(userId);

        // Get top pending predictions for quick action
        var topPredictions = await _predictionRepository.GetPendingAsync(userId, PredictionConfidence.Medium);
        var topTransactions = new List<PredictionTransactionDto>();

        foreach (var prediction in topPredictions.Take(5))
        {
            var transaction = await _dbContext.Transactions.FindAsync(prediction.TransactionId);
            if (transaction != null)
            {
                topTransactions.Add(new PredictionTransactionDto
                {
                    Id = transaction.Id,
                    TransactionDate = transaction.TransactionDate,
                    Description = transaction.Description,
                    Amount = transaction.Amount,
                    HasMatchedReceipt = transaction.MatchedReceiptId.HasValue,
                    Prediction = MapToPredictionSummaryDto(prediction)
                });
            }
        }

        return new PredictionDashboardDto
        {
            PendingCount = confidenceCounts.GetValueOrDefault(PredictionConfidence.High, 0) +
                          confidenceCounts.GetValueOrDefault(PredictionConfidence.Medium, 0),
            HighConfidenceCount = confidenceCounts.GetValueOrDefault(PredictionConfidence.High, 0),
            MediumConfidenceCount = confidenceCounts.GetValueOrDefault(PredictionConfidence.Medium, 0),
            ActivePatternCount = activeCount,
            OverallAccuracyRate = stats.AccuracyRate,
            TopPredictions = topTransactions
        };
    }

    /// <inheritdoc />
    public async Task<PredictionAccuracyStatsDto> GetAccuracyStatsAsync(Guid userId)
    {
        var statusCounts = await _predictionRepository.GetStatusCountsAsync(userId);

        var confirmed = statusCounts.GetValueOrDefault(PredictionStatus.Confirmed, 0);
        var rejected = statusCounts.GetValueOrDefault(PredictionStatus.Rejected, 0);
        var ignored = statusCounts.GetValueOrDefault(PredictionStatus.Ignored, 0);
        var total = confirmed + rejected + ignored + statusCounts.GetValueOrDefault(PredictionStatus.Pending, 0);

        var accuracyRate = (confirmed + rejected) > 0
            ? (decimal)confirmed / (confirmed + rejected)
            : 0m;

        // Calculate accuracy by confidence level (simplified - would need additional query in production)
        var stats = new PredictionAccuracyStatsDto
        {
            TotalPredictions = total,
            ConfirmedCount = confirmed,
            RejectedCount = rejected,
            IgnoredCount = ignored,
            AccuracyRate = accuracyRate,
            HighConfidenceAccuracyRate = accuracyRate, // Placeholder - would filter by confidence
            MediumConfidenceAccuracyRate = accuracyRate // Placeholder - would filter by confidence
        };

        // T087: Log accuracy metrics for observability dashboard (FR-015)
        _logger.LogInformation(
            "Prediction accuracy stats for user {UserId}: " +
            "Total={TotalPredictions}, Confirmed={ConfirmedCount}, Rejected={RejectedCount}, " +
            "Ignored={IgnoredCount}, AccuracyRate={AccuracyRate:P1}",
            userId, total, confirmed, rejected, ignored, accuracyRate);

        return stats;
    }

    /// <inheritdoc />
    public async Task<PredictionAvailabilityDto> CheckAvailabilityAsync(Guid userId)
    {
        var counts = await _patternRepository.GetCountsAsync(userId);
        var patternCount = counts.ActiveCount + counts.SuppressedCount;
        var isAvailable = patternCount > 0;

        var message = isAvailable
            ? $"Predictions available based on {patternCount} learned expense pattern{(patternCount > 1 ? "s" : "")}."
            : "No predictions available yet. Submit expense reports to help the system learn your expense patterns.";

        return new PredictionAvailabilityDto
        {
            IsAvailable = isAvailable,
            PatternCount = patternCount,
            Message = message
        };
    }

    #endregion

    #region Transaction Enrichment

    /// <inheritdoc />
    public async Task<List<PredictionTransactionDto>> EnrichWithPredictionsAsync(IEnumerable<TransactionSummaryDto> transactions)
    {
        var transactionList = transactions.ToList();
        var transactionIds = transactionList.Select(t => t.Id).ToList();
        var predictions = await GetPredictionsForTransactionsAsync(transactionIds);

        return transactionList.Select(t => new PredictionTransactionDto
        {
            Id = t.Id,
            TransactionDate = t.TransactionDate,
            Description = t.Description,
            Amount = t.Amount,
            HasMatchedReceipt = t.HasMatchedReceipt,
            Prediction = predictions.GetValueOrDefault(t.Id)
        }).ToList();
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Calculates exponential decay weight based on date age.
    /// Uses 6-month half-life: weight = 2^(-monthsAgo / 6)
    /// </summary>
    private decimal CalculateDecayWeight(DateTime reportDate)
    {
        var monthsAgo = (DateTime.UtcNow - reportDate).TotalDays / 30.0;
        var weight = Math.Pow(2, -monthsAgo / RecencyHalfLifeMonths);
        return (decimal)Math.Max(0.01, weight); // Minimum weight of 1%
    }

    /// <summary>
    /// Calculates multi-signal confidence score for a prediction.
    /// </summary>
    private decimal CalculateConfidenceScore(ExpensePattern pattern, decimal transactionAmount)
    {
        // Frequency signal: logarithmic scaling, capped at 10 occurrences
        var frequencyScore = Math.Min(1.0m, (decimal)Math.Log10(pattern.OccurrenceCount + 1) / (decimal)Math.Log10(11));

        // Recency signal: based on LastSeenAt
        var recencyScore = CalculateDecayWeight(pattern.LastSeenAt);

        // Amount consistency signal: how close is the amount to the pattern average
        var amountDeviation = Math.Abs(transactionAmount - pattern.AverageAmount) / Math.Max(pattern.AverageAmount, 1m);
        var amountScore = Math.Max(0m, 1.0m - amountDeviation);

        // Feedback signal: positive feedback boosts confidence
        var totalFeedback = pattern.ConfirmCount + pattern.RejectCount;
        var feedbackScore = totalFeedback > 0
            ? (decimal)pattern.ConfirmCount / totalFeedback
            : 0.5m; // Neutral if no feedback

        // Weighted combination
        var score = (frequencyScore * FrequencyWeight) +
                   (recencyScore * RecencyWeight) +
                   (amountScore * AmountConsistencyWeight) +
                   (feedbackScore * FeedbackWeight);

        return Math.Min(1.0m, Math.Max(0m, score));
    }

    /// <summary>
    /// Maps confidence score to confidence level enum.
    /// </summary>
    private static PredictionConfidence GetConfidenceLevel(decimal score)
    {
        if (score >= HighConfidenceThreshold)
            return PredictionConfidence.High;
        if (score >= MediumConfidenceThreshold)
            return PredictionConfidence.Medium;
        return PredictionConfidence.Low;
    }

    /// <summary>
    /// Normalizes vendor name using VendorAlias system.
    /// </summary>
    private async Task<string> NormalizeVendorAsync(string vendorName)
    {
        var alias = await _vendorAliasService.FindMatchingAliasAsync(vendorName);
        return alias?.CanonicalName ?? vendorName.ToUpperInvariant().Trim();
    }

    /// <summary>
    /// Updates pattern statistics with a new expense occurrence.
    /// </summary>
    private void UpdatePatternWithNewOccurrence(ExpensePattern pattern, ExpenseLine line, decimal decayWeight)
    {
        // Weighted moving average for amount
        var totalWeight = 1.0m + decayWeight;
        pattern.AverageAmount = ((pattern.AverageAmount * decayWeight) + line.Amount) / totalWeight;

        // Update min/max
        pattern.MinAmount = Math.Min(pattern.MinAmount, line.Amount);
        pattern.MaxAmount = Math.Max(pattern.MaxAmount, line.Amount);

        // Update counts and timestamps
        pattern.OccurrenceCount++;
        pattern.LastSeenAt = DateTime.UtcNow;
        pattern.UpdatedAt = DateTime.UtcNow;

        // Update defaults if provided
        if (!string.IsNullOrEmpty(line.GLCode))
            pattern.DefaultGLCode = line.GLCode;
        if (!string.IsNullOrEmpty(line.DepartmentCode))
            pattern.DefaultDepartment = line.DepartmentCode;
    }

    /// <summary>
    /// Matches a transaction to the best matching pattern.
    /// </summary>
    private async Task<TransactionPrediction?> MatchTransactionToPatternAsync(Transaction transaction, List<ExpensePattern> patterns)
    {
        var normalizedVendor = await NormalizeVendorAsync(transaction.Description);

        var matchingPattern = patterns.FirstOrDefault(p =>
            p.NormalizedVendor.Equals(normalizedVendor, StringComparison.OrdinalIgnoreCase));

        if (matchingPattern == null || matchingPattern.OccurrenceCount < MinOccurrencesForPrediction)
            return null;

        var confidenceScore = CalculateConfidenceScore(matchingPattern, transaction.Amount);
        var confidenceLevel = GetConfidenceLevel(confidenceScore);

        // T085: Log prediction generation with confidence scores for observability
        _logger.LogDebug(
            "Matched transaction {TransactionId} to pattern {PatternId} ({VendorName}): " +
            "ConfidenceScore={ConfidenceScore:F3}, ConfidenceLevel={ConfidenceLevel}",
            transaction.Id, matchingPattern.Id, matchingPattern.DisplayName,
            confidenceScore, confidenceLevel);

        return new TransactionPrediction
        {
            Id = Guid.NewGuid(),
            PatternId = matchingPattern.Id,
            TransactionId = transaction.Id,
            UserId = transaction.UserId,
            ConfidenceScore = confidenceScore,
            ConfidenceLevel = confidenceLevel,
            Status = PredictionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region Draft Pre-Population (User Story 2)

    /// <inheritdoc />
    public async Task<List<PredictedTransactionDto>> GetPredictedTransactionsForPeriodAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate)
    {
        _logger.LogInformation(
            "Getting predicted transactions for user {UserId} from {StartDate} to {EndDate}",
            userId, startDate, endDate);

        // Query transactions with high-confidence pending predictions in the date range
        // Only include High confidence for auto-selection (Medium shows badge but isn't auto-selected)
        var predictedTransactions = await _dbContext.TransactionPredictions
            .Include(p => p.Transaction)
            .Include(p => p.Pattern)
            .Where(p => p.UserId == userId
                && p.Status == PredictionStatus.Pending
                && p.ConfidenceLevel == PredictionConfidence.High
                && p.Transaction.TransactionDate >= startDate
                && p.Transaction.TransactionDate <= endDate
                && !p.Pattern.IsSuppressed)
            .OrderByDescending(p => p.ConfidenceScore)
            .ToListAsync();

        _logger.LogInformation(
            "Found {Count} high-confidence predicted transactions for period",
            predictedTransactions.Count);

        // Check which transactions already have matched receipts
        var transactionIds = predictedTransactions.Select(p => p.TransactionId).ToList();
        var matchedTransactionIds = await _dbContext.ReceiptTransactionMatches
            .Where(m => transactionIds.Contains(m.TransactionId) && m.Status == MatchProposalStatus.Confirmed)
            .Select(m => m.TransactionId)
            .ToHashSetAsync();

        return predictedTransactions.Select(p => new PredictedTransactionDto
        {
            TransactionId = p.TransactionId,
            TransactionDate = p.Transaction.TransactionDate,
            Description = p.Transaction.OriginalDescription,
            Amount = p.Transaction.Amount,
            PredictionId = p.Id,
            PatternId = p.PatternId,
            VendorName = p.Pattern.DisplayName,
            ConfidenceScore = p.ConfidenceScore,
            ConfidenceLevel = p.ConfidenceLevel,
            SuggestedCategory = p.Pattern.Category,
            SuggestedGLCode = p.Pattern.DefaultGLCode,
            SuggestedDepartment = p.Pattern.DefaultDepartment,
            HasMatchedReceipt = matchedTransactionIds.Contains(p.TransactionId)
        }).ToList();
    }

    #endregion

    #region DTO Mapping

    private static PatternSummaryDto MapToPatternSummaryDto(ExpensePattern pattern)
    {
        var totalFeedback = pattern.ConfirmCount + pattern.RejectCount;
        var accuracyRate = totalFeedback > 0
            ? (decimal)pattern.ConfirmCount / totalFeedback
            : 0m;

        return new PatternSummaryDto
        {
            Id = pattern.Id,
            DisplayName = pattern.DisplayName,
            Category = pattern.Category,
            AverageAmount = pattern.AverageAmount,
            OccurrenceCount = pattern.OccurrenceCount,
            LastSeenAt = pattern.LastSeenAt,
            IsSuppressed = pattern.IsSuppressed,
            AccuracyRate = accuracyRate
        };
    }

    private static PatternDetailDto MapToPatternDetailDto(ExpensePattern pattern)
    {
        var totalFeedback = pattern.ConfirmCount + pattern.RejectCount;
        var accuracyRate = totalFeedback > 0
            ? (decimal)pattern.ConfirmCount / totalFeedback
            : 0m;

        return new PatternDetailDto
        {
            Id = pattern.Id,
            NormalizedVendor = pattern.NormalizedVendor,
            DisplayName = pattern.DisplayName,
            Category = pattern.Category,
            AverageAmount = pattern.AverageAmount,
            MinAmount = pattern.MinAmount,
            MaxAmount = pattern.MaxAmount,
            OccurrenceCount = pattern.OccurrenceCount,
            LastSeenAt = pattern.LastSeenAt,
            DefaultGLCode = pattern.DefaultGLCode,
            DefaultDepartment = pattern.DefaultDepartment,
            ConfirmCount = pattern.ConfirmCount,
            RejectCount = pattern.RejectCount,
            IsSuppressed = pattern.IsSuppressed,
            AccuracyRate = accuracyRate,
            CreatedAt = pattern.CreatedAt,
            UpdatedAt = pattern.UpdatedAt
        };
    }

    private static PredictionSummaryDto MapToPredictionSummaryDto(TransactionPrediction prediction)
    {
        return new PredictionSummaryDto
        {
            Id = prediction.Id,
            TransactionId = prediction.TransactionId,
            PatternId = prediction.PatternId,
            VendorName = prediction.Pattern?.DisplayName ?? string.Empty,
            ConfidenceScore = prediction.ConfidenceScore,
            ConfidenceLevel = prediction.ConfidenceLevel,
            Status = prediction.Status,
            SuggestedCategory = prediction.Pattern?.Category,
            SuggestedGLCode = prediction.Pattern?.DefaultGLCode
        };
    }

    private static PredictionDetailDto MapToPredictionDetailDto(TransactionPrediction prediction)
    {
        return new PredictionDetailDto
        {
            Id = prediction.Id,
            TransactionId = prediction.TransactionId,
            PatternId = prediction.PatternId,
            VendorName = prediction.Pattern?.DisplayName ?? string.Empty,
            ConfidenceScore = prediction.ConfidenceScore,
            ConfidenceLevel = prediction.ConfidenceLevel,
            Status = prediction.Status,
            SuggestedCategory = prediction.Pattern?.Category,
            SuggestedGLCode = prediction.Pattern?.DefaultGLCode,
            SuggestedDepartment = prediction.Pattern?.DefaultDepartment,
            PatternAverageAmount = prediction.Pattern?.AverageAmount ?? 0m,
            PatternOccurrenceCount = prediction.Pattern?.OccurrenceCount ?? 0,
            CreatedAt = prediction.CreatedAt,
            ResolvedAt = prediction.ResolvedAt
        };
    }

    #endregion
}
