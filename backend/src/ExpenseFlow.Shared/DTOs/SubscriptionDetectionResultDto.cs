using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Actions taken during subscription detection.
/// </summary>
public enum SubscriptionDetectionAction
{
    /// <summary>
    /// No subscription pattern detected.
    /// </summary>
    None,

    /// <summary>
    /// New subscription was detected and created.
    /// </summary>
    Created,

    /// <summary>
    /// Existing subscription was updated with new occurrence.
    /// </summary>
    Updated,

    /// <summary>
    /// Subscription status changed (e.g., Active to Missing).
    /// </summary>
    StatusChanged,

    /// <summary>
    /// Amount variance detected, flagged for review.
    /// </summary>
    Flagged
}

/// <summary>
/// Result of subscription detection from a transaction.
/// </summary>
public class SubscriptionDetectionResultDto
{
    /// <summary>
    /// Whether a subscription pattern was detected.
    /// </summary>
    public bool Detected { get; set; }

    /// <summary>
    /// The subscription (created or updated).
    /// </summary>
    public SubscriptionDetailDto? Subscription { get; set; }

    /// <summary>
    /// Action taken during detection.
    /// </summary>
    public SubscriptionDetectionAction Action { get; set; }

    /// <summary>
    /// Source of detection (pattern match or known vendor seed data).
    /// </summary>
    public DetectionSource? DetectionSource { get; set; }

    /// <summary>
    /// Confidence score for pattern detection (0.0-1.0).
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// Amount variance from average if Flagged (as percentage).
    /// </summary>
    public decimal? AmountVariancePercent { get; set; }

    /// <summary>
    /// Human-readable message about the detection.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether this detection requires user review.
    /// </summary>
    public bool RequiresReview { get; set; }
}

/// <summary>
/// Result of batch subscription detection (e.g., processing all transactions in a statement).
/// </summary>
public class BatchSubscriptionDetectionResultDto
{
    /// <summary>
    /// Number of transactions processed.
    /// </summary>
    public int TransactionsProcessed { get; set; }

    /// <summary>
    /// Number of new subscriptions detected.
    /// </summary>
    public int NewSubscriptions { get; set; }

    /// <summary>
    /// Number of existing subscriptions updated.
    /// </summary>
    public int UpdatedSubscriptions { get; set; }

    /// <summary>
    /// Number of subscriptions flagged for review.
    /// </summary>
    public int FlaggedSubscriptions { get; set; }

    /// <summary>
    /// Individual detection results.
    /// </summary>
    public List<SubscriptionDetectionResultDto> Results { get; set; } = new();

    /// <summary>
    /// Processing duration in milliseconds.
    /// </summary>
    public long ProcessingTimeMs { get; set; }
}
