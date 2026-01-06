namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// How reimbursability was determined for a transaction.
/// </summary>
public enum ReimbursabilitySource
{
    /// <summary>User manually marked as reimbursable.</summary>
    UserOverride = 0,

    /// <summary>AI prediction confirmed by user.</summary>
    AIPrediction = 1
}

/// <summary>
/// Summary of a transaction missing a receipt.
/// Used for list display and widget items.
/// </summary>
public class MissingReceiptSummaryDto
{
    /// <summary>Transaction ID.</summary>
    public Guid TransactionId { get; set; }

    /// <summary>Transaction date.</summary>
    public DateOnly TransactionDate { get; set; }

    /// <summary>Vendor/description from transaction.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Transaction amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>Days since transaction date.</summary>
    public int DaysSinceTransaction { get; set; }

    /// <summary>Optional URL where receipt can be retrieved.</summary>
    public string? ReceiptUrl { get; set; }

    /// <summary>Whether transaction has been dismissed from missing receipts list.</summary>
    public bool IsDismissed { get; set; }

    /// <summary>Source of reimbursability determination.</summary>
    public ReimbursabilitySource Source { get; set; }
}

/// <summary>
/// Paginated list of missing receipts.
/// </summary>
public class MissingReceiptsListResponseDto
{
    /// <summary>List of missing receipt items.</summary>
    public List<MissingReceiptSummaryDto> Items { get; set; } = new();

    /// <summary>Total count matching filters.</summary>
    public int TotalCount { get; set; }

    /// <summary>Current page (1-based).</summary>
    public int Page { get; set; }

    /// <summary>Page size.</summary>
    public int PageSize { get; set; }

    /// <summary>Total number of pages.</summary>
    public int TotalPages { get; set; }
}

/// <summary>
/// Widget summary for missing receipts dashboard card.
/// </summary>
public class MissingReceiptsWidgetDto
{
    /// <summary>Total count of missing receipts.</summary>
    public int TotalCount { get; set; }

    /// <summary>Top 3 most recent missing receipts for quick action.</summary>
    public List<MissingReceiptSummaryDto> RecentItems { get; set; } = new();
}

/// <summary>
/// Request to update receipt URL for a transaction.
/// </summary>
public class UpdateReceiptUrlRequestDto
{
    /// <summary>
    /// URL where receipt can be retrieved.
    /// Pass null or empty string to clear.
    /// </summary>
    public string? ReceiptUrl { get; set; }
}

/// <summary>
/// Request to dismiss or restore a transaction from missing receipts.
/// </summary>
public class DismissReceiptRequestDto
{
    /// <summary>
    /// True to dismiss from missing receipts list.
    /// False or null to restore to the list.
    /// </summary>
    public bool? Dismiss { get; set; }
}
