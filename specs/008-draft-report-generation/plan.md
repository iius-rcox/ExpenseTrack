# Implementation Plan: Draft Report Generation

**Branch**: `008-draft-report-generation` | **Date**: 2025-12-16 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/008-draft-report-generation/spec.md`

## Summary

Auto-generate draft expense reports from matched receipts and transactions. The system compiles all confirmed receipt-transaction matches for a user's selected period, pre-populates GL codes and departments using the existing tiered categorization system (Tier 1 cache → Tier 2 embeddings → Tier 3 AI), normalizes descriptions, and flags transactions missing receipts. Users can review, edit categorizations, and provide justifications for missing receipts. All user corrections feed back into the learning system (vendor aliases, embeddings, description cache).

## Technical Context

**Language/Version**: .NET 8 with C# 12 (ASP.NET Core Web API)
**Primary Dependencies**: Entity Framework Core 8, Npgsql, Semantic Kernel, Hangfire
**Storage**: PostgreSQL 15+ with pgvector (Supabase self-hosted), Azure Blob Storage
**Testing**: xUnit, Moq, FluentAssertions
**Target Platform**: Azure Kubernetes Service (dev-aks)
**Project Type**: Web application (backend API + future React frontend)
**Performance Goals**: Draft generation <30 seconds for 50 expenses; report edit saves <500ms
**Constraints**: <$20/month AI costs at steady state; tiered categorization MUST follow cost hierarchy
**Scale/Scope**: 10-20 users, ~50 expenses/user/month typical

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. Cost-First AI Architecture | **PASS** | Uses existing tiered categorization (Tier 1→2→3); logs tier usage per suggestion |
| II. Self-Improving System | **PASS** | User edits update VendorAliases, create ExpenseEmbeddings, populate DescriptionCache |
| III. Receipt Accountability | **PASS** | Flags missing receipts with required justification; placeholder support for Sprint 9 PDF |
| IV. Infrastructure Optimization | **PASS** | Uses existing AKS infrastructure; no new Azure managed services |
| V. Cache-First Design | **PASS** | GL/dept suggestions check cache first via ICategorizationService |

**Gate Result**: All principles satisfied. Proceeding to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/008-draft-report-generation/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── reports-api.yaml # OpenAPI contract
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Api/
│   │   └── Controllers/
│   │       └── ReportsController.cs           # New: Report endpoints
│   ├── ExpenseFlow.Core/
│   │   ├── Entities/
│   │   │   ├── ExpenseReport.cs               # New: Report entity
│   │   │   └── ExpenseLine.cs                 # New: Line item entity
│   │   └── Interfaces/
│   │       ├── IReportService.cs              # New: Report generation service
│   │       └── IExpenseReportRepository.cs    # New: Repository interface
│   ├── ExpenseFlow.Infrastructure/
│   │   ├── Data/
│   │   │   └── Configurations/
│   │   │       ├── ExpenseReportConfiguration.cs  # New: EF config
│   │   │       └── ExpenseLineConfiguration.cs    # New: EF config
│   │   ├── Repositories/
│   │   │   └── ExpenseReportRepository.cs     # New: Repository impl
│   │   └── Services/
│   │       └── ReportService.cs               # New: Report generation logic
│   └── ExpenseFlow.Shared/
│       ├── DTOs/
│       │   ├── ExpenseReportDto.cs            # New: Report DTOs
│       │   ├── ExpenseLineDto.cs              # New: Line DTOs
│       │   └── ReportSummaryDto.cs            # New: Summary stats
│       └── Enums/
│           ├── ReportStatus.cs                # New: Draft/Final (Draft only now)
│           └── MissingReceiptJustification.cs # New: Justification enum
└── tests/
    └── ExpenseFlow.Infrastructure.Tests/
        └── Services/
            └── ReportServiceTests.cs          # New: Unit tests
```

**Structure Decision**: Follows existing Clean Architecture pattern with ExpenseFlow.Core (entities/interfaces), ExpenseFlow.Infrastructure (implementations), ExpenseFlow.Api (controllers), and ExpenseFlow.Shared (DTOs/enums).

## Complexity Tracking

No violations to justify. Feature aligns with existing patterns and constitution principles.
