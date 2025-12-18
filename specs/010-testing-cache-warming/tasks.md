# Tasks: Testing & Cache Warming

**Input**: Design documents from `/specs/010-testing-cache-warming/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Unit tests for new services. Load tests as part of US3 implementation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Backend**: `backend/src/ExpenseFlow.{Layer}/`
- **Tests**: `backend/tests/ExpenseFlow.{Layer}.Tests/`
- **Infrastructure**: `infrastructure/kubernetes/staging/`
- **Documentation**: `docs/uat/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and package dependencies for Sprint 10

- [x] T001 ~~Add ClosedXML package~~ VERIFIED: ClosedXML 0.102.2 already in ExpenseFlow.Infrastructure.csproj (Sprint 4)
- [x] T002 Create ExpenseFlow.LoadTests project in `backend/tests/ExpenseFlow.LoadTests/`
- [x] T003 [P] Add NBomber and NBomber.Http packages to load test project
- [x] T004 [P] Add project reference from LoadTests to ExpenseFlow.Shared

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**CRITICAL**: US4 (Staging Environment Setup) is a foundational prerequisite for US2 and US3

- [x] T005 [US4] ~~Create staging namespace manifest~~ VERIFIED: `infrastructure/namespaces/expenseflow-staging.yaml` already exists
- [x] T006 [US4] Create staging deployment manifest at `infrastructure/kubernetes/staging/deployment.yaml`
- [x] T007 [P] [US4] Create staging ConfigMap at `infrastructure/kubernetes/staging/configmap.yaml`
- [x] T008 [P] [US4] Create staging secrets reference at `infrastructure/kubernetes/staging/secrets.yaml`
- [x] T009 [US4] Create staging Supabase values override at `infrastructure/supabase/staging-values.yaml`
- [x] T010 [US4] Deploy staging environment and verify health checks pass (Completed: 2025-12-17, API healthy at v1.0.0)
- [x] T011 [US4] Verify staging data isolation from production (Completed: separate `expenseflow_staging` database with all 21 tables)

**Checkpoint**: Staging environment ready - UAT and Performance testing can now begin

---

## Phase 3: User Story 1 - Historical Data Import for Cache Warming (Priority: P1)

**Goal**: Import 6 months of historical expense data to populate caches (descriptions, vendor aliases, embeddings)

**Independent Test**: Import historical Excel file, verify >500 descriptions cached, >100 vendor aliases created, >500 embeddings generated with <1% error rate

### Entity & Interface (US1)

- [x] T012 [P] [US1] Create ImportJob entity at `backend/src/ExpenseFlow.Core/Entities/ImportJob.cs`
- [x] T013 [P] [US1] Create ImportJobStatus enum in ImportJob.cs (Pending, Processing, Completed, Failed, Cancelled)
- [x] T014 [US1] Create ImportJobConfiguration for EF Core at `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/ImportJobConfiguration.cs`
- [x] T015 [US1] Add DbSet<ImportJob> to ApplicationDbContext
- [x] T016 [US1] Create EF migration for ImportJobs table: `dotnet ef migrations add AddImportJobTable` (Created: 20251217155334_AddImportJobTable.cs)

### DTOs (US1)

- [x] T017 [P] [US1] Create ImportJobResponse DTO at `backend/src/ExpenseFlow.Shared/DTOs/CacheWarmingDtos.cs`
- [x] T018 [P] [US1] Create ImportProgress DTO in CacheWarmingDtos.cs
- [x] T019 [P] [US1] Create ImportJobListResponse DTO in CacheWarmingDtos.cs
- [x] T020 [P] [US1] Create ImportError and ImportErrorListResponse DTOs in CacheWarmingDtos.cs
- [x] T021 [P] [US1] Create CacheStatisticsResponse and CacheWarmingSummaryResponse DTOs in CacheWarmingDtos.cs

### Service Interface (US1)

- [x] T022 [US1] Create ICacheWarmingService interface at `backend/src/ExpenseFlow.Core/Interfaces/ICacheWarmingService.cs`
  - ImportHistoricalDataAsync(Stream file, string fileName, Guid userId)
  - GetImportJobAsync(Guid jobId)
  - GetImportJobsAsync(Guid userId, ImportJobStatus? status, int page, int pageSize)
  - CancelImportJobAsync(Guid jobId)
  - GetImportJobErrorsAsync(Guid jobId, int page, int pageSize)

### Service Implementation (US1)

