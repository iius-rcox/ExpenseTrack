namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response for cache statistics endpoint.
/// </summary>
public class CacheStatisticsResponse
{
    /// <summary>
    /// Statistics period (e.g., "2025-01" or "last30days").
    /// </summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// Overall statistics across all operation types.
    /// </summary>
    public CacheStatisticsDto Overall { get; set; } = new();

    /// <summary>
    /// Statistics grouped by operation type (when groupBy=operation).
    /// Null when groupBy=none.
    /// </summary>
    public List<CacheStatsByOperationDto>? ByOperation { get; set; }
}
