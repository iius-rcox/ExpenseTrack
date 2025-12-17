using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for expense analytics including MoM comparison and cache statistics.
/// </summary>
[Authorize]
public class AnalyticsController : ApiControllerBase
{
    private readonly IComparisonService _comparisonService;
    private readonly ICacheStatisticsService _cacheStatisticsService;
    private readonly IUserService _userService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IComparisonService comparisonService,
        ICacheStatisticsService cacheStatisticsService,
        IUserService userService,
        ILogger<AnalyticsController> logger)
    {
        _comparisonService = comparisonService;
        _cacheStatisticsService = cacheStatisticsService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Gets month-over-month spending comparison between two periods.
    /// Identifies new vendors, missing recurring charges, and significant changes.
    /// </summary>
    /// <param name="currentPeriod">Current period in YYYY-MM format (e.g., "2025-01")</param>
    /// <param name="previousPeriod">Previous period in YYYY-MM format (defaults to month before current)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Comparison results with summary metrics and anomaly detection</returns>
    [HttpGet("comparison")]
    [ProducesResponseType(typeof(MonthlyComparisonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MonthlyComparisonDto>> GetComparison(
        [FromQuery] string currentPeriod,
        [FromQuery] string? previousPeriod,
        CancellationToken ct)
    {
        // Validate period format
        if (string.IsNullOrWhiteSpace(currentPeriod) || !IsValidPeriod(currentPeriod))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = "Invalid currentPeriod format. Expected YYYY-MM (e.g., 2025-01)."
            });
        }

        // Default previous period to month before current
        if (string.IsNullOrWhiteSpace(previousPeriod))
        {
            previousPeriod = GetPreviousPeriod(currentPeriod);
        }
        else if (!IsValidPeriod(previousPeriod))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = "Invalid previousPeriod format. Expected YYYY-MM (e.g., 2025-01)."
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Generating MoM comparison for user {UserId}: {CurrentPeriod} vs {PreviousPeriod}",
            user.Id, currentPeriod, previousPeriod);

        try
        {
            var comparison = await _comparisonService.GetComparisonAsync(
                user.Id,
                currentPeriod,
                previousPeriod,
                ct);

            return Ok(comparison);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid period format in comparison request");
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets cache tier usage statistics for the specified period.
    /// Includes hit rates, costs, and breakdown by operation type.
    /// </summary>
    /// <param name="period">Period in YYYY-MM format (e.g., "2025-01")</param>
    /// <param name="groupBy">Optional grouping: "tier", "operation", or "day" (default: "tier")</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Cache statistics with hit rates and cost analysis</returns>
    [HttpGet("cache-stats")]
    [ProducesResponseType(typeof(CacheStatisticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CacheStatisticsResponse>> GetCacheStatistics(
        [FromQuery] string period,
        [FromQuery] string? groupBy,
        CancellationToken ct)
    {
        // Validate period format
        if (string.IsNullOrWhiteSpace(period) || !IsValidPeriod(period))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = "Invalid period format. Expected YYYY-MM (e.g., 2025-01)."
            });
        }

        // Validate groupBy parameter
        var validGroupBy = new[] { "tier", "operation", "day" };
        groupBy ??= "tier";
        if (!validGroupBy.Contains(groupBy.ToLowerInvariant()))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = $"Invalid groupBy value. Expected one of: {string.Join(", ", validGroupBy)}."
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Getting cache statistics for user {UserId}, period {Period}, groupBy {GroupBy}",
            user.Id, period, groupBy);

        try
        {
            var stats = await _cacheStatisticsService.GetStatisticsAsync(
                user.Id,
                period,
                groupBy.ToLowerInvariant(),
                ct);

            return Ok(stats);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters in cache stats request");
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = ex.Message
            });
        }
    }

    private static bool IsValidPeriod(string period)
    {
        if (string.IsNullOrWhiteSpace(period))
            return false;

        var parts = period.Split('-');
        if (parts.Length != 2)
            return false;

        return int.TryParse(parts[0], out var year) &&
               int.TryParse(parts[1], out var month) &&
               year >= 2000 && year <= 2100 &&
               month >= 1 && month <= 12;
    }

    private static string GetPreviousPeriod(string period)
    {
        var parts = period.Split('-');
        var year = int.Parse(parts[0]);
        var month = int.Parse(parts[1]);

        if (month == 1)
        {
            return $"{year - 1}-12";
        }
        return $"{year}-{month - 1:D2}";
    }
}
