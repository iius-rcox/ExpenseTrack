# Tasks: Backend API User Preferences

**Input**: Design documents from `/specs/016-user-preferences-api/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: E2E tests will verify via Chrome DevTools MCP (per existing project pattern)

**Organization**: Tasks grouped by user story for independent implementation and testing

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions

## User Story Summary

| Story | Priority | Description | Key Deliverable |
|-------|----------|-------------|-----------------|
| US1 | P1 | Retrieve User Profile | GET /api/user/me with preferences |
| US2 | P1 | View User Preferences | GET /api/user/preferences |
| US3 | P1 | Update Theme Preference | PATCH /api/user/preferences |
| US4 | P2 | Update Default Dept/Project | Validation for dept/project IDs |

**Note**: US1-US3 share foundational infrastructure. Foundational phase must complete before any user story.

---

## Phase 1: Setup

**Purpose**: Verify environment and confirm existing code structure

- [x] T001 Verify feature branch exists: `git checkout 016-user-preferences-api`
- [x] T002 [P] Verify backend builds: `cd backend && dotnet build`
- [x] T003 [P] Review existing UsersController in backend/src/ExpenseFlow.Api/Controllers/UsersController.cs

---

## Phase 2: Foundational (Core Infrastructure)

**Purpose**: Entity, DTOs, interface, and migration that ALL user stories depend on

**⚠️ CRITICAL**: US1-US4 cannot work until this phase is complete

### Entity Layer

- [x] T004 Create UserPreferences entity in backend/src/ExpenseFlow.Core/Entities/UserPreferences.cs
- [x] T005 Add Preferences navigation property to backend/src/ExpenseFlow.Core/Entities/User.cs

### DTO Layer

- [x] T006 [P] Create UserPreferencesResponse DTO in backend/src/ExpenseFlow.Shared/DTOs/UserPreferencesResponse.cs
- [x] T007 [P] Create UpdatePreferencesRequest DTO in backend/src/ExpenseFlow.Shared/DTOs/UpdatePreferencesRequest.cs
- [x] T008 [P] Add Preferences property to existing UserResponse in backend/src/ExpenseFlow.Shared/DTOs/UserResponse.cs

### Service Interface

- [x] T009 Create IUserPreferencesService interface in backend/src/ExpenseFlow.Core/Interfaces/IUserPreferencesService.cs

### Database Configuration

- [x] T010 Add DbSet<UserPreferences> to backend/src/ExpenseFlow.Infrastructure/Data/ExpenseFlowDbContext.cs
- [x] T011 Create EF configuration in backend/src/ExpenseFlow.Infrastructure/Data/Configurations/UserPreferencesConfiguration.cs
- [x] T012 Generate EF migration: `dotnet ef migrations add AddUserPreferences`

### Dependency Injection

- [x] T013 Register IUserPreferencesService in DI container (Program.cs or service registration)

### Route Configuration

- [x] T013a Add `[Route("api/user")]` attribute to UsersController to support both `/api/users/` and `/api/user/` routes (frontend expects singular `/api/user/me`)

**Checkpoint**: Foundation ready - database schema exists, service interface defined, DTOs available, routes configured

---

## Phase 3: User Story 1+2 - Profile & View Preferences (Priority: P1) 🎯 MVP Part 1

**Goal**: Users can retrieve their profile and current preferences via GET endpoints

**Independent Test**: Log in, navigate to Settings, verify profile and preferences load without errors

**Note**: US1 and US2 are combined because they share the same service method (GetOrCreateDefaultsAsync) and differ only in response shape

### Implementation for User Story 1+2

- [x] T014 [US1] Implement GetOrCreateDefaultsAsync in backend/src/ExpenseFlow.Infrastructure/Services/UserPreferencesService.cs
- [x] T015 [US1] Inject IUserPreferencesService into UsersController constructor
- [x] T016 [US1] Update GetCurrentUser endpoint to include preferences in response in backend/src/ExpenseFlow.Api/Controllers/UsersController.cs (ensure DisplayName falls back to email if null per FR-002)
- [x] T017 [US2] Add GetPreferences endpoint (GET /api/user/preferences) to backend/src/ExpenseFlow.Api/Controllers/UsersController.cs
- [x] T018 [US1] Add logging for profile/preferences retrieval operations

### Verification for User Story 1+2

- [ ] T019 [US1] Apply migration to database: `dotnet ef database update`
- [ ] T020 [US1] Manual test: Call GET /api/user/me with auth token, verify preferences included (observe response time <2s per SC-001)
- [ ] T021 [US2] Manual test: Call GET /api/user/preferences with auth token, verify defaults returned

**Checkpoint**: Profile and preferences retrieval working - resolves "Unable to load profile" error

---

## Phase 4: User Story 3 - Update Theme Preference (Priority: P1) 🎯 MVP Part 2

**Goal**: Users can update their theme preference and have it persist across sessions

**Independent Test**: Change theme in Settings, refresh browser, verify theme persists; log out/in on new device, verify same theme

### Implementation for User Story 3

- [x] T022 [US3] Implement UpdateAsync method in backend/src/ExpenseFlow.Infrastructure/Services/UserPreferencesService.cs
- [x] T023 [US3] Add validation for theme values (light/dark/system) in UpdateAsync
- [x] T024 [US3] Add UpdatePreferences endpoint (PATCH /api/user/preferences) to backend/src/ExpenseFlow.Api/Controllers/UsersController.cs
- [x] T025 [US3] Handle partial updates (only theme field provided)
- [x] T026 [US3] Add logging for preference update operations

### Verification for User Story 3

- [ ] T027 [US3] Manual test: PATCH /api/user/preferences with {"theme": "dark"}, verify 200 response (observe response time <1s per SC-002)
- [ ] T028 [US3] Manual test: GET /api/user/preferences, verify theme is "dark"
- [ ] T029 [US3] Manual test: PATCH with invalid theme value, verify 400 response

**Checkpoint**: Theme persistence working - resolves "Failed to update theme" error (SC-005)

---

## Phase 5: User Story 4 - Update Default Department/Project (Priority: P2)

**Goal**: Users can set default department and project for expense reports

**Independent Test**: Set default department in Settings, create new expense report, verify department pre-selected

### Implementation for User Story 4

- [x] T030 [US4] Add department existence validation to UpdateAsync in backend/src/ExpenseFlow.Infrastructure/Services/UserPreferencesService.cs
- [x] T031 [US4] Add project existence validation to UpdateAsync
- [x] T032 [US4] Return proper 400 error with field-specific messages for invalid IDs
- [x] T033 [US4] Handle null values to clear defaults (explicit null vs missing field)

### Verification for User Story 4

- [ ] T034 [US4] Manual test: PATCH with valid departmentId, verify saved
- [ ] T035 [US4] Manual test: PATCH with invalid departmentId, verify 400 with error message
- [ ] T036 [US4] Manual test: PATCH with null departmentId, verify default cleared

**Checkpoint**: Full preference management working

---

## Phase 6: Polish & Validation

**Purpose**: Final validation, error handling, and cleanup

### Error Handling

- [x] T037 Add FluentValidation validator for UpdatePreferencesRequest in backend/src/ExpenseFlow.Api/Validators/
- [x] T038 Verify ProblemDetails format for validation errors (RFC 7807 compliant)

### Staging Validation

- [ ] T039 Build Docker image: `docker buildx build --platform linux/amd64 -t iiusacr.azurecr.io/expenseflow-api:TAG --push .`
- [ ] T040 Update staging deployment manifest and push for ArgoCD sync
- [ ] T041 Verify GET /api/user/me works on staging
- [ ] T042 Verify PATCH /api/user/preferences works on staging (no "Failed to update theme" error)

### Documentation

- [ ] T043 Update OpenAPI/Swagger documentation (should auto-generate from controller)
- [ ] T044 Run quickstart.md validation checklist

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup
    ↓
Phase 2: Foundational ← BLOCKS ALL USER STORIES
    ↓
Phase 3: US1+US2 (Profile + View)
    ↓
Phase 4: US3 (Update Theme) ← RESOLVES PRIMARY ERROR
    ↓
Phase 5: US4 (Dept/Project) ← Can be deferred
    ↓
Phase 6: Polish & Validation
```

