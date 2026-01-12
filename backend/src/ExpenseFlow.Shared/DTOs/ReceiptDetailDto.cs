using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Detailed DTO for single receipt view.
/// </summary>
public class ReceiptDetailDto : ReceiptSummaryDto
{
    public string BlobUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public decimal? Tax { get; set; }
    public List<LineItemDto> LineItems { get; set; } = new();
    public Dictionary<string, double> ConfidenceScores { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public int PageCount { get; set; } = 1;
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Matched transaction details (null if not matched).
    /// </summary>
    public MatchedTransactionInfoDto? MatchedTransaction { get; set; }
}

/// <summary>
/// Information about a matched transaction (displayed on receipt detail page).
/// </summary>
public class MatchedTransactionInfoDto
{
    /// <summary>
    /// The match record ID (needed for unmatch operations).
    /// </summary>
    public Guid MatchId { get; set; }

    /// <summary>
    /// The transaction ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Transaction date.
    /// </summary>
    public DateOnly TransactionDate { get; set; }

    /// <summary>
    /// Transaction description (normalized).
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Transaction amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Merchant name (if available).
    /// </summary>
    public string? MerchantName { get; set; }

    /// <summary>
    /// Match confidence score (0-1).
    /// </summary>
    public decimal MatchConfidence { get; set; }
}

/// <summary>
/// DTO for receipt line items.
/// </summary>
public class LineItemDto
{
    public string Description { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? TotalPrice { get; set; }
    public double? Confidence { get; set; }
}
