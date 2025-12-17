namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Vendor name with associated spending amount.
/// </summary>
public class VendorAmountDto
{
    /// <summary>
    /// Vendor name.
    /// </summary>
    public string VendorName { get; set; } = string.Empty;

    /// <summary>
    /// Spending amount for this vendor.
    /// </summary>
    public decimal Amount { get; set; }
}
