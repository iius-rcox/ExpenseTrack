# Tasks: API Error Resolution

**Input**: Design documents from `/specs/014-api-error-resolution/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Unit tests included for new controller endpoints (per constitution requirement)

**Organization**: Tasks grouped by user story for independent implementation and testing

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- Include exact file paths in descriptions

## User Story Summary

| Story | Priority | Description | Root Cause |
|-------|----------|-------------|------------|
| US1 | P1 | View Dashboard Metrics | 401 - Auth scope mismatch |
| US2 | P1 | View Recent Activity | 401 - Auth scope mismatch |
| US3 | P2 | Access Analytics Categories | 404 - Missing endpoint |
| US4 | P2 | View Pending Actions Queue | 404 - Missing endpoint |

**Note**: US1 and US2 share the same root cause (authentication). Fixing auth in the Foundational phase enables both stories simultaneously.

---

## Phase 1: Setup

**Purpose**: Verify environment and configuration prerequisites

- [x] T001 Verify Azure AD app registration has API scope exposed in Azure Portal (manual check)
- [x] T002 Document current AzureAd configuration in backend/src/ExpenseFlow.Api/appsettings.Staging.json

---

## Phase 2: Foundational (Authentication Fix)

**Purpose**: Fix 401 errors by aligning frontend token scopes with backend expectations

**⚠️ CRITICAL**: US1 and US2 cannot work until this phase is complete

### Backend Configuration

- [x] T003 Verify Audience value in backend/src/ExpenseFlow.Api/appsettings.json matches Azure AD app URI
- [x] T004 [P] Create or update backend/src/ExpenseFlow.Api/appsettings.Staging.json with correct AzureAd configuration

### Frontend Authentication Update

- [x] T005 Update API scopes in frontend/src/auth/authConfig.ts to request access token with api:// scope
- [x] T006 Update loginRequest scopes in frontend/src/auth/authConfig.ts to include API scope
- [x] T007 Add VITE_API_CLIENT_ID to frontend/.env.staging

### DTOs (Shared by US3 and US4)

- [x] T008 [P] Create PendingActionDto in backend/src/ExpenseFlow.Shared/DTOs/PendingActionDto.cs
- [x] T009 [P] Create CategoryBreakdownDto and CategorySpendingDto in backend/src/ExpenseFlow.Shared/DTOs/CategoryBreakdownDto.cs

**Checkpoint**: Authentication should now work - US1 and US2 are unblocked

---

## Phase 3: User Story 1 & 2 - Dashboard Metrics and Activity (Priority: P1) 🎯 MVP

**Goal**: Authenticated users can view dashboard metrics and recent activity without 401 errors

**Independent Test**: Login to staging, verify metrics cards and activity stream load successfully

### Verification (No Code Changes - Auth Fix Validates These)

- [x] T010 [US1] [US2] Build and test frontend locally to verify token acquisition works
- [ ] T011 [US1] [US2] Test dashboard/metrics endpoint with new access token using curl or Postman
- [ ] T012 [US1] [US2] Test dashboard/activity endpoint with new access token using curl or Postman

**Checkpoint**: US1 and US2 complete - dashboard metrics and activity load without 401 errors

---

## Phase 4: User Story 4 - Pending Actions Queue (Priority: P2)

**Goal**: Dashboard displays pending match reviews and categorization suggestions

**Independent Test**: Create a pending match, verify it appears in the actions queue

### Tests for User Story 4

- [x] T013 [P] [US4] Create unit test for GetActions endpoint in backend/tests/ExpenseFlow.Api.Tests/Controllers/DashboardControllerTests.cs

### Implementation for User Story 4

- [x] T014 [US4] Add GetActions endpoint to backend/src/ExpenseFlow.Api/Controllers/DashboardController.cs
- [x] T015 [US4] Add [HttpGet("actions")] route with limit parameter validation
- [x] T016 [US4] Query ReceiptTransactionMatches where Status = Proposed
- [x] T017 [US4] Map results to PendingActionDto with metadata

**Checkpoint**: US4 complete - pending actions endpoint returns data

---

## Phase 5: User Story 3 - Analytics Categories (Priority: P2)

**Goal**: Analytics page shows expense breakdown by category

**Independent Test**: Navigate to analytics, verify category breakdown chart displays data

### Tests for User Story 3

- [x] T018 [P] [US3] Create unit test for GetCategories endpoint in backend/tests/ExpenseFlow.Api.Tests/Controllers/AnalyticsControllerTests.cs

### Implementation for User Story 3

- [x] T019 [US3] Add GetCategories endpoint to backend/src/ExpenseFlow.Api/Controllers/AnalyticsController.cs
- [x] T020 [US3] Add [HttpGet("categories")] route with period parameter validation
- [x] T021 [US3] Implement period parsing with default to current month
- [x] T022 [US3] Group transactions by Category and calculate percentages
- [x] T023 [US3] Return CategoryBreakdownDto with ordered categories

**Checkpoint**: US3 complete - analytics categories endpoint returns data

---

## Phase 6: Polish & Deployment

**Purpose**: Build, deploy, and validate complete solution

### Build and Deploy

- [x] T024 Run backend unit tests with dotnet test
- [x] T025 Build frontend with npm run build
- [x] T026 [P] Build backend Docker image with --platform linux/amd64 flag
- [x] T027 [P] Build frontend Docker image with --platform linux/amd64 flag
- [x] T028 Push images to Azure Container Registry (iiusacr.azurecr.io)
- [x] T029 Update image tags in infrastructure/kubernetes/staging/deployment.yaml
- [x] T030 Update image tags in infrastructure/kubernetes/staging/frontend-deployment.yaml
- [x] T031 Commit and push to trigger deployment (PR #20 merged to main)

### End-to-End Validation

- [ ] T032 Verify staging dashboard loads without 401 errors (SC-001)
- [ ] T033 Verify staging dashboard loads without 404 errors (SC-002)
- [ ] T034 Verify dashboard loads within 3 seconds (SC-003)
- [ ] T035 Verify token refresh works without re-authentication (SC-004)
- [ ] T036 Run quickstart.md validation steps

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup
    ↓
Phase 2: Foundational (Auth Fix) ← BLOCKS ALL USER STORIES
    ↓
Phase 3: US1 & US2 (P1) - Verify auth fix works
    ↓ (can run in parallel with Phase 4, 5)
Phase 4: US4 (P2) - Add actions endpoint
Phase 5: US3 (P2) - Add categories endpoint
    ↓
Phase 6: Polish & Deploy
```

