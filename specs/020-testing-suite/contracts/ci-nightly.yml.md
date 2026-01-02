# Contract: CI Nightly Workflow

**Workflow File**: `.github/workflows/ci-nightly.yml`
**Purpose**: Chaos and resilience testing (FR-003)

## Trigger Conditions

| Trigger | Condition | Action |
|---------|-----------|--------|
| Schedule | Daily at 2:00 AM UTC | Run chaos + resilience tests |
| Manual | `workflow_dispatch` | On-demand chaos testing |

## Job Matrix

```
┌─────────────────────────────────────────────────────────────────┐
│                     ci-nightly.yml                              │
├────────────────┬────────────────┬────────────────┬─────────────┤
│ chaos-tests    │ resilience-    │ property-      │ load-tests  │
│                │ tests          │ exhaustive     │             │
│ (10 min)       │ (10 min)       │ (15 min)       │ (20 min)    │
└────────────────┴────────────────┴────────────────┴─────────────┘
                              │
                              ▼
                    ┌─────────────────┐
                    │ report          │
                    │ (summary +      │
                    │  notifications) │
                    └─────────────────┘
```

## Jobs Specification

### Job: `chaos-tests`

**Runner**: `ubuntu-latest`
**Services**: PostgreSQL, WireMock
**Timeout**: 15 minutes

Tests system behavior under simulated failures using Polly chaos strategies.

#### Chaos Scenarios

| Scenario | Chaos Type | Target Service | Expected Behavior |
|----------|------------|----------------|-------------------|
| OCR Failure | Fault injection | Document Intelligence | Receipt → Error status, retry available |
| AI Rate Limit | 429 response | OpenAI | Fallback to Tier 1 categorization |
| DB Timeout | Latency (30s) | PostgreSQL | Circuit breaker opens |
| Vista Unreachable | Connection fail | Vista ERP | Stale cache served, ops alerted |
| Blob Storage Down | 503 response | Azure Blob | Placeholder image, queue for retry |

#### Implementation

```yaml
- name: Run Chaos Tests
  env:
    CHAOS_ENABLED: true
    CHAOS_INJECTION_RATE: 0.25  # 25% for nightly
  run: |
    dotnet test backend/tests/ExpenseFlow.Scenarios.Tests \
      --filter "Category=Chaos" \
      --logger "trx;LogFileName=chaos-results.trx"
```

### Job: `resilience-tests`

**Runner**: `ubuntu-latest`
**Services**: PostgreSQL, WireMock
**Timeout**: 15 minutes

Tests system recovery from various failure conditions.

#### Resilience Scenarios

| Scenario | Trigger | Recovery Expectation | Timeout |
|----------|---------|---------------------|---------|
| DB Connection Recovery | Kill PostgreSQL connection | Reconnect within 30s | 60s |
| Circuit Breaker Reset | After failures subside | Auto-reset after 30s | 90s |
| Retry Exhaustion | Persistent failure | Graceful degradation | 45s |
| Fallback Cascade | Tier 3 → Tier 2 → Tier 1 | Complete within 5s | 10s |

### Job: `property-exhaustive`

**Runner**: `ubuntu-latest`
**Timeout**: 20 minutes

Extended property-based testing with higher iteration counts.

```yaml
- name: Run Exhaustive Property Tests
  env:
    FSCHECK_MAX_TEST: 10000  # 100x normal
    FSCHECK_END_SIZE: 500    # Larger inputs
  run: |
    dotnet test backend/tests/ExpenseFlow.PropertyTests \
      --configuration Release \
      --logger "trx;LogFileName=property-results.trx"
```

#### Property Invariants Tested

| Domain | Property | Iterations |
|--------|----------|------------|
| Matching Engine | Symmetry | 10,000 |
| Matching Engine | Transitivity | 10,000 |
| Categorization | Tier ordering | 10,000 |
| Embeddings | Similarity bounds | 10,000 |
| Reports | Total accuracy | 10,000 |
| Date handling | Timezone invariance | 10,000 |

### Job: `load-tests`

**Runner**: `ubuntu-latest`
**Services**: PostgreSQL (with real schema)
**Timeout**: 25 minutes

NBomber load tests to verify performance under stress.

#### Load Scenarios

| Scenario | Load Profile | Duration | SLA |
|----------|--------------|----------|-----|
| Concurrent Users | 20 users, ramp 2 min | 5 min | P95 < 2s |
| Batch Receipts | 50 receipts | 5 min | All processed |
| Report Generation | 10 reports/min | 3 min | P95 < 10s |
| API Throughput | 100 req/s | 5 min | Error rate < 1% |

