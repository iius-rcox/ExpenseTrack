using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for expense report operations including draft generation and editing.
/// </summary>
[Authorize]
public class ReportsController : ApiControllerBase
{
    private readonly IReportService _reportService;
    private readonly IUserService _userService;
    private readonly IExcelExportService _excelExportService;
    private readonly IPdfGenerationService _pdfGenerationService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportService reportService,
        IUserService userService,
        IExcelExportService excelExportService,
        IPdfGenerationService pdfGenerationService,
        ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _userService = userService;
        _excelExportService = excelExportService;
        _pdfGenerationService = pdfGenerationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a preview of expense lines that would be included in a report for a given period.
    /// Does not create a report, only returns what would be included.
    /// </summary>
    /// <param name="period">Period in YYYY-MM format</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of expense lines that would be in the report</returns>
    [HttpGet("preview")]
    [ProducesResponseType(typeof(List<ExpenseLineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<ExpenseLineDto>>> GetPreview(
        [FromQuery] string period,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(period) || !System.Text.RegularExpressions.Regex.IsMatch(period, @"^\d{4}-\d{2}$"))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = "Period must be in YYYY-MM format"
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Getting report preview for user {UserId}, period {Period}",
            user.Id, period);

        var preview = await _reportService.GetPreviewAsync(user.Id, period, ct);

        return Ok(preview);
    }

    /// <summary>
    /// Generates a draft expense report for a specific period.
    /// Alias for POST /reports/draft for frontend compatibility.
    /// </summary>
    /// <param name="request">Request containing the period (YYYY-MM format)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Generated draft report with all lines</returns>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(ExpenseReportDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ExpenseReportDto>> Generate(
        [FromBody] GenerateDraftRequest request,
        CancellationToken ct)
    {
        // This is an alias for GenerateDraft for frontend compatibility
        return await GenerateDraft(request, ct);
    }

