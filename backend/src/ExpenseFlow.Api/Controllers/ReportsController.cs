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
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IReportService reportService,
        IUserService userService,
        ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _userService = userService;
        _logger = logger;
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
