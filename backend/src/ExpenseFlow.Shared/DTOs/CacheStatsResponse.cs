namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response DTO for cache statistics.
/// </summary>
public class CacheStatsResponse
{
    public CacheTableStats DescriptionCache { get; set; } = new();
    public CacheTableStats VendorAliases { get; set; } = new();
    public CacheTableStats StatementFingerprints { get; set; } = new();
    public CacheTableStats ExpenseEmbeddings { get; set; } = new();
    public CacheTableStats SplitPatterns { get; set; } = new();
}

/// <summary>
/// Statistics for a single cache table.
/// </summary>
public class CacheTableStats
{
    public int TotalEntries { get; set; }
    public int TotalHits { get; set; }

    /// <summary>
    /// Hit rate percentage (0-100).
    /// </summary>
    public float? HitRate => TotalEntries > 0 ? (float)TotalHits / TotalEntries * 100 : null;
}
