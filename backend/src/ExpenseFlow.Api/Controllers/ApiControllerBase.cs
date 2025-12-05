using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Base controller class for all API controllers.
/// Provides common functionality and route prefix.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Gets the current user's object ID from claims.
    /// </summary>
    protected string? CurrentUserObjectId =>
        User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
        ?? User.FindFirst("oid")?.Value;

    /// <summary>
    /// Gets the current user's email from claims.
    /// </summary>
    protected string? CurrentUserEmail =>
        User.FindFirst("preferred_username")?.Value
        ?? User.FindFirst("email")?.Value;
}
