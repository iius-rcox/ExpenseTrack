using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for normalizing raw bank descriptions to human-readable format.
/// </summary>
public interface IDescriptionNormalizationService
{
    /// <summary>
    /// Normalizes a raw bank description to human-readable format.
    /// Uses tiered approach: Tier 1 (cache) → Tier 3 (AI inference).
    /// </summary>
    /// <param name="rawDescription">The raw bank description to normalize.</param>
    /// <param name="userId">The user ID for tier usage logging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Normalization result including tier used and confidence.</returns>
    Task<NormalizationResultDto> NormalizeAsync(
        string rawDescription,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache statistics for description normalization.
    /// </summary>
    /// <returns>Total entries and cache hit count.</returns>
    Task<(int TotalEntries, int TotalHits)> GetCacheStatsAsync();
}
