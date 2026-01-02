# Tasks: Frontend Integration Tests

**Input**: Design documents from `/specs/022-frontend-integration-tests/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: This feature IS testing infrastructure. No separate tests are needed for the implementation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Frontend**: `frontend/src/`, `frontend/tests/`
- All paths are relative to repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Install dependencies and create base project structure

- [x] T001 Install MSW 2.x dependency via `pnpm add -D msw@^2.7` in frontend/package.json
- [x] T002 [P] Install openapi-typescript dependency via `pnpm add -D openapi-typescript@^7` in frontend/package.json
- [x] T003 Create test-utils directory structure at frontend/src/test-utils/
- [x] T004 [P] Create integration tests directory at frontend/tests/integration/
- [x] T005 [P] Create contracts tests directory at frontend/tests/contracts/
- [x] T006 [P] Create error-boundary components directory at frontend/src/components/error-boundary/

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T007 Create MSW handlers for analytics endpoints in frontend/src/test-utils/msw-handlers.ts
- [x] T008 [P] Create MSW handlers for dashboard endpoint in frontend/src/test-utils/msw-handlers.ts
- [x] T009 Create auth mock utilities with MockAccount and AuthState in frontend/src/test-utils/auth-mock.ts
- [x] T010 [P] Create test fixture factories (createMockAccount, createAuthState, createMonthlyComparison, etc.) in frontend/src/test-utils/fixtures.ts
- [x] T011 Create render-with-providers test wrapper component in frontend/src/test-utils/render-with-providers.tsx
- [x] T012 Update test setup to initialize MSW server in frontend/tests/setup.ts
- [x] T013 Update Vitest config with integration test project in frontend/vitest.config.ts
- [x] T014 [P] Add test:integration script to frontend/package.json
- [x] T015 [P] Add test:contracts script to frontend/package.json
- [x] T016 [P] Add generate:api-types script to frontend/package.json

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Authentication Flow Testing (Priority: P1) 🎯 MVP

**Goal**: Validate login/logout/redirect using MSAL test utilities and Playwright E2E

**Independent Test**: Run `pnpm e2e tests/e2e/auth-flow.spec.ts` to verify auth flows work correctly

### Implementation for User Story 1

- [x] T017 [US1] Create Playwright auth setup with storageState persistence in frontend/tests/e2e/auth.setup.ts
- [x] T018 [US1] Implement login redirect test (Given user on login, When auth completes, Then redirect to dashboard) in frontend/tests/e2e/auth-flow.spec.ts
- [x] T019 [US1] Implement deep link redirect test (Given user with deep link, When auth completes, Then redirect to original URL) in frontend/tests/e2e/auth-flow.spec.ts
- [x] T020 [US1] Implement session expiry test (Given authenticated user, When token expires, Then redirect to login with return URL) in frontend/tests/e2e/auth-flow.spec.ts
- [x] T021 [US1] Implement protected route guard test (Given unauthenticated user, When accessing protected route, Then redirect to login) in frontend/tests/e2e/auth-flow.spec.ts
- [x] T022 [US1] Implement auth failure test (Given user completing login, When auth fails, Then display error message) in frontend/tests/e2e/auth-flow.spec.ts
- [x] T023 [US1] Implement logout flow test in frontend/tests/e2e/auth-flow.spec.ts

**Checkpoint**: At this point, User Story 1 should be fully functional - auth flows are E2E tested

---

## Phase 4: User Story 2 - Analytics Page Integration Testing (Priority: P1)

**Goal**: Test analytics page with realistic API responses via MSW

**Independent Test**: Run `pnpm test:integration tests/integration/analytics.test.tsx` to verify analytics page integration

### Implementation for User Story 2

- [x] T024 [P] [US2] Create analytics integration test file with provider setup in frontend/tests/integration/analytics.test.tsx
- [x] T025 [US2] Implement successful render test (Given all APIs succeed, When page loads, Then all sections render) in frontend/tests/integration/analytics.test.tsx
- [x] T026 [US2] Implement partial failure test (Given one API fails, When page loads, Then section shows error while others work) in frontend/tests/integration/analytics.test.tsx
- [x] T027 [US2] Implement empty data test (Given APIs return empty arrays, When page loads, Then empty states displayed) in frontend/tests/integration/analytics.test.tsx
- [x] T028 [US2] Implement malformed data test (Given API returns unexpected shape, When page loads, Then graceful degradation not crash) in frontend/tests/integration/analytics.test.tsx
- [x] T029 [US2] Implement loading state test (Given data is fetching, When page renders, Then loading indicators shown) in frontend/tests/integration/analytics.test.tsx
- [x] T030 [P] [US2] Create MSW handler variants for error states (401, 500) in frontend/src/test-utils/msw-handlers.ts

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - API Contract Validation (Priority: P2)

**Goal**: Ensure frontend types match backend OpenAPI spec at build time

**Independent Test**: Run `pnpm test:contracts` to verify type compatibility

### Implementation for User Story 3

- [x] T031 [US3] Create script to fetch OpenAPI spec from backend in frontend/scripts/fetch-openapi.ts
- [x] T032 [US3] Configure openapi-typescript to generate types from spec in frontend/scripts/generate-types.ts
- [x] T033 [US3] Create contract validation test with TypeScript type comparison in frontend/tests/contracts/api-contracts.test.ts
- [x] T034 [US3] Implement MonthlyComparisonResponse contract test in frontend/tests/contracts/api-contracts.test.ts
- [x] T035 [P] [US3] Implement SpendingTrendItem contract test in frontend/tests/contracts/api-contracts.test.ts
- [x] T036 [P] [US3] Implement CategoryBreakdownItem contract test in frontend/tests/contracts/api-contracts.test.ts
- [x] T037 [P] [US3] Implement MerchantAnalyticsResponse contract test in frontend/tests/contracts/api-contracts.test.ts
- [x] T038 [P] [US3] Implement SubscriptionDetectionResponse contract test in frontend/tests/contracts/api-contracts.test.ts
- [x] T039 [US3] Implement DashboardSummaryResponse contract test in frontend/tests/contracts/api-contracts.test.ts

**Checkpoint**: Contract validation now catches type mismatches automatically

---

## Phase 6: User Story 4 - Page Route Testing (Priority: P2)

**Goal**: Verify all page routes render without errors

**Independent Test**: Run `pnpm test:integration tests/integration/routes.test.tsx` to verify all routes render

### Implementation for User Story 4

- [x] T040 [US4] Create route integration test file with MSW and provider setup in frontend/tests/integration/routes.test.tsx
- [x] T041 [P] [US4] Implement dashboard page render test in frontend/tests/integration/dashboard.test.tsx
- [x] T042 [P] [US4] Implement transactions page render test in frontend/tests/integration/transactions.test.tsx
- [x] T043 [P] [US4] Implement receipts page render test in frontend/tests/integration/receipts.test.tsx
- [x] T044 [P] [US4] Implement reports page render test in frontend/tests/integration/reports.test.tsx
- [x] T045 [P] [US4] Implement settings page render test in frontend/tests/integration/settings.test.tsx
- [x] T046 [US4] Implement loading state verification across all routes in frontend/tests/integration/routes.test.tsx
- [x] T047 [US4] Implement console error detection for all route tests in frontend/tests/integration/routes.test.tsx

**Checkpoint**: All routes now have integration test coverage

---

## Phase 7: User Story 5 - Error Boundary Coverage (Priority: P3)

**Goal**: Implement error boundaries for graceful degradation

**Independent Test**: Run `pnpm test:integration tests/integration/error-boundary.test.tsx` to verify error handling

### Implementation for User Story 5

- [x] T048 [US5] Create ErrorBoundary class component with state management in frontend/src/components/error-boundary/ErrorBoundary.tsx
- [x] T049 [P] [US5] Create ErrorFallback UI component with retry button in frontend/src/components/error-boundary/ErrorFallback.tsx
- [x] T050 [P] [US5] Create error-boundary index export file in frontend/src/components/error-boundary/index.ts
- [x] T051 [US5] Wrap analytics page sections with error boundaries in frontend/src/routes/_authenticated/analytics/index.tsx
- [x] T052 [US5] Create error boundary integration test (Given component throws, When wrapped in boundary, Then fallback shown) in frontend/tests/integration/error-boundary.test.tsx
- [x] T053 [US5] Implement section isolation test (Given error in one section, When user interacts with others, Then others work) in frontend/tests/integration/error-boundary.test.tsx
- [x] T054 [US5] Implement retry functionality test (Given error caught, When user clicks retry, Then component re-renders) in frontend/tests/integration/error-boundary.test.tsx

**Checkpoint**: All major page sections now protected by error boundaries

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: CI integration and final validation

- [x] T055 [P] Update ci-full.yml to run integration tests after unit tests in .github/workflows/ci-full.yml
- [x] T056 [P] Update ci-full.yml to run contract tests in .github/workflows/ci-full.yml
- [x] T057 [P] Add test:all script combining unit, integration, and contract tests in frontend/package.json
- [x] T058 Verify test suite completes in <3 minutes (performance validation)
- [x] T059 Run quickstart.md validation - ensure all commands work as documented
- [x] T060 Update frontend testing documentation in docs/testing/frontend-features.md

**Checkpoint**: All CI integration complete, documentation updated

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-7)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Auth Flow - Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Analytics Integration - Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 3 (P2)**: Contract Validation - Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 4 (P2)**: Route Testing - Can start after Foundational (Phase 2) - May reuse MSW handlers from US2
- **User Story 5 (P3)**: Error Boundaries - Can start after Foundational (Phase 2) - Error boundaries tested on analytics page

### Within Each User Story

- Infrastructure before tests
- Tests should fail initially (no implementation yet for new files)
- Core implementation before advanced scenarios
- Story complete before moving to next priority

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel (T002, T004, T005, T006)
- All Foundational tasks marked [P] can run in parallel (T008, T010, T14, T015, T016)
- Once Foundational phase completes, User Stories 1-4 can start in parallel
- Contract tests (T035-T038) can run in parallel
- Route tests (T041-T045) can run in parallel
- Error boundary component creation (T048-T050) can run in parallel

---

## Parallel Example: Phase 2 (Foundational)

```bash
# These can run in parallel:
Task T007: "Create MSW handlers for analytics endpoints in frontend/src/test-utils/msw-handlers.ts"
Task T008: "Create MSW handlers for dashboard endpoint in frontend/src/test-utils/msw-handlers.ts"
Task T010: "Create test fixture factories in frontend/src/test-utils/fixtures.ts"
Task T014: "Add test:integration script to frontend/package.json"
Task T015: "Add test:contracts script to frontend/package.json"
Task T016: "Add generate:api-types script to frontend/package.json"
```

## Parallel Example: User Story 4 (Route Testing)

```bash
# These can run in parallel:
Task T041: "Implement dashboard page render test in frontend/tests/integration/dashboard.test.tsx"
Task T042: "Implement transactions page render test in frontend/tests/integration/transactions.test.tsx"
Task T043: "Implement receipts page render test in frontend/tests/integration/receipts.test.tsx"
Task T044: "Implement reports page render test in frontend/tests/integration/reports.test.tsx"
Task T045: "Implement settings page render test in frontend/tests/integration/settings.test.tsx"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

