# Tasks: Receipt Unmatch & Transaction Match Display Fix

**Input**: Design documents from `/specs/031-receipt-unmatch-fix/`
**Prerequisites**: plan.md, spec.md, data-model.md, contracts/, quickstart.md

**Tests**: Not explicitly requested in spec - test tasks omitted (can be added later)

**Organization**: Tasks grouped by user story for independent implementation. Note: US1 and US2 share the receipt page so are combined for efficiency.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: No project setup needed - extending existing codebase

*Phase 1 is empty as this feature extends existing infrastructure*

---

## Phase 2: Foundational (Backend + Frontend Types)

**Purpose**: Backend DTO and repository changes that MUST be complete before user story UI work

**⚠️ CRITICAL**: No frontend user story work can begin until this phase is complete

- [X] T001 [P] Create MatchedTransactionInfoDto class in `backend/src/ExpenseFlow.Shared/DTOs/ReceiptDetailDto.cs`
- [X] T002 [P] Add MatchedTransaction property to ReceiptDetailDto in `backend/src/ExpenseFlow.Shared/DTOs/ReceiptDetailDto.cs`
- [X] T003 Update GetByIdAsync to include match data with Transaction navigation in `backend/src/ExpenseFlow.Api/Controllers/ReceiptsController.cs`
- [X] T004 Add mapping logic for MatchedTransactionInfoDto in `backend/src/ExpenseFlow.Api/Controllers/ReceiptsController.cs`
- [X] T005 Add MatchedTransactionInfo interface to `frontend/src/types/api.ts`
- [X] T006 Update ReceiptDetail interface to include matchedTransaction field in `frontend/src/types/api.ts`

**Checkpoint**: Backend API returns matched transaction info for receipts, frontend types are ready

---

## Phase 3: User Story 2 - View Matched Transaction on Receipt Page (Priority: P1)

**Goal**: Display linked transaction information on receipt detail page with "View Transaction" navigation

**Independent Test**: Navigate to a matched receipt, verify transaction info (date, amount, description, merchant, confidence) displays with clickable "View Transaction" link

### Implementation for User Story 2

- [X] T007 [US2] Add CreditCard icon import and Link component import in `frontend/src/routes/_authenticated/receipts/$receiptId.tsx`
- [X] T008 [US2] Create "Linked Transaction" Card component showing matched transaction info in `frontend/src/routes/_authenticated/receipts/$receiptId.tsx`
- [X] T009 [US2] Add "View Transaction" Button with Link to transaction detail page in `frontend/src/routes/_authenticated/receipts/$receiptId.tsx`
- [X] T010 [US2] Add confidence badge display (percentage) in Linked Transaction section in `frontend/src/routes/_authenticated/receipts/$receiptId.tsx`
- [X] T011 [US2] Add empty state "No transaction matched yet" with guidance in `frontend/src/routes/_authenticated/receipts/$receiptId.tsx`

**Checkpoint**: Receipt detail page shows linked transaction info with navigation - User Story 2 independently functional

---

## Phase 4: User Story 1 - Unmatch Receipt from Receipt Page (Priority: P1)

**Goal**: Add unmatch button and confirmation dialog to receipt detail page

**Independent Test**: Navigate to matched receipt, click Unmatch, confirm action, verify match removed and success notification shown

**Dependency**: User Story 2 must be complete (linked transaction section provides context for unmatch)

### Implementation for User Story 1

- [X] T012 [US1] Add useUnmatch hook import from `@/hooks/queries/use-matching` in `frontend/src/routes/_authenticated/receipts/$receiptId.tsx`
- [X] T013 [US1] Add useState for showUnmatchDialog in `frontend/src/routes/_authenticated/receipts/$receiptId.tsx`
- [X] T014 [US1] Add Link2Off icon import in `frontend/src/routes/_authenticated/receipts/$receiptId.tsx`
- [X] T015 [US1] Add unmatchMutation using useUnmatch hook in `frontend/src/routes/_authenticated/receipts/$receiptId.tsx`
- [X] T016 [US1] Add handleUnmatch function with toast notifications in `frontend/src/routes/_authenticated/receipts/$receiptId.tsx`
- [X] T017 [US1] Add "Unmatch" Button next to "View Transaction" (disabled during pending) in `frontend/src/routes/_authenticated/receipts/$receiptId.tsx`
- [X] T018 [US1] Add AlertDialog for unmatch confirmation in `frontend/src/routes/_authenticated/receipts/$receiptId.tsx`

**Checkpoint**: Receipt page has working unmatch functionality - User Stories 1 AND 2 independently functional

---

## Phase 5: User Story 3 - Fix Transaction Match Status Display (Priority: P1)

