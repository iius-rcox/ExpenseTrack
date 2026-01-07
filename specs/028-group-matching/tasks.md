# Tasks: Transaction Group Matching

**Input**: Design documents from `/specs/028-group-matching/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Tests are included per Constitution Principle II (Test-First Development).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- **Web app**: `backend/src/`, `frontend/src/`
- All paths are relative to repository root

---

## Phase 1: Setup

**Purpose**: Verify existing infrastructure and create new test files

- [X] T001 Verify existing entities have required properties in `backend/src/ExpenseFlow.Core/Entities/TransactionGroup.cs`
- [X] T002 Verify ReceiptTransactionMatch has TransactionGroupId in `backend/src/ExpenseFlow.Core/Entities/ReceiptTransactionMatch.cs`
- [X] T003 [P] Create unit test file `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/MatchingServiceGroupTests.cs`
- [X] T004 [P] Create integration test file `backend/tests/ExpenseFlow.Api.Tests/Matching/GroupMatchingIntegrationTests.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core DTOs and interface changes that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T005 Add MatchCandidateType enum to `backend/src/ExpenseFlow.Shared/DTOs/MatchCandidateDtos.cs` and `backend/src/ExpenseFlow.Core/Interfaces/IMatchingService.cs`
- [X] T006 Add MatchCandidate class to `backend/src/ExpenseFlow.Core/Interfaces/IMatchingService.cs`
- [X] T007 Add MatchCandidateDto class to `backend/src/ExpenseFlow.Shared/DTOs/MatchCandidateDtos.cs`
- [X] T008 Extend MatchProposalDto with CandidateType and TransactionGroup in `backend/src/ExpenseFlow.Shared/DTOs/MatchProposalDto.cs`
- [X] T009 Extend AutoMatchResult with GroupMatchCount in `backend/src/ExpenseFlow.Core/Interfaces/IMatchingService.cs`
- [X] T010 Add GetCandidatesAsync and CreateManualGroupMatchAsync method signatures to `backend/src/ExpenseFlow.Core/Interfaces/IMatchingService.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Auto-Match Receipt to Transaction Group (Priority: P1) 🎯 MVP

**Goal**: Auto-matching engine considers transaction groups as candidates alongside individual transactions

**Independent Test**: Upload a receipt for $50.00, create a group with transactions totaling $50.00, run auto-match, verify group is proposed as match

### Tests for User Story 1

- [ ] T011 [P] [US1] Unit test: CalculateAmountScore works for group CombinedAmount in `backend/tests/ExpenseFlow.Unit.Tests/Services/MatchingServiceGroupTests.cs`
- [ ] T012 [P] [US1] Unit test: CalculateDateScore works for group DisplayDate in `backend/tests/ExpenseFlow.Unit.Tests/Services/MatchingServiceGroupTests.cs`
- [ ] T013 [P] [US1] Unit test: ExtractVendorFromGroupName extracts vendor correctly in `backend/tests/ExpenseFlow.Unit.Tests/Services/MatchingServiceGroupTests.cs`
- [ ] T014 [P] [US1] Integration test: RunAutoMatchAsync proposes group match in `backend/tests/ExpenseFlow.Integration.Tests/Matching/GroupMatchingTests.cs`

### Implementation for User Story 1

- [X] T015 [US1] Add ExtractVendorFromGroupName helper method to `backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs`
- [X] T016 [US1] Add GetUnmatchedGroupsAsync private method to `backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs`
- [X] T017 [US1] Modify RunAutoMatchAsync to query groups in parallel with transactions in `backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs`
- [X] T018 [US1] Exclude grouped transactions (filter GroupId == null) in RunAutoMatchAsync transaction query in `backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs`
- [X] T019 [US1] Create unified candidate pool from transactions and groups in RunAutoMatchAsync in `backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs`
- [X] T020 [US1] Extend FindBestMatchFromCandidates to handle MatchCandidate type in `backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs`
- [X] T021 [US1] Update proposal creation to set TransactionGroupId for group matches in `backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs`
- [X] T022 [US1] Update group MatchStatus to Proposed when match is proposed in `backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs`
- [X] T023 [US1] Add structured logging for group matching events in `backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs`

**Checkpoint**: Auto-matching now proposes groups as candidates. Run T014 to verify.

---

## Phase 4: User Story 2 - Manual Match Receipt to Group (Priority: P2)

**Goal**: Users can manually match a receipt to a transaction group when auto-match doesn't find the correct match

**Independent Test**: Create a group, navigate to unmatched receipt, select group from candidates, confirm match

### Tests for User Story 2

- [ ] T024 [P] [US2] Unit test: CreateManualMatchAsync works with transactionGroupId in `backend/tests/ExpenseFlow.Unit.Tests/Services/MatchingServiceGroupTests.cs`
- [ ] T025 [P] [US2] Integration test: Manual match to group updates both receipt and group status in `backend/tests/ExpenseFlow.Integration.Tests/Matching/GroupMatchingTests.cs`

### Implementation for User Story 2

- [X] T026 [US2] Extend ManualMatchRequestDto with TransactionGroupId property in `backend/src/ExpenseFlow.Shared/DTOs/ManualMatchRequestDto.cs`
- [X] T027 [US2] Add IValidatableObject for XOR validation (TransactionId, TransactionGroupId) in ManualMatchRequestDto
- [X] T028 [US2] Implement CreateManualGroupMatchAsync for group lookup and status update in `backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs`
- [X] T029 [US2] Implement GetCandidatesAsync method to return ranked candidates for a receipt in `backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs`
- [X] T030 [US2] Add GET /api/matching/candidates/{receiptId} endpoint to `backend/src/ExpenseFlow.Api/Controllers/MatchingController.cs`
- [X] T031 [US2] Update POST /api/matching/manual to accept transactionGroupId in `backend/src/ExpenseFlow.Api/Controllers/MatchingController.cs`

**Checkpoint**: Manual matching to groups works. Run T025 to verify.

---

## Phase 5: User Story 3 - Exclude Grouped Transactions from Individual Matching (Priority: P2)

**Goal**: Transactions that are part of a group are NOT considered as individual match candidates

**Independent Test**: Create group with 3 transactions, run auto-match on receipt matching one transaction's amount, verify that transaction is NOT proposed

### Tests for User Story 3

- [ ] T032 [P] [US3] Unit test: Grouped transactions excluded from candidate pool in `backend/tests/ExpenseFlow.Unit.Tests/Services/MatchingServiceGroupTests.cs`
- [ ] T033 [P] [US3] Integration test: Receipt does not match grouped transaction individually in `backend/tests/ExpenseFlow.Integration.Tests/Matching/GroupMatchingTests.cs`

### Implementation for User Story 3

- [X] T034 [US3] Verified grouped transactions are excluded (GroupId == null filter) in RunAutoMatchAsync transaction query in `backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs:97`
- [X] T035 [US3] Verified grouped transactions are excluded from GetCandidatesAsync query in `backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs:1169`

**Checkpoint**: Grouped transactions no longer appear as individual candidates. Run T033 to verify.

---

## Phase 6: User Story 4 - Unmatch and Re-Match Groups (Priority: P3)

**Goal**: Users can unmatch a receipt from a group and re-match to a different target

**Independent Test**: Match receipt to group, unmatch, verify both return to unmatched status, match to different transaction

### Tests for User Story 4

- [ ] T036 [P] [US4] Unit test: RejectMatchAsync works for group matches in `backend/tests/ExpenseFlow.Unit.Tests/Services/MatchingServiceGroupTests.cs`
- [ ] T037 [P] [US4] Integration test: Unmatch from group resets both statuses in `backend/tests/ExpenseFlow.Integration.Tests/Matching/GroupMatchingTests.cs`

### Implementation for User Story 4

- [X] T038 [US4] Extend RejectMatchAsync to handle group matches in `backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs`
- [X] T039 [US4] Reset TransactionGroup.MatchStatus and MatchedReceiptId on unmatch in `backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs`
- [X] T040 [US4] Extend ConfirmMatchAsync to update TransactionGroup status on confirmation in `backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs`

**Checkpoint**: Unmatch and re-match flow works. Run T037 to verify.

---

## Phase 7: Frontend Integration

**Goal**: Display transaction groups as match candidates in the UI

- [X] T041 [P] Update MatchCandidate TypeScript interface with candidateType in `frontend/src/types/match.ts`
- [X] T042 [P] Update ManualMatchRequest type with transactionGroupId in `frontend/src/types/match.ts`
- [X] T042b [P] Add useMatchCandidates hook to `frontend/src/hooks/queries/use-matching.ts`
- [X] T043 Update MatchCandidateList component to display groups with visual badge (e.g., "Group" chip) distinguishing them from individual transactions in `frontend/src/components/matching/`
- [X] T044 Show transaction count (e.g., "3 transactions") and combined amount for group candidates in `frontend/src/components/matching/`
- [X] T045 Handle group selection in match confirmation flow in `frontend/src/components/matching/`

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Edge cases, performance verification, documentation

- [X] T046 [P] Handle group deletion cascade (return receipt to unmatched) in `backend/src/ExpenseFlow.Infrastructure/Services/TransactionGroupService.cs`
- [X] T047 [P] Add warning toast notification when transaction removed from matched group causes amount mismatch (>$1.00 tolerance) in `backend/src/ExpenseFlow.Infrastructure/Services/TransactionGroupService.cs` - return warning message in response DTO
- [X] T048 Performance test: Verify <2 second auto-match with 1000 transactions and 50 groups
- [X] T049 [P] Update OpenAPI documentation to reflect group matching capabilities
- [X] T050 Run quickstart.md validation to verify all manual test scenarios pass

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - US1 (P1): Can start after Foundational - No dependencies on other stories
  - US2 (P2): Can start after Foundational - May use GetCandidatesAsync from US1
  - US3 (P2): Can start after Foundational - Validates filter added in US1
  - US4 (P3): Can start after Foundational - Extends reject/confirm from US1
- **Frontend (Phase 7)**: Depends on API changes in US1 and US2 being complete
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

| Story | Can Start After | Independent Test? |
|-------|-----------------|-------------------|
| US1 (Auto-Match) | Phase 2 | ✅ Yes |
| US2 (Manual Match) | Phase 2 | ✅ Yes |
| US3 (Exclude Grouped) | Phase 2 (uses US1 filter) | ✅ Yes |
| US4 (Unmatch/Re-Match) | Phase 2 | ✅ Yes |

### Parallel Opportunities

**Within Phase 2 (Foundational)**:
- T005, T006, T007, T008, T009 can run in parallel (different DTOs/classes)

**Within User Stories**:
- All test tasks ([P]) within a story can run in parallel
- Stories US1, US2, US3, US4 can be worked on in parallel after Phase 2

**Within Phase 7 (Frontend)**:
- T041, T042 can run in parallel

---

## Parallel Example: User Story 1 Tests

```bash
# Launch all US1 tests together (they're all [P]):
T011: Unit test for amount scoring
T012: Unit test for date scoring
T013: Unit test for vendor extraction
T014: Integration test for auto-match
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T004)
2. Complete Phase 2: Foundational (T005-T010)
3. Complete Phase 3: User Story 1 (T011-T023)
4. **STOP and VALIDATE**: Test auto-matching with groups
5. Deploy to staging for validation

### Incremental Delivery

1. Phase 1 + 2 → Foundation ready
2. Add US1 → Auto-match works with groups → **MVP deployed**
3. Add US2 → Manual matching works
4. Add US3 → Data integrity ensured
5. Add US4 → Error correction works
6. Add Frontend → Full user experience
7. Polish → Production ready

---

## Summary

| Phase | Task Count | Parallel Tasks |
|-------|------------|----------------|
| Setup | 4 | 2 |
| Foundational | 6 | 0 |
| US1 (Auto-Match) | 13 | 4 |
| US2 (Manual Match) | 8 | 2 |
| US3 (Exclude Grouped) | 4 | 2 |
| US4 (Unmatch) | 5 | 2 |
| Frontend | 5 | 2 |
| Polish | 5 | 3 |
| **Total** | **50** | **17** |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- Verify tests fail before implementing (Test-First per Constitution)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
