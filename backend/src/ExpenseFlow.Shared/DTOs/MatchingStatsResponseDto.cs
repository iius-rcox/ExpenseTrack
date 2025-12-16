namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response DTO for matching statistics.
/// </summary>
public class MatchingStatsResponseDto
{
    /// <summary>
    /// Number of confirmed matches.
    /// </summary>
    public int MatchedCount { get; set; }

    /// <summary>
    /// Number of proposed matches awaiting review.
    /// </summary>
    public int ProposedCount { get; set; }

    /// <summary>
    /// Number of receipts without any match.
    /// </summary>
    public int UnmatchedReceiptsCount { get; set; }

    /// <summary>
    /// Number of transactions without any match.
    /// </summary>
    public int UnmatchedTransactionsCount { get; set; }

    /// <summary>
    /// Percentage of receipts auto-matched (0-100).
    /// </summary>
    public decimal AutoMatchRate { get; set; }

    /// <summary>
    /// Average confidence of proposed matches.
    /// </summary>
    public decimal AverageConfidence { get; set; }
}
