using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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

        // Compute file hash for duplicate detection (stored for future checks)
        memoryStream.Position = 0;
        var fileHash = await ComputeFileHashAsync(memoryStream);

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

        // Create receipt entity with file hash
        var receipt = new Receipt
        {
            UserId = userId,
            BlobUrl = blobUrl,
            OriginalFilename = filename,
            ContentType = finalContentType,
            FileSize = uploadStream.Length,
            FileHash = fileHash,
            Status = ReceiptStatus.Uploaded,
            CreatedAt = DateTime.UtcNow
        };

        await _receiptRepository.AddAsync(receipt);

        _logger.LogInformation(
            "Receipt {ReceiptId} uploaded successfully for user {UserId}. File: {Filename}, Size: {Size} bytes, Hash: {FileHash}",
            receipt.Id, userId, filename, uploadStream.Length, fileHash);

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

        // Update content hash when extraction data changes
        if (request.Vendor != null || request.Date.HasValue || request.Amount.HasValue)
        {
            receipt.ContentHash = ComputeContentHash(
                receipt.VendorExtracted,
                receipt.DateExtracted,
                receipt.AmountExtracted);
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

    #region Duplicate Detection

    /// <summary>
    /// Computes SHA-256 hash of file content for exact duplicate detection.
    /// </summary>
    /// <param name="stream">File content stream</param>
    /// <returns>Lowercase hex string of SHA-256 hash (64 characters)</returns>
    public static async Task<string> ComputeFileHashAsync(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        stream.Position = 0; // Reset stream position for subsequent reading

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Computes SHA-256 hash of normalized content (vendor|date|amount) for semantic duplicate detection.
    /// </summary>
    /// <param name="vendor">Extracted vendor name (will be normalized: trimmed, lowercased)</param>
    /// <param name="date">Extracted transaction date (formatted as yyyy-MM-dd)</param>
    /// <param name="amount">Extracted amount (formatted with 2 decimal places)</param>
    /// <returns>Lowercase hex string of SHA-256 hash (64 characters)</returns>
    public static string ComputeContentHash(string? vendor, DateOnly? date, decimal? amount)
    {
        // Normalize vendor: trim whitespace, convert to lowercase
        var normalizedVendor = vendor?.Trim().ToLowerInvariant() ?? string.Empty;

        // Format date as ISO 8601 (yyyy-MM-dd) or empty string if null
        var formattedDate = date?.ToString("yyyy-MM-dd") ?? string.Empty;

        // Format amount with exactly 2 decimal places using InvariantCulture for consistent hashing
        var formattedAmount = amount?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty;

        // Create content string with pipe separator
        var content = $"{normalizedVendor}|{formattedDate}|{formattedAmount}";

        // Compute SHA-256 hash
        var contentBytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = SHA256.HashData(contentBytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Checks if a receipt with the same content hash already exists.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="vendor">Extracted vendor name</param>
    /// <param name="date">Extracted transaction date</param>
    /// <param name="amount">Extracted amount</param>
    /// <returns>Duplicate check result</returns>
    public async Task<DuplicateCheckResult> CheckContentDuplicateAsync(
        Guid userId,
        string? vendor,
        DateOnly? date,
        decimal? amount)
    {
        var contentHash = ComputeContentHash(vendor, date, amount);
        var existingReceipt = await _receiptRepository.FindByContentHashAsync(contentHash, userId);

        if (existingReceipt != null)
        {
            return new DuplicateCheckResult
            {
                IsDuplicate = true,
                DuplicateType = DuplicateType.SameContent,
                ExistingReceiptId = existingReceipt.Id,
                ContentHash = contentHash
            };
        }

        return new DuplicateCheckResult
        {
            IsDuplicate = false,
            DuplicateType = DuplicateType.None,
            ContentHash = contentHash
        };
    }

    /// <summary>
    /// Uploads a receipt with duplicate detection.
    /// </summary>
    /// <param name="stream">File content stream</param>
    /// <param name="filename">Original filename</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <param name="userId">User ID uploading the receipt</param>
    /// <param name="allowDuplicates">If true, allows duplicate uploads without returning conflict</param>
    /// <returns>Upload result with receipt or duplicate information</returns>
    public async Task<ReceiptUploadResult> UploadReceiptAsync(
        Stream stream,
        string filename,
        string contentType,
        Guid userId,
        bool allowDuplicates)
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

        // Compute file hash for duplicate detection
        memoryStream.Position = 0;
        var fileHash = await ComputeFileHashAsync(memoryStream);

        // Check for exact duplicate (unless allowDuplicates is true)
        if (!allowDuplicates)
        {
            var existingReceipt = await _receiptRepository.FindByFileHashAsync(fileHash, userId);
            if (existingReceipt != null)
            {
                _logger.LogInformation(
                    "Duplicate file detected for user {UserId}. Existing receipt: {ExistingReceiptId}, File hash: {FileHash}",
                    userId, existingReceipt.Id, fileHash);

                return new ReceiptUploadResult
                {
                    IsDuplicate = true,
                    DuplicateType = DuplicateType.ExactFile,
                    ExistingReceiptId = existingReceipt.Id,
                    Receipt = null
                };
            }
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

        // Create receipt entity with file hash
        var receipt = new Receipt
        {
            UserId = userId,
            BlobUrl = blobUrl,
            OriginalFilename = filename,
            ContentType = finalContentType,
            FileSize = uploadStream.Length,
            FileHash = fileHash,
            Status = ReceiptStatus.Uploaded,
            CreatedAt = DateTime.UtcNow
        };

        await _receiptRepository.AddAsync(receipt);

        _logger.LogInformation(
            "Receipt {ReceiptId} uploaded successfully for user {UserId}. File: {Filename}, Size: {Size} bytes, Hash: {FileHash}",
            receipt.Id, userId, filename, uploadStream.Length, fileHash);

        // Queue background processing job
        _backgroundJobClient.Enqueue<IReceiptProcessingJob>(job => job.ProcessAsync(receipt.Id));

        return new ReceiptUploadResult
        {
            IsDuplicate = false,
            DuplicateType = DuplicateType.None,
            Receipt = receipt
        };
    }

    /// <summary>
    /// Backfills file hashes for existing receipts that don't have one.
    /// </summary>
    /// <param name="batchSize">Maximum number of receipts to process in this batch</param>
    /// <returns>Number of receipts processed</returns>
    public async Task<int> BackfillFileHashesAsync(int batchSize = 50)
    {
        var receipts = await _receiptRepository.GetReceiptsWithoutFileHashAsync(batchSize);
        var processed = 0;

        foreach (var receipt in receipts)
        {
            try
            {
                if (string.IsNullOrEmpty(receipt.BlobUrl))
                    continue;

                // Download the file from blob storage
                using var downloadStream = await _blobStorageService.DownloadAsync(receipt.BlobUrl);

                // Copy to MemoryStream since blob streams are non-seekable
                using var memoryStream = new MemoryStream();
                await downloadStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Compute the file hash
                var fileHash = await ComputeFileHashAsync(memoryStream);
                receipt.FileHash = fileHash;

                await _receiptRepository.UpdateAsync(receipt);
                processed++;

                _logger.LogDebug("Backfilled file hash for receipt {ReceiptId}: {FileHash}",
                    receipt.Id, fileHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backfilling file hash for receipt {ReceiptId}", receipt.Id);
            }
        }

        _logger.LogInformation("Backfilled file hashes for {Processed}/{Total} receipts",
            processed, receipts.Count);

        return processed;
    }

    #endregion
}

/// <summary>
/// Result of a receipt upload operation with duplicate detection.
/// </summary>
public class ReceiptUploadResult
{
    /// <summary>Whether a duplicate was detected</summary>
    public bool IsDuplicate { get; set; }

    /// <summary>Type of duplicate if detected</summary>
    public DuplicateType DuplicateType { get; set; }

    /// <summary>ID of the existing receipt if duplicate</summary>
    public Guid? ExistingReceiptId { get; set; }

    /// <summary>The uploaded receipt (null if duplicate and not allowed)</summary>
    public Receipt? Receipt { get; set; }
}

/// <summary>
/// Result of a content duplicate check.
/// </summary>
public class DuplicateCheckResult
{
    /// <summary>Whether a duplicate was detected</summary>
    public bool IsDuplicate { get; set; }

    /// <summary>Type of duplicate if detected</summary>
    public DuplicateType DuplicateType { get; set; }

    /// <summary>ID of the existing receipt if duplicate</summary>
    public Guid? ExistingReceiptId { get; set; }

    /// <summary>The computed content hash</summary>
    public string? ContentHash { get; set; }
}

/// <summary>
/// Types of duplicate detection.
/// </summary>
public enum DuplicateType
{
    /// <summary>No duplicate found</summary>
    None,

    /// <summary>Exact file duplicate (same file hash)</summary>
    ExactFile,

    /// <summary>Semantic duplicate (same vendor + date + amount)</summary>
    SameContent
}
