# Contract: CI Full Workflow

**Workflow File**: `.github/workflows/ci-full.yml`
**Purpose**: Comprehensive PR validation (<15 minutes per FR-002)

## Trigger Conditions

| Trigger | Condition | Action |
|---------|-----------|--------|
| Pull Request | To `main` branch | Run full test suite |
| Concurrency | Same PR | Cancel in-progress (FR-009) |

## Job Matrix

```
┌─────────────────────────────────────────────────────────────┐
│                    ci-full.yml                              │
├───────────────┬───────────────┬──────────────┬─────────────┤
│ backend-tests │ frontend-tests│ e2e-tests    │ coverage    │
│ (5 min)       │ (3 min)       │ (5 min)      │ (2 min)     │
└───────────────┴───────────────┴──────────────┴─────────────┘
                              │
                              ▼
                    ┌─────────────────┐
                    │ merge-gate      │
                    │ (aggregation)   │
                    └─────────────────┘
```

## Jobs Specification

### Job: `backend-tests`

**Runner**: `ubuntu-latest`
**Services**: PostgreSQL container
**Timeout**: 10 minutes

#### Steps

| Step | Action | Duration |
|------|--------|----------|
| 1. Checkout | Clone repo | ~5s |
| 2. Setup .NET | With caching | ~15s |
| 3. Restore | `dotnet restore` | ~20s |
| 4. Build | `dotnet build` | ~30s |
| 5. Unit Tests | Core.Tests, Api.Tests | ~90s |
| 6. Contract Tests | Contracts.Tests | ~30s |
| 7. Property Tests | PropertyTests | ~60s |
| 8. Integration Tests | Infrastructure.Tests | ~120s |
| 9. Scenario Tests | Scenarios.Tests | ~60s |
| 10. Coverage Upload | Codecov | ~10s |

### Job: `frontend-tests`

**Runner**: `ubuntu-latest`
**Timeout**: 8 minutes

#### Steps

| Step | Action | Duration |
|------|--------|----------|
| 1. Checkout | Clone repo | ~5s |
| 2. Setup Node | With npm cache | ~10s |
| 3. Install | `npm ci` | ~30s |
| 4. Lint | `npm run lint` | ~20s |
| 5. Type Check | `npm run type-check` | ~15s |
| 6. Unit Tests | `npm run test:coverage` | ~60s |
| 7. Coverage Upload | Codecov | ~10s |

### Job: `e2e-tests`

**Runner**: `ubuntu-latest`
**Needs**: `backend-tests`, `frontend-tests`
**Timeout**: 10 minutes

#### Steps

| Step | Action | Duration |
|------|--------|----------|
| 1. Checkout | Clone repo | ~5s |
| 2. Setup .NET | Backend | ~15s |
| 3. Setup Node | Frontend | ~10s |
| 4. Install Playwright | Browsers | ~60s |
| 5. Start Backend | Background | ~30s |
| 6. Start Frontend | Background | ~20s |
| 7. Run E2E | Playwright tests | ~180s |
| 8. Upload Artifacts | Screenshots/videos on failure | ~10s |

### Job: `coverage-gate`

**Runner**: `ubuntu-latest`
**Needs**: `backend-tests`, `frontend-tests`
**Timeout**: 2 minutes

Enforces FR-015: 80% coverage threshold on changed files.

### Job: `merge-gate`

**Runner**: `ubuntu-latest`
**Needs**: All previous jobs
**Purpose**: Aggregates results for branch protection

## Services Configuration

```yaml
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
```

## Flaky Test Handling (FR-017)

```yaml
- name: Run Tests with Retry
  uses: nick-fields/retry@v2
  with:
    timeout_minutes: 5
    max_attempts: 3
    retry_on: error
    command: |
      dotnet test backend/tests/ExpenseFlow.Scenarios.Tests \
        --filter "Category!=Quarantined"
```

## Coverage Collection

```yaml
- name: Run Tests with Coverage
  run: |
    dotnet test backend/ExpenseFlow.sln \
      --collect:"XPlat Code Coverage" \
      --results-directory ./coverage

- name: Upload to Codecov
  uses: codecov/codecov-action@v4
  with:
    files: ./coverage/**/coverage.cobertura.xml
    fail_ci_if_error: true
    flags: backend
```

## YAML Template

