# Implementation Plan: Extraction Editor with Model Training

**Branch**: `024-extraction-editor-training` | **Date**: 2026-01-03 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/024-extraction-editor-training/spec.md`

## Summary

Enable users to edit AI-extracted receipt fields and capture corrections as training feedback for model improvement. The feature builds on existing `ReceiptIntelligencePanel` and `ExtractedField` components, adding backend persistence for corrections via a new `ExtractionCorrection` entity. The PUT `/api/receipts/{id}` endpoint already exists for updates; this feature extends it to record training feedback and adds a new endpoint for feedback history.

## Technical Context

**Language/Version**:
- Backend: .NET 8 with C# 12
- Frontend: TypeScript 5.7+ with React 18.3+

**Primary Dependencies**:
- Backend: ASP.NET Core Web API, Entity Framework Core 8, FluentValidation
- Frontend: TanStack Query, TanStack Router, shadcn/ui, Framer Motion

**Storage**: PostgreSQL 15+ (Supabase)

**Testing**:
- Backend: xUnit, FluentAssertions
- Frontend: Vitest, Playwright

**Target Platform**: Web application (Azure Kubernetes Service)

**Project Type**: Web application (backend + frontend)

**Performance Goals**:
- Field edit + save: <10 seconds (SC-001)
- Side-by-side view load: <2 seconds (SC-005)
- 100 concurrent editing sessions (SC-007)

**Constraints**:
- Optimistic concurrency for simultaneous edits
- Training feedback retained indefinitely
- Edit existing line items only (no add/delete)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| Clean Architecture | ✅ Pass | New entity in Core, service in Infrastructure, controller in Api |
| Test-First Development | ✅ Pass | Unit tests for feedback service, integration tests for API |
| ERP Integration | ⬜ N/A | No Vista data involved in this feature |
| API Design | ✅ Pass | RESTful endpoints, ProblemDetails errors, FluentValidation |
| Observability | ✅ Pass | Serilog logging for corrections, structured log templates |
| Security | ✅ Pass | [Authorize] on controllers, JWT bearer tokens |

**Gate Result**: PASS - No violations detected.

## Project Structure

### Documentation (this feature)

```text
specs/024-extraction-editor-training/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (OpenAPI)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Api/
│   │   └── Controllers/
│   │       └── ExtractionCorrectionsController.cs  # NEW: Feedback history endpoint
│   ├── ExpenseFlow.Core/
│   │   ├── Entities/
│   │   │   └── ExtractionCorrection.cs             # NEW: Training feedback entity
│   │   └── Interfaces/
│   │       └── IExtractionCorrectionService.cs     # NEW: Service interface
│   ├── ExpenseFlow.Infrastructure/
│   │   ├── Data/
│   │   │   ├── Configurations/
│   │   │   │   └── ExtractionCorrectionConfiguration.cs  # NEW: EF configuration
│   │   │   └── Migrations/
│   │   │       └── 20260104000000_AddExtractionCorrections.cs  # NEW
│   │   └── Services/
│   │       └── ExtractionCorrectionService.cs      # NEW: Service implementation
│   └── ExpenseFlow.Shared/
│       └── DTOs/
│           └── ExtractionCorrectionDtos.cs         # NEW: DTOs for corrections
└── tests/
    └── ExpenseFlow.Infrastructure.Tests/
        └── Services/
            └── ExtractionCorrectionServiceTests.cs # NEW: Unit tests

frontend/
├── src/
│   ├── components/
│   │   └── receipts/
│   │       ├── extracted-field.tsx                 # UPDATE: Add save callback
│   │       └── receipt-intelligence-panel.tsx      # UPDATE: Wire to API
│   ├── hooks/
│   │   └── queries/
│   │       └── use-receipts.ts                     # UPDATE: Add correction mutations
│   └── types/
│       └── receipt.ts                              # UPDATE: Add correction types
└── tests/
    └── unit/
        └── components/
            └── receipts/
                └── receipt-intelligence-panel.test.tsx  # UPDATE: Add tests
```

**Structure Decision**: Web application (backend + frontend) following existing Clean Architecture pattern. New entity `ExtractionCorrection` in Core layer with service implementation in Infrastructure.

## Complexity Tracking

> No violations to justify - all changes align with existing architecture.

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| New Entity | ExtractionCorrection | Follows existing entity pattern (PredictionFeedback) |
| New Controller | ExtractionCorrectionsController | Separates feedback history from receipt CRUD |
| Frontend Changes | Minimal | Existing components already support editing; wire to API |
