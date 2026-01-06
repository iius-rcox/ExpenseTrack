# Tasks: Missing Receipts UI

**Input**: Design documents from `/specs/026-missing-receipts-ui/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Not explicitly requested in spec - implementation tasks only.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Backend**: `backend/src/ExpenseFlow.*`, `backend/tests/ExpenseFlow.*`
- **Frontend**: `frontend/src/`, `frontend/tests/`

---

## Phase 1: Setup

**Purpose**: Database schema changes and shared infrastructure

- [x] T001 Add `ReceiptUrl` and `ReceiptDismissed` columns to Transaction entity in `backend/src/ExpenseFlow.Core/Entities/Transaction.cs`
- [x] T002 Generate EF Core migration for Transaction entity changes in `backend/src/ExpenseFlow.Infrastructure/Migrations/`
- [x] T003 [P] Create DTOs for missing receipts in `backend/src/ExpenseFlow.Shared/DTOs/MissingReceiptDtos.cs` (MissingReceiptSummaryDto, MissingReceiptsListResponseDto, MissingReceiptsWidgetDto, UpdateReceiptUrlRequestDto, DismissReceiptRequestDto, ReimbursabilitySource enum)
- [x] T004 [P] Create IMissingReceiptService interface in `backend/src/ExpenseFlow.Core/Interfaces/IMissingReceiptService.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core backend service that MUST be complete before ANY user story UI can be implemented

**⚠️ CRITICAL**: No frontend work can begin until this phase is complete

- [x] T005 Implement MissingReceiptService with GetMissingReceiptsAsync (paginated list query with sorting) in `backend/src/ExpenseFlow.Infrastructure/Services/MissingReceiptService.cs`
- [x] T006 Add GetWidgetDataAsync method to MissingReceiptService (count + top 3 items)
- [x] T007 Add UpdateReceiptUrlAsync method to MissingReceiptService
- [x] T008 Add DismissTransactionAsync method to MissingReceiptService (dismiss/restore logic)
- [ ] T008a [P] Create unit tests for MissingReceiptService in `backend/tests/ExpenseFlow.Core.Tests/Services/MissingReceiptServiceTests.cs` (test GetMissingReceiptsAsync, GetWidgetDataAsync, UpdateReceiptUrlAsync, DismissTransactionAsync)
- [x] T009 Register MissingReceiptService in DI container in `backend/src/ExpenseFlow.Infrastructure/Extensions/ServiceCollectionExtensions.cs`
- [x] T010 Create MissingReceiptsController with all endpoints in `backend/src/ExpenseFlow.Api/Controllers/MissingReceiptsController.cs`
- [ ] T010a [P] Create integration tests for MissingReceiptsController in `backend/tests/ExpenseFlow.Api.Tests/Controllers/MissingReceiptsControllerTests.cs` (test all 4 endpoints with auth, validation, and error cases)
- [x] T011 Add Serilog logging to MissingReceiptService operations
- [x] T012 [P] Create TanStack Query hooks in `frontend/src/hooks/queries/use-missing-receipts.ts` (useMissingReceipts, useMissingReceiptsWidget, useUpdateReceiptUrl, useDismissMissingReceipt)
- [x] T013 [P] Create API client functions in `frontend/src/lib/api/missing-receipts.ts`

**Checkpoint**: Backend API fully functional, frontend hooks ready - user story UI can now begin

---

## Phase 3: User Story 1 - View Missing Receipts List (Priority: P1) 🎯 MVP

**Goal**: Users can see a list of all transactions flagged as reimbursable without matched receipts

**Independent Test**: Navigate to `/missing-receipts` and verify all predicted reimbursable transactions without receipts appear with date, vendor, amount, and days since transaction

### Implementation for User Story 1

- [x] T014 [P] [US1] Create MissingReceiptCard component in `frontend/src/components/missing-receipts/missing-receipt-card.tsx` (displays transaction date, vendor, amount, days since)
- [x] T015 [P] [US1] Create EmptyState component for missing receipts in `frontend/src/components/missing-receipts/missing-receipts-empty.tsx`
- [x] T016 [US1] Create full list page route in `frontend/src/routes/_authenticated/missing-receipts/index.tsx` with pagination (25/page), sorting (date, amount, vendor), and empty state
- [x] T017 [US1] Create MissingReceiptsWidget component in `frontend/src/components/missing-receipts/missing-receipts-widget.tsx` (count + top 3 items with "View All" link)
- [x] T018 [US1] Integrate MissingReceiptsWidget into Matching page at `frontend/src/routes/_authenticated/matching/index.tsx`

