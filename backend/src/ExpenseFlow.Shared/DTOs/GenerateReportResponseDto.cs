using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response after successfully generating (finalizing) a report.
/// </summary>
public class GenerateReportResponseDto
{
    /// <summary>Report ID</summary>
    public Guid ReportId { get; set; }

    /// <summary>New report status (Generated)</summary>
    public string Status { get; set; } = ReportStatus.Generated.ToString();

    /// <summary>Timestamp when the report was finalized</summary>
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>Number of expense lines in the report</summary>
    public int LineCount { get; set; }

    /// <summary>Total amount of all expense lines</summary>
    public decimal TotalAmount { get; set; }
}
