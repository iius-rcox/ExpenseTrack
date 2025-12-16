namespace ExpenseFlow.Shared.Enums;

/// <summary>
/// Status of a receipt-to-transaction match.
/// </summary>
public enum MatchStatus : short
{
    /// <summary>Receipt/Transaction has no match</summary>
    Unmatched = 0,

    /// <summary>Auto-match proposed, awaiting user review</summary>
    Proposed = 1,

    /// <summary>User confirmed the match</summary>
    Matched = 2
}
