namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request DTO for updating receipt data (manual corrections).
/// Supports optimistic concurrency via RowVersion and training feedback via Corrections.
/// </summary>
public class ReceiptUpdateRequestDto
{
    /// <summary>
    /// Vendor/merchant name.
    /// </summary>
    public string? Vendor { get; set; }

    /// <summary>
    /// Transaction date.
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// Total amount.
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
    /// Tax amount.
    /// </summary>
    public decimal? Tax { get; set; }

    /// <summary>
    /// Currency code (e.g., USD, EUR).
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Line items on the receipt.
    /// </summary>
    public List<LineItemDto>? LineItems { get; set; }

    /// <summary>
    /// Concurrency token for optimistic locking.
    /// Required for concurrent edit detection.
    /// </summary>
    public uint? RowVersion { get; set; }

    /// <summary>
    /// Optional training feedback for each corrected field.
    /// When provided, creates ExtractionCorrection records for model improvement.
    /// </summary>
    public List<CorrectionMetadataDto>? Corrections { get; set; }
}
