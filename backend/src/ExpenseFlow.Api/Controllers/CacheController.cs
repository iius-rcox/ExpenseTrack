using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for cache management and statistics (admin only).
/// </summary>
[Authorize(Policy = "AdminOnly")]
public class CacheController : ApiControllerBase
{
    private readonly ICacheStatsService _cacheStatsService;
    private readonly ILogger<CacheController> _logger;

    public CacheController(ICacheStatsService cacheStatsService, ILogger<CacheController> logger)
    {
        _cacheStatsService = cacheStatsService;
        _logger = logger;
    }

    /// <summary>
    /// Gets statistics for all cache tables.
    /// </summary>
    /// <returns>Cache statistics for all tables.</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CacheStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CacheStatsResponse>> GetCacheStats()
    {
        _logger.LogInformation("Cache stats requested by user {UserId}", CurrentUserObjectId);
        var stats = await _cacheStatsService.GetAllStatsAsync();
        return Ok(stats);
    }
}
