using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for reference data (GL accounts, departments, projects).
/// </summary>
[Authorize]
public class ReferenceController : ApiControllerBase
{
    private readonly IReferenceDataService _referenceDataService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ILogger<ReferenceController> _logger;

    public ReferenceController(
        IReferenceDataService referenceDataService,
        IBackgroundJobService backgroundJobService,
        ILogger<ReferenceController> logger)
    {
        _referenceDataService = referenceDataService;
        _backgroundJobService = backgroundJobService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all GL accounts.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active accounts.</param>
    /// <returns>List of GL accounts.</returns>
    [HttpGet("gl-accounts")]
    [ProducesResponseType(typeof(IEnumerable<GLAccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<GLAccountResponse>>> GetGLAccounts([FromQuery] bool activeOnly = true)
    {
        var accounts = await _referenceDataService.GetGLAccountsAsync(activeOnly);
        var response = accounts.Select(a => new GLAccountResponse
        {
            Id = a.Id,
            Code = a.Code,
            Name = a.Name,
            Description = a.Description,
            IsActive = a.IsActive
        });
        return Ok(response);
    }

    /// <summary>
    /// Gets all departments.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active departments.</param>
    /// <returns>List of departments.</returns>
    [HttpGet("departments")]
    [ProducesResponseType(typeof(IEnumerable<DepartmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<DepartmentResponse>>> GetDepartments([FromQuery] bool activeOnly = true)
    {
        var departments = await _referenceDataService.GetDepartmentsAsync(activeOnly);
        var response = departments.Select(d => new DepartmentResponse
        {
            Id = d.Id,
            Code = d.Code,
            Name = d.Name,
            Description = d.Description,
            IsActive = d.IsActive
        });
        return Ok(response);
    }

    /// <summary>
    /// Gets all projects.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active projects.</param>
    /// <returns>List of projects.</returns>
    [HttpGet("projects")]
    [ProducesResponseType(typeof(IEnumerable<ProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<ProjectResponse>>> GetProjects([FromQuery] bool activeOnly = true)
    {
        var projects = await _referenceDataService.GetProjectsAsync(activeOnly);
        var response = projects.Select(p => new ProjectResponse
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Description = p.Description,
            IsActive = p.IsActive
        });
        return Ok(response);
    }

    /// <summary>
    /// Triggers a reference data sync (admin only).
    /// </summary>
    /// <returns>Job enqueued response.</returns>
    [HttpPost("sync")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(JobEnqueuedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status403Forbidden)]
    public ActionResult<JobEnqueuedResponse> TriggerSync()
    {
        _logger.LogInformation("Reference data sync triggered by user {UserId}", CurrentUserObjectId);

        var jobId = _backgroundJobService.EnqueueReferenceDataSync();

        var response = new JobEnqueuedResponse
        {
            JobId = jobId,
            Status = "enqueued",
            EnqueuedAt = DateTime.UtcNow
        };

        return Accepted(response);
    }
}
