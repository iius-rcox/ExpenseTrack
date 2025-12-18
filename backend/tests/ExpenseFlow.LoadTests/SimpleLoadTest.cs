using NBomber.Contracts;
using NBomber.CSharp;
using NBomber.Http.CSharp;
using Xunit;
using Xunit.Abstractions;

namespace ExpenseFlow.LoadTests;

public class SimpleLoadTest
{
    private readonly ITestOutputHelper _output;
    private const string BaseUrl = "https://staging.expense.ii-us.com";

    public SimpleLoadTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void HealthEndpoint_LoadTest()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
        };
        using var client = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };

        var scenario = Scenario.Create("health_check", async context =>
        {
            var request = Http.CreateRequest("GET", "/health");
            var response = await Http.Send(client, request);
            return response.IsError
                ? Response.Fail(statusCode: response.StatusCode.ToString())
                : Response.Ok(statusCode: response.StatusCode.ToString());
        })
        .WithLoadSimulations(
            Simulation.RampingInject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)),
            Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
            Simulation.Inject(rate: 50, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(20)),
            Simulation.Inject(rate: 10, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(10)));

        var result = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        var stats = result.ScenarioStats[0];

        _output.WriteLine($"=== Load Test Results ===");
        _output.WriteLine($"Base URL: {BaseUrl}");
        _output.WriteLine($"Total Requests: {stats.AllRequestCount}");
        _output.WriteLine($"OK: {stats.Ok.Request.Count}, Failed: {stats.Fail.Request.Count}");
        _output.WriteLine($"RPS: {stats.Ok.Request.RPS:F1}");
        _output.WriteLine($"Mean Latency: {stats.Ok.Latency.MeanMs:F1}ms");
        _output.WriteLine($"P50: {stats.Ok.Latency.Percent50}ms");
        _output.WriteLine($"P75: {stats.Ok.Latency.Percent75}ms");
        _output.WriteLine($"P95: {stats.Ok.Latency.Percent95}ms");
        _output.WriteLine($"P99: {stats.Ok.Latency.Percent99}ms");
        _output.WriteLine($"Max: {stats.Ok.Latency.MaxMs}ms");

        Assert.True(stats.Ok.Request.Count > stats.Fail.Request.Count * 10, "Too many failures");
        Assert.True(stats.Ok.Latency.Percent95 < 1000, $"P95 {stats.Ok.Latency.Percent95}ms > 1000ms");
    }
}
