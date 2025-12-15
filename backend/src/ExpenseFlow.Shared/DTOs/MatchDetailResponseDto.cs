namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Detailed match response including vendor alias information.
/// </summary>
public class MatchDetailResponseDto : MatchProposalDto
{
    /// <summary>
    /// When the match was confirmed or rejected.
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>
    /// Whether this was a manual match.
    /// </summary>
    public bool IsManualMatch { get; set; }

    /// <summary>
    /// Vendor alias details if matched.
    /// </summary>
    public VendorAliasSummaryDto? VendorAlias { get; set; }
}
