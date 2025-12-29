using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using ClosedXML.Excel;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Jobs;

/// <summary>
/// Hangfire background job for processing historical data imports during cache warming.
/// </summary>
public class CacheWarmingJob : JobBase
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<CacheWarmingJob> _logger;
    private readonly int _batchSize;
    private readonly float _similarityThreshold;

    // Expected columns in import file
    private const string ColDate = "Date";
    private const string ColDescription = "Description";
    private const string ColVendor = "Vendor";
    private const string ColAmount = "Amount";
    private const string ColGLCode = "GL Code";
    private const string ColDepartment = "Department";

    // Maximum length for error_log column (varchar(10000) - leave buffer for JSON structure)
    private const int MaxErrorLogLength = 9500;

    public CacheWarmingJob(
        ExpenseFlowDbContext dbContext,
        IEmbeddingService embeddingService,
        IConfiguration configuration,
        ILogger<CacheWarmingJob> logger)
        : base(logger)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _logger = logger;
        _batchSize = configuration.GetValue("CacheWarming:BatchSize", 100);
        _similarityThreshold = configuration.GetValue("CacheWarming:SimilarityThreshold", 0.98f);

        var connectionString = configuration["BlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("BlobStorage:ConnectionString is required");
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Use ProcessImportAsync instead.");
    }

    /// <summary>
    /// Processes a cache warming import job.
    /// </summary>
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public async Task ProcessImportAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        LogJobStart($"CacheWarmingJob-{jobId}");

        var job = await _dbContext.ImportJobs.FindAsync(new object[] { jobId }, cancellationToken);
        if (job == null)
        {
            _logger.LogError("Import job {JobId} not found", jobId);
            return;
        }

        // Check if job was cancelled
        if (job.Status == ImportJobStatus.Cancelled)
        {
            _logger.LogInformation("Import job {JobId} was cancelled, skipping", jobId);
            return;
        }

        try
        {
            // Update status to Processing
            job.Status = ImportJobStatus.Processing;
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Download and parse Excel file
            var records = await DownloadAndParseExcelAsync(job.BlobUrl, cancellationToken);
            job.TotalRecords = records.Count;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Parsed {RecordCount} records from import file", records.Count);

            var errors = new List<ImportError>();
            var processedCount = 0;

            // Process in batches
            foreach (var batch in records.Chunk(_batchSize))
            {
                // Check for cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    await RefreshJobStatus(job, cancellationToken);
                    if (job.Status == ImportJobStatus.Cancelled)
                    {
                        _logger.LogInformation("Import job {JobId} cancelled during processing", jobId);
                        return;
                    }
                }

                await ProcessBatchAsync(job, batch.ToList(), errors, cancellationToken);

                processedCount += batch.Length;
                job.ProcessedRecords = processedCount;
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Processed batch: {Processed}/{Total}", processedCount, job.TotalRecords);
            }

            // Finalize job
            job.Status = ImportJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorLog = TruncateErrorLog(errors);
            job.SkippedRecords = errors.Count;

            await _dbContext.SaveChangesAsync(cancellationToken);

            LogJobComplete($"CacheWarmingJob-{jobId}", DateTime.UtcNow - startTime);
            _logger.LogInformation(
                "Import job {JobId} completed: {Cached} descriptions, {Aliases} aliases, {Embeddings} embeddings, {Skipped} skipped",
                jobId, job.CachedDescriptions, job.CreatedAliases, job.GeneratedEmbeddings, job.SkippedRecords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import job {JobId} failed", jobId);
            job.Status = ImportJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            job.ErrorLog = TruncateErrorLog(new List<ImportError>
            {
                new(0, $"Job failed: {ex.Message}", null)
            });
            await _dbContext.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task RefreshJobStatus(ImportJob job, CancellationToken cancellationToken)
    {
        await _dbContext.Entry(job).ReloadAsync(cancellationToken);
    }

    private async Task<List<HistoricalRecord>> DownloadAndParseExcelAsync(string blobUrl, CancellationToken cancellationToken)
    {
        var uri = new Uri(blobUrl);
        var containerName = uri.Segments[1].TrimEnd('/');
        var blobName = string.Join("", uri.Segments.Skip(2));

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        using var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var records = new List<HistoricalRecord>();

        using var workbook = new XLWorkbook(memoryStream);
        var worksheet = workbook.Worksheets.First();

        // Find header row and column indices
        var headerRow = worksheet.FirstRowUsed();
        if (headerRow == null)
        {
            throw new InvalidOperationException("Excel file has no data rows");
        }

        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var header = cell.Value.ToString().Trim();
            columnMap[header] = cell.Address.ColumnNumber;
        }

        // Validate required columns
        var requiredColumns = new[] { ColDescription };
        foreach (var col in requiredColumns)
        {
            if (!columnMap.ContainsKey(col))
            {
                throw new InvalidOperationException($"Required column '{col}' not found in Excel file");
            }
        }

        // Parse data rows
        var lineNumber = 1;
        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            lineNumber++;
            try
            {
                var description = GetCellValue(row, columnMap, ColDescription);
                if (string.IsNullOrWhiteSpace(description) || description.Length < 3)
                {
                    continue; // Skip empty descriptions
                }

                records.Add(new HistoricalRecord
                {
                    LineNumber = lineNumber,
                    Date = TryParseDate(GetCellValue(row, columnMap, ColDate)),
                    Description = description,
                    Vendor = GetCellValue(row, columnMap, ColVendor),
                    Amount = TryParseDecimal(GetCellValue(row, columnMap, ColAmount)),
                    GLCode = GetCellValue(row, columnMap, ColGLCode),
                    Department = GetCellValue(row, columnMap, ColDepartment)
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse row {LineNumber}", lineNumber);
            }
        }

        return records;
    }

    private static string GetCellValue(IXLRow row, Dictionary<string, int> columnMap, string columnName)
    {
        if (!columnMap.TryGetValue(columnName, out var colIndex))
        {
            return string.Empty;
        }
        return row.Cell(colIndex).Value.ToString().Trim();
    }

    private static DateTime? TryParseDate(string value)
    {
        if (DateTime.TryParse(value, out var date))
        {
            return date;
        }
        return null;
    }

    private static decimal? TryParseDecimal(string value)
    {
        if (decimal.TryParse(value, out var amount))
        {
            return amount;
        }
        return null;
    }

    private async Task ProcessBatchAsync(
        ImportJob job,
        List<HistoricalRecord> batch,
        List<ImportError> errors,
        CancellationToken cancellationToken)
    {
        // Track hashes processed in this batch to avoid duplicates within the same batch
        var processedDescriptionHashes = new HashSet<string>();
        var processedVendorAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Process descriptions for DescriptionCache
        foreach (var record in batch)
        {
            try
            {
                var descriptionCached = await ProcessDescriptionAsync(record, processedDescriptionHashes, cancellationToken);
                if (descriptionCached)
                {
                    job.CachedDescriptions++;
                }
            }
            catch (Exception ex)
            {
                errors.Add(new ImportError(record.LineNumber, $"Description error: {ex.Message}", record.Description));
            }
        }

        // 2. Extract and create vendor aliases
        foreach (var record in batch.Where(r => !string.IsNullOrEmpty(r.Vendor)))
        {
            try
            {
                var aliasCreated = await ProcessVendorAliasAsync(record, processedVendorAliases, cancellationToken);
                if (aliasCreated)
                {
                    job.CreatedAliases++;
                }
            }
            catch (Exception ex)
            {
                errors.Add(new ImportError(record.LineNumber, $"Vendor alias error: {ex.Message}", record.Vendor));
            }
        }

        // 3. Generate embeddings in batch
        var recordsForEmbedding = batch
            .Where(r => !string.IsNullOrEmpty(r.GLCode) && !string.IsNullOrEmpty(r.Department))
            .ToList();

        if (recordsForEmbedding.Count > 0)
        {
            var embeddingsCreated = await ProcessEmbeddingsBatchAsync(job, recordsForEmbedding, errors, cancellationToken);
            job.GeneratedEmbeddings += embeddingsCreated;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> ProcessDescriptionAsync(HistoricalRecord record, HashSet<string> processedHashes, CancellationToken cancellationToken)
    {
        var hash = ComputeDescriptionHash(record.Description);

        // Skip if already processed in this batch
        if (!processedHashes.Add(hash))
        {
            return false; // Already processed in this batch
        }

        // Check if already exists in database
        var existing = await _dbContext.DescriptionCaches
            .FirstOrDefaultAsync(d => d.RawDescriptionHash == hash, cancellationToken);

        if (existing != null)
        {
            // Update hit count
            existing.HitCount++;
            existing.LastAccessedAt = DateTime.UtcNow;
            return false; // Not a new entry
        }

        // Create new cache entry
        var cacheEntry = new DescriptionCache
        {
            RawDescriptionHash = hash,
            RawDescription = record.Description,
            NormalizedDescription = NormalizeDescription(record.Description),
            HitCount = 1,
            LastAccessedAt = DateTime.UtcNow
        };

        _dbContext.DescriptionCaches.Add(cacheEntry);
        return true;
    }

    private async Task<bool> ProcessVendorAliasAsync(HistoricalRecord record, HashSet<string> processedAliases, CancellationToken cancellationToken)
    {
        var vendorPattern = ExtractVendorPattern(record.Vendor ?? record.Description);
        if (string.IsNullOrEmpty(vendorPattern))
        {
            return false;
        }

        var canonicalName = vendorPattern.ToUpperInvariant();

        // Skip if already processed in this batch
        if (!processedAliases.Add(canonicalName))
        {
            return false; // Already processed in this batch
        }

        // Check if already exists in database
        var existing = await _dbContext.VendorAliases
            .FirstOrDefaultAsync(v => v.CanonicalName == canonicalName, cancellationToken);

        if (existing != null)
        {
            // Update match count
            existing.MatchCount++;
            existing.LastMatchedAt = DateTime.UtcNow;

            // Update defaults if we have better data
            if (string.IsNullOrEmpty(existing.DefaultGLCode) && !string.IsNullOrEmpty(record.GLCode))
            {
                existing.DefaultGLCode = record.GLCode;
            }
            if (string.IsNullOrEmpty(existing.DefaultDepartment) && !string.IsNullOrEmpty(record.Department))
            {
                existing.DefaultDepartment = record.Department;
            }

            return false; // Not a new entry
        }

        // Create new alias
        var alias = new VendorAlias
        {
            CanonicalName = canonicalName,
            AliasPattern = vendorPattern,
            DisplayName = FormatDisplayName(vendorPattern),
            DefaultGLCode = record.GLCode,
            DefaultDepartment = record.Department,
            MatchCount = 1,
            LastMatchedAt = DateTime.UtcNow,
            Confidence = 1.0m,
            Category = VendorCategory.Standard
        };

        _dbContext.VendorAliases.Add(alias);
        return true;
    }

    private async Task<int> ProcessEmbeddingsBatchAsync(
        ImportJob job,
        List<HistoricalRecord> records,
        List<ImportError> errors,
        CancellationToken cancellationToken)
    {
        var created = 0;

        // Process embeddings one at a time (API doesn't support true batching)
        foreach (var record in records)
        {
            try
            {
                var normalizedText = NormalizeDescription(record.Description);
                var embedding = await _embeddingService.GenerateEmbeddingAsync(normalizedText, cancellationToken);

                // Check for near-duplicate
                var similar = await _dbContext.ExpenseEmbeddings
                    .Where(e => e.Verified)
                    .OrderBy(e => e.Embedding!.CosineDistance(embedding))
                    .FirstOrDefaultAsync(cancellationToken);

                if (similar != null)
                {
                    var similarity = 1 - similar.Embedding!.CosineDistance(embedding);
                    if (similarity > _similarityThreshold)
                    {
                        // Skip near-duplicate
                        continue;
                    }
                }

                // Create verified embedding
                var expenseEmbedding = new ExpenseEmbedding
                {
                    UserId = job.UserId,
                    DescriptionText = normalizedText,
                    Embedding = embedding,
                    GLCode = record.GLCode!,
                    Department = record.Department!,
                    Verified = true,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.ExpenseEmbeddings.Add(expenseEmbedding);
                created++;
            }
            catch (Exception ex)
            {
                errors.Add(new ImportError(record.LineNumber, $"Embedding error: {ex.Message}", record.Description));
            }
        }

        return created;
    }

    private static string ComputeDescriptionHash(string description)
    {
        var normalized = description.ToUpperInvariant().Trim();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }

    private static string NormalizeDescription(string description)
    {
        // Basic normalization - remove extra whitespace and standardize case
        var normalized = description.Trim();
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");
        return normalized;
    }

    private static string? ExtractVendorPattern(string description)
    {
        // Extract vendor pattern from description
        // Take first word/phrase before numbers or special characters
        var pattern = System.Text.RegularExpressions.Regex.Match(
            description,
            @"^([A-Za-z\s&'-]+?)(?:\s*[\d#*]|$)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (pattern.Success && pattern.Groups[1].Value.Length >= 3)
        {
            return pattern.Groups[1].Value.Trim();
        }

        // Fallback: take first word
        var words = description.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 0 && words[0].Length >= 3 ? words[0] : null;
    }

    private static string FormatDisplayName(string vendorPattern)
    {
        // Title case the vendor name
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(vendorPattern.ToLower());
    }

    /// <summary>
    /// Truncates error log to fit within the database column limit.
    /// Progressively removes errors from the end if the serialized JSON is too long.
    /// </summary>
    private static string? TruncateErrorLog(List<ImportError> errors)
    {
        if (errors.Count == 0)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(errors);

        // If it fits, return as-is
        if (json.Length <= MaxErrorLogLength)
        {
            return json;
        }

        // Progressively remove errors until it fits
        var truncatedErrors = new List<ImportError>(errors);
        var totalErrors = errors.Count;

        while (truncatedErrors.Count > 1 && json.Length > MaxErrorLogLength)
        {
            truncatedErrors.RemoveAt(truncatedErrors.Count - 1);

            // Add a truncation notice
            var truncationNotice = new ImportError(
                0,
                $"[TRUNCATED: Showing {truncatedErrors.Count} of {totalErrors} errors due to log size limit]",
                null);

            var displayList = new List<ImportError>(truncatedErrors) { truncationNotice };
            json = JsonSerializer.Serialize(displayList);
        }

        // Final fallback: if still too long, return a minimal error
        if (json.Length > MaxErrorLogLength)
        {
            json = JsonSerializer.Serialize(new List<ImportError>
            {
                new(0, $"[TRUNCATED: {totalErrors} errors occurred but log was too large to store]", null)
            });
        }

        return json;
    }

    private record HistoricalRecord
    {
        public int LineNumber { get; init; }
        public DateTime? Date { get; init; }
        public string Description { get; init; } = string.Empty;
        public string? Vendor { get; init; }
        public decimal? Amount { get; init; }
        public string? GLCode { get; init; }
        public string? Department { get; init; }
    }

    private record ImportError(int LineNumber, string ErrorMessage, string? RawData);
}
