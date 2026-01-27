using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// DTO representing a single expense line item.
/// </summary>
public class ExpenseLineDto
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public Guid? ReceiptId { get; set; }
    public Guid? TransactionId { get; set; }
    public Guid? AllowanceId { get; set; }
    public int LineOrder { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public decimal Amount { get; set; }
    public string OriginalDescription { get; set; } = string.Empty;
    public string NormalizedDescription { get; set; } = string.Empty;
    public string? VendorName { get; set; }

    // GL Code with tiered categorization info
    public string? GlCode { get; set; }
    public string? GlName { get; set; } // Vista account description (e.g., "Meals & Entertainment")
    public string? GlCodeSuggested { get; set; }
    public int? GlCodeTier { get; set; }
    public string? GlCodeSource { get; set; }

    // Department with tiered categorization info
    public string? DepartmentCode { get; set; }
    public string? DepartmentSuggested { get; set; }
    public int? DepartmentTier { get; set; }
    public string? DepartmentSource { get; set; }

    // Receipt tracking
    public bool HasReceipt { get; set; }
    public MissingReceiptJustification? MissingReceiptJustification { get; set; }
    public string? JustificationNote { get; set; }

    // Edit tracking
    public bool IsUserEdited { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Auto-suggestion tracking (Feature 023 - Expense Prediction)
    /// <summary>
    /// True if this line was auto-suggested by expense prediction.
    /// UI should show "Auto-suggested" indicator for these lines.
    /// </summary>
    public bool IsAutoSuggested { get; set; }

    /// <summary>
    /// The prediction ID that suggested this line (nullable).
    /// </summary>
    public Guid? PredictionId { get; set; }

    // Allowance tracking (Feature 032 - Recurring Allowances)
    /// <summary>
    /// True if this line is from a recurring allowance (e.g., phone, internet).
    /// Computed from AllowanceId.
    /// </summary>
    public bool IsAllowance => AllowanceId.HasValue;

    // Split allocation support
    /// <summary>
    /// True if this line has been split into multiple allocations.
    /// When true, ChildAllocations contains the breakdown.
    /// </summary>
    public bool IsSplitParent { get; set; }

    /// <summary>
    /// Child allocations for split expenses.
    /// Only populated when IsSplitParent is true.
    /// </summary>
    public List<SplitAllocationLineDto>? ChildAllocations { get; set; }
}

/// <summary>
/// DTO representing a child allocation of a split expense line.
/// </summary>
public class SplitAllocationLineDto
{
    public Guid Id { get; set; }
    public string? GlCode { get; set; }
    public string? DepartmentCode { get; set; }
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }
    public int AllocationOrder { get; set; }
}
