namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Data for generating a missing receipt placeholder page in PDF.
/// </summary>
public class MissingReceiptPlaceholderDto
{
    /// <summary>
    /// Date of the expense.
    /// </summary>
    public DateOnly ExpenseDate { get; set; }

    /// <summary>
    /// Vendor name for the expense, or "Unknown" if not available.
    /// </summary>
    public string VendorName { get; set; } = "Unknown";

    /// <summary>
    /// Expense amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Normalized expense description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable justification for missing receipt.
    /// </summary>
    public string Justification { get; set; } = string.Empty;

    /// <summary>
    /// Custom note when justification is "Other".
    /// </summary>
    public string? JustificationNote { get; set; }

    /// <summary>
    /// Name of the employee submitting the expense.
    /// </summary>
    public string EmployeeName { get; set; } = string.Empty;

    /// <summary>
    /// Report identifier for reference.
    /// </summary>
    public string ReportId { get; set; } = string.Empty;
}
