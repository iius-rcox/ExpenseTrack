namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Status of a report generation background job.
/// </summary>
public enum ReportJobStatus
{
    /// <summary>Job is queued and waiting to start.</summary>
    Pending = 0,

    /// <summary>Job is actively processing expense lines.</summary>
    Processing = 1,

    /// <summary>Job completed successfully; report is ready.</summary>
    Completed = 2,

    /// <summary>Job failed due to an unrecoverable error.</summary>
    Failed = 3,

    /// <summary>Job was cancelled by the user.</summary>
    Cancelled = 4,

    /// <summary>User requested cancellation; job will stop at next checkpoint.</summary>
    CancellationRequested = 5
}
