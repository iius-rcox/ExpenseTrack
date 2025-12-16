using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Maps transaction description patterns to canonical vendor names.
/// </summary>
public class VendorAlias : BaseEntity
{
    /// <summary>
    /// Standardized vendor name.
    /// </summary>
    public string CanonicalName { get; set; } = string.Empty;

    /// <summary>
    /// Pattern to match in transaction descriptions.
    /// </summary>
    public string AliasPattern { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable vendor name for UI.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Default GL code for this vendor.
    /// </summary>
    public string? DefaultGLCode { get; set; }

    /// <summary>
    /// Default department for this vendor.
    /// </summary>
    public string? DefaultDepartment { get; set; }

    /// <summary>
    /// Number of times this alias matched.
    /// </summary>
    public int MatchCount { get; set; }

    /// <summary>
    /// Most recent match timestamp.
    /// </summary>
    public DateTime? LastMatchedAt { get; set; }

    /// <summary>
    /// Confidence score (0.00-1.00).
    /// </summary>
    public decimal Confidence { get; set; } = 1.00m;

    /// <summary>
    /// Vendor classification for specialized processing (Airline, Hotel, Subscription).
    /// </summary>
    public VendorCategory Category { get; set; } = VendorCategory.Standard;

    // Navigation properties
    public ICollection<SplitPattern> SplitPatterns { get; set; } = new List<SplitPattern>();
}
