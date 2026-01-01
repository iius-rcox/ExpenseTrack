using Polly;
using Polly.Simmy;
using Polly.Simmy.Fault;
using Polly.Simmy.Latency;

namespace ExpenseFlow.Scenarios.Tests.Chaos;

/// <summary>
/// Factory for creating Polly chaos engineering strategies.
/// Uses Polly v8 ResiliencePipeline patterns.
/// </summary>
public static class ChaosStrategies
{
    /// <summary>
    /// Creates a chaos strategy that injects HTTP request exceptions.
    /// Simulates network failures, DNS issues, connection timeouts.
    /// </summary>
    public static ResiliencePipeline CreateHttpFaultStrategy(ChaosConfiguration config)
    {
        return new ResiliencePipelineBuilder()
            .AddChaosFault(new ChaosFaultStrategyOptions
            {
                InjectionRate = config.InjectionRate,
                EnabledGenerator = static args => new ValueTask<bool>(
                    bool.TryParse(
                        Environment.GetEnvironmentVariable("CHAOS_ENABLED"),
                        out var enabled) && enabled),
                FaultGenerator = static args => new ValueTask<Exception?>(
                    new HttpRequestException("Chaos: Simulated network failure"))
            })
            .Build();
    }

    /// <summary>
    /// Creates a chaos strategy that injects latency.
    /// Simulates slow network, overloaded services, database contention.
    /// </summary>
    public static ResiliencePipeline CreateLatencyStrategy(ChaosConfiguration config)
    {
        return new ResiliencePipelineBuilder()
            .AddChaosLatency(new ChaosLatencyStrategyOptions
            {
                InjectionRate = config.InjectionRate,
                EnabledGenerator = static args => new ValueTask<bool>(
                    bool.TryParse(
                        Environment.GetEnvironmentVariable("CHAOS_ENABLED"),
                        out var enabled) && enabled),
                Latency = TimeSpan.FromMilliseconds(config.MaxLatencyMs)
            })
            .Build();
    }

    /// <summary>
    /// Creates a chaos strategy for database operations.
    /// Injects timeouts and connection failures.
    /// </summary>
    public static ResiliencePipeline CreateDatabaseChaosStrategy(ChaosConfiguration config)
    {
        return new ResiliencePipelineBuilder()
            .AddChaosFault(new ChaosFaultStrategyOptions
            {
                InjectionRate = config.InjectionRate / 2, // Lower rate for DB
                EnabledGenerator = static args => new ValueTask<bool>(
                    bool.TryParse(
                        Environment.GetEnvironmentVariable("CHAOS_ENABLED"),
                        out var enabled) && enabled),
                FaultGenerator = static args => new ValueTask<Exception?>(
                    new TimeoutException("Chaos: Database timeout"))
            })
            .Build();
    }

    /// <summary>
    /// Creates a chaos strategy for external API calls (Azure AI, OpenAI).
    /// Injects rate limiting (429) and service unavailable (503) scenarios.
    /// </summary>
    public static ResiliencePipeline<HttpResponseMessage> CreateApiChaosStrategy(ChaosConfiguration config)
    {
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddChaosResult(new ChaosResultStrategyOptions<HttpResponseMessage>
            {
                InjectionRate = config.InjectionRate,
                EnabledGenerator = static args => new ValueTask<bool>(
                    bool.TryParse(
                        Environment.GetEnvironmentVariable("CHAOS_ENABLED"),
                        out var enabled) && enabled),
                ResultGenerator = static args =>
                {
                    // Randomly choose between 429 and 503
                    var statusCode = Random.Shared.Next(2) == 0
                        ? System.Net.HttpStatusCode.TooManyRequests
                        : System.Net.HttpStatusCode.ServiceUnavailable;

                    return new ValueTask<HttpResponseMessage?>(
                        new HttpResponseMessage(statusCode)
                        {
                            Content = new StringContent($"{{\"error\":\"Chaos: {statusCode}\"}}")
                        });
                }
            })
            .Build();
    }

    /// <summary>
    /// Creates a combined chaos strategy with multiple failure modes.
    /// </summary>
    public static ResiliencePipeline CreateCombinedStrategy(ChaosConfiguration config)
    {
        return new ResiliencePipelineBuilder()
            // 50% of chaos events are faults
            .AddChaosFault(new ChaosFaultStrategyOptions
            {
                InjectionRate = config.InjectionRate * 0.5,
                EnabledGenerator = static args => new ValueTask<bool>(
                    bool.TryParse(
                        Environment.GetEnvironmentVariable("CHAOS_ENABLED"),
                        out var enabled) && enabled),
                FaultGenerator = static args => new ValueTask<Exception?>(
                    new InvalidOperationException("Chaos: Service failure"))
            })
            // 50% of chaos events are latency
            .AddChaosLatency(new ChaosLatencyStrategyOptions
            {
                InjectionRate = config.InjectionRate * 0.5,
                EnabledGenerator = static args => new ValueTask<bool>(
                    bool.TryParse(
                        Environment.GetEnvironmentVariable("CHAOS_ENABLED"),
                        out var enabled) && enabled),
                Latency = TimeSpan.FromMilliseconds(config.MaxLatencyMs / 2)
            })
            .Build();
    }
}
