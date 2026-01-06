using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Jobs;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for managing report generation jobs.
/// </summary>
public class ReportJobService : IReportJobService
{
    private readonly IReportJobRepository _repository;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<ReportJobService> _logger;

    public ReportJobService(
        IReportJobRepository repository,
        IBackgroundJobClient backgroundJobClient,
        ILogger<ReportJobService> logger)
    {
        _repository = repository;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public async Task<ReportGenerationJob> CreateJobAsync(Guid userId, string period, CancellationToken ct = default)
    {
        // Check for existing active job
        var existingJob = await _repository.GetActiveByUserAndPeriodAsync(userId, period, ct);
        if (existingJob != null)
        {
            _logger.LogWarning(
                "Duplicate job request for user {UserId} period {Period}. Existing job {JobId} is {Status}",
                userId, period, existingJob.Id, existingJob.Status);

            throw new InvalidOperationException(
                $"An active report generation job already exists for period {period}");
        }

        // Create job entity
        var job = new ReportGenerationJob
        {
            UserId = userId,
            Period = period,
            Status = ReportJobStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // Save to database
        job = await _repository.AddAsync(job, ct);

        _logger.LogInformation(
            "Created report generation job {JobId} for user {UserId} period {Period}",
            job.Id, userId, period);

        // Enqueue Hangfire job
        var hangfireJobId = _backgroundJobClient.Enqueue<ReportGenerationBackgroundJob>(
            bgJob => bgJob.ExecuteAsync(job.Id, CancellationToken.None));

        // Update with Hangfire job ID
        job.HangfireJobId = hangfireJobId;
        await _repository.UpdateAsync(job, ct);

        _logger.LogInformation(
            "Enqueued Hangfire job {HangfireJobId} for report generation job {JobId}",
            hangfireJobId, job.Id);

        return job;
    }

    public async Task<ReportGenerationJob?> GetByIdAsync(Guid userId, Guid jobId, CancellationToken ct = default)
    {
        var job = await _repository.GetByIdAsync(jobId, ct);

        // Authorization check - user can only access their own jobs
        if (job != null && job.UserId != userId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access job {JobId} owned by {OwnerUserId}",
                userId, jobId, job.UserId);
            return null;
        }

        return job;
    }

    public async Task<(List<ReportGenerationJob> Jobs, int TotalCount)> GetListAsync(
        Guid userId,
        ReportJobStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var jobs = await _repository.GetByUserAsync(userId, status, page, pageSize, ct);
        var totalCount = await _repository.GetCountByUserAsync(userId, status, ct);

        return (jobs, totalCount);
    }

    public async Task<ReportGenerationJob?> CancelAsync(Guid userId, Guid jobId, CancellationToken ct = default)
    {
        var job = await _repository.GetByIdAsync(jobId, ct);

        if (job == null)
        {
            return null;
        }

        // Authorization check
        if (job.UserId != userId)
        {
            _logger.LogWarning(
                "User {UserId} attempted to cancel job {JobId} owned by {OwnerUserId}",
                userId, jobId, job.UserId);
            return null;
        }

        // Check if job can be cancelled
        if (job.Status == ReportJobStatus.Completed ||
            job.Status == ReportJobStatus.Failed ||
            job.Status == ReportJobStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"Cannot cancel job in {job.Status} status");
        }

        // Set cancellation requested status
        job.Status = ReportJobStatus.CancellationRequested;
        await _repository.UpdateAsync(job, ct);

        _logger.LogInformation(
            "Cancellation requested for job {JobId} by user {UserId}",
            jobId, userId);

        return job;
    }

    public async Task<ReportGenerationJob?> GetActiveJobAsync(Guid userId, string period, CancellationToken ct = default)
    {
        return await _repository.GetActiveByUserAndPeriodAsync(userId, period, ct);
    }
}
