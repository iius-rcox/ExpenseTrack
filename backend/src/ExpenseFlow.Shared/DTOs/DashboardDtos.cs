namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response DTO for dashboard metrics.
/// </summary>
public class DashboardMetricsDto
{
    /// <summary>
    /// Number of receipts pending processing.
    /// </summary>
    public int PendingReceiptsCount { get; set; }

    /// <summary>
    /// Number of transactions without a matched receipt.
    /// </summary>
    public int UnmatchedTransactionsCount { get; set; }

    /// <summary>
    /// Number of match proposals awaiting review.
    /// </summary>
    public int PendingMatchesCount { get; set; }

    /// <summary>
    /// Number of expense reports in draft status.
    /// </summary>
    public int DraftReportsCount { get; set; }

    /// <summary>
    /// Monthly spending statistics.
    /// </summary>
    public MonthlySpendingDto MonthlySpending { get; set; } = new();
}

/// <summary>
/// Monthly spending statistics for the dashboard.
/// </summary>
public class MonthlySpendingDto
{
    /// <summary>
    /// Total spending for the current month.
    /// </summary>
    public decimal CurrentMonth { get; set; }

    /// <summary>
    /// Total spending for the previous month.
    /// </summary>
    public decimal PreviousMonth { get; set; }

    /// <summary>
    /// Percentage change from previous month.
    /// </summary>
    public decimal PercentChange { get; set; }
}

/// <summary>
/// Recent activity item for the dashboard.
/// </summary>
public class RecentActivityItemDto
{
    /// <summary>
    /// Type of activity (receipt_uploaded, statement_imported, match_confirmed, report_generated).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Title/summary of the activity.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the activity.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When the activity occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }
}
