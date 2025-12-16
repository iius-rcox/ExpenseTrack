using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for logging and reporting tier usage metrics.
/// </summary>
public interface ITierUsageService
{
    /// <summary>
    /// Logs a tier usage event.
    /// </summary>
    /// <param name="userId">User who triggered the operation.</param>
    /// <param name="operationType">Type of operation ('normalization', 'gl_suggestion', 'dept_suggestion').</param>
    /// <param name="tierUsed">Tier used (1, 2, or 3).</param>
    /// <param name="confidence">Result confidence score.</param>
    /// <param name="responseTimeMs">Processing time in milliseconds.</param>
    /// <param name="cacheHit">Whether result came from cache.</param>
    /// <param name="transactionId">Related transaction ID (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogUsageAsync(
        Guid userId,
        string operationType,
        int tierUsed,
        decimal? confidence = null,
        int? responseTimeMs = null,
        bool cacheHit = false,
        Guid? transactionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets tier usage statistics for a date range.
    /// </summary>
    /// <param name="startDate">Start of date range.</param>
    /// <param name="endDate">End of date range.</param>
    /// <param name="operationType">Optional filter by operation type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tier usage statistics.</returns>
    Task<TierUsageStatsDto> GetStatsAsync(
        DateTime startDate,
        DateTime endDate,
        string? operationType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets vendors with high Tier 3 usage that are candidates for alias creation.
    /// </summary>
    /// <param name="startDate">Start of date range.</param>
    /// <param name="endDate">End of date range.</param>
    /// <param name="minTier3Count">Minimum Tier 3 calls to qualify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of vendor candidates.</returns>
    Task<IReadOnlyList<VendorAliasCandidate>> GetVendorAliasCandidatesAsync(
        DateTime startDate,
        DateTime endDate,
        int minTier3Count = 5,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a vendor that could benefit from creating a vendor alias.
/// </summary>
public record VendorAliasCandidate(
    string Vendor,
    int Tier3Count,
    string Recommendation);
