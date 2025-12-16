using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Repository interface for description cache operations.
/// </summary>
public interface IDescriptionCacheRepository
{
    /// <summary>
    /// Gets a cached description by its hash.
    /// </summary>
    /// <param name="hash">SHA-256 hash of the raw description.</param>
    /// <returns>Cache entry if found.</returns>
    Task<DescriptionCache?> GetByHashAsync(string hash);

    /// <summary>
    /// Adds a new cache entry.
    /// </summary>
    /// <param name="entry">The cache entry to add.</param>
    Task AddAsync(DescriptionCache entry);

    /// <summary>
    /// Increments the hit count and updates last accessed time.
    /// </summary>
    /// <param name="id">Cache entry ID.</param>
    Task IncrementHitCountAsync(Guid id);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Total entries and total hits.</returns>
    Task<(int TotalEntries, long TotalHits)> GetStatsAsync();

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync();
}
