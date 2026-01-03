using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Prediction that a transaction is likely a business expense.
/// Links a transaction to the pattern that generated the prediction,
/// or represents a manual user override (when PatternId is null).
/// </summary>
public class TransactionPrediction : BaseEntity
{
    /// <summary>
    /// FK to ExpensePatterns - the pattern that matched.
    /// Null for manual overrides (user directly marked transaction).
    /// </summary>
    public Guid? PatternId { get; set; }

    /// <summary>FK to Transactions - the predicted transaction</summary>
    public Guid TransactionId { get; set; }

    /// <summary>FK to Users - for efficient user-scoped queries</summary>
    public Guid UserId { get; set; }

    /// <summary>Calculated confidence score (0.00 - 1.00)</summary>
    public decimal ConfidenceScore { get; set; }

    /// <summary>Confidence level: High, Medium, or Low</summary>
    public PredictionConfidence ConfidenceLevel { get; set; }

    /// <summary>Prediction status: Pending, Confirmed, Rejected, Ignored</summary>
    public PredictionStatus Status { get; set; } = PredictionStatus.Pending;

    /// <summary>When user acted on prediction (confirm/reject)</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// True if this prediction was manually created by user (not auto-generated from pattern).
    /// Manual predictions have PatternId = null and can be cleared to allow re-prediction.
    /// </summary>
    public bool IsManualOverride { get; set; }

    // Navigation properties
    public ExpensePattern? Pattern { get; set; }
    public Transaction Transaction { get; set; } = null!;
    public User User { get; set; } = null!;
}
