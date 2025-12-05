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
    /// Gets a paginated list of receipts for a user.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="pageNumber">1-based page number</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="fromDate">Optional start date filter</param>
    /// <param name="toDate">Optional end date filter</param>
    /// <returns>Paginated list of receipts ordered by created date descending</returns>
    Task<(List<Receipt> Items, int TotalCount)> GetPagedAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 20,
        ReceiptStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null);

    /// <summary>
    /// Gets count of receipts by status for a user.
    /// </summary>
    Task<Dictionary<ReceiptStatus, int>> GetStatusCountsAsync(Guid userId);
}
