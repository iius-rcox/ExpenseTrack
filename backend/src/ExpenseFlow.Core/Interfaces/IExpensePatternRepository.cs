using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Repository interface for ExpensePattern entity operations.
/// </summary>
public interface IExpensePatternRepository
{
    /// <summary>
    /// Gets an expense pattern by ID for a specific user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="patternId">Pattern ID.</param>
    /// <returns>Pattern if found, null otherwise.</returns>
    Task<ExpensePattern?> GetByIdAsync(Guid userId, Guid patternId);

    /// <summary>
    /// Gets an expense pattern by normalized vendor name for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="normalizedVendor">Normalized vendor name.</param>
    /// <returns>Pattern if found, null otherwise.</returns>
    Task<ExpensePattern?> GetByNormalizedVendorAsync(Guid userId, string normalizedVendor);

    /// <summary>
    /// Gets paginated patterns for a user with optional suppression filter.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="includeSuppressed">Whether to include suppressed patterns.</param>
    /// <returns>Tuple of patterns list and total count.</returns>
    Task<(List<ExpensePattern> Patterns, int TotalCount)> GetPagedAsync(
        Guid userId,
        int page,
        int pageSize,
        bool includeSuppressed = false);

    /// <summary>
    /// Gets all active (non-suppressed) patterns for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>List of active patterns.</returns>
    Task<List<ExpensePattern>> GetActiveAsync(Guid userId);

    /// <summary>
    /// Gets patterns ordered by occurrence count for dashboard display.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="limit">Maximum patterns to return.</param>
    /// <returns>List of top patterns.</returns>
    Task<List<ExpensePattern>> GetTopPatternsAsync(Guid userId, int limit = 10);

    /// <summary>
    /// Gets the count of active and suppressed patterns for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>Tuple of active count and suppressed count.</returns>
    Task<(int ActiveCount, int SuppressedCount)> GetCountsAsync(Guid userId);

    /// <summary>
    /// Checks if a pattern exists for a specific vendor.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="normalizedVendor">Normalized vendor name.</param>
    /// <returns>True if pattern exists, false otherwise.</returns>
    Task<bool> ExistsAsync(Guid userId, string normalizedVendor);

    /// <summary>
    /// Adds a new expense pattern.
    /// </summary>
    /// <param name="pattern">Pattern to add.</param>
    Task AddAsync(ExpensePattern pattern);

    /// <summary>
    /// Adds multiple expense patterns.
    /// </summary>
    /// <param name="patterns">Patterns to add.</param>
    Task AddRangeAsync(IEnumerable<ExpensePattern> patterns);

    /// <summary>
    /// Updates an existing pattern.
    /// </summary>
    /// <param name="pattern">Pattern to update.</param>
    Task UpdateAsync(ExpensePattern pattern);

    /// <summary>
    /// Deletes a pattern.
    /// </summary>
    /// <param name="pattern">Pattern to delete.</param>
    Task DeleteAsync(ExpensePattern pattern);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync();
}
