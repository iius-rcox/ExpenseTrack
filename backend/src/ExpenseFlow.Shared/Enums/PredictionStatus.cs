namespace ExpenseFlow.Shared.Enums;

/// <summary>
/// Status of a transaction prediction.
/// </summary>
public enum PredictionStatus
{
    /// <summary>Awaiting user action</summary>
    Pending = 0,

    /// <summary>User confirmed as business expense</summary>
    Confirmed = 1,

    /// <summary>User rejected as not a business expense</summary>
    Rejected = 2,

    /// <summary>User took no action (implicit ignore)</summary>
    Ignored = 3
}
