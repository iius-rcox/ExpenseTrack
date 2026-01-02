using ExpenseFlow.Api.Validators;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for expense analytics including spending trends, category breakdowns,
/// merchant analytics, subscription tracking, MoM comparison, and data export.
/// </summary>
[Authorize]
public class AnalyticsController : ApiControllerBase
{
    private readonly IComparisonService _comparisonService;
    private readonly ICacheStatisticsService _cacheStatisticsService;
    private readonly IAnalyticsService _analyticsService;
    private readonly IAnalyticsExportService _analyticsExportService;
    private readonly IUserService _userService;
    private readonly ILogger<AnalyticsController> _logger;
    private readonly ExpenseFlowDbContext _dbContext;

    public AnalyticsController(
        IComparisonService comparisonService,
        ICacheStatisticsService cacheStatisticsService,
        IAnalyticsService analyticsService,
        IAnalyticsExportService analyticsExportService,
        IUserService userService,
        ILogger<AnalyticsController> logger,
        ExpenseFlowDbContext dbContext)
    {
        _comparisonService = comparisonService;
        _cacheStatisticsService = cacheStatisticsService;
        _analyticsService = analyticsService;
        _analyticsExportService = analyticsExportService;
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

    #region Feature 019: Analytics Dashboard Endpoints

    /// <summary>
    /// Gets spending trends over time with configurable granularity.
    /// </summary>
    /// <param name="startDate">Start date (ISO format YYYY-MM-DD)</param>
    /// <param name="endDate">End date (ISO format YYYY-MM-DD)</param>
    /// <param name="granularity">Aggregation: "day" (default), "week", or "month"</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Array of spending data points</returns>
    [HttpGet("spending-trend")]
    [ProducesResponseType(typeof(List<SpendingTrendItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<SpendingTrendItemDto>>> GetSpendingTrend(
        [FromQuery] string startDate,
        [FromQuery] string endDate,
        [FromQuery] string granularity = "day",
        CancellationToken ct = default)
    {
        // Validate date range
        if (!AnalyticsValidation.ValidateDateRange(startDate, endDate, out var start, out var end, out var dateError))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = dateError
            });
        }

        // Validate granularity
        if (!AnalyticsValidation.ValidateGranularity(granularity, out var granularityError))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = granularityError
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Getting spending trend for user {UserId} from {StartDate} to {EndDate} with {Granularity} granularity",
            user.Id, start, end, granularity);

        var result = await _analyticsService.GetSpendingTrendAsync(
            user.Id, start, end, granularity ?? "day", ct);

        return Ok(result);
    }

