using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service for receipt-to-transaction matching operations.
/// </summary>
public interface IMatchingService
{
    /// <summary>
    /// Runs auto-match for all unmatched receipts or specific receipt IDs.
    /// Creates proposed matches with confidence >= 70%.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="receiptIds">Optional specific receipt IDs to match</param>
    /// <returns>Result with proposed count, processed count, ambiguous count, and duration</returns>
    Task<AutoMatchResult> RunAutoMatchAsync(Guid userId, List<Guid>? receiptIds = null);

    /// <summary>
    /// Gets proposed matches awaiting user review.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paginated list of proposed matches ordered by confidence descending</returns>
    Task<(List<ReceiptTransactionMatch> Items, int TotalCount)> GetProposalsAsync(Guid userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Confirms a proposed match, linking receipt to transaction and creating/updating vendor alias.
    /// </summary>
    /// <param name="matchId">Match record ID</param>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="vendorDisplayName">Optional vendor display name override</param>
    /// <param name="defaultGLCode">Optional default GL code for vendor alias</param>
    /// <param name="defaultDepartment">Optional default department for vendor alias</param>
    /// <returns>Updated match record</returns>
    Task<ReceiptTransactionMatch> ConfirmMatchAsync(Guid matchId, Guid userId, string? vendorDisplayName = null, string? defaultGLCode = null, string? defaultDepartment = null);

    /// <summary>
    /// Rejects a proposed match, keeping record for audit.
    /// </summary>
    /// <param name="matchId">Match record ID</param>
    /// <param name="userId">User ID for row-level security</param>
    /// <returns>Updated match record</returns>
    Task<ReceiptTransactionMatch> RejectMatchAsync(Guid matchId, Guid userId);

    /// <summary>
    /// Gets a single match by ID.
    /// </summary>
    /// <param name="matchId">Match record ID</param>
    /// <param name="userId">User ID for row-level security</param>
    /// <returns>Match record if found, null otherwise</returns>
    Task<ReceiptTransactionMatch?> GetMatchAsync(Guid matchId, Guid userId);

    /// <summary>
    /// Creates a manual match between a receipt and transaction.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="receiptId">Receipt to match</param>
    /// <param name="transactionId">Transaction to match</param>
    /// <param name="vendorDisplayName">Optional vendor display name</param>
    /// <param name="defaultGLCode">Optional default GL code</param>
    /// <param name="defaultDepartment">Optional default department</param>
    /// <returns>Created match record</returns>
    Task<ReceiptTransactionMatch> CreateManualMatchAsync(Guid userId, Guid receiptId, Guid transactionId, string? vendorDisplayName = null, string? defaultGLCode = null, string? defaultDepartment = null);

    /// <summary>
    /// Gets unmatched receipts with extracted data.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paginated list of unmatched receipts</returns>
    Task<(List<Receipt> Items, int TotalCount)> GetUnmatchedReceiptsAsync(Guid userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets unmatched transactions.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <returns>Paginated list of unmatched transactions</returns>
    Task<(List<Transaction> Items, int TotalCount)> GetUnmatchedTransactionsAsync(Guid userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Gets matching statistics for the user.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <returns>Statistics including matched, proposed, and unmatched counts</returns>
    Task<MatchingStats> GetStatsAsync(Guid userId);
}

/// <summary>
/// Result of auto-match operation.
/// </summary>
public record AutoMatchResult(
    int ProposedCount,
    int ProcessedCount,
    int AmbiguousCount,
    long DurationMs,
    List<ReceiptTransactionMatch> Proposals);

/// <summary>
/// Matching statistics for a user.
/// </summary>
public record MatchingStats(
    int MatchedCount,
    int ProposedCount,
    int UnmatchedReceiptsCount,
    int UnmatchedTransactionsCount,
    decimal AutoMatchRate,
    decimal AverageConfidence);
