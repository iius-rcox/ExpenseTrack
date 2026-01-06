using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for managing asynchronous report generation jobs.
/// Supports creating jobs, polling status, listing history, and cancellation.
/// </summary>
[Authorize]
public class ReportJobsController : ApiControllerBase
{
    private readonly IReportJobService _reportJobService;
    private readonly IUserService _userService;
    private readonly ILogger<ReportJobsController> _logger;

    public ReportJobsController(
        IReportJobService reportJobService,
        IUserService userService,
        ILogger<ReportJobsController> logger)
    {
        _reportJobService = reportJobService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new report generation job for the specified period.
    /// Returns 202 Accepted with job details and Location header for polling.
    /// </summary>
    /// <param name="request">Request containing the period in YYYY-MM format</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created job details with polling URL</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ReportJobDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReportJobDto>> Create(
        [FromBody] CreateReportJobRequest request,
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
            "Creating report generation job for user {UserId}, period {Period}",
            user.Id, request.Period);

        try
        {
            var job = await _reportJobService.CreateJobAsync(user.Id, request.Period, ct);

            var dto = MapToDto(job);

            _logger.LogInformation(
                "Created report job {JobId} for user {UserId}, period {Period}",
                job.Id, user.Id, request.Period);

            // Return 202 Accepted with Location header for polling
            return AcceptedAtAction(
                nameof(GetById),
                new { id = job.Id },
                dto);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogWarning(
                "Duplicate job request for user {UserId}, period {Period}: {Message}",
                user.Id, request.Period, ex.Message);

            return Conflict(new ProblemDetailsResponse
            {
                Title = "Conflict",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets the current status and progress of a report generation job.
    /// Use for polling until job reaches a terminal state (Completed, Failed, Cancelled).
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Job status and progress details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReportJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportJobDto>> GetById(Guid id, CancellationToken ct)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var job = await _reportJobService.GetByIdAsync(user.Id, id, ct);

        if (job == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = $"Report job with ID {id} was not found"
            });
        }

        return Ok(MapToDto(job));
    }

    /// <summary>
    /// Gets a paginated list of report generation jobs for the current user.
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (max 100, default: 20)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of jobs</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ReportJobListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ReportJobListResponse>> GetList(
        [FromQuery] ReportJobStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var user = await _userService.GetOrCreateUserAsync(User);

        var (jobs, totalCount) = await _reportJobService.GetListAsync(
            user.Id, status, page, pageSize, ct);

        var response = new ReportJobListResponse
        {
            Items = jobs.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Requests cancellation of an active report generation job.
    /// The job will be marked for cancellation and stop at the next checkpoint.
    /// </summary>
    /// <param name="id">Job ID to cancel</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated job with CancellationRequested status</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ReportJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReportJobDto>> Cancel(Guid id, CancellationToken ct)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Cancellation requested for job {JobId} by user {UserId}",
            id, user.Id);

        try
        {
            var job = await _reportJobService.CancelAsync(user.Id, id, ct);

            if (job == null)
            {
                return NotFound(new ProblemDetailsResponse
                {
                    Title = "Not Found",
                    Detail = $"Report job with ID {id} was not found"
                });
            }

            return Ok(MapToDto(job));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Cannot cancel"))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid Operation",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Checks if an active job exists for the specified period.
    /// Useful for preventing duplicate job creation from the frontend.
    /// </summary>
    /// <param name="period">Period in YYYY-MM format</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Active job if exists, otherwise empty response</returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(ActiveJobResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ActiveJobResponse>> GetActiveJob(
        [FromQuery] string period,
        CancellationToken ct)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var job = await _reportJobService.GetActiveJobAsync(user.Id, period, ct);

        return Ok(new ActiveJobResponse
        {
            HasActiveJob = job != null,
            Job = job != null ? MapToDto(job) : null
        });
    }

    private static ReportJobDto MapToDto(ReportGenerationJob job) => new()
    {
        Id = job.Id,
        Period = job.Period,
        Status = job.Status.ToString(),
        StatusMessage = GetStatusMessage(job),
        TotalLines = job.TotalLines,
        ProcessedLines = job.ProcessedLines,
        FailedLines = job.FailedLines,
        ErrorMessage = job.ErrorMessage,
        EstimatedCompletionAt = job.EstimatedCompletionAt,
        StartedAt = job.StartedAt,
        CompletedAt = job.CompletedAt,
        CreatedAt = job.CreatedAt,
        GeneratedReportId = job.GeneratedReportId
    };

    private static string GetStatusMessage(ReportGenerationJob job) => job.Status switch
    {
        ReportJobStatus.Pending => "Waiting to start...",
        ReportJobStatus.Processing when job.TotalLines > 0 =>
            $"Processing {job.ProcessedLines:N0} of {job.TotalLines:N0} lines...",
        ReportJobStatus.Processing => "Gathering transactions...",
        ReportJobStatus.Completed when job.FailedLines > 0 =>
            $"Completed with {job.FailedLines:N0} lines requiring review",
        ReportJobStatus.Completed => "Report generated successfully",
        ReportJobStatus.Failed => job.ErrorMessage ?? "Report generation failed",
        ReportJobStatus.Cancelled => "Cancelled by user",
        ReportJobStatus.CancellationRequested => "Cancelling...",
        _ => job.Status.ToString()
    };
}

/// <summary>
/// Response for checking if an active job exists.
/// </summary>
public class ActiveJobResponse
{
    /// <summary>
    /// True if an active job exists for the specified period.
    /// </summary>
    public bool HasActiveJob { get; set; }

    /// <summary>
    /// The active job details, if any.
    /// </summary>
    public ReportJobDto? Job { get; set; }
}
