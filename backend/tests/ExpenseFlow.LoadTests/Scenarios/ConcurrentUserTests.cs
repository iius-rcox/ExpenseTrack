using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;

namespace ExpenseFlow.LoadTests.Scenarios;

/// <summary>
/// Load tests for concurrent user operations.
/// SC-006: 95th percentile response time remains under 2 seconds with 20 concurrent users.
/// </summary>
public class ConcurrentUserTests : ScenarioBase
{
    private const int TargetConcurrentUsers = 20;
    private const int MaxP95ResponseTimeMs = 2000; // 2 seconds

    /// <summary>
    /// Test typical user operations with 20 concurrent users.
    /// Operations: view receipts, view transactions, view reports.
    /// Target: 95th percentile response time < 2 seconds.
    /// </summary>
    [Fact(Skip = "Load test - run manually against staging")]
    public void ConcurrentUsers_TypicalOperations_MeetsResponseTimeTarget()
    {
        using var client = CreateHttpClient();

        // Scenario 1: View receipts list
        var viewReceiptsScenario = Scenario.Create("view_receipts", async context =>
        {
            var request = Http.CreateRequest("GET", "/api/receipts?page=1&pageSize=20")
                .WithHeader("Authorization", $"Bearer {AuthToken}");

            var response = await Http.Send(client, request);

            return response.IsError
                ? Response.Fail(statusCode: response.StatusCode.ToString())
                : Response.Ok(statusCode: response.StatusCode.ToString());
        })
        .WithLoadSimulations(CreateConcurrentUserLoad());

        // Scenario 2: View transactions list
        var viewTransactionsScenario = Scenario.Create("view_transactions", async context =>
        {
            var request = Http.CreateRequest("GET", "/api/transactions?page=1&pageSize=20")
                .WithHeader("Authorization", $"Bearer {AuthToken}");

            var response = await Http.Send(client, request);

            return response.IsError
                ? Response.Fail(statusCode: response.StatusCode.ToString())
                : Response.Ok(statusCode: response.StatusCode.ToString());
        })
        .WithLoadSimulations(CreateConcurrentUserLoad());

        // Scenario 3: View expense reports list
        var viewReportsScenario = Scenario.Create("view_reports", async context =>
        {
            var request = Http.CreateRequest("GET", "/api/reports?page=1&pageSize=20")
                .WithHeader("Authorization", $"Bearer {AuthToken}");

            var response = await Http.Send(client, request);

            return response.IsError
                ? Response.Fail(statusCode: response.StatusCode.ToString())
                : Response.Ok(statusCode: response.StatusCode.ToString());
        })
        .WithLoadSimulations(CreateConcurrentUserLoad());

        // Scenario 4: View cache statistics
        var viewCacheStatsScenario = Scenario.Create("view_cache_stats", async context =>
        {
            var request = Http.CreateRequest("GET", "/api/cache/statistics")
                .WithHeader("Authorization", $"Bearer {AuthToken}");

            var response = await Http.Send(client, request);

            return response.IsError
                ? Response.Fail(statusCode: response.StatusCode.ToString())
                : Response.Ok(statusCode: response.StatusCode.ToString());
        })
        .WithLoadSimulations(CreateConcurrentUserLoad());

        var result = NBomberRunner
            .RegisterScenarios(
                viewReceiptsScenario,
                viewTransactionsScenario,
                viewReportsScenario,
                viewCacheStatsScenario)
            .WithReportFolder(Path.Combine("Reports", "ConcurrentUsers", DateTime.Now.ToString("yyyy-MM-dd_HH-mm")))
            .Run();

        // Validate results for each scenario
        foreach (var scenarioStats in result.ScenarioStats)
        {
            Console.WriteLine($"\n=== {scenarioStats.ScenarioName} ===");
            Console.WriteLine($"Total Requests: {scenarioStats.AllRequestCount}");
            Console.WriteLine($"Success Rate: {scenarioStats.Ok.Request.Percent}%");
            Console.WriteLine($"Mean Latency: {scenarioStats.Ok.Latency.MeanMs}ms");
            Console.WriteLine($"P95 Latency: {scenarioStats.Ok.Latency.Percent95}ms");
            Console.WriteLine($"P99 Latency: {scenarioStats.Ok.Latency.Percent99}ms");

            // Assert P95 response time is under 2 seconds
            Assert.True(
                scenarioStats.Ok.Latency.Percent95 < MaxP95ResponseTimeMs,
                $"{scenarioStats.ScenarioName}: P95 latency {scenarioStats.Ok.Latency.Percent95}ms exceeded {MaxP95ResponseTimeMs}ms");

            // Assert success rate is above 95%
            Assert.True(
                scenarioStats.Ok.Request.Percent >= 95,
                $"{scenarioStats.ScenarioName}: Success rate {scenarioStats.Ok.Request.Percent}% below 95%");
        }
    }