1. Complete Phase 1: Setup (~15 min)
2. Complete Phase 2: Foundational (~45 min)
3. Complete Phase 3: User Story 1 - Auth Flow (~60 min)
4. Complete Phase 4: User Story 2 - Analytics Integration (~45 min)
5. **STOP and VALIDATE**: Both P1 stories now have full test coverage
6. This addresses the immediate production bugs (login redirect, analytics crash)

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 + 2 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 3 → Contract validation → Deploy/Demo
4. Add User Story 4 → Route coverage → Deploy/Demo
5. Add User Story 5 → Error boundaries → Deploy/Demo
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (Auth E2E)
   - Developer B: User Story 2 (Analytics Integration)
   - Developer C: User Story 3 (Contracts) + User Story 4 (Routes)
3. User Story 5 (Error Boundaries) can be done last by anyone

---

## Task Summary

| Phase | Tasks | Parallel Tasks | Story |
|-------|-------|----------------|-------|
| Phase 1: Setup | 6 | 4 | - |
| Phase 2: Foundational | 10 | 5 | - |
| Phase 3: Auth Flow | 7 | 0 | US1 |
| Phase 4: Analytics Integration | 7 | 2 | US2 |
| Phase 5: Contract Validation | 9 | 4 | US3 |
| Phase 6: Route Testing | 8 | 5 | US4 |
| Phase 7: Error Boundaries | 7 | 2 | US5 |
| Phase 8: Polish | 6 | 3 | - |
| **Total** | **60** | **25** | - |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
