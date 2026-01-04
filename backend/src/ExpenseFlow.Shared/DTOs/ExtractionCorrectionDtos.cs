namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Training feedback metadata for a single field correction.
/// Included in receipt update requests to capture what was changed.
/// </summary>
public class CorrectionMetadataDto
{
    /// <summary>Name of the corrected field (vendor, amount, date, tax, currency, line_item).</summary>
    public string FieldName { get; set; } = null!;

    /// <summary>Original AI-extracted value (JSON-serialized).</summary>
    public string OriginalValue { get; set; } = null!;

    /// <summary>For line_item corrections, the index of the item.</summary>
    public int? LineItemIndex { get; set; }

    /// <summary>For line_item corrections, which field was corrected (description, quantity, unitPrice, totalPrice).</summary>
    public string? LineItemField { get; set; }
}

/// <summary>
/// Summary DTO for extraction correction list views.
/// </summary>
public class ExtractionCorrectionDto
{
    /// <summary>Correction ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Receipt ID that was corrected.</summary>
    public Guid ReceiptId { get; set; }

    /// <summary>User ID who made the correction.</summary>
    public Guid UserId { get; set; }

    /// <summary>Display name of user who made the correction.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Field that was corrected.</summary>
    public string FieldName { get; set; } = null!;

    /// <summary>Original AI-extracted value (JSON-serialized).</summary>
    public string? OriginalValue { get; set; }

    /// <summary>User-corrected value (JSON-serialized).</summary>
    public string? CorrectedValue { get; set; }

    /// <summary>When the correction was submitted.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Detailed DTO for individual extraction correction views.
/// Includes receipt context for review.
/// </summary>
public class ExtractionCorrectionDetailDto : ExtractionCorrectionDto
{
    /// <summary>Current vendor name on the receipt.</summary>
    public string? ReceiptVendor { get; set; }

    /// <summary>Current date on the receipt.</summary>
    public DateOnly? ReceiptDate { get; set; }

    /// <summary>Current amount on the receipt.</summary>
    public decimal? ReceiptAmount { get; set; }
}

/// <summary>
/// Query parameters for filtering extraction corrections.
/// </summary>
public class ExtractionCorrectionQueryParams
{
    /// <summary>Page number (1-based).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Items per page.</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>Filter by field type.</summary>
    public string? FieldName { get; set; }

    /// <summary>Filter corrections from this date.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Filter corrections until this date.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Filter by user who made the correction.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Filter by receipt ID.</summary>
    public Guid? ReceiptId { get; set; }

    /// <summary>Sort field (createdAt, fieldName).</summary>
    public string SortBy { get; set; } = "createdAt";

    /// <summary>Sort direction (asc, desc).</summary>
    public string SortDirection { get; set; } = "desc";
}

/// <summary>
/// Paginated result for extraction corrections.
/// </summary>
public class ExtractionCorrectionPagedResult
{
    /// <summary>List of corrections.</summary>
    public List<ExtractionCorrectionDto> Items { get; set; } = new();

    /// <summary>Current page number.</summary>
    public int Page { get; set; }

    /// <summary>Page size.</summary>
    public int PageSize { get; set; }

    /// <summary>Total count of items matching filters.</summary>
    public int TotalCount { get; set; }

    /// <summary>Total number of pages.</summary>
    public int TotalPages { get; set; }

    /// <summary>Whether there is a next page.</summary>
    public bool HasNextPage { get; set; }

    /// <summary>Whether there is a previous page.</summary>
    public bool HasPreviousPage { get; set; }
}
