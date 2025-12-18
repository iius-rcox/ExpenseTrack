using System.Text.Json;
using Azure.Storage.Blobs;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Infrastructure.Jobs;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for cache warming operations including historical data import.
/// </summary>
public class CacheWarmingService : ICacheWarmingService
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<CacheWarmingService> _logger;
    private readonly string _importsContainer;

    public CacheWarmingService(
        ExpenseFlowDbContext dbContext,
        IConfiguration configuration,
        IBackgroundJobClient backgroundJobClient,
        ILogger<CacheWarmingService> logger)
    {
        _dbContext = dbContext;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;

        var connectionString = configuration["BlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("BlobStorage:ConnectionString is required");
        _importsContainer = configuration["BlobStorage:ImportsContainer"] ?? "cache-warming-imports";

        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<ImportJob> ImportHistoricalDataAsync(Stream fileStream, string fileName, Guid userId)
    {
        _logger.LogInformation("Starting historical data import for user {UserId}, file: {FileName}", userId, fileName);

        // Validate file extension
        if (!fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only Excel (.xlsx) files are supported", nameof(fileName));
        }

        // Upload file to blob storage
        var blobUrl = await UploadFileToBlobAsync(fileStream, fileName, userId);

        // Create import job record
        var importJob = new ImportJob
        {
            UserId = userId,
            SourceFileName = fileName,
            BlobUrl = blobUrl,
            Status = ImportJobStatus.Pending,
            StartedAt = DateTime.UtcNow
        };

        _dbContext.ImportJobs.Add(importJob);
        await _dbContext.SaveChangesAsync();

        // Queue background job for processing
        _backgroundJobClient.Enqueue<CacheWarmingJob>(job => job.ProcessImportAsync(importJob.Id, CancellationToken.None));

        _logger.LogInformation("Import job {JobId} created and queued for processing", importJob.Id);

        return importJob;
    }

    public async Task<ImportJob?> GetImportJobAsync(Guid jobId)
    {
        return await _dbContext.ImportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId);
    }

    public async Task<(List<ImportJob> Jobs, int TotalCount)> GetImportJobsAsync(
        Guid userId,
        ImportJobStatus? status,
        int page,
        int pageSize)
    {
        var query = _dbContext.ImportJobs
            .AsNoTracking()
            .Where(j => j.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(j => j.Status == status.Value);
        }

        var totalCount = await query.CountAsync();

        var jobs = await query
            .OrderByDescending(j => j.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (jobs, totalCount);
    }

    public async Task<bool> CancelImportJobAsync(Guid jobId)
    {
        var job = await _dbContext.ImportJobs.FindAsync(jobId);
        if (job == null)
        {
            return false;
        }

        // Can only cancel pending or processing jobs
        if (job.Status != ImportJobStatus.Pending && job.Status != ImportJobStatus.Processing)
        {
            _logger.LogWarning("Cannot cancel job {JobId} in status {Status}", jobId, job.Status);
            return false;
        }

        job.Status = ImportJobStatus.Cancelled;
        job.CompletedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Import job {JobId} cancelled", jobId);
        return true;
    }

    public async Task<(List<ImportError> Errors, int TotalCount)> GetImportJobErrorsAsync(
        Guid jobId,
        int page,
        int pageSize)
    {
        var job = await _dbContext.ImportJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null || string.IsNullOrEmpty(job.ErrorLog))
        {
            return (new List<ImportError>(), 0);
        }

        var allErrors = JsonSerializer.Deserialize<List<ImportError>>(job.ErrorLog)
            ?? new List<ImportError>();

        var totalCount = allErrors.Count;

        var pagedErrors = allErrors
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (pagedErrors, totalCount);
    }

    private async Task<string> UploadFileToBlobAsync(Stream fileStream, string fileName, Guid userId)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_importsContainer);
        await containerClient.CreateIfNotExistsAsync();

        var blobName = $"{userId}/{DateTime.UtcNow:yyyyMMdd-HHmmss}-{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        fileStream.Position = 0;
        await blobClient.UploadAsync(fileStream, overwrite: true);

        _logger.LogInformation("Uploaded import file to {BlobUrl}", blobClient.Uri);

        return blobClient.Uri.ToString();
    }
}
