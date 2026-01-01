using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace ExpenseFlow.LoadTests;

/// <summary>
/// CI-optimized baseline load test scenarios for nightly pipeline.
/// Runs faster than full tests but captures key performance metrics.
/// </summary>
/// <remarks>
/// This test is designed to:
/// 1. Run in under 5 minutes for CI integration
/// 2. Output metrics in JSON format for trend analysis
/// 3. Fail fast if baseline thresholds are exceeded
/// </remarks>
[Trait("Category", "Load")]
public class BaselineScenario
{
    private readonly ITestOutputHelper _output;
    private readonly string _baseUrl;

    public BaselineScenario(ITestOutputHelper output)
    {
        _output = output;
        // Allow override via environment variable for CI
        _baseUrl = Environment.GetEnvironmentVariable("LOAD_TEST_BASE_URL")
            ?? "https://staging.expense.ii-us.com";
    }

    /// <summary>
    /// Quick baseline test that runs in under 2 minutes.
    /// Captures P50, P95, P99 latencies for health and API endpoints.
    /// </summary>
    [Fact]
    public void Baseline_QuickSmokeTest()
    {
        using var client = CreateHttpClient();

        var scenario = Scenario.Create("baseline_smoke", async context =>
        {
            var endpoints = new[]
            {
                "/health",
                "/api/reference/gl-accounts",
                "/api/reference/departments"
            };

            var endpoint = endpoints[context.InvocationNumber % endpoints.Length];
            var request = Http.CreateRequest("GET", endpoint);
            var response = await Http.Send(client, request);

            // Accept 401 as success (endpoint works, needs auth)
            var isSuccess = !response.IsError ||
                response.StatusCode == "401" ||
                response.StatusCode == "Unauthorized";

            return isSuccess
                ? Response.Ok(statusCode: response.StatusCode?.ToString() ?? "OK")
                : Response.Fail(statusCode: response.StatusCode?.ToString() ?? "Error");
        })
        .WithLoadSimulations(
            // Quick ramp: 1-10 RPS over 30 seconds
            Simulation.RampingInject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            // Sustain: 10 RPS for 60 seconds
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(60)),
            // Cool down: 10 seconds
            Simulation.RampingInject(rate: 2, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var stats = result.ScenarioStats[0];
        OutputBaselineMetrics(stats, "baseline_smoke");

        // Baseline assertions
        Assert.True(stats.Fail.Request.Count < stats.AllRequestCount * 0.02,
            $"Failure rate {(double)stats.Fail.Request.Count / stats.AllRequestCount:P1} exceeds 2%");
        Assert.True(stats.Ok.Latency.Percent95 < 2000,
            $"P95 latency {stats.Ok.Latency.Percent95}ms exceeds 2000ms baseline");
    }

    /// <summary>
    /// Concurrent load baseline - 10 users for 2 minutes.
    /// Lower than full test but validates concurrency handling.
    /// </summary>
    [Fact]
    public void Baseline_ConcurrentUsers()
    {
        using var client = CreateHttpClient();

        var scenario = Scenario.Create("baseline_concurrent", async context =>
        {
            var request = Http.CreateRequest("GET", "/health");
            var response = await Http.Send(client, request);

            return response.IsError
                ? Response.Fail(statusCode: response.StatusCode?.ToString() ?? "Error")
                : Response.Ok(statusCode: response.StatusCode?.ToString() ?? "OK");
        })
        .WithLoadSimulations(
            // 10 concurrent users for 2 minutes
            Simulation.KeepConstant(copies: 10, during: TimeSpan.FromMinutes(2)));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var stats = result.ScenarioStats[0];
        OutputBaselineMetrics(stats, "baseline_concurrent");

        // Baseline assertions
        Assert.True(stats.Ok.Request.Count > 100,
            $"Expected > 100 successful requests, got {stats.Ok.Request.Count}");
        Assert.True(stats.Ok.Latency.Percent95 < 1500,
            $"P95 latency {stats.Ok.Latency.Percent95}ms exceeds 1500ms baseline");
    }

    /// <summary>
    /// Burst traffic baseline - test recovery after spike.
    /// </summary>
    [Fact]
    public void Baseline_BurstRecovery()
    {
        using var client = CreateHttpClient();

        var scenario = Scenario.Create("baseline_burst", async context =>
        {
            var request = Http.CreateRequest("GET", "/health");
            var response = await Http.Send(client, request);

            return response.IsError
                ? Response.Fail(statusCode: response.StatusCode?.ToString() ?? "Error")
                : Response.Ok(statusCode: response.StatusCode?.ToString() ?? "OK");
        })
        .WithLoadSimulations(
            // Baseline: 5 RPS
            Simulation.Inject(rate: 5, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            // Burst: 30 RPS
            Simulation.Inject(rate: 30, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            // Recovery: back to 5 RPS
            Simulation.Inject(rate: 5, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var stats = result.ScenarioStats[0];
        OutputBaselineMetrics(stats, "baseline_burst");

        // Allow higher failure rate during burst (3%)
        var failureRate = (double)stats.Fail.Request.Count / stats.AllRequestCount;
        Assert.True(failureRate < 0.03,
            $"Failure rate {failureRate:P1} exceeds 3% threshold during burst test");
    }

    /// <summary>
    /// Outputs metrics in JSON format for CI trend analysis.
    /// </summary>
    private void OutputBaselineMetrics(ScenarioStats stats, string scenarioName)
    {
        var metrics = new
        {
            scenario = scenarioName,
            timestamp = DateTime.UtcNow.ToString("O"),
            baseUrl = _baseUrl,
            duration_seconds = stats.Duration.TotalSeconds,
            total_requests = stats.AllRequestCount,
            ok_requests = stats.Ok.Request.Count,
            failed_requests = stats.Fail.Request.Count,
            success_rate = (double)stats.Ok.Request.Count / stats.AllRequestCount,
            rps = stats.Ok.Request.RPS,
            latency = new
            {
                mean_ms = stats.Ok.Latency.MeanMs,
                p50_ms = stats.Ok.Latency.Percent50,
                p75_ms = stats.Ok.Latency.Percent75,
                p95_ms = stats.Ok.Latency.Percent95,
                p99_ms = stats.Ok.Latency.Percent99,
                max_ms = stats.Ok.Latency.MaxMs
            },
            thresholds = new
            {
                p95_target_ms = 2000,
                failure_rate_target = 0.02,
                p95_passed = stats.Ok.Latency.Percent95 < 2000,
                failure_rate_passed = (double)stats.Fail.Request.Count / stats.AllRequestCount < 0.02
            }
        };

        var json = JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true });

        _output.WriteLine("=== BASELINE METRICS (JSON) ===");
        _output.WriteLine(json);
        _output.WriteLine("================================");

        // Also output human-readable summary
        _output.WriteLine("");
        _output.WriteLine($"Scenario: {scenarioName}");
        _output.WriteLine($"Base URL: {_baseUrl}");
        _output.WriteLine($"Duration: {stats.Duration.TotalSeconds:F1}s");
        _output.WriteLine($"Requests: {stats.Ok.Request.Count} OK / {stats.Fail.Request.Count} Failed");
        _output.WriteLine($"RPS: {stats.Ok.Request.RPS:F1}");
        _output.WriteLine($"Latency - Mean: {stats.Ok.Latency.MeanMs:F1}ms, P95: {stats.Ok.Latency.Percent95}ms, P99: {stats.Ok.Latency.Percent99}ms");
    }

    private HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
        };
        return new HttpClient(handler) { BaseAddress = new Uri(_baseUrl) };
    }
}
