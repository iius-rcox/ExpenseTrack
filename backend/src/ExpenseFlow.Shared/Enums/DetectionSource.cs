namespace ExpenseFlow.Shared.Enums;

/// <summary>
/// How a subscription was detected.
/// </summary>
public enum DetectionSource
{
    /// <summary>Detected via 2+ consecutive months of similar charges</summary>
    PatternMatch = 0,

    /// <summary>Matched against KnownSubscriptionVendor seed data</summary>
    SeedData = 1
}
