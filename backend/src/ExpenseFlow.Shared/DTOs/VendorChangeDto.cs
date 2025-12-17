namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Vendor with significant spending change between periods.
/// </summary>
public class VendorChangeDto
{
    /// <summary>
    /// Vendor name.
    /// </summary>
    public string VendorName { get; set; } = string.Empty;

    /// <summary>
    /// Spending amount in the current period.
    /// </summary>
    public decimal CurrentAmount { get; set; }

    /// <summary>
    /// Spending amount in the previous period.
    /// </summary>
    public decimal PreviousAmount { get; set; }

    /// <summary>
    /// Absolute change amount.
    /// </summary>
    public decimal Change { get; set; }

    /// <summary>
    /// Percentage change from previous to current.
    /// </summary>
    public decimal ChangePercent { get; set; }
}
