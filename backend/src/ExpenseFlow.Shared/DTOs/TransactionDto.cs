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

    /// <summary>
    /// Matched receipt details (null if not matched).
    /// </summary>
    public MatchedReceiptInfoDto? MatchedReceipt { get; set; }

    /// <summary>
    /// Whether this transaction has a matched receipt.
    /// </summary>
    public bool HasMatchedReceipt => MatchedReceipt != null;
}

/// <summary>
/// Information about a matched receipt.
/// </summary>
public class MatchedReceiptInfoDto
{
    /// <summary>
    /// The match record ID (needed for unmatch operations).
    /// </summary>
    public Guid MatchId { get; set; }

    /// <summary>
    /// The receipt ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Vendor name extracted from receipt.
    /// </summary>
    public string? Vendor { get; set; }

    /// <summary>
    /// Date extracted from receipt.
    /// </summary>
    public DateOnly? Date { get; set; }

    /// <summary>
    /// Amount extracted from receipt.
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Thumbnail URL for receipt preview.
    /// </summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Match confidence score (0-1).
    /// </summary>
    public decimal MatchConfidence { get; set; }
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

/// <summary>
/// Category for transaction filtering.
/// </summary>
public class CategoryDto
{
    /// <summary>
    /// Unique identifier for the category (kebab-case).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the category.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Response containing available categories for transaction filtering.
/// </summary>
public class TransactionCategoriesResponse
{
    /// <summary>
    /// List of available categories.
    /// </summary>
    public List<CategoryDto> Categories { get; set; } = new();
}

/// <summary>
/// Response containing available tags for transaction filtering.
/// </summary>
public class TransactionTagsResponse
{
    /// <summary>
    /// List of available tags (currently not implemented).
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// A suggested filter based on transaction data analysis.
/// </summary>
public class FilterSuggestionDto
{
    /// <summary>
    /// Type of suggestion (e.g., "category", "date_range", "merchant", "amount_range").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable label for the suggestion.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Brief description explaining the suggestion.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The filter value to apply (JSON-serializable).
    /// For categories: category ID string.
    /// For date ranges: object with startDate and endDate.
    /// For merchants: search string.
    /// For amount ranges: object with min and max.
    /// </summary>
    public object? FilterValue { get; set; }

    /// <summary>
    /// Number of transactions that match this filter suggestion.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Relevance score (0-100, higher = more relevant suggestion).
    /// </summary>
    public int RelevanceScore { get; set; }
}

/// <summary>
/// Response containing smart filter suggestions based on transaction data.
/// </summary>
public class FilterSuggestionsResponse
{
    /// <summary>
    /// List of suggested filters.
    /// </summary>
    public List<FilterSuggestionDto> Suggestions { get; set; } = new();

    /// <summary>
    /// Total number of transactions analyzed.
    /// </summary>
    public int TotalTransactions { get; set; }

    /// <summary>
    /// Date range of the transactions analyzed.
    /// </summary>
    public DateOnly? EarliestDate { get; set; }

    /// <summary>
    /// Most recent transaction date.
    /// </summary>
    public DateOnly? LatestDate { get; set; }
}
