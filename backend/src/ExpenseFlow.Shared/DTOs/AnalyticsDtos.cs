namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Single data point in a spending trend time series.
/// Used by GET /api/analytics/spending-trend endpoint.
/// </summary>
public record SpendingTrendItemDto
{
    /// <summary>
    /// Period identifier (ISO date for day, "2025-W05" for week, "2025-02" for month).
    /// </summary>
    public required string Date { get; init; }

    /// <summary>
    /// Total amount for this period (sum of all transactions).
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Number of transactions in this period.
    /// </summary>
    public int TransactionCount { get; init; }
}

/// <summary>
/// Category spending aggregate.
/// Used by GET /api/analytics/spending-by-category endpoint.
/// </summary>
public record SpendingByCategoryItemDto
{
    /// <summary>
    /// Category name (derived from transaction patterns).
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Total amount spent in this category.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Number of transactions in this category.
    /// </summary>
    public int TransactionCount { get; init; }

    /// <summary>
    /// Percentage of total spending (0.00-100.00).
    /// </summary>
    public decimal PercentageOfTotal { get; init; }
}

/// <summary>
/// Vendor spending aggregate.
/// Used by GET /api/analytics/spending-by-vendor endpoint.
/// </summary>
public record SpendingByVendorItemDto
{
    /// <summary>
    /// Vendor name (from transaction description).
    /// </summary>
    public required string VendorName { get; init; }

    /// <summary>
    /// Total amount spent with this vendor.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Number of transactions with this vendor.
    /// </summary>
    public int TransactionCount { get; init; }

    /// <summary>
    /// Percentage of total spending (0.00-100.00).
    /// </summary>
    public decimal PercentageOfTotal { get; init; }
}

/// <summary>
/// Detailed merchant analytics.
/// </summary>
public record TopMerchantDto
{
    /// <summary>
    /// Merchant/vendor name.
    /// </summary>
    public required string MerchantName { get; init; }

    /// <summary>
    /// Display-friendly name (if different).
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// Total amount spent.
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// Transaction count.
    /// </summary>
    public int TransactionCount { get; init; }

    /// <summary>
    /// Average transaction amount.
    /// </summary>
    public decimal AverageAmount { get; init; }

    /// <summary>
    /// Percentage of total spending.
    /// </summary>
    public decimal PercentageOfTotal { get; init; }

    /// <summary>
    /// Previous period amount (null if comparison not requested or new merchant).
    /// </summary>
    public decimal? PreviousAmount { get; init; }

    /// <summary>
    /// Percentage change from previous period.
    /// </summary>
    public decimal? ChangePercent { get; init; }

    /// <summary>
    /// Trend direction: "increasing", "decreasing", or "stable".
    /// </summary>
    public string? Trend { get; init; }
}

/// <summary>
/// Comprehensive merchant analytics response.
/// Used by GET /api/analytics/merchants endpoint.
/// </summary>
public record MerchantAnalyticsResponseDto
{
    /// <summary>
    /// Top merchants by spending amount.
    /// </summary>
    public required List<TopMerchantDto> TopMerchants { get; init; }

    /// <summary>
    /// Merchants appearing in current period but not comparison period.
    /// </summary>
    public required List<TopMerchantDto> NewMerchants { get; init; }

    /// <summary>
    /// Merchants with significant spending changes (>50%).
    /// </summary>
    public required List<TopMerchantDto> SignificantChanges { get; init; }

    /// <summary>
    /// Total unique merchant count in the period.
    /// </summary>
    public int TotalMerchantCount { get; init; }

    /// <summary>
    /// Analysis date range.
    /// </summary>
    public required AnalyticsDateRangeDto DateRange { get; init; }
}

/// <summary>
/// Date range for analytics queries.
/// </summary>
public record AnalyticsDateRangeDto
{
    /// <summary>
    /// Start date (ISO format YYYY-MM-DD).
    /// </summary>
    public required string StartDate { get; init; }

    /// <summary>
    /// End date (ISO format YYYY-MM-DD).
    /// </summary>
    public required string EndDate { get; init; }
}

/// <summary>
/// Subscription detection results for analytics dashboard.
/// Used by GET /api/analytics/subscriptions endpoint.
/// </summary>
public record AnalyticsSubscriptionResponseDto
{
    /// <summary>
    /// Detected subscriptions.
    /// </summary>
    public required List<AnalyticsSubscriptionDetailDto> Subscriptions { get; init; }

    /// <summary>
    /// Summary statistics.
    /// </summary>
    public required AnalyticsSubscriptionSummaryDto Summary { get; init; }

    /// <summary>
    /// Newly detected subscriptions.
    /// </summary>
    public required List<AnalyticsSubscriptionDetailDto> NewSubscriptions { get; init; }

    /// <summary>
    /// Subscriptions that may have ended.
    /// </summary>
    public required List<AnalyticsSubscriptionDetailDto> PossiblyEnded { get; init; }

    /// <summary>
    /// When analysis was performed.
    /// </summary>
    public DateTime AnalyzedAt { get; init; }
}

/// <summary>
/// Subscription detail for analytics view.
/// </summary>
public record AnalyticsSubscriptionDetailDto
{
    /// <summary>
    /// Subscription ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Merchant/vendor name.
    /// </summary>
    public required string MerchantName { get; init; }

    /// <summary>
    /// Detected frequency: weekly, biweekly, monthly, quarterly, annual, unknown.
    /// </summary>
    public required string Frequency { get; init; }

    /// <summary>
    /// Subscription amount.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Detection confidence: high, medium, low.
    /// </summary>
    public required string Confidence { get; init; }

    /// <summary>
    /// When first detected.
    /// </summary>
    public DateOnly FirstSeen { get; init; }

    /// <summary>
    /// When last seen.
    /// </summary>
    public DateOnly LastSeen { get; init; }

    /// <summary>
    /// Whether user has acknowledged this subscription.
    /// </summary>
    public bool IsAcknowledged { get; init; }
}

/// <summary>
/// Subscription summary statistics for analytics.
/// </summary>
public record AnalyticsSubscriptionSummaryDto
{
    /// <summary>
    /// Total subscription count.
    /// </summary>
    public int SubscriptionCount { get; init; }

    /// <summary>
    /// Estimated monthly total across all subscriptions.
    /// </summary>
    public decimal EstimatedMonthlyTotal { get; init; }

    /// <summary>
    /// Estimated annual total across all subscriptions.
    /// </summary>
    public decimal EstimatedAnnualTotal { get; init; }
}

/// <summary>
/// Request for acknowledging a subscription.
/// </summary>
public record AcknowledgeSubscriptionRequest
{
    /// <summary>
    /// Whether to acknowledge or unacknowledge the subscription.
    /// </summary>
    public bool Acknowledged { get; init; }
}

/// <summary>
/// Response from subscription analysis trigger.
/// </summary>
public record SubscriptionAnalysisResultDto
{
    /// <summary>
    /// Number of subscriptions detected.
    /// </summary>
    public int Detected { get; init; }

    /// <summary>
    /// Number of transactions analyzed.
    /// </summary>
    public int Analyzed { get; init; }
}
