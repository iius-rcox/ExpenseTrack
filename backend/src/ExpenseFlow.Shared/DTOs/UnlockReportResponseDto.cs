using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response from unlocking a submitted report back to Draft status.
/// </summary>
public class UnlockReportResponseDto
{
    /// <summary>Report ID</summary>
    public Guid ReportId { get; set; }

    /// <summary>New report status (Draft)</summary>
    public string Status { get; set; } = ReportStatus.Draft.ToString();

    /// <summary>Timestamp when the report was unlocked</summary>
    public DateTimeOffset UnlockedAt { get; set; }

    /// <summary>Previous status before unlocking</summary>
    public string PreviousStatus { get; set; } = string.Empty;
}