- [x] T023 [US1] Create CacheWarmingService at `backend/src/ExpenseFlow.Infrastructure/Services/CacheWarmingService.cs`
- [x] T024 [US1] Implement Excel file upload to blob storage in CacheWarmingService
- [x] T025 [US1] Implement ImportJob creation with Pending status
- [x] T026 [US1] Implement GetImportJobAsync with progress calculation
- [x] T027 [US1] Implement GetImportJobsAsync with filtering and pagination
- [x] T028 [US1] Implement CancelImportJobAsync with status validation
- [x] T029 [US1] Implement GetImportJobErrorsAsync with pagination

### Background Job (US1)

- [x] T030 [US1] Create CacheWarmingJob at `backend/src/ExpenseFlow.Infrastructure/Jobs/CacheWarmingJob.cs`
- [x] T031 [US1] Implement Excel parsing with ClosedXML (expected columns: Date, Description, Vendor, Amount, GL Code, Department)
- [x] T032 [US1] Implement chunked processing (100 records per batch) with progress tracking
- [x] T033 [US1] Implement DescriptionCache population from historical descriptions (check RawDescriptionHash, update HitCount or insert)
- [x] T034 [US1] Implement VendorAlias extraction and creation (extract vendor patterns, match DefaultGLCode/Department)
- [x] T035 [US1] Implement batched embedding generation (up to 100 texts per API call) using IEmbeddingService
- [x] T036 [US1] Implement ExpenseEmbedding population (check similarity >0.98, skip near-duplicates)
- [ ] T036B [US1] Implement StatementFingerprint creation from known statement sources during cache warming (FR-005) (DEFERRED: requires statement format metadata)
- [x] T037 [US1] Implement error handling with skip and log pattern (ErrorLog as JSON array)
- [x] T038 [US1] Implement cancellation token support for job cancellation
- [x] T039 [US1] Register CacheWarmingJob with Hangfire

### Controller (US1)

- [x] T040 [US1] Create CacheWarmingController at `backend/src/ExpenseFlow.Api/Controllers/CacheWarmingController.cs`
- [x] T041 [US1] Implement POST /api/cache-warming/import endpoint (multipart/form-data, 10MB max)
- [x] T042 [US1] Implement GET /api/cache-warming/jobs endpoint with pagination and status filter
- [x] T043 [US1] Implement GET /api/cache-warming/jobs/{jobId} endpoint
- [x] T044 [US1] Implement DELETE /api/cache-warming/jobs/{jobId} endpoint (cancel job)
- [x] T045 [US1] Implement GET /api/cache-warming/jobs/{jobId}/errors endpoint

### Cache Statistics Endpoints (US1)

- [x] T046 [US1] Implement GET /api/cache/statistics endpoint in CacheWarmingController
- [x] T047 [US1] Implement GET /api/cache/statistics/warming-summary endpoint (counts by source: import vs runtime)

### Unit Tests (US1)

- [x] T048 [P] [US1] Create CacheWarmingServiceTests at `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/CacheWarmingServiceTests.cs`
- [x] T049 [P] [US1] Create CacheWarmingJobTests at `backend/tests/ExpenseFlow.Infrastructure.Tests/Jobs/CacheWarmingJobTests.cs`
- [x] T050 [P] [US1] Create CacheWarmingControllerTests at `backend/tests/ExpenseFlow.Api.Tests/Controllers/CacheWarmingControllerTests.cs`

### DI Registration (US1)

- [x] T051 [US1] Register ICacheWarmingService in DI container (Program.cs or InfrastructureServiceExtensions.cs)

**Checkpoint**: Cache warming fully functional - import historical data and verify cache statistics

---

## Phase 4: User Story 2 - User Acceptance Testing Execution (Priority: P1)

**Goal**: Execute structured UAT with 3-5 users covering 7 critical workflows

**Independent Test**: All 7 test cases executed, results documented, P1/P2 defects resolved

**Depends On**: Phase 2 (Staging Environment) must be complete

### UAT Test Plan (US2)

- [x] T052 [US2] Create UAT test plan document at `docs/uat/test-plan.md`
- [x] T053 [US2] Create test case folder structure at `docs/uat/test-cases/`
- [x] T054 [US2] Create defect tracking folder at `docs/uat/defects/`

### Individual Test Cases (US2)

