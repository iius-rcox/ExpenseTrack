namespace ExpenseFlow.Shared.Enums;

/// <summary>
/// Confidence level for expense predictions.
/// </summary>
public enum PredictionConfidence
{
    /// <summary>Confidence below 0.50 - suppressed from display</summary>
    Low = 0,

    /// <summary>Confidence 0.50 - 0.74</summary>
    Medium = 1,

    /// <summary>Confidence 0.75 or higher</summary>
    High = 2
}
