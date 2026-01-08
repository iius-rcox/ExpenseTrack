using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Core.Services;

/// <summary>
/// Service for managing receipt operations including upload, retrieval, and deletion.
/// </summary>
public class ReceiptService : IReceiptService
{
    private readonly IReceiptRepository _receiptRepository;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IHeicConversionService _heicConversionService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IExtractionCorrectionService _correctionService;
    private readonly ILogger<ReceiptService> _logger;
    private readonly int _maxFileSizeBytes;
    private readonly HashSet<string> _allowedContentTypes;

    public ReceiptService(
        IReceiptRepository receiptRepository,
        IBlobStorageService blobStorageService,
        IHeicConversionService heicConversionService,
        IBackgroundJobClient backgroundJobClient,
        IExtractionCorrectionService correctionService,
        IConfiguration configuration,
        ILogger<ReceiptService> logger)
    {
        _receiptRepository = receiptRepository;
        _blobStorageService = blobStorageService;
        _heicConversionService = heicConversionService;
        _backgroundJobClient = backgroundJobClient;
        _correctionService = correctionService;
        _logger = logger;

        var maxSizeMb = configuration.GetValue<int>("ReceiptProcessing:MaxFileSizeMB", 25);
        _maxFileSizeBytes = maxSizeMb * 1024 * 1024;

        var allowedTypes = configuration.GetSection("ReceiptProcessing:AllowedContentTypes").Get<string[]>()
            ?? new[] { "image/jpeg", "image/png", "image/heic", "image/heif", "application/pdf" };
        _allowedContentTypes = new HashSet<string>(allowedTypes, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<Receipt> UploadReceiptAsync(Stream stream, string filename, string contentType, Guid userId)
    {
        // Validate content type
        if (!_allowedContentTypes.Contains(contentType))
        {
            throw new ArgumentException($"Content type '{contentType}' is not allowed. Allowed types: {string.Join(", ", _allowedContentTypes)}");
        }

        // Read stream to memory to check size and potentially convert
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        // Validate file size
        if (memoryStream.Length > _maxFileSizeBytes)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size of {_maxFileSizeBytes / 1024 / 1024}MB");
        }

        // Validate magic bytes match content type
        memoryStream.Position = 0;
        if (!ValidateMagicBytes(memoryStream, contentType))
        {
            throw new ArgumentException("File content does not match the declared content type");
        }

        Stream uploadStream = memoryStream;
        var finalContentType = contentType;
        var finalFilename = filename;

        // Convert HEIC to JPEG if needed
        if (_heicConversionService.IsHeicFormat(contentType))
        {
            _logger.LogInformation("Converting HEIC image to JPEG for file {Filename}", filename);
            memoryStream.Position = 0;
            uploadStream = await _heicConversionService.ConvertToJpegAsync(memoryStream);
            finalContentType = "image/jpeg";
            finalFilename = Path.ChangeExtension(filename, ".jpg");
        }

        // Generate blob path and upload
        uploadStream.Position = 0;
        var blobPath = _blobStorageService.GenerateReceiptPath(userId, finalFilename);
        var blobUrl = await _blobStorageService.UploadAsync(uploadStream, blobPath, finalContentType);

        // Create receipt entity
        var receipt = new Receipt
        {
            UserId = userId,
            BlobUrl = blobUrl,
            OriginalFilename = filename,
            ContentType = finalContentType,
            FileSize = uploadStream.Length,
            Status = ReceiptStatus.Uploaded,
            CreatedAt = DateTime.UtcNow
        };

        await _receiptRepository.AddAsync(receipt);

        _logger.LogInformation(
            "Receipt {ReceiptId} uploaded successfully for user {UserId}. File: {Filename}, Size: {Size} bytes",
            receipt.Id, userId, filename, uploadStream.Length);

        // Queue background processing job
        _backgroundJobClient.Enqueue<IReceiptProcessingJob>(job => job.ProcessAsync(receipt.Id));

        return receipt;
    }

    public async Task<Receipt?> GetReceiptAsync(Guid id, Guid userId)
    {
        return await _receiptRepository.GetByIdAsync(id, userId);
    }

    public async Task<(List<Receipt> Items, int TotalCount)> GetReceiptsAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 20,
        ReceiptStatus? status = null,
        MatchStatus? matchStatus = null,
        string? vendor = null,
        DateOnly? receiptDateFrom = null,
        DateOnly? receiptDateTo = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? sortBy = null,
        string? sortOrder = null)
    {
        return await _receiptRepository.GetPagedAsync(
            userId, pageNumber, pageSize, status, matchStatus, vendor,
            receiptDateFrom, receiptDateTo, fromDate, toDate, sortBy, sortOrder);
    }

