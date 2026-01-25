using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using ExpenseFlow.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Azure Blob Storage implementation of IBlobStorageService.
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly string _receiptsContainer;
    private readonly string _thumbnailsContainer;

    public BlobStorageService(
        IConfiguration configuration,
        ILogger<BlobStorageService> logger)
    {
        _logger = logger;

        var connectionString = configuration["BlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("BlobStorage:ConnectionString is required");

        _receiptsContainer = configuration["BlobStorage:ReceiptsContainer"] ?? "receipts";
        _thumbnailsContainer = configuration["BlobStorage:ThumbnailsContainer"] ?? "thumbnails";

        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadAsync(Stream stream, string path, string contentType)
    {
        var containerName = path.StartsWith("thumbnails/") ? _thumbnailsContainer : _receiptsContainer;
        var blobPath = path.StartsWith("thumbnails/") ? path[11..] : (path.StartsWith("receipts/") ? path[9..] : path);

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobClient = containerClient.GetBlobClient(blobPath);

        stream.Position = 0;
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });

        _logger.LogInformation("Uploaded blob to {BlobUrl}", blobClient.Uri);

        return blobClient.Uri.ToString();
    }

    public async Task DeleteAsync(string blobUrl)
    {
        var blobClient = GetBlobClientFromUrl(blobUrl);
        var response = await blobClient.DeleteIfExistsAsync();

        if (response.Value)
        {
            _logger.LogInformation("Deleted blob at {BlobUrl}", blobUrl);
        }
        else
        {
            _logger.LogWarning("Blob not found for deletion: {BlobUrl}", blobUrl);
        }
    }

    public async Task<string> GenerateSasUrlAsync(string blobUrl, TimeSpan expiry)
    {
        var blobClient = GetBlobClientFromUrl(blobUrl);

        // Check if the blob exists
        if (!await blobClient.ExistsAsync())
        {
            throw new InvalidOperationException($"Blob does not exist: {blobUrl}");
        }

        // Generate SAS token with read permissions
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = blobClient.BlobContainerName,
            BlobName = blobClient.Name,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.Add(expiry)
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasUri = blobClient.GenerateSasUri(sasBuilder);

        _logger.LogDebug("Generated SAS URL for blob {BlobUrl} expiring at {Expiry}",
            blobUrl, sasBuilder.ExpiresOn);

        return sasUri.ToString();
    }

    public async Task<Stream> DownloadAsync(string blobUrl)
    {
        if (string.IsNullOrWhiteSpace(blobUrl))
        {
            throw new ArgumentException("BlobUrl cannot be null or empty", nameof(blobUrl));
        }

        // Validate that the URL's storage account matches our configured account
        var blobUri = new Uri(blobUrl);
        var configuredAccountHost = _blobServiceClient.Uri.Host;
        if (!blobUri.Host.Equals(configuredAccountHost, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "Blob URL host mismatch: URL points to {UrlHost} but configured account is {ConfiguredHost}. BlobUrl: {BlobUrl}",
                blobUri.Host, configuredAccountHost, blobUrl);
            throw new InvalidOperationException(
                $"Blob URL host '{blobUri.Host}' does not match configured storage account '{configuredAccountHost}'");
        }

        var blobClient = GetBlobClientFromUrl(blobUrl);

        _logger.LogDebug("Attempting to download blob from {BlobUrl}", blobUrl);

        var response = await blobClient.DownloadAsync();

        _logger.LogDebug("Successfully downloaded blob from {BlobUrl}", blobUrl);

        return response.Value.Content;
    }

    public async Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken ct = default)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var response = await blobClient.DownloadAsync(ct);

        _logger.LogDebug("Downloaded blob from container {Container}, blob {BlobName}", containerName, blobName);

        return response.Value.Content;
    }

    private BlobClient GetBlobClientFromUrl(string blobUrl)
    {
        var uri = new Uri(blobUrl);
        var pathParts = uri.AbsolutePath.TrimStart('/').Split('/', 2);

        if (pathParts.Length < 2)
        {
            throw new ArgumentException($"Invalid blob URL format: {blobUrl}");
        }

        var containerName = pathParts[0];
        var blobPath = pathParts[1];

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        return containerClient.GetBlobClient(blobPath);
    }

    /// <inheritdoc />
    string IBlobStorageService.GenerateReceiptPath(Guid userId, string originalFilename)
        => GenerateReceiptPath(userId, originalFilename);

    /// <summary>
    /// Generates the standard blob path for a receipt.
    /// </summary>
    public static string GenerateReceiptPath(Guid userId, string originalFilename)
    {
        var now = DateTime.UtcNow;
        var uniqueId = Guid.NewGuid();
        var safeFilename = SanitizeFilename(originalFilename);

        return $"receipts/{userId}/{now.Year}/{now.Month:D2}/{uniqueId}_{safeFilename}";
    }

    /// <summary>
    /// Generates the standard blob path for a thumbnail.
    /// </summary>
    public static string GenerateThumbnailPath(Guid userId, Guid receiptId)
    {
        var now = DateTime.UtcNow;
        return $"thumbnails/{userId}/{now.Year}/{now.Month:D2}/{receiptId}_thumb.jpg";
    }

    private static string SanitizeFilename(string filename)
    {
        // BUG-005/BUG-006 fix: Many characters cause URL encoding issues when round-tripping
        // through blob storage URLs. Use allowlist approach for maximum compatibility.
        // Allowed: letters, digits, underscore, hyphen, period
        var sanitized = new string(filename
            .Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.'
                ? c
                : '_')  // Replace any problematic character with underscore
            .ToArray());

        // Collapse multiple consecutive underscores into single underscore
        while (sanitized.Contains("__"))
        {
            sanitized = sanitized.Replace("__", "_");
        }

        // Trim leading/trailing underscores (but preserve extension dot)
        var extension = Path.GetExtension(sanitized);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized).Trim('_');
        sanitized = nameWithoutExtension + extension;

        // Ensure filename is not empty after sanitization
        if (string.IsNullOrWhiteSpace(nameWithoutExtension))
        {
            sanitized = "file" + extension;
        }

        // Limit to 100 characters
        if (sanitized.Length > 100)
        {
            nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExtension[..(100 - extension.Length)] + extension;
        }

        return sanitized;
    }
}
