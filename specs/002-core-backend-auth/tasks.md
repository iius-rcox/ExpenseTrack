# Tasks: Core Backend & Authentication

**Input**: Design documents from `/specs/002-core-backend-auth/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Not explicitly requested in spec. Test tasks are omitted. Validation tests in `contracts/validation-tests.md` can be run manually.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

Based on plan.md:
- **Backend**: `backend/src/ExpenseFlow.Api/`, `backend/src/ExpenseFlow.Core/`, `backend/src/ExpenseFlow.Infrastructure/`, `backend/src/ExpenseFlow.Shared/`
- **Tests**: `backend/tests/ExpenseFlow.*.Tests/`
- **Infrastructure**: `infrastructure/kubernetes/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Create solution structure: `backend/` directory with `src/` and `tests/` subdirectories
- [x] T002 Create .NET solution file `backend/ExpenseFlow.sln`
- [x] T003 [P] Create ExpenseFlow.Api project in `backend/src/ExpenseFlow.Api/`
- [x] T004 [P] Create ExpenseFlow.Core project in `backend/src/ExpenseFlow.Core/`
- [x] T005 [P] Create ExpenseFlow.Infrastructure project in `backend/src/ExpenseFlow.Infrastructure/`
- [x] T006 [P] Create ExpenseFlow.Shared project in `backend/src/ExpenseFlow.Shared/`
- [x] T007 Add all projects to solution and configure project references per plan.md
- [x] T008 [P] Add NuGet packages to ExpenseFlow.Api (Microsoft.Identity.Web, Hangfire.AspNetCore, Swashbuckle.AspNetCore)
- [x] T009 [P] Add NuGet packages to ExpenseFlow.Infrastructure (Npgsql.EntityFrameworkCore.PostgreSQL, Npgsql.EntityFrameworkCore.PostgreSQL.Pgvector, Hangfire.PostgreSql, Microsoft.Data.SqlClient, Azure.Identity, Polly)
- [x] T010 [P] Add NuGet packages to ExpenseFlow.Core (Pgvector)
- [x] T011 Configure `backend/src/ExpenseFlow.Api/appsettings.json` with placeholder configuration
- [x] T012 Configure `backend/src/ExpenseFlow.Api/appsettings.Development.json` with local development settings per quickstart.md
- [x] T013 Create `.gitignore` entries for .NET artifacts in `backend/.gitignore`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T014 Create base entity interface `IEntity` in `backend/src/ExpenseFlow.Core/Interfaces/IEntity.cs`
- [x] T015 Create `BaseEntity` class in `backend/src/ExpenseFlow.Core/Entities/BaseEntity.cs`
- [x] T016 Create `ExpenseFlowDbContext` in `backend/src/ExpenseFlow.Infrastructure/Data/ExpenseFlowDbContext.cs`
- [x] T017 Configure Entity Framework Core with Npgsql in `backend/src/ExpenseFlow.Api/Program.cs`
- [x] T018 Create global exception middleware in `backend/src/ExpenseFlow.Api/Middleware/ExceptionMiddleware.cs`
- [x] T019 Configure Problem Details (RFC 7807) error responses in `backend/src/ExpenseFlow.Api/Program.cs`
- [x] T020 [P] Create `ProblemDetailsResponse` DTO in `backend/src/ExpenseFlow.Shared/DTOs/ProblemDetailsResponse.cs`
- [x] T021 [P] Configure Swagger/OpenAPI in `backend/src/ExpenseFlow.Api/Program.cs`
- [x] T022 Create base controller class `ApiControllerBase` in `backend/src/ExpenseFlow.Api/Controllers/ApiControllerBase.cs`
- [x] T023 Create Dockerfile in `backend/Dockerfile` for containerization

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Secure API Authentication (Priority: P1)

**Goal**: Authenticate users via Entra ID JWT tokens and auto-create user profiles on first login

**Independent Test**: Access protected endpoint without auth (401), then with valid token (200 + user profile)

**Requirements Covered**: FR-001, FR-002, FR-003, FR-004, FR-005

### User Entity (Blocking for this story)

- [x] T024 [US1] Create `User` entity in `backend/src/ExpenseFlow.Core/Entities/User.cs` per data-model.md
- [x] T025 [US1] Add `User` DbSet and configuration to `backend/src/ExpenseFlow.Infrastructure/Data/ExpenseFlowDbContext.cs`

