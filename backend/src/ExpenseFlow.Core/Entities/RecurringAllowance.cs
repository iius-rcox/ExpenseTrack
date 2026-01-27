using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Represents a recurring expense allowance (e.g., phone, internet) that
/// doesn't appear as card transactions but needs to be added to expense reports.
/// </summary>
public class RecurringAllowance : BaseEntity
{
    /// <summary>
    /// FK to Users - the employee who owns this allowance.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Vendor name for the allowance (e.g., "Verizon", "Comcast").
    /// </summary>
    public string VendorName { get; set; } = string.Empty;

    /// <summary>
    /// Fixed amount for the allowance.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// How often the allowance recurs.
    /// </summary>
    public AllowanceFrequency Frequency { get; set; } = AllowanceFrequency.Monthly;

    /// <summary>
    /// GL account code for expense categorization.
    /// </summary>
    public string? GLCode { get; set; }

    /// <summary>
    /// GL account name (denormalized from GLAccounts for display).
    /// </summary>
    public string? GLName { get; set; }

    /// <summary>
    /// Department code for cost allocation.
    /// </summary>
    public string? DepartmentCode { get; set; }

    /// <summary>
    /// Optional description or notes about the allowance.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this allowance is currently active.
    /// Soft delete sets this to false to preserve history.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// PostgreSQL xmin for optimistic concurrency control.
    /// </summary>
    public uint RowVersion { get; set; }

    // Navigation properties

    /// <summary>
    /// The user who owns this allowance.
    /// </summary>
    public User User { get; set; } = null!;
}
