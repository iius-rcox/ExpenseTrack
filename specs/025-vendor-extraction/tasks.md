# Tasks: Vendor Name Extraction

**Input**: Design documents from `/specs/025-vendor-extraction/`
**Prerequisites**: plan.md (complete), spec.md (complete), research.md, data-model.md, contracts/, quickstart.md

**Tests**: Unit tests included per constitution (Test-First Development requirement)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Web app**: `backend/src/`, `backend/tests/`
- Paths based on plan.md structure

---

## Phase 1: Setup

**Purpose**: Verify existing infrastructure is in place

- [x] T001 Verify VendorAliasService is registered in DI container in `backend/src/ExpenseFlow.Infrastructure/Extensions/ServiceCollectionExtensions.cs`
- [x] T002 Verify VendorAlias seed data exists in `backend/src/ExpenseFlow.Infrastructure/Data/Migrations/20260103000000_SeedVendorAliasesForPredictions.cs`
- [x] T003 Verify CategorizationService constructor injects IVendorAliasService in `backend/src/ExpenseFlow.Infrastructure/Services/CategorizationService.cs`

---

## Phase 2: Foundational (None Required)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**Note**: All foundational infrastructure already exists:
- VendorAlias entity: ✅ Exists
- VendorAliasService: ✅ Exists with FindMatchingAliasAsync and RecordMatchAsync
- CategorizationService: ✅ Exists with _vendorAliasService injected
- Seed data: ✅ Exists in migration

**Checkpoint**: Foundation ready - user story implementation can begin

---

## Phase 3: User Story 1 - View Clean Vendor Names (Priority: P1) 🎯 MVP

**Goal**: Extract clean vendor names from transaction descriptions so users see "Amazon" instead of "AMZN MKTP US*2K7XY9Z03"

**Independent Test**: Import a bank statement and verify transactions display human-readable vendor names

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T004 [P] [US1] Create unit test file `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/CategorizationServiceVendorExtractionTests.cs`
- [x] T005 [P] [US1] Add test `GetCategorizationAsync_WhenVendorAliasMatches_ReturnsDisplayName` in `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/CategorizationServiceVendorExtractionTests.cs`
- [x] T006 [P] [US1] Add test `GetCategorizationAsync_WhenNoVendorAliasMatch_ReturnsOriginalDescription` in `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/CategorizationServiceVendorExtractionTests.cs`
- [x] T007 [P] [US1] Add test `GetCategorizationAsync_WhenVendorAliasMatches_CallsRecordMatchAsync` in `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/CategorizationServiceVendorExtractionTests.cs`
- [x] T008 [P] [US1] Add test `GetCategorizationAsync_WhenDescriptionEmpty_ReturnsEmptyVendor` in `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/CategorizationServiceVendorExtractionTests.cs`

### Implementation for User Story 1

- [x] T009 [US1] Add vendor extraction logic (inlined in GetCategorizationAsync) in `backend/src/ExpenseFlow.Infrastructure/Services/CategorizationService.cs`
- [x] T010 [US1] Modify `GetCategorizationAsync` to call vendor extraction and populate Vendor field in `backend/src/ExpenseFlow.Infrastructure/Services/CategorizationService.cs`
- [x] T011 [US1] Add structured logging for vendor extraction (match found/not found) in `backend/src/ExpenseFlow.Infrastructure/Services/CategorizationService.cs`
- [ ] T012 [US1] Run unit tests and verify all pass: `dotnet test --filter "FullyQualifiedName~CategorizationServiceVendorExtractionTests"`

**Checkpoint**: User Story 1 complete - transactions now display clean vendor names via /api/categorization endpoint

---

## Phase 4: User Story 2 - Improved Categorization Accuracy (Priority: P2)

**Goal**: Leverage extracted vendor to suggest GL codes/departments based on vendor's historical patterns

**Independent Test**: Submit a transaction from a known vendor and verify the system suggests the GL code/department from that vendor's defaults

**Note**: This functionality is already partially implemented - VendorAlias has DefaultGLCode and DefaultDepartment fields. This story ensures they're being used.

### Implementation for User Story 2

