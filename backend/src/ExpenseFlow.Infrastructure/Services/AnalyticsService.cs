using System.Globalization;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for analytics dashboard operations.
/// Provides spending trends, category breakdowns, vendor analysis, and merchant insights.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ISubscriptionDetectionService _subscriptionService;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        ExpenseFlowDbContext dbContext,
        ISubscriptionDetectionService subscriptionService,
        ILogger<AnalyticsService> logger)
    {
        _dbContext = dbContext;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    #region Spending Trends

    /// <inheritdoc />
    public async Task<List<SpendingTrendItemDto>> GetSpendingTrendAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        string granularity,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Getting spending trend for user {UserId} from {StartDate} to {EndDate} with {Granularity} granularity",
            userId, startDate, endDate, granularity);

        // Get all transactions in range (both positive and negative per FR-015)
        var transactions = await _dbContext.Transactions
            .Where(t => t.UserId == userId &&
                       t.TransactionDate >= startDate &&
                       t.TransactionDate <= endDate)
            .Select(t => new { t.TransactionDate, t.Amount })
            .ToListAsync(ct);

        // Group by granularity
        var grouped = granularity.ToLowerInvariant() switch
        {
            "week" => transactions
                .GroupBy(t => GetIsoWeekKey(t.TransactionDate))
                .Select(g => new SpendingTrendItemDto
                {
                    Date = g.Key,
                    Amount = g.Sum(t => t.Amount),
                    TransactionCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList(),

            "month" => transactions
                .GroupBy(t => t.TransactionDate.ToString("yyyy-MM"))
                .Select(g => new SpendingTrendItemDto
                {
                    Date = g.Key,
                    Amount = g.Sum(t => t.Amount),
                    TransactionCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList(),

            _ => transactions // "day" is default
                .GroupBy(t => t.TransactionDate.ToString("yyyy-MM-dd"))
                .Select(g => new SpendingTrendItemDto
                {
                    Date = g.Key,
                    Amount = g.Sum(t => t.Amount),
                    TransactionCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList()
        };

        _logger.LogDebug(
            "Spending trend returned {Count} data points for user {UserId}",
            grouped.Count, userId);

        return grouped;
    }

    /// <summary>
    /// Gets ISO week key in format "YYYY-Www" (e.g., "2025-W05").
    /// Uses ISO 8601 week numbering (weeks start on Monday, first week has 4+ days of year).
    /// </summary>
    private static string GetIsoWeekKey(DateOnly date)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);
        var calendar = CultureInfo.InvariantCulture.Calendar;
        var week = calendar.GetWeekOfYear(
            dateTime,
            CalendarWeekRule.FirstFourDayWeek,
            DayOfWeek.Monday);

        // Handle year boundary: if week is 52/53 but month is January, use previous year
        // If week is 1 but month is December, use next year
        var year = date.Year;
        if (week >= 52 && date.Month == 1)
            year--;
        else if (week == 1 && date.Month == 12)
            year++;

        return $"{year}-W{week:D2}";
    }

    #endregion

    #region Category Analysis

    /// <inheritdoc />
    public async Task<List<SpendingByCategoryItemDto>> GetSpendingByCategoryAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Getting spending by category for user {UserId} from {StartDate} to {EndDate}",
            userId, startDate, endDate);

        // Get all transactions in range (both positive and negative per FR-015)
        var transactions = await _dbContext.Transactions
            .Where(t => t.UserId == userId &&
                       t.TransactionDate >= startDate &&
                       t.TransactionDate <= endDate)
            .Select(t => new { t.Description, t.Amount })
            .ToListAsync(ct);

        var totalAmount = transactions.Sum(t => t.Amount);

        var categories = transactions
            .GroupBy(t => DeriveCategory(t.Description))
            .Select(g => new SpendingByCategoryItemDto
            {
                Category = g.Key,
                Amount = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                PercentageOfTotal = totalAmount != 0
                    ? Math.Round(g.Sum(t => t.Amount) / totalAmount * 100, 2)
                    : 0
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        _logger.LogDebug(
            "Spending by category returned {Count} categories for user {UserId}",
            categories.Count, userId);

        return categories;
    }

    #endregion

    #region Vendor Analysis

    /// <inheritdoc />
    public async Task<List<SpendingByVendorItemDto>> GetSpendingByVendorAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Getting spending by vendor for user {UserId} from {StartDate} to {EndDate}",
            userId, startDate, endDate);

        // Get all transactions in range (both positive and negative per FR-015)
        var transactions = await _dbContext.Transactions
            .Where(t => t.UserId == userId &&
                       t.TransactionDate >= startDate &&
                       t.TransactionDate <= endDate)
            .Select(t => new { t.Description, t.Amount })
            .ToListAsync(ct);

        var totalAmount = transactions.Sum(t => t.Amount);

        // Group by description (vendor name) - no normalization per spec
        var vendors = transactions
            .GroupBy(t => ExtractVendorName(t.Description))
            .Select(g => new SpendingByVendorItemDto
            {
                VendorName = g.Key,
                Amount = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                PercentageOfTotal = totalAmount != 0
                    ? Math.Round(g.Sum(t => t.Amount) / totalAmount * 100, 2)
                    : 0
            })
            .OrderByDescending(v => v.Amount)
            .ToList();

        _logger.LogDebug(
            "Spending by vendor returned {Count} vendors for user {UserId}",
            vendors.Count, userId);

        return vendors;
    }

    /// <summary>
    /// Extracts vendor name from transaction description.
    /// Returns the raw description for now (no normalization per MVP spec).
    /// </summary>
    private static string ExtractVendorName(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return "Unknown";

        return description.Trim();
    }

    #endregion

    #region Merchant Analytics

    /// <inheritdoc />
    public async Task<MerchantAnalyticsResponseDto> GetMerchantAnalyticsAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        int topCount = 10,
        bool includeComparison = false,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Getting merchant analytics for user {UserId} from {StartDate} to {EndDate}, top {TopCount}, comparison: {IncludeComparison}",
            userId, startDate, endDate, topCount, includeComparison);

        // Get current period transactions
        var currentTransactions = await _dbContext.Transactions
            .Where(t => t.UserId == userId &&
                       t.TransactionDate >= startDate &&
                       t.TransactionDate <= endDate)
            .Select(t => new { t.Description, t.Amount })
            .ToListAsync(ct);

        var totalAmount = currentTransactions.Sum(t => t.Amount);

        // Build merchant analytics for current period
        var merchantData = currentTransactions
            .GroupBy(t => ExtractVendorName(t.Description))
            .Select(g => new
            {
                MerchantName = g.Key,
                TotalAmount = g.Sum(t => t.Amount),
                TransactionCount = g.Count(),
                AverageAmount = g.Average(t => t.Amount)
            })
            .OrderByDescending(m => m.TotalAmount)
            .ToList();

        var topMerchants = new List<TopMerchantDto>();
        var newMerchants = new List<TopMerchantDto>();
        var significantChanges = new List<TopMerchantDto>();

        Dictionary<string, decimal>? previousMerchantAmounts = null;

        if (includeComparison)
        {
            // Calculate comparison period (same duration, immediately preceding)
            var duration = endDate.DayNumber - startDate.DayNumber;
            var comparisonEndDate = startDate.AddDays(-1);
            var comparisonStartDate = comparisonEndDate.AddDays(-duration);

            var previousTransactions = await _dbContext.Transactions
                .Where(t => t.UserId == userId &&
                           t.TransactionDate >= comparisonStartDate &&
                           t.TransactionDate <= comparisonEndDate)
                .Select(t => new { t.Description, t.Amount })
                .ToListAsync(ct);

            previousMerchantAmounts = previousTransactions
                .GroupBy(t => ExtractVendorName(t.Description))
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
        }

        foreach (var merchant in merchantData)
        {
            var dto = new TopMerchantDto
            {
                MerchantName = merchant.MerchantName,
                DisplayName = null, // Could be enhanced with VendorAlias lookup
                TotalAmount = merchant.TotalAmount,
                TransactionCount = merchant.TransactionCount,
                AverageAmount = Math.Round(merchant.AverageAmount, 2),
                PercentageOfTotal = totalAmount != 0
                    ? Math.Round(merchant.TotalAmount / totalAmount * 100, 2)
                    : 0,
                PreviousAmount = null,
                ChangePercent = null,
                Trend = null
            };

            if (includeComparison && previousMerchantAmounts != null)
            {
                if (previousMerchantAmounts.TryGetValue(merchant.MerchantName, out var prevAmount))
                {
                    dto = dto with
                    {
                        PreviousAmount = prevAmount,
                        ChangePercent = prevAmount != 0
                            ? Math.Round((merchant.TotalAmount - prevAmount) / prevAmount * 100, 2)
                            : null,
                        Trend = CalculateTrend(merchant.TotalAmount, prevAmount)
                    };

                    // Check for significant change (>50%)
                    if (dto.ChangePercent.HasValue && Math.Abs(dto.ChangePercent.Value) > 50)
                    {
                        significantChanges.Add(dto);
                    }
                }
                else
                {
                    // New merchant (not in previous period)
                    newMerchants.Add(dto);
                }
            }

            topMerchants.Add(dto);
        }

        // Take only topCount for the main list
        topMerchants = topMerchants.Take(topCount).ToList();

        var response = new MerchantAnalyticsResponseDto
        {
            TopMerchants = topMerchants,
            NewMerchants = newMerchants.Take(topCount).ToList(),
            SignificantChanges = significantChanges.OrderByDescending(m => Math.Abs(m.ChangePercent ?? 0)).Take(topCount).ToList(),
            TotalMerchantCount = merchantData.Count,
            DateRange = new AnalyticsDateRangeDto
            {
                StartDate = startDate.ToString("yyyy-MM-dd"),
                EndDate = endDate.ToString("yyyy-MM-dd")
            }
        };

        _logger.LogDebug(
            "Merchant analytics: {TopCount} top, {NewCount} new, {ChangeCount} significant changes for user {UserId}",
            response.TopMerchants.Count, response.NewMerchants.Count, response.SignificantChanges.Count, userId);

        return response;
    }

    /// <summary>
    /// Calculates trend direction based on current and previous amounts.
    /// Stable if change is less than 10%.
    /// </summary>
    private static string CalculateTrend(decimal current, decimal previous)
    {
        if (previous == 0)
            return "increasing";

        var changePercent = (current - previous) / previous * 100;

        return changePercent switch
        {
            > 10 => "increasing",
            < -10 => "decreasing",
            _ => "stable"
        };
    }

    #endregion

    #region Subscription Analytics (Proxy)

    /// <inheritdoc />
    public async Task<AnalyticsSubscriptionResponseDto> GetSubscriptionsAsync(
        Guid userId,
        string? minConfidence = null,
        List<string>? frequency = null,
        bool includeAcknowledged = true,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Getting subscriptions for user {UserId}, minConfidence: {MinConfidence}, includeAcknowledged: {IncludeAcknowledged}",
            userId, minConfidence, includeAcknowledged);

        // Get all subscriptions via existing service
        var subscriptionList = await _subscriptionService.GetSubscriptionsAsync(
            userId, page: 1, pageSize: 1000, status: null);

        var allSubscriptions = subscriptionList.Subscriptions;

        // Filter by confidence if specified
        if (!string.IsNullOrWhiteSpace(minConfidence))
        {
            // Map confidence levels to priority (high = 3, medium = 2, low = 1)
            var minLevel = minConfidence.ToLowerInvariant() switch
            {
                "high" => 3,
                "medium" => 2,
                "low" => 1,
                _ => 0
            };

            // For now, we don't have confidence in SubscriptionSummaryDto
            // This would need enhancement in the subscription service
            // Placeholder: no filtering until subscription DTOs are enhanced
        }

        // Map to analytics DTOs
        var subscriptionDtos = allSubscriptions.Select(s => new AnalyticsSubscriptionDetailDto
        {
            Id = s.Id,
            MerchantName = s.VendorName,
            Frequency = "monthly", // Default - would need enhancement
            Amount = s.AverageAmount,
            Confidence = "medium", // Default - would need enhancement
            FirstSeen = s.LastSeenDate.AddMonths(-s.OccurrenceCount + 1),
            LastSeen = s.LastSeenDate,
            IsAcknowledged = s.Status == SubscriptionStatus.Active
        }).ToList();

        // Get monitoring summary for estimated totals
        var summary = await _subscriptionService.GetMonitoringSummaryAsync(userId);

        var response = new AnalyticsSubscriptionResponseDto
        {
            Subscriptions = subscriptionDtos,
            Summary = new AnalyticsSubscriptionSummaryDto
            {
                SubscriptionCount = subscriptionDtos.Count,
                EstimatedMonthlyTotal = subscriptionDtos.Sum(s => s.Amount),
                EstimatedAnnualTotal = subscriptionDtos.Sum(s => s.Amount) * 12
            },
            NewSubscriptions = [], // Would need tracking of "new" subscriptions
            PossiblyEnded = [], // Would need tracking of missing subscriptions
            AnalyzedAt = DateTime.UtcNow
        };

        return response;
    }

    /// <inheritdoc />
    public async Task<SubscriptionAnalysisResultDto> AnalyzeSubscriptionsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Triggering subscription analysis for user {UserId}", userId);

        // Get recent transactions (last 90 days)
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = endDate.AddDays(-90);

        var transactions = await _dbContext.Transactions
            .Where(t => t.UserId == userId &&
                       t.TransactionDate >= startDate &&
                       t.TransactionDate <= endDate)
            .ToListAsync(ct);

        // Run batch detection
        var result = await _subscriptionService.DetectFromTransactionsAsync(userId, transactions);

        return new SubscriptionAnalysisResultDto
        {
            Detected = result.NewSubscriptions,
            Analyzed = result.TransactionsProcessed
        };
    }

    /// <inheritdoc />
    public async Task<bool> AcknowledgeSubscriptionAsync(
        Guid userId,
        Guid subscriptionId,
        bool acknowledged,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Acknowledging subscription {SubscriptionId} for user {UserId}: {Acknowledged}",
            subscriptionId, userId, acknowledged);

        // Get the subscription
        var subscription = await _subscriptionService.GetSubscriptionAsync(userId, subscriptionId);
        if (subscription == null)
        {
            _logger.LogWarning(
                "Subscription {SubscriptionId} not found for user {UserId}",
                subscriptionId, userId);
            return false;
        }

        // Update status based on acknowledgement
        var updateRequest = new UpdateSubscriptionRequestDto
        {
            VendorName = subscription.VendorName,
            ExpectedAmount = subscription.AverageAmount,
            ExpectedDay = subscription.ExpectedNextDate?.Day ?? 1,
            Status = acknowledged ? SubscriptionStatus.Active : SubscriptionStatus.Flagged,
            Category = subscription.Category
        };

        var updated = await _subscriptionService.UpdateSubscriptionAsync(userId, subscriptionId, updateRequest);
        return updated != null;
    }

    #endregion

    #region Helpers

    /// <inheritdoc />
    public string DeriveCategory(string description)
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

    #endregion
}
