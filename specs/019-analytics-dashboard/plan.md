# Implementation Plan: Analytics Dashboard API Endpoints

**Branch**: `019-analytics-dashboard` | **Date**: 2025-12-31 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/019-analytics-dashboard/spec.md`

## Summary

Implement 5 new API endpoints in the existing AnalyticsController to provide spending analytics that the frontend already expects. Endpoints include spending trends (with day/week/month granularity), category breakdowns, vendor breakdowns, merchant analytics with comparison periods, and subscription detection proxies. Uses existing Transaction and VendorAlias entities with pattern-based category derivation.

## Technical Context

**Language/Version**: .NET 8 with C# 12
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, Npgsql, FluentValidation
**Storage**: PostgreSQL 15+ (Supabase self-hosted), existing Transaction/VendorAlias tables
**Testing**: xUnit, integration tests using WebApplicationFactory
**Target Platform**: Azure Kubernetes Service (linux/amd64)
**Project Type**: Web API (backend only - frontend already exists)
**Performance Goals**: 500ms response for 90-day range with 1000 transactions (SC-002)
**Constraints**: 5-year maximum date range (FR-014), Entra ID authentication required
**Scale/Scope**: Single user context per request, up to 1000 transactions typical

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. Clean Architecture | ✅ PASS | Extends Api layer controller, uses Core entities, Shared DTOs |
| II. Test-First Development | ✅ PASS | Integration tests planned for all 5 endpoints |
| III. ERP Integration | ⚪ N/A | No Vista integration needed for analytics |
| IV. API Design | ✅ PASS | RESTful under `/api/analytics/`, ProblemDetails for errors |
| V. Observability | ✅ PASS | Serilog logging with structured parameters |
| VI. Security | ✅ PASS | [Authorize] attribute, user filtering via IUserService |

**Gate Result**: ✅ PASS - No violations, proceed to Phase 0

## Project Structure

### Documentation (this feature)

```text
specs/019-analytics-dashboard/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (OpenAPI)
└── checklists/          # Quality validation
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Api/
│   │   ├── Controllers/
│   │   │   └── AnalyticsController.cs      # Extend with 5 new endpoints
│   │   └── Validators/
│   │       └── AnalyticsValidators.cs      # Date range validation (new)
│   ├── ExpenseFlow.Core/
│   │   ├── Entities/
│   │   │   └── Transaction.cs              # Existing (read only)
│   │   └── Interfaces/
│   │       └── IAnalyticsService.cs        # New interface
│   ├── ExpenseFlow.Infrastructure/
│   │   └── Services/
│   │       └── AnalyticsService.cs         # New service implementation
│   └── ExpenseFlow.Shared/
│       └── DTOs/
│           └── AnalyticsDtos.cs            # New DTOs for responses
└── tests/
    └── ExpenseFlow.Api.Tests/
        └── Controllers/
            └── AnalyticsControllerTests.cs # Integration tests
```

**Structure Decision**: Follows existing Clean Architecture pattern. New analytics logic goes in Infrastructure/Services, exposed via DTOs in Shared, called from Api/Controllers.

## Complexity Tracking

> No violations - table empty

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none) | - | - |
