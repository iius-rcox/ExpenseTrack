namespace ExpenseFlow.Shared.Enums;

/// <summary>
/// Budget comparison status indicating variance severity.
/// </summary>
public enum BudgetComparisonStatus
{
    /// <summary>
    /// Spending within acceptable range (0-80% of budget).
    /// </summary>
    OnTrack = 0,

    /// <summary>
    /// Approaching budget limit (80-100% of budget).
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Exceeded budget amount (&gt;100% of budget).
    /// </summary>
    OverBudget = 2
}
