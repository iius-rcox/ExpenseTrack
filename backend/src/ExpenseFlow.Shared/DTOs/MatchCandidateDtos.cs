namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Type of match candidate (transaction or group).
/// </summary>
public enum MatchCandidateType
{
    /// <summary>
    /// Individual transaction candidate.
    /// </summary>
    Transaction = 0,

    /// <summary>
    /// Transaction group candidate.
    /// </summary>
    Group = 1
}

/// <summary>
/// DTO representing a potential match candidate (transaction or group).
/// Used by the GetCandidates endpoint for manual matching.
/// </summary>
public class MatchCandidateDto
{
    /// <summary>
    /// Candidate ID (TransactionId or TransactionGroupId).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Type of candidate: "transaction" or "group".
    /// </summary>
    public string CandidateType { get; set; } = "transaction";

    /// <summary>
    /// Amount (Transaction.Amount or TransactionGroup.CombinedAmount).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Date (Transaction.TransactionDate or TransactionGroup.DisplayDate).
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Display name (Transaction.Description or TransactionGroup.Name).
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Number of transactions in group (null for individual transactions).
    /// </summary>
    public int? TransactionCount { get; set; }

    /// <summary>
    /// Calculated match confidence score (0-100).
    /// </summary>
    public decimal ConfidenceScore { get; set; }

    /// <summary>
    /// Amount match score component (0-40).
    /// </summary>
    public decimal AmountScore { get; set; }

    /// <summary>
    /// Date match score component (0-35).
    /// </summary>
    public decimal DateScore { get; set; }

    /// <summary>
    /// Vendor match score component (0-25).
    /// </summary>
    public decimal VendorScore { get; set; }

    /// <summary>
    /// Human-readable explanation of the match score.
    /// </summary>
    public string? MatchReason { get; set; }
}

/// <summary>
/// Transaction group summary for matching context.
/// </summary>
public class MatchTransactionGroupSummaryDto
{
    /// <summary>
    /// Group ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Group name (e.g., "TWILIO (3 charges)").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Combined amount of all transactions in the group.
    /// </summary>
    public decimal CombinedAmount { get; set; }

    /// <summary>
    /// Display date for the group.
    /// </summary>
    public DateOnly DisplayDate { get; set; }

    /// <summary>
    /// Number of transactions in the group.
    /// </summary>
    public int TransactionCount { get; set; }
}
