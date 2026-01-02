namespace ExpenseFlow.Shared.Enums;

/// <summary>
/// Status of an expense report.
/// State machine: Draft → Generated → Submitted
/// </summary>
public enum ReportStatus : short
{
    /// <summary>Draft - being edited by user</summary>
    Draft = 0,

    /// <summary>Generated - finalized and locked for editing</summary>
    Generated = 1,

    /// <summary>Submitted - marked as complete for audit trail</summary>
    Submitted = 2

    // Future states:
    // Approved = 3,
    // Exported = 4
}
