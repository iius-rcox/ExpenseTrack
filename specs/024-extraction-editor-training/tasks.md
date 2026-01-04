# Tasks: Extraction Editor with Model Training

**Input**: Design documents from `/specs/024-extraction-editor-training/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Tests included as per project constitution (Test-First Development principle).

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4, US5)
- Exact file paths included in descriptions

## Path Conventions

- **Backend**: `backend/src/` (ExpenseFlow.Api, ExpenseFlow.Core, ExpenseFlow.Infrastructure, ExpenseFlow.Shared)
- **Frontend**: `frontend/src/`
- **Tests**: `backend/tests/`, `frontend/tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and branch setup

- [x] T001 Create feature branch `024-extraction-editor-training` from main
- [x] T002 [P] Verify existing components exist: `frontend/src/components/receipts/extracted-field.tsx`
- [x] T003 [P] Verify existing components exist: `frontend/src/components/design-system/confidence-indicator.tsx`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Backend infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Entity & Database

- [x] T004 Create ExtractionCorrection entity in `backend/src/ExpenseFlow.Core/Entities/ExtractionCorrection.cs` per data-model.md
- [x] T005 Create EF configuration in `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/ExtractionCorrectionConfiguration.cs` per data-model.md
- [x] T006 Register DbSet in `backend/src/ExpenseFlow.Infrastructure/Data/ExpenseFlowDbContext.cs`
- [x] T007 Add RowVersion property to Receipt entity in `backend/src/ExpenseFlow.Core/Entities/Receipt.cs` for optimistic concurrency
- [x] T008 Update ReceiptConfiguration with xmin concurrency token in `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/ReceiptConfiguration.cs`
- [x] T009 Generate EF migration `AddExtractionCorrections` in `backend/src/ExpenseFlow.Infrastructure/Data/Migrations/`

### DTOs

- [x] T010 [P] Create ExtractionCorrectionDtos in `backend/src/ExpenseFlow.Shared/DTOs/ExtractionCorrectionDtos.cs` (CorrectionMetadataDto, ExtractionCorrectionDto, ExtractionCorrectionDetailDto, ExtractionCorrectionPagedResult)
- [x] T011 [P] Update ReceiptUpdateRequestDto in `backend/src/ExpenseFlow.Shared/DTOs/ReceiptDtos.cs` to include corrections array and rowVersion

### Service Interface

- [x] T012 Create IExtractionCorrectionService interface in `backend/src/ExpenseFlow.Core/Interfaces/IExtractionCorrectionService.cs`

### Service Implementation

