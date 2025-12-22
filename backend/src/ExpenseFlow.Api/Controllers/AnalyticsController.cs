using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    private readonly ExpenseFlowDbContext _dbContext;

    public AnalyticsController(
        IComparisonService comparisonService,
        ICacheStatisticsService cacheStatisticsService,
        IUserService userService,
        ILogger<AnalyticsController> logger,
        ExpenseFlowDbContext dbContext)
    {
        _comparisonService = comparisonService;
        _cacheStatisticsService = cacheStatisticsService;
        _userService = userService;
        _logger = logger;
        _dbContext = dbContext;
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

    /// <summary>
    /// Gets expense breakdown by category for a given period.
    /// </summary>
    /// <param name="period">Period in YYYY-MM format (e.g., "2025-01"). Defaults to current month.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Category breakdown with spending totals and percentages</returns>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(CategoryBreakdownDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CategoryBreakdownDto>> GetCategories(
        [FromQuery] string? period,
        CancellationToken ct)
    {
        // Default to current month if not specified
        var targetPeriod = period ?? $"{DateTime.UtcNow:yyyy-MM}";

        // Validate period format
        if (!IsValidPeriod(targetPeriod))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = "Invalid period format. Expected YYYY-MM (e.g., 2025-01)."
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Getting category breakdown for user {UserId}, period {Period}",
            user.Id, targetPeriod);

        // Parse period to get date range
        var parts = targetPeriod.Split('-');
        var year = int.Parse(parts[0]);
        var month = int.Parse(parts[1]);
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Get all transactions for the period with vendor alias info
        var transactions = await _dbContext.Transactions
            .Where(t => t.UserId == user.Id &&
                       t.TransactionDate >= startDate &&
                       t.TransactionDate <= endDate &&
                       t.Amount > 0) // Only expenses (positive amounts)
            .ToListAsync(ct);

        var totalSpending = transactions.Sum(t => t.Amount);
        var totalCount = transactions.Count;

        // Group by vendor category derived from description patterns
        // For now, we use the first word/segment of description as a pseudo-category
        var categories = transactions
            .GroupBy(t => DeriveCategory(t.Description))
            .Select(g => new CategorySpendingDto
            {
                Category = g.Key,
                Amount = g.Sum(t => t.Amount),
                Percentage = totalSpending > 0
                    ? Math.Round(g.Sum(t => t.Amount) / totalSpending * 100, 2)
                    : 0,
                TransactionCount = g.Count()
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        var breakdown = new CategoryBreakdownDto
        {
            Period = targetPeriod,
            TotalSpending = totalSpending,
            TransactionCount = totalCount,
            Categories = categories
        };

        return Ok(breakdown);
    }

    /// <summary>
    /// Derives a category name from a transaction description.
    /// Uses common vendor patterns to infer spending categories.
    /// </summary>
    private static string DeriveCategory(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return "Uncategorized";

        var desc = description.ToUpperInvariant();

        // Transportation
        if (desc.Contains("UBER") || desc.Contains("LYFT") || desc.Contains("TAXI") ||
            desc.Contains("PARKING") || desc.Contains("GAS") || desc.Contains("SHELL") ||
            desc.Contains("CHEVRON") || desc.Contains("EXXON") || desc.Contains("FUEL"))
            return "Transportation";

        // Food & Dining
        if (desc.Contains("RESTAURANT") || desc.Contains("CAFE") || desc.Contains("COFFEE") ||
            desc.Contains("STARBUCKS") || desc.Contains("MCDONALD") || desc.Contains("CHIPOTLE") ||
            desc.Contains("DOORDASH") || desc.Contains("GRUBHUB") || desc.Contains("UBEREATS") ||
            desc.Contains("DINER") || desc.Contains("PIZZA") || desc.Contains("SUSHI"))
            return "Food & Dining";

        // Travel & Lodging
        if (desc.Contains("HOTEL") || desc.Contains("MARRIOTT") || desc.Contains("HILTON") ||
            desc.Contains("AIRBNB") || desc.Contains("AIRLINE") || desc.Contains("SOUTHWEST") ||
            desc.Contains("DELTA") || desc.Contains("UNITED") || desc.Contains("AMERICAN AIR"))
            return "Travel & Lodging";

        // Shopping & Retail
        if (desc.Contains("AMAZON") || desc.Contains("WALMART") || desc.Contains("TARGET") ||
            desc.Contains("COSTCO") || desc.Contains("BEST BUY") || desc.Contains("APPLE"))
            return "Shopping & Retail";

        // Entertainment & Subscriptions
        if (desc.Contains("NETFLIX") || desc.Contains("SPOTIFY") || desc.Contains("HULU") ||
            desc.Contains("DISNEY") || desc.Contains("YOUTUBE") || desc.Contains("MOVIE") ||
            desc.Contains("THEATER") || desc.Contains("CONCERT"))
            return "Entertainment";

        // Office & Business
        if (desc.Contains("OFFICE") || desc.Contains("STAPLES") || desc.Contains("FEDEX") ||
            desc.Contains("UPS") || desc.Contains("ZOOM") || desc.Contains("MICROSOFT") ||
            desc.Contains("ADOBE") || desc.Contains("GOOGLE"))
            return "Office & Business";

        // Healthcare
        if (desc.Contains("PHARMACY") || desc.Contains("CVS") || desc.Contains("WALGREENS") ||
            desc.Contains("MEDICAL") || desc.Contains("DOCTOR") || desc.Contains("HOSPITAL"))
            return "Healthcare";

        // Utilities & Bills
        if (desc.Contains("ELECTRIC") || desc.Contains("WATER") || desc.Contains("GAS BILL") ||
            desc.Contains("INTERNET") || desc.Contains("PHONE") || desc.Contains("VERIZON") ||
            desc.Contains("AT&T") || desc.Contains("T-MOBILE"))
            return "Utilities & Bills";

        return "Other";
    }
}
