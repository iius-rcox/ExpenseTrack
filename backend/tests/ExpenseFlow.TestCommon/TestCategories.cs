namespace ExpenseFlow.TestCommon;

/// <summary>
/// Standard test category constants for consistent trait usage across test projects.
/// Use these with [Trait("Category", TestCategories.Unit)] attribute.
/// </summary>
public static class TestCategories
{
    /// <summary>
    /// Unit tests - fast, isolated, no external dependencies.
    /// Target: < 1 second per test.
    /// </summary>
    public const string Unit = "Unit";

    /// <summary>
    /// Contract tests - validate API conformance to OpenAPI spec.
    /// Target: < 5 seconds per test.
    /// </summary>
    public const string Contract = "Contract";

    /// <summary>
    /// Integration tests - test component interactions with real dependencies.
    /// May require database or external services.
    /// Target: < 30 seconds per test.
    /// </summary>
    public const string Integration = "Integration";

    /// <summary>
    /// Scenario tests - end-to-end workflow tests with full infrastructure.
    /// Requires Docker for Testcontainers.
    /// Target: < 60 seconds per test.
    /// </summary>
    public const string Scenario = "Scenario";

    /// <summary>
    /// Property-based tests - FsCheck tests verifying invariants.
    /// May run many iterations.
    /// Target: < 60 seconds per test suite.
    /// </summary>
    public const string Property = "Property";

    /// <summary>
    /// Chaos tests - verify system behavior under failure conditions.
    /// Run in nightly builds only.
    /// </summary>
    public const string Chaos = "Chaos";

    /// <summary>
    /// Resilience tests - verify recovery from failures.
    /// Run in nightly builds only.
    /// </summary>
    public const string Resilience = "Resilience";

    /// <summary>
    /// Load tests - performance and scalability testing.
    /// Run in nightly builds only.
    /// </summary>
    public const string Load = "Load";

    /// <summary>
    /// Quarantined tests - flaky tests temporarily removed from CI.
    /// These tests are tracked for remediation.
    /// </summary>
    public const string Quarantined = "Quarantined";
}

/// <summary>
/// Test priority levels for execution ordering.
/// </summary>
public static class TestPriority
{
    /// <summary>
    /// Critical path tests - must pass for any deployment.
    /// </summary>
    public const string Critical = "Critical";

    /// <summary>
    /// High priority tests - core functionality.
    /// </summary>
    public const string High = "High";

    /// <summary>
    /// Medium priority tests - important but not blocking.
    /// </summary>
    public const string Medium = "Medium";

    /// <summary>
    /// Low priority tests - edge cases and nice-to-haves.
    /// </summary>
    public const string Low = "Low";
}
