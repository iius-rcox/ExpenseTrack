# Tasks: Receipt Thumbnail Previews

**Input**: Design documents from `/specs/030-receipt-thumbnails/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/thumbnail-endpoints.yaml

**Key Finding**: Most infrastructure already exists. This feature focuses on configuration alignment, frontend enhancement, and adding a backfill job.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Backend**: `backend/src/ExpenseFlow.*`
- **Frontend**: `frontend/src/*`
- **Tests**: `backend/tests/*`, `frontend/tests/*`

---

## Phase 1: Setup (Configuration Alignment)

**Purpose**: Align existing services with spec requirements (150x150 instead of 200x200)

- [x] T001 [P] Add thumbnail dimension configuration to `backend/src/ExpenseFlow.Api/appsettings.json` under ReceiptProcessing:Thumbnail
- [x] T002 [P] Update ThumbnailService to read dimensions from config in `backend/src/ExpenseFlow.Infrastructure/Services/ThumbnailService.cs`
- [x] T003 [P] Update HtmlThumbnailService to read dimensions from config in `backend/src/ExpenseFlow.Infrastructure/Services/HtmlThumbnailService.cs`
- [x] T004 Update Receipt.ThumbnailUrl comment from "200x200" to "150x150" in `backend/src/ExpenseFlow.Core/Entities/Receipt.cs:17`

**Checkpoint**: Thumbnail services now use configurable 150x150 dimensions

---

## Phase 2: Foundational (Shared DTOs and Repository Methods)

**Purpose**: Create shared infrastructure needed by multiple user stories

**⚠️ CRITICAL**: User Story 5 (backfill) requires these foundational pieces

- [x] T005 [P] Create ThumbnailBackfillRequest DTO in `backend/src/ExpenseFlow.Shared/DTOs/ThumbnailBackfillDtos.cs`
- [x] T006 [P] Create ThumbnailBackfillResponse DTO in `backend/src/ExpenseFlow.Shared/DTOs/ThumbnailBackfillDtos.cs`
- [x] T007 [P] Create ThumbnailBackfillStatus DTO in `backend/src/ExpenseFlow.Shared/DTOs/ThumbnailBackfillDtos.cs`
- [x] T008 [P] Create ThumbnailRegenerationResponse DTO in `backend/src/ExpenseFlow.Shared/DTOs/ThumbnailBackfillDtos.cs`
- [x] T009 Add GetReceiptsWithoutThumbnailsAsync method to IReceiptRepository in `backend/src/ExpenseFlow.Core/Interfaces/IReceiptRepository.cs`
- [x] T010 Implement GetReceiptsWithoutThumbnailsAsync in ReceiptRepository in `backend/src/ExpenseFlow.Infrastructure/Repositories/ReceiptRepository.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 & 2 - View Thumbnails & Preview (Priority: P1) 🎯 MVP

**Goal**: Display thumbnails in expense list and enable click-to-preview with zoom/pan

**Independent Test**: Upload a receipt, verify thumbnail displays in list, click to open preview modal with zoom controls

**Note**: US1 and US2 are combined because they share the same frontend component (preview modal) and are both P1 priority.

### Implementation for User Stories 1 & 2

- [x] T011 [P] [US1] Install react-zoom-pan-pinch package via `cd frontend && pnpm add react-zoom-pan-pinch`
- [x] T012 [P] [US1] Add loading="lazy" attribute to thumbnail img in `frontend/src/components/receipts/receipt-card.tsx:87`
- [x] T013 [US2] Create ReceiptPreviewModal component with zoom/pan in `frontend/src/components/receipts/receipt-preview-modal.tsx`
- [x] T014 [US2] Add preview modal state and handlers to ReceiptCard in `frontend/src/components/receipts/receipt-card.tsx`
- [x] T015 [US2] Add keyboard navigation (Escape to close, +/- for zoom) to ReceiptPreviewModal in `frontend/src/components/receipts/receipt-preview-modal.tsx`
- [x] T016 [US1] Add placeholder icon variants for different file types in `frontend/src/components/receipts/receipt-card.tsx`
- [x] T017 [US1] Export ReceiptPreviewModal from receipts index in `frontend/src/components/receipts/index.ts`

**Checkpoint**: Users can view thumbnails and click to preview with zoom - MVP complete

---

## Phase 4: User Story 3 - PDF Thumbnail Generation (Priority: P2)

**Goal**: Ensure PDF receipts generate proper first-page thumbnails

**Independent Test**: Upload a multi-page PDF, verify thumbnail shows first page content

**Note**: ThumbnailService already handles PDFs and images (JPEG, PNG, HEIC, WebP) via Magick.NET. This phase focuses on PDF edge cases; image support (FR-002) is covered by existing infrastructure.

### Implementation for User Story 3

- [x] T018 [US3] Add password-protected PDF detection in ThumbnailService in `backend/src/ExpenseFlow.Infrastructure/Services/ThumbnailService.cs`
- [x] T019 [US3] Return null/fallback for password-protected PDFs with logging in `backend/src/ExpenseFlow.Infrastructure/Services/ThumbnailService.cs`
- [x] T020 [P] [US3] Add PDF-specific fallback icon to frontend in `frontend/src/components/receipts/receipt-card.tsx`
- [x] T021 [US3] Add unit test for password-protected PDF handling in `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/ThumbnailServiceTests.cs`

**Checkpoint**: PDF thumbnails work correctly including edge cases

---

## Phase 5: User Story 4 - HTML Thumbnail Generation (Priority: P2)

**Goal**: Ensure HTML email receipts generate visual thumbnails via headless browser

**Independent Test**: Upload an HTML receipt file, verify thumbnail shows rendered HTML content

**Note**: HtmlThumbnailService already exists via PuppeteerSharp. This phase is verification and fallback handling.

### Implementation for User Story 4

- [x] T022 [US4] Add graceful fallback when Chromium unavailable in HtmlThumbnailService in `backend/src/ExpenseFlow.Infrastructure/Services/HtmlThumbnailService.cs`
- [x] T023 [P] [US4] Add HTML-specific fallback icon to frontend in `frontend/src/components/receipts/receipt-card.tsx`
- [x] T024 [US4] Verify HTML sanitization before rendering in ProcessReceiptJob in `backend/src/ExpenseFlow.Infrastructure/Jobs/ProcessReceiptJob.cs`
- [x] T025 [US4] Add logging for HTML thumbnail generation success/failure in `backend/src/ExpenseFlow.Infrastructure/Services/HtmlThumbnailService.cs`

**Checkpoint**: HTML thumbnails work correctly with proper fallbacks

---

## Phase 6: User Story 5 - Thumbnail Backfill for Existing Receipts (Priority: P3)

**Goal**: Generate thumbnails for historical receipts that don't have them

**Independent Test**: Run backfill job, verify previously uploaded receipts gain thumbnails

### Implementation for User Story 5

- [x] T026 [US5] Create IThumbnailBackfillService interface in `backend/src/ExpenseFlow.Core/Interfaces/IThumbnailBackfillService.cs`
- [x] T027 [US5] Create ThumbnailBackfillJob Hangfire job in `backend/src/ExpenseFlow.Infrastructure/Jobs/ThumbnailBackfillJob.cs`
- [x] T028 [US5] Implement batch processing with configurable batch size in ThumbnailBackfillJob in `backend/src/ExpenseFlow.Infrastructure/Jobs/ThumbnailBackfillJob.cs`
- [x] T029 [US5] Add exponential backoff retry logic (1min, 5min, 30min) to ThumbnailBackfillJob in `backend/src/ExpenseFlow.Infrastructure/Jobs/ThumbnailBackfillJob.cs`
- [x] T030 [US5] Add progress tracking and structured logging to ThumbnailBackfillJob in `backend/src/ExpenseFlow.Infrastructure/Jobs/ThumbnailBackfillJob.cs`
- [x] T031 [US5] Create ThumbnailsController for admin endpoints in `backend/src/ExpenseFlow.Api/Controllers/ThumbnailsController.cs`
- [x] T032 [US5] Implement POST /api/admin/thumbnails/backfill endpoint in `backend/src/ExpenseFlow.Api/Controllers/ThumbnailsController.cs`
- [x] T033 [US5] Implement GET /api/admin/thumbnails/backfill/status endpoint in `backend/src/ExpenseFlow.Api/Controllers/ThumbnailsController.cs`
- [x] T034 [US5] Add POST /api/receipts/{id}/regenerate-thumbnail endpoint to ReceiptsController in `backend/src/ExpenseFlow.Api/Controllers/ReceiptsController.cs`
- [x] T035 [US5] Register ThumbnailBackfillJob in DI container in `backend/src/ExpenseFlow.Infrastructure/Extensions/ServiceCollectionExtensions.cs`
- [x] T036 [US5] Add unit tests for ThumbnailBackfillJob in `backend/tests/ExpenseFlow.Infrastructure.Tests/Jobs/ThumbnailBackfillJobTests.cs`

**Checkpoint**: Historical receipts can be backfilled with thumbnails via admin endpoint

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Verification, documentation, and cleanup

- [x] T037 [P] Verify cascade delete behavior when receipt is deleted in `backend/src/ExpenseFlow.Infrastructure/Services/BlobStorageService.cs`
- [x] T038 [P] Update OpenAPI documentation for new endpoints via build
- [x] T039 [P] Add E2E test for thumbnail display in `frontend/tests/e2e/thumbnail-display.spec.ts`
- [x] T040 Run quickstart.md validation steps to verify feature works end-to-end
- [x] T041 Update CLAUDE.md timestamp to reflect completion date

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies - can start immediately
- **Phase 2 (Foundational)**: No dependencies - can run in parallel with Phase 1
- **Phase 3 (US1 & US2)**: Depends on Phase 1 only - frontend work
- **Phase 4 (US3)**: Depends on Phase 1 - backend PDF edge cases
- **Phase 5 (US4)**: Depends on Phase 1 - backend HTML edge cases
- **Phase 6 (US5)**: Depends on Phase 2 - uses DTOs and repository methods
- **Phase 7 (Polish)**: Depends on all user stories

### User Story Dependencies

- **User Stories 1 & 2 (P1)**: Frontend-only, can start after Phase 1
- **User Story 3 (P2)**: Backend PDF handling, can start after Phase 1
- **User Story 4 (P2)**: Backend HTML handling, can start after Phase 1
- **User Story 5 (P3)**: Requires Phase 2 completion

### Parallel Opportunities

```text
After Phase 1 completes, these can run in parallel:
├── Phase 3 (US1 & US2) - Frontend team
├── Phase 4 (US3) - Backend team
└── Phase 5 (US4) - Backend team

After Phase 2 completes:
└── Phase 6 (US5) - Backend team (can overlap with Phase 3-5)
```

---

## Parallel Example: Phase 1 (Setup)

```bash
# All Phase 1 tasks can run in parallel:
Task: "Add thumbnail dimension configuration to appsettings.json"
Task: "Update ThumbnailService to read dimensions from config"
Task: "Update HtmlThumbnailService to read dimensions from config"
Task: "Update Receipt.ThumbnailUrl comment"
```

## Parallel Example: Phase 3 (US1 & US2)

```bash
# These can run in parallel:
Task: "Install react-zoom-pan-pinch package"
Task: "Add loading='lazy' attribute to thumbnail img"

# Then these in parallel:
Task: "Add placeholder icon variants for different file types"
Task: "Add PDF-specific fallback icon to frontend"
Task: "Add HTML-specific fallback icon to frontend"
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2 Only)

1. Complete Phase 1: Setup (config alignment)
2. Complete Phase 3: User Stories 1 & 2 (preview modal)
3. **STOP and VALIDATE**: Test thumbnail display and preview modal
4. Deploy/demo if ready - users get immediate value

### Incremental Delivery

1. Phase 1 + Phase 2 → Config aligned + DTOs ready (can run in parallel)
2. Phase 3 (US1 & US2) → Thumbnail preview works → **Deploy (MVP)**
3. Phase 4 (US3) → PDF edge cases handled → Deploy
4. Phase 5 (US4) → HTML edge cases handled → Deploy
5. Phase 6 (US5) → Backfill capability → Deploy
6. Phase 7 → Polish complete → Final release

### Parallel Team Strategy

With multiple developers:

| Developer | Phase 1 | After Phase 1 |
|-----------|---------|---------------|
| Frontend Dev | T001-T004 (help) | Phase 3 (US1 & US2) |
| Backend Dev A | T001-T004 | Phase 4 (US3) + Phase 5 (US4) |
| Backend Dev B | Phase 2 | Phase 6 (US5) |

---

## Task Summary

| Phase | Task Count | User Stories | Parallel Tasks |
|-------|------------|--------------|----------------|
| Phase 1 (Setup) | 4 | - | 4 |
| Phase 2 (Foundational) | 6 | - | 4 |
| Phase 3 (US1 & US2) | 7 | US1, US2 | 3 |
| Phase 4 (US3) | 4 | US3 | 1 |
| Phase 5 (US4) | 4 | US4 | 1 |
| Phase 6 (US5) | 11 | US5 | 0 |
| Phase 7 (Polish) | 5 | - | 3 |
| **Total** | **41** | **5 stories** | **16 parallel** |

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [Story] label maps task to specific user story for traceability
- Most infrastructure already exists - focus on gaps identified in plan.md
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Test ThumbnailService locally before deploying config changes
