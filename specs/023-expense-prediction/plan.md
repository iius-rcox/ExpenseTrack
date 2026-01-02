# Implementation Plan: Expense Prediction from Historical Reports

**Branch**: `023-expense-prediction` | **Date**: 2026-01-02 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/023-expense-prediction/spec.md`

## Summary

This feature adds personalized expense prediction by analyzing users' approved expense reports to learn which transactions are likely business expenses. When new transactions are imported, the system applies learned patterns to display "Likely Expense" badges with confidence levels, pre-populate expense report drafts, and improve predictions based on user feedback.

**Technical Approach**: Extract vendor/category/amount patterns from approved `ExpenseReport` entities with `ExpenseLine` data, store as `ExpensePattern` entities with exponential decay weighting, and generate `TransactionPrediction` records when transactions are imported. Frontend displays prediction badges with explicit confirm/reject buttons.

## Technical Context

**Language/Version**: .NET 8 with C# 12 (backend), TypeScript 5.7+ with React 18.3+ (frontend)
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, TanStack Query, shadcn/ui
**Storage**: PostgreSQL 15+ (Supabase self-hosted)
**Testing**: xUnit, Vitest, MSW
**Target Platform**: Azure Kubernetes Service (linux/amd64)
**Project Type**: Web application (backend + frontend)
**Performance Goals**: Pattern matching for 1000 transactions in <5 seconds (SC-005)
**Constraints**: Medium+ confidence threshold for display, exponential decay (2x weighting)
**Scale/Scope**: Single-user patterns (no cross-user learning), ~100 patterns per user typical

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture | ✅ Pass | New entities in Core, service in Infrastructure, endpoints in Api |
| II. Test-First Development | ✅ Pass | Unit tests for pattern extraction, integration tests for endpoints |
| III. ERP Integration | ✅ N/A | No Vista integration required for this feature |
| IV. API Design | ✅ Pass | RESTful endpoints under `/api/predictions/`, ProblemDetails errors |
| V. Observability | ✅ Pass | Serilog logging for prediction accuracy metrics (FR-015) |
| VI. Security | ✅ Pass | User-scoped patterns (FR-009), [Authorize] on all endpoints |

**All gates pass. Proceeding to Phase 0.**

## Project Structure

### Documentation (this feature)

```text
specs/023-expense-prediction/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── predictions-api.yaml
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Core/
│   │   ├── Entities/
│   │   │   ├── ExpensePattern.cs        # NEW: Learned expense pattern
│   │   │   ├── TransactionPrediction.cs # NEW: Prediction for transaction
│   │   │   └── PredictionFeedback.cs    # NEW: User feedback record
│   │   └── Interfaces/
│   │       ├── IExpensePredictionService.cs  # NEW
│   │       └── IExpensePatternRepository.cs  # NEW
│   ├── ExpenseFlow.Infrastructure/
│   │   ├── Data/Configurations/
│   │   │   ├── ExpensePatternConfiguration.cs    # NEW
│   │   │   ├── TransactionPredictionConfiguration.cs # NEW
│   │   │   └── PredictionFeedbackConfiguration.cs    # NEW
│   │   ├── Repositories/
│   │   │   └── ExpensePatternRepository.cs  # NEW
│   │   └── Services/
│   │       └── ExpensePredictionService.cs  # NEW
│   ├── ExpenseFlow.Api/
│   │   └── Controllers/
│   │       └── PredictionsController.cs     # NEW
│   └── ExpenseFlow.Shared/
│       └── DTOs/
│           ├── PatternDto.cs                # NEW: Matches OpenAPI PatternDto schema
│           ├── PredictionDto.cs             # NEW: Matches OpenAPI PredictionDto schema
│           └── PredictionFeedbackDto.cs     # NEW
└── tests/
    ├── ExpenseFlow.Core.Tests/
    │   └── Services/
    │       └── ExpensePredictionServiceTests.cs # NEW
    └── ExpenseFlow.Api.Tests/
        └── Controllers/
            └── PredictionsControllerTests.cs    # NEW

frontend/
├── src/
│   ├── components/
│   │   ├── predictions/
│   │   │   ├── expense-badge.tsx          # NEW: "Likely Expense" badge
│   │   │   ├── prediction-feedback.tsx    # NEW: Confirm/reject buttons
│   │   │   └── pattern-dashboard.tsx      # NEW: User Story 4
│   │   └── transactions/
│   │       └── transaction-row.tsx        # MODIFY: Add prediction badge
│   ├── hooks/queries/
│   │   └── use-predictions.ts             # NEW: TanStack Query hooks
│   └── types/
│       └── predictions.ts                 # NEW: TypeScript types
└── tests/
    └── unit/components/predictions/
        ├── expense-badge.test.tsx         # NEW
        └── prediction-feedback.test.tsx   # NEW
```

**Structure Decision**: Web application with full-stack changes. Backend adds new domain entities and service following Clean Architecture. Frontend adds new components and hooks following existing patterns.

## Complexity Tracking

> No constitution violations requiring justification.

---

## Phase 0: Research

See [research.md](./research.md) for:
- Pattern extraction algorithm design
- Exponential decay formula
- Confidence score calculation
- Batch prediction performance optimization

## Phase 1: Design Artifacts

- [data-model.md](./data-model.md) - Entity definitions with EF Core configurations
- [contracts/predictions-api.yaml](./contracts/predictions-api.yaml) - OpenAPI specification
- [quickstart.md](./quickstart.md) - Local development setup