**Checkpoint**: User Story 1 complete - users can view missing receipts list and widget

---

## Phase 4: User Story 2 - Add Receipt URL (Priority: P2)

**Goal**: Users can store a URL where they can retrieve a missing receipt

**Independent Test**: Click "Add URL" on a missing receipt, enter URL, save, and verify URL persists on page reload

### Implementation for User Story 2

- [x] T019 [P] [US2] Create ReceiptUrlDialog component in `frontend/src/components/missing-receipts/receipt-url-dialog.tsx` (add/edit/clear URL)
- [x] T020 [US2] Add "Add URL" / "Edit URL" action button to MissingReceiptCard in `frontend/src/components/missing-receipts/missing-receipt-card.tsx`
- [x] T021 [US2] Wire ReceiptUrlDialog to useUpdateReceiptUrl mutation with optimistic updates

**Checkpoint**: User Story 2 complete - users can add/edit/clear receipt URLs

---

## Phase 5: User Story 3 - Upload Receipt from View (Priority: P2)

**Goal**: Users can upload a receipt directly from the missing receipts list

**Independent Test**: Click "Upload" on a missing receipt, select file, verify receipt uploads and item is removed from list after matching

### Implementation for User Story 3

- [x] T022 [US3] Add "Upload Receipt" button to MissingReceiptCard in `frontend/src/components/missing-receipts/missing-receipt-card.tsx`
- [x] T023 [US3] Add quick upload button to MissingReceiptsWidget items in `frontend/src/components/missing-receipts/missing-receipts-widget.tsx`
- [ ] T024 [US3] Integrate existing receipt upload dialog/flow with targetTransactionId hint parameter
- [ ] T025 [US3] Add query invalidation to refresh missing receipts list after successful upload

**Checkpoint**: User Story 3 complete - users can upload receipts directly from missing receipts view

---

## Phase 6: User Story 4 - Navigate to Saved URL (Priority: P3)

**Goal**: Saved URLs are clickable links that open in new browser tab

**Independent Test**: Save a URL for a receipt, click the link, verify it opens in new tab

### Implementation for User Story 4

- [x] T026 [US4] Update MissingReceiptCard to render saved URL as clickable link with `target="_blank"` and `rel="noopener noreferrer"` in `frontend/src/components/missing-receipts/missing-receipt-card.tsx`
- [x] T027 [US4] Add URL truncation with tooltip for long URLs (show first 40 chars, full URL on hover)

**Checkpoint**: User Story 4 complete - saved URLs are clickable

---

## Phase 7: User Story 5 - Dismiss/Ignore Missing Receipt (Priority: P3)

**Goal**: Users can dismiss transactions incorrectly flagged as reimbursable and restore dismissed items

**Independent Test**: Dismiss a receipt, verify it disappears from list; enable "Show Dismissed" filter, verify item appears with "Restore" option

### Implementation for User Story 5

- [x] T028 [P] [US5] Create DismissConfirmDialog component in `frontend/src/components/missing-receipts/dismiss-confirm-dialog.tsx`
- [x] T029 [US5] Add "Dismiss" action button to MissingReceiptCard in `frontend/src/components/missing-receipts/missing-receipt-card.tsx`
- [x] T030 [US5] Add "Show Dismissed" toggle/filter to full list page in `frontend/src/routes/_authenticated/missing-receipts/index.tsx`
- [x] T031 [US5] Add "Restore" action for dismissed items in MissingReceiptCard
- [x] T032 [US5] Wire dismiss/restore actions to useDismissMissingReceipt mutation with optimistic updates

**Checkpoint**: User Story 5 complete - users can dismiss and restore receipts

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T033 [P] Add loading skeletons for list and widget components
- [x] T034 [P] Add error boundary and retry logic for failed API calls
- [ ] T035 Apply database migration to staging environment (manual step per CLAUDE.md)
- [ ] T036 Run quickstart.md validation scenarios

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user story UI work
- **User Stories (Phase 3-7)**: All depend on Foundational phase completion
  - US1 (P1) must complete before US4 (builds on same component)
  - US2, US3, US5 can proceed in parallel after US1
  - US4 can proceed after US1 (extends MissingReceiptCard)
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

