namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request DTO for updating receipt data (manual corrections).
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
}
