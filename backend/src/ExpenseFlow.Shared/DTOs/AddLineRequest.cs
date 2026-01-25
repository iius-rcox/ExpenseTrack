namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request to add a transaction as a new expense line to a report.
/// </summary>
public class AddLineRequest
{
    /// <summary>
    /// ID of the transaction to add to the report.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Optional GL code to assign to the line.
    /// If not provided, AI categorization will be attempted.
    /// </summary>
    public string? GlCode { get; set; }

    /// <summary>
    /// Optional department code to assign to the line.
    /// If not provided, AI categorization will be attempted.
    /// </summary>
    public string? DepartmentCode { get; set; }
}

/// <summary>
/// Transaction available to be added to a report.
/// Extends TransactionSummaryDto with period awareness.
/// </summary>
public class AvailableTransactionDto
{
    /// <summary>
    /// Unique transaction identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Date the transaction occurred.
    /// </summary>
    public DateOnly TransactionDate { get; set; }

    /// <summary>
    /// Parsed/normalized description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Original description from statement.
    /// </summary>
    public string OriginalDescription { get; set; } = string.Empty;

    /// <summary>
    /// Transaction amount (positive = expense).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Whether this transaction has a matched receipt.
    /// </summary>
    public bool HasMatchedReceipt { get; set; }

    /// <summary>
    /// Matched receipt ID (if any).
    /// </summary>
    public Guid? ReceiptId { get; set; }

    /// <summary>
    /// Vendor name (if available).
    /// </summary>
    public string? Vendor { get; set; }

    /// <summary>
    /// True if the transaction date is outside the report period.
    /// Frontend should display a warning for these transactions.
    /// </summary>
    public bool IsOutsidePeriod { get; set; }
}

/// <summary>
/// Response containing transactions available to add to a report.
/// </summary>
public class AvailableTransactionsResponse
{
    /// <summary>
    /// List of available transactions.
    /// </summary>
    public List<AvailableTransactionDto> Transactions { get; set; } = new();

    /// <summary>
    /// Total count of available transactions.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Report period (YYYY-MM) for period comparison.
    /// </summary>
    public string ReportPeriod { get; set; } = string.Empty;
}
