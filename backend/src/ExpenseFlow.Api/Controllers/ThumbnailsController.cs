using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Administrative endpoints for thumbnail management including backfill operations.
/// In production, requires Admin role. In staging, allows any authenticated user.
/// </summary>
[Route("api/admin/thumbnails")]
[Authorize] // Role check is done in methods for environment-specific behavior
public class ThumbnailsController : ApiControllerBase
{
    private readonly IThumbnailBackfillService _backfillService;
    private readonly ILogger<ThumbnailsController> _logger;

    public ThumbnailsController(
        IThumbnailBackfillService backfillService,
        ILogger<ThumbnailsController> logger)
    {
        _backfillService = backfillService;
        _logger = logger;
    }

    /// <summary>
    /// Starts a thumbnail backfill job for receipts missing thumbnails.
    /// </summary>
    /// <param name="request">Backfill configuration</param>
    /// <returns>Job ID and estimated count</returns>
    /// <response code="202">Backfill job started</response>
    /// <response code="409">Backfill job already running</response>
    [HttpPost("backfill")]
    [ProducesResponseType(typeof(ThumbnailBackfillResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartBackfill([FromBody] ThumbnailBackfillRequest? request)
    {
        try
        {
            var response = await _backfillService.StartBackfillAsync(request ?? new ThumbnailBackfillRequest());

            if (string.IsNullOrEmpty(response.JobId))
            {
                // No receipts to process
                return Ok(response);
            }

            _logger.LogInformation(
                "Admin started thumbnail backfill job {JobId} for {EstimatedCount} receipts",
                response.JobId, response.EstimatedCount);

            return AcceptedAtAction(nameof(GetBackfillStatus), response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already running"))
        {
            _logger.LogWarning("Attempted to start backfill while job already running");
            return Conflict(new ProblemDetails
            {
                Title = "Backfill Already Running",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    /// <summary>
    /// Gets the current status of the thumbnail backfill job.
    /// </summary>
    /// <returns>Current status with progress information</returns>
    /// <response code="200">Status retrieved</response>
    [HttpGet("backfill/status")]
    [ProducesResponseType(typeof(ThumbnailBackfillStatus), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBackfillStatus()
    {
        var status = await _backfillService.GetStatusAsync();
        return Ok(status);
    }

    /// <summary>
    /// Internal endpoint to trigger thumbnail regeneration (staging only, no auth required).
    /// Access via: POST /api/admin/thumbnails/internal/regenerate
    /// </summary>
    [HttpPost("internal/regenerate")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)] // Hide from Swagger
    public async Task<IActionResult> InternalRegenerate([FromBody] ThumbnailBackfillRequest? request)
    {
        // Only allow in non-production environments
        var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (env.IsProduction())
        {
            return NotFound(); // Pretend endpoint doesn't exist in production
        }

        _logger.LogWarning("Internal regenerate endpoint called - bypassing auth for staging");

        var req = request ?? new ThumbnailBackfillRequest();
        // Force regeneration when using internal endpoint
        req = req with { ForceRegenerate = true };

        try
        {
            var response = await _backfillService.StartBackfillAsync(req);
            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already running"))
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Internal endpoint to check backfill status (staging only, no auth required).
    /// </summary>
    [HttpGet("internal/status")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> InternalStatus()
    {
        var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
        if (env.IsProduction())
        {
            return NotFound();
        }

        var status = await _backfillService.GetStatusAsync();
        return Ok(status);
    }
}
