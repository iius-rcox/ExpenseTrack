using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Paginated response DTO for receipt list.
/// </summary>
public class ReceiptListResponseDto
{
    public List<ReceiptSummaryDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

/// <summary>
/// Response DTO for receipt status counts.
/// </summary>
public class ReceiptStatusCountsDto
{
    public Dictionary<string, int> Counts { get; set; } = new();
    public int Total { get; set; }
}
