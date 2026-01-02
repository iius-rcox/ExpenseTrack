using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Entities;

/// <summary>
/// User feedback on a specific prediction.
/// Used for observability metrics and pattern improvement.
/// </summary>
public class PredictionFeedback : BaseEntity
{
    /// <summary>FK to TransactionPredictions</summary>
    public Guid PredictionId { get; set; }

    /// <summary>FK to Users</summary>
    public Guid UserId { get; set; }

    /// <summary>Feedback type: Confirmed or Rejected</summary>
    public FeedbackType FeedbackType { get; set; }

    // Navigation properties
    public TransactionPrediction Prediction { get; set; } = null!;
    public User User { get; set; } = null!;
}
