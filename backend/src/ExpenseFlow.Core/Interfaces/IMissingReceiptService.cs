using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for managing missing receipts.
/// Handles queries and updates for transactions marked as reimbursable but lacking matched receipts.
/// </summary>
public interface IMissingReceiptService
{
    /// <summary>
    /// Gets a paginated list of missing receipts for a user.
    /// Missing receipts are transactions that are:
    /// - Marked as reimbursable (user override or confirmed AI prediction)
    /// - Have no matched receipt (MatchedReceiptId is null)
    /// - Not dismissed (ReceiptDismissed is null or false), unless includeDismissed is true
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page (1-100, defaults to 25).</param>
    /// <param name="sortBy">Sort field: "date", "amount", or "vendor".</param>
    /// <param name="sortOrder">Sort order: "asc" or "desc".</param>
    /// <param name="includeDismissed">Whether to include dismissed items.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of missing receipt summaries.</returns>
    Task<MissingReceiptsListResponseDto> GetMissingReceiptsAsync(
        Guid userId,
        int page = 1,
        int pageSize = 25,
        string sortBy = "date",
        string sortOrder = "desc",
        bool includeDismissed = false,
        CancellationToken ct = default);

    /// <summary>
    /// Gets widget summary data for the missing receipts dashboard card.
    /// Returns total count and top 3 most recent missing receipts.
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Widget summary with count and recent items.</returns>
    Task<MissingReceiptsWidgetDto> GetWidgetDataAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Updates the receipt URL for a transaction.
    /// The URL is stored as plain text without validation (per spec clarification).
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="transactionId">The transaction ID to update.</param>
    /// <param name="receiptUrl">The URL to store, or null/empty to clear.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated transaction summary, or null if not found.</returns>
    Task<MissingReceiptSummaryDto?> UpdateReceiptUrlAsync(
        Guid userId,
        Guid transactionId,
        string? receiptUrl,
        CancellationToken ct = default);

    /// <summary>
    /// Dismisses or restores a transaction from the missing receipts list.
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="transactionId">The transaction ID to update.</param>
    /// <param name="dismiss">True to dismiss, false/null to restore.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated transaction summary, or null if not found.</returns>
    Task<MissingReceiptSummaryDto?> DismissTransactionAsync(
        Guid userId,
        Guid transactionId,
        bool? dismiss,
        CancellationToken ct = default);
}
