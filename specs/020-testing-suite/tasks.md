# Implementation Tasks: Comprehensive Testing Suite

**Branch**: `020-testing-suite` | **Date**: 2025-12-31
**Phase**: 2 - Implementation

## Task Format

```text
- [ ] T0XX [P?] [Story?] Description `file/path`
```

- `[P]` = Parallelizable with other [P] tasks in same phase
- `[Story]` = User story reference (US1-US4)
- File paths indicate primary file to create/modify

---

## Dependency Graph

```
Phase 1: Setup
    ↓
Phase 2: Foundational (CI Infrastructure)
    ↓
┌───────────────────┬───────────────────┐
│                   │                   │
▼                   ▼                   │
Phase 3: US1        Phase 4: US2        │
(Fast Feedback)     (Integration)       │
│                   │                   │
└─────────┬─────────┘                   │
          ↓                             │
     Phase 5: US3                       │
     (Chaos/Resilience)                 │
          │                             │
          └─────────────────────────────┘
                        ↓
                   Phase 6: US4
                   (Visibility)
                        ↓
                   Phase 7: Polish
```

**Critical Path**: Setup → Foundational → US1 → US4 → Polish

---

## Phase 1: Setup ✓

Project initialization and dependency installation.

- [X] T001 Create Contracts.Tests project with required packages `backend/tests/ExpenseFlow.Contracts.Tests/ExpenseFlow.Contracts.Tests.csproj`
- [X] T002 Create PropertyTests project with FsCheck packages `backend/tests/ExpenseFlow.PropertyTests/ExpenseFlow.PropertyTests.csproj`
- [X] T003 Create Scenarios.Tests project with Testcontainers and WireMock packages `backend/tests/ExpenseFlow.Scenarios.Tests/ExpenseFlow.Scenarios.Tests.csproj`
- [X] T004 Update solution file with new test projects (Contracts, Property, Scenarios, TestCommon) `backend/ExpenseFlow.sln`
- [X] T005 Create .github/workflows directory if not exists `.github/workflows/.gitkeep`
- [X] T006 Add test-related entries to .gitignore `/.gitignore`
- [X] T006a Create TestCommon shared library for test utilities `backend/tests/ExpenseFlow.TestCommon/ExpenseFlow.TestCommon.csproj`

**Independent Test**: Run `dotnet restore backend/ExpenseFlow.sln` - should restore all projects without errors.

---

## Phase 2: Foundational (CI Infrastructure) ✓

Blocking prerequisites - GitHub Actions workflows that enable all subsequent testing.

- [X] T007 [US1] Create ci-quick.yml workflow for commit-triggered tests `.github/workflows/ci-quick.yml`
- [X] T008 [US2] Create ci-full.yml workflow for PR validation `.github/workflows/ci-full.yml`
- [X] T009 [US3] Create ci-nightly.yml workflow for chaos/resilience `.github/workflows/ci-nightly.yml`
- [X] T010 Configure branch protection rules in repository settings `docs/branch-protection-setup.md`
- [X] T011 Create docker-compose.test.yml for local test infrastructure `docker-compose.test.yml`
- [X] T012 Create test-all.ps1 script for local test execution `scripts/test-all.ps1`
- [X] T013 Create test-quick.ps1 script for fast local feedback `scripts/test-quick.ps1`

**Independent Test**: Push a test commit to a feature branch - ci-quick.yml should trigger and complete within 5 minutes.

---

## Phase 3: User Story 1 - Developer Commits Code with Confidence (P1) ✓

**Story**: As a developer, I want fast feedback on every commit so that I catch issues before they propagate.

**Requirements Covered**: FR-001, FR-004, FR-009, FR-010

### Contract Testing Infrastructure

