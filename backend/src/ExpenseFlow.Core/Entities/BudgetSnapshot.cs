namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Historical monthly snapshot of budget vs actual spending.
/// Created at month-end for trend analysis and historical reporting.
/// </summary>
public class BudgetSnapshot : BaseEntity
{
    /// <summary>
    /// Owner of this snapshot (FK to Users).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Reference to the Vista budget (FK to VistaBudgets).
    /// </summary>
    public Guid VistaBudgetId { get; set; }

    /// <summary>
    /// Snapshot month in YYYY-MM format (e.g., "2025-01").
    /// </summary>
    public string SnapshotMonth { get; set; } = string.Empty;

    /// <summary>
    /// Budget amount at time of snapshot.
    /// Captures historical budget value for audit trail.
    /// </summary>
    public decimal BudgetAmount { get; set; }

    /// <summary>
    /// Actual expenses for the month.
    /// </summary>
    public decimal ActualAmount { get; set; }

    /// <summary>
    /// Cumulative year-to-date budget.
    /// </summary>
    public decimal YtdBudget { get; set; }

    /// <summary>
    /// Cumulative year-to-date actual expenses.
    /// </summary>
    public decimal YtdActual { get; set; }

    /// <summary>
    /// Monthly variance (ActualAmount - BudgetAmount).
    /// </summary>
    public decimal MonthlyVariance { get; set; }

    /// <summary>
    /// Year-to-date variance (YtdActual - YtdBudget).
    /// </summary>
    public decimal YtdVariance { get; set; }

    /// <summary>
    /// Number of transactions in this month.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Whether this snapshot is finalized (month is closed).
    /// Finalized snapshots are immutable.
    /// </summary>
    public bool IsFinalized { get; set; }

    /// <summary>
    /// When the snapshot was created.
    /// </summary>
    public DateTime SnapshotTakenAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public VistaBudget VistaBudget { get; set; } = null!;
}
