using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for statement fingerprint operations.
/// </summary>
public interface IStatementFingerprintService
{
    /// <summary>
    /// Gets a fingerprint by user and header hash.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="headerHash">SHA-256 hash of header row.</param>
    /// <returns>The fingerprint if found, null otherwise.</returns>
    Task<StatementFingerprint?> GetByUserAndHashAsync(Guid userId, string headerHash);

    /// <summary>
    /// Gets all fingerprints for a user.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <returns>List of fingerprints.</returns>
    Task<IReadOnlyList<StatementFingerprint>> GetByUserAsync(Guid userId);

    /// <summary>
    /// Adds or updates a statement fingerprint.
    /// </summary>
    /// <param name="fingerprint">The fingerprint to add or update.</param>
    /// <returns>The saved fingerprint.</returns>
    Task<StatementFingerprint> AddOrUpdateAsync(StatementFingerprint fingerprint);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Total entries and placeholder hit count.</returns>
    Task<(int TotalEntries, int TotalHits)> GetStatsAsync();
}