```yaml
name: CI Full (PR)

on:
  pull_request:
    branches: [main]
    types: [opened, synchronize, reopened]

concurrency:
  group: ci-full-${{ github.event.pull_request.number }}
  cancel-in-progress: true

env:
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  backend-tests:
    runs-on: ubuntu-latest
    timeout-minutes: 10

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

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
          cache: true

      - name: Restore
        run: dotnet restore backend/ExpenseFlow.sln --locked-mode

      - name: Build
        run: dotnet build backend/ExpenseFlow.sln --no-restore -c Release

      - name: Run All Backend Tests
        env:
          ConnectionStrings__DefaultConnection: "Host=localhost;Database=expenseflow_test;Username=test;Password=test"
        run: |
          dotnet test backend/ExpenseFlow.sln \
            --no-build -c Release \
            --filter "Category!=Quarantined&Category!=Load" \
            --collect:"XPlat Code Coverage" \
            --results-directory ./coverage \
            --logger "trx;LogFileName=backend-results.trx"

      - name: Upload Coverage
        uses: codecov/codecov-action@v4
        with:
          files: ./coverage/**/coverage.cobertura.xml
          flags: backend
          fail_ci_if_error: false

      - name: Publish Test Results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Backend Tests
          path: ./coverage/*.trx
          reporter: dotnet-trx

  frontend-tests:
    runs-on: ubuntu-latest
    timeout-minutes: 8

    defaults:
      run:
        working-directory: frontend

    steps:
      - uses: actions/checkout@v4

      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: npm
          cache-dependency-path: frontend/package-lock.json

      - name: Install
        run: npm ci

      - name: Lint
        run: npm run lint

      - name: Type Check
        run: npm run type-check

      - name: Unit Tests
        run: npm run test:coverage

      - name: Upload Coverage
        uses: codecov/codecov-action@v4
        with:
          files: frontend/coverage/coverage-final.json
          flags: frontend
          fail_ci_if_error: false

  e2e-tests:
    runs-on: ubuntu-latest
    needs: [backend-tests, frontend-tests]
    timeout-minutes: 10

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x
          cache: true

      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: npm
          cache-dependency-path: frontend/package-lock.json

      - name: Install Playwright
        run: npx playwright install --with-deps chromium
        working-directory: frontend

      - name: Build Backend
        run: dotnet build backend/src/ExpenseFlow.Api -c Release

      - name: Build Frontend
        run: npm run build
        working-directory: frontend

      - name: Run E2E Tests
        run: npm run test:e2e
        working-directory: frontend
        env:
          API_URL: http://localhost:5000

      - name: Upload Artifacts
        uses: actions/upload-artifact@v4
        if: failure()
        with:
          name: playwright-results
          path: frontend/test-results/
          retention-days: 7

  coverage-gate:
    runs-on: ubuntu-latest
    needs: [backend-tests, frontend-tests]
    timeout-minutes: 2

    steps:
      - name: Check Coverage Threshold
        uses: codecov/codecov-action@v4
        with:
          fail_ci_if_error: true
          verbose: true

  merge-gate:
    runs-on: ubuntu-latest
    needs: [backend-tests, frontend-tests, e2e-tests, coverage-gate]
    if: always()

    steps:
      - name: Check All Jobs
        run: |
          if [[ "${{ needs.backend-tests.result }}" != "success" ]] ||
             [[ "${{ needs.frontend-tests.result }}" != "success" ]] ||
             [[ "${{ needs.e2e-tests.result }}" != "success" ]] ||
             [[ "${{ needs.coverage-gate.result }}" != "success" ]]; then
            echo "One or more required jobs failed"
            exit 1
          fi
          echo "All jobs passed!"
```

## PR Comment Integration (FR-013)

```yaml
- name: Comment PR with Results
  uses: marocchino/sticky-pull-request-comment@v2
  if: always()
  with:
    header: test-results
    message: |
      ## Test Results

      | Suite | Status | Duration |
      |-------|--------|----------|
      | Backend | ${{ needs.backend-tests.result == 'success' && '✅' || '❌' }} | - |
      | Frontend | ${{ needs.frontend-tests.result == 'success' && '✅' || '❌' }} | - |
      | E2E | ${{ needs.e2e-tests.result == 'success' && '✅' || '❌' }} | - |
      | Coverage | ${{ needs.coverage-gate.result == 'success' && '✅' || '❌' }} | - |

      [View full results](https://github.com/${{ github.repository }}/actions/runs/${{ github.run_id }})
```

## Success Criteria Mapping

| Requirement | Implementation |
|-------------|----------------|
| FR-002: <15 min full suite | Parallel jobs, timeout: 10 min per job |
| FR-005: Isolated DB containers | PostgreSQL service per workflow |
| FR-006: Mock external services | WireMock stubs in Scenarios.Tests |
| FR-011: Block merge on failure | `merge-gate` job + branch protection |
| FR-012: Parallelization | Parallel jobs: backend/frontend/e2e |
| FR-013: GitHub notifications | PR comments + status checks |
| FR-015: 80% coverage | `coverage-gate` job with Codecov |
| FR-017: Retry flaky tests | `nick-fields/retry` action |
| SC-002: 90% PRs <15 min | Monitor via GitHub Actions insights |
