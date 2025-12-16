namespace ExpenseFlow.Shared.Enums;

/// <summary>
/// Status of an expense report.
/// </summary>
public enum ReportStatus : short
{
    /// <summary>Draft - being edited by user</summary>
    Draft = 0

    // Future states (Sprint 9+):
    // Submitted = 1,
    // Approved = 2,
    // Exported = 3
}
