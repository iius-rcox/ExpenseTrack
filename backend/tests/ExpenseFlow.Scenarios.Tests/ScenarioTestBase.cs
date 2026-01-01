using ExpenseFlow.Scenarios.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExpenseFlow.Scenarios.Tests;

/// <summary>
/// Base class for scenario tests that require full infrastructure.
/// Provides PostgreSQL via Testcontainers and WireMock for external services.
/// </summary>
[Trait("Category", "Scenario")]
[Collection("PostgreSQL")]
public abstract class ScenarioTestBase : IAsyncLifetime
{
    protected readonly PostgresContainerFixture PostgresFixture;
    protected readonly WireMockFixture WireMockFixture;
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;

    protected ScenarioTestBase(PostgresContainerFixture postgresFixture)
    {
        PostgresFixture = postgresFixture;
        WireMockFixture = new WireMockFixture();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Override connection strings to use test containers
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = PostgresFixture.ConnectionString,
                        ["Services:DocumentIntelligence:Endpoint"] = WireMockFixture.BaseUrl,
                        ["Services:OpenAI:Endpoint"] = WireMockFixture.BaseUrl,
                        ["Services:Vista:Endpoint"] = WireMockFixture.BaseUrl,
                        ["Testing:IsScenarioTest"] = "true"
                    });
                });

                builder.ConfigureServices(services =>
                {
                    // Additional test service overrides can be added here
                });
            });

        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    /// <summary>
    /// Initialize test infrastructure before each test.
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        await WireMockFixture.InitializeAsync();

        // Load default stubs
        var mocksPath = Path.Combine(AppContext.BaseDirectory, "Mocks");
        if (Directory.Exists(mocksPath))
        {
            foreach (var file in Directory.GetFiles(mocksPath, "*.json"))
            {
                WireMockFixture.LoadStubsFromFile(file);
            }
        }
    }

    /// <summary>
    /// Clean up after each test.
    /// </summary>
    public virtual async Task DisposeAsync()
    {
        await WireMockFixture.DisposeAsync();
        Client.Dispose();
        await Factory.DisposeAsync();
    }

    /// <summary>
    /// Gets a scoped service from the test application.
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return Factory.Services.CreateScope().ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Resets all test infrastructure to clean state.
    /// </summary>
    protected async Task ResetAsync()
    {
        await PostgresFixture.ResetDatabaseAsync();
        WireMockFixture.Reset();
    }

    /// <summary>
    /// Helper to wait for background processing to complete.
    /// </summary>
    protected static async Task WaitForProcessingAsync(
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(30);
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(100);
        var deadline = DateTime.UtcNow.Add(maxWait);

        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(interval);
        }
    }
}
