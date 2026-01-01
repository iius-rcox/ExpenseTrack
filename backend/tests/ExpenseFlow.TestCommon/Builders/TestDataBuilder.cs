namespace ExpenseFlow.TestCommon.Builders;

/// <summary>
/// Base class for fluent test data builders.
/// Implements the Builder pattern for creating test entities.
/// </summary>
/// <typeparam name="TEntity">The entity type to build</typeparam>
/// <typeparam name="TBuilder">The concrete builder type (for fluent returns)</typeparam>
public abstract class TestDataBuilder<TEntity, TBuilder>
    where TEntity : class
    where TBuilder : TestDataBuilder<TEntity, TBuilder>
{
    /// <summary>
    /// Unique prefix for test data identification.
    /// </summary>
    protected string TestPrefix { get; } = $"test_{Guid.NewGuid():N}";

    /// <summary>
    /// Builds the entity with current configuration.
    /// </summary>
    public abstract TEntity Build();

    /// <summary>
    /// Builds multiple entities with sequential variations.
    /// </summary>
    /// <param name="count">Number of entities to create</param>
    public virtual IEnumerable<TEntity> BuildMany(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return WithVariation(i).Build();
        }
    }

    /// <summary>
    /// Creates a variation of the builder for sequential builds.
    /// </summary>
    /// <param name="index">The variation index</param>
    protected abstract TBuilder WithVariation(int index);

    /// <summary>
    /// Returns this as the concrete builder type.
    /// </summary>
    protected TBuilder Self => (TBuilder)this;

    /// <summary>
    /// Gets a test-prefixed identifier.
    /// </summary>
    protected string TestId(string suffix = "") =>
        string.IsNullOrEmpty(suffix) ? TestPrefix : $"{TestPrefix}_{suffix}";

    /// <summary>
    /// Gets a random date within a range.
    /// </summary>
    protected static DateTime RandomDate(DateTime? min = null, DateTime? max = null)
    {
        var start = min ?? DateTime.UtcNow.AddYears(-1);
        var end = max ?? DateTime.UtcNow;
        var range = (end - start).Days;
        return start.AddDays(Random.Shared.Next(range));
    }

    /// <summary>
    /// Gets a random decimal amount within a range.
    /// </summary>
    protected static decimal RandomAmount(decimal min = 1m, decimal max = 1000m)
    {
        var range = (double)(max - min);
        return min + (decimal)(Random.Shared.NextDouble() * range);
    }

    /// <summary>
    /// Gets a random item from a list.
    /// </summary>
    protected static T RandomFrom<T>(params T[] items) =>
        items[Random.Shared.Next(items.Length)];
}
