namespace ExpenseFlow.Shared.Enums;

/// <summary>
/// Justification reasons for expenses without receipts.
/// </summary>
public enum MissingReceiptJustification : short
{
    /// <summary>Not specified yet</summary>
    None = 0,

    /// <summary>Vendor did not provide a receipt</summary>
    NotProvided = 1,

    /// <summary>Receipt was lost</summary>
    Lost = 2,

    /// <summary>Digital subscription with no physical/email receipt</summary>
    DigitalSubscription = 3,

    /// <summary>Amount under company threshold requiring receipt</summary>
    UnderThreshold = 4,

    /// <summary>Other reason - see JustificationNote</summary>
    Other = 5
}
