using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for cache tier usage statistics aggregation.
/// </summary>
public interface ICacheStatisticsService
{
    /// <summary>
    /// Gets cache tier usage statistics for a specified period.
    /// Returns hit rates, estimated costs, and performance metrics.
    /// </summary>
    /// <param name="userId">User ID for filtering (admin may access all)</param>
    /// <param name="period">Period specification ("YYYY-MM", "last30days", "last7days")</param>
    /// <param name="groupBy">Grouping option ("none" or "operation")</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Cache statistics with overall metrics and optional per-operation breakdown</returns>
    Task<CacheStatisticsResponse> GetStatisticsAsync(
        Guid userId,
        string period,
        string? groupBy,
        CancellationToken ct = default);
}
