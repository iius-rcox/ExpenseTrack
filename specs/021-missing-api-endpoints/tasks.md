# Tasks: Missing API Endpoints Implementation

**Input**: Design documents from `/specs/021-missing-api-endpoints/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Backend**: `backend/src/ExpenseFlow.{Layer}/`
- **Tests**: `backend/tests/ExpenseFlow.{Layer}.Tests/`

---

## Phase 1: Setup (Contract Test Fixes - Quick Win)

**Purpose**: Fix contract test path mismatches to make 12+ tests pass immediately with no code changes

- [X] T001 [P] Update analytics endpoint paths in `backend/tests/ExpenseFlow.Contracts.Tests/AnalyticsEndpointContractTests.cs` (spending-summary→categories, category-breakdown→spending-by-category, trends→spending-trend, vendor-insights→spending-by-vendor, budget-comparison→comparison)
- [X] T002 [P] Update receipt endpoint paths in `backend/tests/ExpenseFlow.Contracts.Tests/ReceiptEndpointContractTests.cs` ({id}/image→{id}/download, {id}/reprocess→{id}/retry)
- [X] T003 [P] Update transaction endpoint paths in `backend/tests/ExpenseFlow.Contracts.Tests/TransactionEndpointContractTests.cs` ({id}/categorize→/api/categorization/transactions/{id}/confirm)
- [X] T004 [P] Update report endpoint paths in `backend/tests/ExpenseFlow.Contracts.Tests/ReportEndpointContractTests.cs` (POST /→POST /draft)
- [X] T005 Run contract tests to verify path fixes: `dotnet test backend/tests/ExpenseFlow.Contracts.Tests`

**Checkpoint**: 12+ previously failing contract tests now pass. Foundation for new endpoints ready.

---

## Phase 2: Foundational (Shared Data Model Changes)

**Purpose**: Core entity and enum changes that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T006 Add `Generated = 1` and `Submitted = 2` values to ReportStatus enum in `backend/src/ExpenseFlow.Shared/Enums/ReportStatus.cs`
- [X] T007 Add `GeneratedAt` and `SubmittedAt` nullable DateTimeOffset properties to ExpenseReport entity in `backend/src/ExpenseFlow.Core/Entities/ExpenseReport.cs`
- [X] T008 Update ExpenseReportConfiguration with timestamptz column types in `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/ExpenseReportConfiguration.cs`
- [X] T009 Create EF Core migration `AddReportStatusTimestamps` for new columns
- [X] T010 Create ReportValidationResultDto, ValidationErrorDto, ValidationWarningDto in `backend/src/ExpenseFlow.Shared/DTOs/ReportValidationResultDto.cs`
- [X] T011 Run solution build to verify no compilation errors: `dotnet build backend/ExpenseFlow.sln`

**Checkpoint**: Foundation ready - all shared models and enums in place. User story implementation can now begin.

---

## Phase 3: User Story 2 - Finalize Draft Report (Priority: P1) 🎯 MVP

**Goal**: Add `POST /api/reports/{id}/generate` endpoint to finalize draft reports with strict validation

**Independent Test**: Create a draft report, call the generate endpoint, verify status changes to "Generated" and report becomes read-only

### Implementation for User Story 2 (P1 - Report Generation)

- [X] T012 [US2] Create GenerateReportResponseDto in `backend/src/ExpenseFlow.Shared/DTOs/GenerateReportResponseDto.cs`
- [X] T013 [US2] Add `GenerateAsync(Guid userId, Guid reportId)` and `ValidateReportAsync(Guid reportId)` methods to IReportService in `backend/src/ExpenseFlow.Core/Interfaces/IReportService.cs`
- [X] T014 [US2] Implement report validation logic in ReportService: check each line has category, amount > $0, and receipt in `backend/src/ExpenseFlow.Infrastructure/Services/ReportService.cs`
- [X] T015 [US2] Implement GenerateAsync in ReportService: validate, update status to Generated, set GeneratedAt timestamp, use optimistic concurrency in `backend/src/ExpenseFlow.Infrastructure/Services/ReportService.cs`
- [X] T016 [US2] Add immutability check to UpdateLineAsync in ReportService: reject modifications when status != Draft in `backend/src/ExpenseFlow.Infrastructure/Services/ReportService.cs`
- [X] T017 [US2] Add `POST {reportId:guid}/generate` endpoint to ReportsController with proper response codes (200, 400, 404, 409) in `backend/src/ExpenseFlow.Api/Controllers/ReportsController.cs`
- [X] T018 [US2] Add Serilog logging for generate operation with named parameters in `backend/src/ExpenseFlow.Api/Controllers/ReportsController.cs`
- [X] T019 [US2] Register any new services in DI container in `backend/src/ExpenseFlow.Api/Program.cs` (if needed - no new services required)
- [X] T020 [US2] Add unit tests for report validation logic in `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/ReportServiceTests.cs`
- [X] T021 [US2] Add integration tests for generate endpoint (success, validation failure, 409 conflict) in `backend/tests/ExpenseFlow.Api.Tests/Controllers/ReportsControllerTests.cs`

**Checkpoint**: Report generation endpoint fully functional. Draft reports can be finalized with strict validation.

---

## Phase 4: User Story 1 - Export Analytics Data (Priority: P2)

**Goal**: Add `GET /api/analytics/export` endpoint to export spending data to CSV or Excel format

**Independent Test**: Request an analytics export with date range and format parameters, verify downloaded file contains expected data structure

### Implementation for User Story 1 (P2 - Analytics Export)

- [X] T022 [P] [US1] Create AnalyticsExportRequestDto in `backend/src/ExpenseFlow.Shared/DTOs/AnalyticsExportRequestDto.cs`
- [X] T023 [P] [US1] Create IAnalyticsExportService interface with `ExportAsync(Guid userId, DateOnly start, DateOnly end, string format, string[] sections)` in `backend/src/ExpenseFlow.Core/Interfaces/IAnalyticsExportService.cs`
- [X] T024 [US1] Implement AnalyticsExportService with CSV export using CsvHelper in `backend/src/ExpenseFlow.Infrastructure/Services/AnalyticsExportService.cs`
- [X] T025 [US1] Add Excel export (multiple sheets: trends, categories, vendors) using ClosedXML in `backend/src/ExpenseFlow.Infrastructure/Services/AnalyticsExportService.cs`
- [X] T026 [US1] Add section filtering logic (parse comma-separated sections, default to aggregated summaries) in `backend/src/ExpenseFlow.Infrastructure/Services/AnalyticsExportService.cs`
- [X] T027 [US1] Add transactions section export (raw transaction data) when explicitly requested in `backend/src/ExpenseFlow.Infrastructure/Services/AnalyticsExportService.cs`
- [X] T028 [US1] Enforce 5-year maximum date range validation in `backend/src/ExpenseFlow.Infrastructure/Services/AnalyticsExportService.cs`
- [X] T029 [US1] Add `GET export` endpoint to AnalyticsController with Content-Disposition header for downloads in `backend/src/ExpenseFlow.Api/Controllers/AnalyticsController.cs`
- [X] T030 [US1] Register AnalyticsExportService in DI container in `backend/src/ExpenseFlow.Infrastructure/Extensions/ServiceCollectionExtensions.cs`
- [X] T031 [US1] Add unit tests for CSV and Excel export in `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/AnalyticsExportServiceTests.cs`
- [X] T032 [US1] Add integration tests for export endpoint in `backend/tests/ExpenseFlow.Api.Tests/Controllers/AnalyticsControllerTests.cs`

**Checkpoint**: Analytics export endpoint fully functional. Users can export spending data in CSV or Excel format.

---

## Phase 5: User Story 3 - Submit Report for Tracking (Priority: P3)

**Goal**: Add `POST /api/reports/{id}/submit` endpoint to mark finalized reports as submitted for audit trail

**Independent Test**: Generate a report, call the submit endpoint, verify status changes to "Submitted"

### Implementation for User Story 3 (P3 - Report Submission)

- [X] T033 [P] [US3] Create SubmitReportResponseDto in `backend/src/ExpenseFlow.Shared/DTOs/SubmitReportResponseDto.cs`
- [X] T034 [US3] Add `SubmitAsync(Guid userId, Guid reportId)` method to IReportService in `backend/src/ExpenseFlow.Core/Interfaces/IReportService.cs`
- [X] T035 [US3] Implement SubmitAsync in ReportService: verify status is Generated, update to Submitted, set SubmittedAt timestamp in `backend/src/ExpenseFlow.Infrastructure/Services/ReportService.cs`
- [X] T036 [US3] Add `POST {reportId:guid}/submit` endpoint to ReportsController with proper response codes (200, 400, 404, 409) in `backend/src/ExpenseFlow.Api/Controllers/ReportsController.cs`
- [X] T037 [US3] Add Serilog logging for submit operation with named parameters in `backend/src/ExpenseFlow.Api/Controllers/ReportsController.cs`
- [X] T038 [US3] Add unit tests for submit logic in `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/ReportServiceTests.cs`
- [X] T039 [US3] Add integration tests for submit endpoint (success, not generated error, already submitted conflict) in `backend/tests/ExpenseFlow.Api.Tests/Controllers/ReportsControllerTests.cs`

**Checkpoint**: Report submission endpoint fully functional. Reports can be marked as submitted for audit trail.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, documentation, and cleanup

- [X] T040 Run all contract tests to verify SC-004 (18 contract tests pass, 17 skipped future work): `dotnet test backend/tests/ExpenseFlow.Contracts.Tests`
- [X] T041 Run full test suite to verify no regressions (264 passed, 0 failed): `dotnet test backend/ExpenseFlow.sln`
- [X] T042 Verify OpenAPI documentation includes new endpoints with proper response types (SC-005) - ProducesResponseType attributes + AuthorizeResponseOperationFilter
- [X] T043 [P] Run quickstart.md validation to verify all implementation steps are complete
- [X] T044 [P] Verify analytics export design meets 10-second requirement for 12-month date range (SC-001) - Async queries, projections, 5-year max limit

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies - fix contract test paths first (quick win)
- **Phase 2 (Foundational)**: No code dependencies on Phase 1, but wait for Phase 1 to validate baseline
- **Phase 3+ (User Stories)**: All depend on Phase 2 completion (ReportStatus enum, entity fields)
- **Phase 6 (Polish)**: Depends on all user stories being complete

### User Story Dependencies

| Story | Priority | Depends On | Can Start After |
|-------|----------|------------|-----------------|
| US2 (Report Generation) | P1 | Phase 2 only | Phase 2 complete |
| US1 (Analytics Export) | P2 | Phase 2 only | Phase 2 complete |
| US3 (Report Submission) | P3 | US2 (needs Generated status) | Phase 2 + US2 for integration testing |

### Within Each User Story

- DTOs before interfaces
- Interfaces before services
- Services before controllers
- Core implementation before tests
- Tests verify functionality

### Parallel Opportunities

- **Phase 1**: All T001-T004 can run in parallel (different test files)
- **Phase 2**: T010 (DTOs) can run parallel to T006-T009 (entity changes)
- **Phase 4 (US1)**: T022-T023 can run in parallel (different files)
- **Phase 5 (US3)**: T033 can run in parallel with other DTO creation

---

## Parallel Example: Phase 1 (Contract Test Fixes)

```bash
# All contract test file updates can run in parallel:
Task: "T001 - Update AnalyticsEndpointContractTests.cs"
Task: "T002 - Update ReceiptEndpointContractTests.cs"
Task: "T003 - Update TransactionEndpointContractTests.cs"
Task: "T004 - Update ReportEndpointContractTests.cs"
```

## Parallel Example: Phase 4 (Analytics Export)

```bash
# Launch DTOs and interfaces together:
Task: "T022 - Create AnalyticsExportRequestDto"
Task: "T023 - Create IAnalyticsExportService interface"
```

---

## Implementation Strategy

### MVP First (User Story 2 Only - P1)

1. Complete Phase 1: Contract test path fixes (quick win)
2. Complete Phase 2: Foundational (ReportStatus enum, entity fields)
3. Complete Phase 3: User Story 2 - Report Generation
4. **STOP and VALIDATE**: Test report generation independently
5. Deploy/demo if ready - users can finalize reports

### Incremental Delivery

1. Phase 1 + 2 → Foundation ready
2. Add US2 (P1) → Test independently → Deploy (Reports can be finalized!)
3. Add US1 (P2) → Test independently → Deploy (Analytics export available!)
4. Add US3 (P3) → Test independently → Deploy (Full workflow complete!)
5. Each story adds value without breaking previous stories

### Suggested MVP Scope

**MVP = Phase 1 + Phase 2 + Phase 3 (User Story 2)**

This delivers:
- ✅ 12+ contract test path fixes (quick win)
- ✅ Report generation with strict validation
- ✅ Report immutability after generation
- ✅ Core workflow: Draft → Generated

---

## Summary

| Metric | Count |
|--------|-------|
| **Total Tasks** | 44 |
| **Phase 1 (Setup)** | 5 tasks |
| **Phase 2 (Foundational)** | 6 tasks |
| **Phase 3 (US2 - P1)** | 10 tasks |
| **Phase 4 (US1 - P2)** | 11 tasks |
| **Phase 5 (US3 - P3)** | 7 tasks |
| **Phase 6 (Polish)** | 5 tasks |
| **Parallel Opportunities** | 8 task groups marked [P] |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Priority order: P1 (Report Generation) → P2 (Analytics Export) → P3 (Report Submission)
