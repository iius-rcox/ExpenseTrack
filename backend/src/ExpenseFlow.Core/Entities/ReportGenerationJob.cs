namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Tracks the status and progress of expense report generation background jobs.
/// </summary>
public class ReportGenerationJob : BaseEntity
{
    /// <summary>User who initiated the job.</summary>
    public Guid UserId { get; set; }

    /// <summary>Billing period in YYYY-MM format.</summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>Current job status.</summary>
    public ReportJobStatus Status { get; set; } = ReportJobStatus.Pending;

    /// <summary>Total expense lines to process.</summary>
    public int TotalLines { get; set; }

    /// <summary>Number of lines processed so far.</summary>
    public int ProcessedLines { get; set; }

    /// <summary>Number of lines that failed categorization after all retries.</summary>
    public int FailedLines { get; set; }

    /// <summary>Total number of retry attempts due to rate limiting.</summary>
    public int RetryCount { get; set; }

    /// <summary>User-friendly error message if job failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Detailed error log for debugging (not shown to user).</summary>
    public string? ErrorDetails { get; set; }

    /// <summary>Hangfire job ID for correlation.</summary>
    public string? HangfireJobId { get; set; }

    /// <summary>Estimated completion time, updated dynamically based on processing rate.</summary>
    public DateTime? EstimatedCompletionAt { get; set; }

    /// <summary>When processing started (null if still queued).</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>When processing completed (success, failure, or cancellation).</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>ID of the generated report (null until completed).</summary>
    public Guid? GeneratedReportId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ExpenseReport? GeneratedReport { get; set; }
}
