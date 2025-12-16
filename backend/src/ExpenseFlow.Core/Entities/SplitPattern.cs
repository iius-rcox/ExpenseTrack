namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Defines expense allocation rules for vendors requiring split accounting.
/// </summary>
public class SplitPattern : BaseEntity
{
    /// <summary>
    /// Owner of the split pattern (FK to Users).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User-defined name for the pattern.
    /// </summary>
    public string Name { get; set; } = string.Empty;

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

    /// <summary>
    /// Whether this is the default pattern for the vendor.
    /// </summary>
    public bool IsDefault { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public VendorAlias? VendorAlias { get; set; }
}
