using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Vista budget data for API responses.
/// </summary>
public record VistaBudgetDto
{
    /// <summary>
    /// Unique budget record identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Job Cost Company from Vista.
    /// </summary>
    public int JCCo { get; init; }

    /// <summary>
    /// Vista job/contract code.
    /// </summary>
    public required string Job { get; init; }

    /// <summary>
    /// Phase group code for cost categorization.
    /// </summary>
    public required string PhaseCode { get; init; }

    /// <summary>
    /// Cost type code (1=Labor, 2=Material, 3=Equipment, 6=Subcontract).
    /// </summary>
    public required string CostType { get; init; }

    /// <summary>
    /// Total budget amount.
    /// </summary>
    public decimal BudgetAmount { get; init; }

    /// <summary>
    /// Fiscal year for budget.
    /// </summary>
    public int FiscalYear { get; init; }

    /// <summary>
    /// Job description from Vista.
    /// </summary>
    public string? JobDescription { get; init; }

    /// <summary>
    /// Whether the budget is active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Last sync timestamp.
    /// </summary>
    public DateTime SyncedAt { get; init; }
}

/// <summary>
/// Summary view of Vista budget for list displays.
/// </summary>
public record VistaBudgetSummaryDto
{
    /// <summary>
    /// Budget record identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Combined display: "Job - Phase (CostType)".
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Job description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Budget amount.
    /// </summary>
    public decimal BudgetAmount { get; init; }

    /// <summary>
    /// Fiscal year.
    /// </summary>
    public int FiscalYear { get; init; }
}

/// <summary>
/// Result of a Vista budget sync operation.
/// </summary>
public record VistaBudgetSyncResultDto
{
    /// <summary>
    /// Total budgets retrieved from Vista.
    /// </summary>
    public int TotalRetrieved { get; init; }

    /// <summary>
    /// New budgets inserted.
    /// </summary>
    public int Inserted { get; init; }

    /// <summary>
    /// Existing budgets updated.
    /// </summary>
    public int Updated { get; init; }

    /// <summary>
    /// Budgets marked inactive (no longer in Vista).
    /// </summary>
    public int Deactivated { get; init; }

    /// <summary>
    /// Records that failed to process.
    /// </summary>
    public int Failed { get; init; }

    /// <summary>
    /// Sync duration in milliseconds.
    /// </summary>
    public long DurationMs { get; init; }

    /// <summary>
    /// When sync completed.
    /// </summary>
    public DateTime SyncedAt { get; init; }

    /// <summary>
    /// Error messages if any failures occurred.
    /// </summary>
    public List<string>? Errors { get; init; }
}

/// <summary>
/// Budget comparison data for dashboard displays.
/// </summary>
public record BudgetComparisonDto
{
    /// <summary>
    /// Comparison record identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Vista budget identifier.
    /// </summary>
    public Guid VistaBudgetId { get; init; }

    /// <summary>
    /// Job code from Vista.
    /// </summary>
    public required string Job { get; init; }

    /// <summary>
    /// Phase code.
    /// </summary>
    public required string PhaseCode { get; init; }

    /// <summary>
    /// Cost type code.
    /// </summary>
    public required string CostType { get; init; }

    /// <summary>
    /// Job description.
    /// </summary>
    public string? JobDescription { get; init; }

    /// <summary>
    /// Period start date.
    /// </summary>
    public DateOnly PeriodStart { get; init; }

    /// <summary>
    /// Period end date.
    /// </summary>
    public DateOnly PeriodEnd { get; init; }

    /// <summary>
    /// Budget amount for period.
    /// </summary>
    public decimal BudgetAmount { get; init; }

    /// <summary>
    /// Actual expenses for period.
    /// </summary>
    public decimal ActualAmount { get; init; }

    /// <summary>
    /// Variance (Actual - Budget).
    /// </summary>
    public decimal VarianceAmount { get; init; }

    /// <summary>
    /// Variance percentage.
    /// </summary>
    public decimal? VariancePercent { get; init; }

    /// <summary>
    /// Current month actual (for real-time display).
    /// </summary>
    public decimal CurrentMonthActual { get; init; }

    /// <summary>
    /// Number of transactions.
    /// </summary>
    public int TransactionCount { get; init; }

    /// <summary>
    /// Status: OnTrack, Warning, OverBudget.
    /// </summary>
    public BudgetComparisonStatus Status { get; init; }

    /// <summary>
    /// When comparison was calculated.
    /// </summary>
    public DateTime CalculatedAt { get; init; }
}

/// <summary>
/// Variance detail for budget analysis.
/// </summary>
public record BudgetVarianceDto
{
    /// <summary>
    /// Job code.
    /// </summary>
    public required string Job { get; init; }

