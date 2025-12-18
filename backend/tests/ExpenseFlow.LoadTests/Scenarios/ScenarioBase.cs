using Microsoft.Extensions.Configuration;
using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http;
using NBomber.Http.CSharp;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ExpenseFlow.LoadTests.Scenarios;

/// <summary>
/// Base class for NBomber load test scenarios providing common configuration and HTTP client setup.
/// </summary>
public abstract class ScenarioBase
{
    protected readonly IConfiguration Configuration;
    protected readonly string BaseUrl;
    protected readonly string AuthToken;
    protected readonly TimeSpan DefaultTimeout;

    protected ScenarioBase()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        BaseUrl = Configuration["LoadTest:BaseUrl"] ?? "https://localhost:7123";
        AuthToken = Configuration["LoadTest:AuthToken"] ?? string.Empty;
        DefaultTimeout = TimeSpan.FromSeconds(
            int.Parse(Configuration["LoadTest:DefaultTimeoutSeconds"] ?? "30"));
    }

    /// <summary>
    /// Creates a configured HTTP client for load testing.
    /// </summary>
    protected HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        var client = new HttpClient(handler)
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = DefaultTimeout
        };

        if (!string.IsNullOrEmpty(AuthToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", AuthToken);
        }

        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        return client;
    }

    /// <summary>
    /// Creates an NBomber HTTP client factory with authentication configured.
    /// </summary>
    protected IClientFactory<HttpClient> CreateClientFactory(string name = "http_client")
    {
        return HttpClientFactory.Create(
            name: name,
            clientCount: 20,
            initClient: (number, context) =>
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
                };

                var client = new HttpClient(handler)
                {
                    BaseAddress = new Uri(BaseUrl),
                    Timeout = DefaultTimeout
                };

                if (!string.IsNullOrEmpty(AuthToken))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", AuthToken);
                }

                return Task.FromResult(client);
            });
    }

    /// <summary>
    /// Runs the scenario with specified duration and warm-up period.
    /// </summary>
    protected NBomberRunner RunScenario(
        ScenarioProps scenario,
        TimeSpan warmUpDuration,
        TimeSpan runDuration)
    {
        return NBomberRunner
            .RegisterScenarios(scenario)
            .WithWorkerPlugins(new HttpMetricsPlugin())
            .WithReportFolder(Path.Combine("Reports", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")))
            .WithReportFormats(ReportFormat.Html, ReportFormat.Md);
    }

    /// <summary>
    /// Creates standard scenario configuration with rate limiting.
    /// </summary>
    protected static LoadSimulation[] CreateLoadSimulation(
        int targetUsersCount,
        TimeSpan warmUpDuration,
        TimeSpan holdDuration)
    {
        return new[]
        {
            // Ramp up to target users
            Simulation.RampingInject(
                rate: targetUsersCount,
                interval: TimeSpan.FromSeconds(1),
                during: warmUpDuration),

            // Hold at target load
            Simulation.Inject(
                rate: targetUsersCount,
                interval: TimeSpan.FromSeconds(1),
                during: holdDuration)
        };
    }

    /// <summary>
    /// Standard success criteria for load tests.
    /// </summary>
    protected static class SuccessCriteria
    {
        public const double MinSuccessRate = 0.95;  // 95% success rate
        public const int MaxP95ResponseTimeMs = 2000;  // 2 seconds
        public const int MaxP99ResponseTimeMs = 5000;  // 5 seconds
    }

    /// <summary>
    /// Serializes object to JSON string content.
    /// </summary>
    protected static StringContent ToJsonContent(object data)
    {
        var json = JsonSerializer.Serialize(data);
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }
}
