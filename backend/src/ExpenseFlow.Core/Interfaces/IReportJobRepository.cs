using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Repository interface for ReportGenerationJob entity operations.
/// </summary>
public interface IReportJobRepository
{
    /// <summary>
    /// Gets a job by ID.
    /// </summary>
    /// <param name="id">Job ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Job if found, null otherwise</returns>
    Task<ReportGenerationJob?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets an active (non-terminal status) job by user and period.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="period">Period in YYYY-MM format</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Active job if exists, null otherwise</returns>
    Task<ReportGenerationJob?> GetActiveByUserAndPeriodAsync(Guid userId, string period, CancellationToken ct = default);

    /// <summary>
    /// Gets jobs for a user with optional filtering and pagination.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of jobs ordered by created date descending</returns>
    Task<List<ReportGenerationJob>> GetByUserAsync(Guid userId, ReportJobStatus? status, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Gets count of jobs for a user with optional filtering.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Total count of matching jobs</returns>
    Task<int> GetCountByUserAsync(Guid userId, ReportJobStatus? status, CancellationToken ct = default);

    /// <summary>
    /// Adds a new report generation job.
    /// </summary>
    /// <param name="job">Job to add</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Added job with generated ID</returns>
    Task<ReportGenerationJob> AddAsync(ReportGenerationJob job, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing report generation job.
    /// </summary>
    /// <param name="job">Job to update</param>
    /// <param name="ct">Cancellation token</param>
    Task UpdateAsync(ReportGenerationJob job, CancellationToken ct = default);

    /// <summary>
    /// Updates job progress fields atomically.
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="processedLines">Number of processed lines</param>
    /// <param name="failedLines">Number of failed lines</param>
    /// <param name="estimatedCompletionAt">Updated estimated completion time</param>
    /// <param name="ct">Cancellation token</param>
    Task UpdateProgressAsync(Guid jobId, int processedLines, int failedLines, DateTime? estimatedCompletionAt, CancellationToken ct = default);

    /// <summary>
    /// Checks if job cancellation has been requested.
    /// </summary>
    /// <param name="jobId">Job ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if cancellation was requested</returns>
    Task<bool> IsCancellationRequestedAsync(Guid jobId, CancellationToken ct = default);

    /// <summary>
    /// Deletes completed jobs older than the specified date.
    /// </summary>
    /// <param name="olderThan">Cutoff date</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of deleted jobs</returns>
    Task<int> DeleteOlderThanAsync(DateTime olderThan, CancellationToken ct = default);
}
