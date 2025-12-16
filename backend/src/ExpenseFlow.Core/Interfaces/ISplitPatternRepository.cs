using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Repository interface for SplitPattern entity operations.
/// </summary>
public interface ISplitPatternRepository
{
    /// <summary>
    /// Gets a split pattern by ID for a specific user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="patternId">Pattern ID.</param>
    /// <returns>Split pattern if found, null otherwise.</returns>
    Task<SplitPattern?> GetByIdAsync(Guid userId, Guid patternId);

    /// <summary>
    /// Gets paginated split patterns for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>Tuple of patterns list and total count.</returns>
    Task<(List<SplitPattern> Patterns, int TotalCount)> GetPagedAsync(
        Guid userId,
        int page,
        int pageSize);

    /// <summary>
    /// Gets split patterns for a specific vendor alias.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="vendorAliasId">Vendor alias ID.</param>
    /// <returns>List of split patterns.</returns>
    Task<List<SplitPattern>> GetByVendorAliasAsync(Guid userId, Guid vendorAliasId);

    /// <summary>
    /// Gets the most recently used split pattern for a vendor.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="vendorAliasId">Vendor alias ID.</param>
    /// <returns>Most recent pattern if found, null otherwise.</returns>
    Task<SplitPattern?> GetMostRecentByVendorAsync(Guid userId, Guid vendorAliasId);

    /// <summary>
    /// Gets the default split pattern for a vendor.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="vendorAliasId">Vendor alias ID.</param>
    /// <returns>Default pattern if found, null otherwise.</returns>
    Task<SplitPattern?> GetDefaultByVendorAsync(Guid userId, Guid vendorAliasId);

    /// <summary>
    /// Adds a new split pattern.
    /// </summary>
    /// <param name="pattern">Pattern to add.</param>
    Task AddAsync(SplitPattern pattern);

    /// <summary>
    /// Updates an existing split pattern.
    /// </summary>
    /// <param name="pattern">Pattern to update.</param>
    Task UpdateAsync(SplitPattern pattern);

    /// <summary>
    /// Deletes a split pattern.
    /// </summary>
    /// <param name="pattern">Pattern to delete.</param>
    Task DeleteAsync(SplitPattern pattern);

    /// <summary>
    /// Increments the usage count and updates last used timestamp.
    /// </summary>
    /// <param name="pattern">Pattern to update.</param>
    Task IncrementUsageAsync(SplitPattern pattern);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync();
}
