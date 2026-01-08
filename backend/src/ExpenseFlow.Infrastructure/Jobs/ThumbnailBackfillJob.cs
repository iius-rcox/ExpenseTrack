using System.Collections.Concurrent;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Jobs;

/// <summary>
/// Hangfire background job for generating thumbnails for historical receipts.
/// Processes receipts in batches with exponential backoff retry logic.
/// </summary>
public class ThumbnailBackfillJob : JobBase, IThumbnailBackfillService
{
    private readonly IReceiptRepository _receiptRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IThumbnailService _thumbnailService;
    private readonly IHtmlThumbnailService _htmlThumbnailService;
    private readonly IHtmlSanitizationService _htmlSanitizationService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<ThumbnailBackfillJob> _logger;
    private readonly int _defaultBatchSize;

    // Static state for tracking progress across invocations
    // In production, this would be stored in a database or Redis
    private static BackfillJobStatus _currentStatus = BackfillJobStatus.Idle;
    private static string? _currentJobId;
    private static int _processedCount;
    private static int _failedCount;
    private static int _totalCount;
    private static DateTime? _startedAt;
    private static DateTime? _completedAt;
    private static int _currentBatch;
    private static readonly ConcurrentQueue<ThumbnailBackfillError> _errors = new();
    private static readonly object _statusLock = new();

    public ThumbnailBackfillJob(
        IReceiptRepository receiptRepository,
        IBlobStorageService blobStorageService,
        IThumbnailService thumbnailService,
        IHtmlThumbnailService htmlThumbnailService,
        IHtmlSanitizationService htmlSanitizationService,
        IBackgroundJobClient backgroundJobClient,
        IConfiguration configuration,
        ILogger<ThumbnailBackfillJob> logger)
        : base(logger)
    {
        _receiptRepository = receiptRepository;
        _blobStorageService = blobStorageService;
        _thumbnailService = thumbnailService;
        _htmlThumbnailService = htmlThumbnailService;
        _htmlSanitizationService = htmlSanitizationService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
        _defaultBatchSize = configuration.GetValue("ReceiptProcessing:Thumbnail:BackfillBatchSize", 50);
    }

