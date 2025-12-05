using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Detailed DTO for single receipt view.
/// </summary>
public class ReceiptDetailDto : ReceiptSummaryDto
{
    public string BlobUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public decimal? Tax { get; set; }
    public List<LineItemDto> LineItems { get; set; } = new();
    public Dictionary<string, double> ConfidenceScores { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int PageCount { get; set; } = 1;
    public DateTime? ProcessedAt { get; set; }
}

/// <summary>
/// DTO for receipt line items.
/// </summary>
public class LineItemDto
{
    public string Description { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? TotalPrice { get; set; }
    public double? Confidence { get; set; }
}
