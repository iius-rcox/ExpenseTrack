using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service for managing receipt operations.
/// </summary>
public interface IReceiptService
{
    /// <summary>
    /// Uploads a receipt file, stores it in blob storage, and queues processing.
    /// </summary>
    /// <param name="stream">File content stream</param>
    /// <param name="filename">Original filename</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <param name="userId">User ID uploading the receipt</param>
    /// <returns>Created receipt entity</returns>
    Task<Receipt> UploadReceiptAsync(Stream stream, string filename, string contentType, Guid userId);

    /// <summary>
    /// Gets a receipt by ID for a specific user.
    /// </summary>
    Task<Receipt?> GetReceiptAsync(Guid id, Guid userId);

    /// <summary>
    /// Gets a paginated list of receipts for a user with filtering and sorting.
    /// </summary>
    Task<(List<Receipt> Items, int TotalCount)> GetReceiptsAsync(
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
        string? sortOrder = null);

    /// <summary>
    /// Deletes a receipt and its associated blob storage files.
    /// </summary>
    Task<bool> DeleteReceiptAsync(Guid id, Guid userId);

    /// <summary>
    /// Generates a temporary SAS URL for accessing a receipt blob.
    /// </summary>
    Task<string> GetReceiptUrlAsync(Guid id, Guid userId, TimeSpan? expiry = null);

    /// <summary>
    /// Gets count of receipts by status for a user.
    /// </summary>
    Task<Dictionary<ReceiptStatus, int>> GetStatusCountsAsync(Guid userId);

    /// <summary>
    /// Retries processing a failed receipt.
    /// </summary>
    Task<Receipt?> RetryReceiptAsync(Guid id, Guid userId);

    /// <summary>
    /// Triggers processing for an uploaded receipt.
    /// </summary>
    Task<Receipt?> TriggerProcessingAsync(Guid id, Guid userId);

    /// <summary>
    /// Updates receipt data with manual corrections.
    /// </summary>
    Task<Receipt?> UpdateReceiptAsync(Guid id, Guid userId, Shared.DTOs.ReceiptUpdateRequestDto request);
}
