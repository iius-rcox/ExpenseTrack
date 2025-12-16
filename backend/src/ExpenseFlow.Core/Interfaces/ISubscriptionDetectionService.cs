using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for subscription detection and management.
/// Uses Tier 1 (rule-based) detection with pattern matching against known vendors.
/// </summary>
public interface ISubscriptionDetectionService
{
    #region Detection Operations

    /// <summary>
    /// Detects subscription patterns from a single transaction.
    /// Uses Tier 1 rule-based detection against known subscription vendors and patterns.
    /// </summary>
    /// <param name="transaction">The transaction to analyze.</param>
    /// <returns>Detection result with subscription details if detected.</returns>
    Task<SubscriptionDetectionResultDto> DetectFromTransactionAsync(Transaction transaction);

    /// <summary>
    /// Batch processes transactions to detect subscription patterns.
    /// More efficient for processing multiple transactions from a statement import.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="transactions">Transactions to analyze.</param>
    /// <returns>Batch detection results.</returns>
    Task<BatchSubscriptionDetectionResultDto> DetectFromTransactionsAsync(
        Guid userId, IEnumerable<Transaction> transactions);

    /// <summary>
    /// Runs monthly subscription check to identify missing subscriptions.
    /// Called by scheduled job at month-end.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="month">Month to check (YYYY-MM format).</param>
    /// <returns>List of alerts for missing or anomalous subscriptions.</returns>
    Task<List<SubscriptionAlertDto>> RunMonthlyCheckAsync(Guid userId, string month);

    #endregion

    #region Subscription Management

    /// <summary>
    /// Gets paginated list of subscriptions with optional filters.
    /// </summary>
    Task<SubscriptionListResponseDto> GetSubscriptionsAsync(
        Guid userId,
        int page,
        int pageSize,
        SubscriptionStatus? status = null);

    /// <summary>
    /// Gets subscription details by ID.
    /// </summary>
    Task<SubscriptionDetailDto?> GetSubscriptionAsync(Guid userId, Guid subscriptionId);

    /// <summary>
    /// Creates a manual subscription entry.
    /// </summary>
    Task<SubscriptionDetailDto> CreateSubscriptionAsync(
        Guid userId, CreateSubscriptionRequestDto request);

    /// <summary>
    /// Updates an existing subscription.
    /// </summary>
    Task<SubscriptionDetailDto?> UpdateSubscriptionAsync(
        Guid userId, Guid subscriptionId, UpdateSubscriptionRequestDto request);

    /// <summary>
    /// Deletes a subscription.
    /// </summary>
    Task<bool> DeleteSubscriptionAsync(Guid userId, Guid subscriptionId);

    #endregion

    #region Alerts

    /// <summary>
    /// Gets active alerts for user subscriptions.
    /// </summary>
    Task<SubscriptionAlertListResponseDto> GetAlertsAsync(
        Guid userId, bool includeAcknowledged = false);

    /// <summary>
    /// Acknowledges one or more alerts.
    /// </summary>
    Task<int> AcknowledgeAlertsAsync(Guid userId, List<Guid> alertIds);

    /// <summary>
    /// Gets subscription monitoring summary dashboard data.
    /// </summary>
    Task<SubscriptionMonitoringSummaryDto> GetMonitoringSummaryAsync(Guid userId);

    #endregion

    #region Known Vendors

    /// <summary>
    /// Checks if a vendor pattern matches known subscription vendors.
    /// </summary>
    /// <param name="vendorDescription">The vendor description to check.</param>
    /// <returns>The matched known vendor if found, null otherwise.</returns>
    Task<KnownSubscriptionVendor?> FindKnownVendorAsync(string vendorDescription);

    #endregion
}
