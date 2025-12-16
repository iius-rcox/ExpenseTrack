using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Full expense report DTO with all lines.
/// </summary>
public class ExpenseReportDto
{
    public Guid Id { get; set; }
    public string Period { get; set; } = string.Empty;
    public ReportStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public int LineCount { get; set; }
    public int MissingReceiptCount { get; set; }
    public int Tier1HitCount { get; set; }
    public int Tier2HitCount { get; set; }
    public int Tier3HitCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ExpenseLineDto> Lines { get; set; } = new();
}
