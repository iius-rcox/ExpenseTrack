# Tasks: Automated UAT Testing for Claude Code

**Input**: Design documents from `/specs/012-automated-uat-testing/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: No separate test tasks—this feature IS the test framework. The "tests" are the UAT scenarios Claude Code executes.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Backend**: `backend/src/ExpenseFlow.Api/`
- **Test Data**: `test-data/`
- **Specs**: `specs/012-automated-uat-testing/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and test data configuration

- [X] T001 Create expected values JSON file at test-data/expected-values.json with schema from data-model.md
- [X] T002 [P] Add RDU parking receipt expected values (amount: 84.00, date: 2025-12-11, vendor: RDU Airport Parking) to test-data/expected-values.json
- [X] T003 [P] Add PDF receipt (Receipt 2768.pdf) expected values to test-data/expected-values.json
- [X] T004 [P] Add statement import expectations (484 rows, chase.csv format) to test-data/expected-values.json
- [X] T005 [P] Add RDU transaction expected values (RDUAA PUBLIC PARKING, Travel category) to test-data/expected-values.json
- [X] T006 Add expected match pair (RDU receipt ↔ RDUAA transaction, minConfidence: 0.80) to test-data/expected-values.json

**Checkpoint**: Expected values file ready for assertion validation

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Cleanup endpoint that MUST be complete before ANY user story can be tested idempotently

**⚠️ CRITICAL**: No user story testing can be repeated reliably until this phase is complete

