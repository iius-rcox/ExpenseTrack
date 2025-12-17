using ExpenseFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExpenseFlow.Api.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Uses in-memory database and test authentication.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "TestDb_" + Guid.NewGuid().ToString();
    public Guid TestUserId { get; private set; }
    public string TestUserEmail => TestAuthHandler.TestEmail;
    public string TestUserName => TestAuthHandler.TestName;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove ALL DbContext-related registrations
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ExpenseFlowDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(ExpenseFlowDbContext)).ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing (without vector support)
            services.AddDbContext<ExpenseFlowDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.EnableSensitiveDataLogging();
            });

            // Configure test authentication - remove existing auth schemes first
            services.RemoveAll<AuthenticationHandler<AuthenticationSchemeOptions>>();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, null);
        });

        builder.UseEnvironment("Development");
    }

    public void SeedDatabase(Action<ExpenseFlowDbContext, Guid> seedAction)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseFlowDbContext>();
        db.Database.EnsureCreated();

        // Seed test user if not exists
        var existingUser = db.Users.FirstOrDefault(u => u.EntraObjectId == TestAuthHandler.TestObjectId);
        if (existingUser == null)
        {
            TestUserId = Guid.NewGuid();
            var user = new ExpenseFlow.Core.Entities.User
            {
                Id = TestUserId,
                EntraObjectId = TestAuthHandler.TestObjectId,
                Email = TestAuthHandler.TestEmail,
                DisplayName = TestAuthHandler.TestName,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            db.SaveChanges();
        }
        else
        {
            TestUserId = existingUser.Id;
        }

        seedAction(db, TestUserId);
    }
}
