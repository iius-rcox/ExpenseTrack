using System.ComponentModel.DataAnnotations;
using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request to batch update multiple expense lines in a report.
/// Used by the Save button to persist all dirty lines at once.
/// </summary>
public class BatchUpdateLinesRequest
{
    /// <summary>
    /// Maximum number of lines that can be updated in a single batch.
    /// </summary>
    public const int MaxBatchSize = 100;

    /// <summary>
    /// List of line updates to apply.
    /// </summary>
    [Required(ErrorMessage = "Lines collection is required")]
    [MinLength(1, ErrorMessage = "At least one line must be provided")]
    [MaxLength(MaxBatchSize, ErrorMessage = "Cannot update more than 100 lines in a single request")]
    public List<BatchLineUpdate> Lines { get; set; } = new();
}

/// <summary>
/// Update for a single expense line in a batch operation.
/// All fields except LineId are optional - only provided fields will be updated.
/// </summary>
public class BatchLineUpdate
{
    /// <summary>
    /// ID of the expense line to update.
    /// </summary>
    [Required]
    public Guid LineId { get; set; }

    /// <summary>
    /// Updated GL code (user override).
    /// </summary>
    [StringLength(50, ErrorMessage = "GL Code cannot exceed 50 characters")]
    public string? GlCode { get; set; }

    /// <summary>
    /// Updated department code (user override).
    /// </summary>
    [StringLength(50, ErrorMessage = "Department Code cannot exceed 50 characters")]
    public string? DepartmentCode { get; set; }

    /// <summary>
    /// Justification for missing receipt (when HasReceipt is false).
    /// </summary>
    public MissingReceiptJustification? MissingReceiptJustification { get; set; }

    /// <summary>
    /// Additional note for the missing receipt justification.
    /// Required when MissingReceiptJustification is Other.
    /// </summary>
    [StringLength(500, ErrorMessage = "Justification note cannot exceed 500 characters")]
    public string? JustificationNote { get; set; }
}

/// <summary>
/// Response from a batch update lines operation.
/// </summary>
public class BatchUpdateLinesResponse
{
    /// <summary>
    /// ID of the report that was updated.
    /// </summary>
    public Guid ReportId { get; set; }

    /// <summary>
    /// Number of lines successfully updated.
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    /// Number of lines that failed to update.
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Timestamp when the update was completed.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Current report status after the save operation.
    /// IMPORTANT: Should always be "Draft" for save operations.
    /// </summary>
    public string? ReportStatus { get; set; }

    /// <summary>
    /// Details of any lines that failed to update.
    /// </summary>
    public List<FailedLineUpdate> FailedLines { get; set; } = new();
}

/// <summary>
/// Details of a failed line update in a batch operation.
/// </summary>
public class FailedLineUpdate
{
    /// <summary>
    /// ID of the line that failed to update.
    /// </summary>
    public Guid LineId { get; set; }

    /// <summary>
    /// Error message describing why the update failed.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}
