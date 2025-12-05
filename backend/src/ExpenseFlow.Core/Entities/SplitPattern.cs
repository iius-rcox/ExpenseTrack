namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Defines expense allocation rules for vendors requiring split accounting.
/// </summary>
public class SplitPattern : BaseEntity
{
    /// <summary>
    /// Associated vendor alias (optional).
    /// </summary>
    public Guid? VendorAliasId { get; set; }

    /// <summary>
    /// Split allocation configuration (JSON).
    /// </summary>
    public string SplitConfig { get; set; } = "{}";

    /// <summary>
    /// Number of times this pattern was used.
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Most recent usage timestamp.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    // Navigation properties
    public VendorAlias? VendorAlias { get; set; }
}