- [x] T055 [P] [US2] Create TC-001-receipt-upload.md (Receipt Upload Flow)
- [x] T056 [P] [US2] Create TC-002-statement-import.md (Statement Import)
- [x] T057 [P] [US2] Create TC-003-matching.md (Receipt-to-Transaction Matching)
- [x] T058 [P] [US2] Create TC-004-categorization.md (AI Categorization)
- [x] T059 [P] [US2] Create TC-005-travel-detection.md (Travel Period Detection)
- [x] T060 [P] [US2] Create TC-006-report-generation.md (Draft Report Generation)
- [x] T061 [P] [US2] Create TC-007-mom-comparison.md (Month-over-Month Comparison)

### UAT Execution (US2)

- [x] T062 [US2] Deploy application to staging with cache warmed (from US1) (Completed: staging deployed 2025-12-17)
- [ ] T063 [US2] Execute TC-001 through TC-007 with 3-5 test users (MANUAL: ready for execution)
- [ ] T064 [US2] Document pass/fail results and notes in test case files (MANUAL: after T063)
- [ ] T065 [US2] Create GitHub Issues for discovered defects with priority labels (MANUAL: after T063)
- [ ] T066 [US2] Fix all P1 (Critical) defects and re-test (MANUAL: after T065)
- [ ] T067 [US2] Fix all P2 (High) defects and re-test (MANUAL: after T065)
- [ ] T068 [US2] Generate UAT summary report in test-plan.md (MANUAL: after T063-T067)
- [ ] T069 [US2] Obtain sign-off from at least 3 test users (MANUAL: after T068)

**Checkpoint**: UAT complete with all P1/P2 defects resolved and user sign-offs obtained

---

## Phase 5: User Story 3 - Performance Testing (Priority: P2)

**Goal**: Validate 50 receipts in 5 minutes and <2s response with 20 concurrent users

**Independent Test**: Run NBomber scenarios, verify batch processing and concurrent user metrics

**Depends On**: Phase 2 (Staging Environment) must be complete

### Load Test Project Setup (US3)

- [x] T070 [US3] Create NBomber scenario base class at `backend/tests/ExpenseFlow.LoadTests/Scenarios/ScenarioBase.cs`
- [x] T071 [US3] Configure load test appsettings.json with staging URL and auth token

### Batch Receipt Processing Test (US3)

- [x] T072 [US3] Create BatchReceiptProcessingTests at `backend/tests/ExpenseFlow.LoadTests/Scenarios/BatchReceiptProcessingTests.cs`
- [x] T073 [US3] Implement scenario: Upload 50 receipts, measure total processing time
- [x] T074 [US3] Add assertions: All 50 processed within 5 minutes (300 seconds)

### Concurrent User Test (US3)

- [x] T075 [US3] Create ConcurrentUserTests at `backend/tests/ExpenseFlow.LoadTests/Scenarios/ConcurrentUserTests.cs`
- [x] T076 [US3] Implement scenario: 20 virtual users performing typical operations (upload, view, edit)
- [x] T077 [US3] Add assertions: 95th percentile response time <2 seconds

### Load Test Execution (US3)

- [x] T078 [US3] Create load test report template at `docs/performance/load-test-report-template.md`
- [ ] T079 [US3] Execute batch receipt processing test against staging (READY: staging deployed)
- [ ] T080 [US3] Execute concurrent user test against staging (READY: staging deployed)
- [ ] T081 [US3] Document results in load test report (after T079/T080)

**Checkpoint**: Performance validated - batch processing and concurrent user targets met

---

## Phase 6: User Story 5 - Query Performance Optimization (Priority: P3)

**Goal**: Ensure all database queries complete within 500ms

**Independent Test**: Enable pg_stat_statements, identify slow queries, optimize, verify <500ms

**Depends On**: US3 (Performance Testing) to identify slow queries

### Query Monitoring Setup (US5)

- [x] T082 [US5] Enable pg_stat_statements extension in staging PostgreSQL (Completed: 2025-12-17)
- [ ] T083 [US5] Configure EF Core logging for query timing in staging appsettings (READY: staging deployed)

### Query Analysis (US5)

- [ ] T084 [US5] Run performance tests to generate query load (BLOCKED: requires T079/T080)
- [ ] T085 [US5] Query pg_stat_statements for queries exceeding 500ms (BLOCKED: requires T084)
- [ ] T086 [US5] Document slow queries with execution plans (BLOCKED: requires T085)

### Query Optimization (US5)