- [x] T013 Implement ExtractionCorrectionService in `backend/src/ExpenseFlow.Infrastructure/Services/ExtractionCorrectionService.cs` with GetCorrectionsAsync, GetByIdAsync, RecordCorrectionsAsync methods
- [x] T014 Register ExtractionCorrectionService in DI container in `backend/src/ExpenseFlow.Infrastructure/DependencyInjection.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Edit Extracted Receipt Fields (Priority: P1) 🎯 MVP

**Goal**: Enable users to edit AI-extracted fields and save changes with validation

**Independent Test**: Upload a receipt, view extracted fields, edit vendor/amount/date, save, verify changes persisted

### Tests for User Story 1

- [ ] T015 [P] [US1] Unit test for receipt update with concurrency handling in `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/ReceiptServiceTests.cs`
- [ ] T016 [P] [US1] Integration test for PUT /api/receipts/{id} with rowVersion in `backend/tests/ExpenseFlow.Api.Tests/Controllers/ReceiptsControllerTests.cs`
- [ ] T061 [P] [US1] Frontend test for field validation error display in `frontend/tests/unit/components/receipts/extracted-field.test.tsx`

### Implementation for User Story 1

- [x] T017 [US1] Update ReceiptService.UpdateReceiptAsync to handle rowVersion and DbUpdateConcurrencyException in `backend/src/ExpenseFlow.Core/Services/ReceiptService.cs`
- [x] T018 [US1] Update ReceiptsController.UpdateAsync to return 409 Conflict on concurrency exception in `backend/src/ExpenseFlow.Api/Controllers/ReceiptsController.cs`
- [x] T019 [US1] Add FluentValidation rules for ReceiptUpdateRequestDto (required fields, numeric validation) in `backend/src/ExpenseFlow.Api/Validators/ReceiptUpdateRequestValidator.cs`
- [x] T020 [P] [US1] Update receipt types with rowVersion in `frontend/src/types/receipt.ts`
- [x] T021 [P] [US1] Add CorrectionMetadata type to `frontend/src/types/receipt.ts`
- [x] T022 [US1] Extend useUpdateReceipt mutation to handle 409 Conflict with toast notification in `frontend/src/hooks/queries/use-receipts.ts`
- [x] T023 [US1] Add pending corrections state to ReceiptIntelligencePanel in `frontend/src/components/receipts/receipt-intelligence-panel.tsx`
- [x] T024 [US1] Wire ExtractedField onSave callback to track corrections in `frontend/src/components/receipts/receipt-intelligence-panel.tsx`
- [x] T025 [US1] Add "Save All" button to batch submit corrections in `frontend/src/components/receipts/receipt-intelligence-panel.tsx`
- [x] T026 [US1] Add undo capability per field before save in `frontend/src/components/receipts/receipt-intelligence-panel.tsx`
- [x] T027 [US1] Add visual indicator for manually edited fields in `frontend/src/components/receipts/extracted-field.tsx`
- [x] T028 [US1] Prevent editing when receipt status is Processing in `frontend/src/components/receipts/receipt-intelligence-panel.tsx`

**Checkpoint**: User Story 1 complete - users can edit and save receipt fields with validation and concurrency handling

---

## Phase 4: User Story 2 - View Extraction Confidence Scores (Priority: P1)

**Goal**: Display color-coded confidence indicators for each extracted field

**Independent Test**: View a processed receipt, verify each field shows green/yellow/red indicator based on confidence score

### Implementation for User Story 2

> **NOTE**: Per research.md, ConfidenceIndicator component already exists and is integrated into ExtractedField. This phase verifies existing functionality.

- [x] T029 [US2] Verify ConfidenceIndicator thresholds match spec (≥90% green, 70-89% yellow, <70% red) in `frontend/src/components/design-system/confidence-indicator.tsx`
- [x] T030 [US2] Verify ExtractedField displays confidence scores correctly in `frontend/src/components/receipts/extracted-field.tsx`
- [x] T031 [US2] Add manual test in quickstart.md verification section for confidence display

**Checkpoint**: User Story 2 complete - confidence scores display correctly with color-coded indicators

---

## Phase 5: User Story 3 - Submit Corrections as Training Feedback (Priority: P2)

**Goal**: Capture corrections as training feedback records for model improvement

**Independent Test**: Edit a field, save, query /api/extraction-corrections to verify feedback record created with original and corrected values

### Tests for User Story 3

- [ ] T032 [P] [US3] Unit test for ExtractionCorrectionService.RecordCorrectionsAsync in `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/ExtractionCorrectionServiceTests.cs`
- [ ] T033 [P] [US3] Unit test for no-op correction filtering (same value = no record) in `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/ExtractionCorrectionServiceTests.cs`

### Implementation for User Story 3

- [x] T034 [US3] Integrate IExtractionCorrectionService into ReceiptService for recording corrections on update in `backend/src/ExpenseFlow.Core/Services/ReceiptService.cs`
- [x] T035 [US3] Filter no-op corrections (original value equals corrected value) in ExtractionCorrectionService in `backend/src/ExpenseFlow.Infrastructure/Services/ExtractionCorrectionService.cs`
- [x] T036 [US3] Add structured logging for correction submissions using Serilog in `backend/src/ExpenseFlow.Infrastructure/Services/ExtractionCorrectionService.cs`
- [x] T037 [US3] Update frontend to include corrections metadata in save request in `frontend/src/components/receipts/receipt-intelligence-panel.tsx`

**Checkpoint**: User Story 3 complete - corrections are captured as training feedback

---

## Phase 6: User Story 4 - Side-by-Side Document and Fields View (Priority: P2)

**Goal**: Display receipt document alongside extracted fields for visual verification

**Independent Test**: Open receipt detail, verify document viewer on left, fields panel on right (desktop), stacked on mobile

### Implementation for User Story 4

> **NOTE**: DocumentViewer component was recently created. This phase ensures proper responsive layout.

- [x] T038 [US4] Update ReceiptIntelligencePanel layout to flex-col lg:flex-row for side-by-side view in `frontend/src/components/receipts/receipt-intelligence-panel.tsx`
- [x] T039 [US4] Ensure DocumentViewer renders PDFs with zoom/scroll controls in `frontend/src/components/ui/document-viewer.tsx`
- [x] T040 [US4] Ensure DocumentViewer renders images with zoom/pan/rotation controls in `frontend/src/components/ui/document-viewer.tsx`
- [x] T041 [US4] Test responsive layout stacks vertically on mobile viewport
- [x] T042 [US4] Add error handling when document fails to load with retry option in `frontend/src/components/ui/document-viewer.tsx`

**Checkpoint**: User Story 4 complete - side-by-side view works on desktop, stacked on mobile

---

## Phase 7: User Story 5 - View Training Feedback History (Priority: P3)

**Goal**: Provide admin view of correction history with filtering and sorting

**Independent Test**: Navigate to feedback history view, verify paginated list with field/date/user filtering

### Tests for User Story 5

- [ ] T043 [P] [US5] Unit test for ExtractionCorrectionService.GetCorrectionsAsync with filters in `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/ExtractionCorrectionServiceTests.cs`
- [ ] T044 [P] [US5] Integration test for GET /api/extraction-corrections with query parameters in `backend/tests/ExpenseFlow.Api.Tests/Controllers/ExtractionCorrectionsControllerTests.cs`

### Implementation for User Story 5

- [x] T045 [US5] Create ExtractionCorrectionsController with GET endpoints in `backend/src/ExpenseFlow.Api/Controllers/ExtractionCorrectionsController.cs`
- [x] T046 [US5] Add query parameters parsing for fieldName, dateRange, userId, receiptId in `backend/src/ExpenseFlow.Api/Controllers/ExtractionCorrectionsController.cs`
- [x] T047 [US5] Create ExtractionCorrectionQueryParams DTO for query binding in `backend/src/ExpenseFlow.Shared/DTOs/ExtractionCorrectionDtos.cs`
- [x] T048 [US5] Implement pagination, filtering, and sorting in ExtractionCorrectionService in `backend/src/ExpenseFlow.Infrastructure/Services/ExtractionCorrectionService.cs`
- [x] T049 [P] [US5] Create useExtractionCorrections query hook in `frontend/src/hooks/queries/use-extraction-corrections.ts`
- [x] T050 [P] [US5] Create ExtractionCorrectionsPage route at /admin/extraction-corrections in `frontend/src/routes/_authenticated/admin/extraction-corrections.tsx`
- [x] T051 [US5] Create ExtractionCorrectionsList component with DataTable in `frontend/src/components/admin/extraction-corrections-list.tsx`
- [x] T052 [US5] Add filters for fieldName dropdown and date range picker in `frontend/src/components/admin/extraction-corrections-list.tsx`
- [x] T053 [US5] Add user attribution column showing who made each correction in `frontend/src/components/admin/extraction-corrections-list.tsx`

**Checkpoint**: User Story 5 complete - admin can view and filter correction history

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T054 [P] Apply database migration to staging environment per CLAUDE.md instructions (SQL in quickstart.md - requires cluster access)
- [ ] T055 [P] Run quickstart.md verification steps end-to-end
- [ ] T056 Performance validation: verify field edit + save < 10 seconds (SC-001)
- [ ] T057 Performance validation: verify side-by-side view load < 2 seconds (SC-005)
- [ ] T058 Load test: verify 100 concurrent editing sessions without degradation (SC-007)
- [x] T059 [P] Code cleanup: remove any TODO comments added during implementation
- [ ] T060 Final manual testing of all 5 user stories in staging environment

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup) ─────────────────┐
                                 │
                                 ▼
Phase 2 (Foundational) ──────────┤ ⚠️ BLOCKS all user stories
                                 │
         ┌───────────────────────┼───────────────────────┐
         │                       │                       │
         ▼                       ▼                       ▼
Phase 3 (US1)              Phase 4 (US2)           Phase 6 (US4)
Edit Fields                Confidence              Side-by-Side
    │                          │                       │
    │                          │                       │
    ▼                          │                       │
Phase 5 (US3)                  │                       │
Training Feedback              │                       │
(depends on US1)               │                       │
    │                          │                       │
    └──────────────────────────┴───────────────────────┘
                                 │
                                 ▼
                          Phase 7 (US5)
                          Feedback History
                          (depends on US3)
                                 │
                                 ▼
                          Phase 8 (Polish)
```

