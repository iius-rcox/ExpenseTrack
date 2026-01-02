# Implementation Plan: Missing API Endpoints

**Branch**: `021-missing-api-endpoints` | **Date**: 2026-01-01 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/021-missing-api-endpoints/spec.md`

## Summary

Implement three missing API endpoints identified by contract tests: analytics export (`GET /api/analytics/export`), report generation (`POST /api/reports/{id}/generate`), and report submission (`POST /api/reports/{id}/submit`). Additionally, update contract tests to use actual endpoint paths for existing functionality that uses different naming conventions.

## Technical Context

**Language/Version**: .NET 8 with C# 12
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, CsvHelper 31.0.0, ClosedXML 0.102.2, FluentValidation
**Storage**: PostgreSQL 15+ (Supabase), Azure Blob Storage
**Testing**: xUnit, FluentAssertions, Moq, Contract tests
**Target Platform**: Azure Kubernetes Service (linux/amd64)
**Project Type**: Web application (4-layer Clean Architecture: Api → Infrastructure → Core ← Shared)
**Performance Goals**: Analytics export < 10 seconds for 12-month date ranges
**Constraints**: All endpoints require JWT authentication, ProblemDetails error responses
**Scale/Scope**: Single-user expense tracking; typical reports have 10-100 expense lines

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Clean Architecture** | ✅ PASS | New endpoints follow Api → Infrastructure → Core flow |
| **II. Test-First Development** | ✅ PASS | Contract tests exist; will add unit/integration tests |
| **III. ERP Integration** | ⚪ N/A | No Vista integration required for these endpoints |
| **IV. API Design** | ✅ PASS | RESTful, ProblemDetails, FluentValidation, OpenAPI |
| **V. Observability** | ✅ PASS | Will use Serilog with named parameters |
| **VI. Security** | ✅ PASS | All endpoints use [Authorize] attribute |

**Gate Result**: ✅ All checks pass. Proceed to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/021-missing-api-endpoints/
├── plan.md              # This file
├── spec.md              # Feature specification (completed)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (OpenAPI fragments)
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Api/
│   │   └── Controllers/
│   │       ├── AnalyticsController.cs    # Add export endpoint
│   │       └── ReportsController.cs      # Add generate/submit endpoints
│   ├── ExpenseFlow.Core/
│   │   ├── Entities/
│   │   │   └── ExpenseReport.cs          # May need status tracking fields
│   │   └── Interfaces/
│   │       ├── IAnalyticsExportService.cs  # New interface
│   │       └── IReportService.cs           # Add generate/submit methods
│   ├── ExpenseFlow.Infrastructure/
│   │   └── Services/
│   │       ├── AnalyticsExportService.cs   # New service
│   │       └── ReportService.cs            # Add generate/submit logic
│   └── ExpenseFlow.Shared/
│       ├── DTOs/
│       │   ├── AnalyticsExportRequestDto.cs  # New DTO
│       │   └── ReportValidationResultDto.cs  # New DTO
│       └── Enums/
│           └── ReportStatus.cs             # Add Generated, Submitted values
└── tests/
    ├── ExpenseFlow.Api.Tests/
    │   └── Controllers/
    │       ├── AnalyticsControllerTests.cs   # Add export tests
    │       └── ReportsControllerTests.cs     # Add generate/submit tests
    └── ExpenseFlow.Contracts.Tests/
        ├── AnalyticsEndpointContractTests.cs  # Update to actual paths
        ├── ReportEndpointContractTests.cs     # Update to actual paths
        ├── ReceiptEndpointContractTests.cs    # Update to actual paths
        └── TransactionEndpointContractTests.cs # Update to actual paths
```

**Structure Decision**: Extends existing 4-layer Clean Architecture. New files follow established patterns in each layer.

## Complexity Tracking

> No constitution violations requiring justification.

| Change | Justification |
|--------|---------------|
| Add `Generated` and `Submitted` to ReportStatus enum | Minimal change to existing enum; backward compatible |
| New IAnalyticsExportService interface | Single Responsibility: separates export logic from analytics queries |