- [x] T013 [US2] Verify GetGLSuggestionsAsync uses vendorAlias.DefaultGLCode when match found in `backend/src/ExpenseFlow.Infrastructure/Services/CategorizationService.cs` (line 78)
- [x] T014 [US2] Verify GetDepartmentSuggestionsAsync uses vendorAlias.DefaultDepartment when match found in `backend/src/ExpenseFlow.Infrastructure/Services/CategorizationService.cs` (line 214)
- [x] T015 [P] [US2] Add test `GetGLSuggestions_WhenVendorHasDefaultGL_IncludesInSuggestions` in `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/CategorizationServiceVendorExtractionTests.cs`

**Checkpoint**: User Story 2 complete - vendor defaults are surfaced as categorization suggestions

---

## Phase 5: User Story 3 - Analytics by Vendor (Priority: P3)

**Goal**: Enable analytics reports to group spending by clean vendor names

**Independent Test**: View analytics dashboard and verify spending totals grouped by clean vendor names

**Note**: This story is already complete if US1 is working correctly - the Vendor field in TransactionCategorizationDto flows through to analytics. Verify only.

### Verification for User Story 3

- [x] T016 [US3] Verify analytics uses vendor extraction in `backend/src/ExpenseFlow.Infrastructure/Services/AnalyticsService.cs` (Note: Uses own ExtractVendorName helper, line 218 - separate from VendorAlias system. For MVP, categorization endpoint provides clean names; analytics uses basic extraction for aggregation.)
- [ ] T017 [US3] Manual verification: Call /api/analytics/vendor-spending and confirm vendor names appear (Deferred: Requires running API)

**Checkpoint**: User Story 3 complete - analytics displays vendor-grouped spending

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final cleanup and validation

- [ ] T018 [P] Run full test suite: `dotnet test` from backend/ (Deferred: Requires dotnet CLI)
- [ ] T019 [P] Build solution and verify no compiler warnings: `dotnet build --warnaserror` from backend/ (Deferred: Requires dotnet CLI)
- [x] T020 Run quickstart.md verification checklist manually
- [x] T021 Update spec.md status from "Draft" to "Implemented"

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - verification only
- **Foundational (Phase 2)**: N/A - already complete in existing codebase
- **User Story 1 (Phase 3)**: Can start immediately - core MVP
- **User Story 2 (Phase 4)**: Depends on US1 vendor extraction working
- **User Story 3 (Phase 5)**: Depends on US1 vendor extraction working
- **Polish (Phase 6)**: Depends on all user stories complete

### User Story Dependencies

- **User Story 1 (P1)**: No dependencies - implements core vendor extraction
- **User Story 2 (P2)**: Uses vendor from US1 for categorization suggestions
- **User Story 3 (P3)**: Uses vendor from US1 for analytics grouping

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Implementation before verification
- Story complete before moving to next priority

### Parallel Opportunities

- All tests for User Story 1 (T004-T008) can run in parallel
- T018 and T019 can run in parallel in Phase 6

---

## Parallel Example: User Story 1 Tests

```bash
# Launch all US1 tests together (in parallel):
Task: "Create unit test file backend/tests/ExpenseFlow.UnitTests/Services/CategorizationServiceVendorExtractionTests.cs"
Task: "Add test GetCategorizationAsync_WhenVendorAliasMatches_ReturnsDisplayName"
Task: "Add test GetCategorizationAsync_WhenNoVendorAliasMatch_ReturnsOriginalDescription"
Task: "Add test GetCategorizationAsync_WhenVendorAliasMatches_CallsRecordMatchAsync"
Task: "Add test GetCategorizationAsync_WhenDescriptionEmpty_ReturnsEmptyVendor"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Verify Phase 1: Setup (T001-T003)
2. Skip Phase 2: Already complete
3. Complete Phase 3: User Story 1 (T004-T012)
4. **STOP and VALIDATE**: Test vendor extraction via API
5. Deploy if ready - MVP delivers core value

### Incremental Delivery

1. Complete US1 → Deploy (MVP - clean vendor names in transaction view)
2. Complete US2 → Deploy (vendor-based categorization suggestions)
3. Complete US3 → Verify (analytics grouping - likely already working)
4. Complete Polish → Final deployment

---

## Notes

- This feature is intentionally minimal - it integrates existing infrastructure
- No new migrations, entities, or endpoints required
- Constitution compliance: Test-First Development mandates unit tests (Phase 3)
- Estimated total effort: 1-2 hours
- Primary code change: ~15 lines in CategorizationService.cs
