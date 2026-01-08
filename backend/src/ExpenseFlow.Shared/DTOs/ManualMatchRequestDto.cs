using System.ComponentModel.DataAnnotations;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request DTO for creating a manual match.
/// </summary>
public class ManualMatchRequestDto : IValidatableObject
{
    /// <summary>
    /// Receipt ID to match.
    /// </summary>
    [Required]
    public Guid ReceiptId { get; set; }

    /// <summary>
    /// Transaction ID to match. Required if TransactionGroupId is not provided.
    /// Mutually exclusive with TransactionGroupId.
    /// </summary>
    public Guid? TransactionId { get; set; }

    /// <summary>
    /// Transaction Group ID to match. Required if TransactionId is not provided.
    /// Mutually exclusive with TransactionId.
    /// </summary>
    public Guid? TransactionGroupId { get; set; }

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

    /// <summary>
    /// Validates that exactly one of TransactionId or TransactionGroupId is provided.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasTransactionId = TransactionId.HasValue && TransactionId.Value != Guid.Empty;
        var hasGroupId = TransactionGroupId.HasValue && TransactionGroupId.Value != Guid.Empty;

        if (!hasTransactionId && !hasGroupId)
        {
            yield return new ValidationResult(
                "Either TransactionId or TransactionGroupId must be provided.",
                new[] { nameof(TransactionId), nameof(TransactionGroupId) });
        }
        else if (hasTransactionId && hasGroupId)
        {
            yield return new ValidationResult(
                "TransactionId and TransactionGroupId are mutually exclusive. Provide only one.",
                new[] { nameof(TransactionId), nameof(TransactionGroupId) });
        }
    }
}
