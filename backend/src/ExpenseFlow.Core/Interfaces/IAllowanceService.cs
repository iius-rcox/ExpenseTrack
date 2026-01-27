using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service for managing recurring expense allowances.
/// </summary>
public interface IAllowanceService
{
    /// <summary>
    /// Gets all allowances for a user.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="activeOnly">If true, returns only active allowances. If false or null, returns all.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of allowances.</returns>
    Task<AllowanceListResponse> GetByUserAsync(Guid userId, bool? activeOnly = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific allowance by ID, ensuring user ownership.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="allowanceId">The allowance ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The allowance if found and owned by user, null otherwise.</returns>
    Task<AllowanceResponse?> GetByIdAsync(Guid userId, Guid allowanceId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new recurring allowance.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="request">The create request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created allowance.</returns>
    Task<AllowanceResponse> CreateAsync(Guid userId, CreateAllowanceRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing allowance with partial update semantics.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="allowanceId">The allowance ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated allowance if found and owned by user, null otherwise.</returns>
    Task<AllowanceResponse?> UpdateAsync(Guid userId, Guid allowanceId, UpdateAllowanceRequest request, CancellationToken ct = default);

    /// <summary>
    /// Soft deletes an allowance by setting IsActive to false.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="allowanceId">The allowance ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if deactivated, false if not found or not owned by user.</returns>
    Task<bool> DeactivateAsync(Guid userId, Guid allowanceId, CancellationToken ct = default);

    /// <summary>
    /// Gets active allowances that should be included in a report for a specific period.
    /// Returns expanded list based on frequency (weekly returns multiple, monthly/quarterly once).
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="periodStart">Start date of the period.</param>
    /// <param name="periodEnd">End date of the period.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of allowances expanded by frequency.</returns>
    Task<List<RecurringAllowance>> GetActiveAllowancesForPeriodAsync(
        Guid userId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken ct = default);
}
