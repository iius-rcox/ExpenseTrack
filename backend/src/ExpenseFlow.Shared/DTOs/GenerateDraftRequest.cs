using System.ComponentModel.DataAnnotations;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request to generate a new draft expense report for a specific period.
/// </summary>
public class GenerateDraftRequest
{
    /// <summary>
    /// The billing period in YYYY-MM format.
    /// </summary>
    [Required]
    [RegularExpression(@"^\d{4}-(0[1-9]|1[0-2])$", ErrorMessage = "Period must be in YYYY-MM format")]
    public string Period { get; set; } = string.Empty;
}