    public override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Use ProcessBackfillAsync instead.");
    }

    /// <inheritdoc />
    public async Task<ThumbnailBackfillResponse> StartBackfillAsync(ThumbnailBackfillRequest request)
    {
        lock (_statusLock)
        {
            if (_currentStatus == BackfillJobStatus.Running)
            {
                throw new InvalidOperationException("A backfill job is already running.");
            }
        }

        var batchSize = Math.Clamp(request.BatchSize, 1, 500);
        var estimatedCount = await _receiptRepository.GetReceiptsWithoutThumbnailsCountAsync(request.ContentTypes);

        if (estimatedCount == 0)
        {
            return new ThumbnailBackfillResponse
            {
                JobId = string.Empty,
                EstimatedCount = 0,
                Message = "No receipts require thumbnail generation."
            };
        }

        // Initialize status
        lock (_statusLock)
        {
            _currentStatus = BackfillJobStatus.Running;
            _processedCount = 0;
            _failedCount = 0;
            _totalCount = estimatedCount;
            _startedAt = DateTime.UtcNow;
            _completedAt = null;
            _currentBatch = 0;
            while (_errors.TryDequeue(out _)) { } // Clear errors
        }

        // Enqueue the background job
        var jobId = _backgroundJobClient.Enqueue<ThumbnailBackfillJob>(
            job => job.ProcessBackfillAsync(batchSize, request.ContentTypes, CancellationToken.None));

        lock (_statusLock)
        {
            _currentJobId = jobId;
        }

        _logger.LogInformation(
            "Started thumbnail backfill job {JobId}: {EstimatedCount} receipts, batch size {BatchSize}",
            jobId, estimatedCount, batchSize);

        return new ThumbnailBackfillResponse
        {
            JobId = jobId,
            EstimatedCount = estimatedCount,
            Message = "Backfill job started successfully."
        };
    }

    /// <inheritdoc />
    public Task<ThumbnailBackfillStatus> GetStatusAsync()
    {
        lock (_statusLock)
        {
            var errors = _errors.ToArray().TakeLast(100).ToList();

            return Task.FromResult(new ThumbnailBackfillStatus
            {
                Status = _currentStatus,
                JobId = _currentJobId,
                ProcessedCount = _processedCount,
                FailedCount = _failedCount,
                TotalCount = _totalCount,
                StartedAt = _startedAt,
                CompletedAt = _completedAt,
                CurrentBatch = _currentBatch,
                Errors = errors.Any() ? errors : null
            });
        }
    }

    /// <inheritdoc />
    public async Task<ThumbnailRegenerationResponse> RegenerateThumbnailAsync(Guid receiptId, Guid userId)
    {
        var receipt = await _receiptRepository.GetByIdAsync(receiptId, userId);
        if (receipt == null)
        {
            throw new KeyNotFoundException($"Receipt {receiptId} not found.");
        }

        // Enqueue a job to regenerate this specific thumbnail
        var jobId = _backgroundJobClient.Enqueue<ThumbnailBackfillJob>(
            job => job.ProcessSingleReceiptAsync(receiptId, CancellationToken.None));

        _logger.LogInformation(
            "Queued thumbnail regeneration for receipt {ReceiptId}, job {JobId}",
            receiptId, jobId);

        return new ThumbnailRegenerationResponse
        {
            ReceiptId = receiptId,
            JobId = jobId,
            Message = "Thumbnail regeneration queued."
        };
    }

    /// <summary>
    /// Processes the backfill job in batches.
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 1800 })] // 1min, 5min, 30min
    public async Task ProcessBackfillAsync(int batchSize, List<string>? contentTypes, CancellationToken ct)
    {
        var startTime = DateTime.UtcNow;
        LogJobStart("ThumbnailBackfillJob");

        try
        {
            while (!ct.IsCancellationRequested)
            {
                // Get next batch of receipts without thumbnails
                var receipts = await _receiptRepository.GetReceiptsWithoutThumbnailsAsync(
                    batchSize, contentTypes);

                if (receipts.Count == 0)
                {
                    _logger.LogInformation("Thumbnail backfill complete. No more receipts to process.");
                    break;
                }

                lock (_statusLock)
                {
                    _currentBatch++;
                }

                _logger.LogInformation(
                    "Processing batch {BatchNumber}: {Count} receipts",
                    _currentBatch, receipts.Count);

                foreach (var receipt in receipts)
                {
                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        await GenerateThumbnailForReceiptAsync(receipt, ct);

                        lock (_statusLock)
                        {
                            _processedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to generate thumbnail for receipt {ReceiptId}",
                            receipt.Id);

                        lock (_statusLock)
                        {
                            _failedCount++;
                            _errors.Enqueue(new ThumbnailBackfillError
                            {
                                ReceiptId = receipt.Id,
                                Error = ex.Message
                            });

                            // Keep only last 100 errors
                            while (_errors.Count > 100)
                            {
                                _errors.TryDequeue(out _);
                            }
                        }
                    }
                }

                _logger.LogDebug(
                    "Batch {BatchNumber} complete. Progress: {Processed}/{Total} processed, {Failed} failed",
                    _currentBatch, _processedCount, _totalCount, _failedCount);
            }

            lock (_statusLock)
            {
                _currentStatus = BackfillJobStatus.Completed;
                _completedAt = DateTime.UtcNow;
            }

            var duration = DateTime.UtcNow - startTime;
            LogJobComplete("ThumbnailBackfillJob", duration);

            _logger.LogInformation(
                "Thumbnail backfill completed in {Duration}. Processed: {Processed}, Failed: {Failed}",
                duration, _processedCount, _failedCount);
        }
        catch (Exception ex)
        {
            lock (_statusLock)
            {
                _currentStatus = BackfillJobStatus.Failed;
                _completedAt = DateTime.UtcNow;
            }

            LogJobFailed("ThumbnailBackfillJob", ex);
            throw; // Re-throw for Hangfire retry
        }
    }

    /// <summary>
    /// Processes a single receipt for thumbnail regeneration.
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 1800 })]
    public async Task ProcessSingleReceiptAsync(Guid receiptId, CancellationToken ct)
    {
        var receipt = await _receiptRepository.GetByIdAsync(receiptId);
        if (receipt == null)
        {
            _logger.LogWarning("Receipt {ReceiptId} not found for thumbnail regeneration", receiptId);
            return;
        }

        await GenerateThumbnailForReceiptAsync(receipt, ct);

        _logger.LogInformation(
            "Successfully regenerated thumbnail for receipt {ReceiptId}",
            receiptId);
    }

    /// <summary>
    /// Generates a thumbnail for a single receipt.
    /// </summary>
    private async Task GenerateThumbnailForReceiptAsync(Core.Entities.Receipt receipt, CancellationToken ct)
    {
        Stream? thumbnailStream = null;

        try
        {
            if (receipt.ContentType.Equals("text/html", StringComparison.OrdinalIgnoreCase))
            {
                // HTML thumbnail generation
                if (!await _htmlThumbnailService.IsAvailableAsync())
                {
                    _logger.LogWarning(
                        "HTML thumbnail service unavailable, skipping receipt {ReceiptId}",
                        receipt.Id);
                    return;
                }

                using var htmlStream = await _blobStorageService.DownloadAsync(receipt.BlobUrl);
                using var reader = new StreamReader(htmlStream);
                var htmlContent = await reader.ReadToEndAsync(ct);

                var sanitizedHtml = _htmlSanitizationService.Sanitize(htmlContent);
                thumbnailStream = await _htmlThumbnailService.GenerateThumbnailAsync(sanitizedHtml, ct: ct);
            }
            else if (_thumbnailService.CanGenerateThumbnail(receipt.ContentType))
            {
                // Image/PDF thumbnail generation
                using var sourceStream = await _blobStorageService.DownloadAsync(receipt.BlobUrl);
                thumbnailStream = await _thumbnailService.GenerateThumbnailAsync(
                    sourceStream, receipt.ContentType);
            }
            else
            {
                _logger.LogDebug(
                    "Unsupported content type {ContentType} for receipt {ReceiptId}",
                    receipt.ContentType, receipt.Id);
                return;
            }

            if (thumbnailStream != null)
            {
                var thumbnailPath = BlobStorageService.GenerateThumbnailPath(receipt.UserId, receipt.Id);
                receipt.ThumbnailUrl = await _blobStorageService.UploadAsync(
                    thumbnailStream, thumbnailPath, "image/jpeg");
                await _receiptRepository.UpdateAsync(receipt);

                _logger.LogDebug(
                    "Generated thumbnail for receipt {ReceiptId}: {ThumbnailUrl}",
                    receipt.Id, receipt.ThumbnailUrl);
            }
        }
        finally
        {
            if (thumbnailStream != null)
            {
                await thumbnailStream.DisposeAsync();
            }
        }
    }
}
