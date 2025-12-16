namespace ExpenseFlow.Shared.Enums;

/// <summary>
/// Source of travel period creation.
/// </summary>
public enum TravelPeriodSource
{
    /// <summary>Created from flight receipt detection</summary>
    Flight = 0,

    /// <summary>Created from hotel/lodging receipt detection</summary>
    Hotel = 1,

    /// <summary>Manually created by user</summary>
    Manual = 2
}
