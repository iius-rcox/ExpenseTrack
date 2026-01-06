# Tasks: Async Report Generation

**Input**: Design documents from `/specs/027-async-report-generation/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Per Constitution Principle II - unit tests for services, integration tests for API endpoints.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Backend**: `backend/src/ExpenseFlow.{Core,Infrastructure,Api,Shared}/`
- **Frontend**: `frontend/src/`
- Based on plan.md project structure

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Database migration and core entity creation

- [ ] T001 Create ReportJobStatus enum in `backend/src/ExpenseFlow.Core/Entities/ReportJobStatus.cs`
- [ ] T002 Create ReportGenerationJob entity in `backend/src/ExpenseFlow.Core/Entities/ReportGenerationJob.cs`
- [ ] T003 Create ReportGenerationJobConfiguration in `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/ReportGenerationJobConfiguration.cs`
- [ ] T004 Add DbSet<ReportGenerationJob> to ExpenseFlowDbContext in `backend/src/ExpenseFlow.Infrastructure/Data/ExpenseFlowDbContext.cs`
- [ ] T005 Generate EF Core migration using `dotnet ef migrations add AddReportGenerationJobs`
- [ ] T006 Apply migration to staging database (see data-model.md for SQL)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [ ] T007 [P] Create IReportJobRepository interface in `backend/src/ExpenseFlow.Core/Interfaces/IReportJobRepository.cs`
- [ ] T008 [P] Create ReportJobDto in `backend/src/ExpenseFlow.Shared/DTOs/ReportJobDto.cs`
- [ ] T009 [P] Create CreateReportJobRequest in `backend/src/ExpenseFlow.Shared/DTOs/CreateReportJobRequest.cs`
- [ ] T010 [P] Create ReportJobListResponse in `backend/src/ExpenseFlow.Shared/DTOs/ReportJobListResponse.cs`
- [ ] T011 Implement ReportJobRepository in `backend/src/ExpenseFlow.Infrastructure/Repositories/ReportJobRepository.cs`
- [ ] T012 Create IReportJobService interface in `backend/src/ExpenseFlow.Core/Interfaces/IReportJobService.cs`
- [ ] T013 Implement ReportJobService (job creation, status, list, cancel) in `backend/src/ExpenseFlow.Infrastructure/Services/ReportJobService.cs`
- [ ] T014 Register IReportJobRepository and IReportJobService in DI container in `backend/src/ExpenseFlow.Infrastructure/Extensions/ServiceCollectionExtensions.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Generate Report with Progress Visibility (Priority: P1) 🎯 MVP

**Goal**: Users can initiate async report generation and see real-time progress updates

**Independent Test**: Trigger report generation via API, poll for status, verify progress updates appear every 2-5 seconds, and final report is accessible upon completion

### Backend Implementation for User Story 1

- [ ] T015 [US1] Create GenerateReportRequestValidator in `backend/src/ExpenseFlow.Api/Validators/GenerateReportRequestValidator.cs`
- [ ] T016 [US1] Create ReportJobsController with POST endpoint (returns 202 Accepted) in `backend/src/ExpenseFlow.Api/Controllers/ReportJobsController.cs`
- [ ] T017 [US1] Add GET /report-jobs/{id} endpoint for status polling to ReportJobsController
- [ ] T018 [US1] Create ReportGenerationBackgroundJob (Hangfire job) in `backend/src/ExpenseFlow.Infrastructure/Jobs/ReportGenerationBackgroundJob.cs`
- [ ] T019 [US1] Refactor ReportService.GenerateDraftAsync to support progress callbacks in `backend/src/ExpenseFlow.Infrastructure/Services/ReportService.cs`
- [ ] T020 [US1] Implement progress update logic (every 10 lines or 5 seconds) in ReportGenerationBackgroundJob
- [ ] T021 [US1] Add estimated completion time calculation based on processing rate in ReportGenerationBackgroundJob
- [ ] T022 [US1] Add Serilog logging for job start, progress, and completion in ReportGenerationBackgroundJob

### Frontend Implementation for User Story 1

- [ ] T023 [P] [US1] Create useReportJob hook with adaptive polling in `frontend/src/hooks/queries/use-report-jobs.ts`
- [ ] T024 [P] [US1] Create useCreateReportJob mutation hook in `frontend/src/hooks/queries/use-report-jobs.ts`
- [ ] T025 [US1] Create ReportGenerationProgress component in `frontend/src/components/reports/report-generation-progress.tsx`
- [ ] T026 [US1] Integrate ReportGenerationProgress into reports page/flow (update existing report generation trigger)
- [ ] T027 [US1] Add completion callback to navigate to generated report when job completes