### User Story Dependencies

| Story | Depends On | Can Skip? |
|-------|------------|-----------|
| US1+US2 | Foundational | No - core feature |
| US3 | US1+US2 (needs preferences to exist) | No - primary deliverable |
| US4 | US3 | Yes - P2 priority, can defer |

### Within-Phase Parallel Opportunities

**Phase 2 (Foundational)**:
- T006, T007, T008 (DTOs) can run in parallel - different files

**Phase 3 (US1+US2)**:
- T020, T021 (verification) can run in parallel

---

## Parallel Example: Foundational Phase DTOs

```bash
# These can run in parallel (different files):
Task: "Create UserPreferencesResponse DTO in backend/src/ExpenseFlow.Shared/DTOs/UserPreferencesResponse.cs"
Task: "Create UpdatePreferencesRequest DTO in backend/src/ExpenseFlow.Shared/DTOs/UpdatePreferencesRequest.cs"
Task: "Add Preferences property to UserResponse in backend/src/ExpenseFlow.Shared/DTOs/UserResponse.cs"
```

---

## Implementation Strategy

### MVP First (Phases 1-4)

1. Complete Phase 1: Setup (5 min)
2. Complete Phase 2: Foundational - entity, DTOs, migration (30 min)
3. Complete Phase 3: US1+US2 - profile and view preferences (20 min)
4. Complete Phase 4: US3 - update theme preference (20 min)
5. **STOP and VALIDATE**: Theme toggle works without "Failed to update" error
6. Deploy to staging - primary issue resolved

