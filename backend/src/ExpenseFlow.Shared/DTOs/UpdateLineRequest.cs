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

