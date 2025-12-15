using System.ComponentModel.DataAnnotations;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request DTO for creating a manual match.
/// </summary>
public class ManualMatchRequestDto
{
    /// <summary>
    /// Receipt ID to match.
    /// </summary>
    [Required]
    public Guid ReceiptId { get; set; }

    /// <summary>
    /// Transaction ID to match.
    /// </summary>
    [Required]
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Optional vendor display name.
    /// </summary>
    [StringLength(200, ErrorMessage = "Vendor display name cannot exceed 200 characters")]
    public string? VendorDisplayName { get; set; }

    /// <summary>
    /// Optional default GL code.
    /// </summary>
    [StringLength(50, ErrorMessage = "GL code cannot exceed 50 characters")]
    public string? DefaultGLCode { get; set; }

    /// <summary>
    /// Optional default department.
    /// </summary>
    [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters")]
    public string? DefaultDepartment { get; set; }
}
