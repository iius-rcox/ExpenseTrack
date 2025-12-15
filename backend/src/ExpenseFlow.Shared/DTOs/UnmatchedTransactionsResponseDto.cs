namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response DTO for unmatched transactions list.
/// </summary>
public class UnmatchedTransactionsResponseDto
{
    /// <summary>
    /// List of unmatched transactions.
    /// </summary>
    public List<MatchTransactionSummaryDto> Items { get; set; } = new();

    /// <summary>
    /// Total number of unmatched transactions.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Items per page.
    /// </summary>
    public int PageSize { get; set; }
}
