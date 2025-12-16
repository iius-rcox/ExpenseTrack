using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Repository interface for tier usage log operations.
/// </summary>
public interface ITierUsageRepository
{
    /// <summary>
    /// Adds a new tier usage log entry.
    /// </summary>
    /// <param name="log">The log entry to add.</param>
    Task AddAsync(TierUsageLog log);

    /// <summary>
    /// Gets tier usage statistics for a date range.
    /// </summary>
    /// <param name="startDate">Start of date range.</param>
    /// <param name="endDate">End of date range.</param>
    /// <param name="operationType">Optional filter by operation type.</param>
    /// <returns>Statistics grouped by tier and operation type.</returns>
    Task<IReadOnlyList<TierUsageAggregate>> GetAggregateAsync(
        DateTime startDate,
        DateTime endDate,
        string? operationType = null);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync();

    /// <summary>
    /// Gets vendors with high Tier 3 usage as candidates for alias creation.
    /// </summary>
    /// <param name="startDate">Start of date range.</param>
    /// <param name="endDate">End of date range.</param>
    /// <param name="minTier3Count">Minimum Tier 3 count to be considered a candidate.</param>
    /// <returns>List of vendor candidates with their Tier 3 counts.</returns>
    Task<IReadOnlyList<VendorTier3Usage>> GetVendorTier3UsageAsync(
        DateTime startDate,
        DateTime endDate,
        int minTier3Count = 5);
}

/// <summary>
/// Aggregated tier usage data for reporting.
/// </summary>
public record TierUsageAggregate(
    string OperationType,
    int TierUsed,
    int Count,
    decimal? AverageConfidence,
    int? AverageResponseTimeMs);

/// <summary>
/// Vendor with Tier 3 usage count for alias creation candidates.
/// </summary>
public record VendorTier3Usage(
    string VendorDescription,
    int Tier3Count);