    /// <summary>
    /// Generates a draft expense report for a specific period.
    /// </summary>
    /// <param name="request">Request containing the period (YYYY-MM format)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Generated draft report with all lines</returns>
    [HttpPost("draft")]
    [ProducesResponseType(typeof(ExpenseReportDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ExpenseReportDto>> GenerateDraft(
        [FromBody] GenerateDraftRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = "Invalid request. Period must be in YYYY-MM format."
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Generating draft report for user {UserId}, period {Period}",
            user.Id, request.Period);

        var report = await _reportService.GenerateDraftAsync(user.Id, request.Period, ct);

        return CreatedAtAction(nameof(GetById), new { reportId = report.Id }, report);
    }

    /// <summary>
    /// Check if a draft report already exists for the user and period.
    /// </summary>
    /// <param name="period">Period in YYYY-MM format</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Existing draft ID if found</returns>
    [HttpGet("draft/exists")]
    [ProducesResponseType(typeof(ExistingDraftResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ExistingDraftResponse>> CheckExistingDraft(
        [FromQuery] string period,
        CancellationToken ct)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var existingId = await _reportService.GetExistingDraftIdAsync(user.Id, period, ct);

        return Ok(new ExistingDraftResponse
        {
            Exists = existingId.HasValue,
            ReportId = existingId
        });
    }

    /// <summary>
    /// Gets a report by ID with all expense lines.
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Report with lines if found</returns>
    [HttpGet("{reportId:guid}")]
    [ProducesResponseType(typeof(ExpenseReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ExpenseReportDto>> GetById(Guid reportId, CancellationToken ct)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var report = await _reportService.GetByIdAsync(user.Id, reportId, ct);

        if (report == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = $"Report with ID {reportId} was not found"
            });
        }

        return Ok(report);
    }

    /// <summary>
    /// Gets paginated list of reports for the current user.
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="period">Optional period filter (YYYY-MM format)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page (max 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of report summaries</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ReportListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ReportListResponse>> GetList(
        [FromQuery] ReportStatus? status,
        [FromQuery] string? period,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var user = await _userService.GetOrCreateUserAsync(User);
        var response = await _reportService.GetListAsync(user.Id, status, period, page, pageSize, ct);

        return Ok(response);
    }

    /// <summary>
    /// Updates an expense line within a report.
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="lineId">Line ID to update</param>
    /// <param name="request">Update request with changed fields</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated expense line</returns>
    [HttpPatch("{reportId:guid}/lines/{lineId:guid}")]
    [ProducesResponseType(typeof(ExpenseLineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ExpenseLineDto>> UpdateLine(
        Guid reportId,
        Guid lineId,
        [FromBody] UpdateLineRequest request,
        CancellationToken ct)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var updatedLine = await _reportService.UpdateLineAsync(user.Id, reportId, lineId, request, ct);

        if (updatedLine == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = $"Report with ID {reportId} or line with ID {lineId} was not found"
            });
        }

        return Ok(updatedLine);
    }

    /// <summary>
    /// Deletes a report (soft delete).
    /// </summary>
    /// <param name="reportId">Report ID to delete</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{reportId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid reportId, CancellationToken ct)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var deleted = await _reportService.DeleteAsync(user.Id, reportId, ct);

        if (!deleted)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = $"Report with ID {reportId} was not found"
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Exports a report to Excel format matching the AP department template.
    /// </summary>
    /// <param name="reportId">Report ID to export</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Excel file as downloadable attachment</returns>
    [HttpGet("{reportId:guid}/export/excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportExcel(Guid reportId, CancellationToken ct)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        // Verify user owns the report
        var report = await _reportService.GetByIdAsync(user.Id, reportId, ct);
        if (report == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = $"Report with ID {reportId} was not found"
            });
        }

        _logger.LogInformation(
            "Exporting report {ReportId} to Excel for user {UserId}",
            reportId, user.Id);

        try
        {
            var excelBytes = await _excelExportService.GenerateExcelAsync(reportId, ct);

            var fileName = $"ExpenseReport_{report.Period}_{DateTime.UtcNow:yyyyMMdd}.xlsx";

            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("template"))
        {
            _logger.LogError(ex, "Excel template not found for export");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetailsResponse
            {
                Title = "Service Unavailable",
                Detail = "Excel template is not configured. Please contact administrator."
            });
        }
    }

    /// <summary>
    /// Exports edited expense lines to Excel without database persistence.
    /// Stateless export for the lightweight editable report workflow.
    /// </summary>
    /// <param name="request">Export request with period and edited lines</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Excel file as downloadable attachment</returns>
    [HttpPost("export/preview/excel")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportPreviewExcel(
        [FromBody] ExportPreviewRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = "Invalid export request. Check period format and line data."
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Exporting preview to Excel for user {UserId}, period {Period}, {LineCount} lines",
            user.Id, request.Period, request.Lines.Count);

        try
        {
            var excelBytes = await _excelExportService.GenerateExcelFromPreviewAsync(
                request, user.DisplayName, ct);

            var fileName = $"ExpenseReport_{request.Period}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excel export from preview failed for period {Period}", request.Period);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetailsResponse
            {
                Title = "Export Failed",
                Detail = "An error occurred while generating the Excel file. Please try again."
            });
        }
    }

    /// <summary>
    /// Exports edited expense lines to PDF without database persistence.
    /// Generates summary table only (no receipt images).
    /// </summary>
    /// <param name="request">Export request with period and edited lines</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>PDF file as downloadable attachment</returns>
    [HttpPost("export/preview/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportPreviewPdf(
        [FromBody] ExportPreviewRequest request,
        CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = "Invalid export request. Check period format and line data."
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Exporting preview to PDF for user {UserId}, period {Period}, {LineCount} lines",
            user.Id, request.Period, request.Lines.Count);

        try
        {
            var pdfBytes = await _pdfGenerationService.GenerateSummaryPdfAsync(
                request, user.DisplayName, ct);

            var fileName = $"ExpenseReport_{request.Period}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

            return File(
                pdfBytes,
                "application/pdf",
                fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF export from preview failed for period {Period}", request.Period);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetailsResponse
            {
                Title = "Export Failed",
                Detail = "An error occurred while generating the PDF file. Please try again."
            });
        }
    }

    /// <summary>
    /// Finalizes a draft report, changing status to Generated.
    /// Report must pass validation: each line needs category, amount > 0, and receipt.
    /// </summary>
    /// <param name="reportId">Report ID to finalize</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Generate response with status and timestamp</returns>
    [HttpPost("{reportId:guid}/generate")]
    [ProducesResponseType(typeof(GenerateReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GenerateReportResponseDto>> Generate(
        Guid reportId,
        CancellationToken ct)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Generating (finalizing) report {ReportId} for user {UserId}",
            reportId, user.Id);

        try
        {
            var result = await _reportService.GenerateAsync(user.Id, reportId, ct);

            _logger.LogInformation(
                "Report {ReportId} successfully finalized at {GeneratedAt}",
                reportId, result.GeneratedAt);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already been finalized"))
        {
            return Conflict(new ProblemDetailsResponse
            {
                Title = "Conflict",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Submits a generated report for tracking/audit purposes.
    /// Report must be in Generated status.
    /// </summary>
    /// <param name="reportId">Report ID to submit</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Submit response with status and timestamp</returns>
    [HttpPost("{reportId:guid}/submit")]
    [ProducesResponseType(typeof(SubmitReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SubmitReportResponseDto>> Submit(
        Guid reportId,
        CancellationToken ct)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Submitting report {ReportId} for user {UserId}",
            reportId, user.Id);

        try
        {
            var result = await _reportService.SubmitAsync(user.Id, reportId, ct);

            _logger.LogInformation(
                "Report {ReportId} successfully submitted at {SubmittedAt}",
                reportId, result.SubmittedAt);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already been submitted"))
        {
            return Conflict(new ProblemDetailsResponse
            {
                Title = "Conflict",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Exports a complete PDF report containing both the itemized expense list
    /// and all associated receipt images in a single document.
    /// </summary>
    /// <param name="reportId">Report ID to export</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>PDF file as downloadable attachment</returns>
    [HttpGet("{reportId:guid}/export/complete")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportCompletePdf(Guid reportId, CancellationToken ct)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        // Verify user owns the report
        var report = await _reportService.GetByIdAsync(user.Id, reportId, ct);
        if (report == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = $"Report with ID {reportId} was not found"
            });
        }

        _logger.LogInformation(
            "Exporting complete PDF (itemized + receipts) for report {ReportId} for user {UserId}",
            reportId, user.Id);

        try
        {
            var pdfResult = await _pdfGenerationService.GenerateCompleteReportPdfAsync(reportId, ct);

            // Add custom headers with metadata
            Response.Headers.Append("X-Page-Count", pdfResult.PageCount.ToString());
            Response.Headers.Append("X-Placeholder-Count", pdfResult.PlaceholderCount.ToString());

            return File(
                pdfResult.FileContents,
                pdfResult.ContentType,
                pdfResult.FileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to generate complete PDF for report {ReportId}", reportId);
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Complete PDF generation failed for report {ReportId}: {Error}", reportId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetailsResponse
            {
                Title = "PDF Generation Failed",
                Detail = "An error occurred while generating the complete PDF report. Please try again or contact support."
            });
        }
    }

    /// <summary>
    /// Adds a transaction as a new expense line to a report.
    /// Enforces transaction exclusivity - a transaction can only be on one report at a time.
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="request">Request containing transaction ID and optional categorization</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created expense line</returns>
    [HttpPost("{reportId:guid}/lines")]
    [ProducesResponseType(typeof(ExpenseLineDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExpenseLineDto>> AddLine(
        Guid reportId,
        [FromBody] AddLineRequest request,
        CancellationToken ct)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Adding transaction {TransactionId} to report {ReportId} for user {UserId}",
            request.TransactionId, reportId, user.Id);

        try
        {
            var line = await _reportService.AddLineAsync(user.Id, reportId, request, ct);

            return CreatedAtAction(
                nameof(UpdateLine),
                new { reportId, lineId = line.Id },
                line);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already assigned"))
        {
            return Conflict(new ProblemDetailsResponse
            {
                Title = "Conflict",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("locked for editing"))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Removes an expense line from a report.
    /// If the line is a split parent, all child allocations are removed (cascade delete).
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="lineId">Line ID to remove</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{reportId:guid}/lines/{lineId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveLine(
        Guid reportId,
        Guid lineId,
        CancellationToken ct)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Removing line {LineId} from report {ReportId} for user {UserId}",
            lineId, reportId, user.Id);

        try
        {
            var removed = await _reportService.RemoveLineAsync(user.Id, reportId, lineId, ct);

            if (!removed)
            {
                return NotFound(new ProblemDetailsResponse
                {
                    Title = "Not Found",
                    Detail = $"Line with ID {lineId} was not found in report {reportId}"
                });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("locked for editing"))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets transactions available to add to a report.
    /// Excludes transactions already on any active report.
    /// </summary>
    /// <param name="reportId">Report ID (to determine period)</param>
    /// <param name="search">Optional search term</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page (max 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Available transactions with period awareness</returns>
    [HttpGet("{reportId:guid}/available-transactions")]
    [ProducesResponseType(typeof(AvailableTransactionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AvailableTransactionsResponse>> GetAvailableTransactions(
        Guid reportId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var response = await _reportService.GetAvailableTransactionsAsync(
                user.Id, reportId, search, page, pageSize, ct);

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Saves all pending changes to a draft report.
    /// Batch updates multiple expense lines while keeping the report in Draft status.
    /// CRITICAL: This does NOT finalize the report - it remains editable after save.
    /// </summary>
    /// <param name="reportId">Report ID to save</param>
    /// <param name="request">Batch update request with line changes</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response with update counts and any failures</returns>
    [HttpPost("{reportId:guid}/save")]
    [ProducesResponseType(typeof(BatchUpdateLinesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BatchUpdateLinesResponse>> SaveReport(
        Guid reportId,
        [FromBody] BatchUpdateLinesRequest request,
        CancellationToken ct)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Saving report {ReportId} with {LineCount} line updates for user {UserId}",
            reportId, request.Lines.Count, user.Id);

        try
        {
            var response = await _reportService.BatchUpdateLinesAsync(user.Id, reportId, request, ct);

            _logger.LogInformation(
                "Report {ReportId} saved: {UpdatedCount} updated, {FailedCount} failed. Status: {Status}",
                reportId, response.UpdatedCount, response.FailedCount, response.ReportStatus);

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("locked for editing"))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Exports a consolidated PDF of all receipts for a report.
    /// Includes placeholder pages for missing receipts with justification details.
    /// </summary>
    /// <param name="reportId">Report ID to export</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>PDF file as downloadable attachment</returns>
    [HttpGet("{reportId:guid}/export/receipts")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/pdf")]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportReceiptsPdf(Guid reportId, CancellationToken ct)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        // Verify user owns the report
        var report = await _reportService.GetByIdAsync(user.Id, reportId, ct);
        if (report == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = $"Report with ID {reportId} was not found"
            });
        }

        _logger.LogInformation(
            "Exporting receipts PDF for report {ReportId} for user {UserId}",
            reportId, user.Id);

        try
        {
            var pdfResult = await _pdfGenerationService.GenerateReceiptPdfAsync(reportId, ct);

            // Add custom headers with metadata
            Response.Headers.Append("X-Page-Count", pdfResult.PageCount.ToString());
            Response.Headers.Append("X-Placeholder-Count", pdfResult.PlaceholderCount.ToString());

            return File(
                pdfResult.FileContents,
                pdfResult.ContentType,
                pdfResult.FileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Failed to generate receipts PDF for report {ReportId}", reportId);
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            // BUG-008 fix: Handle all exceptions during PDF generation
            // This catches font-related errors, image processing errors, and other PDF generation issues
            _logger.LogError(ex, "PDF generation failed for report {ReportId}: {Error}", reportId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetailsResponse
            {
                Title = "PDF Generation Failed",
                Detail = "An error occurred while generating the receipt PDF. Please try again or contact support."
            });
        }
    }
}

/// <summary>
/// Response for checking if a draft exists.
/// </summary>
public class ExistingDraftResponse
{
    /// <summary>
    /// True if a draft exists for the specified period.
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// ID of the existing draft, if any.
    /// </summary>
    public Guid? ReportId { get; set; }
}
