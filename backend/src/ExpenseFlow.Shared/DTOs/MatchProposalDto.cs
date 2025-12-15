namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// DTO for a proposed receipt-to-transaction match.
/// </summary>
public class MatchProposalDto
{
    /// <summary>
    /// Match record ID.
    /// </summary>
    public Guid MatchId { get; set; }

    /// <summary>
    /// Linked receipt ID.
    /// </summary>
    public Guid ReceiptId { get; set; }

    /// <summary>
    /// Linked transaction ID.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Overall confidence score (0-100).
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
    /// Human-readable explanation of match.
    /// </summary>
    public string? MatchReason { get; set; }

    /// <summary>
    /// Match status: Proposed, Confirmed, Rejected.
    /// </summary>
    public string Status { get; set; } = "Proposed";

    /// <summary>
    /// Receipt summary data.
    /// </summary>
    public MatchReceiptSummaryDto? Receipt { get; set; }

    /// <summary>
    /// Transaction summary data.
    /// </summary>
    public MatchTransactionSummaryDto? Transaction { get; set; }

    /// <summary>
    /// When the match was proposed.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Receipt summary for matching context.
/// </summary>
public class MatchReceiptSummaryDto
{
    public Guid Id { get; set; }
    public string? VendorExtracted { get; set; }
    public DateOnly? DateExtracted { get; set; }
    public decimal? AmountExtracted { get; set; }
    public string Currency { get; set; } = "USD";
    public string? ThumbnailUrl { get; set; }
    public string? OriginalFilename { get; set; }
}

/// <summary>
/// Transaction summary for matching context.
/// </summary>
public class MatchTransactionSummaryDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? OriginalDescription { get; set; }
    public DateOnly TransactionDate { get; set; }
    public DateOnly? PostDate { get; set; }
    public decimal Amount { get; set; }
}
