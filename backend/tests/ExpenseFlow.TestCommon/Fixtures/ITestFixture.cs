namespace ExpenseFlow.TestCommon.Fixtures;

/// <summary>
/// Interface for test fixtures that provide pre-configured test data.
/// Fixtures encapsulate the setup and teardown of complex test scenarios.
/// </summary>
public interface ITestFixture : IAsyncDisposable
{
    /// <summary>
    /// Unique identifier for tracking fixture data in the database.
    /// Used for cleanup after tests complete.
    /// </summary>
    string FixtureId { get; }

    /// <summary>
    /// Initializes the fixture, creating any required test data.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Cleans up fixture data from the database.
    /// Called automatically when fixture is disposed.
    /// </summary>
    Task CleanupAsync();
}

/// <summary>
/// Base class for test fixtures providing common functionality.
/// </summary>
public abstract class TestFixtureBase : ITestFixture
{
    /// <summary>
    /// Unique identifier for this fixture instance.
    /// Prefixed with "test_" to easily identify test data.
    /// </summary>
    public string FixtureId { get; } = $"test_{Guid.NewGuid():N}";

    private bool _isInitialized;
    private bool _isDisposed;

    /// <summary>
    /// Initializes the fixture. Safe to call multiple times.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        await SetupAsync();
        _isInitialized = true;
    }

    /// <summary>
    /// Override to provide fixture-specific setup logic.
    /// </summary>
    protected abstract Task SetupAsync();

    /// <summary>
    /// Cleans up all fixture data.
    /// </summary>
    public async Task CleanupAsync()
    {
        if (_isDisposed)
            return;

        await TeardownAsync();
        _isDisposed = true;
    }

    /// <summary>
    /// Override to provide fixture-specific cleanup logic.
    /// </summary>
    protected abstract Task TeardownAsync();

    /// <summary>
    /// Disposes the fixture, ensuring cleanup is called.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await CleanupAsync();
        GC.SuppressFinalize(this);
    }
}
