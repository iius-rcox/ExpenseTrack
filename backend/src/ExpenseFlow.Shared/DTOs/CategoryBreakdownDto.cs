namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Category spending breakdown for a period.
/// </summary>
public record CategoryBreakdownDto
{
    /// <summary>
    /// Period in YYYY-MM format.
    /// </summary>
    public required string Period { get; init; }

    /// <summary>
    /// Total spending amount for the period.
    /// </summary>
    public required decimal TotalSpending { get; init; }

    /// <summary>
    /// Number of transactions in the period.
    /// </summary>
    public required int TransactionCount { get; init; }

    /// <summary>
    /// Breakdown by category.
    /// </summary>
    public required List<CategorySpendingDto> Categories { get; init; }
}

/// <summary>
/// Spending summary for a single category.
/// </summary>
public record CategorySpendingDto
{
    /// <summary>
    /// Category name (e.g., "Food & Dining", "Transportation").
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Total amount spent in this category.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Percentage of total spending.
    /// </summary>
    public required decimal Percentage { get; init; }

    /// <summary>
    /// Number of transactions in this category.
    /// </summary>
    public required int TransactionCount { get; init; }
}
