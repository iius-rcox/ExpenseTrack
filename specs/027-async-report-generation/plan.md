# Implementation Plan: Async Report Generation

**Branch**: `027-async-report-generation` | **Date**: 2026-01-05 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/027-async-report-generation/spec.md`

## Summary

Convert synchronous expense report generation (currently 2-3+ minutes) to background job processing with real-time progress tracking. Uses existing Hangfire infrastructure for job management, adds polling-based progress updates, implements exponential backoff for OpenAI rate limiting, and includes nightly cache warming for vendor categorizations.

## Technical Context

**Language/Version**: .NET 8 with C# 12
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, Hangfire, Microsoft.Extensions.Resilience (Polly v8), TanStack Query (frontend polling)
**Storage**: PostgreSQL 15+ (Supabase) for job state; existing transaction/receipt tables
**Testing**: xUnit, Moq, Testcontainers for integration tests
**Target Platform**: Azure Kubernetes Service (linux/amd64)
**Project Type**: Web application (backend API + React frontend)
**Performance Goals**:
- Immediate response (<2s) when initiating report generation
- Progress updates visible within 5 seconds
- 300 lines processed in 5-10 minutes depending on rate limiting
**Constraints**:
- Must work with existing Hangfire setup
- Must handle OpenAI rate limiting (HTTP 429)
- Frontend polling only (no WebSocket for MVP)
**Scale/Scope**: Single user per job; ~300 expense lines typical; 30-day job history retention

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture | ✅ Pass | New entities in Core, services in Infrastructure, endpoints in Api |
| II. Test-First Development | ✅ Pass | Unit tests for job service, integration tests for endpoints |
| III. ERP Integration | ✅ N/A | No Vista changes required |
| IV. API Design | ✅ Pass | RESTful endpoints, ProblemDetails errors, FluentValidation |
| V. Observability | ✅ Pass | Serilog logging for job progress and failures |
| VI. Security | ✅ Pass | [Authorize] on all new endpoints, user-scoped jobs |

**Gate Status**: ✅ PASSED - No violations requiring justification

## Project Structure

### Documentation (this feature)

```text
specs/027-async-report-generation/
├── spec.md              # Feature specification (complete)
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (OpenAPI specs)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Core/
│   │   └── Entities/
│   │       └── ReportGenerationJob.cs          # New entity
│   ├── ExpenseFlow.Infrastructure/
│   │   ├── Jobs/
│   │   │   ├── ReportGenerationBackgroundJob.cs # Hangfire job
│   │   │   └── CacheWarmingJob.cs              # Nightly cache warming
│   │   ├── Services/
│   │   │   └── ReportJobService.cs             # Job management service
│   │   ├── Repositories/
│   │   │   └── ReportJobRepository.cs          # Job persistence
│   │   └── Data/
│   │       ├── Configurations/
│   │       │   └── ReportGenerationJobConfiguration.cs
│   │       └── Migrations/
│   │           └── YYYYMMDD_AddReportGenerationJobs.cs
│   └── ExpenseFlow.Api/
│       ├── Controllers/
│       │   └── ReportJobsController.cs         # Job status endpoints
│       └── Validators/
│           └── GenerateReportRequestValidator.cs
└── tests/
    ├── ExpenseFlow.Infrastructure.Tests/
    │   └── Jobs/
    │       └── ReportGenerationBackgroundJobTests.cs
    └── ExpenseFlow.Api.Tests/
        └── Controllers/
            └── ReportJobsControllerTests.cs

frontend/
├── src/
│   ├── hooks/
│   │   └── queries/
│   │       └── use-report-jobs.ts              # Job polling hook
│   └── components/
│       └── reports/
│           ├── report-generation-progress.tsx   # Progress UI
│           └── job-history-panel.tsx           # History view
└── tests/
    └── hooks/
        └── use-report-jobs.test.ts
```

**Structure Decision**: Follows existing Clean Architecture layout. New Hangfire job classes go in `Infrastructure/Jobs/` alongside existing jobs. New entity follows existing pattern in `Core/Entities/`.

## Complexity Tracking

> No Constitution Check violations - this section is not required.
