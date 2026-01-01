# ExpenseFlow Testing Architecture

## Overview

ExpenseFlow implements a **three-layer testing strategy** designed for fast developer feedback, thorough integration validation, and continuous resilience verification.

```
┌─────────────────────────────────────────────────────────────────┐
│                        CI Pipeline                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐       │
│  │  ci-quick    │    │  ci-full     │    │  ci-nightly  │       │
│  │  (<3 min)    │    │  (<15 min)   │    │  (30-60 min) │       │
│  └──────┬───────┘    └──────┬───────┘    └──────┬───────┘       │
│         │                   │                   │                │
│  ┌──────▼───────┐    ┌──────▼───────┐    ┌──────▼───────┐       │
│  │ Unit Tests   │    │ Scenario     │    │ Property     │       │
│  │ Contract     │    │ Integration  │    │ Chaos        │       │
│  │ Tests        │    │ E2E          │    │ Load         │       │
│  └──────────────┘    └──────────────┘    └──────────────┘       │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## Test Projects

### 1. ExpenseFlow.Contracts.Tests

**Purpose**: Validate API contracts against OpenAPI specification

**Location**: `backend/tests/ExpenseFlow.Contracts.Tests/`

**Key Components**:
- `ContractTestBase.cs` - Base class for OpenAPI validation
- `*EndpointContractTests.cs` - Per-controller contract validation

**Category**: `Contract`

**When Run**: Every commit (ci-quick.yml)

### 2. ExpenseFlow.PropertyTests

**Purpose**: Verify domain invariants through property-based testing

**Location**: `backend/tests/ExpenseFlow.PropertyTests/`

**Key Components**:
- `Generators/DomainGenerators.cs` - FsCheck custom generators
- `*PropertyTests.cs` - Property-based test classes

**Category**: `Property`

**When Run**: Every PR (ci-full.yml), Nightly (ci-nightly.yml with 10,000 iterations)

### 3. ExpenseFlow.Scenarios.Tests

**Purpose**: End-to-end workflow validation with real infrastructure

**Location**: `backend/tests/ExpenseFlow.Scenarios.Tests/`

**Key Components**:
- `Infrastructure/PostgresContainerFixture.cs` - Testcontainers for PostgreSQL
- `Infrastructure/WireMockFixture.cs` - HTTP mock server
- `Chaos/ChaosConfiguration.cs` - Chaos engineering configuration
- `Chaos/ChaosStrategies.cs` - Polly fault injection strategies
- `Mocks/*.json` - WireMock stub definitions

**Categories**: `Scenario`, `Integration`, `Chaos`, `Resilience`

**When Run**: PRs (ci-full.yml), Nightly chaos/resilience (ci-nightly.yml)

### 4. ExpenseFlow.TestCommon

**Purpose**: Shared test utilities and fixtures

**Location**: `backend/tests/ExpenseFlow.TestCommon/`

**Key Components**:
- `TestCategories.cs` - Category constants for test filtering
- `Fixtures/ITestFixture.cs` - Test fixture interface
- `Builders/TestDataBuilder.cs` - Fluent builder base class

## Test Categories

| Category | Description | CI Workflow |
|----------|-------------|-------------|
| `Unit` | Isolated unit tests | ci-quick |
| `Contract` | API contract validation | ci-quick |
| `Integration` | Component integration | ci-full |
| `Scenario` | End-to-end workflows | ci-full |
| `Property` | Property-based tests | ci-full, ci-nightly |
| `Chaos` | Fault injection tests | ci-nightly |
| `Resilience` | Recovery tests | ci-nightly |
| `Load` | Performance tests | ci-nightly |
| `Quarantined` | Flaky tests under investigation | excluded |

### Running Tests by Category

```bash
# Run only unit tests
dotnet test --filter "Category=Unit"

# Run contract tests
dotnet test --filter "Category=Contract"

# Run all fast tests (unit + contract)
dotnet test --filter "Category=Unit|Category=Contract"

# Run scenario tests (requires Docker)
dotnet test --filter "Category=Scenario"

# Run chaos tests
CHAOS_ENABLED=true dotnet test --filter "Category=Chaos"
```

## Infrastructure Dependencies

### Testcontainers

The scenario testing project uses [Testcontainers](https://dotnet.testcontainers.org/) for isolated, reproducible infrastructure:

```csharp
// PostgreSQL container
var container = new PostgreSqlBuilder()
    .WithImage("postgres:15-alpine")
    .WithDatabase("expenseflow_test")
    .Build();

await container.StartAsync();
var connectionString = container.GetConnectionString();
```

**Requirements**:
- Docker Desktop or compatible runtime
- Testcontainers.PostgreSql NuGet package

### WireMock

External service mocking using [WireMock.Net](https://github.com/WireMock-Net/WireMock.Net):

```csharp
var server = WireMockServer.Start();

// Setup mock response
server.Given(Request.Create().WithPath("/formrecognizer/*"))
      .RespondWith(Response.Create()
          .WithStatusCode(200)
          .WithBodyFromFile("Mocks/azure-ai-stubs.json"));
```

**Mock Stubs Location**: `backend/tests/ExpenseFlow.Scenarios.Tests/Mocks/`

- `azure-ai-stubs.json` - Azure Document Intelligence (OCR)
- `openai-stubs.json` - OpenAI API (categorization)
- `vista-erp-stubs.json` - Vista ERP (export/sync)

## CI/CD Integration

### GitHub Actions Workflows

| Workflow | Trigger | Tests Run | Target Duration |
|----------|---------|-----------|-----------------|
| `ci-quick.yml` | Push to any branch | Unit, Contract | <3 minutes |
| `ci-full.yml` | PR to main | All except Chaos | <15 minutes |
| `ci-nightly.yml` | Daily at 2 AM UTC | Chaos, Property (extended), Load | 30-60 minutes |

### Coverage Enforcement

Code coverage is enforced via Codecov:

- **Project target**: 80% overall
- **Patch target**: 80% for new/modified code
- **Configuration**: `.github/codecov.yml`

### Branch Protection

Main branch requires:
- All CI workflows to pass
- 80% coverage threshold met
- At least one approval review

## Property-Based Testing

### FsCheck Integration

Custom generators for domain types in `DomainGenerators.cs`:

```csharp
// Generates valid monetary amounts
public static Arbitrary<decimal> ValidAmount() =>
    Arb.Default.Decimal()
        .Filter(d => d > 0 && d < 1_000_000)
        .Generator
        .Select(d => Math.Round(d, 2))
        .ToArbitrary();

// Generates normalized embedding vectors
public static Arbitrary<float[]> ValidEmbedding() =>
    Gen.ArrayOf(1536, Arb.Default.Float().Generator)
        .Select(NormalizeVector)
        .ToArbitrary();
```

### Property Test Patterns

1. **Symmetry**: `match(A, B) == match(B, A)`
2. **Idempotency**: `categorize(categorize(x)) == categorize(x)`
3. **Bounds**: `0.0 <= confidence <= 1.0`
4. **Conservation**: `sum(allocations) == total`

## Chaos Engineering

### Configuration

Chaos behavior is controlled via environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `CHAOS_ENABLED` | Master switch for chaos injection | `false` |
| `CHAOS_INJECTION_RATE` | Probability of fault injection (0.0-1.0) | `0.05` |
| `CHAOS_MAX_LATENCY_MS` | Maximum latency to inject | `5000` |

### Chaos Strategies

Implemented using Polly v8 ResiliencePipeline:

1. **HTTP Fault**: Simulates network failures
2. **Latency**: Injects response delays
3. **Database**: Simulates connection timeouts
4. **API Chaos**: Returns 429/503 responses

See: `docs/testing/chaos-runbook.md` for operational procedures.

## Local Development

### Quick Start

```bash
# Run all tests locally
./scripts/test-all.ps1

# Run fast tests only (unit + contract)
./scripts/test-quick.ps1

# Start test infrastructure
docker-compose -f docker-compose.test.yml up -d

# Run specific test project
dotnet test backend/tests/ExpenseFlow.Contracts.Tests
```

### Validation

```bash
# Validate test category configuration
./scripts/validate-test-categories.ps1
```

## Best Practices

### Writing New Tests

1. **Choose the right category**: Use appropriate `[Trait("Category", "...")]`
2. **Follow naming conventions**: `{Method}_When{Condition}_Should{Result}`
3. **Use fixtures for setup**: Extend from appropriate base classes
4. **Mock external services**: Use WireMock stubs for external APIs
5. **Keep tests fast**: Unit tests should complete in milliseconds

### Test Data Management

1. **Use builders**: `new ReceiptBuilder().WithAmount(100).Build()`
2. **Avoid shared state**: Each test should be isolated
3. **Clean up resources**: Use `IAsyncLifetime` for container cleanup
4. **Seed carefully**: Only seed what the test requires

### Debugging Failures

1. Check GitHub Actions logs for full error output
2. Run locally with Docker to reproduce containerized failures
3. Use `--logger "console;verbosity=detailed"` for verbose output
4. Check quarantine list for known flaky tests
