using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Services;
using ExpenseFlow.Shared.Enums;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Jobs;

/// <summary>
/// Hangfire background job for processing receipt images through Document Intelligence.
/// Extracts vendor, date, amount, and line items from receipt images.
/// </summary>
public class ProcessReceiptJob : IReceiptProcessingJob
{
    private readonly IReceiptRepository _receiptRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IDocumentIntelligenceService _documentIntelligenceService;
    private readonly IThumbnailService _thumbnailService;
    private readonly ILogger<ProcessReceiptJob> _logger;
    private readonly double _confidenceThreshold;
    private readonly int _maxRetries;

    public ProcessReceiptJob(
        IReceiptRepository receiptRepository,
        IBlobStorageService blobStorageService,
        IDocumentIntelligenceService documentIntelligenceService,
        IThumbnailService thumbnailService,
        IConfiguration configuration,
        ILogger<ProcessReceiptJob> logger)
    {
        _receiptRepository = receiptRepository;
        _blobStorageService = blobStorageService;
        _documentIntelligenceService = documentIntelligenceService;
        _thumbnailService = thumbnailService;
        _logger = logger;

        _confidenceThreshold = configuration.GetValue<double>("ReceiptProcessing:ConfidenceThreshold", 0.60);
        _maxRetries = configuration.GetValue<int>("ReceiptProcessing:MaxRetries", 3);
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ProcessAsync(Guid receiptId)
    {
        _logger.LogInformation("Starting processing for receipt {ReceiptId}", receiptId);

        var receipt = await _receiptRepository.GetByIdAsync(receiptId);
        if (receipt == null)
        {
            _logger.LogWarning("Receipt {ReceiptId} not found", receiptId);
            return;
        }

        // Check if already processed
        if (receipt.Status is ReceiptStatus.Ready or ReceiptStatus.ReviewRequired)
        {
            _logger.LogInformation("Receipt {ReceiptId} already processed with status {Status}", receiptId, receipt.Status);
            return;
        }

        // Check retry count
        if (receipt.RetryCount >= _maxRetries)
        {
            _logger.LogWarning("Receipt {ReceiptId} exceeded max retries ({MaxRetries})", receiptId, _maxRetries);
            receipt.Status = ReceiptStatus.Error;
            receipt.ErrorMessage = $"Processing failed after {_maxRetries} retries";
            await _receiptRepository.UpdateAsync(receipt);
            return;
        }

        try
        {
            // Update status to Processing
            receipt.Status = ReceiptStatus.Processing;
            receipt.RetryCount++;
            await _receiptRepository.UpdateAsync(receipt);

            // Download the receipt image from blob storage
            using var imageStream = await _blobStorageService.DownloadAsync(receipt.BlobUrl);

            // Extract data using Document Intelligence
            var extractionResult = await _documentIntelligenceService.AnalyzeReceiptAsync(imageStream, receipt.ContentType);

            // Update receipt with extracted data
            receipt.VendorExtracted = extractionResult.VendorName;
            receipt.DateExtracted = extractionResult.TransactionDate.HasValue
                ? DateOnly.FromDateTime(extractionResult.TransactionDate.Value)
                : null;
            receipt.AmountExtracted = extractionResult.TotalAmount;
            receipt.TaxExtracted = extractionResult.TaxAmount;
            receipt.Currency = extractionResult.Currency ?? "USD";
            receipt.LineItems = extractionResult.LineItems;
            receipt.ConfidenceScores = extractionResult.ConfidenceScores;
            receipt.PageCount = extractionResult.PageCount;
            receipt.ProcessedAt = DateTime.UtcNow;

            // Determine status based on confidence threshold
            receipt.Status = extractionResult.RequiresReview(_confidenceThreshold)
                ? ReceiptStatus.ReviewRequired
                : ReceiptStatus.Ready;

            receipt.ErrorMessage = null;

            // Generate and upload thumbnail
            if (_thumbnailService.CanGenerateThumbnail(receipt.ContentType))
            {
                try
                {
                    // Re-download for thumbnail (the previous stream may have been consumed)
                    using var thumbSourceStream = await _blobStorageService.DownloadAsync(receipt.BlobUrl);
                    using var thumbnailStream = await _thumbnailService.GenerateThumbnailAsync(thumbSourceStream, receipt.ContentType);

                    var thumbnailPath = BlobStorageService.GenerateThumbnailPath(receipt.UserId, receipt.Id);
                    receipt.ThumbnailUrl = await _blobStorageService.UploadAsync(thumbnailStream, thumbnailPath, "image/jpeg");

                    _logger.LogDebug("Generated thumbnail for receipt {ReceiptId}", receiptId);
                }
                catch (Exception thumbEx)
                {
                    // Log but don't fail the whole process for thumbnail failures
                    _logger.LogWarning(thumbEx, "Failed to generate thumbnail for receipt {ReceiptId}", receiptId);
                }
            }

            await _receiptRepository.UpdateAsync(receipt);

            _logger.LogInformation(
                "Receipt {ReceiptId} processed successfully. Status: {Status}, Confidence: {Confidence:P1}, Vendor: {Vendor}, Amount: {Amount}",
                receiptId,
                receipt.Status,
                extractionResult.OverallConfidence,
                receipt.VendorExtracted ?? "Unknown",
                receipt.AmountExtracted?.ToString("C") ?? "Unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing receipt {ReceiptId}", receiptId);

            receipt.Status = ReceiptStatus.Error;
            receipt.ErrorMessage = ex.Message;
            await _receiptRepository.UpdateAsync(receipt);

            // Re-throw to trigger Hangfire retry
            throw;
        }
    }
}
