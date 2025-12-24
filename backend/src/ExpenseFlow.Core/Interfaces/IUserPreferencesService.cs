using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service for managing user preferences.
/// </summary>
public interface IUserPreferencesService
{
    /// <summary>
    /// Gets existing preferences or returns in-memory defaults if none exist.
    /// Does not persist defaults to database.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <returns>User preferences (persisted or default).</returns>
    Task<UserPreferences> GetOrCreateDefaultsAsync(Guid userId);

    /// <summary>
    /// Updates user preferences with partial update semantics.
    /// Creates a new record if none exists (upsert).
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="request">The partial update request.</param>
    /// <returns>Updated user preferences.</returns>
    Task<UserPreferences> UpdateAsync(Guid userId, UpdatePreferencesRequest request);
}
