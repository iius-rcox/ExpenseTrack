using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ExpenseFlow.Api.Middleware;

/// <summary>
/// Development-only authentication handler that creates a mock user.
/// This should NEVER be used in production.
/// </summary>
public class DevAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DevAuth";
    private const string DevUserId = "00000000-0000-0000-0000-000000000001";
    private const string DevUserEmail = "dev@expenseflow.local";
    private const string DevUserName = "Development User";

    public DevAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, DevUserId),
            new Claim(ClaimTypes.Email, DevUserEmail),
            new Claim(ClaimTypes.Name, DevUserName),
            new Claim("oid", DevUserId), // Azure AD object ID claim
            new Claim("preferred_username", DevUserEmail),
            new Claim("name", DevUserName)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        Logger.LogWarning("DEV AUTH: Authenticated as {UserName} ({UserId})", DevUserName, DevUserId);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
