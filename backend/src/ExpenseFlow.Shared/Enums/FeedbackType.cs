namespace ExpenseFlow.Shared.Enums;

/// <summary>
/// Type of user feedback on a prediction.
/// </summary>
public enum FeedbackType
{
    /// <summary>User confirmed the prediction was correct</summary>
    Confirmed = 1,

    /// <summary>User rejected the prediction as incorrect</summary>
    Rejected = 2
}