### Authentication Infrastructure

- [x] T026 [US1] Configure Microsoft.Identity.Web JWT authentication in `backend/src/ExpenseFlow.Api/Program.cs`
- [x] T027 [US1] Create custom `ClaimsPrincipalExtensions` in `backend/src/ExpenseFlow.Shared/Extensions/ClaimsPrincipalExtensions.cs`
- [x] T028 [US1] Create `IUserService` interface in `backend/src/ExpenseFlow.Core/Interfaces/IUserService.cs`
- [x] T029 [US1] Create `UserService` implementation in `backend/src/ExpenseFlow.Infrastructure/Services/UserService.cs`
- [x] T030 [US1] Configure authorization policies for admin role in `backend/src/ExpenseFlow.Api/Program.cs`

### User Profile Endpoints

- [x] T031 [P] [US1] Create `UserResponse` DTO in `backend/src/ExpenseFlow.Shared/DTOs/UserResponse.cs` per api-spec.yaml
- [x] T032 [US1] Create `UsersController` with GET /users/me endpoint in `backend/src/ExpenseFlow.Api/Controllers/UsersController.cs`
- [x] T033 [US1] Implement auto-create user profile on first login in `UserService.GetOrCreateUserAsync()`

### Health Endpoint (No Auth Required)

- [x] T034 [P] [US1] Create `HealthResponse` DTO in `backend/src/ExpenseFlow.Shared/DTOs/HealthResponse.cs` per api-spec.yaml
- [x] T035 [US1] Create `HealthController` with GET /health endpoint (AllowAnonymous) in `backend/src/ExpenseFlow.Api/Controllers/HealthController.cs`

### Database Migration

- [x] T036 [US1] Create initial EF Core migration for User table in `backend/src/ExpenseFlow.Infrastructure/`

**Checkpoint**: Authentication is functional. Protected endpoints return 401 without token, 200 with valid token. User profile auto-created on first login.

---

## Phase 4: User Story 2 - Cache Tables Foundation (Priority: P1)

**Goal**: Establish the 5 cache tables for Cost-First AI Architecture (Tier 1/2 lookups)

**Independent Test**: Insert records into each cache table, query by hash/pattern, verify cached results returned

**Requirements Covered**: FR-006, FR-007, FR-008, FR-009, FR-010, FR-011

### Cache Entities

- [x] T037 [P] [US2] Create `DescriptionCache` entity in `backend/src/ExpenseFlow.Core/Entities/DescriptionCache.cs` per data-model.md
- [x] T038 [P] [US2] Create `VendorAlias` entity in `backend/src/ExpenseFlow.Core/Entities/VendorAlias.cs` per data-model.md
- [x] T039 [P] [US2] Create `StatementFingerprint` entity in `backend/src/ExpenseFlow.Core/Entities/StatementFingerprint.cs` per data-model.md
- [x] T040 [P] [US2] Create `SplitPattern` entity in `backend/src/ExpenseFlow.Core/Entities/SplitPattern.cs` per data-model.md
- [x] T041 [P] [US2] Create `ExpenseEmbedding` entity with Vector type in `backend/src/ExpenseFlow.Core/Entities/ExpenseEmbedding.cs` per data-model.md

### Entity Configuration

- [x] T042 [P] [US2] Create `DescriptionCacheConfiguration` in `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/DescriptionCacheConfiguration.cs`
- [x] T043 [P] [US2] Create `VendorAliasConfiguration` in `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/VendorAliasConfiguration.cs`
- [x] T044 [P] [US2] Create `StatementFingerprintConfiguration` in `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/StatementFingerprintConfiguration.cs`
- [x] T045 [P] [US2] Create `SplitPatternConfiguration` in `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/SplitPatternConfiguration.cs`
- [x] T046 [P] [US2] Create `ExpenseEmbeddingConfiguration` with IVFFlat index in `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/ExpenseEmbeddingConfiguration.cs`

### DbContext Updates

- [x] T047 [US2] Add all cache entity DbSets to `backend/src/ExpenseFlow.Infrastructure/Data/ExpenseFlowDbContext.cs`
- [x] T048 [US2] Apply entity configurations in DbContext OnModelCreating

### Cache Service Interfaces

