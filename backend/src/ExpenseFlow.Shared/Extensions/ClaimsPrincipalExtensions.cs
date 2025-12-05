using System.Security.Claims;

namespace ExpenseFlow.Shared.Extensions;

/// <summary>
/// Extension methods for ClaimsPrincipal to extract common claims.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the Entra ID object identifier (oid claim).
    /// </summary>
    public static string? GetObjectId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? principal.FindFirst("oid")?.Value;
    }

    /// <summary>
    /// Gets the user's email address.
    /// </summary>
    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("preferred_username")?.Value
            ?? principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst("email")?.Value;
    }

    /// <summary>
    /// Gets the user's display name.
    /// </summary>
    public static string? GetDisplayName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("name")?.Value
            ?? principal.FindFirst(ClaimTypes.Name)?.Value;
    }

    /// <summary>
    /// Gets the user's department from claims.
    /// </summary>
    public static string? GetDepartment(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("department")?.Value
            ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/department")?.Value;
    }

    /// <summary>
    /// Checks if the user has the Admin role.
    /// </summary>
    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        return principal.HasClaim("roles", "Admin")
            || principal.HasClaim("roles", "ExpenseFlow.Admin")
            || principal.IsInRole("Admin");
    }

    /// <summary>
    /// Gets all roles assigned to the user.
    /// </summary>
    public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll("roles").Select(c => c.Value)
            .Concat(principal.FindAll(ClaimTypes.Role).Select(c => c.Value))
            .Distinct();
    }
}
