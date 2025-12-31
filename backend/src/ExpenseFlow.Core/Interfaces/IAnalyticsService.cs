using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for analytics dashboard operations.
/// Provides spending trends, category breakdowns, vendor analysis, and merchant insights.
/// </summary>
public interface IAnalyticsService
{
    #region Spending Trends

    /// <summary>
    /// Gets spending trends over time with configurable granularity.
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="startDate">Start of the date range (inclusive).</param>
    /// <param name="endDate">End of the date range (inclusive).</param>
    /// <param name="granularity">Aggregation granularity: "day", "week", or "month".</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of spending data points for the requested period.</returns>
    Task<List<SpendingTrendItemDto>> GetSpendingTrendAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        string granularity,
        CancellationToken ct = default);

    #endregion

    #region Category Analysis

    /// <summary>
    /// Gets spending breakdown by category for a date range.
    /// Categories are derived from transaction descriptions using pattern matching.
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="startDate">Start of the date range (inclusive).</param>
    /// <param name="endDate">End of the date range (inclusive).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of category spending summaries with percentages.</returns>
    Task<List<SpendingByCategoryItemDto>> GetSpendingByCategoryAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default);

    #endregion

    #region Vendor Analysis

    /// <summary>
    /// Gets spending breakdown by vendor for a date range.
    /// Vendors are extracted from transaction descriptions.
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="startDate">Start of the date range (inclusive).</param>
    /// <param name="endDate">End of the date range (inclusive).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of vendor spending summaries with percentages.</returns>
    Task<List<SpendingByVendorItemDto>> GetSpendingByVendorAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default);

    #endregion

    #region Merchant Analytics

    /// <summary>
    /// Gets comprehensive merchant analytics with optional comparison period.
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="startDate">Start of the date range (inclusive).</param>
    /// <param name="endDate">End of the date range (inclusive).</param>
    /// <param name="topCount">Number of top merchants to return (1-100, default 10).</param>
    /// <param name="includeComparison">Whether to include comparison with previous period.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Merchant analytics response with top merchants, new merchants, and changes.</returns>
    Task<MerchantAnalyticsResponseDto> GetMerchantAnalyticsAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        int topCount = 10,
        bool includeComparison = false,
        CancellationToken ct = default);

    #endregion

    #region Subscription Analytics (Proxy)

    /// <summary>
    /// Gets subscription analytics by proxying to the subscription detection service.
    /// Exposed at /api/analytics/subscriptions for frontend convenience.
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="minConfidence">Optional minimum confidence filter: "high", "medium", "low".</param>
    /// <param name="frequency">Optional frequency filters: weekly, biweekly, monthly, quarterly, annual.</param>
    /// <param name="includeAcknowledged">Whether to include acknowledged subscriptions (default true).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Analytics subscription response with detected subscriptions.</returns>
    Task<AnalyticsSubscriptionResponseDto> GetSubscriptionsAsync(
        Guid userId,
        string? minConfidence = null,
        List<string>? frequency = null,
        bool includeAcknowledged = true,
        CancellationToken ct = default);

    /// <summary>
    /// Triggers subscription analysis for the user.
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Analysis result with detected and analyzed counts.</returns>
    Task<SubscriptionAnalysisResultDto> AnalyzeSubscriptionsAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Acknowledges or unacknowledges a subscription.
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="subscriptionId">The subscription ID to acknowledge.</param>
    /// <param name="acknowledged">Whether to acknowledge or unacknowledge.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the subscription was found and updated.</returns>
    Task<bool> AcknowledgeSubscriptionAsync(
        Guid userId,
        Guid subscriptionId,
        bool acknowledged,
        CancellationToken ct = default);

    #endregion

    #region Helpers

    /// <summary>
    /// Derives a category from a transaction description using pattern matching.
    /// Categories: Transportation, Food &amp; Dining, Travel &amp; Lodging, Shopping &amp; Retail,
    /// Entertainment, Office &amp; Business, Healthcare, Utilities &amp; Bills, Other.
    /// </summary>
    /// <param name="description">The transaction description to categorize.</param>
    /// <returns>The derived category name.</returns>
    string DeriveCategory(string description);

    #endregion
}
