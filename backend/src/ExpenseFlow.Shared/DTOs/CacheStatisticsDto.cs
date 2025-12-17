namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Cache tier usage statistics for cost monitoring.
/// </summary>
public class CacheStatisticsDto
{
    /// <summary>
    /// Count of Tier 1 (cache) hits.
    /// </summary>
    public int Tier1Hits { get; set; }

    /// <summary>
    /// Count of Tier 2 (embedding) hits.
    /// </summary>
    public int Tier2Hits { get; set; }

    /// <summary>
    /// Count of Tier 3 (AI) hits.
    /// </summary>
    public int Tier3Hits { get; set; }

    /// <summary>
    /// Total categorization operations.
    /// </summary>
    public int TotalOperations { get; set; }

    /// <summary>
    /// Percentage of operations resolved by Tier 1.
    /// </summary>
    public decimal Tier1HitRate { get; set; }

    /// <summary>
    /// Percentage of operations resolved by Tier 2.
    /// </summary>
    public decimal Tier2HitRate { get; set; }

    /// <summary>
    /// Percentage of operations resolved by Tier 3.
    /// </summary>
    public decimal Tier3HitRate { get; set; }

    /// <summary>
    /// Projected AI API cost in USD based on usage.
    /// </summary>
    public decimal EstimatedMonthlyCost { get; set; }

    /// <summary>
    /// Average response time across all tiers in milliseconds.
    /// </summary>
    public int AvgResponseTimeMs { get; set; }

    /// <summary>
    /// True if Tier 1 hit rate is below the 50% target threshold.
    /// </summary>
    public bool BelowTarget { get; set; }
}