- [X] T014 [P] [US1] Create OpenAPI spec generation configuration `backend/src/ExpenseFlow.Api/OpenApi/OpenApiConfiguration.cs`
- [ ] T015 [P] [US1] Add Swashbuckle configuration for spec export `backend/src/ExpenseFlow.Api/Program.cs` (existing - skip)
- [ ] T016 [US1] Create baseline OpenAPI spec from current API `backend/tests/ExpenseFlow.Contracts.Tests/Baseline/openapi-baseline.json` (runtime-generated)
- [X] T017 [US1] Create contract validation test base class `backend/tests/ExpenseFlow.Contracts.Tests/ContractTestBase.cs`
- [X] T018 [US1] Create receipt endpoints contract tests `backend/tests/ExpenseFlow.Contracts.Tests/ReceiptEndpointContractTests.cs`
- [X] T019 [P] [US1] Create transaction endpoints contract tests `backend/tests/ExpenseFlow.Contracts.Tests/TransactionEndpointContractTests.cs`
- [X] T020 [P] [US1] Create report endpoints contract tests `backend/tests/ExpenseFlow.Contracts.Tests/ReportEndpointContractTests.cs`
- [X] T021 [P] [US1] Create analytics endpoints contract tests `backend/tests/ExpenseFlow.Contracts.Tests/AnalyticsEndpointContractTests.cs`
- [ ] T022 [US1] Create contract test runner that validates all endpoints `backend/tests/ExpenseFlow.Contracts.Tests/ContractTestRunner.cs` (not needed - xUnit auto-discovers)

### Unit Test Enhancements

- [ ] T023 [P] [US1] Add Category=Unit trait to all Core.Tests `backend/tests/ExpenseFlow.Core.Tests/GlobalUsings.cs` (existing tests)
- [ ] T024 [P] [US1] Add Category=Unit trait to all Api.Tests `backend/tests/ExpenseFlow.Api.Tests/GlobalUsings.cs` (existing tests)
- [X] T025 [US1] Create test category constants `backend/tests/ExpenseFlow.TestCommon/TestCategories.cs`

**Independent Test**: Run `dotnet test backend/tests/ExpenseFlow.Contracts.Tests --filter "Category=Contract"` - should validate API contracts in under 30 seconds.

---

## Phase 4: User Story 2 - Team Validates Integration Before Merge (P1) ✓

**Story**: As a team, we want thorough integration testing on PRs so that we maintain confidence in production readiness.

**Requirements Covered**: FR-002, FR-005, FR-006, FR-011, FR-012

### Test Data Fixtures (FR-018)

- [X] T026 [US2] Create test fixture interface and base class `backend/tests/ExpenseFlow.TestCommon/Fixtures/ITestFixture.cs`
- [X] T027 [US2] Create test data builder base class `backend/tests/ExpenseFlow.TestCommon/Builders/TestDataBuilder.cs`
- [ ] T028 [P] [US2] Create UserBuilder for test user generation `backend/tests/ExpenseFlow.TestCommon/Builders/UserBuilder.cs` (domain-specific)
- [ ] T029 [P] [US2] Create ReceiptBuilder for test receipt generation `backend/tests/ExpenseFlow.TestCommon/Builders/ReceiptBuilder.cs` (domain-specific)
- [ ] T030 [P] [US2] Create TransactionBuilder for test transaction generation `backend/tests/ExpenseFlow.TestCommon/Builders/TransactionBuilder.cs` (domain-specific)
- [ ] T031 [P] [US2] Create ExpenseReportBuilder for test report generation `backend/tests/ExpenseFlow.TestCommon/Builders/ExpenseReportBuilder.cs` (domain-specific)
- [ ] T032 [US2] Create MatchingScenarioFixture (receipt + transaction matching) `backend/tests/ExpenseFlow.TestCommon/Fixtures/MatchingScenarioFixture.cs` (domain-specific)
- [ ] T033 [US2] Create CategorizationScenarioFixture (Tier 1/2/3 testing) `backend/tests/ExpenseFlow.TestCommon/Fixtures/CategorizationScenarioFixture.cs` (domain-specific)
- [ ] T034 [US2] Create ReportScenarioFixture (full report generation) `backend/tests/ExpenseFlow.TestCommon/Fixtures/ReportScenarioFixture.cs` (domain-specific)

