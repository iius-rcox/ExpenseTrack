using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Repository interface for Receipt entity operations.
/// All methods enforce row-level security by requiring userId.
/// </summary>
public interface IReceiptRepository
{
    /// <summary>
    /// Adds a new receipt to the database.
    /// </summary>
    Task<Receipt> AddAsync(Receipt receipt);

    /// <summary>
    /// Gets a receipt by ID for a specific user.
    /// </summary>
    /// <param name="id">Receipt ID</param>
    /// <param name="userId">User ID for row-level security</param>
    /// <returns>Receipt if found and belongs to user, null otherwise</returns>
    Task<Receipt?> GetByIdAsync(Guid id, Guid userId);

    /// <summary>
    /// Gets a receipt by ID without user filtering (for background jobs).
    /// </summary>
    /// <param name="id">Receipt ID</param>
    /// <returns>Receipt if found, null otherwise</returns>
    Task<Receipt?> GetByIdAsync(Guid id);

    /// <summary>
    /// Updates an existing receipt.
    /// </summary>
    Task<Receipt> UpdateAsync(Receipt receipt);

    /// <summary>
    /// Deletes a receipt by ID for a specific user.
    /// </summary>
    /// <param name="id">Receipt ID</param>
    /// <param name="userId">User ID for row-level security</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, Guid userId);

    /// <summary>
    /// Gets a paginated list of receipts for a user with filtering and sorting.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="pageNumber">1-based page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="status">Optional receipt status filter</param>
    /// <param name="matchStatus">Optional match status filter</param>
    /// <param name="vendor">Optional vendor name search (case-insensitive)</param>
    /// <param name="receiptDateFrom">Optional receipt date start filter (DateExtracted)</param>
    /// <param name="receiptDateTo">Optional receipt date end filter (DateExtracted)</param>
    /// <param name="fromDate">Optional upload date start filter (CreatedAt)</param>
    /// <param name="toDate">Optional upload date end filter (CreatedAt)</param>
    /// <param name="sortBy">Sort field: date, amount, vendor, created (default)</param>
    /// <param name="sortOrder">Sort order: asc or desc (default)</param>
    /// <returns>Paginated list of receipts</returns>
    Task<(List<Receipt> Items, int TotalCount)> GetPagedAsync(
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
    /// Gets count of receipts by status for a user.
    /// </summary>
    Task<Dictionary<ReceiptStatus, int>> GetStatusCountsAsync(Guid userId);

    /// <summary>
    /// Gets the count of receipts without thumbnails, optionally filtered by content types.
    /// Used for thumbnail backfill progress estimation.
    /// </summary>
    /// <param name="contentTypes">Optional list of content types to filter (e.g., "image/jpeg", "application/pdf")</param>
    /// <returns>Count of receipts without thumbnails</returns>
    Task<int> GetReceiptsWithoutThumbnailsCountAsync(List<string>? contentTypes = null);

    /// <summary>
    /// Gets a batch of receipts that don't have thumbnails, for backfill processing.
    /// </summary>
    /// <param name="batchSize">Maximum number of receipts to return</param>
    /// <param name="contentTypes">Optional list of content types to filter</param>
    /// <returns>List of receipts needing thumbnail generation</returns>
    Task<List<Receipt>> GetReceiptsWithoutThumbnailsAsync(int batchSize, List<string>? contentTypes = null);
}
