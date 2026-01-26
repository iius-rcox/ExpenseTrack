using Hangfire.Dashboard;

namespace ExpenseFlow.Api.Filters;

/// <summary>
/// Authorization filter for Hangfire Dashboard.
/// Restricts access to users with Admin role.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var env = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();

        // In development or staging, allow anonymous access for testing/debugging
        // Production requires Admin role
        if (env.IsDevelopment() || env.IsEnvironment("Staging"))
        {
            return true;
        }

        // Require authentication
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        // Check for Admin role
        return httpContext.User.HasClaim("roles", "Admin")
            || httpContext.User.HasClaim("roles", "ExpenseFlow.Admin")
            || httpContext.User.IsInRole("Admin");
    }
}