    /// <summary>
    /// Test mixed user operations simulating real-world usage patterns.
    /// </summary>
    [Fact(Skip = "Load test - run manually against staging")]
    public void ConcurrentUsers_MixedWorkload_MeetsPerformanceTargets()
    {
        using var client = CreateHttpClient();

        // Mixed workload scenario with weighted operations
        var mixedWorkloadScenario = Scenario.Create("mixed_workload", async context =>
        {
            // Randomly select operation type (weighted towards reads)
            var random = new Random(context.InvocationNumber);
            var operationType = random.Next(100);

            HttpRequestMessage? httpRequest = null;
            string endpoint;

            if (operationType < 40)
            {
                // 40%: View receipts
                endpoint = "/api/receipts?page=1&pageSize=20";
                httpRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
            }
            else if (operationType < 70)
            {
                // 30%: View transactions
                endpoint = "/api/transactions?page=1&pageSize=20";
                httpRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
            }
            else if (operationType < 85)
            {
                // 15%: View reports
                endpoint = "/api/reports?page=1&pageSize=20";
                httpRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
            }
            else if (operationType < 95)
            {
                // 10%: View matches
                endpoint = "/api/matches?page=1&pageSize=20";
                httpRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
            }
            else
            {
                // 5%: View travel periods
                endpoint = "/api/travel-periods?page=1&pageSize=20";
                httpRequest = new HttpRequestMessage(HttpMethod.Get, endpoint);
            }

            var request = Http.CreateRequest("GET", endpoint)
                .WithHeader("Authorization", $"Bearer {AuthToken}");

            var response = await Http.Send(client, request);

            return response.IsError
                ? Response.Fail(statusCode: response.StatusCode.ToString())
                : Response.Ok(statusCode: response.StatusCode.ToString());
        })
        .WithLoadSimulations(
            // Warm up
            Simulation.RampingInject(
                rate: TargetConcurrentUsers,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(1)),
            // Sustained load
            Simulation.Inject(
                rate: TargetConcurrentUsers,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(5)));

        var result = NBomberRunner
            .RegisterScenarios(mixedWorkloadScenario)
            .WithReportFolder(Path.Combine("Reports", "MixedWorkload", DateTime.Now.ToString("yyyy-MM-dd_HH-mm")))
            .Run();

        var stats = result.ScenarioStats[0];

        Console.WriteLine($"\n=== Mixed Workload Results ===");
        Console.WriteLine($"Total Requests: {stats.AllRequestCount}");
        Console.WriteLine($"Success Rate: {stats.Ok.Request.Percent}%");
        Console.WriteLine($"Mean Latency: {stats.Ok.Latency.MeanMs}ms");
        Console.WriteLine($"P95 Latency: {stats.Ok.Latency.Percent95}ms");
        Console.WriteLine($"P99 Latency: {stats.Ok.Latency.Percent99}ms");
        Console.WriteLine($"RPS: {stats.Ok.Request.RPS}");

        Assert.True(stats.Ok.Latency.Percent95 < MaxP95ResponseTimeMs,
            $"P95 latency {stats.Ok.Latency.Percent95}ms exceeded {MaxP95ResponseTimeMs}ms");
        Assert.True(stats.Ok.Request.Percent >= 95,
            $"Success rate {stats.Ok.Request.Percent}% below 95%");
    }

    /// <summary>
    /// Test API response under increasing load to find breaking point.
    /// </summary>
    [Fact(Skip = "Load test - run manually against staging")]
    public void ConcurrentUsers_SpikeTest_HandlesLoadSpikes()
    {
        using var client = CreateHttpClient();

        var spikeScenario = Scenario.Create("spike_test", async context =>
        {
            var request = Http.CreateRequest("GET", "/api/receipts?page=1&pageSize=20")
                .WithHeader("Authorization", $"Bearer {AuthToken}");

            var response = await Http.Send(client, request);

            return response.IsError
                ? Response.Fail(statusCode: response.StatusCode.ToString())
                : Response.Ok(statusCode: response.StatusCode.ToString());
        })
        .WithLoadSimulations(
            // Normal load
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            // Spike to 50 users
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            // Back to normal
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            // Another spike
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)),
            // Recovery
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1)));

        var result = NBomberRunner
            .RegisterScenarios(spikeScenario)
            .WithReportFolder(Path.Combine("Reports", "SpikeTest", DateTime.Now.ToString("yyyy-MM-dd_HH-mm")))
            .Run();

        var stats = result.ScenarioStats[0];

        Console.WriteLine($"\n=== Spike Test Results ===");
        Console.WriteLine($"Total Requests: {stats.AllRequestCount}");
        Console.WriteLine($"Success Rate: {stats.Ok.Request.Percent}%");
        Console.WriteLine($"Mean Latency: {stats.Ok.Latency.MeanMs}ms");
        Console.WriteLine($"P95 Latency: {stats.Ok.Latency.Percent95}ms");
        Console.WriteLine($"Max Latency: {stats.Ok.Latency.MaxMs}ms");

        // During spikes, allow slightly higher latency but still require high success rate
        Assert.True(stats.Ok.Request.Percent >= 90,
            $"Success rate {stats.Ok.Request.Percent}% below 90% during spike test");
    }

    private static LoadSimulation[] CreateConcurrentUserLoad()
    {
        return new[]
        {
            // Warm up: Ramp up over 30 seconds
            Simulation.RampingInject(
                rate: TargetConcurrentUsers,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromSeconds(30)),

            // Sustained load: Hold for 3 minutes
            Simulation.Inject(
                rate: TargetConcurrentUsers,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(3))
        };
    }
}
