namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Month-over-month spending comparison results.
/// </summary>
public class MonthlyComparisonDto
{
    /// <summary>
    /// Current period in YYYY-MM format.
    /// </summary>
    public string CurrentPeriod { get; set; } = string.Empty;

    /// <summary>
    /// Previous period in YYYY-MM format.
    /// </summary>
    public string PreviousPeriod { get; set; } = string.Empty;

    /// <summary>
    /// Summary totals and change metrics.
    /// </summary>
    public ComparisonSummaryDto Summary { get; set; } = new();

    /// <summary>
    /// Vendors appearing in current period but not in previous.
    /// </summary>
    public List<VendorAmountDto> NewVendors { get; set; } = new();

    /// <summary>
    /// Recurring vendors (2+ consecutive months) missing from current period.
    /// </summary>
    public List<VendorAmountDto> MissingRecurring { get; set; } = new();

    /// <summary>
    /// Vendors with spending change exceeding 50%.
    /// </summary>
    public List<VendorChangeDto> SignificantChanges { get; set; } = new();
}
