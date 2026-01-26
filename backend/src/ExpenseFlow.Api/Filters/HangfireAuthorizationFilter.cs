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

        // TEMPORARY: Always allow in non-production for debugging thumbnail backfill
        // TODO: Restore proper authorization after debugging
        if (!env.IsProduction())
        {
            return true;
        }

        // Production requires authentication and Admin role
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }

        return httpContext.User.HasClaim("roles", "Admin")
            || httpContext.User.HasClaim("roles", "ExpenseFlow.Admin")
            || httpContext.User.IsInRole("Admin");
    }
}
