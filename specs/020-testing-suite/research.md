# Research: Comprehensive Testing Suite

**Branch**: `020-testing-suite` | **Date**: 2025-12-31
**Phase**: 0 - Research

## Overview

This document captures research findings for implementing a comprehensive testing suite with three testing strategies: Contract-Driven, Scenario-Based Integration, and Property-Based + Chaos Testing.

## 1. Contract Testing Tools

### Decision: OpenAPI.NET + Swashbuckle

**Evaluated Options**:
| Tool | Pros | Cons | Decision |
|------|------|------|----------|
| [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore) | Already in project, auto-generates OpenAPI spec | Requires runtime to generate spec | **Selected** (existing) |
| NSwag | Code-first and spec-first support | Additional complexity, learning curve | Pass |
| OpenAPI.NET | Microsoft-supported, parses/validates OpenAPI specs | Lower-level API | **Selected** (validation) |

**Implementation Approach**:
1. Use Swashbuckle to generate `openapi.json` at build time
2. Use OpenAPI.NET (`Microsoft.OpenApi.Readers`) to parse the generated spec
3. Create contract tests that verify actual API responses match spec definitions
4. Validate request/response schemas, status codes, content types

**Key Package**: `Microsoft.OpenApi.Readers` (parse and validate OpenAPI documents)

### Contract Test Pattern

```csharp
[Fact]
public async Task GetReceipts_ReturnsResponseMatchingOpenApiSpec()
{
    // Load OpenAPI spec
    var spec = await OpenApiDocument.LoadAsync("openapi.json");
    var operation = spec.Paths["/api/receipts"].Operations[OperationType.Get];

    // Make actual API call
    var response = await _client.GetAsync("/api/receipts");

    // Validate response matches spec
    response.Should().MatchOpenApiOperation(operation);
}
```

---

## 2. WireMock Configuration

### Decision: WireMock.Net with Static Mappings

