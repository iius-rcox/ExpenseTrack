namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response DTO for unmatched receipts list.
/// </summary>
public class UnmatchedReceiptsResponseDto
{
    /// <summary>
    /// List of unmatched receipts.
    /// </summary>
    public List<MatchReceiptSummaryDto> Items { get; set; } = new();

    /// <summary>
    /// Total number of unmatched receipts.
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
