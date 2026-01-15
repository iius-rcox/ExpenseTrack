using System.ComponentModel.DataAnnotations;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request to export expense line preview data to Excel or PDF without database persistence.
/// Supports the lightweight editable report workflow where users edit directly in the frontend.
/// </summary>
public class ExportPreviewRequest
{
    /// <summary>
    /// Period in YYYY-MM format (e.g., "2026-01").
    /// Used for filename generation and report header.
    /// </summary>
    [Required(ErrorMessage = "Period is required")]
    [RegularExpression(@"^\d{4}-\d{2}$", ErrorMessage = "Period must be in YYYY-MM format")]
    public string Period { get; set; } = string.Empty;

    /// <summary>
    /// List of expense lines to export (edited by user in frontend).
    /// Minimum 1 line required for valid export.
    /// </summary>
    [Required(ErrorMessage = "Expense lines are required")]
    [MinLength(1, ErrorMessage = "At least 1 expense line is required")]
    public List<ExportLineDto> Lines { get; set; } = new();
}
