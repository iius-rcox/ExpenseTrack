using System.Globalization;
using System.Text.RegularExpressions;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Core.Services;
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
/// Supports HTML receipts via AI-based extraction.
/// </summary>
public class ProcessReceiptJob : IReceiptProcessingJob
{
    private readonly IReceiptRepository _receiptRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IDocumentIntelligenceService _documentIntelligenceService;
    private readonly IThumbnailService _thumbnailService;
    private readonly IHtmlReceiptExtractionService _htmlExtractionService;
    private readonly IHtmlThumbnailService _htmlThumbnailService;
    private readonly ITravelDetectionService _travelDetectionService;
    private readonly IVendorAliasService _vendorAliasService;
    private readonly ILogger<ProcessReceiptJob> _logger;
    private readonly double _confidenceThreshold;
    private readonly int _maxRetries;
    private readonly bool _enableInvoiceFallback;
    private readonly double _fallbackConfidenceThreshold;
    private readonly bool _preferInvoiceForMultiPage;

    public ProcessReceiptJob(
        IReceiptRepository receiptRepository,
        IBlobStorageService blobStorageService,
        IDocumentIntelligenceService documentIntelligenceService,
        IThumbnailService thumbnailService,
        IHtmlReceiptExtractionService htmlExtractionService,
        IHtmlThumbnailService htmlThumbnailService,
        ITravelDetectionService travelDetectionService,
        IVendorAliasService vendorAliasService,
        IConfiguration configuration,
        ILogger<ProcessReceiptJob> logger)
    {
        _receiptRepository = receiptRepository;
        _blobStorageService = blobStorageService;
        _documentIntelligenceService = documentIntelligenceService;
        _thumbnailService = thumbnailService;
        _htmlExtractionService = htmlExtractionService;
        _htmlThumbnailService = htmlThumbnailService;
        _travelDetectionService = travelDetectionService;
        _vendorAliasService = vendorAliasService;
        _logger = logger;

        _confidenceThreshold = configuration.GetValue<double>("ReceiptProcessing:ConfidenceThreshold", 0.60);
        _maxRetries = configuration.GetValue<int>("ReceiptProcessing:MaxRetries", 3);

        // Invoice fallback configuration
        _enableInvoiceFallback = configuration.GetValue<bool>("ReceiptProcessing:Extraction:EnableInvoiceFallback", true);
        _fallbackConfidenceThreshold = configuration.GetValue<double>("ReceiptProcessing:Extraction:FallbackConfidenceThreshold", 0.50);
        _preferInvoiceForMultiPage = configuration.GetValue<bool>("ReceiptProcessing:Extraction:PreferInvoiceForMultiPage", true);
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

            // Branch processing based on content type
            ReceiptExtractionResult extractionResult;

            if (receipt.ContentType.Equals("text/html", StringComparison.OrdinalIgnoreCase) ||
                receipt.ContentType.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase))
            {
                // HTML receipt processing via AI extraction
                extractionResult = await ProcessHtmlReceiptAsync(receipt);
            }
            else
            {
                // Image/PDF receipt processing via Document Intelligence
                // Use fallback method if enabled to try invoice model when receipt model fails
                using var imageStream = await _blobStorageService.DownloadAsync(receipt.BlobUrl);

                if (_enableInvoiceFallback)
                {
                    _logger.LogDebug("Invoice fallback enabled, using AnalyzeWithFallbackAsync for receipt {ReceiptId}", receipt.Id);
                    extractionResult = await _documentIntelligenceService.AnalyzeWithFallbackAsync(
                        imageStream,
                        receipt.ContentType,
                        _fallbackConfidenceThreshold);

                    // Log which model was used
                    _logger.LogInformation(
                        "Receipt {ReceiptId} extracted using model: {Model}, field sources: {Sources}",
                        receipt.Id,
                        extractionResult.ExtractionModel,
                        string.Join(", ", extractionResult.FieldSources.Select(kv => $"{kv.Key}={kv.Value}")));
                }
                else
                {
                    extractionResult = await _documentIntelligenceService.AnalyzeReceiptAsync(imageStream, receipt.ContentType);
                }
            }

            // Update receipt with extracted data
            // BUG-004 fix: If vendor is missing, try to extract from filename or use fallback patterns
            var rawVendor = extractionResult.VendorName ?? ExtractVendorFromFilename(receipt.OriginalFilename);

            // Normalize vendor name using VendorAlias lookup (e.g., "STRIPE*ANTHROPIC" → "Anthropic Claude")
            receipt.VendorExtracted = await NormalizeVendorNameAsync(rawVendor);

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
            await GenerateThumbnailAsync(receipt);

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