- [x] T049 [P] [US2] Create `IDescriptionCacheService` interface in `backend/src/ExpenseFlow.Core/Interfaces/IDescriptionCacheService.cs`
- [x] T050 [P] [US2] Create `IVendorAliasService` interface in `backend/src/ExpenseFlow.Core/Interfaces/IVendorAliasService.cs`
- [x] T051 [P] [US2] Create `IStatementFingerprintService` interface in `backend/src/ExpenseFlow.Core/Interfaces/IStatementFingerprintService.cs`
- [x] T052 [P] [US2] Create `IExpenseEmbeddingService` interface in `backend/src/ExpenseFlow.Core/Interfaces/IExpenseEmbeddingService.cs`

> **Note**: `SplitPattern` is a Tier 1 deterministic lookup (no AI orchestration). A dedicated service interface is deferred until Sprint 3+ when split allocation logic is implemented.

### Cache Service Implementations

- [x] T053 [US2] Create `DescriptionCacheService` in `backend/src/ExpenseFlow.Infrastructure/Services/DescriptionCacheService.cs`
- [x] T054 [US2] Create `VendorAliasService` in `backend/src/ExpenseFlow.Infrastructure/Services/VendorAliasService.cs`
- [x] T055 [US2] Create `StatementFingerprintService` in `backend/src/ExpenseFlow.Infrastructure/Services/StatementFingerprintService.cs`
- [x] T056 [US2] Create `ExpenseEmbeddingService` with vector similarity search in `backend/src/ExpenseFlow.Infrastructure/Services/ExpenseEmbeddingService.cs`

> **Constitution II Note**: Each cache service MUST include `AddOrUpdateAsync()` methods to support the Self-Improving System principle. These will be called by future sprints when users confirm suggestions (e.g., GL code selection → new ExpenseEmbedding entry).

### Cache Statistics Endpoint

- [x] T057 [P] [US2] Create `CacheStatsResponse` and `CacheTableStats` DTOs in `backend/src/ExpenseFlow.Shared/DTOs/CacheStatsResponse.cs` per api-spec.yaml
- [x] T058 [US2] Create `ICacheStatsService` interface in `backend/src/ExpenseFlow.Core/Interfaces/ICacheStatsService.cs`
- [x] T059 [US2] Create `CacheStatsService` implementation in `backend/src/ExpenseFlow.Infrastructure/Services/CacheStatsService.cs`
- [x] T060 [US2] Create `CacheController` with GET /cache/stats endpoint (admin only) in `backend/src/ExpenseFlow.Api/Controllers/CacheController.cs`

### Database Migration

- [x] T061 [US2] Create EF Core migration for cache tables with indexes in `backend/src/ExpenseFlow.Infrastructure/`

**Checkpoint**: All 5 cache tables exist with proper indexes. Cache statistics endpoint returns counts and hit rates.

---

## Phase 5: User Story 3 - Background Job Processing (Priority: P2)

**Goal**: Configure Hangfire for async job processing with PostgreSQL storage and admin dashboard

**Independent Test**: Enqueue a test job, verify it appears in dashboard, confirm successful execution

**Requirements Covered**: FR-012, FR-013, FR-014, FR-015, FR-016

### Hangfire Configuration

- [x] T062 [US3] Configure Hangfire with PostgreSQL storage in `backend/src/ExpenseFlow.Api/Program.cs`
- [x] T063 [US3] Configure Hangfire dashboard with admin-only authorization in `backend/src/ExpenseFlow.Api/Program.cs`
- [x] T064 [US3] Create `HangfireAuthorizationFilter` for admin role check in `backend/src/ExpenseFlow.Api/Filters/HangfireAuthorizationFilter.cs`

### Job Infrastructure

- [x] T065 [P] [US3] Create `IBackgroundJobService` interface in `backend/src/ExpenseFlow.Core/Interfaces/IBackgroundJobService.cs`
- [x] T066 [US3] Create `BackgroundJobService` implementation in `backend/src/ExpenseFlow.Infrastructure/Services/BackgroundJobService.cs`
- [x] T067 [P] [US3] Create base `JobBase` class with retry attributes in `backend/src/ExpenseFlow.Infrastructure/Jobs/JobBase.cs`

### Test Job for Validation

- [x] T068 [US3] Create `TestJob` for dashboard validation in `backend/src/ExpenseFlow.Infrastructure/Jobs/TestJob.cs`

### Dependency Injection Registration

