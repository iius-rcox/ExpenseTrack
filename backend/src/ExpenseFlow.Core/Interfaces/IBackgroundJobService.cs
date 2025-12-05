namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for background job operations.
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Enqueues a job for immediate execution.
    /// </summary>
    /// <typeparam name="T">The job type.</typeparam>
    /// <param name="jobId">Optional job identifier.</param>
    /// <returns>The enqueued job ID.</returns>
    string EnqueueJob<T>(string? jobId = null) where T : class;

    /// <summary>
    /// Enqueues a reference data sync job.
    /// </summary>
    /// <returns>The enqueued job ID.</returns>
    string EnqueueReferenceDataSync();

    /// <summary>
    /// Checks if a job is currently running.
    /// </summary>
    /// <param name="jobId">The job ID to check.</param>
    /// <returns>True if the job is running.</returns>
    bool IsJobRunning(string jobId);
}