    public async Task<bool> DeleteReceiptAsync(Guid id, Guid userId)
    {
        var receipt = await _receiptRepository.GetByIdAsync(id, userId);
        if (receipt == null)
        {
            return false;
        }

        // Delete blob files
        try
        {
            await _blobStorageService.DeleteAsync(receipt.BlobUrl);
            if (!string.IsNullOrEmpty(receipt.ThumbnailUrl))
            {
                await _blobStorageService.DeleteAsync(receipt.ThumbnailUrl);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete blob files for receipt {ReceiptId}", id);
        }

        await _receiptRepository.DeleteAsync(id, userId);

        _logger.LogInformation("Receipt {ReceiptId} deleted for user {UserId}", id, userId);
        return true;
    }

    public async Task<string> GetReceiptUrlAsync(Guid id, Guid userId, TimeSpan? expiry = null)
    {
        var receipt = await _receiptRepository.GetByIdAsync(id, userId);
        if (receipt == null)
        {
            throw new KeyNotFoundException($"Receipt {id} not found");
        }

        return await _blobStorageService.GenerateSasUrlAsync(receipt.BlobUrl, expiry ?? TimeSpan.FromHours(1));
    }

    public async Task<Dictionary<ReceiptStatus, int>> GetStatusCountsAsync(Guid userId)
    {
        return await _receiptRepository.GetStatusCountsAsync(userId);
    }

    public async Task<Receipt?> RetryReceiptAsync(Guid id, Guid userId)
    {
        var receipt = await _receiptRepository.GetByIdAsync(id, userId);
        if (receipt == null || receipt.Status != ReceiptStatus.Error)
        {
            return null;
        }

        // Reset status and queue for reprocessing
        receipt.Status = ReceiptStatus.Processing;
        receipt.ErrorMessage = null;
        await _receiptRepository.UpdateAsync(receipt);

        // Queue background processing job
        _backgroundJobClient.Enqueue<IReceiptProcessingJob>(job => job.ProcessAsync(receipt.Id));

        _logger.LogInformation("Receipt {ReceiptId} queued for retry (attempt {RetryCount})", id, receipt.RetryCount + 1);

        return receipt;
    }

    public async Task<Receipt?> TriggerProcessingAsync(Guid id, Guid userId)
    {
        var receipt = await _receiptRepository.GetByIdAsync(id, userId);
        if (receipt == null || receipt.Status != ReceiptStatus.Uploaded)
        {
            return null;
        }

        // Queue background processing job
        _backgroundJobClient.Enqueue<IReceiptProcessingJob>(job => job.ProcessAsync(receipt.Id));

        _logger.LogInformation("Receipt {ReceiptId} queued for processing", id);

        return receipt;
    }

    public async Task<Receipt?> UpdateReceiptAsync(Guid id, Guid userId, ReceiptUpdateRequestDto request)
    {
        var receipt = await _receiptRepository.GetByIdAsync(id, userId);
        if (receipt == null)
        {
            return null;
        }

        // Prevent editing while receipt is being processed
        if (receipt.Status == ReceiptStatus.Processing)
        {
            _logger.LogWarning(
                "Attempted to update receipt {ReceiptId} while processing",
                id);
            return null;
        }

        // Check for optimistic concurrency conflict
        if (request.RowVersion.HasValue && request.RowVersion.Value != receipt.RowVersion)
        {
            _logger.LogWarning(
                "Concurrency conflict on receipt {ReceiptId}: client version {ClientVersion}, server version {ServerVersion}",
                id, request.RowVersion.Value, receipt.RowVersion);
            throw new DbUpdateConcurrencyException(
                $"Receipt {id} has been modified by another user. Please refresh and try again.");
        }

        // Update fields if provided
        if (request.Vendor != null) receipt.VendorExtracted = request.Vendor;
        if (request.Date.HasValue) receipt.DateExtracted = DateOnly.FromDateTime(request.Date.Value);
        if (request.Amount.HasValue) receipt.AmountExtracted = request.Amount;
        if (request.Tax.HasValue) receipt.TaxExtracted = request.Tax;
        if (request.Currency != null) receipt.Currency = request.Currency;

        if (request.LineItems != null)
        {
            receipt.LineItems = request.LineItems.Select(li => new Entities.ReceiptLineItem
            {
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                TotalPrice = li.TotalPrice,
                Confidence = li.Confidence
            }).ToList();
        }

        // If status was Error or ReviewRequired, change to Ready
        if (receipt.Status is ReceiptStatus.Error or ReceiptStatus.ReviewRequired)
        {
            receipt.Status = ReceiptStatus.Ready;
        }

        await _receiptRepository.UpdateAsync(receipt);

        // Record corrections for training feedback if provided
        if (request.Corrections is { Count: > 0 })
        {
            var currentValues = new Dictionary<string, string?>
            {
                ["vendor"] = receipt.VendorExtracted,
                ["amount"] = receipt.AmountExtracted?.ToString(),
                ["date"] = receipt.DateExtracted?.ToString("yyyy-MM-dd"),
                ["tax"] = receipt.TaxExtracted?.ToString(),
                ["currency"] = receipt.Currency
            };

            await _correctionService.RecordCorrectionsAsync(
                id,
                userId,
                request.Corrections,
                currentValues);
        }

        _logger.LogInformation("Receipt {ReceiptId} updated by user {UserId}", id, userId);

        return receipt;
    }

    private static bool ValidateMagicBytes(Stream stream, string contentType)
    {
        var buffer = new byte[12];
        var bytesRead = stream.Read(buffer, 0, buffer.Length);
        stream.Position = 0;

        if (bytesRead < 4)
        {
            return false;
        }

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF,
            "image/png" => buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47,
            "application/pdf" => buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46,
            "image/heic" or "image/heif" => ValidateHeicMagicBytes(buffer),
            "text/html" or "application/xhtml+xml" or "application/x-html" => ValidateHtmlMagicBytes(stream),
            _ => true // Allow unknown types to pass through
        };
    }

