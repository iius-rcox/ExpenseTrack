namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Caches raw-to-normalized description mappings for Tier 1 lookups.
/// </summary>
public class DescriptionCache : BaseEntity
{
    /// <summary>
    /// SHA-256 hash of raw description for O(1) lookup.
    /// </summary>
    public string RawDescriptionHash { get; set; } = string.Empty;

    /// <summary>
    /// Original transaction description.
    /// </summary>
    public string RawDescription { get; set; } = string.Empty;

    /// <summary>
    /// AI-normalized description.
    /// </summary>
    public string NormalizedDescription { get; set; } = string.Empty;

    /// <summary>
    /// Number of cache hits for metrics.
    /// </summary>
    public int HitCount { get; set; }
}