**Checkpoint**: User Story 1 complete - users can generate reports with progress visibility

---

## Phase 3b: Testing (Constitution II Compliance)

**Purpose**: Unit and integration tests per Constitution Principle II

- [ ] T027a [US1] Create ReportJobServiceTests (unit) in `backend/tests/ExpenseFlow.UnitTests/Services/ReportJobServiceTests.cs`
- [ ] T027b [US1] Create ReportJobsControllerTests (integration) in `backend/tests/ExpenseFlow.IntegrationTests/Controllers/ReportJobsControllerTests.cs`
- [ ] T027c [US1] Create ReportGenerationBackgroundJobTests (unit) in `backend/tests/ExpenseFlow.UnitTests/Jobs/ReportGenerationBackgroundJobTests.cs`

**Test Coverage Requirements**:
- Unit tests: job creation, status transitions, duplicate prevention, cancellation logic
- Integration tests: POST returns 202, GET returns progress, DELETE cancels, 409 on duplicate

**Checkpoint**: US1 tests passing - Constitution II compliance verified

---

## Phase 4: User Story 2 - Graceful Handling of Processing Delays (Priority: P2)

**Goal**: System handles rate limiting gracefully with exponential backoff and fallback values

**Independent Test**: Generate report during high-load conditions, verify it completes without user-visible errors, and lines with failed categorization are flagged

### Backend Implementation for User Story 2

- [ ] T028 [US2] Enhance Polly resilience pipeline to handle HTTP 429 responses in `backend/src/ExpenseFlow.Infrastructure/Extensions/ServiceCollectionExtensions.cs`
- [ ] T029 [US2] Add jitter and increased retry attempts (5) for rate limit resilience in ServiceCollectionExtensions.cs
- [ ] T030 [US2] Implement per-line retry tracking and fallback logic in ReportGenerationBackgroundJob
- [ ] T031 [US2] Add RequiresManualCategorization flag handling for failed lines in ExpenseLine entity (if not exists)
- [ ] T032 [US2] Implement dynamic throttling (pause 30s if 429 rate > 20%) in ReportGenerationBackgroundJob
- [ ] T033 [US2] Update estimated completion time dynamically based on actual processing rate

### Frontend Implementation for User Story 2

- [ ] T034 [US2] Display failed line count in ReportGenerationProgress component
- [ ] T035 [US2] Show dynamic estimated completion time updates in ReportGenerationProgress component
- [ ] T036 [US2] Add visual indicator for lines requiring manual categorization on completed report

**Checkpoint**: User Story 2 complete - rate limiting is handled gracefully

---

## Phase 5: User Story 3 - View Generation History and Status (Priority: P3)

**Goal**: Users can view their report generation job history with status and details

**Independent Test**: Generate multiple reports, view job history page, verify all jobs shown with correct status, timestamps, and duration

### Backend Implementation for User Story 3

- [ ] T037 [US3] Add GET /report-jobs endpoint (list with pagination) to ReportJobsController
- [ ] T038 [US3] Add status filter query parameter to list endpoint
- [ ] T039 [US3] Implement GetListAsync in ReportJobService with pagination

### Frontend Implementation for User Story 3

- [ ] T040 [P] [US3] Create useReportJobList hook in `frontend/src/hooks/queries/use-report-jobs.ts`
- [ ] T041 [US3] Create JobHistoryPanel component in `frontend/src/components/reports/job-history-panel.tsx`
- [ ] T042 [US3] Display job history with status, start time, duration, and error messages
- [ ] T043 [US3] Add link to generated report for completed jobs

**Checkpoint**: User Story 3 complete - job history is viewable

---

## Phase 6: User Story 4 - Cancel In-Progress Generation (Priority: P4)

**Goal**: Users can cancel active report generation jobs

**Independent Test**: Start generation, click cancel, verify job stops within 10 seconds and no partial report is created

### Backend Implementation for User Story 4

- [ ] T044 [US4] Add DELETE /report-jobs/{id} endpoint (cancel) to ReportJobsController
- [ ] T045 [US4] Implement CancelAsync in ReportJobService (set status to CancellationRequested)
- [ ] T046 [US4] Add cancellation check in ReportGenerationBackgroundJob processing loop (ShouldCancelAsync)
- [ ] T047 [US4] Handle cleanup on cancellation (delete partial report if exists)

### Frontend Implementation for User Story 4

- [ ] T048 [P] [US4] Create useCancelReportJob mutation hook in `frontend/src/hooks/queries/use-report-jobs.ts`
- [ ] T049 [US4] Add Cancel button to ReportGenerationProgress component
- [ ] T050 [US4] Show "Cancelled by user" status in job history

**Checkpoint**: User Story 4 complete - cancellation works

