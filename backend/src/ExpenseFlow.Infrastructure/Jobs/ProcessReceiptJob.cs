using System.Globalization;
using System.Text.RegularExpressions;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Services;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Jobs;

/// <summary>
/// Hangfire background job for processing receipt images through Document Intelligence.
/// Extracts vendor, date, amount, and line items from receipt images.
/// Also triggers travel period detection for airline/hotel receipts.
/// </summary>
public class ProcessReceiptJob : IReceiptProcessingJob
{
    private readonly IReceiptRepository _receiptRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IDocumentIntelligenceService _documentIntelligenceService;
    private readonly IThumbnailService _thumbnailService;
    private readonly ITravelDetectionService _travelDetectionService;
    private readonly ILogger<ProcessReceiptJob> _logger;
    private readonly double _confidenceThreshold;
    private readonly int _maxRetries;

    public ProcessReceiptJob(
        IReceiptRepository receiptRepository,
        IBlobStorageService blobStorageService,
        IDocumentIntelligenceService documentIntelligenceService,
        IThumbnailService thumbnailService,
        ITravelDetectionService travelDetectionService,
        IConfiguration configuration,
        ILogger<ProcessReceiptJob> logger)
    {
        _receiptRepository = receiptRepository;
        _blobStorageService = blobStorageService;
        _documentIntelligenceService = documentIntelligenceService;
        _thumbnailService = thumbnailService;
        _travelDetectionService = travelDetectionService;
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
            // BUG-004 fix: If vendor is missing, try to extract from filename or use fallback patterns
            receipt.VendorExtracted = extractionResult.VendorName ?? ExtractVendorFromFilename(receipt.OriginalFilename);

            // BUG-003 fix: Validate/correct OCR date using filename date
            // Parking receipts often have entry AND exit dates; OCR may pick entry date incorrectly
            // Photos taken at payment time have correct date in filename (YYYYMMDD_HHMMSS format)
            receipt.DateExtracted = ResolveExtractedDate(
                extractionResult.TransactionDate,
                receipt.OriginalFilename,
                receipt.Id);

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

            // Trigger travel period detection (Tier 1 - rule-based)
            await DetectTravelPeriodAsync(receipt);
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

    /// <summary>
    /// Attempts to detect a travel period from the processed receipt.
    /// Uses Tier 1 (rule-based) detection for airline/hotel vendors.
    /// </summary>
    private async Task DetectTravelPeriodAsync(Core.Entities.Receipt receipt)
    {
        try
        {
            _logger.LogDebug(
                "Tier 1 - Checking travel detection for receipt {ReceiptId}, vendor: {Vendor}",
                receipt.Id,
                receipt.VendorExtracted ?? "Unknown");

            var result = await _travelDetectionService.DetectFromReceiptAsync(receipt);

            if (result.Detected)
            {
                _logger.LogInformation(
                    "Tier 1 - Travel period detection: {Action} for receipt {ReceiptId}. " +
                    "Period: {StartDate} to {EndDate}, Destination: {Destination}, Confidence: {Confidence:P0}",
                    result.Action,
                    receipt.Id,
                    result.TravelPeriod?.StartDate,
                    result.TravelPeriod?.EndDate,
                    result.ExtractedDestination ?? "Unknown",
                    result.Confidence);
            }
            else
            {
                _logger.LogDebug(
                    "Tier 1 - No travel period detected for receipt {ReceiptId}: {Message}",
                    receipt.Id,
                    result.Message);
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail the main receipt processing for travel detection failures
            _logger.LogWarning(
                ex,
                "Tier 1 - Failed to detect travel period for receipt {ReceiptId}",
                receipt.Id);
        }
    }

    /// <summary>
    /// Resolves the correct transaction date by validating OCR date against filename date.
    /// For receipts with multiple dates (like parking entry/exit), prefers the later date
    /// since that typically represents the payment date.
    /// </summary>
    /// <remarks>
    /// BUG-003 fix: Parking receipts show both entry and exit dates. Azure Document Intelligence
    /// may extract the entry date instead of the payment/exit date. Mobile photos taken at
    /// payment time encode the correct date in the filename (YYYYMMDD_HHMMSS.ext format).
    /// </remarks>
    private DateOnly? ResolveExtractedDate(DateTime? ocrDate, string filename, Guid receiptId)
    {
        var filenameDate = TryParseDateFromFilename(filename);

        // If no OCR date, use filename date as fallback
        if (!ocrDate.HasValue)
        {
            if (filenameDate.HasValue)
            {
                _logger.LogInformation(
                    "Receipt {ReceiptId}: No OCR date found, using filename date {FilenameDate}",
                    receiptId, filenameDate.Value);
            }
            return filenameDate;
        }

        var ocrDateOnly = DateOnly.FromDateTime(ocrDate.Value);

        // If no filename date available, use OCR date
        if (!filenameDate.HasValue)
        {
            return ocrDateOnly;
        }

        // If OCR date is earlier than filename date, prefer filename date
        // This handles parking receipts where entry date (earlier) might be extracted instead of exit/payment date
        if (ocrDateOnly < filenameDate.Value)
        {
            var daysDifference = filenameDate.Value.DayNumber - ocrDateOnly.DayNumber;

            _logger.LogWarning(
                "Receipt {ReceiptId}: OCR date {OcrDate} is {DaysDiff} days before filename date {FilenameDate}. " +
                "Using filename date (likely payment date vs entry date for parking/travel receipts).",
                receiptId, ocrDateOnly, daysDifference, filenameDate.Value);

            return filenameDate.Value;
        }

        // If dates match or OCR date is later, use OCR date (it's probably correct)
        if (ocrDateOnly != filenameDate.Value)
        {
            _logger.LogDebug(
                "Receipt {ReceiptId}: OCR date {OcrDate} differs from filename date {FilenameDate} but is later, using OCR date",
                receiptId, ocrDateOnly, filenameDate.Value);
        }

        return ocrDateOnly;
    }

    /// <summary>
    /// Attempts to parse a date from common filename formats.
    /// Supports: YYYYMMDD_HHMMSS, YYYY-MM-DD, IMG_YYYYMMDD patterns.
    /// </summary>
    private DateOnly? TryParseDateFromFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return null;

        // Pattern 1: YYYYMMDD_HHMMSS (common for mobile photos)
        // Example: 20251211_212334.jpg
        var match = Regex.Match(filename, @"(\d{4})(\d{2})(\d{2})_\d{6}");
        if (match.Success)
        {
            if (int.TryParse(match.Groups[1].Value, out var year) &&
                int.TryParse(match.Groups[2].Value, out var month) &&
                int.TryParse(match.Groups[3].Value, out var day))
            {
                try
                {
                    return new DateOnly(year, month, day);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Invalid date components
                }
            }
        }

        // Pattern 2: YYYY-MM-DD anywhere in filename
        match = Regex.Match(filename, @"(\d{4})-(\d{2})-(\d{2})");
        if (match.Success)
        {
            if (int.TryParse(match.Groups[1].Value, out var year) &&
                int.TryParse(match.Groups[2].Value, out var month) &&
                int.TryParse(match.Groups[3].Value, out var day))
            {
                try
                {
                    return new DateOnly(year, month, day);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Invalid date components
                }
            }
        }

        // Pattern 3: IMG_YYYYMMDD (iPhone format)
        match = Regex.Match(filename, @"IMG_(\d{4})(\d{2})(\d{2})", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            if (int.TryParse(match.Groups[1].Value, out var year) &&
                int.TryParse(match.Groups[2].Value, out var month) &&
                int.TryParse(match.Groups[3].Value, out var day))
            {
                try
                {
                    return new DateOnly(year, month, day);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Invalid date components
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Attempts to extract a vendor name from the filename when OCR fails to extract it.
    /// This is a fallback for cases where the vendor is recognizable from naming patterns.
    /// </summary>
    /// <remarks>
    /// BUG-004 fix: Some receipts (like parking) have vendor info in address format
    /// that Azure Document Intelligence doesn't recognize as MerchantName.
    /// </remarks>
    private static string? ExtractVendorFromFilename(string filename)
    {
        // Currently returns null - filename typically doesn't contain vendor info
        // This method is a placeholder for future pattern matching if needed
        // (e.g., scanning-app generated filenames that include vendor)
        return null;
    }
}
