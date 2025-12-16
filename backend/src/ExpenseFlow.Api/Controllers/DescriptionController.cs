using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for transaction description normalization.
/// </summary>
[Authorize]
public class DescriptionController : ApiControllerBase
{
    private readonly IDescriptionNormalizationService _normalizationService;
    private readonly IUserService _userService;
    private readonly ILogger<DescriptionController> _logger;

    public DescriptionController(
        IDescriptionNormalizationService normalizationService,
        IUserService userService,
        ILogger<DescriptionController> logger)
    {
        _normalizationService = normalizationService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Normalizes a raw bank/card transaction description to human-readable format.
    /// Uses a tiered approach: Tier 1 (cache) for instant responses, Tier 3 (AI) for uncached descriptions.
    /// </summary>
    /// <param name="request">Normalization request containing raw description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Normalization result with tier information.</returns>
    /// <response code="200">Description normalized successfully.</response>
    /// <response code="400">Invalid request - empty or null description.</response>
    /// <response code="503">AI service temporarily unavailable.</response>
    [HttpPost("normalize")]
    [ProducesResponseType(typeof(NormalizationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<NormalizationResultDto>> Normalize(
        [FromBody] NormalizationRequest request,
        CancellationToken cancellationToken)
    {
        // T032: Request validation for empty/invalid descriptions
        if (request == null || string.IsNullOrWhiteSpace(request.RawDescription))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "Raw description is required and cannot be empty"
            });
        }

        if (request.RawDescription.Length > 500)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Description too long",
                Detail = "Raw description cannot exceed 500 characters"
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var result = await _normalizationService.NormalizeAsync(
                request.RawDescription,
                user.Id,
                cancellationToken);

            // T033: Error handling for AI service unavailability
            if (result.Tier == 0 && result.Confidence == 0)
            {
                _logger.LogWarning("AI service unavailable for description normalization");

                Response.Headers.Append("Retry-After", "30");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetailsResponse
                {
                    Title = "AI service unavailable",
                    Detail = "The AI normalization service is temporarily unavailable. Original description returned. Please retry after 30 seconds."
                });
            }

            _logger.LogDebug(
                "Normalized description for user {UserId}: Tier {Tier}, CacheHit {CacheHit}",
                user.Id, result.Tier, result.CacheHit);

            return Ok(result);
        }
        catch (OperationCanceledException)
        {
            throw; // Let framework handle cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing description for user {UserId}", user.Id);

            Response.Headers.Append("Retry-After", "30");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetailsResponse
            {
                Title = "Normalization failed",
                Detail = "An error occurred during description normalization. Please retry."
            });
        }
    }

    /// <summary>
    /// Gets cache statistics for description normalization.
    /// </summary>
    /// <returns>Cache statistics including total entries and hit count.</returns>
    [HttpGet("cache-stats")]
    [ProducesResponseType(typeof(DescriptionCacheStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DescriptionCacheStatsDto>> GetCacheStats()
    {
        var (totalEntries, totalHits) = await _normalizationService.GetCacheStatsAsync();

        return Ok(new DescriptionCacheStatsDto
        {
            TotalEntries = totalEntries,
            TotalHits = totalHits,
            HitRate = totalEntries > 0 ? (decimal)totalHits / totalEntries * 100 : 0
        });
    }
}

/// <summary>
/// Response DTO for description cache statistics.
/// </summary>
public class DescriptionCacheStatsDto
{
    /// <summary>
    /// Total number of cached descriptions.
    /// </summary>
    public int TotalEntries { get; set; }

    /// <summary>
    /// Total number of cache hits.
    /// </summary>
    public int TotalHits { get; set; }

    /// <summary>
    /// Cache hit rate percentage.
    /// </summary>
    public decimal HitRate { get; set; }
}
