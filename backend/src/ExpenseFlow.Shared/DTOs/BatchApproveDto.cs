namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request DTO for batch approving matches.
/// </summary>
public class BatchApproveRequestDto
{
    /// <summary>
    /// Optional list of specific match IDs to approve.
    /// If provided, minConfidence is ignored.
    /// </summary>
    public List<Guid>? Ids { get; set; }

    /// <summary>
    /// Minimum confidence score (0-100) to approve.
    /// Used when Ids is not provided.
    /// </summary>
    public decimal? MinConfidence { get; set; }
}

/// <summary>
/// Response DTO for batch approve operation.
/// </summary>
public class BatchApproveResponseDto
{
    /// <summary>
    /// Number of matches that were approved.
    /// </summary>
    public int Approved { get; set; }

    /// <summary>
    /// Number of matches that were skipped (errors or invalid state).
    /// </summary>
    public int Skipped { get; set; }
}
