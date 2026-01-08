namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response DTO for auto-match operation.
/// </summary>
public class AutoMatchResponseDto
{
    /// <summary>
    /// Number of new proposed matches created (sum of TransactionMatchCount and GroupMatchCount).
    /// </summary>
    public int ProposedCount { get; set; }

    /// <summary>
    /// Number of matches to individual transactions.
    /// </summary>
    public int TransactionMatchCount { get; set; }

    /// <summary>
    /// Number of matches to transaction groups.
    /// </summary>
    public int GroupMatchCount { get; set; }

    /// <summary>
    /// Total receipts processed.
    /// </summary>
    public int ProcessedCount { get; set; }

    /// <summary>
    /// Receipts with multiple close matches (flagged for review).
    /// </summary>
    public int AmbiguousCount { get; set; }

    /// <summary>
    /// Processing time in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// List of proposed matches (includes both transaction and group matches).
    /// </summary>
    public List<MatchProposalDto> Proposals { get; set; } = new();
}
