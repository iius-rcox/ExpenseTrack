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
    /// <param name="search">Optional text search on description (case-insensitive).</param>
    /// <param name="sortBy">Field to sort by (date, amount, description). Defaults to date.</param>
    /// <param name="sortOrder">Sort direction (asc or desc). Defaults to desc.</param>
    /// <param name="minAmount">Optional minimum amount filter.</param>
    /// <param name="maxAmount">Optional maximum amount filter.</param>
    /// <param name="hasPendingPrediction">Optional filter for transactions with pending expense predictions.</param>
    /// <returns>Tuple of transactions list, total count, and unmatched count.</returns>
    Task<(List<Transaction> Transactions, int TotalCount, int UnmatchedCount)> GetPagedAsync(
        Guid userId,
        int page,
        int pageSize,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool? matched = null,
        Guid? importId = null,
        string? search = null,
        string? sortBy = null,
        string? sortOrder = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        bool? hasPendingPrediction = null);

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

    /// <summary>
    /// Gets unmatched transactions for a user within a date range (for draft report generation).
    /// Returns transactions that do not have a matched receipt.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="startDate">Start of the period (inclusive)</param>
    /// <param name="endDate">End of the period (inclusive)</param>
    /// <returns>List of unmatched transactions</returns>
    Task<List<Transaction>> GetUnmatchedByPeriodAsync(Guid userId, DateOnly startDate, DateOnly endDate);
}
