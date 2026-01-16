using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for expense report operations including draft generation and editing.
/// </summary>
public class ReportService : IReportService
{
    private readonly IExpenseReportRepository _reportRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategorizationService _categorizationService;
    private readonly IDescriptionNormalizationService _normalizationService;
    private readonly IVendorAliasService _vendorAliasService;
    private readonly IDescriptionCacheService _descriptionCacheService;
    private readonly IExpensePredictionService? _predictionService;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IExpenseReportRepository reportRepository,
        IMatchRepository matchRepository,
        ITransactionRepository transactionRepository,
        ICategorizationService categorizationService,
        IDescriptionNormalizationService normalizationService,
        IVendorAliasService vendorAliasService,
        IDescriptionCacheService descriptionCacheService,
        ILogger<ReportService> logger,
        IExpensePredictionService? predictionService = null)
    {
        _reportRepository = reportRepository;
        _matchRepository = matchRepository;
        _transactionRepository = transactionRepository;
        _categorizationService = categorizationService;
        _normalizationService = normalizationService;
        _vendorAliasService = vendorAliasService;
        _descriptionCacheService = descriptionCacheService;
        _predictionService = predictionService;
        _logger = logger;
    }

    public async Task<ExpenseReportDto> GenerateDraftAsync(Guid userId, string period, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating draft report for user {UserId}, period {Period}", userId, period);

        // Parse period to get date range
        var (startDate, endDate) = ParsePeriod(period);

        // Check for existing draft
        var existingDraft = await _reportRepository.GetDraftByUserAndPeriodAsync(userId, period, ct);
        if (existingDraft != null)
        {
            _logger.LogInformation("Deleting existing draft {ReportId} for period {Period}", existingDraft.Id, period);
            await _reportRepository.SoftDeleteAsync(existingDraft.Id, ct);
        }

        // Get confirmed matches for the period
        var confirmedMatches = await _matchRepository.GetConfirmedByPeriodAsync(userId, startDate, endDate);
        _logger.LogInformation("Found {MatchCount} confirmed matches for period {Period}", confirmedMatches.Count, period);

        // Get unmatched transactions for the period
        var unmatchedTransactions = await _transactionRepository.GetUnmatchedByPeriodAsync(userId, startDate, endDate);
        _logger.LogInformation("Found {UnmatchedCount} unmatched transactions for period {Period}", unmatchedTransactions.Count, period);

        // Feature 023: Get predicted transactions for auto-suggestion
        var predictedTransactionLookup = new Dictionary<Guid, PredictedTransactionDto>();
        if (_predictionService != null)
        {
            try
            {
                var predictions = await _predictionService.GetPredictedTransactionsForPeriodAsync(userId, startDate, endDate);
                predictedTransactionLookup = predictions.ToDictionary(p => p.TransactionId);
                _logger.LogInformation(
                    "Found {PredictionCount} predicted transactions for period {Period}",
                    predictions.Count, period);
            }
            catch (Exception ex)
            {
                // Non-blocking: predictions are enhancement, not critical
                _logger.LogWarning(ex, "Failed to get predictions for draft report, continuing without auto-suggestions");
            }
        }

        // Create the report
        var report = new ExpenseReport
        {
            UserId = userId,
            Period = period,
            Status = ReportStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        var lines = new List<ExpenseLine>();
        var lineOrder = 1;
        var tier1Hits = 0;
        var tier2Hits = 0;
        var tier3Hits = 0;
        var missingReceiptCount = 0;

        // Process confirmed matches (has receipt)
        foreach (var match in confirmedMatches)
        {
            var receipt = match.Receipt;

            // Collect transactions to process - either single transaction or group members
            var transactionsToProcess = new List<(Transaction transaction, string? vendorName)>();

            if (match.TransactionId != null && match.Transaction != null)
            {
                // Individual transaction match
                transactionsToProcess.Add((match.Transaction, receipt.VendorExtracted ?? match.MatchedVendorAlias?.DisplayName));
            }
            else if (match.TransactionGroupId != null && match.TransactionGroup != null)
            {
                // Group match - expand to all transactions in the group
                var group = match.TransactionGroup;
                var groupVendorName = receipt.VendorExtracted ?? match.MatchedVendorAlias?.DisplayName ?? group.Name;

                foreach (var groupTransaction in group.Transactions.OrderBy(t => t.TransactionDate).ThenBy(t => t.CreatedAt))
                {
                    transactionsToProcess.Add((groupTransaction, groupVendorName));
                }
            }

            // Process each transaction
            foreach (var (transaction, vendorName) in transactionsToProcess)
            {
                // Get categorization suggestions for the transaction
                var categorization = await GetCategorizationSafeAsync(transaction.Id, userId, ct);

                // Normalize the description
                var normalizedDesc = await NormalizeDescriptionSafeAsync(transaction.OriginalDescription, userId, ct);

                // Feature 023: Check if this transaction has a prediction
                var hasPrediction = predictedTransactionLookup.TryGetValue(transaction.Id, out var prediction);

                var line = new ExpenseLine
                {
                    // ReportId will be set automatically by EF Core via navigation property
                    ReceiptId = receipt.Id,
                    TransactionId = transaction.Id,
                    LineOrder = lineOrder++,
                    ExpenseDate = transaction.TransactionDate,
                    Amount = transaction.Amount,
                    OriginalDescription = transaction.OriginalDescription,
                    NormalizedDescription = normalizedDesc,
                    VendorName = vendorName,
                    HasReceipt = true,
                    CreatedAt = DateTime.UtcNow,
                    // Feature 023: Mark as auto-suggested if prediction exists
                    IsAutoSuggested = hasPrediction,
                    PredictionId = hasPrediction ? prediction!.PredictionId : null
                };

                // Apply GL code suggestion
                if (categorization?.GL?.TopSuggestion != null)
                {
                    var glSuggestion = categorization.GL.TopSuggestion;
                    line.GLCodeSuggested = glSuggestion.Code;
                    line.GLCode = glSuggestion.Code; // Pre-fill with suggestion
                    line.GLCodeTier = glSuggestion.Tier;
                    line.GLCodeSource = glSuggestion.Source;
                    UpdateTierCounts(glSuggestion.Tier, ref tier1Hits, ref tier2Hits, ref tier3Hits);
                }

                // Apply department suggestion
                if (categorization?.Department?.TopSuggestion != null)
                {
                    var deptSuggestion = categorization.Department.TopSuggestion;
                    line.DepartmentSuggested = deptSuggestion.Code;
                    line.DepartmentCode = deptSuggestion.Code; // Pre-fill with suggestion
                    line.DepartmentTier = deptSuggestion.Tier;
                    line.DepartmentSource = deptSuggestion.Source;
                    UpdateTierCounts(deptSuggestion.Tier, ref tier1Hits, ref tier2Hits, ref tier3Hits);
                }

                lines.Add(line);
            }
        }

        // Process unmatched transactions (missing receipt)
        foreach (var transaction in unmatchedTransactions)
        {
            // Get categorization suggestions
            var categorization = await GetCategorizationSafeAsync(transaction.Id, userId, ct);

            // Normalize the description
            var normalizedDesc = await NormalizeDescriptionSafeAsync(transaction.OriginalDescription, userId, ct);

            // Feature 023: Check if this transaction has a prediction
            var hasPrediction = predictedTransactionLookup.TryGetValue(transaction.Id, out var prediction);

            var line = new ExpenseLine
            {
                // ReportId will be set automatically by EF Core via navigation property
                ReceiptId = null,
                TransactionId = transaction.Id,
                LineOrder = lineOrder++,
                ExpenseDate = transaction.TransactionDate,
                Amount = transaction.Amount,
                OriginalDescription = transaction.OriginalDescription,
                NormalizedDescription = normalizedDesc,
                VendorName = hasPrediction ? prediction!.VendorName : null, // Use predicted vendor if available
                HasReceipt = false,
                MissingReceiptJustification = MissingReceiptJustification.NotProvided,
                CreatedAt = DateTime.UtcNow,
                // Feature 023: Mark as auto-suggested if prediction exists
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
                UpdateTierCounts(glSuggestion.Tier, ref tier1Hits, ref tier2Hits, ref tier3Hits);
            }
            else if (hasPrediction && !string.IsNullOrEmpty(prediction!.SuggestedGLCode))
            {
                // Feature 023: Use prediction's GL code if no other categorization available
                line.GLCodeSuggested = prediction.SuggestedGLCode;
                line.GLCode = prediction.SuggestedGLCode;
                line.GLCodeSource = "ExpensePrediction";
            }

            // Apply department suggestion (prefer categorization, fallback to prediction)
            if (categorization?.Department?.TopSuggestion != null)
            {
                var deptSuggestion = categorization.Department.TopSuggestion;
                line.DepartmentSuggested = deptSuggestion.Code;
                line.DepartmentCode = deptSuggestion.Code;
                line.DepartmentTier = deptSuggestion.Tier;
                line.DepartmentSource = deptSuggestion.Source;
                UpdateTierCounts(deptSuggestion.Tier, ref tier1Hits, ref tier2Hits, ref tier3Hits);
            }
            else if (hasPrediction && !string.IsNullOrEmpty(prediction!.SuggestedDepartment))
            {
                // Feature 023: Use prediction's department if no other categorization available
                line.DepartmentSuggested = prediction.SuggestedDepartment;
                line.DepartmentCode = prediction.SuggestedDepartment;
                line.DepartmentSource = "ExpensePrediction";
            }

            missingReceiptCount++;
            lines.Add(line);
        }

        // Set report summary values
        report.Lines = lines;
        report.TotalAmount = lines.Sum(l => l.Amount);
        report.LineCount = lines.Count;
        report.MissingReceiptCount = missingReceiptCount;
        report.Tier1HitCount = tier1Hits;
        report.Tier2HitCount = tier2Hits;
        report.Tier3HitCount = tier3Hits;

        // Save to database
        await _reportRepository.AddAsync(report, ct);

        _logger.LogInformation(
            "Created draft report {ReportId} with {LineCount} lines, {MissingCount} missing receipts",
            report.Id, report.LineCount, report.MissingReceiptCount);

        return MapToDto(report);
    }

    public async Task<ExpenseReportDto?> GetByIdAsync(Guid userId, Guid reportId, CancellationToken ct = default)
    {
        var report = await _reportRepository.GetByIdWithLinesAsync(reportId, ct);

        if (report == null || report.UserId != userId)
        {
            return null;
        }

        return MapToDto(report);
    }

    public async Task<ReportListResponse> GetListAsync(
        Guid userId,
        ReportStatus? status,
        string? period,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var reports = await _reportRepository.GetByUserAsync(userId, status, period, page, pageSize, ct);
        var totalCount = await _reportRepository.GetCountByUserAsync(userId, status, period, ct);

        return new ReportListResponse
        {
            Items = reports.Select(MapToSummaryDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ExpenseLineDto?> UpdateLineAsync(
        Guid userId,
        Guid reportId,
        Guid lineId,
        UpdateLineRequest request,
        CancellationToken ct = default)
    {
        // Verify report ownership
        var report = await _reportRepository.GetByIdAsync(reportId, ct);
        if (report == null || report.UserId != userId)
        {
            return null;
        }

        // Check if report is immutable (Generated or Submitted)
        if (report.Status != ReportStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Cannot modify expense line: report is in {report.Status} status and is locked for editing");
        }

        // Get the line
        var line = await _reportRepository.GetLineByIdAsync(reportId, lineId, ct);
        if (line == null)
        {
            return null;
        }

        var wasEdited = false;

        // Track if GL code was changed from suggestion
        if (request.GlCode != null && request.GlCode != line.GLCodeSuggested)
        {
            line.GLCode = request.GlCode;
            wasEdited = true;
        }
        else if (request.GlCode != null)
        {
            line.GLCode = request.GlCode;
        }

        // Track if department was changed from suggestion
        if (request.DepartmentCode != null && request.DepartmentCode != line.DepartmentSuggested)
        {
            line.DepartmentCode = request.DepartmentCode;
            wasEdited = true;
        }
        else if (request.DepartmentCode != null)
        {
            line.DepartmentCode = request.DepartmentCode;
        }

        // Update missing receipt justification
        if (request.MissingReceiptJustification.HasValue)
        {
            line.MissingReceiptJustification = request.MissingReceiptJustification;
            line.JustificationNote = request.JustificationNote;
        }

        // Mark as user-edited if changes were made that differ from suggestions
        if (wasEdited)
        {
            line.IsUserEdited = true;

            // Trigger learning loop for user corrections
            await TriggerLearningLoopAsync(line, request, ct);
        }

        line.UpdatedAt = DateTime.UtcNow;

        await _reportRepository.UpdateLineAsync(line, ct);

        _logger.LogInformation(
            "Updated expense line {LineId} in report {ReportId}, wasEdited={WasEdited}",
            lineId, reportId, wasEdited);

        return MapLineToDto(line);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid reportId, CancellationToken ct = default)
    {
        var report = await _reportRepository.GetByIdAsync(reportId, ct);

        if (report == null || report.UserId != userId)
        {
            return false;
        }

        await _reportRepository.SoftDeleteAsync(reportId, ct);

        _logger.LogInformation("Soft-deleted report {ReportId} for user {UserId}", reportId, userId);

        return true;
    }

    public async Task<Guid?> GetExistingDraftIdAsync(Guid userId, string period, CancellationToken ct = default)
    {
        var draft = await _reportRepository.GetDraftByUserAndPeriodAsync(userId, period, ct);
        return draft?.Id;
    }

    public async Task<ReportValidationResultDto> ValidateReportAsync(Guid reportId, CancellationToken ct = default)
    {
        var result = new ReportValidationResultDto();
        var report = await _reportRepository.GetByIdWithLinesAsync(reportId, ct);

        if (report == null)
        {
            result.Errors.Add(new ValidationErrorDto
            {
                Field = "ReportId",
                Code = "REPORT_NOT_FOUND",
                Message = "Report not found"
            });
            return result;
        }

        // Check report status
        if (report.Status != ReportStatus.Draft)
        {
            result.Errors.Add(new ValidationErrorDto
            {
                Field = "Status",
                Code = report.Status == ReportStatus.Generated ? "REPORT_ALREADY_GENERATED" : "REPORT_NOT_DRAFT",
                Message = report.Status == ReportStatus.Generated
                    ? "Report has already been finalized"
                    : "Report must be in Draft status to generate"
            });
            return result;
        }

        // Check for at least one line
        if (report.Lines == null || !report.Lines.Any())
        {
            result.Errors.Add(new ValidationErrorDto
            {
                Field = "Lines",
                Code = "REPORT_EMPTY",
                Message = "Report must have at least one expense line"
            });
            return result;
        }

        // Validate each line
        foreach (var line in report.Lines)
        {
            // Check category (GLCode)
            if (string.IsNullOrWhiteSpace(line.GLCode))
            {
                result.Errors.Add(new ValidationErrorDto
                {
                    LineId = line.Id,
                    Field = "GLCode",
                    Code = "LINE_NO_CATEGORY",
                    Message = "Expense line must have a category (GL code) assigned"
                });
            }

            // Check amount > 0
            if (line.Amount <= 0)
            {
                result.Errors.Add(new ValidationErrorDto
                {
                    LineId = line.Id,
                    Field = "Amount",
                    Code = "LINE_INVALID_AMOUNT",
                    Message = "Expense line amount must be greater than zero"
                });
            }

            // Check receipt attached
            if (!line.HasReceipt || line.ReceiptId == null)
            {
                result.Errors.Add(new ValidationErrorDto
                {
                    LineId = line.Id,
                    Field = "ReceiptId",
                    Code = "LINE_NO_RECEIPT",
                    Message = "Expense line must have an attached receipt"
                });
            }
        }

        return result;
    }

    public async Task<GenerateReportResponseDto> GenerateAsync(Guid userId, Guid reportId, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating (finalizing) report {ReportId} for user {UserId}", reportId, userId);

        // Get report with lines
        var report = await _reportRepository.GetByIdWithLinesAsync(reportId, ct);

        if (report == null || report.UserId != userId)
        {
            throw new InvalidOperationException($"Report with ID {reportId} was not found");
        }

        // Validate report
        var validationResult = await ValidateReportAsync(reportId, ct);
        if (!validationResult.IsValid)
        {
            var errorMessages = string.Join("; ", validationResult.Errors.Select(e => e.Message));
            throw new InvalidOperationException($"Report validation failed: {errorMessages}");
        }

        // Update status to Generated
        report.Status = ReportStatus.Generated;
        report.GeneratedAt = DateTimeOffset.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;

        await _reportRepository.UpdateAsync(report, ct);

        _logger.LogInformation(
            "Report {ReportId} finalized with status {Status} at {GeneratedAt}",
            report.Id, report.Status, report.GeneratedAt);

        return new GenerateReportResponseDto
        {
            ReportId = report.Id,
            Status = ReportStatus.Generated.ToString(),
            GeneratedAt = report.GeneratedAt.Value,
            LineCount = report.LineCount,
            TotalAmount = report.TotalAmount
        };
    }

    public async Task<SubmitReportResponseDto> SubmitAsync(Guid userId, Guid reportId, CancellationToken ct = default)
    {
        _logger.LogInformation("Submitting report {ReportId} for user {UserId}", reportId, userId);

        var report = await _reportRepository.GetByIdAsync(reportId, ct);

        if (report == null || report.UserId != userId)
        {
            throw new InvalidOperationException($"Report with ID {reportId} was not found");
        }

        // Check current status
        if (report.Status == ReportStatus.Submitted)
        {
            throw new InvalidOperationException("Report has already been submitted");
        }

        if (report.Status != ReportStatus.Generated)
        {
            throw new InvalidOperationException("Report must be in Generated status before submitting");
        }

        // Update status to Submitted
        report.Status = ReportStatus.Submitted;
        report.SubmittedAt = DateTimeOffset.UtcNow;
        report.UpdatedAt = DateTime.UtcNow;

        await _reportRepository.UpdateAsync(report, ct);

        _logger.LogInformation(
            "Report {ReportId} submitted with status {Status} at {SubmittedAt}",
            report.Id, report.Status, report.SubmittedAt);

        // Feature 023: Learn expense patterns from submitted report
        if (_predictionService != null)
        {
            try
            {
                var patternsLearned = await _predictionService.LearnFromReportAsync(userId, reportId);
                _logger.LogInformation(
                    "Learned {PatternCount} expense patterns from report {ReportId}",
                    patternsLearned, reportId);
            }
            catch (Exception ex)
            {
                // Pattern learning is non-critical - log and continue
                _logger.LogWarning(ex,
                    "Failed to learn expense patterns from report {ReportId}",
                    reportId);
            }
        }

        return new SubmitReportResponseDto
        {
            ReportId = report.Id,
            Status = ReportStatus.Submitted.ToString(),
            GeneratedAt = report.GeneratedAt!.Value,
            SubmittedAt = report.SubmittedAt!.Value
        };
    }

    public async Task<List<ExpenseLineDto>> GetPreviewAsync(Guid userId, string period, CancellationToken ct = default)
    {
        _logger.LogInformation("Getting report preview for user {UserId}, period {Period}", userId, period);

        // Parse period to get date range
        var (startDate, endDate) = ParsePeriod(period);

        // Get confirmed matches for the period
        var confirmedMatches = await _matchRepository.GetConfirmedByPeriodAsync(userId, startDate, endDate);
        _logger.LogDebug("Found {MatchCount} confirmed matches for preview", confirmedMatches.Count);

        var previewLines = new List<ExpenseLineDto>();
        var lineOrder = 1;

        // Create preview lines from confirmed matches
        foreach (var match in confirmedMatches)
        {
            var receipt = match.Receipt;

            // Handle individual transaction matches
            if (match.TransactionId != null && match.Transaction != null)
            {
                var transaction = match.Transaction;

                // Get categorization suggestions (GL code + Department)
                var categorization = await GetCategorizationSafeAsync(transaction.Id, userId, ct);

                previewLines.Add(new ExpenseLineDto
                {
                    Id = Guid.Empty, // No ID yet - this is just a preview
                    ReportId = Guid.Empty,
                    ReceiptId = receipt.Id,
                    TransactionId = transaction.Id,
                    LineOrder = lineOrder++,
                    ExpenseDate = transaction.TransactionDate,
                    Amount = Math.Abs(transaction.Amount), // Use absolute value for display
                    OriginalDescription = transaction.OriginalDescription,
                    NormalizedDescription = transaction.Description,
                    VendorName = receipt.VendorExtracted ?? match.MatchedVendorAlias?.DisplayName,
                    GlCode = categorization?.GL?.TopSuggestion?.Code,
                    GlName = categorization?.GL?.TopSuggestion?.Name, // Vista description
                    GlCodeSuggested = categorization?.GL?.TopSuggestion?.Code,
                    DepartmentCode = categorization?.Department?.TopSuggestion?.Code,
                    DepartmentSuggested = categorization?.Department?.TopSuggestion?.Code,
                    HasReceipt = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            // Handle group matches - expand to individual transactions
            else if (match.TransactionGroupId != null && match.TransactionGroup != null)
            {
                var group = match.TransactionGroup;
                var groupTransactions = group.Transactions.OrderBy(t => t.TransactionDate).ThenBy(t => t.CreatedAt);

                foreach (var transaction in groupTransactions)
                {
                    // Get categorization suggestions for each transaction in group
                    var categorization = await GetCategorizationSafeAsync(transaction.Id, userId, ct);

                    previewLines.Add(new ExpenseLineDto
                    {
                        Id = Guid.Empty,
                        ReportId = Guid.Empty,
                        ReceiptId = receipt.Id,
                        TransactionId = transaction.Id,
                        LineOrder = lineOrder++,
                        ExpenseDate = transaction.TransactionDate,
                        Amount = Math.Abs(transaction.Amount),
                        OriginalDescription = transaction.OriginalDescription,
                        NormalizedDescription = transaction.Description,
                        VendorName = receipt.VendorExtracted ?? match.MatchedVendorAlias?.DisplayName ?? group.Name,
                        GlCode = categorization?.GL?.TopSuggestion?.Code,
                        GlName = categorization?.GL?.TopSuggestion?.Name, // Vista description
                        GlCodeSuggested = categorization?.GL?.TopSuggestion?.Code,
                        DepartmentCode = categorization?.Department?.TopSuggestion?.Code,
                        DepartmentSuggested = categorization?.Department?.TopSuggestion?.Code,
                        HasReceipt = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        _logger.LogInformation(
            "Report preview for period {Period}: {LineCount} lines, total {TotalAmount:C}",
            period, previewLines.Count, previewLines.Sum(l => l.Amount));

        return previewLines;
    }

    #region Private Helpers

    private static (DateOnly StartDate, DateOnly EndDate) ParsePeriod(string period)
    {
        // Period format: YYYY-MM
        var parts = period.Split('-');
        var year = int.Parse(parts[0]);
        var month = int.Parse(parts[1]);

        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return (startDate, endDate);
    }

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
            _logger.LogWarning(ex, "Failed to get categorization for transaction {TransactionId}", transactionId);
            return null;
        }
    }

    private async Task<string> NormalizeDescriptionSafeAsync(
        string rawDescription,
        Guid userId,
        CancellationToken ct)
    {
        try
        {
            var result = await _normalizationService.NormalizeAsync(rawDescription, userId, ct);
            return result.NormalizedDescription;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to normalize description: {Description}", rawDescription);
            return rawDescription; // Fall back to original
        }
    }

    private static void UpdateTierCounts(int tier, ref int tier1, ref int tier2, ref int tier3)
    {
        switch (tier)
        {
            case 1:
                tier1++;
                break;
            case 2:
                tier2++;
                break;
            case 3:
                tier3++;
                break;
        }
    }

    /// <summary>
    /// Triggers learning loop when user corrects categorization.
    /// Updates vendor aliases and description cache with user corrections.
    /// </summary>
    private async Task TriggerLearningLoopAsync(
        ExpenseLine line,
        UpdateLineRequest request,
        CancellationToken ct)
    {
        try
        {
            // Update vendor alias with new default GL code/department if vendor name exists
            if (!string.IsNullOrEmpty(line.VendorName))
            {
                var vendorAlias = await _vendorAliasService.GetByVendorNameAsync(line.VendorName);
                if (vendorAlias != null)
                {
                    var aliasUpdated = false;

                    // Update GL code if user changed it
                    if (request.GlCode != null && request.GlCode != line.GLCodeSuggested)
                    {
                        vendorAlias.DefaultGLCode = request.GlCode;
                        aliasUpdated = true;
                        _logger.LogInformation(
                            "Updating vendor alias {AliasId} default GL code to {GlCode} from user correction",
                            vendorAlias.Id, request.GlCode);
                    }

                    // Update department if user changed it
                    if (request.DepartmentCode != null && request.DepartmentCode != line.DepartmentSuggested)
                    {
                        vendorAlias.DefaultDepartment = request.DepartmentCode;
                        aliasUpdated = true;
                        _logger.LogInformation(
                            "Updating vendor alias {AliasId} default department to {DeptCode} from user correction",
                            vendorAlias.Id, request.DepartmentCode);
                    }

                    if (aliasUpdated)
                    {
                        await _vendorAliasService.UpdateAsync(vendorAlias);
                    }
                }
            }

            // Add description to cache for future normalization if not already cached
            if (!string.IsNullOrEmpty(line.OriginalDescription) &&
                !string.IsNullOrEmpty(line.NormalizedDescription) &&
                line.OriginalDescription != line.NormalizedDescription)
            {
                await _descriptionCacheService.AddOrUpdateAsync(
                    line.OriginalDescription,
                    line.NormalizedDescription);

                _logger.LogInformation(
                    "Added description mapping to cache: '{Original}' -> '{Normalized}'",
                    line.OriginalDescription, line.NormalizedDescription);
            }
        }
        catch (Exception ex)
        {
            // Don't fail the line update if learning loop fails
            _logger.LogWarning(ex, "Failed to trigger learning loop for line {LineId}", line.Id);
        }
    }

    private static ExpenseReportDto MapToDto(ExpenseReport report)
    {
        return new ExpenseReportDto
        {
            Id = report.Id,
            Period = report.Period,
            Status = report.Status,
            TotalAmount = report.TotalAmount,
            LineCount = report.LineCount,
            MissingReceiptCount = report.MissingReceiptCount,
            Tier1HitCount = report.Tier1HitCount,
            Tier2HitCount = report.Tier2HitCount,
            Tier3HitCount = report.Tier3HitCount,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt,
            Lines = report.Lines.Select(MapLineToDto).ToList()
        };
    }

    private static ReportSummaryDto MapToSummaryDto(ExpenseReport report)
    {
        return new ReportSummaryDto
        {
            Id = report.Id,
            Period = report.Period,
            Status = report.Status,
            TotalAmount = report.TotalAmount,
            LineCount = report.LineCount,
            MissingReceiptCount = report.MissingReceiptCount,
            Tier1HitCount = report.Tier1HitCount,
            Tier2HitCount = report.Tier2HitCount,
            Tier3HitCount = report.Tier3HitCount,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }

    private static ExpenseLineDto MapLineToDto(ExpenseLine line)
    {
        return new ExpenseLineDto
        {
            Id = line.Id,
            ReportId = line.ReportId,
            ReceiptId = line.ReceiptId,
            TransactionId = line.TransactionId,
            LineOrder = line.LineOrder,
            ExpenseDate = line.ExpenseDate,
            Amount = line.Amount,
            OriginalDescription = line.OriginalDescription,
            NormalizedDescription = line.NormalizedDescription,
            VendorName = line.VendorName,
            GlCode = line.GLCode,
            GlCodeSuggested = line.GLCodeSuggested,
            GlCodeTier = line.GLCodeTier,
            GlCodeSource = line.GLCodeSource,
            DepartmentCode = line.DepartmentCode,
            DepartmentSuggested = line.DepartmentSuggested,
            DepartmentTier = line.DepartmentTier,
            DepartmentSource = line.DepartmentSource,
            HasReceipt = line.HasReceipt,
            MissingReceiptJustification = line.MissingReceiptJustification,
            JustificationNote = line.JustificationNote,
            IsUserEdited = line.IsUserEdited,
            CreatedAt = line.CreatedAt,
            UpdatedAt = line.UpdatedAt,
            // Feature 023: Auto-suggestion tracking
            IsAutoSuggested = line.IsAutoSuggested,
            PredictionId = line.PredictionId
        };
    }

    #endregion
}