---

## Phase 7: User Story 5 - Pre-warmed Categorization Cache (Priority: P5)

**Goal**: Nightly cache warming job pre-computes categorizations for faster report generation

**Independent Test**: Run cache warming job, generate report for same vendors, verify categorization is instant (no AI call)

### Backend Implementation for User Story 5

- [ ] T051 [US5] Create ReportJobCleanupJob (30-day retention) in `backend/src/ExpenseFlow.Infrastructure/Jobs/ReportJobCleanupJob.cs`
- [ ] T052 [US5] Register ReportJobCleanupJob as Hangfire recurring job (3 AM daily) in Program.cs or HangfireConfiguration
- [ ] T053 [US5] Enhance existing CacheWarmingJob to pre-compute categorizations for recent transactions
- [ ] T054 [US5] Add cache hit logging to categorization service for observability

**Checkpoint**: User Story 5 complete - cache warming operational

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T055 [P] Add duplicate job prevention check (409 Conflict) to POST /report-jobs endpoint
- [ ] T056 [P] Validate empty transaction case (immediate feedback, no job created)
- [ ] T057 [P] Add operations alert for job failure rate > 10% (FR-012) in monitoring configuration
- [ ] T058 Run quickstart.md validation scenarios
- [ ] T059 Update existing report generation UI to use async flow (deprecate sync endpoint if applicable)
- [ ] T060 Commit all changes with descriptive commit message

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-7)**: All depend on Foundational phase completion
  - User stories can proceed sequentially in priority order (P1 → P2 → P3 → P4 → P5)
  - Some stories can run in parallel if staffed (US3 and US4 are independent)
- **Polish (Phase 8)**: Depends on at least US1 and US2 being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational - No dependencies on other stories - **MVP**
- **User Story 2 (P2)**: Builds on US1 (enhances resilience) - Can be tested independently
- **User Story 3 (P3)**: Can start after Foundational - Independent of US1/US2
- **User Story 4 (P4)**: Requires US1 infrastructure (ReportGenerationBackgroundJob)
- **User Story 5 (P5)**: Independent - can be developed alongside other stories

### Within Each User Story

- Backend before frontend (API must exist for frontend to consume)
- Core entities/interfaces before implementations
- Services before controllers
- Hooks before components

### Parallel Opportunities

- All DTOs in Phase 2 (T007-T010) can run in parallel
- Frontend hooks for each story marked [P] can run in parallel within that story
- US3 and US4 can run in parallel after US1 completes
- All [P] tasks in Phase 8 can run in parallel

---

## Parallel Example: User Story 1

```bash
# Launch all parallelizable US1 tasks together:
Task T023: "Create useReportJob hook in frontend/src/hooks/queries/use-report-jobs.ts"
Task T024: "Create useCreateReportJob mutation hook in frontend/src/hooks/queries/use-report-jobs.ts"

# Note: T023 and T024 can run in parallel as they're in the same file but define different exports
# However, T025-T027 depend on hooks being complete
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T006)
2. Complete Phase 2: Foundational (T007-T014)
3. Complete Phase 3: User Story 1 (T015-T027)
4. **STOP and VALIDATE**: Test async report generation end-to-end
5. Deploy/demo if ready - Users get immediate feedback and progress visibility

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy (MVP!)
3. Add User Story 2 → Rate limiting handled → Deploy
4. Add User Story 3 → Job history visible → Deploy
5. Add User Story 4 → Cancellation works → Deploy
6. Add User Story 5 → Cache warming → Deploy
7. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (critical path - MVP)
   - Developer B: User Story 3 (can start in parallel - no US1 dependency)
3. After US1:
   - Developer A: User Story 2 (builds on US1 infrastructure)
   - Developer B: User Story 4 (uses US1 background job)
4. User Story 5 can be done by either developer

---

## Summary

| Phase | Story | Task Count | Key Deliverable |
|-------|-------|------------|-----------------|
| 1 | Setup | 6 | Database migration, entity |
| 2 | Foundational | 8 | Repository, service, DTOs |
| 3 | US1 (P1) 🎯 | 13 | Async generation + progress |
| 3b | Testing | 3 | Constitution II compliance |
| 4 | US2 (P2) | 9 | Rate limit resilience |
| 5 | US3 (P3) | 7 | Job history |
| 6 | US4 (P4) | 7 | Cancellation |
| 7 | US5 (P5) | 4 | Cache warming |
| 8 | Polish | 6 | Edge cases, alerts |

**Total Tasks**: 63
**MVP Scope**: Phase 1 + 2 + 3 + 3b = 30 tasks
**Parallel Opportunities**: 12 tasks marked [P]

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
