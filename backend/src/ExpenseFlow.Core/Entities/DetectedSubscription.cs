using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Tracks recurring charges identified through pattern analysis or seed data matching.
/// </summary>
public class DetectedSubscription : BaseEntity
{
    /// <summary>
    /// Owner of the subscription (FK to Users).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Linked vendor alias if matched (optional).
    /// </summary>
    public Guid? VendorAliasId { get; set; }

    /// <summary>
    /// Display name of the subscription vendor.
    /// </summary>
    public string VendorName { get; set; } = string.Empty;

    /// <summary>
    /// Average charge amount across occurrences.
    /// </summary>
    public decimal AverageAmount { get; set; }

    /// <summary>
    /// JSON array of occurrence months (YYYY-MM format).
    /// </summary>
    public string OccurrenceMonths { get; set; } = "[]";

    /// <summary>
    /// Date of most recent charge.
    /// </summary>
    public DateOnly LastSeenDate { get; set; }

    /// <summary>
    /// Expected date of next charge (calculated).
    /// </summary>
    public DateOnly? ExpectedNextDate { get; set; }

    /// <summary>
    /// Current subscription status (Active, Missing, Flagged).
    /// </summary>
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    /// <summary>
    /// How the subscription was detected (PatternMatch, SeedData).
    /// </summary>
    public DetectionSource DetectionSource { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public VendorAlias? VendorAlias { get; set; }
}
