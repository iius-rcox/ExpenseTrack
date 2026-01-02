using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response after successfully submitting a report.
/// </summary>
public class SubmitReportResponseDto
{
    /// <summary>Report ID</summary>
    public Guid ReportId { get; set; }

    /// <summary>New report status (Submitted)</summary>
    public string Status { get; set; } = ReportStatus.Submitted.ToString();

    /// <summary>Timestamp when the report was submitted</summary>
    public DateTimeOffset SubmittedAt { get; set; }
}
