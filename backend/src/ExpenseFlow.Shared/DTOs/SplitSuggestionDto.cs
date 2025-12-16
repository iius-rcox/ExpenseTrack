namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Source of the split suggestion.
/// </summary>
public enum SplitSuggestionSource
{
    /// <summary>
    /// Suggested from a saved pattern for the vendor.
    /// </summary>
    VendorPattern,

    /// <summary>
    /// Suggested from the most recently used pattern.
    /// </summary>
    RecentUsage,

    /// <summary>
    /// Suggested from similar expense history.
    /// </summary>
    SimilarExpense,

    /// <summary>
    /// No suggestion available.
    /// </summary>
    None
}

/// <summary>
/// Split suggestion response for an expense.
/// </summary>
public class SplitSuggestionDto
{
    /// <summary>
    /// Whether a suggestion is available.
    /// </summary>
    public bool HasSuggestion { get; set; }

    /// <summary>
    /// The source of this suggestion.
    /// </summary>
    public SplitSuggestionSource Source { get; set; }

    /// <summary>
    /// The pattern ID if suggestion comes from a saved pattern.
    /// </summary>
    public Guid? PatternId { get; set; }

    /// <summary>
    /// The pattern name if suggestion comes from a saved pattern.
    /// </summary>
    public string? PatternName { get; set; }

    /// <summary>
    /// The suggested allocations.
    /// </summary>
    public List<SplitAllocationDto> SuggestedAllocations { get; set; } = new();

    /// <summary>
    /// Confidence score for the suggestion (0.0-1.0).
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// How many times this pattern has been used.
    /// </summary>
    public int PatternUsageCount { get; set; }

    /// <summary>
    /// Human-readable explanation of why this suggestion was made.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Current split status for an expense.
/// </summary>
public class ExpenseSplitStatusDto
{
    /// <summary>
    /// The expense ID.
    /// </summary>
    public Guid ExpenseId { get; set; }

    /// <summary>
    /// Whether this expense is currently split.
    /// </summary>
    public bool IsSplit { get; set; }

    /// <summary>
    /// The current allocations if split.
    /// </summary>
    public List<SplitAllocationDto> CurrentAllocations { get; set; } = new();

    /// <summary>
    /// The total expense amount.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// The suggestion for this expense.
    /// </summary>
    public SplitSuggestionDto? Suggestion { get; set; }
}
