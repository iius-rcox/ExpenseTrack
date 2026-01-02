# Contract: CI Quick Workflow

**Workflow File**: `.github/workflows/ci-quick.yml`
**Purpose**: Fast feedback on every commit (<3 minutes per FR-001)

## Trigger Conditions

| Trigger | Condition | Action |
|---------|-----------|--------|
| Push | Any branch | Run unit + contract tests |
| Concurrency | Same branch | Cancel in-progress (FR-009) |

## Job Specification

### Job: `quick-check`

**Runner**: `ubuntu-latest`
**Timeout**: 5 minutes (buffer for 3-min target)

#### Steps

| Step | Action | Duration Target |
|------|--------|-----------------|
| 1. Checkout | `actions/checkout@v4` | ~5s |
| 2. Setup .NET | `actions/setup-dotnet@v4` with cache | ~15s |
| 3. Restore | `dotnet restore --locked-mode` | ~20s (cached) |
| 4. Build | `dotnet build --no-restore` | ~30s |
| 5. Unit Tests | `dotnet test` (Unit category only) | ~60s |
| 6. Contract Tests | `dotnet test` (Contract category only) | ~30s |
| 7. Report Status | GitHub status check | ~5s |

**Total Target**: < 3 minutes

## Environment Variables

```yaml
env:
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true
  NUGET_PACKAGES: ${{ github.workspace }}/.nuget/packages
```

## Test Filtering

```yaml
# Unit tests only
dotnet test backend/tests/ExpenseFlow.Core.Tests \
  --filter "Category=Unit" \
  --no-build \
  --verbosity normal

# Contract tests only
dotnet test backend/tests/ExpenseFlow.Contracts.Tests \
  --no-build \
  --verbosity normal
```

## Concurrency Control

```yaml
concurrency:
  group: ci-quick-${{ github.ref }}
  cancel-in-progress: true
```

## Expected Outputs

| Output | Type | Description |
|--------|------|-------------|
| GitHub Status | Check | ✅ Pass or ❌ Fail on commit |
| Test Summary | Annotation | Test count and failures |
| Duration | Log | Total workflow time |

## Failure Handling

| Failure Type | Action |
|--------------|--------|
| Test failure | Mark commit as failed, show failing test in status |
| Build failure | Mark commit as failed, show build error |
| Timeout | Mark commit as failed, log timeout |

## YAML Template

```yaml
name: CI Quick (Commit)

on:
  push:
    branches: ['**']
    paths:
      - 'backend/**'
      - 'frontend/**'
      - '!**/*.md'

concurrency:
  group: ci-quick-${{ github.ref }}
  cancel-in-progress: true

env:
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  quick-check:
    runs-on: ubuntu-latest
    timeout-minutes: 5

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
        run: dotnet build backend/ExpenseFlow.sln --no-restore --configuration Release

      - name: Run Unit Tests
        run: |
          dotnet test backend/tests/ExpenseFlow.Core.Tests \
            --no-build \
            --configuration Release \
            --logger "trx;LogFileName=unit-results.trx" \
            --results-directory ./test-results

      - name: Run Contract Tests
        run: |
          dotnet test backend/tests/ExpenseFlow.Contracts.Tests \
            --no-build \
            --configuration Release \
            --logger "trx;LogFileName=contract-results.trx" \
            --results-directory ./test-results

      - name: Publish Test Results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Quick Tests
          path: ./test-results/*.trx
          reporter: dotnet-trx
          fail-on-error: true
```

## Success Criteria Mapping

| Requirement | Implementation |
|-------------|----------------|
| FR-001: <3 min unit + contract | Timeout: 5 min, target: 3 min |
| FR-009: Cancel redundant | `cancel-in-progress: true` |
| FR-010: Cache dependencies | `setup-dotnet` with `cache: true` |
| FR-004: Clear pass/fail status | `dorny/test-reporter` + GitHub checks |
| SC-001: 95% commits <3 min | Monitor via GitHub Actions insights |
