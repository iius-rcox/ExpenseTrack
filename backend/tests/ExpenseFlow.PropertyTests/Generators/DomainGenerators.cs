using FsCheck;

namespace ExpenseFlow.PropertyTests.Generators;

/// <summary>
/// Custom FsCheck generators for ExpenseFlow domain types.
/// These generators produce domain-valid test data for property-based testing.
/// </summary>
public static class DomainGenerators
{
    /// <summary>
    /// Generates valid monetary amounts (positive, reasonable range).
    /// </summary>
    public static Arbitrary<decimal> ValidAmount() =>
        Arb.Default.Decimal()
            .Filter(d => d > 0 && d < 1_000_000)
            .Generator
            .Select(d => Math.Round(d, 2))
            .ToArbitrary();

    /// <summary>
    /// Generates valid dates within a reasonable range.
    /// </summary>
    public static Arbitrary<DateTime> ValidDate() =>
        Gen.Choose(0, 365 * 2) // Up to 2 years
            .Select(days => DateTime.UtcNow.AddDays(-days))
            .ToArbitrary();

    /// <summary>
    /// Generates valid date ranges (start before end).
    /// </summary>
    public static Arbitrary<(DateTime Start, DateTime End)> ValidDateRange() =>
        from daysAgo in Gen.Choose(1, 365)
        from duration in Gen.Choose(1, 30)
        let start = DateTime.UtcNow.AddDays(-daysAgo)
        let end = start.AddDays(duration)
        select (start, end);

    /// <summary>
    /// Generates valid GL account codes (4-digit format).
    /// </summary>
    public static Arbitrary<string> ValidGlCode() =>
        Gen.Choose(1000, 9999)
            .Select(n => n.ToString())
            .ToArbitrary();

    /// <summary>
    /// Generates valid vendor names.
    /// </summary>
    public static Arbitrary<string> ValidVendorName() =>
        Gen.Elements(
            "Amazon", "Starbucks", "Delta Airlines", "Marriott",
            "Office Depot", "FedEx", "UPS", "Home Depot",
            "Best Buy", "Costco", "Target", "Walmart",
            "Shell", "Chevron", "United Airlines", "Hilton")
            .ToArbitrary();

    /// <summary>
    /// Generates valid expense categories.
    /// </summary>
    public static Arbitrary<string> ValidCategory() =>
        Gen.Elements(
            "Travel", "Meals", "Office Supplies", "Equipment",
            "Software", "Training", "Marketing", "Professional Services")
            .ToArbitrary();

    /// <summary>
    /// Generates normalized embedding vectors (1536 dimensions).
    /// </summary>
    public static Arbitrary<float[]> ValidEmbedding() =>
        Gen.ArrayOf(1536, Arb.Default.Float().Generator)
            .Select(NormalizeVector)
            .ToArbitrary();

    /// <summary>
    /// Generates valid match confidence scores (0.0 to 1.0).
    /// </summary>
    public static Arbitrary<double> ValidConfidence() =>
        Gen.Choose(0, 100)
            .Select(n => n / 100.0)
            .ToArbitrary();

    /// <summary>
    /// Generates valid expense statuses.
    /// </summary>
    public static Arbitrary<string> ValidStatus() =>
        Gen.Elements("Draft", "Pending", "Approved", "Rejected", "Submitted")
            .ToArbitrary();

    private static float[] NormalizeVector(float[] vector)
    {
        var magnitude = MathF.Sqrt(vector.Sum(x => x * x));
        if (magnitude == 0)
            return vector.Select(_ => 1f / MathF.Sqrt(vector.Length)).ToArray();

        return vector.Select(x => x / magnitude).ToArray();
    }
}

/// <summary>
/// Extension methods for FsCheck generator composition.
/// </summary>
public static class GeneratorExtensions
{
    /// <summary>
    /// Converts a Gen to an Arbitrary with a default shrinker.
    /// </summary>
    public static Arbitrary<T> ToArbitrary<T>(this Gen<T> gen) =>
        Arb.From(gen);
}