### User Story Dependencies

| Story | Depends On | Can Parallel With |
|-------|------------|-------------------|
| US1 (Edit Fields) | Foundational | US2, US4 |
| US2 (Confidence) | Foundational | US1, US4 |
| US3 (Training Feedback) | US1 | - |
| US4 (Side-by-Side) | Foundational | US1, US2 |
| US5 (Feedback History) | US3 | - |

### Within Each User Story

1. Tests MUST be written and FAIL before implementation
2. Backend before frontend
3. Models → Services → Controllers → Frontend
4. Core implementation before integration

---

## Parallel Opportunities

### Phase 2 (Foundational)

```bash
# Can run in parallel (different files):
T010 "Create ExtractionCorrectionDtos"
T011 "Update ReceiptUpdateRequestDto"
```

### Phase 3 (User Story 1)

```bash
# Tests can run in parallel:
T015 "Unit test for receipt update concurrency"
T016 "Integration test for PUT receipts with rowVersion"

# Frontend tasks can run in parallel:
T020 "Update receipt types with rowVersion"
T021 "Add CorrectionMetadata type"
```

### Across User Stories (After Foundational)

```bash
# These user stories can be worked on in parallel by different developers:
Developer A: Phase 3 (US1 - Edit Fields)
Developer B: Phase 4 (US2 - Confidence) + Phase 6 (US4 - Side-by-Side)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL)
3. Complete Phase 3: User Story 1 (Edit Fields)
4. **STOP and VALIDATE**: Test editing flow end-to-end
5. Deploy to staging

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. **US1 (Edit Fields)** → Deploy (MVP!)
3. **US2 (Confidence)** → Verify existing implementation
4. **US3 (Training Feedback)** → Deploy (captures corrections)
5. **US4 (Side-by-Side)** → Deploy (better UX)
6. **US5 (Feedback History)** → Deploy (admin visibility)
7. Polish → Final deployment

---

## Summary

| Phase | Tasks | Parallel Tasks | User Story |
|-------|-------|----------------|------------|
| Setup | 3 | 2 | - |
| Foundational | 11 | 2 | - |
| US1 Edit Fields | 15 | 5 | P1 🎯 MVP |
| US2 Confidence | 3 | 0 | P1 |
| US3 Training Feedback | 6 | 2 | P2 |
| US4 Side-by-Side | 5 | 0 | P2 |
| US5 Feedback History | 11 | 4 | P3 |
| Polish | 7 | 3 | - |
| **Total** | **61** | **18** | - |

### MVP Scope (User Story 1)

- Tasks: T001-T028, T061 (29 tasks)
- Deliverable: Users can edit extracted fields with validation and concurrency handling
- Estimated effort: 4-6 hours

### Notes

- [P] tasks can run in parallel (different files, no dependencies)
- [Story] label maps task to specific user story
- Each user story is independently completable and testable
- Commit after each task or logical group
