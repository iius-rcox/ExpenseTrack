namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Seed data table for immediate subscription recognition without pattern detection.
/// </summary>
public class KnownSubscriptionVendor : BaseEntity
{
    /// <summary>
    /// Pattern to match in transaction descriptions.
    /// </summary>
    public string VendorPattern { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable vendor name for UI.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Subscription category (Software, Cloud, Media, etc.).
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Typical monthly charge amount (for reference).
    /// </summary>
    public decimal? TypicalAmount { get; set; }

    /// <summary>
    /// Whether this vendor pattern is actively used for detection.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
