namespace ExpenseFlow.Infrastructure.Configuration;

/// <summary>
/// Configuration options for analytics and cost estimation.
/// </summary>
public class AnalyticsOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Analytics";

    /// <summary>
    /// Target percentage for Tier 1 hit rate (default: 50%).
    /// </summary>
    public decimal Tier1HitRateTarget { get; set; } = 50.0m;

    /// <summary>
    /// Estimated cost per Tier 2 (embedding) operation in USD.
    /// </summary>
    public decimal Tier2CostPerOperation { get; set; } = 0.00002m;

    /// <summary>
    /// Estimated cost per Tier 3 (AI inference) operation in USD.
    /// </summary>
    public decimal Tier3CostPerOperation { get; set; } = 0.0003m;
}