```
                    ┌─────────────────────────────────────────┐
                    │          Setup (Phase 1)                │
                    │  T001-T004: Entity, Migration, DTOs     │
                    └────────────────┬────────────────────────┘
                                     │
                    ┌────────────────▼────────────────────────┐
                    │       Foundational (Phase 2)            │
                    │  T005-T013: Service, Controller, Hooks  │
                    └────────────────┬────────────────────────┘
                                     │
              ┌──────────────────────▼──────────────────────────┐
              │            User Story 1 (P1) 🎯 MVP             │
              │  T014-T018: List page, Widget, Card, EmptyState │
              └────┬───────────────┬───────────────┬────────────┘
                   │               │               │
       ┌───────────▼───┐   ┌───────▼───────┐   ┌──▼──────────────┐
       │  US2 (P2)     │   │  US3 (P2)     │   │  US4 (P3)       │
       │  T019-T021    │   │  T022-T025    │   │  T026-T027      │
       │  Add URL      │   │  Quick Upload │   │  Clickable Link │
       └───────┬───────┘   └───────┬───────┘   └────────┬────────┘
               │                   │                    │
               │           ┌───────▼───────┐           │
               │           │  US5 (P3)     │           │
               └──────────►│  T028-T032    │◄──────────┘
                           │  Dismiss      │
                           └───────┬───────┘
                                   │
                    ┌──────────────▼──────────────────────┐
                    │       Polish (Phase 8)              │
                    │  T033-T036: Skeletons, Errors, DB   │
                    └─────────────────────────────────────┘
```

### Within Each User Story

- Models/DTOs before services
- Services before controllers
- Backend complete before frontend
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

**Phase 1 (Setup):**
```
Parallel: T003 (DTOs) || T004 (Interface)
Sequential: T001 → T002 (Entity before migration)
```

**Phase 2 (Foundational):**
```
Sequential: T005 → T006 → T007 → T008 → T009 → T010 → T011 (backend)
Parallel: T008a (service tests) || T010a (controller tests) - can parallel with T011
Parallel: T012 (hooks) || T013 (API client) - frontend can parallel with backend
```

**User Stories (after Phase 2):**
```
Parallel: US2, US3, US5 can run in parallel after US1 completes
Sequential: US1 → US4 (US4 extends US1 component)
```

---

## Parallel Example: Phase 1 Setup

```bash
# These can run in parallel (different files):
Task: "T003 [P] Create DTOs in backend/src/ExpenseFlow.Shared/DTOs/MissingReceiptDtos.cs"
Task: "T004 [P] Create interface in backend/src/ExpenseFlow.Core/Interfaces/IMissingReceiptService.cs"

# These must be sequential (T002 depends on T001):
Task: "T001 Add columns to Transaction.cs"
Task: "T002 Generate EF Core migration" (after T001)
```

## Parallel Example: User Story 1

```bash
# Launch all independent components together:
Task: "T014 [P] [US1] Create MissingReceiptCard component"
Task: "T015 [P] [US1] Create EmptyState component"

# Then sequential integration:
Task: "T016 [US1] Create full list page" (uses T014, T015)
Task: "T017 [US1] Create Widget component" (uses T014)
Task: "T018 [US1] Integrate Widget into Matching page" (uses T017)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T004)
2. Complete Phase 2: Foundational (T005-T013)
3. Complete Phase 3: User Story 1 (T014-T018)
4. **STOP and VALIDATE**: Test list view and widget independently
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Backend API ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test URL functionality → Deploy/Demo
4. Add User Story 3 → Test upload flow → Deploy/Demo
5. Add User Story 4 → Test clickable links → Deploy/Demo
6. Add User Story 5 → Test dismiss/restore → Deploy/Demo
7. Each story adds value without breaking previous stories

### Single Developer Strategy

Follow priority order: US1 → US2 → US3 → US4 → US5

### Parallel Team Strategy

With 2 developers after US1:
- Developer A: US2 (Add URL)
- Developer B: US3 (Quick Upload)
Then:
- Developer A: US4 (Clickable Links)
- Developer B: US5 (Dismiss/Restore)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Database migration must be applied manually to staging per CLAUDE.md instructions
