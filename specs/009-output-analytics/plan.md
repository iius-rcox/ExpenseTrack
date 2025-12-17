# Implementation Plan: Output Generation & Analytics

**Branch**: `009-output-analytics` | **Date**: 2025-12-16 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/009-output-analytics/spec.md`

## Summary

Generate Excel expense reports matching the AP department template format, consolidate receipts into a single PDF with placeholder pages for missing receipts, provide month-over-month spending comparison with anomaly detection, and expose cache tier usage statistics for cost monitoring. All features operate on existing data from Sprints 6-8 with no new database entities required.

## Technical Context

**Language/Version**: .NET 8 with C# 12 (ASP.NET Core Web API)
**Primary Dependencies**: Entity Framework Core 8, Npgsql, ClosedXML (Excel), PdfSharpCore (PDF), SixLabors.ImageSharp (image conversion)
**Storage**: PostgreSQL 15+ with pgvector (Supabase self-hosted), Azure Blob Storage (ccproctemp2025)
**Testing**: xUnit, Moq, FluentAssertions
**Target Platform**: Azure Kubernetes Service (dev-aks)
**Project Type**: Web application (backend API + future React frontend)
**Performance Goals**: Excel export <5 seconds, PDF generation <30 seconds for 50 items
**Constraints**: PDF <20MB for typical reports; receipt limit of 100 per PDF
**Scale/Scope**: 10-20 users, ~50 expenses/user/month typical

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. Cost-First AI Architecture | **PASS** | No AI operations in Sprint 9; pure data transformation/export |
| II. Self-Improving System | **PASS** | N/A - read-only output operations |
| III. Receipt Accountability | **PASS** | Missing receipt placeholders with required justification (FR-006, FR-007, FR-008) |
| IV. Infrastructure Optimization | **PASS** | Uses existing AKS infrastructure; no new managed services |
| V. Cache-First Design | **PASS** | Cache statistics dashboard monitors Tier 1 hit rates (FR-013, FR-014) |

**Gate Result**: All principles satisfied. Proceeding to implementation.

## Project Structure

### Documentation (this feature)

```text
specs/009-output-analytics/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── output-api.yaml  # OpenAPI contract
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Api/
│   │   └── Controllers/
│   │       ├── ReportsController.cs           # Extended: Export endpoints
│   │       └── AnalyticsController.cs         # NEW: Comparison/stats endpoints
│   ├── ExpenseFlow.Core/
│   │   └── Interfaces/
│   │       ├── IExcelExportService.cs         # NEW: Excel generation interface
│   │       ├── IPdfGenerationService.cs       # NEW: PDF generation interface
│   │       ├── IComparisonService.cs          # NEW: MoM comparison interface
│   │       └── ICacheStatisticsService.cs     # NEW: Cache stats interface
│   ├── ExpenseFlow.Infrastructure/
│   │   └── Services/
│   │       ├── ExcelExportService.cs          # NEW: ClosedXML implementation
│   │       ├── PdfGenerationService.cs        # NEW: PdfSharpCore implementation
│   │       ├── MonthlyComparisonService.cs    # NEW: MoM calculation
│   │       └── CacheStatisticsService.cs      # NEW: TierUsageLog aggregation
│   └── ExpenseFlow.Shared/
│       └── DTOs/
│           ├── MonthlyComparisonDto.cs        # NEW: Comparison response
│           ├── CacheStatisticsDto.cs          # NEW: Stats response
│           └── VendorChangeDto.cs             # NEW: Vendor change tracking
└── tests/
    └── ExpenseFlow.Infrastructure.Tests/
        └── Services/
            ├── ExcelExportServiceTests.cs     # NEW: Export unit tests
            ├── PdfGenerationServiceTests.cs   # NEW: PDF unit tests
            ├── MonthlyComparisonServiceTests.cs # NEW: Comparison tests
            └── CacheStatisticsServiceTests.cs # NEW: Stats tests
```

**Structure Decision**: Follows existing Clean Architecture pattern with ExpenseFlow.Core (interfaces), ExpenseFlow.Infrastructure (implementations), ExpenseFlow.Api (controllers), and ExpenseFlow.Shared (DTOs). New services are scoped to this feature with no cross-cutting concerns.

## Complexity Tracking

No violations to justify. Feature aligns with existing patterns and constitution principles.

## Phase Summary

### Phase 0: Research (Complete)

See [research.md](./research.md) for:
- Excel library selection: ClosedXML (formula preservation, MIT license)
- PDF library selection: PdfSharpCore + ImageSharp (cross-platform, MIT license)
- MoM comparison algorithm: SQL-based with CTEs
- Cache statistics aggregation: TierUsageLog queries

### Phase 1: Design (Complete)

See [data-model.md](./data-model.md) for:
- No new database entities (uses existing from Sprints 6-8)
- DTO definitions for export and analytics responses
- Query patterns for MoM comparison and cache statistics

See [contracts/output-api.yaml](./contracts/output-api.yaml) for:
- `GET /api/reports/{reportId}/excel` - Excel export
- `GET /api/reports/{reportId}/receipts.pdf` - PDF export
- `GET /api/analytics/comparison` - MoM comparison
- `GET /api/analytics/cache-stats` - Cache statistics

See [quickstart.md](./quickstart.md) for:
- NuGet package installation
- Service registration
- Implementation patterns
- Testing commands

### Phase 2: Tasks (Next Step)

Run `/speckit.tasks` to generate detailed implementation tasks.

## Key Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| ClosedXML | 0.102.x | Excel generation with formula preservation |
| PdfSharpCore | 1.3.x | PDF generation and merging |
| SixLabors.ImageSharp | 3.1.x | Image to PDF conversion |

All packages are MIT licensed and compatible with .NET 8.

## Risk Mitigations

| Risk | Mitigation |
|------|------------|
| Large PDF memory usage | Streaming generation, 100-receipt limit |
| Excel template drift | Store template in blob storage with versioning |
| MoM query performance | Indexed columns, single user context |
| Formula preservation | Use template-based approach, not formula generation |

## Next Steps

1. Run `/speckit.tasks` to generate implementation tasks
2. Install NuGet packages per quickstart.md
3. Upload AP template to blob storage
4. Implement services in priority order (Excel → PDF → MoM → Stats)