    /// <summary>
    /// Gets spending breakdown by category for a date range.
    /// Categories are derived from transaction descriptions using pattern matching.
    /// </summary>
    /// <param name="startDate">Start date (ISO format YYYY-MM-DD)</param>
    /// <param name="endDate">End date (ISO format YYYY-MM-DD)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Array of category spending summaries</returns>
    [HttpGet("spending-by-category")]
    [ProducesResponseType(typeof(List<SpendingByCategoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<SpendingByCategoryItemDto>>> GetSpendingByCategory(
        [FromQuery] string startDate,
        [FromQuery] string endDate,
        CancellationToken ct = default)
    {
        // Validate date range
        if (!AnalyticsValidation.ValidateDateRange(startDate, endDate, out var start, out var end, out var dateError))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = dateError
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Getting spending by category for user {UserId} from {StartDate} to {EndDate}",
            user.Id, start, end);

        var result = await _analyticsService.GetSpendingByCategoryAsync(user.Id, start, end, ct);

        return Ok(result);
    }

    /// <summary>
    /// Gets spending breakdown by vendor for a date range.
    /// </summary>
    /// <param name="startDate">Start date (ISO format YYYY-MM-DD)</param>
    /// <param name="endDate">End date (ISO format YYYY-MM-DD)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Array of vendor spending summaries</returns>
    [HttpGet("spending-by-vendor")]
    [ProducesResponseType(typeof(List<SpendingByVendorItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<SpendingByVendorItemDto>>> GetSpendingByVendor(
        [FromQuery] string startDate,
        [FromQuery] string endDate,
        CancellationToken ct = default)
    {
        // Validate date range
        if (!AnalyticsValidation.ValidateDateRange(startDate, endDate, out var start, out var end, out var dateError))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = dateError
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Getting spending by vendor for user {UserId} from {StartDate} to {EndDate}",
            user.Id, start, end);

        var result = await _analyticsService.GetSpendingByVendorAsync(user.Id, start, end, ct);

        return Ok(result);
    }

    /// <summary>
    /// Gets merchant analytics with optional comparison to previous period.
    /// </summary>
    /// <param name="startDate">Start date (ISO format YYYY-MM-DD)</param>
    /// <param name="endDate">End date (ISO format YYYY-MM-DD)</param>
    /// <param name="topCount">Number of top merchants (1-100, default 10)</param>
    /// <param name="includeComparison">Include comparison with previous period</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Merchant analytics with trends</returns>
    [HttpGet("merchants")]
    [ProducesResponseType(typeof(MerchantAnalyticsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MerchantAnalyticsResponseDto>> GetMerchantAnalytics(
        [FromQuery] string startDate,
        [FromQuery] string endDate,
        [FromQuery] int topCount = 10,
        [FromQuery] bool includeComparison = false,
        CancellationToken ct = default)
    {
        // Validate date range
        if (!AnalyticsValidation.ValidateDateRange(startDate, endDate, out var start, out var end, out var dateError))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = dateError
            });
        }

        // Validate topCount
        if (!AnalyticsValidation.ValidateTopCount(topCount, out var topCountError))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = topCountError
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Getting merchant analytics for user {UserId} from {StartDate} to {EndDate}, top {TopCount}, comparison: {IncludeComparison}",
            user.Id, start, end, topCount, includeComparison);

        var result = await _analyticsService.GetMerchantAnalyticsAsync(
            user.Id, start, end, topCount, includeComparison, ct);

        return Ok(result);
    }

    /// <summary>
    /// Gets detected subscriptions with optional filters.
    /// Proxies to existing subscription detection service.
    /// </summary>
    /// <param name="minConfidence">Minimum confidence level: "high", "medium", "low"</param>
    /// <param name="frequency">Filter by frequencies (comma-separated)</param>
    /// <param name="includeAcknowledged">Include acknowledged subscriptions (default true)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Subscription detection results</returns>
    [HttpGet("subscriptions")]
    [ProducesResponseType(typeof(AnalyticsSubscriptionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AnalyticsSubscriptionResponseDto>> GetSubscriptions(
        [FromQuery] string? minConfidence = null,
        [FromQuery] List<string>? frequency = null,
        [FromQuery] bool includeAcknowledged = true,
        CancellationToken ct = default)
    {
        // Validate minConfidence
        if (!AnalyticsValidation.ValidateConfidenceLevel(minConfidence, out var confidenceError))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = confidenceError
            });
        }

        // Validate frequencies
        if (!AnalyticsValidation.ValidateFrequencies(frequency, out var frequencyError))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = frequencyError
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Getting subscriptions for user {UserId}, minConfidence: {MinConfidence}, includeAcknowledged: {IncludeAcknowledged}",
            user.Id, minConfidence, includeAcknowledged);

        var result = await _analyticsService.GetSubscriptionsAsync(
            user.Id, minConfidence, frequency, includeAcknowledged, ct);

        return Ok(result);
    }

    /// <summary>
    /// Triggers subscription analysis for the user.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Analysis result with detected count</returns>
    [HttpPost("subscriptions/analyze")]
    [ProducesResponseType(typeof(SubscriptionAnalysisResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SubscriptionAnalysisResultDto>> AnalyzeSubscriptions(
        CancellationToken ct = default)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation("Triggering subscription analysis for user {UserId}", user.Id);

        var result = await _analyticsService.AnalyzeSubscriptionsAsync(user.Id, ct);

        return Ok(result);
    }

    /// <summary>
    /// Acknowledges or unacknowledges a subscription.
    /// </summary>
    /// <param name="subscriptionId">Subscription ID</param>
    /// <param name="request">Acknowledgement request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>204 if successful, 404 if not found</returns>
    [HttpPost("subscriptions/{subscriptionId}/acknowledge")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AcknowledgeSubscription(
        [FromRoute] Guid subscriptionId,
        [FromBody] AcknowledgeSubscriptionRequest request,
        CancellationToken ct = default)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Acknowledging subscription {SubscriptionId} for user {UserId}: {Acknowledged}",
            subscriptionId, user.Id, request.Acknowledged);

        var success = await _analyticsService.AcknowledgeSubscriptionAsync(
            user.Id, subscriptionId, request.Acknowledged, ct);

        if (!success)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = $"Subscription {subscriptionId} not found."
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Exports analytics data to CSV or Excel format.
    /// Supports multiple sections: trends, categories, vendors, and transactions.
    /// </summary>
    /// <param name="request">Export request with date range, format, and sections</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>File download with analytics data</returns>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "text/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportAnalytics(
        [FromQuery] AnalyticsExportRequestDto request,
        CancellationToken ct = default)
    {
        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.StartDate) || string.IsNullOrWhiteSpace(request.EndDate))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = "StartDate and EndDate are required."
            });
        }

        // Validate date range format
        if (!AnalyticsValidation.ValidateDateRange(request.StartDate, request.EndDate, out var startDate, out var endDate, out var dateError))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = dateError
            });
        }

        // Validate format
        var format = request.Format?.ToLowerInvariant() ?? "csv";
        if (format != "csv" && format != "xlsx")
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = "Format must be 'csv' or 'xlsx'."
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);
        var sections = request.GetSectionsList();

        _logger.LogInformation(
            "Exporting analytics for user {UserId} from {StartDate} to {EndDate}, format: {Format}, sections: {Sections}",
            user.Id, startDate, endDate, format, string.Join(",", sections));

        try
        {
            var (fileBytes, contentType, fileName) = await _analyticsExportService.ExportAsync(
                user.Id, startDate, endDate, format, sections, ct);

            // Set Content-Disposition header for file download
            Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");

            return File(fileBytes, contentType, fileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Export validation error for user {UserId}", user.Id);
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Validation Error",
                Detail = ex.Message
            });
        }
    }

    #endregion
}
