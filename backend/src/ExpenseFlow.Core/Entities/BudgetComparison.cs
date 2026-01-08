using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Tracks expense vs budget comparisons with variance calculations.
/// Created when comparing actual expenses against Vista job cost budgets.
/// </summary>
public class BudgetComparison : BaseEntity
{
    /// <summary>
    /// Owner of this comparison (FK to Users).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Reference to the Vista budget being compared against (FK to VistaBudgets).
    /// </summary>
    public Guid VistaBudgetId { get; set; }

    /// <summary>
    /// Period start date for expense aggregation.
    /// </summary>
    public DateOnly PeriodStart { get; set; }

    /// <summary>
    /// Period end date for expense aggregation.
    /// </summary>
    public DateOnly PeriodEnd { get; set; }

    /// <summary>
    /// Total budget amount from Vista for this period.
    /// Copied from VistaBudget at comparison time for historical accuracy.
    /// </summary>
    public decimal BudgetAmount { get; set; }

    /// <summary>
    /// Sum of actual expenses in the period for matching job/phase/cost type.
    /// </summary>
    public decimal ActualAmount { get; set; }

    /// <summary>
    /// Variance amount (ActualAmount - BudgetAmount).
    /// Negative = under budget, Positive = over budget.
    /// </summary>
    public decimal VarianceAmount { get; set; }

    /// <summary>
    /// Variance percentage ((ActualAmount - BudgetAmount) / BudgetAmount * 100).
    /// Null if BudgetAmount is zero.
    /// </summary>
    public decimal? VariancePercent { get; set; }

    /// <summary>
    /// Current month's actual expenses (for real-time dashboard).
    /// Updated separately from full period calculations.
    /// </summary>
    public decimal CurrentMonthActual { get; set; }

    /// <summary>
    /// Number of transactions included in ActualAmount.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Comparison status: OnTrack, Warning, OverBudget.
    /// Derived from variance thresholds.
    /// </summary>
    public BudgetComparisonStatus Status { get; set; } = BudgetComparisonStatus.OnTrack;

    /// <summary>
    /// Optional notes or explanations for variance.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// When the comparison was last recalculated.
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public VistaBudget VistaBudget { get; set; } = null!;
}
