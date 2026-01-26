namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request to start a thumbnail backfill job for receipts without thumbnails.
/// </summary>
public record ThumbnailBackfillRequest
{
    /// <summary>
    /// Number of receipts to process per batch. Default: 50, Max: 500.
    /// </summary>
    public int BatchSize { get; init; } = 50;

    /// <summary>
    /// Optional filter for content types to process.
    /// If omitted or empty, all supported types are processed.
    /// </summary>
    /// <example>["application/pdf", "text/html"]</example>
    public List<string>? ContentTypes { get; init; }

    /// <summary>
    /// If true, regenerate ALL thumbnails (even existing ones) at current resolution settings.
    /// Use this after changing thumbnail size defaults to upgrade existing thumbnails.
    /// Default: false (only process receipts without thumbnails).
    /// </summary>
    public bool ForceRegenerate { get; init; } = false;
}

/// <summary>
/// Response when a thumbnail backfill job is started.
/// </summary>
public record ThumbnailBackfillResponse
{
    /// <summary>
    /// Hangfire job ID for tracking the backfill job.
    /// </summary>
    public required string JobId { get; init; }

    /// <summary>
    /// Estimated number of receipts to process.
    /// </summary>
    public int EstimatedCount { get; init; }

    /// <summary>
    /// Human-readable status message.
    /// </summary>
    public required string Message { get; init; }
}

/// <summary>
/// Status of the thumbnail backfill job.
/// </summary>
public record ThumbnailBackfillStatus
{
    /// <summary>
    /// Current job status.
    /// </summary>
    public required BackfillJobStatus Status { get; init; }

    /// <summary>
    /// Current or last job ID (null if no job has ever run).
    /// </summary>
    public string? JobId { get; init; }

    /// <summary>
    /// Number of receipts successfully processed.
    /// </summary>
    public int ProcessedCount { get; init; }

    /// <summary>
    /// Number of thumbnails that failed to generate.
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// Total receipts without thumbnails at job start.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// When the job started (null if never run).
    /// </summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>
    /// When the job completed (null if still running or never completed).
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Current batch number being processed.
    /// </summary>
    public int CurrentBatch { get; init; }

    /// <summary>
    /// Recent errors (limited to last 100).
    /// </summary>
    public List<ThumbnailBackfillError>? Errors { get; init; }
}

/// <summary>
/// Error detail for a failed thumbnail generation during backfill.
/// </summary>
public record ThumbnailBackfillError
{
    /// <summary>
    /// Receipt ID that failed.
    /// </summary>
    public Guid ReceiptId { get; init; }

    /// <summary>
    /// Error message describing the failure.
    /// </summary>
    public required string Error { get; init; }
}

/// <summary>
/// Status values for the backfill job.
/// </summary>
public enum BackfillJobStatus
{
    /// <summary>No job running, ready to start.</summary>
    Idle,

    /// <summary>Job is currently running.</summary>
    Running,

    /// <summary>Job completed successfully.</summary>
    Completed,

    /// <summary>Job failed with errors.</summary>
    Failed
}

/// <summary>
/// Response when a thumbnail regeneration is queued for a specific receipt.
/// </summary>
public record ThumbnailRegenerationResponse
{
    /// <summary>
    /// ID of the receipt being processed.
    /// </summary>
    public Guid ReceiptId { get; init; }

    /// <summary>
    /// Hangfire job ID for tracking.
    /// </summary>
    public required string JobId { get; init; }

    /// <summary>
    /// Human-readable status message.
    /// </summary>
    public required string Message { get; init; }
}