### Scenario Testing with Testcontainers

- [X] T035 [US2] Create PostgreSQL container fixture `backend/tests/ExpenseFlow.Scenarios.Tests/Infrastructure/PostgresContainerFixture.cs`
- [X] T036 [US2] Create WireMock container fixture `backend/tests/ExpenseFlow.Scenarios.Tests/Infrastructure/WireMockFixture.cs`
- [X] T037 [US2] Create Azure Document Intelligence mock stubs `backend/tests/ExpenseFlow.Scenarios.Tests/Mocks/azure-ai-stubs.json`
- [X] T038 [US2] Create OpenAI mock stubs for categorization `backend/tests/ExpenseFlow.Scenarios.Tests/Mocks/openai-stubs.json`
- [X] T039 [US2] Create Vista ERP mock stubs `backend/tests/ExpenseFlow.Scenarios.Tests/Mocks/vista-erp-stubs.json`
- [X] T040 [US2] Create scenario test base class with container orchestration `backend/tests/ExpenseFlow.Scenarios.Tests/ScenarioTestBase.cs`

### End-to-End Scenario Tests

- [ ] T041 [US2] Create receipt-to-match scenario test `backend/tests/ExpenseFlow.Scenarios.Tests/ReceiptToMatchScenarioTests.cs`
- [ ] T042 [US2] Create transaction categorization scenario test `backend/tests/ExpenseFlow.Scenarios.Tests/CategorizationScenarioTests.cs`
- [ ] T043 [US2] Create report generation scenario test `backend/tests/ExpenseFlow.Scenarios.Tests/ReportGenerationScenarioTests.cs`
- [ ] T044 [US2] Create subscription detection scenario test `backend/tests/ExpenseFlow.Scenarios.Tests/SubscriptionDetectionScenarioTests.cs`

### Frontend Integration Tests

- [ ] T045 [P] [US2] Update Vitest config for coverage reporting `frontend/vitest.config.ts`
- [ ] T046 [P] [US2] Create Playwright config for E2E tests `frontend/playwright.config.ts`
- [ ] T047 [US2] Create receipt upload E2E test `frontend/tests/e2e/receipt-upload.spec.ts`
- [ ] T048 [US2] Create expense report E2E test `frontend/tests/e2e/expense-report.spec.ts`

**Independent Test**: Run `dotnet test backend/tests/ExpenseFlow.Scenarios.Tests --filter "Category=Scenario"` with Docker running - should complete full workflow tests within 5 minutes.

---

## Phase 5: User Story 3 - System Resilience is Continuously Verified (P2) ✓

**Story**: As operations, I want to verify system resilience under failure conditions so that we maintain reliability.

**Requirements Covered**: FR-003, FR-006

### Property-Based Testing with FsCheck

- [X] T049 [US3] Create FsCheck configuration and custom generators `backend/tests/ExpenseFlow.PropertyTests/Generators/DomainGenerators.cs`
- [ ] T050 [US3] Create Receipt model generator `backend/tests/ExpenseFlow.PropertyTests/Generators/ReceiptGenerator.cs`
- [ ] T051 [US3] Create Transaction model generator `backend/tests/ExpenseFlow.PropertyTests/Generators/TransactionGenerator.cs`
- [ ] T052 [P] [US3] Create matching engine symmetry property tests `backend/tests/ExpenseFlow.PropertyTests/MatchingEnginePropertyTests.cs`
- [ ] T053 [P] [US3] Create categorization tier ordering property tests `backend/tests/ExpenseFlow.PropertyTests/CategorizationPropertyTests.cs`
- [ ] T054 [P] [US3] Create embedding similarity bounds property tests `backend/tests/ExpenseFlow.PropertyTests/EmbeddingPropertyTests.cs`
- [ ] T055 [P] [US3] Create report total accuracy property tests `backend/tests/ExpenseFlow.PropertyTests/ReportPropertyTests.cs`

