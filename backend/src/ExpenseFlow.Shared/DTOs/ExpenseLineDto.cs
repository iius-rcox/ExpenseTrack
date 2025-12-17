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
    public int LineOrder { get; set; }
    public DateOnly ExpenseDate { get; set; }
    public decimal Amount { get; set; }
    public string OriginalDescription { get; set; } = string.Empty;
    public string NormalizedDescription { get; set; } = string.Empty;
    public string? VendorName { get; set; }

    // GL Code with tiered categorization info
    public string? GlCode { get; set; }
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
}
