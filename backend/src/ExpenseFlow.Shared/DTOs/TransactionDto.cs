namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Summary view of a transaction for list displays.
/// </summary>
public class TransactionSummaryDto
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
    /// Transaction amount (positive = expense).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Whether this transaction has a matched receipt.
    /// </summary>
    public bool HasMatchedReceipt { get; set; }

    /// <summary>
    /// ID of the transaction group this transaction belongs to (nullable if ungrouped).
    /// </summary>
    public Guid? GroupId { get; set; }

    /// <summary>
    /// Prediction data for expense reimbursability (nullable if no prediction exists).
    /// </summary>
    public PredictionSummaryDto? Prediction { get; set; }
}

/// <summary>
/// Detailed view of a transaction.
/// </summary>
public class TransactionDetailDto
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
    /// Date the transaction posted (nullable).
    /// </summary>
    public DateOnly? PostDate { get; set; }

    /// <summary>
    /// Parsed/normalized description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Raw description from statement.
    /// </summary>
    public string OriginalDescription { get; set; } = string.Empty;

    /// <summary>
    /// Transaction amount (positive = expense).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// ID of matched receipt (nullable).
    /// </summary>
    public Guid? MatchedReceiptId { get; set; }

    /// <summary>
    /// ID of the import batch.
    /// </summary>
    public Guid ImportId { get; set; }

    /// <summary>
    /// Original filename from import.
    /// </summary>
    public string ImportFileName { get; set; } = string.Empty;

    /// <summary>
    /// When the transaction was imported.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Paginated list of transactions.
/// </summary>
public class TransactionListResponse
{
    /// <summary>
    /// List of transactions.
    /// </summary>
    public List<TransactionSummaryDto> Transactions { get; set; } = new();

    /// <summary>
    /// Total count of transactions matching filters.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total transactions without matched receipts.
    /// </summary>
    public int UnmatchedCount { get; set; }
}