    /// <summary>
    /// Phase code.
    /// </summary>
    public required string PhaseCode { get; init; }

    /// <summary>
    /// Cost type.
    /// </summary>
    public required string CostType { get; init; }

    /// <summary>
    /// Budget amount.
    /// </summary>
    public decimal Budget { get; init; }

    /// <summary>
    /// Actual spent.
    /// </summary>
    public decimal Actual { get; init; }

    /// <summary>
    /// Variance amount.
    /// </summary>
    public decimal Variance { get; init; }

    /// <summary>
    /// Variance percentage.
    /// </summary>
    public decimal? VariancePercent { get; init; }

    /// <summary>
    /// Remaining budget (Budget - Actual).
    /// </summary>
    public decimal Remaining { get; init; }

    /// <summary>
    /// Percentage of budget consumed.
    /// </summary>
    public decimal? PercentUsed { get; init; }

    /// <summary>
    /// Status indicator.
    /// </summary>
    public BudgetComparisonStatus Status { get; init; }

    /// <summary>
    /// Trend direction: "increasing", "decreasing", "stable".
    /// </summary>
    public string? Trend { get; init; }
}

/// <summary>
/// Monthly snapshot data for trend analysis.
/// </summary>
public record BudgetSnapshotDto
{
    /// <summary>
    /// Snapshot identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Month in YYYY-MM format.
    /// </summary>
    public required string Month { get; init; }

    /// <summary>
    /// Budget amount.
    /// </summary>
    public decimal BudgetAmount { get; init; }

    /// <summary>
    /// Actual amount.
    /// </summary>
    public decimal ActualAmount { get; init; }

    /// <summary>
    /// Year-to-date budget.
    /// </summary>
    public decimal YtdBudget { get; init; }

    /// <summary>
    /// Year-to-date actual.
    /// </summary>
    public decimal YtdActual { get; init; }

    /// <summary>
    /// Monthly variance.
    /// </summary>
    public decimal MonthlyVariance { get; init; }

    /// <summary>
    /// YTD variance.
    /// </summary>
    public decimal YtdVariance { get; init; }

    /// <summary>
    /// Transaction count.
    /// </summary>
    public int TransactionCount { get; init; }

    /// <summary>
    /// Whether snapshot is finalized.
    /// </summary>
    public bool IsFinalized { get; init; }
}

/// <summary>
/// Paginated list of budget comparisons.
/// </summary>
public record BudgetComparisonListResponse
{
    /// <summary>
    /// Budget comparisons.
    /// </summary>
    public required List<BudgetComparisonDto> Comparisons { get; init; }

    /// <summary>
    /// Total count matching filters.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Current page.
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Summary statistics.
    /// </summary>
    public required BudgetSummaryDto Summary { get; init; }
}

/// <summary>
/// Summary statistics for budget overview.
/// </summary>
public record BudgetSummaryDto
{
    /// <summary>
    /// Total budget across all jobs.
    /// </summary>
    public decimal TotalBudget { get; init; }

    /// <summary>
    /// Total actual expenses.
    /// </summary>
    public decimal TotalActual { get; init; }

    /// <summary>
    /// Total variance.
    /// </summary>
    public decimal TotalVariance { get; init; }

    /// <summary>
    /// Count on track.
    /// </summary>
    public int OnTrackCount { get; init; }

    /// <summary>
    /// Count with warnings.
    /// </summary>
    public int WarningCount { get; init; }

    /// <summary>
    /// Count over budget.
    /// </summary>
    public int OverBudgetCount { get; init; }

    /// <summary>
    /// Last sync timestamp.
    /// </summary>
    public DateTime? LastSyncedAt { get; init; }
}

/// <summary>
/// Trend data for budget visualization.
/// </summary>
public record BudgetTrendDto
{
    /// <summary>
    /// Job identifier.
    /// </summary>
    public required string Job { get; init; }

    /// <summary>
    /// Phase code.
    /// </summary>
    public required string PhaseCode { get; init; }

    /// <summary>
    /// Monthly snapshots for trend line.
    /// </summary>
    public required List<BudgetSnapshotDto> Snapshots { get; init; }

    /// <summary>
    /// Forecast for remaining months (optional).
    /// </summary>
    public List<BudgetForecastPointDto>? Forecast { get; init; }
}

/// <summary>
/// Single point in budget forecast.
/// </summary>
public record BudgetForecastPointDto
{
    /// <summary>
    /// Month in YYYY-MM format.
    /// </summary>
    public required string Month { get; init; }

    /// <summary>
    /// Forecasted amount.
    /// </summary>
    public decimal ForecastAmount { get; init; }

    /// <summary>
    /// Confidence level (0.0-1.0).
    /// </summary>
    public decimal Confidence { get; init; }
}
