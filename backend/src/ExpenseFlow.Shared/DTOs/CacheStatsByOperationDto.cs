namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Cache statistics for a specific operation type.
/// </summary>
public class CacheStatsByOperationDto
{
    /// <summary>
    /// Operation type ('normalization', 'gl_suggestion', 'dept_suggestion').
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Count of Tier 1 (cache) hits for this operation.
    /// </summary>
    public int Tier1Hits { get; set; }

    /// <summary>
    /// Count of Tier 2 (embedding) hits for this operation.
    /// </summary>
    public int Tier2Hits { get; set; }

    /// <summary>
    /// Count of Tier 3 (AI) hits for this operation.
    /// </summary>
    public int Tier3Hits { get; set; }

    /// <summary>
    /// Percentage of this operation resolved by Tier 1.
    /// </summary>
    public decimal Tier1HitRate { get; set; }
}
