using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for managing transaction groups.
/// Handles grouping multiple transactions into single units for receipt matching.
/// </summary>
public interface ITransactionGroupService
{
    /// <summary>
    /// Creates a new transaction group from selected transactions.
    /// Validates all transactions belong to the user, are not already grouped,
    /// and have no matched receipts.
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="request">Create group request with transaction IDs.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created group details.</returns>
    /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
    Task<TransactionGroupDetailDto> CreateGroupAsync(
        Guid userId,
        CreateGroupRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a transaction group by ID with full transaction details.
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="groupId">The group ID to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Group details, or null if not found.</returns>
    Task<TransactionGroupDetailDto?> GetGroupAsync(
        Guid userId,
        Guid groupId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all transaction groups for a user.
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of transaction groups.</returns>
    Task<TransactionGroupListResponse> GetGroupsAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Updates a group's name or display date.
    /// Setting DisplayDate marks IsDateOverridden as true.
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="groupId">The group ID to update.</param>
    /// <param name="request">Update request with optional name and date.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated group details, or null if not found.</returns>
    Task<TransactionGroupDetailDto?> UpdateGroupAsync(
        Guid userId,
        Guid groupId,
        UpdateGroupRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Dissolves a group (ungroup operation).
    /// Removes group reference from all transactions and deletes the group.
    /// Also clears any receipt match on the group.
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="groupId">The group ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteGroupAsync(
        Guid userId,
        Guid groupId,
        CancellationToken ct = default);

    /// <summary>
    /// Adds transactions to an existing group.
    /// Validates transactions are not already grouped and have no matched receipts.
    /// Recalculates combined amount, count, and display date.
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="groupId">The group ID to add transactions to.</param>
    /// <param name="request">Request with transaction IDs to add.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated group details, or null if group not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when validation fails.</exception>
    Task<TransactionGroupDetailDto?> AddTransactionsToGroupAsync(
        Guid userId,
        Guid groupId,
        AddToGroupRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a single transaction from a group.
    /// Group must maintain at least 2 transactions; use DeleteGroupAsync to fully ungroup.
    /// Recalculates combined amount, count, and display date.
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="groupId">The group ID to remove from.</param>
    /// <param name="transactionId">The transaction ID to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Updated group details, or null if group not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when removing would leave fewer than 2 transactions.</exception>
    Task<TransactionGroupDetailDto?> RemoveTransactionFromGroupAsync(
        Guid userId,
        Guid groupId,
        Guid transactionId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a mixed list of ungrouped transactions and transaction groups.
    /// Used by the Transactions page to display all items in a single view.
    /// </summary>
    /// <param name="userId">The authenticated user ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="matched">Optional filter by receipt match status.</param>
    /// <param name="search">Optional text search on description.</param>
    /// <param name="sortBy">Sort field: "date" (default), "amount".</param>
    /// <param name="sortOrder">Sort order: "desc" (default), "asc".</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mixed list of transactions and groups.</returns>
    Task<TransactionMixedListResponse> GetMixedListAsync(
        Guid userId,
        int page = 1,
        int pageSize = 50,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool? matched = null,
        string? search = null,
        string sortBy = "date",
        string sortOrder = "desc",
        CancellationToken ct = default);
}
