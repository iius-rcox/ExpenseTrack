namespace ExpenseFlow.Shared.Enums;

/// <summary>
/// Status of a detected subscription.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>Subscription detected and recurring charges appearing as expected</summary>
    Active = 0,

    /// <summary>Expected charge not seen by calendar month end</summary>
    Missing = 1,

    /// <summary>Unusual amount variation (>20% from average)</summary>
    Flagged = 2
}
