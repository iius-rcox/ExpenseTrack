using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for aggregated cache statistics.
/// </summary>
public interface ICacheStatsService
{
    /// <summary>
    /// Gets statistics for all cache tables.
    /// </summary>
    /// <returns>Aggregated cache statistics.</returns>
    Task<CacheStatsResponse> GetAllStatsAsync();
}
