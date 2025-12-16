namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Represents a single allocation within an expense split.
/// </summary>
public class SplitAllocationDto
{
    /// <summary>
    /// The GL account code for this allocation.
    /// </summary>
    public string GLCode { get; set; } = string.Empty;

    /// <summary>
    /// The department code for this allocation.
    /// </summary>
    public string? DepartmentCode { get; set; }

    /// <summary>
    /// The project code for this allocation.
    /// </summary>
    public string? ProjectCode { get; set; }

    /// <summary>
    /// The percentage of the total expense for this allocation (0-100).
    /// All allocations in a split must sum to exactly 100.
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// The calculated amount for this allocation.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Optional description for this allocation.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request DTO for applying a split to an expense.
/// </summary>
public class ApplySplitRequestDto
{
    /// <summary>
    /// The allocations for the split.
    /// Must sum to exactly 100%.
    /// </summary>
    public List<SplitAllocationDto> Allocations { get; set; } = new();

    /// <summary>
    /// Whether to save this split as a pattern for future suggestions.
    /// </summary>
    public bool SaveAsPattern { get; set; }

    /// <summary>
    /// Optional name for the pattern (required if SaveAsPattern is true).
    /// </summary>
    public string? PatternName { get; set; }
}

/// <summary>
/// Response DTO after applying a split.
/// </summary>
public class ApplySplitResultDto
{
    /// <summary>
    /// Whether the split was successfully applied.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The expense ID that was split.
    /// </summary>
    public Guid ExpenseId { get; set; }

    /// <summary>
    /// The created split line IDs.
    /// </summary>
    public List<Guid> SplitLineIds { get; set; } = new();

    /// <summary>
    /// The pattern ID if a pattern was saved.
    /// </summary>
    public Guid? PatternId { get; set; }

    /// <summary>
    /// Message about the result.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
