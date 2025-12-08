using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Repository interface for Transaction entity operations.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Gets a transaction by ID for a specific user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="transactionId">Transaction ID.</param>
    /// <returns>Transaction if found, null otherwise.</returns>
    Task<Transaction?> GetByIdAsync(Guid userId, Guid transactionId);

    /// <summary>
    /// Gets paginated transactions for a user with optional filters.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="matched">Optional matched receipt filter.</param>
    /// <param name="importId">Optional import batch filter.</param>
    /// <returns>Tuple of transactions list, total count, and unmatched count.</returns>
    Task<(List<Transaction> Transactions, int TotalCount, int UnmatchedCount)> GetPagedAsync(
        Guid userId,
        int page,
        int pageSize,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool? matched = null,
        Guid? importId = null);

    /// <summary>
    /// Checks if a duplicate transaction exists.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="duplicateHash">Duplicate detection hash.</param>
    /// <returns>True if duplicate exists.</returns>
    Task<bool> ExistsByDuplicateHashAsync(Guid userId, string duplicateHash);

    /// <summary>
    /// Adds multiple transactions in a batch.
    /// </summary>
    /// <param name="transactions">Transactions to add.</param>
    Task AddRangeAsync(IEnumerable<Transaction> transactions);

    /// <summary>
    /// Deletes a transaction.
    /// </summary>
    /// <param name="transaction">Transaction to delete.</param>
    Task DeleteAsync(Transaction transaction);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync();
}
