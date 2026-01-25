using System.ComponentModel.DataAnnotations;
using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request to update an expense line item.
/// All fields are optional - only provided fields will be updated.
/// </summary>
public class UpdateLineRequest
{
    /// <summary>
    /// Updated GL code (user override).
    /// </summary>
    public string? GlCode { get; set; }

    /// <summary>
    /// Updated department code (user override).
    /// </summary>
    public string? DepartmentCode { get; set; }

    /// <summary>
    /// Justification for missing receipt (when HasReceipt is false).
    /// </summary>
    public MissingReceiptJustification? MissingReceiptJustification { get; set; }

    /// <summary>
    /// Additional note for the missing receipt justification.
    /// Required when MissingReceiptJustification is Other.
    /// </summary>
    public string? JustificationNote { get; set; }

    /// <summary>
    /// Split allocations for this expense line.
    /// When provided, the line becomes a split parent with child allocations.
    /// Pass an empty array to remove existing splits.
    /// </summary>
    public List<SplitAllocationDto>? SplitAllocations { get; set; }
}

/// <summary>
/// DTO for a split allocation within an expense line.
/// </summary>
public class SplitAllocationDto
{
    /// <summary>
    /// GL code for this allocation.
    /// </summary>
    [StringLength(50, ErrorMessage = "GL Code cannot exceed 50 characters")]
    public string? GlCode { get; set; }

    /// <summary>
    /// Department code for this allocation.
    /// </summary>
    [StringLength(50, ErrorMessage = "Department Code cannot exceed 50 characters")]
    public string? DepartmentCode { get; set; }

    /// <summary>
    /// Percentage of the parent line amount (0-100).
    /// </summary>
    [Range(0, 100, ErrorMessage = "Percentage must be between 0 and 100")]
    public decimal Percentage { get; set; }

    /// <summary>
    /// Calculated amount for this allocation.
    /// </summary>
    public decimal? Amount { get; set; }
}