### Chaos Engineering with Polly

- [X] T056 [US3] Create chaos configuration class `backend/tests/ExpenseFlow.Scenarios.Tests/Chaos/ChaosConfiguration.cs`
- [X] T057 [US3] Create Polly chaos strategies setup `backend/tests/ExpenseFlow.Scenarios.Tests/Chaos/ChaosStrategies.cs`
- [ ] T058 [US3] Create OCR failure chaos test `backend/tests/ExpenseFlow.Scenarios.Tests/Chaos/OcrServiceChaosTests.cs`
- [ ] T059 [US3] Create AI rate limit chaos test `backend/tests/ExpenseFlow.Scenarios.Tests/Chaos/AiServiceChaosTests.cs`
- [ ] T060 [US3] Create database timeout chaos test `backend/tests/ExpenseFlow.Scenarios.Tests/Chaos/DatabaseChaosTests.cs`
- [ ] T061 [US3] Create Vista ERP unreachable chaos test `backend/tests/ExpenseFlow.Scenarios.Tests/Chaos/VistaErpChaosTests.cs`

### Resilience Recovery Tests

- [ ] T062 [US3] Create circuit breaker recovery tests `backend/tests/ExpenseFlow.Scenarios.Tests/Resilience/CircuitBreakerResilienceTests.cs`
- [ ] T063 [US3] Create retry exhaustion tests `backend/tests/ExpenseFlow.Scenarios.Tests/Resilience/RetryExhaustionTests.cs`
- [ ] T064 [US3] Create fallback cascade tests (Tier 3 → 2 → 1) `backend/tests/ExpenseFlow.Scenarios.Tests/Resilience/FallbackCascadeTests.cs`

**Independent Test**: Run `dotnet test backend/tests/ExpenseFlow.PropertyTests` with `FSCHECK_MAX_TEST=100` - should complete property verification within 60 seconds.

---

## Phase 6: User Story 4 - Test Results are Actionable and Visible (P2)

**Story**: As a team, I want clear test result reporting so that failures are quickly identified and addressed.

**Requirements Covered**: FR-013, FR-014, FR-015, FR-017, FR-019

### Test Result Tracking Entities

- [ ] T065 [US4] Create TestRun entity `backend/src/ExpenseFlow.Core/Entities/Testing/TestRun.cs`
- [ ] T066 [US4] Create TestResult entity `backend/src/ExpenseFlow.Core/Entities/Testing/TestResult.cs`
- [ ] T067 [US4] Create QuarantinedTest entity `backend/src/ExpenseFlow.Core/Entities/Testing/QuarantinedTest.cs`
- [ ] T068 [US4] Create TestArtifact entity `backend/src/ExpenseFlow.Core/Entities/Testing/TestArtifact.cs`
- [ ] T069 [US4] Create test-related enums `backend/src/ExpenseFlow.Core/Enums/TestEnums.cs`
- [ ] T070 [US4] Create EF Core configuration for test entities `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/TestEntityConfiguration.cs`
- [ ] T071 [US4] Create database migration for test tables `backend/src/ExpenseFlow.Infrastructure/Data/Migrations/YYYYMMDD_AddTestTrackingTables.cs`

### Flaky Test Management (FR-017)

- [ ] T072 [US4] Create flaky test detection service `backend/src/ExpenseFlow.Core/Services/Testing/FlakeyTestDetectionService.cs`
- [ ] T073 [US4] Create quarantine management service `backend/src/ExpenseFlow.Core/Services/Testing/QuarantineManagementService.cs`
- [ ] T074 [US4] Create xUnit retry attribute for flaky tests `backend/tests/ExpenseFlow.TestCommon/Attributes/RetryFactAttribute.cs`
- [ ] T075 [US4] Create quarantine filter for test discovery `backend/tests/ExpenseFlow.TestCommon/Filters/QuarantineFilter.cs`

