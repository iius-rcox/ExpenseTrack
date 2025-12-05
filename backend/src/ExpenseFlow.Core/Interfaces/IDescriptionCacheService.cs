using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for description cache operations.
/// </summary>
public interface IDescriptionCacheService
{
    /// <summary>
    /// Gets a cached description by its hash.
    /// </summary>
    /// <param name="rawDescriptionHash">SHA-256 hash of raw description.</param>
    /// <returns>The cached entry if found, null otherwise.</returns>
    Task<DescriptionCache?> GetByHashAsync(string rawDescriptionHash);

    /// <summary>
    /// Adds or updates a description cache entry.
    /// Increments hit count if entry exists.
    /// </summary>
    /// <param name="rawDescription">The original description.</param>
    /// <param name="normalizedDescription">The AI-normalized description.</param>
    /// <returns>The cache entry.</returns>
    Task<DescriptionCache> AddOrUpdateAsync(string rawDescription, string normalizedDescription);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Total entries and total hits.</returns>
    Task<(int TotalEntries, int TotalHits)> GetStatsAsync();
}