    private static bool ValidateHeicMagicBytes(byte[] buffer)
    {
        // HEIC files have 'ftyp' at offset 4 followed by 'heic', 'heix', 'mif1', or 'msf1'
        if (buffer.Length < 12)
        {
            return false;
        }

        // Check for 'ftyp' at offset 4
        if (buffer[4] != 0x66 || buffer[5] != 0x74 || buffer[6] != 0x79 || buffer[7] != 0x70)
        {
            return false;
        }

        // Check for HEIC/HEIF brand identifiers
        var brand = System.Text.Encoding.ASCII.GetString(buffer, 8, 4);
        return brand is "heic" or "heix" or "mif1" or "msf1" or "hevc" or "hevx";
    }

    private static bool ValidateHtmlMagicBytes(Stream stream)
    {
        // Read more bytes for HTML detection (may have BOM or whitespace)
        var buffer = new byte[256];
        stream.Position = 0;
        var bytesRead = stream.Read(buffer, 0, buffer.Length);
        stream.Position = 0;

        if (bytesRead < 10)
        {
            return false;
        }

        // Convert to string and trim leading whitespace/BOM
        var content = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead).TrimStart();

        // Check for common HTML start patterns (case-insensitive)
        return content.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
            || content.StartsWith("<html", StringComparison.OrdinalIgnoreCase)
            || content.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
            || content.StartsWith("<head", StringComparison.OrdinalIgnoreCase)
            || content.StartsWith("<body", StringComparison.OrdinalIgnoreCase)
            || content.StartsWith("<!--", StringComparison.Ordinal); // HTML comment at start
    }
}
