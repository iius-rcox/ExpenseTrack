using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for expense splitting functionality.
/// Provides split pattern suggestions and manages expense split operations.
/// </summary>
public interface IExpenseSplittingService
{
    #region Split Operations

    /// <summary>
    /// Gets the current split status and suggestions for an expense.
    /// Uses Tier 1 pattern matching to suggest splits based on vendor patterns.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="expenseId">The expense ID.</param>
    /// <returns>Split status with current allocations and suggestions.</returns>
    Task<ExpenseSplitStatusDto?> GetSplitStatusAsync(Guid userId, Guid expenseId);

    /// <summary>
    /// Applies a split to an expense.
    /// Creates split lines for each allocation.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="expenseId">The expense ID.</param>
    /// <param name="request">The split allocation request.</param>
    /// <returns>Result of the split operation.</returns>
    Task<ApplySplitResultDto> ApplySplitAsync(Guid userId, Guid expenseId, ApplySplitRequestDto request);

    /// <summary>
    /// Removes a split from an expense.
    /// Deletes all split lines and restores original single allocation.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="expenseId">The expense ID.</param>
    /// <returns>True if split was removed, false if expense was not found or not split.</returns>
    Task<bool> RemoveSplitAsync(Guid userId, Guid expenseId);

    #endregion

    #region Split Pattern Management

    /// <summary>
    /// Gets paginated list of split patterns for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="vendorAliasId">Optional filter by vendor alias.</param>
    /// <returns>Paginated list of patterns.</returns>
    Task<SplitPatternListResponseDto> GetPatternsAsync(
        Guid userId,
        int page,
        int pageSize,
        Guid? vendorAliasId = null);

    /// <summary>
    /// Gets a split pattern by ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="patternId">The pattern ID.</param>
    /// <returns>Pattern details or null if not found.</returns>
    Task<SplitPatternDetailDto?> GetPatternAsync(Guid userId, Guid patternId);

    /// <summary>
    /// Creates a new split pattern.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">Pattern creation request.</param>
    /// <returns>Created pattern details.</returns>
    Task<SplitPatternDetailDto> CreatePatternAsync(Guid userId, CreateSplitPatternRequestDto request);

    /// <summary>
    /// Updates an existing split pattern.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="patternId">The pattern ID.</param>
    /// <param name="request">Pattern update request.</param>
    /// <returns>Updated pattern details or null if not found.</returns>
    Task<SplitPatternDetailDto?> UpdatePatternAsync(
        Guid userId,
        Guid patternId,
        UpdateSplitPatternRequestDto request);

    /// <summary>
    /// Deletes a split pattern.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="patternId">The pattern ID.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeletePatternAsync(Guid userId, Guid patternId);

    #endregion

    #region Suggestions

    /// <summary>
    /// Gets a split suggestion for an expense based on vendor patterns.
    /// Uses Tier 1 (rule-based) matching against saved patterns.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="expenseId">The expense ID.</param>
    /// <returns>Split suggestion or null if expense not found.</returns>
    Task<SplitSuggestionDto?> GetSuggestionAsync(Guid userId, Guid expenseId);

    /// <summary>
    /// Gets the default split pattern for a vendor.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="vendorAliasId">The vendor alias ID.</param>
    /// <returns>Default pattern or null if none set.</returns>
    Task<SplitPatternDetailDto?> GetDefaultPatternForVendorAsync(Guid userId, Guid vendorAliasId);

    #endregion

    #region Validation

    /// <summary>
    /// Validates that allocations sum to exactly 100%.
    /// </summary>
    /// <param name="allocations">The allocations to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    bool ValidateAllocations(IEnumerable<SplitAllocationDto> allocations);

    #endregion
}