### User Story Dependencies

| Story | Depends On | Can Run Parallel With |
|-------|------------|----------------------|
| US1 | Phase 2 (Auth) | US2 |
| US2 | Phase 2 (Auth) | US1 |
| US3 | Phase 2 (Auth) | US4 |
| US4 | Phase 2 (Auth) | US3 |

### Within-Phase Parallel Opportunities

**Phase 2 (Foundational)**:
- T008 and T009 (DTOs) can run in parallel
- T003/T004 (backend config) can run parallel with T005/T006/T007 (frontend config)

**Phase 4 & 5 (US3 & US4)**:
- T013 and T018 (tests) can run in parallel
- Entire Phase 4 can run parallel with Phase 5

**Phase 6 (Deploy)**:
- T026 and T027 (Docker builds) can run in parallel

---

## Parallel Example: Foundational Phase

```bash
# These can run in parallel (different files):
Task: "Create PendingActionDto in backend/src/ExpenseFlow.Shared/DTOs/PendingActionDto.cs"
Task: "Create CategoryBreakdownDto in backend/src/ExpenseFlow.Shared/DTOs/CategoryBreakdownDto.cs"

# These can run in parallel (different directories):
Task: "Update backend appsettings.Staging.json"
Task: "Update frontend authConfig.ts"
```

---

## Implementation Strategy

### MVP First (Phase 1-3 Only)

1. Complete Phase 1: Setup verification
2. Complete Phase 2: Foundational (auth fix)
3. Complete Phase 3: Verify US1 & US2 work
4. **STOP and VALIDATE**: Dashboard metrics and activity load correctly
5. Deploy MVP - core dashboard functionality restored

### Full Feature (All Phases)

1. Complete MVP (Phases 1-3)
2. Complete Phase 4: US4 (pending actions)
3. Complete Phase 5: US3 (category analytics)
4. Complete Phase 6: Deploy and validate
5. All success criteria met

### Time Estimates

| Phase | Estimated Time | Cumulative |
|-------|---------------|------------|
| Setup | 15 min | 15 min |
| Foundational | 45 min | 1 hour |
| US1 & US2 | 15 min | 1h 15m |
| US4 | 30 min | 1h 45m |
| US3 | 30 min | 2h 15m |
| Deploy | 30 min | 2h 45m |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story
- US1 and US2 share root cause - auth fix enables both
- US3 and US4 are independent 404 fixes
- Verify tests fail before implementing (T013, T018)
- Commit after each logical group
- Use `--platform linux/amd64` for Docker builds (Apple Silicon → AKS)
