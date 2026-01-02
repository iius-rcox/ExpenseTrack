namespace ExpenseFlow.Scenarios.Tests.Chaos;

/// <summary>
/// Configuration for chaos testing behavior.
/// Controlled via environment variables for CI/CD integration.
/// </summary>
public class ChaosConfiguration
{
    /// <summary>
    /// Whether chaos injection is enabled.
    /// Set via CHAOS_ENABLED environment variable.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// The rate at which faults are injected (0.0 to 1.0).
    /// Set via CHAOS_INJECTION_RATE environment variable.
    /// Default: 0.05 (5%)
    /// </summary>
    public double InjectionRate { get; init; }

    /// <summary>
    /// Maximum latency to inject in milliseconds.
    /// Set via CHAOS_MAX_LATENCY_MS environment variable.
    /// </summary>
    public int MaxLatencyMs { get; init; }

    /// <summary>
    /// Creates configuration from environment variables.
    /// </summary>
    public static ChaosConfiguration FromEnvironment()
    {
        return new ChaosConfiguration
        {
            Enabled = bool.TryParse(
                Environment.GetEnvironmentVariable("CHAOS_ENABLED"),
                out var enabled) && enabled,

            InjectionRate = double.TryParse(
                Environment.GetEnvironmentVariable("CHAOS_INJECTION_RATE"),
                out var rate) ? Math.Clamp(rate, 0, 1) : 0.05,

            MaxLatencyMs = int.TryParse(
                Environment.GetEnvironmentVariable("CHAOS_MAX_LATENCY_MS"),
                out var latency) ? latency : 5000
        };
    }

    /// <summary>
    /// Creates configuration for testing with specified values.
    /// </summary>
    public static ChaosConfiguration ForTesting(
        bool enabled = true,
        double injectionRate = 0.25,
        int maxLatencyMs = 3000)
    {
        return new ChaosConfiguration
        {
            Enabled = enabled,
            InjectionRate = injectionRate,
            MaxLatencyMs = maxLatencyMs
        };
    }
}