- [x] T069 [US3] Register Hangfire services in DI container in `backend/src/ExpenseFlow.Api/Program.cs`

**Checkpoint**: Hangfire dashboard accessible at /hangfire for admins. Jobs can be enqueued and monitored.

---

## Phase 6: User Story 4 - Reference Data Synchronization (Priority: P2)

**Goal**: Sync GL accounts, departments, and projects from external SQL Server on weekly schedule

**Independent Test**: Trigger sync job, verify data populated in local tables, query reference endpoints

**Requirements Covered**: FR-017, FR-018, FR-019, FR-020

### Reference Data Entities

- [x] T070 [P] [US4] Create `GLAccount` entity in `backend/src/ExpenseFlow.Core/Entities/GLAccount.cs` per data-model.md
- [x] T071 [P] [US4] Create `Department` entity in `backend/src/ExpenseFlow.Core/Entities/Department.cs` per data-model.md
- [x] T072 [P] [US4] Create `Project` entity in `backend/src/ExpenseFlow.Core/Entities/Project.cs` per data-model.md

### Entity Configuration

- [x] T073 [P] [US4] Create `GLAccountConfiguration` in `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/GLAccountConfiguration.cs`
- [x] T074 [P] [US4] Create `DepartmentConfiguration` in `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/DepartmentConfiguration.cs`
- [x] T075 [P] [US4] Create `ProjectConfiguration` in `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/ProjectConfiguration.cs`

### DbContext Updates

- [x] T076 [US4] Add reference data entity DbSets to `backend/src/ExpenseFlow.Infrastructure/Data/ExpenseFlowDbContext.cs`

### Reference Data DTOs

- [x] T077 [P] [US4] Create `GLAccountResponse` DTO in `backend/src/ExpenseFlow.Shared/DTOs/GLAccountResponse.cs` per api-spec.yaml
- [x] T078 [P] [US4] Create `DepartmentResponse` DTO in `backend/src/ExpenseFlow.Shared/DTOs/DepartmentResponse.cs` per api-spec.yaml
- [x] T079 [P] [US4] Create `ProjectResponse` DTO in `backend/src/ExpenseFlow.Shared/DTOs/ProjectResponse.cs` per api-spec.yaml
- [x] T080 [P] [US4] Create `JobEnqueuedResponse` DTO in `backend/src/ExpenseFlow.Shared/DTOs/JobEnqueuedResponse.cs` per api-spec.yaml

### External SQL Server Client

- [x] T081 [US4] Create `IExternalDataSource` interface in `backend/src/ExpenseFlow.Core/Interfaces/IExternalDataSource.cs`
- [x] T082 [US4] Create `SqlServerDataSource` with Managed Identity auth in `backend/src/ExpenseFlow.Infrastructure/Services/SqlServerDataSource.cs`
- [x] T083 [US4] Configure Polly retry policies for SQL Server connection in `SqlServerDataSource`

### Reference Data Services

- [x] T084 [P] [US4] Create `IReferenceDataService` interface in `backend/src/ExpenseFlow.Core/Interfaces/IReferenceDataService.cs`
- [x] T085 [US4] Create `ReferenceDataService` implementation in `backend/src/ExpenseFlow.Infrastructure/Services/ReferenceDataService.cs`

### Sync Job

- [x] T086 [US4] Create `ReferenceDataSyncJob` Hangfire job in `backend/src/ExpenseFlow.Infrastructure/Jobs/ReferenceDataSyncJob.cs`
- [x] T087 [US4] Configure weekly recurring job (Sunday 2 AM) in `backend/src/ExpenseFlow.Api/Program.cs`

### Reference Data Controller

- [x] T088 [US4] Create `ReferenceController` with GET /reference/gl-accounts, /departments, /projects in `backend/src/ExpenseFlow.Api/Controllers/ReferenceController.cs`
- [x] T089 [US4] Add POST /reference/sync endpoint (admin only) to `ReferenceController`

### Database Migration

- [x] T090 [US4] Create EF Core migration for reference data tables in `backend/src/ExpenseFlow.Infrastructure/`

**Checkpoint**: Reference data endpoints return data. Sync job can be triggered manually or runs on schedule. Failed syncs preserve existing data.

---

## Phase 7: Deployment & Polish

**Purpose**: Kubernetes deployment and cross-cutting concerns

