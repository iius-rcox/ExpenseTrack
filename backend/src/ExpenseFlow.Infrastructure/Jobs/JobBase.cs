using Hangfire;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Jobs;

/// <summary>
/// Base class for Hangfire background jobs with common functionality.
/// </summary>
public abstract class JobBase
{
    protected readonly ILogger Logger;

    protected JobBase(ILogger logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// Executes the job with automatic retry on failure.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public abstract Task ExecuteAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Logs job start with context.
    /// </summary>
    protected void LogJobStart(string jobName)
    {
        Logger.LogInformation("Starting job: {JobName}", jobName);
    }

    /// <summary>
    /// Logs job completion with duration.
    /// </summary>
    protected void LogJobComplete(string jobName, TimeSpan duration)
    {
        Logger.LogInformation("Completed job: {JobName} in {Duration}ms", jobName, duration.TotalMilliseconds);
    }

    /// <summary>
    /// Logs job failure with exception details.
    /// </summary>
    protected void LogJobFailed(string jobName, Exception ex)
    {
        Logger.LogError(ex, "Job failed: {JobName}", jobName);
    }
}
