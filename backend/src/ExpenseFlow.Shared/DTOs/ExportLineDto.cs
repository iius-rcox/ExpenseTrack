using System.ComponentModel.DataAnnotations;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Minimal expense line data for stateless export operations.
/// Contains only the fields needed for Excel/PDF generation without database persistence.
/// </summary>
public class ExportLineDto
{
    /// <summary>
    /// Date of the expense.
    /// </summary>
    [Required]
    public DateOnly ExpenseDate { get; set; }

    /// <summary>
    /// Vendor/merchant name.
    /// </summary>
    [MaxLength(255, ErrorMessage = "Vendor name cannot exceed 255 characters")]
    public string VendorName { get; set; } = string.Empty;

    /// <summary>
    /// GL account code (expense category).
    /// Example: "63300" (Meals & Entertainment), "66300" (Travel)
    /// </summary>
    [MaxLength(10, ErrorMessage = "GL code cannot exceed 10 characters")]
    public string GlCode { get; set; } = string.Empty;

    /// <summary>
    /// Department or phase code.
    /// Example: "07" (Field Operations)
    /// </summary>
    [MaxLength(10, ErrorMessage = "Department code cannot exceed 10 characters")]
    public string DepartmentCode { get; set; } = string.Empty;

    /// <summary>
    /// Expense description (user-editable).
    /// </summary>
    [Required(ErrorMessage = "Description is required")]
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this expense has an attached receipt.
    /// </summary>
    public bool HasReceipt { get; set; }

    /// <summary>
    /// Expense amount in dollars.
    /// </summary>
    [Required]
    [Range(0.01, 1000000, ErrorMessage = "Amount must be between $0.01 and $1,000,000")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Units for calculation (always 1 for MVP, future: mileage tracking).
    /// </summary>
    public int Units => 1;
}
