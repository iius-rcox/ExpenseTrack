# Implementation Plan: Missing Receipts UI

**Branch**: `026-missing-receipts-ui` | **Date**: 2026-01-05 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/026-missing-receipts-ui/spec.md`

## Summary

Create a UI for identifying and managing transactions that are predicted/confirmed as reimbursable but lack matching receipts. The feature includes:
1. A summary widget on the Matching page (`/matching`) showing count and top 3 missing receipts with quick upload
2. A dedicated full list page (`/missing-receipts`) with pagination (25/page), sorting, and URL management
3. Backend computed query joining Transactions + TransactionPredictions (user override precedence)
4. Transaction entity extended with `ReceiptUrl` and `ReceiptDismissed` fields

## Technical Context

**Language/Version**: .NET 8 with C# 12 (backend), TypeScript 5.7+ with React 18.3+ (frontend)
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, TanStack Router, TanStack Query, shadcn/ui
**Storage**: PostgreSQL 15+ (Supabase self-hosted) - extends existing Transaction table
**Testing**: xUnit (backend), Vitest + Playwright (frontend)
**Target Platform**: Azure Kubernetes Service (linux/amd64), Web Browser
**Project Type**: Web application (backend + frontend)
**Performance Goals**: <2s page load, support 20-100 missing receipts per user
**Constraints**: Pagination at 25 items, computed view (not materialized)
**Scale/Scope**: Medium scale per user, single-tenant per user query

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture | ✅ PASS | Core entity extension, Infrastructure service, API controller, Shared DTOs |
| II. Test-First Development | ✅ PASS | Unit tests for service logic, integration tests for endpoints |
| III. ERP Integration | N/A | No Vista integration needed for this feature |
| IV. API Design | ✅ PASS | RESTful endpoints, ProblemDetails errors, FluentValidation |
| V. Observability | ✅ PASS | Serilog structured logging for all operations |
| VI. Security | ✅ PASS | User-scoped queries, [Authorize] on all endpoints |

**Gate Result**: ✅ All applicable gates pass

## Project Structure

### Documentation (this feature)

```text
specs/026-missing-receipts-ui/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── missing-receipts-api.yaml
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Core/
│   │   └── Entities/
│   │       └── Transaction.cs           # Extended with ReceiptUrl, ReceiptDismissed
│   ├── ExpenseFlow.Shared/
│   │   └── DTOs/
│   │       └── MissingReceiptDtos.cs    # New DTOs for missing receipts
│   ├── ExpenseFlow.Infrastructure/
│   │   └── Services/
│   │       └── MissingReceiptService.cs # New service for computed queries
│   └── ExpenseFlow.Api/
│       └── Controllers/
│           └── MissingReceiptsController.cs # New REST endpoints
└── tests/
    ├── ExpenseFlow.Infrastructure.Tests/
    │   └── Services/
    │       └── MissingReceiptServiceTests.cs
    └── ExpenseFlow.Api.Tests/
        └── Controllers/
            └── MissingReceiptsControllerTests.cs

frontend/
├── src/
│   ├── components/
│   │   └── missing-receipts/
│   │       ├── missing-receipts-widget.tsx     # Summary widget for /matching
│   │       ├── missing-receipt-card.tsx        # Individual item card
│   │       ├── receipt-url-dialog.tsx          # Add/edit URL dialog
│   │       └── dismiss-confirm-dialog.tsx      # Dismiss confirmation
│   ├── routes/
│   │   └── _authenticated/
│   │       └── missing-receipts/
│   │           └── index.tsx                   # Full list page
│   └── hooks/
│       └── queries/
│           └── use-missing-receipts.ts         # TanStack Query hooks
└── tests/
    └── components/
        └── missing-receipts/
            └── missing-receipts.test.tsx
```

**Structure Decision**: Web application with separate backend (ASP.NET Core) and frontend (React). This follows the existing ExpenseFlow architecture pattern.

## Complexity Tracking

No constitution violations requiring justification.
