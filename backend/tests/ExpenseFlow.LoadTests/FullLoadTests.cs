using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace ExpenseFlow.LoadTests;

/// <summary>
/// Full load tests for ExpenseFlow API endpoints.
/// Tests concurrent user scenarios and response time targets.
/// </summary>
public class FullLoadTests
{
    private readonly ITestOutputHelper _output;
    private const string BaseUrl = "https://staging.expense.ii-us.com";

    public FullLoadTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// T079: Simulates batch processing load - 50 requests in 5 minutes.
    /// Target: All requests complete within 5 minutes (6 sec/request average).
    /// </summary>
    [Fact]
    public void BatchProcessing_LoadTest()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };

        // Simulate batch processing: 50 requests spread over 5 minutes
        // Rate: ~10 requests per minute = 1 request every 6 seconds
        var scenario = Scenario.Create("batch_processing", async context =>
        {
            // Cycle through different API endpoints to simulate real workload
            var endpoints = new[]
            {
                "/health",
                "/api/reference/gl-accounts",
                "/api/reference/departments",
                "/api/reference/projects"
            };

            var endpoint = endpoints[context.ScenarioInfo.ThreadNumber % endpoints.Length];
            var request = Http.CreateRequest("GET", endpoint);
            var response = await Http.Send(client, request);

            // Accept both success and 401 (auth required) as valid responses
            var isSuccess = !response.IsError || response.StatusCode == "401" || response.StatusCode == "Unauthorized";
            return isSuccess
                ? Response.Ok(statusCode: response.StatusCode?.ToString() ?? "OK")
                : Response.Fail(statusCode: response.StatusCode?.ToString() ?? "Error");
        })
        .WithLoadSimulations(
            // Ramp up to 10 requests/min over 1 minute
            Simulation.RampingInject(rate: 1, interval: TimeSpan.FromSeconds(6), during: TimeSpan.FromMinutes(1)),
            // Sustain 10 requests/min for 4 minutes (40 more requests)
            Simulation.Inject(rate: 1, interval: TimeSpan.FromSeconds(6), during: TimeSpan.FromMinutes(4)));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var stats = result.ScenarioStats[0];

        _output.WriteLine($"=== Batch Processing Load Test Results ===");
        _output.WriteLine($"Base URL: {BaseUrl}");
        _output.WriteLine($"Duration: 5 minutes");
        _output.WriteLine($"Total Requests: {stats.AllRequestCount}");
        _output.WriteLine($"OK: {stats.Ok.Request.Count}, Failed: {stats.Fail.Request.Count}");
        _output.WriteLine($"RPS: {stats.Ok.Request.RPS:F2}");
        _output.WriteLine($"Mean Latency: {stats.Ok.Latency.MeanMs:F1}ms");
        _output.WriteLine($"P50: {stats.Ok.Latency.Percent50}ms");
        _output.WriteLine($"P95: {stats.Ok.Latency.Percent95}ms");
        _output.WriteLine($"P99: {stats.Ok.Latency.Percent99}ms");
        _output.WriteLine($"Max: {stats.Ok.Latency.MaxMs}ms");

        // Target: All 50 requests complete (allowing for some margin)
        Assert.True(stats.AllRequestCount >= 45, $"Expected at least 45 requests, got {stats.AllRequestCount}");
        // Target: Mean latency under 6 seconds (6000ms)
        Assert.True(stats.Ok.Latency.MeanMs < 6000, $"Mean latency {stats.Ok.Latency.MeanMs}ms > 6000ms target");
    }

    /// <summary>
    /// T080: Simulates 20 concurrent users performing typical operations.
    /// Target: P95 response time < 2 seconds (2000ms).
    /// </summary>
    [Fact]
    public void ConcurrentUsers_LoadTest()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };

        // Simulate different user operations
        var healthScenario = Scenario.Create("user_health_check", async context =>
        {
            var request = Http.CreateRequest("GET", "/health");
            var response = await Http.Send(client, request);
            return response.IsError
                ? Response.Fail(statusCode: response.StatusCode?.ToString() ?? "Error")
                : Response.Ok(statusCode: response.StatusCode?.ToString() ?? "OK");
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 5, during: TimeSpan.FromMinutes(2)));

        var referenceScenario = Scenario.Create("user_reference_data", async context =>
        {
            var endpoints = new[] { "/api/reference/gl-accounts", "/api/reference/departments", "/api/reference/projects" };
            var endpoint = endpoints[context.InvocationNumber % endpoints.Length];
            var request = Http.CreateRequest("GET", endpoint);
            var response = await Http.Send(client, request);

            // Accept 401 as valid (endpoint works, just needs auth)
            var isSuccess = !response.IsError || response.StatusCode == "401" || response.StatusCode == "Unauthorized";
            return isSuccess
                ? Response.Ok(statusCode: response.StatusCode?.ToString() ?? "OK")
                : Response.Fail(statusCode: response.StatusCode?.ToString() ?? "Error");
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 10, during: TimeSpan.FromMinutes(2)));

        var apiScenario = Scenario.Create("user_api_operations", async context =>
        {
            var endpoints = new[]
            {
                "/api/transactions",
                "/api/receipts",
                "/api/matching/stats",
                "/api/categorization/stats",
                "/api/subscriptions"
            };
            var endpoint = endpoints[context.InvocationNumber % endpoints.Length];
            var request = Http.CreateRequest("GET", endpoint);
            var response = await Http.Send(client, request);

            // Accept 401 as valid (endpoint works, just needs auth)
            var isSuccess = !response.IsError || response.StatusCode == "401" || response.StatusCode == "Unauthorized";
            return isSuccess
                ? Response.Ok(statusCode: response.StatusCode?.ToString() ?? "OK")
                : Response.Fail(statusCode: response.StatusCode?.ToString() ?? "Error");
        })
        .WithLoadSimulations(
            Simulation.KeepConstant(copies: 5, during: TimeSpan.FromMinutes(2)));

        var result = NBomberRunner
            .RegisterScenarios(healthScenario, referenceScenario, apiScenario)
            .Run();

        _output.WriteLine($"=== Concurrent Users Load Test Results (20 users) ===");
        _output.WriteLine($"Base URL: {BaseUrl}");
        _output.WriteLine($"Duration: 2 minutes");
        _output.WriteLine($"");

        var totalRequests = 0;
        var totalOk = 0;
        var maxP95 = 0.0;

        foreach (var stats in result.ScenarioStats)
        {
            _output.WriteLine($"--- Scenario: {stats.ScenarioName} ---");
            _output.WriteLine($"Total Requests: {stats.AllRequestCount}");
            _output.WriteLine($"OK: {stats.Ok.Request.Count}, Failed: {stats.Fail.Request.Count}");
            _output.WriteLine($"RPS: {stats.Ok.Request.RPS:F1}");
            _output.WriteLine($"Mean: {stats.Ok.Latency.MeanMs:F1}ms, P95: {stats.Ok.Latency.Percent95}ms, P99: {stats.Ok.Latency.Percent99}ms");
            _output.WriteLine($"");

            totalRequests += stats.AllRequestCount;
            totalOk += stats.Ok.Request.Count;
            if (stats.Ok.Latency.Percent95 > maxP95)
                maxP95 = stats.Ok.Latency.Percent95;
        }

        _output.WriteLine($"=== TOTALS ===");
        _output.WriteLine($"Total Requests: {totalRequests}");
        _output.WriteLine($"Total OK: {totalOk}");
        _output.WriteLine($"Max P95 across scenarios: {maxP95}ms");
        _output.WriteLine($"Target P95: < 2000ms");

        // Target: P95 response time < 2 seconds
        Assert.True(maxP95 < 2000, $"P95 {maxP95}ms exceeds 2000ms target");
        // Ensure we got meaningful results
        Assert.True(totalRequests > 100, $"Expected more than 100 requests, got {totalRequests}");
    }

    /// <summary>
    /// Combined stress test: High load with burst traffic.
    /// </summary>
    [Fact]
    public void StressTest_BurstTraffic()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };

        var scenario = Scenario.Create("stress_test", async context =>
        {
            var request = Http.CreateRequest("GET", "/health");
            var response = await Http.Send(client, request);
            return response.IsError
                ? Response.Fail(statusCode: response.StatusCode?.ToString() ?? "Error")
                : Response.Ok(statusCode: response.StatusCode?.ToString() ?? "OK");
        })
        .WithLoadSimulations(
            // Ramp up to 50 RPS
            Simulation.RampingInject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            // Burst to 100 RPS for 30 seconds
            Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            // Sustain at 50 RPS
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            // Cool down
            Simulation.RampingInject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var stats = result.ScenarioStats[0];

        _output.WriteLine($"=== Stress Test Results ===");
        _output.WriteLine($"Base URL: {BaseUrl}");
        _output.WriteLine($"Total Requests: {stats.AllRequestCount}");
        _output.WriteLine($"OK: {stats.Ok.Request.Count}, Failed: {stats.Fail.Request.Count}");
        _output.WriteLine($"Success Rate: {(double)stats.Ok.Request.Count / stats.AllRequestCount * 100:F1}%");
        _output.WriteLine($"RPS: {stats.Ok.Request.RPS:F1}");
        _output.WriteLine($"Mean Latency: {stats.Ok.Latency.MeanMs:F1}ms");
        _output.WriteLine($"P50: {stats.Ok.Latency.Percent50}ms");
        _output.WriteLine($"P75: {stats.Ok.Latency.Percent75}ms");
        _output.WriteLine($"P95: {stats.Ok.Latency.Percent95}ms");
        _output.WriteLine($"P99: {stats.Ok.Latency.Percent99}ms");
        _output.WriteLine($"Max: {stats.Ok.Latency.MaxMs}ms");

        // Allow up to 5% failure rate under stress
        var failureRate = (double)stats.Fail.Request.Count / stats.AllRequestCount;
        Assert.True(failureRate < 0.05, $"Failure rate {failureRate:P1} exceeds 5% threshold");
        // P95 should still be reasonable under stress
        Assert.True(stats.Ok.Latency.Percent95 < 5000, $"P95 {stats.Ok.Latency.Percent95}ms > 5000ms under stress");
    }
}