[WireMock.Net](https://github.com/WireMock-Net/WireMock.Net) is the recommended approach for mocking Azure services.

**Services to Mock**:
| Service | Mock Approach | Key Endpoints |
|---------|---------------|---------------|
| Azure Document Intelligence | WireMock with recorded responses | `POST /documentintelligence/documentModels/{model}:analyze` |
| Azure OpenAI | [MockGPT](https://mockgpt.wiremock.io/) or custom stubs | `POST /openai/deployments/{deployment}/embeddings`, `POST /completions` |
| Viewpoint Vista ERP | SQL result stubs via repository mocks | N/A (use in-memory mocks) |

**Implementation Pattern**:

```csharp
public class WireMockFixture : IAsyncLifetime
{
    private WireMockServer _server;

    public string BaseUrl => _server.Url;

    public async Task InitializeAsync()
    {
        _server = WireMockServer.Start();

        // Load static mapping files
        _server.ReadStaticMappingAndAddOrUpdate("./Mocks/azure-document-intelligence.json");
        _server.ReadStaticMappingAndAddOrUpdate("./Mocks/azure-openai-embeddings.json");
    }

    public Task DisposeAsync()
    {
        _server.Stop();
        return Task.CompletedTask;
    }
}
```

**Mapping File Example** (`azure-document-intelligence.json`):
```json
{
  "Request": {
    "Path": { "Matchers": [{ "Name": "WildcardMatcher", "Pattern": "/documentintelligence/*" }] },
    "Methods": ["POST"]
  },
  "Response": {
    "StatusCode": 202,
    "Headers": { "Operation-Location": "{{request.url}}/analyzeResults/{{guid}}" }
  }
}
```

### MockGPT for OpenAI

[MockGPT](https://mockgpt.wiremock.io/) provides pre-built stubs for OpenAI APIs:
- Set base URL to `https://mockgpt.wiremockapi.cloud/v1`
- Supports chat completions, embeddings, and function calling
- Free tier available for development/testing

---

## 3. FsCheck Property-Based Testing

### Decision: FsCheck 3.x with Custom Generators

[FsCheck](https://fscheck.github.io/FsCheck/) version 3.x provides excellent C# support with improved APIs.

**Package**: `FsCheck.Xunit` (xUnit integration)

**Domain-Specific Generators Needed**:

| Generator | Domain Rules | Purpose |
|-----------|--------------|---------|
| `TransactionGenerator` | Amount > 0, Valid date range | Test matching engine invariants |
| `ReceiptGenerator` | Valid vendor, amount, date | Test OCR extraction invariants |
| `GLCodeGenerator` | Format: XXXX (4-digit codes) | Test categorization invariants |
| `ExpenseEmbeddingGenerator` | 1536-dimension vectors, normalized | Test similarity search |

**Example Custom Generator**:

```csharp
public static class ExpenseGenerators
{
    public static Arbitrary<Transaction> TransactionArb() =>
        (from amount in Gen.Choose(1, 1000000).Select(x => x / 100m)
         from date in Gen.Choose(0, 365).Select(d => DateTime.Today.AddDays(-d))
         from vendor in Gen.Elements("Amazon", "Walmart", "Home Depot", "Costco")
         select new Transaction
         {
             Amount = amount,
             TransactionDate = date,
             Description = vendor
         }).ToArbitrary();
}

// Usage in property test
[Property]
public Property MatchingEngine_AlwaysMatchesExactAmountAndDate()
{
    return Prop.ForAll(
        ExpenseGenerators.TransactionArb(),
        ExpenseGenerators.ReceiptArb(),
        (txn, receipt) =>
        {
            // Property: If amount and date match exactly, confidence should be high
            if (txn.Amount == receipt.Total && txn.TransactionDate == receipt.TransactionDate)
            {
                var score = _matchingEngine.CalculateConfidence(txn, receipt);
                return score >= 0.9f;
            }
            return true;
        });
}
```

### Properties to Test

| Domain | Property | Invariant |
|--------|----------|-----------|
| Matching Engine | Symmetric matching | `match(a,b) == match(b,a)` |
| Matching Engine | Amount tolerance | `abs(txn.amount - receipt.amount) < 1.00` → eligible for match |
| Categorization | Tier ordering | Tier 1 checked before Tier 2, Tier 2 before Tier 3 |
| Embeddings | Similarity bounds | `0.0 <= similarity <= 1.0` |
| Reports | Totals accuracy | `sum(line_items) == report.total` |

---

## 4. Polly Chaos Engineering

### Decision: Polly v8 Built-in Chaos Strategies

As of [Polly v8.3.0](https://www.pollydocs.org/chaos/), chaos engineering is built into the core library (formerly Simmy).

**Chaos Strategies Available**:
| Strategy | Purpose | Use Case |
|----------|---------|----------|
| `AddChaosFault` | Inject exceptions | Test retry logic, circuit breakers |
| `AddChaosLatency` | Add artificial delays | Test timeout handling |
| `AddChaosOutcome` | Return fake results | Test fallback behavior |
| `AddChaosBehavior` | Custom behavior injection | Test logging, metrics |

**Implementation Pattern**:

```csharp
var chaosOptions = new ChaosFaultStrategyOptions
{
    InjectionRate = 0.05, // 5% of calls
    EnabledGenerator = static args =>
    {
        // Only inject in test environment
        return new ValueTask<bool>(
            Environment.GetEnvironmentVariable("CHAOS_ENABLED") == "true");
    },
    FaultGenerator = static args =>
    {
        return new ValueTask<Exception?>(
            new HttpRequestException("Simulated service failure"));
    }
};

services.AddResiliencePipeline("document-intelligence", builder =>
{
    builder
        .AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 3 })
        .AddCircuitBreaker()
        .AddChaosFault(chaosOptions); // Place last
});
```

### Chaos Test Scenarios

| Scenario | Chaos Type | Expected Behavior |
|----------|------------|-------------------|
| OCR service unavailable | Fault (HttpRequestException) | Receipt marked as `Error`, job can retry |
| OpenAI rate limited | Fault (429 response) | Fallback to Tier 1 categorization |
| Database connection timeout | Latency (30s) | Circuit breaker opens, cached data served |
| Vista ERP unreachable | Fault (SqlException) | Stale reference data cache used, ops alerted |

---

## 5. Testcontainers Configuration

### Decision: Container per Test Collection with xUnit Collection Fixtures

[Testcontainers.PostgreSql](https://dotnet.testcontainers.org/modules/postgres/) version 4.9.0 (latest) will be used.

**Isolation Strategy**:
| Test Type | Container Strategy | Reason |
|-----------|-------------------|--------|
| Unit tests | No container (in-memory) | Speed |
| Integration tests | Collection fixture (shared) | Balance speed + isolation |
| Scenario tests | Class fixture (per test class) | Full isolation for E2E |
| Parallel test classes | Separate containers | Prevent data interference |

**Collection Fixture Pattern**:

```csharp
public class PostgreSqlFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("expenseflow_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await ApplyMigrationsAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private async Task ApplyMigrationsAsync()
    {
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var context = new ExpenseFlowDbContext(options);
        await context.Database.MigrateAsync();
    }
}

[CollectionDefinition("PostgreSQL")]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture> { }

[Collection("PostgreSQL")]
public class ReceiptRepositoryTests
{
    private readonly PostgreSqlFixture _fixture;

    public ReceiptRepositoryTests(PostgreSqlFixture fixture) => _fixture = fixture;
}
```

### Data Isolation Between Tests

Each test should:
1. Use unique identifiers (GUIDs) for test data
2. Clean up in `Dispose()` or use transaction rollback
3. Never share mutable state across tests

```csharp
public class ScenarioTestBase : IAsyncLifetime
{
    protected readonly Guid TestRunId = Guid.NewGuid();
    protected ExpenseFlowDbContext DbContext;

    public async Task InitializeAsync()
    {
        // Seed test-specific data with TestRunId as prefix
        await SeedTestDataAsync();
    }

    public async Task DisposeAsync()
    {
        // Clean up only data created by this test run
        await DbContext.Transactions
            .Where(t => t.Description.StartsWith(TestRunId.ToString()))
            .ExecuteDeleteAsync();
    }
}
```

---

## 6. GitHub Actions Optimization

### Decision: Caching + Parallel Jobs + Concurrency Control

**Caching Strategy** (per [GitHub Docs](https://docs.github.com/en/actions/tutorials/build-and-test-code/net)):

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: 8.0.x
    cache: true  # Built-in NuGet caching

- name: Restore dependencies
  run: dotnet restore --locked-mode
```

**Requirements for built-in caching**:
1. Add `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>` to Directory.Build.props
2. Commit `packages.lock.json` files
3. Use `--locked-mode` in restore commands

**Workflow Structure**:

```yaml
name: CI Full (PR)

on:
  pull_request:
    branches: [main]

concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true  # FR-009: Cancel redundant runs

jobs:
  backend-tests:
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:15-alpine
        env:
          POSTGRES_DB: expenseflow_test
          POSTGRES_PASSWORD: test
        ports: ['5432:5432']
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
          cache: true
      - run: dotnet test --collect:"XPlat Code Coverage"
      - uses: codecov/codecov-action@v4

  frontend-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json
      - run: npm ci
      - run: npm run test:coverage
```

**Optimization Techniques**:
| Technique | Impact | Implementation |
|-----------|--------|----------------|
| NuGet caching | ~30s savings | `setup-dotnet` with `cache: true` |
| npm caching | ~20s savings | `setup-node` with `cache: 'npm'` |
| Parallel jobs | ~50% time reduction | Separate backend/frontend jobs |
| Concurrency groups | Avoid waste | Cancel in-progress on new push |
| Docker layer caching | ~60s savings | GitHub Actions cache for Docker |

---

## 7. Coverage Reporting

### Decision: Codecov with PR Comments

**Configuration** (`.codecov.yml`):

```yaml
codecov:
  require_ci_to_pass: true

coverage:
  precision: 2
  round: down
  range: "70...100"
  status:
    project:
      default:
        target: 80%  # FR-015: 80% threshold
        threshold: 1%
    patch:
      default:
        target: 80%
        threshold: 1%

comment:
  layout: "reach,diff,flags,files"
  behavior: default
  require_changes: true
```

**Coverage Collection**:
- Backend: `dotnet test --collect:"XPlat Code Coverage"` (produces Cobertura XML)
- Frontend: `vitest run --coverage` (uses `@vitest/coverage-v8`)

---

## 8. Test Data Factories (Fixtures)

### Decision: Builder Pattern with Seeded Defaults

Per FR-018, test data factories must be programmatic and self-cleaning.

**Pattern**:

```csharp
public class TransactionBuilder
{
    private Guid _id = Guid.NewGuid();
    private decimal _amount = 100.00m;
    private DateTime _date = DateTime.Today;
    private string _description = "Test Transaction";
    private Guid _userId = TestConstants.DefaultUserId;

    public TransactionBuilder WithAmount(decimal amount) { _amount = amount; return this; }
    public TransactionBuilder WithDate(DateTime date) { _date = date; return this; }
    public TransactionBuilder WithDescription(string desc) { _description = desc; return this; }
    public TransactionBuilder ForUser(Guid userId) { _userId = userId; return this; }

    public Transaction Build() => new()
    {
        Id = _id,
        Amount = _amount,
        TransactionDate = _date,
        Description = _description,
        UserId = _userId
    };

    public async Task<Transaction> CreateAsync(ExpenseFlowDbContext db)
    {
        var transaction = Build();
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();
        return transaction;
    }
}
```

**Usage**:

```csharp
// Arrange
var transaction = await new TransactionBuilder()
    .WithAmount(150.00m)
    .WithDescription("Amazon.com")
    .CreateAsync(_dbContext);

var receipt = await new ReceiptBuilder()
    .WithTotal(150.00m)
    .WithVendor("AMAZON")
    .CreateAsync(_dbContext);

// Act
var result = await _matchingService.FindMatchesAsync(receipt);

// Assert
result.Should().Contain(m => m.TransactionId == transaction.Id);
```

---

## 9. Flaky Test Handling

### Decision: xUnit Retry + Custom Quarantine Attribute

Per FR-017, tests that fail inconsistently are quarantined.

**Implementation**:

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class RetryFactAttribute : FactAttribute
{
    public int MaxRetries { get; set; } = 2;
}

public class RetryTestCase : XunitTestCase
{
    protected override async Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink,
        IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator,
        CancellationToken cancellationToken)
    {
        var maxRetries = GetMaxRetries();
        RunSummary summary = null;

        for (int attempt = 1; attempt <= maxRetries + 1; attempt++)
        {
            summary = await base.RunAsync(...);

            if (summary.Failed == 0) break;

            if (attempt <= maxRetries)
            {
                diagnosticMessageSink.OnMessage(new DiagnosticMessage(
                    $"Retry {attempt}/{maxRetries} for {DisplayName}"));
            }
        }

        return summary;
    }
}
```

**Quarantine Workflow**:
1. Test fails initially
2. Auto-retry up to 2 times
3. If still failing inconsistently, add `[Quarantined("reason")]` attribute
4. Quarantined tests run but don't fail the build (log warning)
5. Track in `QuarantinedTest` table for visibility

---

## Research Sources

- [FsCheck Documentation](https://fscheck.github.io/FsCheck/)
- [FsCheck Test Data Generators](https://fscheck.github.io/FsCheck/TestData.html)
- [WireMock.Net Wiki](https://github.com/WireMock-Net/WireMock.Net/wiki)
- [MockGPT for OpenAI](https://mockgpt.wiremock.io/)
- [Polly Chaos Engineering](https://www.pollydocs.org/chaos/)
- [.NET Blog - Resilience and Chaos Engineering](https://devblogs.microsoft.com/dotnet/resilience-and-chaos-engineering/)
- [Testcontainers PostgreSQL Module](https://dotnet.testcontainers.org/modules/postgres/)
- [Testcontainers Best Practices](https://www.milanjovanovic.tech/blog/testcontainers-best-practices-dotnet-integration-testing)
- [GitHub Actions .NET Caching](https://docs.github.com/en/actions/tutorials/build-and-test-code/net)
- [NuGet Caching in GitHub Actions](https://www.damirscorner.com/blog/posts/20240726-CachingNuGetPackagesInGitHubActions.html)

---

## Package Versions (Recommended)

| Package | Version | Purpose |
|---------|---------|---------|
| FsCheck | 3.2.0 | Property-based testing |
| FsCheck.Xunit | 3.2.0 | xUnit integration |
| WireMock.Net | 1.6.x | HTTP mocking |
| Testcontainers.PostgreSql | 4.9.0 | Container isolation |
| Microsoft.OpenApi.Readers | 1.6.x | OpenAPI parsing |
| Polly | 8.5.x | Resilience + chaos |
| coverlet.collector | 6.0.x | Code coverage |

---

## Next Steps

1. **Phase 1 Design**: Create data-model.md defining test entities
2. **Contracts**: Specify GitHub Actions workflow YAML contracts
3. **Quickstart**: Document local testing setup matching CI
