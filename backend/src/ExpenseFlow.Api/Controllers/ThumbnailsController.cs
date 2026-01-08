using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Administrative endpoints for thumbnail management including backfill operations.
/// </summary>
[Route("api/admin/thumbnails")]
[Authorize(Roles = "Admin")]
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
}
