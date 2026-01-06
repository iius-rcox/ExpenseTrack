namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Paginated list of report generation jobs.
/// </summary>
public record ReportJobListResponse
{
    public List<ReportJobDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
