# Implementation Plan: Receipt Thumbnail Previews

**Branch**: `030-receipt-thumbnails` | **Date**: 2026-01-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/030-receipt-thumbnails/spec.md`

## Summary

This feature enhances the expense review experience by ensuring receipt thumbnails are consistently generated and displayed across the application. The core infrastructure already exists - this feature focuses on gap analysis, configuration alignment, and adding a backfill job for historical receipts.

**Key Finding**: Most infrastructure is already implemented:
- `Receipt.ThumbnailUrl` property exists
- `ThumbnailService` (Magick.NET) handles images and PDFs
- `HtmlThumbnailService` (PuppeteerSharp) handles HTML receipts
- `ProcessReceiptJob` already generates thumbnails on upload
- `ReceiptCard` frontend component displays thumbnails

**Gaps to Address**:
1. Thumbnail dimensions mismatch (spec says 150x150, code uses 200x200)
2. No background job for backfilling existing receipts without thumbnails
3. No dedicated endpoint to regenerate failed thumbnails
4. Missing explicit cascade delete verification
5. Frontend receipt preview modal enhancement (click-to-zoom)

## Technical Context

**Language/Version**: .NET 8 with C# 12 (backend), TypeScript 5.7+ with React 18.3+ (frontend)
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, Hangfire, Magick.NET (ImageMagick), PuppeteerSharp (Chromium), TanStack Query, shadcn/ui
**Storage**: PostgreSQL 15+ (Supabase self-hosted), Azure Blob Storage (ccproctemp2025)
**Testing**: xUnit, Moq, Playwright (frontend E2E)
**Target Platform**: Azure Kubernetes Service (dev-aks)
**Project Type**: Web application (backend + frontend)
**Performance Goals**:
- Thumbnail generation within 5 seconds for standard images
- Expense list with thumbnails loads within 3 seconds for 100 items
- Full receipt preview loads within 2 seconds
**Constraints**:
- Thumbnail size limit: 150x150 pixels (spec requirement)
- JPEG format at 80% quality for optimal size/quality balance
- Async generation to avoid blocking uploads

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| Clean Architecture (4-layer) | ✅ PASS | Using existing Core/Infrastructure/Api/Shared layers |
| Test-First Development | ✅ PASS | Will add unit tests for new backfill job |
| ERP Integration | ✅ N/A | No Vista ERP interaction |
| RESTful API Design | ✅ PASS | Using existing endpoints pattern |
| Observability | ✅ PASS | Using existing Serilog patterns |
| Security (Entra ID auth) | ✅ PASS | Using existing [Authorize] attributes |

## Project Structure

### Documentation (this feature)

```text
specs/030-receipt-thumbnails/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Technology decisions
├── data-model.md        # Entity extensions (minimal)
├── quickstart.md        # Verification steps
├── contracts/           # API contract updates
│   └── thumbnail-regeneration.yaml
└── tasks.md             # Implementation tasks
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Core/
│   │   ├── Entities/Receipt.cs           # Already has ThumbnailUrl
│   │   └── Interfaces/
│   │       ├── IThumbnailService.cs      # Already exists
│   │       └── IHtmlThumbnailService.cs  # Already exists
│   ├── ExpenseFlow.Infrastructure/
│   │   ├── Services/
│   │   │   ├── ThumbnailService.cs       # Update dimensions to 150x150
│   │   │   └── HtmlThumbnailService.cs   # Update dimensions to 150x150
│   │   └── Jobs/
│   │       ├── ProcessReceiptJob.cs      # Already generates thumbnails
│   │       └── ThumbnailBackfillJob.cs   # NEW: Backfill job
│   └── ExpenseFlow.Api/
│       └── Controllers/
│           └── ReceiptsController.cs     # Add regenerate endpoint
└── tests/
    └── ExpenseFlow.Infrastructure.Tests/
        └── Jobs/ThumbnailBackfillJobTests.cs  # NEW

frontend/
├── src/
│   ├── components/
│   │   └── receipts/
│   │       ├── receipt-card.tsx          # Already displays thumbnails
│   │       └── receipt-preview-modal.tsx # NEW: Full preview with zoom
│   └── hooks/
│       └── queries/use-receipts.ts       # Already has thumbnail support
└── tests/
    └── e2e/thumbnail-display.spec.ts     # NEW: E2E verification
```

**Structure Decision**: Web application structure selected. Backend follows existing Clean Architecture. Frontend follows existing component organization.

## Existing Infrastructure Analysis

### What Already Works

| Component | Location | Status |
|-----------|----------|--------|
| Receipt.ThumbnailUrl | `Core/Entities/Receipt.cs:17` | ✅ Exists |
| IThumbnailService | `Core/Interfaces/IThumbnailService.cs` | ✅ Exists |
| ThumbnailService | `Infrastructure/Services/ThumbnailService.cs` | ✅ Exists (200x200) |
| IHtmlThumbnailService | `Core/Interfaces/IHtmlThumbnailService.cs` | ✅ Exists |
| HtmlThumbnailService | `Infrastructure/Services/HtmlThumbnailService.cs` | ✅ Exists (200x200) |
| ProcessReceiptJob | `Infrastructure/Jobs/ProcessReceiptJob.cs:419-473` | ✅ Generates thumbnails |
| ReceiptCard UI | `frontend/src/components/receipts/receipt-card.tsx:85-100` | ✅ Displays thumbnails |
| API types | `frontend/src/types/api.ts:16` | ✅ Has thumbnailUrl |

### Gaps Requiring Implementation

| Gap | Priority | Effort |
|-----|----------|--------|
| Align thumbnail dimensions to 150x150 | P1 | Low |
| Add ThumbnailBackfillJob for existing receipts | P2 | Medium |
| Add regenerate thumbnail endpoint | P2 | Low |
| Add receipt preview modal with zoom | P1 | Medium |
| Verify cascade delete on receipt deletion | P3 | Low |
| Add E2E test for thumbnail display | P3 | Medium |

## Complexity Tracking

> No constitution violations. Feature primarily leverages existing infrastructure.

## Implementation Phases

### Phase 1: Configuration Alignment
- Update `ThumbnailService` default dimensions from 200 to 150
- Update `HtmlThumbnailService` default dimensions from 200 to 150
- Add configuration option for thumbnail dimensions (appsettings.json)

### Phase 2: Backfill Infrastructure
- Create `ThumbnailBackfillJob` Hangfire job
- Add batch processing with configurable batch size
- Add progress tracking and logging
- Add regenerate endpoint to `ReceiptsController`

### Phase 3: Frontend Enhancement
- Create `ReceiptPreviewModal` component with zoom/pan
- Integrate modal into receipt list views
- Add lazy loading for thumbnails in lists

### Phase 4: Verification
- Add unit tests for backfill job
- Add E2E test for thumbnail display workflow
- Verify cascade delete behavior
