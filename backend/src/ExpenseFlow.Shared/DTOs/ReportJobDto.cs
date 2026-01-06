namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// DTO for report generation job status and progress.
/// </summary>
public record ReportJobDto
{
    public Guid Id { get; init; }
    public string Period { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int TotalLines { get; init; }
    public int ProcessedLines { get; init; }
    public int FailedLines { get; init; }

    /// <summary>Progress percentage (0-100).</summary>
    public int ProgressPercent => TotalLines > 0 ? (int)(ProcessedLines * 100.0 / TotalLines) : 0;

    /// <summary>Human-readable status message.</summary>
    public string StatusMessage { get; init; } = string.Empty;

    public DateTime? EstimatedCompletionAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public Guid? GeneratedReportId { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; }
}
