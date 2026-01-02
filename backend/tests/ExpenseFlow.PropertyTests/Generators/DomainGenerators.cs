using FsCheck;

namespace ExpenseFlow.PropertyTests.Generators;

/// <summary>
/// Custom FsCheck generators for ExpenseFlow domain types.
/// These generators produce domain-valid test data for property-based testing.
/// Uses FsCheck 2.x API patterns.
/// </summary>
public static class DomainGenerators
{
    /// <summary>
    /// Generates valid monetary amounts (positive, reasonable range).
    /// </summary>
    public static Arbitrary<decimal> ValidAmount() =>
        Arb.From(
            Gen.Choose(1, 999999)
                .Select(n => Math.Round((decimal)n / 100, 2)));

    /// <summary>
    /// Generates valid dates within a reasonable range (up to 2 years ago).
    /// </summary>
    public static Arbitrary<DateTime> ValidDate() =>
        Arb.From(
            Gen.Choose(0, 365 * 2)
                .Select(days => DateTime.UtcNow.AddDays(-days)));

    /// <summary>
    /// Generates valid date ranges (start before end).
    /// </summary>
    public static Arbitrary<(DateTime Start, DateTime End)> ValidDateRange() =>
        Arb.From(
            from daysAgo in Gen.Choose(1, 365)
            from duration in Gen.Choose(1, 30)
            let start = DateTime.UtcNow.AddDays(-daysAgo)
            let end = start.AddDays(duration)
            select (start, end));

    /// <summary>
    /// Generates valid GL account codes (4-digit format).
    /// </summary>
    public static Arbitrary<string> ValidGlCode() =>
        Arb.From(
            Gen.Choose(1000, 9999)
                .Select(n => n.ToString()));

    /// <summary>
    /// Generates valid vendor names.
    /// </summary>
    public static Arbitrary<string> ValidVendorName() =>
        Arb.From(
            Gen.Elements(
                "Amazon", "Starbucks", "Delta Airlines", "Marriott",
                "Office Depot", "FedEx", "UPS", "Home Depot",
                "Best Buy", "Costco", "Target", "Walmart",
                "Shell", "Chevron", "United Airlines", "Hilton"));

    /// <summary>
    /// Generates valid expense categories.
    /// </summary>
    public static Arbitrary<string> ValidCategory() =>
        Arb.From(
            Gen.Elements(
                "Travel", "Meals", "Office Supplies", "Equipment",
                "Software", "Training", "Marketing", "Professional Services"));

    /// <summary>
    /// Generates normalized embedding vectors (1536 dimensions for OpenAI ada-002).
    /// </summary>
    public static Arbitrary<float[]> ValidEmbedding() =>
        Arb.From(
            Gen.ArrayOf(1536, Gen.Choose(-1000, 1000).Select(n => n / 1000f))
                .Select(NormalizeVector));

    /// <summary>
    /// Generates valid match confidence scores (0.0 to 1.0).
    /// </summary>
    public static Arbitrary<double> ValidConfidence() =>
        Arb.From(
            Gen.Choose(0, 100)
                .Select(n => n / 100.0));

    /// <summary>
    /// Generates valid expense statuses.
    /// </summary>
    public static Arbitrary<string> ValidStatus() =>
        Arb.From(
            Gen.Elements("Draft", "Pending", "Approved", "Rejected", "Submitted"));

    /// <summary>
    /// Generates valid currency codes (ISO 4217).
    /// </summary>
    public static Arbitrary<string> ValidCurrencyCode() =>
        Arb.From(
            Gen.Elements("USD", "EUR", "GBP", "CAD", "AUD", "JPY"));

    /// <summary>
    /// Generates valid email addresses.
    /// </summary>
    public static Arbitrary<string> ValidEmail() =>
        Arb.From(
            from name in Gen.Elements("john", "jane", "bob", "alice", "test", "user")
            from domain in Gen.Elements("example.com", "test.org", "company.io")
            select $"{name}@{domain}");

    private static float[] NormalizeVector(float[] vector)
    {
        var magnitude = MathF.Sqrt(vector.Sum(x => x * x));
        if (magnitude == 0 || float.IsNaN(magnitude) || float.IsInfinity(magnitude))
            return Enumerable.Repeat(1f / MathF.Sqrt(vector.Length), vector.Length).ToArray();

        return vector.Select(x => x / magnitude).ToArray();
    }
}
