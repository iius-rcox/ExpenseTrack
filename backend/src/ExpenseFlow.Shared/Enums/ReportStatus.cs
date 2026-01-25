namespace ExpenseFlow.Shared.Enums;

/// <summary>
/// Status of an expense report.
/// Typical workflow: Draft → Submitted
/// Alternative workflow: Draft → Generated → Submitted
/// Once Submitted, the report is locked and cannot be edited.
/// </summary>
public enum ReportStatus : short
{
    /// <summary>Draft - being edited by user, can be saved and modified</summary>
    Draft = 0,

    /// <summary>Generated - intermediate state, can still be submitted</summary>
    Generated = 1,

    /// <summary>Submitted - locked, marked as complete for audit trail</summary>
    Submitted = 2

    // Future states:
    // Approved = 3,
    // Exported = 4
}