- [X] T007 Create TestCleanupController.cs at backend/src/ExpenseFlow.Api/Controllers/TestCleanupController.cs with route [Route("api/test")] and [Authorize] attribute (per FR-013 authentication requirement)
- [X] T008 Implement CleanupRequest DTO in backend/src/ExpenseFlow.Shared/DTOs/CleanupRequest.cs per data-model.md schema
- [X] T009 Implement CleanupResponse DTO in backend/src/ExpenseFlow.Shared/DTOs/CleanupResponse.cs per data-model.md schema
- [X] T010 Implement POST /api/test/cleanup endpoint per contracts/test-cleanup-api.yaml specification
- [X] T011 Add receipt deletion logic (including blob storage cleanup) to TestCleanupController
- [X] T012 Add transaction and match deletion logic to TestCleanupController
- [X] T013 Add statement import deletion logic to TestCleanupController
- [X] T014 Add environment restriction (#if DEBUG || STAGING) to TestCleanupController to prevent production deployment
- [ ] T015 Test cleanup endpoint manually via curl/Postman against local API

**Checkpoint**: Cleanup endpoint deployed to staging - idempotent test runs now possible

---

## Phase 3: User Story 1 - Receipt Upload and OCR Processing (Priority: P1) 🎯 MVP

**Goal**: Upload 19 test receipts and verify OCR processing completes with expected extraction values

**Independent Test**: Upload receipts from `test-data/receipts/`, poll for completion, validate against expected-values.json

### Implementation for User Story 1

- [X] T016 [US1] Document receipt upload test procedure in specs/012-automated-uat-testing/uat-us1-receipts.md
- [X] T017 [US1] Add all 17 remaining receipt filenames with placeholder expected values to test-data/expected-values.json
- [X] T018 [US1] Document polling logic (5-second intervals, 2-minute timeout) for receipt status in uat-us1-receipts.md
- [X] T019 [US1] Document assertion logic for receipt extraction validation (amount, date, vendor, confidence) in uat-us1-receipts.md
- [X] T020 [US1] Document RDU parking specific assertion ($84.00, Dec 11 2025) in uat-us1-receipts.md
- [X] T021 [US1] Document PDF receipt (Receipt 2768.pdf) processing validation in uat-us1-receipts.md
- [X] T022 [US1] Add error case handling documentation (corrupted images, timeouts) in uat-us1-receipts.md
- [X] T022a [US1] Document duplicate receipt upload detection test case in uat-us1-receipts.md (per spec edge case: detect and warn about potential duplicates)

**Checkpoint**: User Story 1 UAT documentation complete - Claude Code can execute receipt upload tests

---

## Phase 4: User Story 2 - Statement Import and Transaction Fingerprinting (Priority: P1)

**Goal**: Import Chase CSV and verify 486 transactions are created with proper categorization

**Independent Test**: Import `test-data/statements/chase.csv`, verify transaction count and fingerprinting

### Implementation for User Story 2

- [X] T023 [US2] Document statement analyze and import test procedure in specs/012-automated-uat-testing/uat-us2-statements.md
- [X] T024 [US2] Document column mapping expectations for Chase CSV format in uat-us2-statements.md
- [X] T025 [US2] Document transaction count assertion (484 imported, 0 skipped) in uat-us2-statements.md
- [X] T026 [US2] Document RDU transaction fingerprinting validation (Travel category, normalized vendor) in uat-us2-statements.md
- [X] T027 [US2] Document Amazon vendor normalization validation in uat-us2-statements.md
- [X] T028 [US2] Add duplicate detection test case to uat-us2-statements.md (re-import should show duplicates)
- [X] T028a [US2] Document malformed CSV row handling test case in uat-us2-statements.md (per spec edge case: skip bad rows and report count of skipped entries)

**Checkpoint**: User Story 2 UAT documentation complete - Claude Code can execute statement import tests

---

## Phase 5: User Story 3 - Automated Receipt-Transaction Matching (Priority: P2)

**Goal**: Trigger auto-matching and verify RDU parking receipt matches RDUAA transaction

**Independent Test**: After P1 stories complete, trigger auto-match and confirm RDU match with >80% confidence

**Dependencies**: Requires US1 (receipts uploaded) and US2 (transactions imported) to complete first

### Implementation for User Story 3

- [X] T029 [US3] Document auto-match trigger procedure in specs/012-automated-uat-testing/uat-us3-matching.md
- [X] T030 [US3] Document proposal retrieval and validation logic in uat-us3-matching.md
- [X] T031 [US3] Document RDU parking match assertion (confidence >80%) in uat-us3-matching.md
- [X] T032 [US3] Document match confirmation procedure and receipt status update validation in uat-us3-matching.md
- [X] T033 [US3] Document unmatched receipt handling validation in uat-us3-matching.md
- [X] T033a [US3] Document amount tolerance matching test case in uat-us3-matching.md (per spec edge case: configurable tolerance, default $0.01)
- [X] T033b [US3] Document date window mismatch handling test case in uat-us3-matching.md (per spec edge case: remain unmatched with suggestions when no transaction in reasonable window)

**Checkpoint**: User Story 3 UAT documentation complete - Claude Code can execute matching tests

---

## Phase 6: User Story 4 - End-to-End Workflow Validation (Priority: P2)

**Goal**: Generate draft expense report and validate it includes matched expenses

**Independent Test**: After matching, request draft report and verify category summaries

**Dependencies**: Requires US3 (matches confirmed) to complete first

### Implementation for User Story 4

- [X] T034 [US4] Document draft report generation procedure in specs/012-automated-uat-testing/uat-us4-reports.md
- [X] T035 [US4] Document report content validation (matched expenses included) in uat-us4-reports.md
- [X] T036 [US4] Document category summary validation in uat-us4-reports.md
- [X] T037 [US4] Document unmatched receipt flagging validation in uat-us4-reports.md

**Checkpoint**: User Story 4 UAT documentation complete - Claude Code can execute report tests

---

## Phase 7: User Story 5 - Test Results Validation and Reporting (Priority: P3)

**Goal**: Ensure clear pass/fail JSON output with execution timing and error details

**Independent Test**: Complete a full test run and verify JSON output format matches schema

### Implementation for User Story 5

- [X] T038 [US5] Document test result JSON output schema in specs/012-automated-uat-testing/uat-us5-results.md
- [X] T039 [US5] Document pass/fail summary generation logic in uat-us5-results.md
- [X] T040 [US5] Document failure detail format (expected vs actual, assertion context) in uat-us5-results.md
- [X] T041 [US5] Document execution timing reporting in uat-us5-results.md
- [X] T042 [US5] Document skipped test handling (dependency failures) in uat-us5-results.md

**Checkpoint**: User Story 5 UAT documentation complete - Claude Code can produce structured test reports

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Integration, validation, and final documentation

- [X] T043 Create unified UAT execution guide consolidating all user story docs in specs/012-automated-uat-testing/uat-complete.md
- [X] T044 Update quickstart.md with links to individual user story UAT docs
- [X] T045 [P] Add environment variable documentation to quickstart.md (EXPENSEFLOW_API_URL, EXPENSEFLOW_API_KEY, EXPENSEFLOW_USER_TOKEN)
- [X] T046 [P] Add success criteria checklist (SC-001 through SC-007) to uat-complete.md
- [ ] T047 Execute full UAT suite against staging to validate end-to-end
- [ ] T048 Document any discovered issues or edge cases found during validation

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all idempotent user story testing
- **User Stories (Phase 3-7)**: All depend on Foundational phase completion
  - US1 (Receipts) and US2 (Statements) can run in parallel
  - US3 (Matching) depends on US1 + US2 completion
  - US4 (Reports) depends on US3 completion
  - US5 (Results) can run in parallel with US1-US4 (documents output format)
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

```
Setup (Phase 1)
    │
    ▼
Foundational (Phase 2) ─── Cleanup Endpoint
    │
    ├──────────────────┬─────────────────────────┐
    ▼                  ▼                         ▼
US1: Receipts (P1)  US2: Statements (P1)    US5: Results (P3)
    │                  │                     (parallel)
    └────────┬─────────┘
             ▼
      US3: Matching (P2)
             │
             ▼
      US4: Reports (P2)
             │
             ▼
      Polish (Phase 8)
```

### Within Each User Story

- Documentation tasks can be completed in sequence
- Each story's docs reference expected-values.json for assertions
- Story complete when Claude Code can execute that test segment

### Parallel Opportunities

- T002, T003, T004, T005 can run in parallel (different sections of expected-values.json)
- US1 (Receipts) and US2 (Statements) can be documented in parallel
- US5 (Results) can be documented in parallel with US1-US4
- T045, T046 can run in parallel (different files)

---

## Parallel Example: Phase 1 Setup

```bash
# Launch expected values tasks in parallel:
Task: "Add RDU parking receipt expected values to test-data/expected-values.json"
Task: "Add PDF receipt expected values to test-data/expected-values.json"
Task: "Add statement import expectations to test-data/expected-values.json"
Task: "Add RDU transaction expected values to test-data/expected-values.json"
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2 Only)

1. Complete Phase 1: Setup (expected-values.json)
2. Complete Phase 2: Foundational (cleanup endpoint)
3. Complete Phase 3: User Story 1 - Receipt Upload
4. Complete Phase 4: User Story 2 - Statement Import
5. **STOP and VALIDATE**: Claude Code can now test receipts and statements independently
6. This MVP validates the two most critical pipelines

### Incremental Delivery

1. Complete Setup + Foundational → Test infrastructure ready
2. Add US1 + US2 → Test receipt and statement pipelines (MVP!)
3. Add US3 → Test matching engine
4. Add US4 → Test report generation
5. Add US5 → Structured test output
6. Polish → Production-ready UAT suite

### Task Counts by User Story

| Phase | Story | Task Count | Description |
|-------|-------|------------|-------------|
| 1 | Setup | 6 | Expected values file creation |
| 2 | Foundational | 9 | Cleanup endpoint implementation |
| 3 | US1 (P1) | 8 | Receipt upload UAT docs (incl. edge cases) |
| 4 | US2 (P1) | 7 | Statement import UAT docs (incl. edge cases) |
| 5 | US3 (P2) | 7 | Matching UAT docs (incl. edge cases) |
| 6 | US4 (P2) | 4 | Report generation UAT docs |
| 7 | US5 (P3) | 5 | Results format UAT docs |
| 8 | Polish | 6 | Integration and validation |
| **Total** | | **52** | |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story UAT doc should be independently executable by Claude Code
- The cleanup endpoint (Phase 2) is the only new code—everything else is configuration and documentation
- Expected values in JSON enable easy updates without code changes
- Run cleanup before each test run for idempotency