### Full Feature (All Phases)

1. Complete MVP (Phases 1-4)
2. Complete Phase 5: US4 - dept/project defaults (20 min)
3. Complete Phase 6: Polish & validation (15 min)
4. All success criteria met

### Time Estimates

| Phase | Estimated Time | Cumulative |
|-------|---------------|------------|
| Setup | 5 min | 5 min |
| Foundational | 30 min | 35 min |
| US1+US2 (Profile/View) | 20 min | 55 min |
| US3 (Update Theme) | 20 min | 1h 15m |
| US4 (Dept/Project) | 20 min | 1h 35m |
| Polish | 15 min | 1h 50m |

---

## Manual CLI Commands Required

**Run these commands from the `backend/` directory:**

```bash
# 1. Generate EF Migration (T012)
dotnet ef migrations add AddUserPreferences \
  --project src/ExpenseFlow.Infrastructure \
  --startup-project src/ExpenseFlow.Api

# 2. Apply Migration to Database (T019)
dotnet ef database update \
  --project src/ExpenseFlow.Infrastructure \
  --startup-project src/ExpenseFlow.Api

# 3. Build & Test Locally
dotnet build
dotnet test

# 4. Build Docker Image for AKS (T039) - MUST use linux/amd64
docker buildx build --platform linux/amd64 \
  -t iiusacr.azurecr.io/expenseflow-api:v1.X.X-$(git rev-parse --short HEAD) \
  --push .

# 5. Update staging manifest (T040)
# Edit infrastructure/kubernetes/staging/api-deployment.yaml with new image tag
# Commit and push to main - ArgoCD auto-syncs
```

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story
- US1+US2 combined because they share infrastructure
- MVP (Phases 1-4) resolves the "Failed to update theme" staging error
- US4 (P2) can be deferred if time-constrained
- Verify existing /users/me endpoint still works (no breaking changes)
- Use `dotnet ef migrations add` for schema changes
- Docker build requires `--platform linux/amd64` for AKS deployment