- [ ] T087 [US5] Add missing indexes identified from slow query analysis (BLOCKED: requires T086)
- [ ] T088 [US5] Optimize N+1 queries with eager loading where identified (BLOCKED: requires T086)
- [ ] T089 [US5] Rewrite inefficient queries as needed (BLOCKED: requires T086)
- [ ] T090 [US5] Create EF migration for any new indexes (BLOCKED: requires T087-T089)

### Verification (US5)

- [ ] T091 [US5] Re-run performance tests after optimizations (BLOCKED: requires T090)
- [ ] T092 [US5] Verify all queries now complete within 500ms (BLOCKED: requires T091)
- [ ] T093 [US5] Update load test report with post-optimization results (BLOCKED: requires T091/T092)

**Checkpoint**: All queries optimized to <500ms, performance targets met

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and documentation

- [x] T094 [P] Update CLAUDE.md with Sprint 10 technologies and patterns
- [x] T095 [P] Run all existing unit tests to ensure no regressions: `dotnet test` (217 unit tests pass; 13 integration tests fail due to pre-existing InMemory DB limitations)
- [ ] T096 Run quickstart.md validation steps (MANUAL: requires staging deployment)
- [ ] T097 Verify all success criteria from spec.md are met (MANUAL: requires staging deployment)
- [ ] T097B [US1] Verify SC-009: Import error rate <1% from ImportJob statistics (MANUAL: requires staging deployment)
- [ ] T097C [US1] Verify SC-010: Counts meet targets (≥500 descriptions, ≥100 vendor aliases, ≥500 embeddings) (MANUAL: requires staging deployment)

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1 (Setup)
    │
    ▼
Phase 2 (Foundational/US4 Staging) ◄─── CRITICAL BLOCKER
    │
    ├──────────────────────────────────────┐
    │                                      │
    ▼                                      ▼
Phase 3 (US1 Cache Warming)         Phase 4 (US2 UAT) ◄── needs staging + warmed cache
    │                                      │
    │                                      ▼
    │                               Phase 5 (US3 Performance)
    │                                      │
    │                                      ▼
    │                               Phase 6 (US5 Query Optimization)
    │                                      │
    └──────────────────────────────────────┘
                    │
                    ▼
             Phase 7 (Polish)
```

### User Story Dependencies

| Story | Depends On | Can Start After |
|-------|------------|-----------------|
| US4 (Staging) | Phase 1 Setup | T001-T004 complete |
| US1 (Cache Warming) | Phase 1 Setup | T001-T004 complete |
| US2 (UAT) | US4 + US1 | Staging deployed + cache warmed |
| US3 (Performance) | US4 | Staging deployed |
| US5 (Query Optimization) | US3 | Slow queries identified |

### Parallel Opportunities

**Within Phase 1**:
- T003 and T004 can run in parallel

**Within Phase 2**:
- T007 and T008 can run in parallel

**Within Phase 3 (US1)**:
- T012 and T013 (entities) can run in parallel
- T017-T021 (all DTOs) can run in parallel
- T048-T050 (all unit tests) can run in parallel

**Within Phase 4 (US2)**:
- T055-T061 (all test cases) can run in parallel

**Across Phases**:
- US1 (Cache Warming) and US4 (Staging) can progress in parallel until UAT needs both
- US3 (Performance Testing) can start as soon as staging is ready, doesn't need cache warming

---

## Implementation Strategy

### Recommended Execution Order

1. **Day 1-2**: Phase 1 (Setup) + Phase 2 (Staging/US4)
2. **Day 2-4**: Phase 3 (US1 Cache Warming) - can overlap with staging deployment
3. **Day 5**: Deploy to staging with warmed cache
4. **Day 5-7**: Phase 4 (US2 UAT Execution)
5. **Day 7-8**: Phase 5 (US3 Performance Testing)
6. **Day 9**: Phase 6 (US5 Query Optimization)
7. **Day 10**: Phase 7 (Polish) + Final validation

### MVP Delivery

**Minimum for Production Readiness**:
- US4 (Staging) - Required for any testing
- US1 (Cache Warming) - Required for cost optimization
- US2 (UAT) - Required for quality assurance
- All P1/P2 defects resolved

**Can Defer If Needed**:
- US3 (Performance Testing) - Document as tech debt
- US5 (Query Optimization) - Address reactively if issues arise

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- US4 (Staging) is implemented in Phase 2 because it's a foundational prerequisite
- Cache warming (US1) should complete before UAT to ensure realistic testing
- Performance testing may reveal optimization needs - US5 tasks are reactive
- All test case documents follow the template defined in research.md
