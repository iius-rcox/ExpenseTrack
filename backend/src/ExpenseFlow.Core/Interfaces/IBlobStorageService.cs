namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service for managing blob storage operations.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a file to blob storage.
    /// </summary>
    /// <param name="stream">File content stream</param>
    /// <param name="path">Blob path (e.g., receipts/{userId}/{year}/{month}/{uuid}_{filename})</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <returns>Full URL to the uploaded blob</returns>
    Task<string> UploadAsync(Stream stream, string path, string contentType);

    /// <summary>
    /// Deletes a blob from storage.
    /// </summary>
    /// <param name="blobUrl">Full URL to the blob</param>
    Task DeleteAsync(string blobUrl);

    /// <summary>
    /// Generates a SAS URL for temporary access to a blob.
    /// </summary>
    /// <param name="blobUrl">Full URL to the blob</param>
    /// <param name="expiry">Duration for which the SAS URL is valid</param>
    /// <returns>SAS URL for temporary access</returns>
    Task<string> GenerateSasUrlAsync(string blobUrl, TimeSpan expiry);

    /// <summary>
    /// Downloads a blob as a stream.
    /// </summary>
    /// <param name="blobUrl">Full URL to the blob</param>
    /// <returns>Stream containing the blob content</returns>
    Task<Stream> DownloadAsync(string blobUrl);
}