**Goal**: Fix bug where transaction detail page shows incorrect match status by using matchedReceipt object as source of truth

**Independent Test**: View transaction with matched receipt, verify badge shows "Matched" and receipt info displays correctly

**Dependency**: None - this is an independent bug fix

### Implementation for User Story 3

- [X] T019 [US3] Replace `transaction.hasMatchedReceipt` with `!!transaction.matchedReceipt` for header badge in `frontend/src/routes/_authenticated/transactions/$transactionId.tsx`
- [X] T020 [US3] Replace `transaction.hasMatchedReceipt` with `!!transaction.matchedReceipt` for CardDescription text in `frontend/src/routes/_authenticated/transactions/$transactionId.tsx`
- [X] T021 [US3] Verify all conditional renders use matchedReceipt object presence in `frontend/src/routes/_authenticated/transactions/$transactionId.tsx`

**Checkpoint**: Transaction page correctly displays match status - All user stories complete

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final verification and cleanup

- [X] T022 [P] Verify cache invalidation works correctly after unmatch (receipts + transactions queries)
- [X] T023 Run manual verification using quickstart.md test scenarios
- [X] T024 Verify TypeScript compilation passes in `frontend/` directory

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup ─────────────────────────────────────────► (empty)
                                                              │
Phase 2: Foundational ◄─────────────────────────────────────┘
         [T001-T006: Backend DTO + Repository + Frontend Types]
                    │
                    ▼
          ┌────────┴────────┐
          │                 │
Phase 3: US2 ◄──────┐  Phase 5: US3 ◄───────────────────────┐
[T007-T011]         │  [T019-T021]                          │
          │         │  (Independent)                        │
          ▼         │                                       │
Phase 4: US1 ◄──────┘                                       │
[T012-T018]                                                 │
          │                                                 │
          └─────────────────┬───────────────────────────────┘
                            ▼
                    Phase 6: Polish
                    [T022-T024]
```

### User Story Dependencies

- **User Story 2 (View Transaction)**: Depends on Foundational phase - No other story dependencies
- **User Story 1 (Unmatch)**: Depends on Foundational phase AND User Story 2 (needs linked transaction section)
- **User Story 3 (Bug Fix)**: Depends on Foundational phase - No other story dependencies (can run in parallel with US1/US2)

### Parallel Opportunities

**Phase 2 (Foundational)**:
```bash
# Backend tasks can run in parallel:
Task T001: "Create MatchedTransactionInfoDto class"
Task T002: "Add MatchedTransaction property to ReceiptDetailDto"

# Then sequentially:
Task T003-T004: Repository changes (depend on DTOs)

# Frontend types in parallel with backend:
Task T005: "Add MatchedTransactionInfo interface"
Task T006: "Update ReceiptDetail interface"
```

**User Stories (after Foundational)**:
```bash
# US2 and US3 can run in parallel:
Developer A: User Story 2 (T007-T011) - Receipt page linked transaction
Developer B: User Story 3 (T019-T021) - Transaction page bug fix

# Then US1 after US2:
Developer A: User Story 1 (T012-T018) - Receipt page unmatch
```

---

## Implementation Strategy

### MVP First (User Story 2 Only)

1. Complete Phase 2: Foundational (T001-T006)
2. Complete Phase 3: User Story 2 (T007-T011)
3. **STOP and VALIDATE**: Verify matched transaction info displays on receipt page
4. Users can now see linked transactions from receipts

### Full Feature Delivery

1. Complete Foundational → Backend returns match data
2. Complete US2 → Receipt page shows linked transaction
3. Complete US1 → Receipt page has unmatch button
4. Complete US3 → Transaction page bug fixed
5. Polish → Final verification

### Recommended Order (Single Developer)

```
T001 → T002 → T003 → T004 → T005 → T006  (Foundational)
                                    ↓
T007 → T008 → T009 → T010 → T011        (US2: View Transaction)
                              ↓
T012 → T013 → T014 → T015 → T016 → T017 → T018  (US1: Unmatch)
                                            ↓
T019 → T020 → T021                          (US3: Bug Fix)
            ↓
T022 → T023 → T024                          (Polish)
```

---

## Notes

- All frontend changes to receipt page (`$receiptId.tsx`) are in the same file - execute sequentially
- Transaction page bug fix (`$transactionId.tsx`) is isolated - can be done anytime after Foundational
- Cache invalidation already handled by existing `useUnmatch` hook
- Backend changes are additive only - no breaking changes to existing API consumers
- Total tasks: 24 (6 foundational, 5 US2, 7 US1, 3 US3, 3 polish)
