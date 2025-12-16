using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Repository interface for DetectedSubscription entity operations.
/// </summary>
public interface ISubscriptionRepository
{
    /// <summary>
    /// Gets a detected subscription by ID for a specific user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="subscriptionId">Subscription ID.</param>
    /// <returns>Subscription if found, null otherwise.</returns>
    Task<DetectedSubscription?> GetByIdAsync(Guid userId, Guid subscriptionId);

    /// <summary>
    /// Gets paginated subscriptions for a user with optional status filter.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="status">Optional status filter.</param>
    /// <returns>Tuple of subscriptions list and total count.</returns>
    Task<(List<DetectedSubscription> Subscriptions, int TotalCount)> GetPagedAsync(
        Guid userId,
        int page,
        int pageSize,
        SubscriptionStatus? status = null);

    /// <summary>
    /// Gets a subscription by vendor name for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="vendorName">Vendor name.</param>
    /// <returns>Subscription if found, null otherwise.</returns>
    Task<DetectedSubscription?> GetByVendorNameAsync(Guid userId, string vendorName);

    /// <summary>
    /// Gets subscriptions with a specific status for alerting.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="statuses">Statuses to filter by.</param>
    /// <returns>List of subscriptions.</returns>
    Task<List<DetectedSubscription>> GetByStatusesAsync(Guid userId, params SubscriptionStatus[] statuses);

    /// <summary>
    /// Gets subscriptions expecting a charge before a date.
    /// </summary>
    /// <param name="expectedBeforeDate">Date by which charge was expected.</param>
    /// <returns>List of subscriptions.</returns>
    Task<List<DetectedSubscription>> GetExpectedByDateAsync(DateOnly expectedBeforeDate);

    /// <summary>
    /// Adds a new detected subscription.
    /// </summary>
    /// <param name="subscription">Subscription to add.</param>
    Task AddAsync(DetectedSubscription subscription);

    /// <summary>
    /// Updates an existing subscription.
    /// </summary>
    /// <param name="subscription">Subscription to update.</param>
    Task UpdateAsync(DetectedSubscription subscription);

    /// <summary>
    /// Deletes a subscription.
    /// </summary>
    /// <param name="subscription">Subscription to delete.</param>
    Task DeleteAsync(DetectedSubscription subscription);

    /// <summary>
    /// Gets all known subscription vendors (active only).
    /// </summary>
    /// <returns>List of known subscription vendors.</returns>
    Task<List<KnownSubscriptionVendor>> GetKnownVendorsAsync();

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync();
}
