using System.Security.Claims;
using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for user management operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets or creates a user based on their claims principal.
    /// Auto-creates on first login per FR-003.
    /// </summary>
    /// <param name="principal">The authenticated user's claims principal.</param>
    /// <returns>The user entity.</returns>
    Task<User> GetOrCreateUserAsync(ClaimsPrincipal principal);

    /// <summary>
    /// Gets a user by their Entra Object ID.
    /// </summary>
    /// <param name="entraObjectId">The Azure AD object ID.</param>
    /// <returns>The user if found, null otherwise.</returns>
    Task<User?> GetByEntraObjectIdAsync(string entraObjectId);

    /// <summary>
    /// Gets a user by their internal ID.
    /// </summary>
    /// <param name="id">The user's GUID.</param>
    /// <returns>The user if found, null otherwise.</returns>
    Task<User?> GetByIdAsync(Guid id);

    /// <summary>
    /// Updates the user's last login timestamp.
    /// </summary>
    /// <param name="user">The user to update.</param>
    Task UpdateLastLoginAsync(User user);
}
