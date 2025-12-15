using System.ComponentModel.DataAnnotations;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request DTO for confirming a match.
/// </summary>
public class ConfirmMatchRequestDto
{
    /// <summary>
    /// Override vendor display name for new alias.
    /// </summary>
    [StringLength(200, ErrorMessage = "Vendor display name cannot exceed 200 characters")]
    public string? VendorDisplayName { get; set; }

    /// <summary>
    /// Set default GL code for vendor alias.
    /// </summary>
    [StringLength(50, ErrorMessage = "GL code cannot exceed 50 characters")]
    public string? DefaultGLCode { get; set; }

    /// <summary>
    /// Set default department for vendor alias.
    /// </summary>
    [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters")]
    public string? DefaultDepartment { get; set; }
}
