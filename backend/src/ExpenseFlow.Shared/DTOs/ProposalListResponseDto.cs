namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response DTO for paginated proposal list.
/// </summary>
public class ProposalListResponseDto
{
    /// <summary>
    /// List of proposed matches.
    /// </summary>
    public List<MatchProposalDto> Items { get; set; } = new();

    /// <summary>
    /// Total number of proposals.
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
