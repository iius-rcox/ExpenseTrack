namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response DTO for enqueued background job.
/// </summary>
public class JobEnqueuedResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = "enqueued";
    public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EstimatedCompletion { get; set; }
}
