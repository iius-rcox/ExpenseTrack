using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Jobs;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for managing background job operations.
/// </summary>
public class BackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<BackgroundJobService> _logger;

    public BackgroundJobService(
        IBackgroundJobClient backgroundJobClient,
        ILogger<BackgroundJobService> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public string EnqueueJob<T>(string? jobId = null) where T : class
    {
        var id = _backgroundJobClient.Enqueue<T>(job => ((JobBase)(object)job).ExecuteAsync(CancellationToken.None));
        _logger.LogInformation("Enqueued job {JobType} with ID {JobId}", typeof(T).Name, id);
        return id;
    }

    /// <inheritdoc />
    public string EnqueueReferenceDataSync()
    {
        var jobId = _backgroundJobClient.Enqueue<ReferenceDataSyncJob>(
            job => job.ExecuteAsync(CancellationToken.None));

        _logger.LogInformation("Enqueued reference data sync job with ID {JobId}", jobId);
        return jobId;
    }

    /// <inheritdoc />
    public bool IsJobRunning(string jobId)
    {
        using var connection = JobStorage.Current.GetConnection();
        var jobData = connection.GetJobData(jobId);
        return jobData?.State == "Processing";
    }
}
