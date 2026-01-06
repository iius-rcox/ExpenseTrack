using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for report generation job management.
/// </summary>
public interface IReportJobService
{
    /// <summary>
    /// Creates a new report generation job and enqueues it for background processing.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="period">Billing period in YYYY-MM format</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created job</returns>
    /// <exception cref="InvalidOperationException">If an active job exists for this user/period</exception>
    Task<ReportGenerationJob> CreateJobAsync(Guid userId, string period, CancellationToken ct = default);

    /// <summary>
    /// Gets a job by ID (for the specified user).
    /// </summary>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="jobId">Job ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Job if found and owned by user, null otherwise</returns>
    Task<ReportGenerationJob?> GetByIdAsync(Guid userId, Guid jobId, CancellationToken ct = default);

    /// <summary>
    /// Gets paginated list of jobs for a user.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tuple of (jobs, total count)</returns>
    Task<(List<ReportGenerationJob> Jobs, int TotalCount)> GetListAsync(
        Guid userId,
        ReportJobStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Requests cancellation of an active job.
    /// </summary>
    /// <param name="userId">User ID (for authorization)</param>
    /// <param name="jobId">Job ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated job if cancellation requested, null if job not found</returns>
    /// <exception cref="InvalidOperationException">If job cannot be cancelled (already completed/failed/cancelled)</exception>
    Task<ReportGenerationJob?> CancelAsync(Guid userId, Guid jobId, CancellationToken ct = default);

    /// <summary>
    /// Gets an active job for the user and period (if exists).
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="period">Billing period</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Active job if exists, null otherwise</returns>
    Task<ReportGenerationJob?> GetActiveJobAsync(Guid userId, string period, CancellationToken ct = default);
}
