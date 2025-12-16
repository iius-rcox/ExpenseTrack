using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Repository interface for ReceiptTransactionMatch entity operations.
/// </summary>
public interface IMatchRepository
{
    /// <summary>
    /// Adds a new match record to the database.
    /// </summary>
    Task<ReceiptTransactionMatch> AddAsync(ReceiptTransactionMatch match);

    /// <summary>
    /// Adds multiple match records to the database.
    /// </summary>
    Task<List<ReceiptTransactionMatch>> AddRangeAsync(IEnumerable<ReceiptTransactionMatch> matches);

    /// <summary>
    /// Gets a match by ID for a specific user.
    /// </summary>
    /// <param name="id">Match record ID</param>
    /// <param name="userId">User ID for row-level security</param>
    /// <returns>Match record if found and belongs to user, null otherwise</returns>
    Task<ReceiptTransactionMatch?> GetByIdAsync(Guid id, Guid userId);

    /// <summary>
    /// Gets a match by receipt ID.
    /// </summary>
    /// <param name="receiptId">Receipt ID</param>
    /// <param name="userId">User ID for row-level security</param>
    /// <returns>Match record if found, null otherwise</returns>
    Task<ReceiptTransactionMatch?> GetByReceiptIdAsync(Guid receiptId, Guid userId);

    /// <summary>
    /// Gets a match by transaction ID.
    /// </summary>
    /// <param name="transactionId">Transaction ID</param>
    /// <param name="userId">User ID for row-level security</param>
    /// <returns>Match record if found, null otherwise</returns>
    Task<ReceiptTransactionMatch?> GetByTransactionIdAsync(Guid transactionId, Guid userId);

    /// <summary>
    /// Gets proposed matches for a user with pagination.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paginated list of proposed matches ordered by confidence descending</returns>
    Task<(List<ReceiptTransactionMatch> Items, int TotalCount)> GetProposedByUserIdAsync(Guid userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Updates an existing match record.
    /// </summary>
    Task<ReceiptTransactionMatch> UpdateAsync(ReceiptTransactionMatch match);

    /// <summary>
    /// Deletes a match record by ID for a specific user.
    /// </summary>
    /// <param name="id">Match record ID</param>
    /// <param name="userId">User ID for row-level security</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, Guid userId);

    /// <summary>
    /// Gets count of matches by status for a user.
    /// </summary>
    Task<Dictionary<MatchStatus, int>> GetStatusCountsAsync(Guid userId);

    /// <summary>
    /// Gets the average confidence score of proposed matches.
    /// </summary>
    Task<decimal> GetAverageConfidenceAsync(Guid userId);

    /// <summary>
    /// Checks if a receipt already has a confirmed match.
    /// </summary>
    /// <param name="receiptId">Receipt ID to check</param>
    /// <param name="userId">Optional user ID for row-level security. If null, checks across all users (for constraint validation).</param>
    /// <returns>True if a confirmed match exists</returns>
    Task<bool> HasConfirmedMatchForReceiptAsync(Guid receiptId, Guid? userId = null);

    /// <summary>
    /// Checks if a transaction already has a confirmed match.
    /// </summary>
    /// <param name="transactionId">Transaction ID to check</param>
    /// <param name="userId">Optional user ID for row-level security. If null, checks across all users (for constraint validation).</param>
    /// <returns>True if a confirmed match exists</returns>
    Task<bool> HasConfirmedMatchForTransactionAsync(Guid transactionId, Guid? userId = null);

    /// <summary>
    /// Gets confirmed matches for a user within a date range (for draft report generation).
    /// Includes Receipt and Transaction navigation properties.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="startDate">Start of the period (inclusive)</param>
    /// <param name="endDate">End of the period (inclusive)</param>
    /// <returns>List of confirmed matches with related entities</returns>
    Task<List<ReceiptTransactionMatch>> GetConfirmedByPeriodAsync(Guid userId, DateOnly startDate, DateOnly endDate);
}
