using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Summary DTO for receipt list views.
/// </summary>
public class ReceiptSummaryDto
{
    public Guid Id { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string OriginalFilename { get; set; } = string.Empty;
    public ReceiptStatus Status { get; set; }
    public string? Vendor { get; set; }
    public DateTime? Date { get; set; }
    public decimal? Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAt { get; set; }
}