    /// <summary>
    /// Normalizes vendor name using VendorAlias lookup.
    /// Maps raw extracted values (e.g., "STRIPE*ANTHROPIC") to canonical names (e.g., "Anthropic Claude").
    /// </summary>
    /// <param name="rawVendor">Raw vendor name from OCR extraction.</param>
    /// <returns>Normalized vendor name if alias found, otherwise returns original value.</returns>
    private async Task<string?> NormalizeVendorNameAsync(string? rawVendor)
    {
        if (string.IsNullOrWhiteSpace(rawVendor))
            return rawVendor;

        try
        {
            var alias = await _vendorAliasService.FindMatchingAliasAsync(rawVendor);
            if (alias != null)
            {
                _logger.LogInformation(
                    "Vendor normalized via alias: '{RawVendor}' → '{NormalizedVendor}' (pattern: {Pattern})",
                    rawVendor,
                    alias.DisplayName,
                    alias.AliasPattern);

                // Record the match for frequency tracking
                await _vendorAliasService.RecordMatchAsync(alias.Id);

                return alias.DisplayName;
            }
        }
        catch (Exception ex)
        {
            // Don't fail receipt processing if alias lookup fails
            _logger.LogWarning(ex, "Failed to lookup vendor alias for '{RawVendor}', using original value", rawVendor);
        }

        return rawVendor;
    }

    /// <summary>
    /// Processes an HTML receipt using AI-based extraction.
    /// Downloads HTML content from blob storage and extracts receipt data via Azure OpenAI.
    /// </summary>
    private async Task<ReceiptExtractionResult> ProcessHtmlReceiptAsync(Core.Entities.Receipt receipt)
    {
        _logger.LogInformation("Processing HTML receipt {ReceiptId}", receipt.Id);

        // Download HTML content
        using var htmlStream = await _blobStorageService.DownloadAsync(receipt.BlobUrl);
        using var reader = new StreamReader(htmlStream);
        var htmlContent = await reader.ReadToEndAsync();

        // Extract receipt data using AI
        var (result, metrics) = await _htmlExtractionService.ExtractWithMetricsAsync(
            htmlContent,
            receipt.Id);

        // Log extraction metrics
        _logger.LogInformation(
            "HTML extraction metrics for receipt {ReceiptId}: " +
            "Success: {Success}, Confidence: {Confidence:P0}, Fields: {Fields}, " +
            "HtmlSize: {HtmlSize}, TextLength: {TextLength}, Time: {Time}ms",
            receipt.Id,
            metrics.Success,
            metrics.OverallConfidence ?? 0,
            metrics.FieldsExtracted,
            metrics.HtmlSizeBytes,
            metrics.TextContentLength,
            metrics.ProcessingTime.TotalMilliseconds);

        return result;
    }

    /// <summary>
    /// Generates and uploads a thumbnail for the receipt based on its content type.
    /// Supports both image/PDF (via ThumbnailService) and HTML (via HtmlThumbnailService).
    /// </summary>
    private async Task GenerateThumbnailAsync(Core.Entities.Receipt receipt)
    {
        try
        {
            Stream? thumbnailStream = null;

            if (receipt.ContentType.Equals("text/html", StringComparison.OrdinalIgnoreCase) ||
                receipt.ContentType.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase))
            {
                // HTML thumbnail generation via headless browser
                if (await _htmlThumbnailService.IsAvailableAsync())
                {
                    using var htmlStream = await _blobStorageService.DownloadAsync(receipt.BlobUrl);
                    using var reader = new StreamReader(htmlStream);
                    var htmlContent = await reader.ReadToEndAsync();

                    thumbnailStream = await _htmlThumbnailService.GenerateThumbnailAsync(htmlContent);
                    _logger.LogDebug("Generated HTML thumbnail for receipt {ReceiptId}", receipt.Id);
                }
                else
                {
                    _logger.LogWarning(
                        "HTML thumbnail service unavailable for receipt {ReceiptId}. " +
                        "Chromium may not be installed.",
                        receipt.Id);
                    return;
                }
            }
            else if (_thumbnailService.CanGenerateThumbnail(receipt.ContentType))
            {
                // Image/PDF thumbnail generation
                using var sourceStream = await _blobStorageService.DownloadAsync(receipt.BlobUrl);
                thumbnailStream = await _thumbnailService.GenerateThumbnailAsync(sourceStream, receipt.ContentType);
                _logger.LogDebug("Generated image thumbnail for receipt {ReceiptId}", receipt.Id);
            }
            else
            {
                _logger.LogDebug(
                    "No thumbnail generation available for receipt {ReceiptId} with content type {ContentType}",
                    receipt.Id, receipt.ContentType);
                return;
            }

            if (thumbnailStream != null)
            {
                var thumbnailPath = BlobStorageService.GenerateThumbnailPath(receipt.UserId, receipt.Id);
                receipt.ThumbnailUrl = await _blobStorageService.UploadAsync(thumbnailStream, thumbnailPath, "image/jpeg");
                await thumbnailStream.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail the whole process for thumbnail failures
            _logger.LogWarning(ex, "Failed to generate thumbnail for receipt {ReceiptId}", receipt.Id);
        }
    }
}
