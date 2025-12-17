namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Paginated response for listing expense reports.
/// </summary>
public class ReportListResponse
{
    /// <summary>
    /// List of report summaries for the current page.
    /// </summary>
    public List<ReportSummaryDto> Items { get; set; } = new();

    /// <summary>
    /// Total number of reports matching the filter criteria.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages available.
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>
    /// Whether there are more pages after the current one.
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Whether there are pages before the current one.
    /// </summary>
    public bool HasPreviousPage => Page > 1;
}
