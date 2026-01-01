# Implementation Plan: Comprehensive Testing Suite

**Branch**: `020-testing-suite` | **Date**: 2025-12-31 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/020-testing-suite/spec.md`

## Summary

Implement a comprehensive testing suite that delivers three testing strategies (Contract-Driven, Scenario-Based Integration, Property-Based + Chaos) with CI/CD automation via GitHub Actions. The suite provides fast feedback on every commit (<3 minutes for unit + contract tests) and comprehensive validation on pull requests (<15 minutes for the full suite), with nightly chaos/resilience testing.

## Technical Context

**Language/Version**: .NET 8 with C# 12 (backend), TypeScript 5.7+ (frontend)
**Primary Dependencies**:
- Backend: xUnit 2.7.0, FsCheck (property testing), Testcontainers.PostgreSQL 3.7.0, WireMock.Net, Polly (chaos injection), Moq 4.20.70, FluentAssertions 6.12.0, NBomber 5.5.0
- Frontend: Vitest 4.0.16, @testing-library/react 16.3.1, @playwright/test 1.57.0
**Storage**: PostgreSQL 15+ via Testcontainers (test isolation per workflow)
**Testing**: xUnit (backend unit/integration/contract), Vitest (frontend unit), Playwright (E2E)
**Target Platform**: GitHub Actions (Linux ubuntu-latest runners with Docker support)
**Project Type**: Web application (backend + frontend)
**Performance Goals**: <3 min commit feedback, <15 min PR suite, <2% test flakiness
**Constraints**: GitHub Actions concurrent job limits, 80% code coverage threshold on changed files, Docker-in-Docker for Testcontainers
**Scale/Scope**: 18 API controllers (~60 endpoints), 12 frontend routes, 6 background jobs, 5 external service integrations

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| Clean Architecture 4-Layer | ✅ PASS | Test projects follow layer structure: Api.Tests, Core.Tests, Infrastructure.Tests |
| Test-First Development | ✅ PASS | Feature directly implements and extends test infrastructure |
| ERP Integration (Vista) | ✅ PASS | WireMock will mock Vista SQL responses for integration tests |
| API Design (RESTful + ProblemDetails) | ✅ PASS | Contract tests validate OpenAPI spec compliance |
| Observability (Serilog) | ✅ PASS | Test infrastructure will use structured logging |
| Security (Azure AD/Entra) | ✅ PASS | Integration tests will mock JWT bearer tokens |

**Constitution Compliance**: All principles satisfied. No violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/020-testing-suite/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output - testing strategy research
├── data-model.md        # Phase 1 output - test entity definitions
├── quickstart.md        # Phase 1 output - running tests locally
├── contracts/           # Phase 1 output - GitHub Actions workflow specs
│   ├── ci-quick.yml.md  # Commit-triggered workflow contract
│   ├── ci-full.yml.md   # PR-triggered workflow contract
│   └── ci-nightly.yml.md # Nightly chaos workflow contract
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
.github/
├── workflows/
│   ├── ci-quick.yml         # NEW: Commit → unit + contract tests (<3 min)
│   ├── ci-full.yml          # NEW: PR → full test suite (<15 min)
│   └── ci-nightly.yml       # NEW: Scheduled chaos + resilience tests

backend/
├── src/
│   └── [existing 4-layer structure]
└── tests/
    ├── ExpenseFlow.Api.Tests/           # EXISTING: Controller tests
    ├── ExpenseFlow.Core.Tests/          # EXISTING: Service unit tests
    ├── ExpenseFlow.Infrastructure.Tests/ # EXISTING: Repository tests
    ├── ExpenseFlow.LoadTests/           # EXISTING: NBomber load tests
    ├── ExpenseFlow.Contracts.Tests/     # NEW: OpenAPI contract validation
    ├── ExpenseFlow.PropertyTests/       # NEW: FsCheck property-based tests
    └── ExpenseFlow.Scenarios.Tests/     # NEW: End-to-end scenario tests
        ├── Fixtures/                    # Test data factories
        ├── Mocks/                       # WireMock stubs for Azure AI, Vista
        └── Scenarios/                   # User journey tests

frontend/
├── src/
│   └── [existing component structure]
└── tests/
    ├── unit/                            # EXISTING: Vitest component tests
    ├── integration/                     # NEW: API integration tests
    └── e2e/                             # EXISTING: Playwright E2E tests
```

**Structure Decision**: Web application structure with existing backend/frontend separation. Three new test projects added to backend: Contracts (OpenAPI validation), PropertyTests (FsCheck invariants), Scenarios (TestContainers integration). GitHub Actions workflows created at repository root `.github/workflows/`.

## Phase 0: Research

Research required to inform implementation decisions:

1. **Contract Testing Tools**: Evaluate OpenAPI validation libraries for .NET (Swashbuckle, NSwag, OpenAPI.NET)
2. **WireMock Configuration**: Research WireMock.Net patterns for stubbing Azure Document Intelligence and OpenAI responses
3. **FsCheck Integration**: Research FsCheck generator patterns for expense domain (amounts, dates, GL codes)
4. **Polly Chaos Policies**: Research chaos injection patterns for simulating external service failures
5. **GitHub Actions Optimization**: Research caching strategies, parallel job configuration, and Docker-in-Docker setup
6. **Codecov Integration**: Research coverage reporting and threshold enforcement configuration

## Phase 1: Design

Design artifacts to be produced:

1. **data-model.md**: Define test entities (TestRun, TestResult, QuarantinedTest, TestFixture)
2. **contracts/ci-quick.yml.md**: Workflow specification for commit-triggered tests
3. **contracts/ci-full.yml.md**: Workflow specification for PR-triggered tests
4. **contracts/ci-nightly.yml.md**: Workflow specification for scheduled chaos tests
5. **quickstart.md**: Local development guide for running tests identically to CI

## Complexity Tracking

> **No violations identified** - Feature aligns with constitution principles.

| Category | Justification |
|----------|---------------|
| Three new test projects | Required to separate concerns: contracts (API validation), property tests (invariants), scenarios (E2E) |
| GitHub Actions workflows | Essential for CI/CD automation - core requirement of the feature |
| WireMock external mocks | Constitution requires mocking ERP and external services for test isolation |
