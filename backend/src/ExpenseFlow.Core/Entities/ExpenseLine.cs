using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Individual expense item within a report.
/// </summary>
public class ExpenseLine : BaseEntity
{
    /// <summary>FK to ExpenseReports</summary>
    public Guid ReportId { get; set; }

    /// <summary>FK to Receipts (if matched)</summary>
    public Guid? ReceiptId { get; set; }

    /// <summary>FK to Transactions</summary>
    public Guid? TransactionId { get; set; }

    /// <summary>Display order in report (1-based)</summary>
    public int LineOrder { get; set; }

    /// <summary>Date of expense</summary>
    public DateOnly ExpenseDate { get; set; }

    /// <summary>Expense amount</summary>
    public decimal Amount { get; set; }

    /// <summary>Raw bank description</summary>
    public string OriginalDescription { get; set; } = string.Empty;

    /// <summary>Human-readable description</summary>
    public string NormalizedDescription { get; set; } = string.Empty;

    /// <summary>Extracted/matched vendor name</summary>
    public string? VendorName { get; set; }

    /// <summary>Selected GL account code</summary>
    public string? GLCode { get; set; }

    /// <summary>System-suggested GL code</summary>
    public string? GLCodeSuggested { get; set; }

    /// <summary>Tier that provided GL suggestion (1, 2, or 3)</summary>
    public int? GLCodeTier { get; set; }

    /// <summary>Source: "VendorAlias", "EmbeddingSimilarity", "AIInference"</summary>
    public string? GLCodeSource { get; set; }

    /// <summary>Selected department code</summary>
    public string? DepartmentCode { get; set; }

    /// <summary>System-suggested department</summary>
    public string? DepartmentSuggested { get; set; }

    /// <summary>Tier that provided department suggestion (1, 2, or 3)</summary>
    public int? DepartmentTier { get; set; }

    /// <summary>Source description for department</summary>
    public string? DepartmentSource { get; set; }

    /// <summary>True if receipt linked</summary>
    public bool HasReceipt { get; set; }

    /// <summary>Justification if no receipt</summary>
    public MissingReceiptJustification? MissingReceiptJustification { get; set; }

    /// <summary>Custom note for "Other" justification</summary>
    public string? JustificationNote { get; set; }

    /// <summary>True if user modified categorization</summary>
    public bool IsUserEdited { get; set; }

    /// <summary>
    /// True if this line was auto-suggested by expense prediction (Feature 023).
    /// Auto-suggested lines were pre-selected based on high-confidence patterns
    /// learned from previous expense reports.
    /// </summary>
    public bool IsAutoSuggested { get; set; }

    /// <summary>
    /// The prediction ID that suggested this line (nullable).
    /// Allows tracking which prediction led to auto-suggestion.
    /// </summary>
    public Guid? PredictionId { get; set; }

    /// <summary>Last modification timestamp</summary>
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ExpenseReport Report { get; set; } = null!;
    public Receipt? Receipt { get; set; }
    public Transaction? Transaction { get; set; }
}
