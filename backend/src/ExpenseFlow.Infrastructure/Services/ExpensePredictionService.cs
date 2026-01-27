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
        // Track patterns created during this report to handle duplicate vendors within same report
        var localPatternCache = new Dictionary<string, ExpensePattern>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in report.Lines)
        {
            var vendorName = line.VendorName ?? line.OriginalDescription;
            var normalized = await NormalizeVendorAsync(vendorName);

            // Check local cache first (for patterns created in this report iteration)
            if (!localPatternCache.TryGetValue(normalized, out var pattern))
            {
                // Not in local cache, check database
                pattern = await _patternRepository.GetByNormalizedVendorAsync(userId, normalized);
            }

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
                localPatternCache[normalized] = pattern; // Cache for subsequent lines
                _logger.LogDebug("Created new pattern for vendor {Vendor}", normalized);
            }
            else
            {
                // Update existing pattern with exponential decay weighting
                var decayWeight = CalculateDecayWeight(report.CreatedAt);
                UpdatePatternWithNewOccurrence(pattern, line, decayWeight);
                await _patternRepository.UpdateAsync(pattern);
                localPatternCache[normalized] = pattern; // Update cache
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
            // Clear change tracker between reports to prevent stale entity tracking
            // This fixes DbUpdateConcurrencyException when the same vendor appears in multiple reports
            _dbContext.ChangeTracker.Clear();

            totalPatterns += await LearnFromReportAsync(userId, reportId);
        }

        return totalPatterns;
    }

    /// <inheritdoc />
    public async Task<int> RebuildPatternsAsync(Guid userId)
    {
        _logger.LogInformation("Rebuilding all patterns for user {UserId}", userId);

        // Get all reports for the user (Draft, Generated, and Submitted)
        var allReportIds = await _dbContext.ExpenseReports
            .Where(r => r.UserId == userId)
            .OrderBy(r => r.CreatedAt)
            .Select(r => r.Id)
            .ToListAsync();

        _logger.LogInformation("Found {Count} reports to learn from for user {UserId}", allReportIds.Count, userId);

        // Clear existing patterns
        var existingPatterns = await _patternRepository.GetActiveAsync(userId);
        foreach (var pattern in existingPatterns)
        {
            await _patternRepository.DeleteAsync(pattern);
        }
        await _patternRepository.SaveChangesAsync();

        // Rebuild from all reports
        return await LearnFromReportsAsync(userId, allReportIds);
    }

    /// <inheritdoc />
    public async Task<ImportPatternsResponseDto> ImportPatternsAsync(Guid userId, ImportPatternsRequestDto request)
    {
        _logger.LogInformation("Importing {Count} expense entries for user {UserId}", request.Entries.Count, userId);

        var createdCount = 0;
        var updatedCount = 0;

        // Group entries by vendor for efficient pattern creation
        var entriesByVendor = request.Entries
            .GroupBy(e => e.Vendor.ToUpperInvariant().Trim())
            .ToList();

        foreach (var vendorGroup in entriesByVendor)
        {
            var entries = vendorGroup.ToList();
            var firstEntry = entries.First();
            var normalized = await NormalizeVendorAsync(firstEntry.Vendor);

            var existingPattern = await _patternRepository.GetByNormalizedVendorAsync(userId, normalized);

            if (existingPattern == null)
            {
                // Create new pattern from aggregated entries
                var amounts = entries.Select(e => e.Amount).ToList();
                var dates = entries.Select(e => e.Date).ToList();

                // Use mode for GL code and department
                var glCode = entries
                    .Where(e => !string.IsNullOrWhiteSpace(e.GLCode))
                    .GroupBy(e => e.GLCode)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key;

                var department = entries
                    .Where(e => !string.IsNullOrWhiteSpace(e.Department))
                    .GroupBy(e => e.Department)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault()?.Key;

                var pattern = new ExpensePattern
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    NormalizedVendor = normalized,
                    DisplayName = !string.IsNullOrWhiteSpace(firstEntry.DisplayName)
                        ? firstEntry.DisplayName
                        : firstEntry.Vendor,
                    Category = firstEntry.Category ?? glCode,
                    AverageAmount = amounts.Average(),
                    MinAmount = amounts.Min(),
                    MaxAmount = amounts.Max(),
                    OccurrenceCount = entries.Count,
                    LastSeenAt = dates.Max(),
                    DefaultGLCode = glCode,
                    DefaultDepartment = department,
                    ConfirmCount = entries.Count, // Treat imported entries as confirmed
                    RejectCount = 0,
                    IsSuppressed = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _patternRepository.AddAsync(pattern);
                createdCount++;
            }
            else
            {
                // Update existing pattern with new occurrences
                foreach (var entry in entries)
                {
                    existingPattern.OccurrenceCount++;
                    existingPattern.ConfirmCount++;

                    // Weighted average update
                    existingPattern.AverageAmount =
                        ((existingPattern.AverageAmount * (existingPattern.OccurrenceCount - 1)) + entry.Amount)
                        / existingPattern.OccurrenceCount;

                    existingPattern.MinAmount = Math.Min(existingPattern.MinAmount, entry.Amount);
                    existingPattern.MaxAmount = Math.Max(existingPattern.MaxAmount, entry.Amount);

                    if (entry.Date > existingPattern.LastSeenAt)
                    {
                        existingPattern.LastSeenAt = entry.Date;
                    }
                }

                existingPattern.UpdatedAt = DateTime.UtcNow;
                await _patternRepository.UpdateAsync(existingPattern);
                updatedCount++;
            }
        }

        await _patternRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Pattern import complete: {Created} created, {Updated} updated for user {UserId}",
            createdCount, updatedCount, userId);

        return new ImportPatternsResponseDto
        {
            CreatedCount = createdCount,
            UpdatedCount = updatedCount,
            TotalProcessed = request.Entries.Count,
            Message = $"Successfully imported {request.Entries.Count} entries: {createdCount} patterns created, {updatedCount} patterns updated"
        };
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

        // Update pattern confirm count (if pattern exists - manual overrides have no pattern)
        ExpensePattern? pattern = null;
        if (prediction.PatternId.HasValue)
        {
            pattern = await _patternRepository.GetByIdAsync(userId, prediction.PatternId.Value);
        }
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

        // Update pattern reject count (if pattern exists - manual overrides have no pattern)
        ExpensePattern? pattern = null;
        if (prediction.PatternId.HasValue)
        {
            pattern = await _patternRepository.GetByIdAsync(userId, prediction.PatternId.Value);
        }
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

    #region Manual Transaction Marking

    /// <inheritdoc />
    public async Task<PredictionActionResponseDto> MarkTransactionReimbursableAsync(Guid userId, Guid transactionId)
    {
        return await MarkTransactionAsync(userId, transactionId, PredictionStatus.Confirmed, "reimbursable");
    }

    /// <inheritdoc />
    public async Task<PredictionActionResponseDto> MarkTransactionNotReimbursableAsync(Guid userId, Guid transactionId)
    {
        return await MarkTransactionAsync(userId, transactionId, PredictionStatus.Rejected, "not reimbursable");
    }

    /// <summary>
    /// Internal helper to mark a transaction with the specified status.
    /// </summary>
    private async Task<PredictionActionResponseDto> MarkTransactionAsync(
        Guid userId,
        Guid transactionId,
        PredictionStatus status,
        string statusDescription)
    {
        // Validate transaction exists and belongs to user
        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);

        if (transaction == null)
        {
            _logger.LogWarning(
                "Attempted to mark non-existent transaction {TransactionId} for user {UserId}",
                transactionId, userId);

            return new PredictionActionResponseDto
            {
                Success = false,
                Message = "Transaction not found"
            };
        }

        // Check for existing prediction
        var existingPrediction = await _dbContext.TransactionPredictions
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId && p.UserId == userId);

        TransactionPrediction prediction;

        if (existingPrediction != null)
        {
            // Update existing prediction
            existingPrediction.Status = status;
            existingPrediction.IsManualOverride = true;
            existingPrediction.ResolvedAt = DateTime.UtcNow;
            prediction = existingPrediction;

            _logger.LogInformation(
                "Updated existing prediction {PredictionId} to {Status} (manual override) for transaction {TransactionId}",
                existingPrediction.Id, status, transactionId);
        }
        else
        {
            // Create new manual override prediction
            prediction = new TransactionPrediction
            {
                TransactionId = transactionId,
                UserId = userId,
                PatternId = null, // No pattern for manual overrides
                Status = status,
                ConfidenceScore = 1.0m, // User is 100% confident
                ConfidenceLevel = PredictionConfidence.High,
                IsManualOverride = true,
                ResolvedAt = DateTime.UtcNow
            };

            _dbContext.TransactionPredictions.Add(prediction);

            // Save new prediction first to get the database-generated ID
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Created manual override prediction {PredictionId} for transaction {TransactionId} with status {Status}",
                prediction.Id, transactionId, status);
        }

        // Record feedback for audit trail
        var feedbackType = status == PredictionStatus.Confirmed
            ? FeedbackType.Confirmed
            : FeedbackType.Rejected;

        var feedback = new PredictionFeedback
        {
            PredictionId = prediction.Id,
            UserId = userId,
            FeedbackType = feedbackType
        };
        _dbContext.PredictionFeedback.Add(feedback);

        await _dbContext.SaveChangesAsync();

        return new PredictionActionResponseDto
        {
            Success = true,
            NewStatus = status,
            Message = $"Transaction marked as {statusDescription}"
        };
    }

    /// <inheritdoc />
    public async Task<PredictionActionResponseDto> ClearManualOverrideAsync(Guid userId, Guid transactionId)
    {
        // Find manual override prediction for this transaction
        var prediction = await _dbContext.TransactionPredictions
            .FirstOrDefaultAsync(p =>
                p.TransactionId == transactionId &&
                p.UserId == userId &&
                p.IsManualOverride);

        if (prediction == null)
        {
            _logger.LogDebug(
                "No manual override found for transaction {TransactionId}",
                transactionId);

            return new PredictionActionResponseDto
            {
                Success = false,
                Message = "No manual override exists for this transaction"
            };
        }

        // Remove the manual prediction
        _dbContext.TransactionPredictions.Remove(prediction);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Cleared manual override for transaction {TransactionId}",
            transactionId);

        return new PredictionActionResponseDto
        {
            Success = true,
            Message = "Manual override cleared. Transaction may be re-predicted on next generation cycle."
        };
    }

    /// <inheritdoc />
    public async Task<BulkTransactionReimbursabilityResponseDto> BulkMarkTransactionsAsync(
        Guid userId,
        BulkTransactionReimbursabilityRequestDto request)
    {
        var successCount = 0;
        var failedIds = new List<Guid>();

        var status = request.IsReimbursable ? PredictionStatus.Confirmed : PredictionStatus.Rejected;
        var statusDescription = request.IsReimbursable ? "reimbursable" : "not reimbursable";

        _logger.LogInformation(
            "Starting bulk mark {Status} for {Count} transactions for user {UserId}",
            statusDescription, request.TransactionIds.Count, userId);

        foreach (var transactionId in request.TransactionIds)
        {
            try
            {
                var result = await MarkTransactionAsync(userId, transactionId, status, statusDescription);
                if (result.Success)
                {
                    successCount++;
                }
                else
                {
                    failedIds.Add(transactionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to mark transaction {TransactionId} as {Status}",
                    transactionId, statusDescription);
                failedIds.Add(transactionId);
            }
        }

        _logger.LogInformation(
            "Bulk mark {Status} completed for user {UserId}: {SuccessCount}/{TotalCount} succeeded",
            statusDescription, userId, successCount, request.TransactionIds.Count);

        var message = failedIds.Count == 0
            ? $"Successfully marked {successCount} transaction(s) as {statusDescription}"
            : $"Marked {successCount} transaction(s) as {statusDescription}. {failedIds.Count} failed.";

        return new BulkTransactionReimbursabilityResponseDto
        {
            SuccessCount = successCount,
            FailedCount = failedIds.Count,
            FailedTransactionIds = failedIds,
            Message = message
        };
    }

    #endregion

    #region Pattern Management

    /// <inheritdoc />
    public async Task<PatternListResponseDto> GetPatternsAsync(
        Guid userId,
        int page,
        int pageSize,
        bool includeSuppressed = false,
        bool suppressedOnly = false,
        string? category = null,
        string? search = null,
        string sortBy = "accuracyRate",
        string sortOrder = "desc")
    {
        // Get all patterns for this user (includeSuppressed=true to filter in memory)
        var (allPatterns, _) = await _patternRepository.GetPagedAsync(userId, 1, 10000, true);
        var (activeCount, suppressedCount) = await _patternRepository.GetCountsAsync(userId);

        // Apply filters
        IEnumerable<ExpensePattern> filtered = allPatterns;

        // Status filter
        if (suppressedOnly)
        {
            filtered = filtered.Where(p => p.IsSuppressed);
        }
        else if (!includeSuppressed)
        {
            filtered = filtered.Where(p => !p.IsSuppressed);
        }

        // Category filter
        if (!string.IsNullOrWhiteSpace(category))
        {
            filtered = filtered.Where(p =>
                p.Category != null &&
                p.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        }

        // Search filter (vendor name)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.DisplayName.ToLowerInvariant().Contains(searchLower) ||
                p.NormalizedVendor.ToLowerInvariant().Contains(searchLower));
        }

        // Apply sorting
        var ascending = sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);
        filtered = sortBy.ToLowerInvariant() switch
        {
            "displayname" => ascending
                ? filtered.OrderBy(p => p.DisplayName)
                : filtered.OrderByDescending(p => p.DisplayName),
            "averageamount" => ascending
                ? filtered.OrderBy(p => p.AverageAmount)
                : filtered.OrderByDescending(p => p.AverageAmount),
            "occurrencecount" => ascending
                ? filtered.OrderBy(p => p.OccurrenceCount)
                : filtered.OrderByDescending(p => p.OccurrenceCount),
            _ => ascending // accuracyRate (default)
                ? filtered.OrderBy(p => p.ConfirmCount + p.RejectCount > 0
                    ? (decimal)p.ConfirmCount / (p.ConfirmCount + p.RejectCount)
                    : 0m)
                : filtered.OrderByDescending(p => p.ConfirmCount + p.RejectCount > 0
                    ? (decimal)p.ConfirmCount / (p.ConfirmCount + p.RejectCount)
                    : 0m),
        };

        // Get total count after filtering (for pagination)
        var filteredList = filtered.ToList();
        var totalCount = filteredList.Count;

        // Apply pagination
        var pagedPatterns = filteredList
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PatternListResponseDto
        {
            Patterns = pagedPatterns.Select(MapToPatternSummaryDto).ToList(),
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
    public async Task<bool> UpdatePatternReceiptMatchAsync(Guid userId, UpdatePatternReceiptMatchRequestDto request)
    {
        var pattern = await _patternRepository.GetByIdAsync(userId, request.PatternId);

        if (pattern == null)
            return false;

        pattern.RequiresReceiptMatch = request.RequiresReceiptMatch;
        pattern.UpdatedAt = DateTime.UtcNow;
        await _patternRepository.UpdateAsync(pattern);
        await _patternRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Pattern {PatternId} RequiresReceiptMatch set to {RequiresReceiptMatch}",
            request.PatternId, request.RequiresReceiptMatch);
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

    /// <inheritdoc />
    public async Task<BulkPatternActionResponseDto> BulkPatternActionAsync(Guid userId, BulkPatternActionRequestDto request)
    {
        var response = new BulkPatternActionResponseDto();
        var failedIds = new List<Guid>();
        var successCount = 0;

        foreach (var patternId in request.PatternIds)
        {
            try
            {
                var pattern = await _patternRepository.GetByIdAsync(userId, patternId);
                if (pattern == null)
                {
                    failedIds.Add(patternId);
                    continue;
                }

                switch (request.Action.ToLowerInvariant())
                {
                    case "suppress":
                        pattern.IsSuppressed = true;
                        pattern.UpdatedAt = DateTime.UtcNow;
                        await _patternRepository.UpdateAsync(pattern);
                        successCount++;
                        break;

                    case "enable":
                        pattern.IsSuppressed = false;
                        pattern.UpdatedAt = DateTime.UtcNow;
                        await _patternRepository.UpdateAsync(pattern);
                        successCount++;
                        break;

                    case "delete":
                        await _patternRepository.DeleteAsync(pattern);
                        successCount++;
                        break;

                    default:
                        failedIds.Add(patternId);
                        _logger.LogWarning("Unknown bulk action '{Action}' for pattern {PatternId}", request.Action, patternId);
                        break;
                }
            }
            catch (Exception ex)
            {
                failedIds.Add(patternId);
                _logger.LogError(ex, "Failed to process bulk action '{Action}' for pattern {PatternId}", request.Action, patternId);
            }
        }

        await _patternRepository.SaveChangesAsync();

        response.SuccessCount = successCount;
        response.FailedCount = failedIds.Count;
        response.FailedIds = failedIds;

        var actionPastTense = request.Action.ToLowerInvariant() switch
        {
            "suppress" => "suppressed",
            "enable" => "enabled",
            "delete" => "deleted",
            _ => "processed"
        };

        response.Message = failedIds.Count == 0
            ? $"{successCount} pattern(s) {actionPastTense} successfully."
            : $"{successCount} pattern(s) {actionPastTense}, {failedIds.Count} failed.";

        _logger.LogInformation(
            "Bulk pattern action '{Action}' completed for user {UserId}: {SuccessCount} succeeded, {FailedCount} failed",
            request.Action, userId, successCount, failedIds.Count);

        return response;
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

        // Check if pattern requires a confirmed receipt match before generating prediction
        if (matchingPattern.RequiresReceiptMatch)
        {
            var hasConfirmedReceiptMatch = await _dbContext.ReceiptTransactionMatches
                .AnyAsync(m => m.TransactionId == transaction.Id
                            && m.Status == MatchProposalStatus.Confirmed);

            if (!hasConfirmedReceiptMatch)
            {
                _logger.LogDebug(
                    "Pattern {PatternId} ({VendorName}) requires receipt match but " +
                    "transaction {TransactionId} has no confirmed match - skipping prediction",
                    matchingPattern.Id, matchingPattern.DisplayName, transaction.Id);
                return null;
            }
        }

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
        // Filter to only pattern-based predictions (manual overrides have no pattern and aren't auto-suggested)
        var predictedTransactions = await _dbContext.TransactionPredictions
            .Include(p => p.Transaction)
            .Include(p => p.Pattern)
            .Where(p => p.UserId == userId
                && p.Status == PredictionStatus.Pending
                && p.ConfidenceLevel == PredictionConfidence.High
                && p.PatternId.HasValue
                && p.Transaction.TransactionDate >= startDate
                && p.Transaction.TransactionDate <= endDate
                && !p.Pattern!.IsSuppressed)
            .OrderByDescending(p => p.ConfidenceScore)
            .ToListAsync();

        _logger.LogInformation(
            "Found {Count} high-confidence predicted transactions for period",
            predictedTransactions.Count);

        // Check which transactions already have matched receipts
        var transactionIds = predictedTransactions.Select(p => p.TransactionId).ToList();
        var matchedTransactionIds = await _dbContext.ReceiptTransactionMatches
            .Where(m => m.TransactionId.HasValue && transactionIds.Contains(m.TransactionId.Value) && m.Status == MatchProposalStatus.Confirmed)
            .Select(m => m.TransactionId!.Value)
            .ToHashSetAsync();

        return predictedTransactions.Select(p => new PredictedTransactionDto
        {
            TransactionId = p.TransactionId,
            TransactionDate = p.Transaction.TransactionDate,
            Description = p.Transaction.OriginalDescription,
            Amount = p.Transaction.Amount,
            PredictionId = p.Id,
            PatternId = p.PatternId!.Value,  // Safe: filtered by PatternId.HasValue above
            VendorName = p.Pattern!.DisplayName,
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
            RequiresReceiptMatch = pattern.RequiresReceiptMatch,
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
            RequiresReceiptMatch = pattern.RequiresReceiptMatch,
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
