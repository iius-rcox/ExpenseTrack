using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Jobs;

/// <summary>
/// Hangfire background job for asynchronous report generation with progress tracking.
/// Processes expense lines with real-time progress updates and cancellation support.
/// </summary>
public class ReportGenerationBackgroundJob
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly IReportJobRepository _jobRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategorizationService _categorizationService;
    private readonly IDescriptionNormalizationService _normalizationService;
    private readonly IExpenseReportRepository _reportRepository;
    private readonly IExpensePredictionService? _predictionService;
    private readonly ILogger<ReportGenerationBackgroundJob> _logger;

    // Progress update frequency controls
    private const int ProgressUpdateLineInterval = 10;
    private static readonly TimeSpan ProgressUpdateTimeInterval = TimeSpan.FromSeconds(5);

    public ReportGenerationBackgroundJob(
        ExpenseFlowDbContext dbContext,
        IReportJobRepository jobRepository,
        IMatchRepository matchRepository,
        ITransactionRepository transactionRepository,
        ICategorizationService categorizationService,
        IDescriptionNormalizationService normalizationService,
        IExpenseReportRepository reportRepository,
        ILogger<ReportGenerationBackgroundJob> logger,
        IExpensePredictionService? predictionService = null)
    {
        _dbContext = dbContext;
        _jobRepository = jobRepository;
        _matchRepository = matchRepository;
        _transactionRepository = transactionRepository;
        _categorizationService = categorizationService;
        _normalizationService = normalizationService;
        _reportRepository = reportRepository;
        _predictionService = predictionService;
        _logger = logger;
    }

    /// <summary>
    /// Executes the report generation job with progress tracking and cancellation support.
    /// Per-line retry instead of per-job retry to handle individual categorization failures gracefully.
    /// </summary>
    /// <param name="jobId">The report generation job ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [AutomaticRetry(Attempts = 0)] // Per-line retry, not per-job
    public async Task ExecuteAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting report generation job {JobId}", jobId);

        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
        {
            _logger.LogError("Report generation job {JobId} not found", jobId);
            return;
        }

        // Check if already cancelled before starting
        if (await ShouldCancelAsync(jobId, cancellationToken))
        {
            await MarkAsCancelledAsync(job, cancellationToken);
            return;
        }

        try
        {
            // Update status to Processing
            job.Status = ReportJobStatus.Processing;
            job.StartedAt = DateTime.UtcNow;
            await _jobRepository.UpdateAsync(job, cancellationToken);

            _logger.LogInformation("Job {JobId} processing started for period {Period}", jobId, job.Period);

            // Generate the report with progress tracking
            var reportId = await GenerateReportWithProgressAsync(job, cancellationToken);

            if (reportId.HasValue)
            {
                // Success - update job as completed
                job.Status = ReportJobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                job.GeneratedReportId = reportId.Value;
                await _jobRepository.UpdateAsync(job, cancellationToken);

                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation(
                    "Report generation job {JobId} completed in {Duration}ms. Generated report {ReportId}",
                    jobId, duration.TotalMilliseconds, reportId.Value);
            }
            else
            {
                // Cancelled during processing
                await MarkAsCancelledAsync(job, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Report generation job {JobId} failed", jobId);

            // Check if job was already cancelled - don't overwrite cancellation status
            // This prevents race conditions where cancellation + exception occur simultaneously
            var currentJob = await _jobRepository.GetByIdAsync(job.Id, CancellationToken.None);
            if (currentJob?.Status == ReportJobStatus.Cancelled ||
                currentJob?.Status == ReportJobStatus.CancellationRequested)
            {
                _logger.LogInformation(
                    "Job {JobId} was cancelled during error handling, preserving cancelled status",
                    jobId);
                await MarkAsCancelledAsync(job, CancellationToken.None);
                return; // Don't rethrow - cancellation is expected
            }

            job.Status = ReportJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorMessage = ex.Message;
            job.ErrorDetails = ex.ToString();
            await _jobRepository.UpdateAsync(job, CancellationToken.None);

            throw;
        }
    }

    /// <summary>
    /// Generates the expense report with line-by-line progress tracking.
    /// </summary>
    private async Task<Guid?> GenerateReportWithProgressAsync(
        ReportGenerationJob job,
        CancellationToken cancellationToken)
    {
        // Parse period to get date range
        var (startDate, endDate) = ParsePeriod(job.Period);

        // Get data for report generation
        var confirmedMatches = await _matchRepository.GetConfirmedByPeriodAsync(job.UserId, startDate, endDate);
        var unmatchedTransactions = await _transactionRepository.GetUnmatchedByPeriodAsync(job.UserId, startDate, endDate);

        var totalLines = confirmedMatches.Count + unmatchedTransactions.Count;
        job.TotalLines = totalLines;
        await _jobRepository.UpdateAsync(job, cancellationToken);

        _logger.LogInformation(
            "Job {JobId}: Processing {TotalLines} lines ({MatchCount} matches, {UnmatchedCount} unmatched)",
            job.Id, totalLines, confirmedMatches.Count, unmatchedTransactions.Count);

        if (totalLines == 0)
        {
            _logger.LogWarning("Job {JobId}: No transactions found for period {Period}", job.Id, job.Period);
            job.ErrorMessage = "No transactions found for the specified period";
            job.Status = ReportJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            await _jobRepository.UpdateAsync(job, cancellationToken);
            return null;
        }

        // Get predictions for auto-suggestion (non-blocking)
        var predictedTransactionLookup = await GetPredictionsAsync(job.UserId, startDate, endDate);

        // Create the report entity
        var report = new ExpenseReport
        {
            UserId = job.UserId,
            Period = job.Period,
            Status = ReportStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        var lines = new List<ExpenseLine>();
        var lineOrder = 1;
        var processedLines = 0;
        var failedLines = 0;
        var tierCounters = new TierCounters();
        var missingReceiptCount = 0;
        var lastProgressUpdate = DateTime.UtcNow;
        var processingStartTime = DateTime.UtcNow;

        // Process confirmed matches (with receipts)
        foreach (var match in confirmedMatches)
        {
            try
            {
                var line = await ProcessMatchAsync(match, lineOrder++, predictedTransactionLookup,
                    tierCounters, cancellationToken);
                lines.Add(line);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Job {JobId}: Failed to process match {MatchId}", job.Id, match.Id);
                failedLines++;

                // Create line with minimal data on failure
                lines.Add(CreateFailedLine(match, lineOrder++));
            }

            processedLines++;

            // Update progress and check cancellation periodically (reduces N+1 queries)
            if (ShouldUpdateProgress(processedLines, lastProgressUpdate))
            {
                // Check for cancellation only on progress updates to minimize DB queries
                if (await ShouldCancelAsync(job.Id, cancellationToken))
                {
                    _logger.LogInformation("Job {JobId} cancelled at line {Line}/{Total}", job.Id, processedLines, totalLines);
                    return null;
                }

                var estimatedCompletion = CalculateEstimatedCompletion(
                    processedLines, totalLines, processingStartTime);

                await _jobRepository.UpdateProgressAsync(
                    job.Id, processedLines, failedLines, estimatedCompletion, cancellationToken);

                lastProgressUpdate = DateTime.UtcNow;
            }
        }

        // Process unmatched transactions (missing receipts)
        foreach (var transaction in unmatchedTransactions)
        {
            try
            {
                var line = await ProcessUnmatchedTransactionAsync(
                    transaction, lineOrder++, predictedTransactionLookup,
                    tierCounters, cancellationToken);
                lines.Add(line);
                missingReceiptCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Job {JobId}: Failed to process transaction {TransactionId}",
                    job.Id, transaction.Id);
                failedLines++;

                // Create line with minimal data on failure
                lines.Add(CreateFailedTransactionLine(transaction, lineOrder++));
                missingReceiptCount++;
            }

            processedLines++;

            // Update progress and check cancellation periodically (reduces N+1 queries)
            if (ShouldUpdateProgress(processedLines, lastProgressUpdate))
            {
                // Check for cancellation only on progress updates to minimize DB queries
                if (await ShouldCancelAsync(job.Id, cancellationToken))
                {
                    _logger.LogInformation("Job {JobId} cancelled at line {Line}/{Total}", job.Id, processedLines, totalLines);
                    return null;
                }

                var estimatedCompletion = CalculateEstimatedCompletion(
                    processedLines, totalLines, processingStartTime);

                await _jobRepository.UpdateProgressAsync(
                    job.Id, processedLines, failedLines, estimatedCompletion, cancellationToken);

                lastProgressUpdate = DateTime.UtcNow;
            }
        }

        // Final progress update
        await _jobRepository.UpdateProgressAsync(job.Id, processedLines, failedLines, null, cancellationToken);

        // Set report summary values
        report.Lines = lines;
        report.TotalAmount = lines.Sum(l => l.Amount);
        report.LineCount = lines.Count;
        report.MissingReceiptCount = missingReceiptCount;
        report.Tier1HitCount = tierCounters.Tier1;
        report.Tier2HitCount = tierCounters.Tier2;
        report.Tier3HitCount = tierCounters.Tier3;

        // Save report to database
        await _reportRepository.AddAsync(report, cancellationToken);

        _logger.LogInformation(
            "Job {JobId}: Created draft report {ReportId} with {LineCount} lines, {MissingCount} missing receipts, {FailedCount} failed",
            job.Id, report.Id, report.LineCount, report.MissingReceiptCount, failedLines);

        return report.Id;
    }

    /// <summary>
    /// Processes a confirmed match into an expense line.
    /// </summary>
    private async Task<ExpenseLine> ProcessMatchAsync(
        ReceiptTransactionMatch match,
        int lineOrder,
        Dictionary<Guid, PredictedTransactionDto> predictions,
        TierCounters tierCounters,
        CancellationToken ct)
    {
        var receipt = match.Receipt;

        // Handle both transaction and transaction group matches
        Guid entityId;
        DateOnly transactionDate;
        decimal amount;
        string originalDescription;

        if (match.Transaction != null)
        {
            // Single transaction match
            entityId = match.Transaction.Id;
            transactionDate = match.Transaction.TransactionDate;
            amount = match.Transaction.Amount;
            originalDescription = match.Transaction.OriginalDescription;
        }
        else if (match.TransactionGroup != null)
        {
            // Transaction group match - use group aggregate data
            entityId = match.TransactionGroup.Id;
            transactionDate = match.TransactionGroup.DisplayDate;
            amount = match.TransactionGroup.CombinedAmount;
            originalDescription = match.TransactionGroup.Name;
        }
        else
        {
            throw new InvalidOperationException($"Match {match.Id} has neither Transaction nor TransactionGroup");
        }

        // Get categorization
        var categorization = await GetCategorizationSafeAsync(entityId, match.UserId, ct);

        // Normalize description
        var normalizedDesc = await NormalizeDescriptionSafeAsync(
            originalDescription, match.UserId, ct);

        // Check for prediction
        var hasPrediction = predictions.TryGetValue(entityId, out var prediction);

        // Determine vendor name with fallback chain
        var vendorName = receipt.VendorExtracted
            ?? match.MatchedVendorAlias?.DisplayName
            ?? match.TransactionGroup?.MerchantName; // Use group merchant name if available

        var line = new ExpenseLine
        {
            ReceiptId = receipt.Id,
            TransactionId = match.TransactionId, // Null for group matches, that's OK
            LineOrder = lineOrder,
            ExpenseDate = transactionDate,
            Amount = amount,
            OriginalDescription = originalDescription,
            NormalizedDescription = normalizedDesc,
            VendorName = vendorName,
            HasReceipt = true,
            CreatedAt = DateTime.UtcNow,
            IsAutoSuggested = hasPrediction,
            PredictionId = hasPrediction ? prediction!.PredictionId : null
        };

        // Apply GL code suggestion
        if (categorization?.GL?.TopSuggestion != null)
        {
            var glSuggestion = categorization.GL.TopSuggestion;
            line.GLCodeSuggested = glSuggestion.Code;
            line.GLCode = glSuggestion.Code;
            line.GLCodeTier = glSuggestion.Tier;
            line.GLCodeSource = glSuggestion.Source;
            UpdateTierCounts(glSuggestion.Tier, tierCounters);
        }

        // Apply department suggestion
        if (categorization?.Department?.TopSuggestion != null)
        {
            var deptSuggestion = categorization.Department.TopSuggestion;
            line.DepartmentSuggested = deptSuggestion.Code;
            line.DepartmentCode = deptSuggestion.Code;
            line.DepartmentTier = deptSuggestion.Tier;
            line.DepartmentSource = deptSuggestion.Source;
            UpdateTierCounts(deptSuggestion.Tier, tierCounters);
        }

        return line;
    }

    /// <summary>
    /// Processes an unmatched transaction into an expense line (missing receipt).
    /// </summary>
    private async Task<ExpenseLine> ProcessUnmatchedTransactionAsync(
        Transaction transaction,
        int lineOrder,
        Dictionary<Guid, PredictedTransactionDto> predictions,
        TierCounters tierCounters,
        CancellationToken ct)
    {
        // Get categorization
        var categorization = await GetCategorizationSafeAsync(transaction.Id, transaction.UserId, ct);

        // Normalize description
        var normalizedDesc = await NormalizeDescriptionSafeAsync(
            transaction.OriginalDescription, transaction.UserId, ct);

        // Check for prediction
        var hasPrediction = predictions.TryGetValue(transaction.Id, out var prediction);

        // Determine vendor name: prefer prediction, fallback to extract from description
        var vendorName = hasPrediction && !string.IsNullOrWhiteSpace(prediction!.VendorName)
            ? prediction.VendorName
            : ExtractVendorFromDescription(transaction.OriginalDescription);

        var line = new ExpenseLine
        {
            ReceiptId = null,
            TransactionId = transaction.Id,
            LineOrder = lineOrder,
            ExpenseDate = transaction.TransactionDate,
            Amount = transaction.Amount,
            OriginalDescription = transaction.OriginalDescription,
            NormalizedDescription = normalizedDesc,
            VendorName = vendorName,
            HasReceipt = false,
            MissingReceiptJustification = MissingReceiptJustification.NotProvided,
            CreatedAt = DateTime.UtcNow,
            IsAutoSuggested = hasPrediction,
            PredictionId = hasPrediction ? prediction!.PredictionId : null
        };

        // Apply GL code suggestion (prefer categorization, fallback to prediction)
        if (categorization?.GL?.TopSuggestion != null)
        {
            var glSuggestion = categorization.GL.TopSuggestion;
            line.GLCodeSuggested = glSuggestion.Code;
            line.GLCode = glSuggestion.Code;
            line.GLCodeTier = glSuggestion.Tier;
            line.GLCodeSource = glSuggestion.Source;
            UpdateTierCounts(glSuggestion.Tier, tierCounters);
        }
        else if (hasPrediction && !string.IsNullOrEmpty(prediction!.SuggestedGLCode))
        {
            line.GLCodeSuggested = prediction.SuggestedGLCode;
            line.GLCode = prediction.SuggestedGLCode;
            line.GLCodeSource = "ExpensePrediction";
        }

        // Apply department suggestion
        if (categorization?.Department?.TopSuggestion != null)
        {
            var deptSuggestion = categorization.Department.TopSuggestion;
            line.DepartmentSuggested = deptSuggestion.Code;
            line.DepartmentCode = deptSuggestion.Code;
            line.DepartmentTier = deptSuggestion.Tier;
            line.DepartmentSource = deptSuggestion.Source;
            UpdateTierCounts(deptSuggestion.Tier, tierCounters);
        }
        else if (hasPrediction && !string.IsNullOrEmpty(prediction!.SuggestedDepartment))
        {
            line.DepartmentSuggested = prediction.SuggestedDepartment;
            line.DepartmentCode = prediction.SuggestedDepartment;
            line.DepartmentSource = "ExpensePrediction";
        }

        return line;
    }

    /// <summary>
    /// Creates a minimal expense line when processing fails.
    /// </summary>
    private static ExpenseLine CreateFailedLine(ReceiptTransactionMatch match, int lineOrder)
    {
        // Handle both transaction and transaction group matches
        DateOnly expenseDate;
        decimal amount;
        string description;

        if (match.Transaction != null)
        {
            expenseDate = match.Transaction.TransactionDate;
            amount = match.Transaction.Amount;
            description = match.Transaction.OriginalDescription;
        }
        else if (match.TransactionGroup != null)
        {
            expenseDate = match.TransactionGroup.DisplayDate;
            amount = match.TransactionGroup.CombinedAmount;
            description = match.TransactionGroup.Name;
        }
        else
        {
            // Fallback if both are null (shouldn't happen, but defensive)
            expenseDate = DateOnly.FromDateTime(DateTime.UtcNow);
            amount = 0m;
            description = "Unknown - Match Error";
        }

        return new ExpenseLine
        {
            ReceiptId = match.ReceiptId,
            TransactionId = match.TransactionId, // Null for groups, that's OK
            LineOrder = lineOrder,
            ExpenseDate = expenseDate,
            Amount = amount,
            OriginalDescription = description,
            NormalizedDescription = description,
            HasReceipt = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a minimal expense line when processing fails for unmatched transaction.
    /// </summary>
    private static ExpenseLine CreateFailedTransactionLine(Transaction transaction, int lineOrder) => new()
    {
        ReceiptId = null,
        TransactionId = transaction.Id,
        LineOrder = lineOrder,
        ExpenseDate = transaction.TransactionDate,
        Amount = transaction.Amount,
        OriginalDescription = transaction.OriginalDescription,
        NormalizedDescription = transaction.OriginalDescription,
        HasReceipt = false,
        MissingReceiptJustification = MissingReceiptJustification.NotProvided,
        CreatedAt = DateTime.UtcNow
    };

    /// <summary>
    /// Checks if the job should be cancelled.
    /// </summary>
    private async Task<bool> ShouldCancelAsync(Guid jobId, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return true;
        }

        return await _jobRepository.IsCancellationRequestedAsync(jobId, ct);
    }

    /// <summary>
    /// Marks the job as cancelled.
    /// </summary>
    private async Task MarkAsCancelledAsync(ReportGenerationJob job, CancellationToken ct)
    {
        job.Status = ReportJobStatus.Cancelled;
        job.CompletedAt = DateTime.UtcNow;
        job.ErrorMessage = "Cancelled by user";
        await _jobRepository.UpdateAsync(job, ct);

        _logger.LogInformation("Report generation job {JobId} cancelled", job.Id);
    }

    /// <summary>
    /// Determines if progress should be updated based on line count or time elapsed.
    /// </summary>
    private static bool ShouldUpdateProgress(int processedLines, DateTime lastUpdate)
    {
        return processedLines % ProgressUpdateLineInterval == 0 ||
               DateTime.UtcNow - lastUpdate >= ProgressUpdateTimeInterval;
    }

    /// <summary>
    /// Calculates the estimated completion time based on current processing rate.
    /// </summary>
    private static DateTime? CalculateEstimatedCompletion(
        int processedLines,
        int totalLines,
        DateTime processingStartTime)
    {
        if (processedLines == 0)
        {
            return null;
        }

        var elapsed = DateTime.UtcNow - processingStartTime;
        var avgTimePerLine = elapsed.TotalMilliseconds / processedLines;
        var remainingLines = totalLines - processedLines;
        var estimatedRemainingMs = avgTimePerLine * remainingLines;

        return DateTime.UtcNow.AddMilliseconds(estimatedRemainingMs);
    }

    /// <summary>
    /// Gets predictions for auto-suggestion (non-blocking).
    /// </summary>
    private async Task<Dictionary<Guid, PredictedTransactionDto>> GetPredictionsAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate)
    {
        if (_predictionService == null)
        {
            return new Dictionary<Guid, PredictedTransactionDto>();
        }

        try
        {
            var predictions = await _predictionService.GetPredictedTransactionsForPeriodAsync(
                userId, startDate, endDate);
            return predictions.ToDictionary(p => p.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get predictions, continuing without auto-suggestions");
            return new Dictionary<Guid, PredictedTransactionDto>();
        }
    }

    /// <summary>
    /// Gets categorization safely, returning null on failure.
    /// </summary>
    private async Task<TransactionCategorizationDto?> GetCategorizationSafeAsync(
        Guid transactionId,
        Guid userId,
        CancellationToken ct)
    {
        try
        {
            return await _categorizationService.GetCategorizationAsync(transactionId, userId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Categorization failed for transaction {TransactionId}", transactionId);
            return null;
        }
    }

    /// <summary>
    /// Normalizes description safely, returning original on failure.
    /// </summary>
    private async Task<string> NormalizeDescriptionSafeAsync(
        string description,
        Guid userId,
        CancellationToken ct)
    {
        try
        {
            var result = await _normalizationService.NormalizeAsync(description, userId, ct);
            return result.NormalizedDescription;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Normalization failed for description");
            return description;
        }
    }

    private static void UpdateTierCounts(int tier, TierCounters counters)
    {
        switch (tier)
        {
            case 1:
                counters.Tier1++;
                break;
            case 2:
                counters.Tier2++;
                break;
            case 3:
                counters.Tier3++;
                break;
        }
    }

    /// <summary>
    /// Mutable container for tier hit counts. Used instead of ref parameters
    /// since C# doesn't allow ref parameters in async methods.
    /// </summary>
    private class TierCounters
    {
        public int Tier1 { get; set; }
        public int Tier2 { get; set; }
        public int Tier3 { get; set; }
    }

    private static (DateOnly startDate, DateOnly endDate) ParsePeriod(string period)
    {
        var parts = period.Split('-');
        var year = int.Parse(parts[0]);
        var month = int.Parse(parts[1]);

        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return (startDate, endDate);
    }

    /// <summary>
    /// Extracts a clean vendor name from transaction description.
    /// Removes trailing reference numbers and transaction IDs.
    /// </summary>
    private static string ExtractVendorFromDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        // Common patterns to clean up
        var cleaned = description.Trim();

        // Remove common suffixes: AMAZON MKTPL*WU2K02YC3 → AMAZON MKTPL
        if (cleaned.Contains("*"))
        {
            cleaned = cleaned.Substring(0, cleaned.IndexOf("*")).Trim();
        }

        // Remove trailing reference numbers: PAYPAL *NEWEGGCOM → PAYPAL
        if (cleaned.Contains("  "))
        {
            cleaned = cleaned.Substring(0, cleaned.IndexOf("  ")).Trim();
        }

        // Take first 3 words maximum for readability
        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 3)
        {
            cleaned = string.Join(" ", words.Take(3));
        }

        return cleaned;
    }
}
