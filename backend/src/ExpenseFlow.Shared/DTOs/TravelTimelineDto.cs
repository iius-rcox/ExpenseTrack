namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// A single travel period entry in the timeline.
/// </summary>
public class TravelTimelineEntryDto
{
    /// <summary>
    /// The travel period ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Start date of the travel period.
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// End date of the travel period.
    /// </summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Travel destination (extracted from receipts).
    /// </summary>
    public string? Destination { get; set; }

    /// <summary>
    /// Purpose of travel (if provided).
    /// </summary>
    public string? Purpose { get; set; }

    /// <summary>
    /// Duration in days.
    /// </summary>
    public int DurationDays { get; set; }

    /// <summary>
    /// Total expense amount for this trip.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Number of receipts linked to this period.
    /// </summary>
    public int ReceiptCount { get; set; }

    /// <summary>
    /// Number of transactions linked to this period.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Number of unlinked expenses within the period dates.
    /// </summary>
    public int UnlinkedExpenseCount { get; set; }

    /// <summary>
    /// Source documents that created this period.
    /// </summary>
    public List<TravelSourceDocumentDto> SourceDocuments { get; set; } = new();

    /// <summary>
    /// Expenses within this travel period.
    /// </summary>
    public List<TravelTimelineExpenseDto> Expenses { get; set; } = new();

    /// <summary>
    /// Whether this period requires review (overlapping dates, missing receipts).
    /// </summary>
    public bool RequiresReview { get; set; }

    /// <summary>
    /// Review reason if RequiresReview is true.
    /// </summary>
    public string? ReviewReason { get; set; }
}

/// <summary>
/// Source document that triggered a travel period.
/// </summary>
public class TravelSourceDocumentDto
{
    /// <summary>
    /// Receipt ID.
    /// </summary>
    public Guid ReceiptId { get; set; }

    /// <summary>
    /// Document type (Flight, Hotel, etc.).
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Vendor name.
    /// </summary>
    public string VendorName { get; set; } = string.Empty;

    /// <summary>
    /// Document date.
    /// </summary>
    public DateOnly DocumentDate { get; set; }

    /// <summary>
    /// Document amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Thumbnail URL.
    /// </summary>
    public string? ThumbnailUrl { get; set; }
}

/// <summary>
/// An expense within a travel period timeline.
/// </summary>
public class TravelTimelineExpenseDto
{
    /// <summary>
    /// The expense ID (receipt or transaction).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Whether this is a receipt or transaction.
    /// </summary>
    public string ExpenseType { get; set; } = string.Empty; // "Receipt" or "Transaction"

    /// <summary>
    /// Expense date.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Vendor/Description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Expense amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// GL code assigned (or suggested).
    /// </summary>
    public string? GLCode { get; set; }

    /// <summary>
    /// Whether GL code was auto-suggested (66300 for travel).
    /// </summary>
    public bool GLCodeSuggested { get; set; }

    /// <summary>
    /// Whether this expense is explicitly linked to the travel period.
    /// </summary>
    public bool IsLinked { get; set; }

    /// <summary>
    /// Thumbnail URL for receipts.
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Category from receipt extraction.
    /// </summary>
    public string? Category { get; set; }
}

/// <summary>
/// Travel timeline response with multiple periods.
/// </summary>
public class TravelTimelineResponseDto
{
    /// <summary>
    /// Travel periods in date order.
    /// </summary>
    public List<TravelTimelineEntryDto> Periods { get; set; } = new();

    /// <summary>
    /// Total count of periods.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Total amount across all periods.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Number of periods requiring review.
    /// </summary>
    public int PeriodsPendingReview { get; set; }

    /// <summary>
    /// Total unlinked expenses across all periods.
    /// </summary>
    public int TotalUnlinkedExpenses { get; set; }

    /// <summary>
    /// Summary statistics.
    /// </summary>
    public TravelTimelineSummaryDto Summary { get; set; } = new();
}

/// <summary>
/// Summary statistics for the travel timeline.
/// </summary>
public class TravelTimelineSummaryDto
{
    /// <summary>
    /// Total travel days in the period.
    /// </summary>
    public int TotalTravelDays { get; set; }

    /// <summary>
    /// Number of unique destinations.
    /// </summary>
    public int UniqueDestinations { get; set; }

    /// <summary>
    /// Average trip duration in days.
    /// </summary>
    public decimal AverageTripDuration { get; set; }

    /// <summary>
    /// Average trip cost.
    /// </summary>
    public decimal AverageTripCost { get; set; }

    /// <summary>
    /// Most visited destination.
    /// </summary>
    public string? MostVisitedDestination { get; set; }

    /// <summary>
    /// Total receipts across all periods.
    /// </summary>
    public int TotalReceipts { get; set; }

    /// <summary>
    /// Total transactions across all periods.
    /// </summary>
    public int TotalTransactions { get; set; }
}