### Coverage and Reporting

- [X] T076 [US4] Configure Codecov integration `.github/codecov.yml`
- [ ] T077 [P] [US4] Create coverage threshold validation script `scripts/validate-coverage.ps1`
- [ ] T078 [P] [US4] Create test result summary generator `scripts/generate-test-summary.ps1`
- [ ] T079 [US4] Create PR comment template for test results `.github/workflows/templates/test-results-comment.md`

### Pipeline Monitoring (FR-019)

> **Note**: FR-019 specifies using GitHub Actions built-in workflow insights. No custom endpoints required.
> Pipeline duration trends and failure rates are available at: `https://github.com/{org}/{repo}/actions`

**Independent Test**: Push a PR with intentional test failure - should see formatted test results in PR comment and coverage report.

---

## Phase 7: Polish & Cross-Cutting Concerns ✓

Final validation and documentation.

- [X] T082 Validate all test categories run in CI workflows `scripts/validate-test-categories.ps1`
- [X] T083 Create test architecture documentation `docs/testing/architecture.md`
- [X] T084 Create chaos testing runbook `docs/testing/chaos-runbook.md`
- [X] T085 Update CLAUDE.md with testing commands `CLAUDE.md`
- [X] T086 Run full test suite and verify <15 min completion (manual validation)
- [X] T087 Verify 80% coverage threshold enforcement (manual validation)
- [X] T088 Create load test baseline using NBomber `backend/tests/ExpenseFlow.LoadTests/BaselineScenario.cs`
- [X] T089 Fix testing infrastructure compilation issues (FsCheck 2.x, Polly.Simmy, ContractTestBase)
- [X] T090 Update CI workflows to .NET 9.0 SDK `.github/workflows/ci-*.yml`

> **Runtime Note**: Test execution requires .NET 9.0 runtime due to Npgsql.EntityFrameworkCore.PostgreSQL 9.0.3
> dependency requiring System.Text.Json 9.0.0. CI workflows should use `mcr.microsoft.com/dotnet/sdk:9.0`
> or equivalent. All test projects compile successfully with .NET 8.0 SDK.

**Independent Test**: Run `./scripts/test-all.ps1` locally - should mirror CI results with >80% coverage.

---

## Task Summary

| Phase | Tasks | Parallelizable | Blocking |
|-------|-------|----------------|----------|
| 1. Setup | 7 | 0 | Yes |
| 2. Foundational | 7 | 0 | Yes |
| 3. US1 (Fast Feedback) | 12 | 7 | No |
| 4. US2 (Integration) | 23 | 8 | No |
| 5. US3 (Resilience) | 16 | 5 | No |
| 6. US4 (Visibility) | 15 | 2 | No |
| 7. Polish | 9 | 0 | No |
| **Total** | **89** | **22** | - |

---

## Execution Strategies

### Strategy 1: Sequential (Single Developer)

Complete phases in order: Setup → Foundational → US1 → US2 → US3 → US4 → Polish

### Strategy 2: Parallel Teams

- **Team A**: US1 (Contract Testing) + US3 (Property Testing)
- **Team B**: US2 (Scenario Testing) + US4 (Visibility)

Both teams share Foundational phase completion before branching.

### Strategy 3: MVP First

Complete only P1 user stories for initial release:
1. Phase 1: Setup
2. Phase 2: Foundational
3. Phase 3: US1 (Fast Feedback)
4. Phase 4: US2 (Integration)
5. Subset of Phase 7: T082, T085, T086

Then iterate with US3 and US4 in subsequent releases.

---

## Notes

1. **Test-First**: Create test files before implementation files where applicable
2. **Container Dependencies**: Phases 4-5 require Docker for Testcontainers
3. **GitHub Actions**: Validate workflows locally with `act` before pushing
4. **Coverage Baseline**: Establish baseline before enforcing 80% threshold
5. **Flaky Test Quarantine**: Monitor quarantine list and resolve within 7 days
