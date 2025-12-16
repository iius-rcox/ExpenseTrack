using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Represents a detected or manually created business travel period.
/// </summary>
public class TravelPeriod : BaseEntity
{
    /// <summary>
    /// Owner of the travel period (FK to Users).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Start date of travel period.
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// End date of travel period.
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Travel destination (city, airport code, etc.).
    /// </summary>
    public string? Destination { get; set; }

    /// <summary>
    /// How the travel period was created (Flight, Hotel, Manual).
    /// </summary>
    public TravelPeriodSource Source { get; set; }

    /// <summary>
    /// Receipt that triggered this travel period detection.
    /// </summary>
    public Guid? SourceReceiptId { get; set; }

    /// <summary>
    /// Whether AI review is required for complex itineraries.
    /// </summary>
    public bool RequiresAiReview { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Receipt? SourceReceipt { get; set; }
}
