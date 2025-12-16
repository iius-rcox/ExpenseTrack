namespace ExpenseFlow.Shared.Enums;

/// <summary>
/// Classification of vendors for specialized processing.
/// </summary>
public enum VendorCategory
{
    /// <summary>Normal vendor with no special processing</summary>
    Standard = 0,

    /// <summary>Airline vendor - triggers travel period detection</summary>
    Airline = 1,

    /// <summary>Hotel/lodging vendor - triggers travel period extension</summary>
    Hotel = 2,

    /// <summary>Known recurring subscription vendor</summary>
    Subscription = 3
}
