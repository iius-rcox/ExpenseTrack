using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for vendor alias operations.
/// </summary>
public interface IVendorAliasService
{
    /// <summary>
    /// Finds a vendor alias matching the given description.
    /// </summary>
    /// <param name="description">Transaction description to match.</param>
    /// <returns>The matching alias if found, null otherwise.</returns>
    Task<VendorAlias?> FindMatchingAliasAsync(string description);

    /// <summary>
    /// Finds a vendor alias matching the given description with specific categories.
    /// Used for travel detection (Airline, Hotel) and subscription detection.
    /// </summary>
    /// <param name="description">Transaction description to match.</param>
    /// <param name="categories">Categories to filter by.</param>
    /// <returns>The matching alias if found, null otherwise.</returns>
    Task<VendorAlias?> FindMatchingAliasAsync(string description, params VendorCategory[] categories);

    /// <summary>
    /// Gets a vendor alias by its canonical name.
    /// </summary>
    /// <param name="canonicalName">The standardized vendor name.</param>
    /// <returns>The alias if found, null otherwise.</returns>
    Task<VendorAlias?> GetByCanonicalNameAsync(string canonicalName);

    /// <summary>
    /// Adds or updates a vendor alias.
    /// </summary>
    /// <param name="alias">The vendor alias to add or update.</param>
    /// <returns>The saved alias.</returns>
    Task<VendorAlias> AddOrUpdateAsync(VendorAlias alias);

    /// <summary>
    /// Records a match for the given alias.
    /// </summary>
    /// <param name="aliasId">The alias ID.</param>
    Task RecordMatchAsync(Guid aliasId);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Total entries and total matches.</returns>
    Task<(int TotalEntries, int TotalHits)> GetStatsAsync();

    /// <summary>
    /// Gets a vendor alias by vendor name pattern.
    /// </summary>
    /// <param name="vendorName">The vendor name to search for.</param>
    /// <returns>The matching vendor alias if found, null otherwise.</returns>
    Task<VendorAlias?> GetByVendorNameAsync(string vendorName);

    /// <summary>
    /// Updates an existing vendor alias.
    /// </summary>
    /// <param name="alias">The vendor alias to update.</param>
    Task UpdateAsync(VendorAlias alias);
}
