namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Represents a pending action requiring user review.
/// </summary>
public record PendingActionDto
{
    /// <summary>
    /// Unique identifier for the action item.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Type of action: "match_review" or "categorization".
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Human-readable action title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Detailed description of what needs review.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// When the action was created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Optional metadata for the action (e.g., confidence score for matches).
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