### Kubernetes Manifests

- [x] T091 [P] Create deployment manifest in `infrastructure/kubernetes/deployment.yaml`
- [x] T092 [P] Create service manifest in `infrastructure/kubernetes/service.yaml`
- [x] T093 [P] Create ingress manifest in `infrastructure/kubernetes/ingress.yaml` with host `dev.expense.ii-us.com`, TLS via cert-manager ClusterIssuer `letsencrypt-prod` from Sprint 1

### Dependency Injection Registration

- [x] T094 Register all services in DI container in `backend/src/ExpenseFlow.Api/Program.cs`
- [x] T095 Create `ServiceCollectionExtensions` for clean DI registration in `backend/src/ExpenseFlow.Infrastructure/Extensions/ServiceCollectionExtensions.cs`

### Final Configuration

- [x] T096 Configure CORS policy in `backend/src/ExpenseFlow.Api/Program.cs`
- [x] T097 Add request logging middleware in `backend/src/ExpenseFlow.Api/Program.cs`
- [x] T098 Configure structured logging (Serilog or similar) in `backend/src/ExpenseFlow.Api/Program.cs`

### Validation

- [x] T099 Run quickstart.md local development validation
- [x] T100 Run validation-tests.md test cases, including T3.5 (vector similarity <500ms for 10k rows) and T6.1-T6.3 performance tests
- [x] T101 Build and test Docker container locally
- [x] T102 Deploy to dev-aks and verify health endpoint

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - US1 and US2 are both P1, can proceed in parallel
  - US3 and US4 are both P2, can proceed in parallel after US1/US2
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (Auth, P1)**: Can start after Foundational - No dependencies on other stories
- **User Story 2 (Cache, P1)**: Can start after Foundational - No dependencies on other stories
- **User Story 3 (Jobs, P2)**: Can start after Foundational - Hangfire is independent of auth/cache
- **User Story 4 (Ref Data, P2)**: Depends on US3 (uses Hangfire for sync job)

### Within Each User Story

- Models/Entities before Services
- Services before Controllers
- Core implementation before integration
- Story complete before moving to next priority

### Parallel Opportunities

- T003, T004, T005, T006: All project creation can run in parallel
- T008, T009, T010: All NuGet package installations can run in parallel
- T037-T041: All cache entities can be created in parallel
- T042-T046: All entity configurations can be created in parallel
- T049-T052: All cache service interfaces can be created in parallel
- T070-T072: All reference data entities can be created in parallel
- T073-T075: All reference data configurations can be created in parallel
- T077-T080: All reference data DTOs can be created in parallel
- T091-T093: All Kubernetes manifests can be created in parallel

---

## Parallel Example: User Story 2 (Cache Tables)

```bash
# Launch all cache entities together (T037-T041):
Task: "Create DescriptionCache entity in backend/src/ExpenseFlow.Core/Entities/DescriptionCache.cs"
Task: "Create VendorAlias entity in backend/src/ExpenseFlow.Core/Entities/VendorAlias.cs"
Task: "Create StatementFingerprint entity in backend/src/ExpenseFlow.Core/Entities/StatementFingerprint.cs"
Task: "Create SplitPattern entity in backend/src/ExpenseFlow.Core/Entities/SplitPattern.cs"
Task: "Create ExpenseEmbedding entity in backend/src/ExpenseFlow.Core/Entities/ExpenseEmbedding.cs"

# Then launch all configurations together (T042-T046):
Task: "Create DescriptionCacheConfiguration in backend/src/ExpenseFlow.Infrastructure/Data/Configurations/"
Task: "Create VendorAliasConfiguration in backend/src/ExpenseFlow.Infrastructure/Data/Configurations/"
# ... etc.
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Authentication)
4. Complete Phase 4: User Story 2 (Cache Tables)
5. **STOP and VALIDATE**: Test authentication works, cache tables exist with indexes
6. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy (Auth works!)
3. Add User Story 2 → Test independently → Deploy (Cache ready!)
4. Add User Story 3 → Test independently → Deploy (Jobs work!)
5. Add User Story 4 → Test independently → Deploy (Ref data syncs!)
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (Auth)
   - Developer B: User Story 2 (Cache)
3. Once P1 stories complete:
   - Developer A: User Story 3 (Jobs)
   - Developer B: User Story 4 (Ref Data)
4. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
