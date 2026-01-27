using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response DTO for a recurring allowance.
/// </summary>
public class AllowanceResponse
{
    /// <summary>Unique identifier</summary>
    public Guid Id { get; set; }

    /// <summary>User who owns this allowance</summary>
    public Guid UserId { get; set; }

    /// <summary>Vendor name (e.g., "Verizon", "Comcast")</summary>
    public string VendorName { get; set; } = string.Empty;

    /// <summary>Fixed amount for the allowance</summary>
    public decimal Amount { get; set; }

    /// <summary>How often the allowance recurs</summary>
    public AllowanceFrequency Frequency { get; set; }

    /// <summary>GL account code for categorization</summary>
    public string? GLCode { get; set; }

    /// <summary>GL account name (for display)</summary>
    public string? GLName { get; set; }

    /// <summary>Department code for cost allocation</summary>
    public string? DepartmentCode { get; set; }

    /// <summary>Optional description</summary>
    public string? Description { get; set; }

    /// <summary>Whether this allowance is currently active</summary>
    public bool IsActive { get; set; }

    /// <summary>When the allowance was created</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>When the allowance was last modified</summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// List response for recurring allowances.
/// </summary>
public class AllowanceListResponse
{
    /// <summary>List of allowances</summary>
    public List<AllowanceResponse> Items { get; set; } = new();

    /// <summary>Total number of items</summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Request DTO for creating a recurring allowance.
/// </summary>
public class CreateAllowanceRequest
{
    /// <summary>Vendor name (required)</summary>
    public string VendorName { get; set; } = string.Empty;

    /// <summary>Fixed amount (required, must be positive)</summary>
    public decimal Amount { get; set; }

    /// <summary>Frequency (defaults to Monthly)</summary>
    public AllowanceFrequency Frequency { get; set; } = AllowanceFrequency.Monthly;

    /// <summary>GL account code (optional)</summary>
    public string? GLCode { get; set; }

    /// <summary>Department code (optional)</summary>
    public string? DepartmentCode { get; set; }

    /// <summary>Description or notes (optional)</summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request DTO for updating a recurring allowance.
/// All fields are optional - only provided fields are updated.
/// </summary>
public class UpdateAllowanceRequest
{
    /// <summary>Vendor name (optional)</summary>
    public string? VendorName { get; set; }

    /// <summary>Fixed amount (optional)</summary>
    public decimal? Amount { get; set; }

    /// <summary>Frequency (optional)</summary>
    public AllowanceFrequency? Frequency { get; set; }

    /// <summary>GL account code (optional)</summary>
    public string? GLCode { get; set; }

    /// <summary>Department code (optional)</summary>
    public string? DepartmentCode { get; set; }

    /// <summary>Description or notes (optional)</summary>
    public string? Description { get; set; }
}
