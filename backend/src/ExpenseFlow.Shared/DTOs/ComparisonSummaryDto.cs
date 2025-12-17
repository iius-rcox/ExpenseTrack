namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Summary totals for month-over-month comparison.
/// </summary>
public class ComparisonSummaryDto
{
    /// <summary>
    /// Total spending in the current period.
    /// </summary>
    public decimal CurrentTotal { get; set; }

    /// <summary>
    /// Total spending in the previous period.
    /// </summary>
    public decimal PreviousTotal { get; set; }

    /// <summary>
    /// Absolute change amount (current - previous).
    /// </summary>
    public decimal Change { get; set; }

    /// <summary>
    /// Percentage change from previous to current period.
    /// </summary>
    public decimal ChangePercent { get; set; }
}