```yaml
- name: Run Load Tests
  run: |
    dotnet test backend/tests/ExpenseFlow.LoadTests \
      --configuration Release \
      --logger "trx;LogFileName=load-results.trx"
```

### Job: `report`

**Runner**: `ubuntu-latest`
**Needs**: All previous jobs
**Purpose**: Generate summary report and notify on failures

```yaml
- name: Generate Summary Report
  run: |
    echo "## Nightly Test Summary" >> $GITHUB_STEP_SUMMARY
    echo "" >> $GITHUB_STEP_SUMMARY
    echo "| Job | Status |" >> $GITHUB_STEP_SUMMARY
    echo "|-----|--------|" >> $GITHUB_STEP_SUMMARY
    echo "| Chaos Tests | ${{ needs.chaos-tests.result }} |" >> $GITHUB_STEP_SUMMARY
    echo "| Resilience Tests | ${{ needs.resilience-tests.result }} |" >> $GITHUB_STEP_SUMMARY
    echo "| Property Tests | ${{ needs.property-exhaustive.result }} |" >> $GITHUB_STEP_SUMMARY
    echo "| Load Tests | ${{ needs.load-tests.result }} |" >> $GITHUB_STEP_SUMMARY
```

## YAML Template

```yaml
name: CI Nightly (Chaos & Resilience)

on:
  schedule:
    - cron: '0 2 * * *'  # Daily at 2:00 AM UTC
  workflow_dispatch:
    inputs:
      chaos_rate:
        description: 'Chaos injection rate (0.0 - 1.0)'
        required: false
        default: '0.25'
      skip_load_tests:
        description: 'Skip load tests'
        required: false
        type: boolean
        default: false

env:
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  chaos-tests:
    runs-on: ubuntu-latest
    timeout-minutes: 15

    services:
      postgres:
        image: postgres:15-alpine
        env:
          POSTGRES_DB: expenseflow_test
          POSTGRES_USER: test
          POSTGRES_PASSWORD: test
        ports:
          - 5432:5432
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5

      wiremock:
        image: wiremock/wiremock:3.3.1
        ports:
          - 8080:8080

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
          cache: true

      - name: Restore & Build
        run: |
          dotnet restore backend/ExpenseFlow.sln --locked-mode
          dotnet build backend/ExpenseFlow.sln -c Release --no-restore

      - name: Load WireMock Stubs
        run: |
          curl -X POST http://localhost:8080/__admin/mappings \
            -H "Content-Type: application/json" \
            -d @backend/tests/ExpenseFlow.Scenarios.Tests/Mocks/azure-ai-stubs.json

      - name: Run Chaos Tests
        env:
          CHAOS_ENABLED: true
          CHAOS_INJECTION_RATE: ${{ github.event.inputs.chaos_rate || '0.25' }}
          ConnectionStrings__DefaultConnection: "Host=localhost;Database=expenseflow_test;Username=test;Password=test"
          Services__DocumentIntelligence__Endpoint: "http://localhost:8080"
          Services__OpenAI__Endpoint: "http://localhost:8080"
        run: |
          dotnet test backend/tests/ExpenseFlow.Scenarios.Tests \
            --no-build -c Release \
            --filter "Category=Chaos" \
            --logger "trx;LogFileName=chaos-results.trx" \
            --results-directory ./test-results

      - name: Upload Results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: chaos-test-results
          path: ./test-results/
          retention-days: 7

  resilience-tests:
    runs-on: ubuntu-latest
    timeout-minutes: 15

    services:
      postgres:
        image: postgres:15-alpine
        env:
          POSTGRES_DB: expenseflow_test
          POSTGRES_USER: test
          POSTGRES_PASSWORD: test
        ports:
          - 5432:5432

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
          cache: true

      - name: Restore & Build
        run: |
          dotnet restore backend/ExpenseFlow.sln --locked-mode
          dotnet build backend/ExpenseFlow.sln -c Release --no-restore

      - name: Run Resilience Tests
        env:
          ConnectionStrings__DefaultConnection: "Host=localhost;Database=expenseflow_test;Username=test;Password=test"
        run: |
          dotnet test backend/tests/ExpenseFlow.Scenarios.Tests \
            --no-build -c Release \
            --filter "Category=Resilience" \
            --logger "trx;LogFileName=resilience-results.trx" \
            --results-directory ./test-results

      - name: Upload Results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: resilience-test-results
          path: ./test-results/
          retention-days: 7

  property-exhaustive:
    runs-on: ubuntu-latest
    timeout-minutes: 20

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
          cache: true

      - name: Restore & Build
        run: |
          dotnet restore backend/ExpenseFlow.sln --locked-mode
          dotnet build backend/ExpenseFlow.sln -c Release --no-restore

      - name: Run Exhaustive Property Tests
        env:
          FSCHECK_MAX_TEST: 10000
          FSCHECK_END_SIZE: 500
        run: |
          dotnet test backend/tests/ExpenseFlow.PropertyTests \
            --no-build -c Release \
            --logger "trx;LogFileName=property-results.trx" \
            --results-directory ./test-results

      - name: Upload Results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: property-test-results
          path: ./test-results/
          retention-days: 7

  load-tests:
    runs-on: ubuntu-latest
    if: ${{ !inputs.skip_load_tests }}
    timeout-minutes: 25

    services:
      postgres:
        image: postgres:15-alpine
        env:
          POSTGRES_DB: expenseflow_test
          POSTGRES_USER: test
          POSTGRES_PASSWORD: test
        ports:
          - 5432:5432

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
          cache: true

      - name: Restore & Build
        run: |
          dotnet restore backend/ExpenseFlow.sln --locked-mode
          dotnet build backend/ExpenseFlow.sln -c Release --no-restore

      - name: Start API Server
        run: |
          dotnet run --project backend/src/ExpenseFlow.Api -c Release &
          sleep 10  # Wait for server to start

      - name: Run Load Tests
        env:
          API_BASE_URL: http://localhost:5000
        run: |
          dotnet test backend/tests/ExpenseFlow.LoadTests \
            --no-build -c Release \
            --logger "trx;LogFileName=load-results.trx" \
            --results-directory ./test-results

      - name: Upload NBomber Reports
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: load-test-reports
          path: |
            ./test-results/
            ./nbomber-reports/
          retention-days: 7

  report:
    runs-on: ubuntu-latest
    needs: [chaos-tests, resilience-tests, property-exhaustive, load-tests]
    if: always()

    steps:
      - name: Generate Summary
        run: |
          echo "# 🌙 Nightly Test Report" >> $GITHUB_STEP_SUMMARY
          echo "" >> $GITHUB_STEP_SUMMARY
          echo "**Run Date**: $(date -u +%Y-%m-%d)" >> $GITHUB_STEP_SUMMARY
          echo "" >> $GITHUB_STEP_SUMMARY
          echo "## Results" >> $GITHUB_STEP_SUMMARY
          echo "" >> $GITHUB_STEP_SUMMARY
          echo "| Test Suite | Status |" >> $GITHUB_STEP_SUMMARY
          echo "|------------|--------|" >> $GITHUB_STEP_SUMMARY
          echo "| Chaos Tests | ${{ needs.chaos-tests.result == 'success' && '✅ Pass' || '❌ Fail' }} |" >> $GITHUB_STEP_SUMMARY
          echo "| Resilience Tests | ${{ needs.resilience-tests.result == 'success' && '✅ Pass' || '❌ Fail' }} |" >> $GITHUB_STEP_SUMMARY
          echo "| Property Tests | ${{ needs.property-exhaustive.result == 'success' && '✅ Pass' || '❌ Fail' }} |" >> $GITHUB_STEP_SUMMARY
          echo "| Load Tests | ${{ needs.load-tests.result == 'success' && '✅ Pass' || (needs.load-tests.result == 'skipped' && '⏭️ Skipped' || '❌ Fail') }} |" >> $GITHUB_STEP_SUMMARY

      - name: Check for Failures
        if: contains(needs.*.result, 'failure')
        run: |
          echo "::error::One or more nightly test suites failed. Check the workflow run for details."
          # Create GitHub issue for tracking (optional)
```

## Success Criteria Mapping

| Requirement | Implementation |
|-------------|----------------|
| FR-003: Nightly chaos/resilience | Cron schedule + chaos-tests/resilience-tests jobs |
| FR-006: Mock external services | WireMock service container |
| FR-014: 7-day artifact retention | `retention-days: 7` on artifacts |
| SC-003: <2% flakiness | Property tests with 10,000 iterations |
| User Story 3: Resilience verification | Chaos + resilience test jobs |

## Chaos Test Categories

```csharp
[Trait("Category", "Chaos")]
public class OcrServiceChaosTests
{
    [Fact]
    public async Task WhenDocumentIntelligenceFails_ReceiptMarkedAsError()
    {
        // Chaos policy injects HttpRequestException
        // Test verifies receipt goes to Error status
        // and can be retried
    }
}

[Trait("Category", "Resilience")]
public class CircuitBreakerResilienceTests
{
    [Fact]
    public async Task WhenCircuitBreaks_FallbackBehaviorActivates()
    {
        // Verify circuit breaker opens after threshold
        // and fallback logic is invoked
    }
}
```
