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

        // In development, allow anonymous access for testing
        if (httpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
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
