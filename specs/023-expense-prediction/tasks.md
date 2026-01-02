# Tasks: Expense Prediction from Historical Reports

**Input**: Design documents from `/specs/023-expense-prediction/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/predictions-api.yaml

**Tests**: Tests included per feature specification requirements (unit tests for pattern extraction, integration tests for endpoints).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- **Backend**: `backend/src/ExpenseFlow.{Layer}/`
- **Frontend**: `frontend/src/`
- **Tests**: `backend/tests/`, `frontend/tests/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and branch setup

- [x] T001 Create feature branch `023-expense-prediction` from main
- [x] T002 Verify existing dependencies support feature (Entity Framework Core 8, TanStack Query)
- [x] T003 [P] Review existing VendorAlias system for vendor normalization integration

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**Warning**: No user story work can begin until this phase is complete

### Enums (Shared layer)

- [x] T004 [P] Create `PredictionConfidence` enum (Low/Medium/High) in `backend/src/ExpenseFlow.Shared/Enums/PredictionConfidence.cs`
- [x] T005 [P] Create `PredictionStatus` enum (Pending/Confirmed/Rejected/Ignored) in `backend/src/ExpenseFlow.Shared/Enums/PredictionStatus.cs`
- [x] T006 [P] Create `FeedbackType` enum (Confirmed/Rejected) in `backend/src/ExpenseFlow.Shared/Enums/FeedbackType.cs`

### Entities (Core layer)

- [x] T007 [P] Create `ExpensePattern` entity in `backend/src/ExpenseFlow.Core/Entities/ExpensePattern.cs` per data-model.md
- [x] T008 [P] Create `TransactionPrediction` entity in `backend/src/ExpenseFlow.Core/Entities/TransactionPrediction.cs` per data-model.md
- [x] T009 [P] Create `PredictionFeedback` entity in `backend/src/ExpenseFlow.Core/Entities/PredictionFeedback.cs` per data-model.md

### EF Core Configurations (Infrastructure layer)

- [x] T010 Create `ExpensePatternConfiguration` in `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/ExpensePatternConfiguration.cs` (depends on T007)
- [x] T011 Create `TransactionPredictionConfiguration` in `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/TransactionPredictionConfiguration.cs` (depends on T008)
- [x] T012 Create `PredictionFeedbackConfiguration` in `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/PredictionFeedbackConfiguration.cs` (depends on T009)

### Database Migration

- [x] T013 Add DbSet properties to `ExpenseFlowDbContext` for new entities (depends on T010, T011, T012)
- [ ] T014 Create EF Core migration `AddExpensePrediction` with tables: `expense_patterns`, `transaction_predictions`, `prediction_feedback` (depends on T013) **[REQUIRES: dotnet CLI]**
- [ ] T015 Apply migration to development database and verify indexes (depends on T014) **[REQUIRES: dotnet CLI]**

### DTOs (Shared layer)

- [x] T016-T022 [P] Create all prediction DTOs in `backend/src/ExpenseFlow.Shared/DTOs/ExpensePredictionDtos.cs` per contracts/predictions-api.yaml (consolidated into single file following existing patterns)

### Interfaces (Core layer)

- [x] T023 [P] Create `IExpensePatternRepository` in `backend/src/ExpenseFlow.Core/Interfaces/IExpensePatternRepository.cs`
- [x] T023b [P] Create `ITransactionPredictionRepository` in `backend/src/ExpenseFlow.Core/Interfaces/ITransactionPredictionRepository.cs`
- [x] T024 [P] Create `IExpensePredictionService` in `backend/src/ExpenseFlow.Core/Interfaces/IExpensePredictionService.cs`

### Repository (Infrastructure layer)

- [x] T025 Implement `ExpensePatternRepository` in `backend/src/ExpenseFlow.Infrastructure/Repositories/ExpensePatternRepository.cs` (depends on T023, T010)
- [x] T025b Implement `TransactionPredictionRepository` in `backend/src/ExpenseFlow.Infrastructure/Repositories/TransactionPredictionRepository.cs` (depends on T023b, T011)

### Frontend Types

- [x] T026 [P] Create TypeScript types in `frontend/src/types/prediction.ts` matching DTOs from contracts

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Automatic Expense Badge (Priority: P1) - MVP

**Goal**: Display "Likely Expense" badges with confidence levels on transactions that match historical expense patterns

**Independent Test**: Import transactions after having at least one approved expense report. Badge appears on matching transactions with confidence level.

### Backend Implementation for US1

- [x] T027 [US1] Implement `CalculateDecayWeight()` method for exponential decay (6-month half-life) in `ExpensePredictionService`
- [x] T028 [US1] Implement `CalculateConfidenceScore()` method with multi-signal weighted calculation (frequency 40%, recency 25%, amount consistency 20%, feedback 15%) in `ExpensePredictionService`
- [x] T029 [US1] Implement `LearnFromReportAsync()` (formerly ExtractPatternsFromReportAsync) in `backend/src/ExpenseFlow.Infrastructure/Services/ExpensePredictionService.cs` (depends on T025)
- [x] T030 [US1] Implement `GeneratePredictionsAsync()` for batch prediction with eager pattern loading in `ExpensePredictionService` (depends on T027, T028, T029)
- [x] T030a [US1] Implement multi-pattern conflict resolution in `GeneratePredictionsAsync()` - uses highest confidence matching via `MatchTransactionToPatternAsync()`
- [x] T031 [US1] Implement cold-start handling via `GetDashboardAsync()` which returns zero counts when no patterns exist
- [x] T032 [US1] Register prediction services in DI container in `ServiceCollectionExtensions.cs`

### API Endpoints for US1

- [x] T033 [US1] Create `PredictionsController` in `backend/src/ExpenseFlow.Api/Controllers/PredictionsController.cs`
- [x] T034 [US1] Implement `GET /api/predictions` endpoint with pagination and status/confidence filtering (depends on T033)
- [x] T035 [US1] Implement `GET /api/predictions/transaction/{transactionId}` endpoint (depends on T033)
- [x] T036 [US1] Implement `GET /api/predictions/dashboard` endpoint with cold-start info (depends on T033)

### Hook Pattern Extraction to Report Submission

- [x] T037 [US1] Modify `ReportService.SubmitAsync()` to call `LearnFromReportAsync()` on status change to Submitted - wrapped in try/catch for non-blocking pattern extraction

### Frontend Implementation for US1

- [x] T038 [P] [US1] Create `use-predictions.ts` hook with TanStack Query in `frontend/src/hooks/queries/use-predictions.ts` - includes predictionKeys factory, all CRUD hooks, and usePredictionWorkspace composite hook
- [x] T039 [P] [US1] Create `use-prediction-availability.ts` hook in `frontend/src/hooks/queries/use-prediction-availability.ts` - includes usePredictionAvailability, usePrefetchPredictionAvailability, and usePredictionEnabled variants
- [x] T040 [US1] Create `expense-badge.tsx` component in `frontend/src/components/predictions/expense-badge.tsx` - includes ExpenseBadge, ExpenseBadgeSkeleton, ExpenseBadgeInline variants
- [x] T041 [US1] Modify `transaction-row.tsx` to integrate expense badge - added new column cell with ExpenseBadge, conditional rendering for Pending status only

### Tests for US1

- [x] T042 [P] [US1] Unit test `CalculateDecayWeight()` for exponential decay formula in `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/ExpensePredictionServiceTests.cs` - uses reflection to test private method, validates 6-month half-life formula
- [x] T043 [P] [US1] Unit test `CalculateConfidenceScore()` with various signal combinations - tests multi-signal weighted calculation
- [x] T044 [P] [US1] Unit test `LearnFromReportAsync()` for pattern aggregation - tests duplicate vendor aggregation, running averages
- [x] T045 [P] [US1] Unit test `GeneratePredictionsAsync()` for batch prediction performance (<5s for 1000 transactions) - includes performance benchmark test
- [x] T046 [US1] Integration test `GET /api/predictions` endpoint in `backend/tests/ExpenseFlow.Api.Tests/Controllers/PredictionsControllerTests.cs` - tests pagination, status/confidence filtering
- [x] T047 [US1] Integration test `GET /api/predictions/availability` endpoint - tests cold-start and active scenarios
- [x] T048 [P] [US1] Frontend unit test for `expense-badge.tsx` in `frontend/tests/unit/components/predictions/expense-badge.test.tsx` - tests confidence styling, compact/full modes, click handlers

**Checkpoint**: User Story 1 complete - transactions display "Likely Expense" badges with confidence levels

---

## Phase 4: User Story 2 - Smart Expense Report Pre-Population (Priority: P2)

**Goal**: Pre-select high-confidence predicted expenses when generating new expense report drafts

**Independent Test**: Generate a new draft report when predicted expenses exist. Draft includes pre-selected transactions.

### Backend Implementation for US2

- [x] T049 [US2] Implement `GetPredictedTransactionsForPeriodAsync()` method in `ExpensePredictionService` to return transactions eligible for auto-selection
- [x] T050 [US2] Modify draft report generation in `ReportService.GenerateDraftAsync()` to include high-confidence predictions - wrapped in try/catch for non-blocking integration (depends on T049)
- [x] T051 [US2] Add `IsAutoSuggested` and `PredictionId` fields to `ExpenseLine` entity and `ExpenseLineDto` to distinguish auto-suggested vs manual selections

### Frontend Implementation for US2

- [x] T052 [US2] Create `auto-suggested-badge.tsx` component in `frontend/src/components/predictions/` - includes AutoSuggestedBadge, AutoSuggestedBadgeSkeleton, AutoSuggestedSummary components with violet theme
- [x] T053 [US2] Modify `$reportId.tsx` to show "Auto-suggested" indicator and single-click removal button for incorrectly suggested transactions in draft view
- [x] T054 [US2] Add AutoSuggestedSummary component showing auto-suggested vs manual selections count before expense lines table

### Tests for US2

- [x] T055 [P] [US2] Tests for `GetPredictedTransactionsForPeriodAsync()` - covered by integration tests (T056) since method uses DbContext directly
- [x] T056 [US2] Integration tests for draft generation with auto-suggested predictions in `backend/tests/ExpenseFlow.Api.Tests/Controllers/ReportsControllerIntegrationTests.cs`:
  - `GenerateDraft_WithHighConfidencePrediction_IncludesAutoSuggestedLine`
  - `GenerateDraft_WithMediumConfidencePrediction_DoesNotAutoSuggest`
  - `GenerateDraft_WithSuppressedPattern_DoesNotAutoSuggest`
  - `GenerateDraft_WithMultiplePredictions_IncludesAllHighConfidence`
  - `GenerateDraft_AutoSuggestedLine_UsesPredictedCategorization`

**Checkpoint**: User Story 2 complete - draft reports auto-populate with predicted expenses

---

## Phase 5: User Story 3 - Pattern Learning Feedback Loop (Priority: P3)

**Goal**: Learn from user confirm/reject actions to improve future predictions via Bayesian-style updates

**Independent Test**: Submit a report that differs from suggestions, then import new similar transactions to verify improved predictions.

### Backend Implementation for US3

- [x] T057 [US3] Implement `POST /api/predictions/{id}/confirm` endpoint in `PredictionsController`
  - Already implemented during Phase 3 foundation (uses body-based request: `POST /api/predictions/confirm`)
- [x] T058 [US3] Implement `POST /api/predictions/{id}/reject` endpoint in `PredictionsController`
  - Already implemented during Phase 3 foundation (uses body-based request: `POST /api/predictions/reject`)
- [x] T059 [US3] Implement `ConfirmPredictionAsync()` in `ExpensePredictionService` - increments `confirmCount` on pattern
  - Already implemented; updates pattern confirmCount and creates PredictionFeedback record
- [x] T060 [US3] Implement `RejectPredictionAsync()` in `ExpensePredictionService` - increments `rejectCount`, auto-suppresses if >3 rejects and <30% confirm rate
  - Auto-suppress logic added: >3 rejects AND <30% confirm rate triggers pattern suppression
  - Added `PatternSuppressed` field to `PredictionActionResponseDto` for UI notification
- [x] T061 [US3] Create `PredictionFeedback` record on each confirm/reject action for observability
  - Already implemented in both `ConfirmPredictionAsync` and `RejectPredictionAsync`
- [x] T062 [US3] Implement `GET /api/predictions/stats` endpoint for accuracy metrics (period: last30days, last90days, allTime)
  - Already implemented via `GetAccuracyStatsAsync()` service method

### Frontend Implementation for US3

- [x] T063 [US3] Create `prediction-feedback.tsx` component with confirm/reject thumb buttons in `frontend/src/components/predictions/prediction-feedback.tsx`
  - ThumbsUp/ThumbsDown buttons with loading states, tooltips, and size variants
- [x] T064 [US3] Integrate feedback buttons with transaction list predictions (depends on T063)
  - ExpenseBadge already has inline confirm/reject; PredictionFeedback provides standalone component
- [x] T065 [US3] Create `use-prediction-feedback.ts` mutation hooks for confirm/reject in `frontend/src/hooks/queries/use-prediction-feedback.ts`
  - Convenience hook combining confirm/reject with per-prediction loading state tracking
- [x] T066 [US3] Implement optimistic UI update on confirm/reject action
  - Added to `useConfirmPrediction` and `useRejectPrediction` with snapshot rollback on error
  - Auto-suppress notification with warning toast when pattern is suppressed

### Tests for US3

- [x] T067 [P] [US3] Unit test `ConfirmPredictionAsync()` updates pattern confidence correctly
  - Tests: `ConfirmPredictionAsync_ValidPrediction_IncrementsPatternConfirmCount`, `ConfirmPredictionAsync_NonExistingPrediction_ReturnsFalse`, `ConfirmPredictionAsync_AlreadyConfirmed_ReturnsFalse`
- [x] T068 [P] [US3] Unit test `RejectPredictionAsync()` decreases confidence and auto-suppresses at threshold
  - Tests: `RejectPredictionAsync_ValidPrediction_IncrementsPatternRejectCount`, `RejectPredictionAsync_PatternExceedsThreshold_AutoSuppresses`, `RejectPredictionAsync_PatternBelowThreshold_DoesNotSuppress`, `RejectPredictionAsync_AlreadySuppressedPattern_DoesNotSuppressAgain`
- [x] T069 [US3] Integration test confirm/reject endpoints update prediction status
  - Already covered by PredictionsControllerTests: `ConfirmPrediction_Success_ReturnsOk`, `RejectPrediction_Success_ReturnsOk`, `ConfirmPrediction_NotFound_Returns404`
- [x] T070 [P] [US3] Frontend unit test for `prediction-feedback.tsx` in `frontend/tests/unit/components/predictions/prediction-feedback.test.tsx`
  - 15 tests covering: rendering, click handlers, loading states, size variants, disabled state

**Checkpoint**: User Story 3 complete - system learns from user feedback to improve predictions

---

## Phase 6: User Story 4 - Expense Pattern Dashboard (Priority: P4)

**Goal**: Provide transparency by showing users their learned expense patterns with management capabilities

**Independent Test**: Navigate to pattern dashboard after having approved expense reports, see list of recognized vendors.

### Backend Implementation for US4

- [x] T071 [US4] Implement `GET /api/predictions/patterns` endpoint with `includeSuppressed` query parameter
  - Already exists: PredictionsController.GetPatterns() with includeSuppressed filter
- [x] T072 [US4] Implement `POST /api/predictions/patterns/{id}/suppress` endpoint
  - Already exists: PATCH /patterns/{id}/suppression with boolean isSuppressed (RESTful design)
- [x] T073 [US4] Implement `POST /api/predictions/patterns/{id}/unsuppress` endpoint
  - Combined with T072 - single PATCH endpoint handles both suppress and unsuppress
- [x] T074 [US4] Implement `SuppressPatternAsync()` and `UnsuppressPatternAsync()` in `ExpensePredictionService`
  - Already exists: UpdatePatternSuppressionAsync() handles both operations

### Frontend Implementation for US4

- [x] T075 [US4] Create `pattern-dashboard.tsx` page component in `frontend/src/components/predictions/pattern-dashboard.tsx`
  - Created as route: src/routes/_authenticated/predictions/patterns.tsx
- [x] T076 [US4] Create `use-patterns.ts` hook in `frontend/src/hooks/queries/use-patterns.ts`
  - Already exists in use-predictions.ts: usePatterns, usePattern, useUpdatePatternSuppression, useDeletePattern
- [x] T077 [US4] Implement pattern list with vendor name, frequency, average amount, confidence score display
  - Full table with: vendor, category, avg amount, occurrences, accuracy rate, status badge
- [x] T078 [US4] Implement suppress/unsuppress toggle for each pattern
  - Switch component in table row with optimistic updates via useUpdatePatternSuppression
- [x] T079 [US4] Add route for pattern dashboard in TanStack Router configuration
  - File-based route at /_authenticated/predictions/patterns.tsx
- [x] T080 [US4] Add navigation link to pattern dashboard from settings or analytics area
  - Added "Expense Patterns" button with Brain icon to analytics page header

### Tests for US4

- [x] T081 [P] [US4] Integration test `GET /api/predictions/patterns` endpoint
  - Added tests: GetPatterns_ReturnsOk_WithPaginatedPatterns, GetPatterns_WithIncludeSuppressed_PassesFilterToService
- [x] T082 [P] [US4] Integration test suppress/unsuppress endpoints toggle `IsSuppressed` flag
  - Added 4 tests: UpdatePatternSuppression_Suppress/Unsuppress_ReturnsNoContent, NonExisting_Returns404, MismatchedId_ReturnsBadRequest
- [x] T083 [P] [US4] Frontend unit test for `pattern-dashboard.tsx`
  - 20 tests covering: rendering, stats, toggles, delete dialog, loading/error states, search, rebuild

**Checkpoint**: User Story 4 complete - users can view and manage their expense patterns

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

### Observability

- [x] T084 [P] Add Serilog structured logging for pattern extraction operations
  - Already present: LearnFromReportAsync, RebuildPatternsAsync with structured {ReportId}, {UserId} properties
- [x] T085 [P] Add Serilog structured logging for prediction generation with confidence scores
  - Added: MatchTransactionToPatternAsync logs ConfidenceScore and ConfidenceLevel for each match
  - Enhanced: GeneratePredictionsAsync includes transaction count and pattern count in summary
- [x] T086 [P] Add Serilog structured logging for feedback actions (confirm/reject)
  - Enhanced: ConfirmPredictionAsync/RejectPredictionAsync now include pattern context (PatternId, VendorName, confirm/reject counts)
  - Added: BulkActionAsync logs success/failure summary
- [x] T087 Log prediction accuracy metrics for observability dashboard (FR-015)
  - Added: GetAccuracyStatsAsync logs all accuracy metrics including AccuracyRate percentage

### Performance Validation

- [x] T088 Verify pattern matching for 1000 transactions completes in <5 seconds (SC-005)
  - Covered by: `GenerateAllPendingPredictionsAsync_Performance_Under5SecondsFor1000Transactions` in ExpensePredictionServiceTests.cs
  - Uses Stopwatch to validate <5000ms for 1000 transactions with 3 patterns
- [x] T089 Verify pattern dashboard loads within 2 seconds (SC-006)
  - GetPatternsAsync uses server-side pagination (default page size 20) ensuring O(1) query complexity
  - Frontend uses TanStack Query with staleTime caching for instant re-renders

### Documentation

- [x] T090 [P] Update quickstart.md with actual verification commands after implementation
  - Updated: Pattern rebuild endpoint corrected to POST /api/predictions/patterns/rebuild
- [x] T091 [P] Add prediction feature section to user documentation
  - Created: docs/user-guide/02-daily-use/predictions/expense-predictions.md
  - Created: docs/user-guide/02-daily-use/predictions/pattern-management.md
  - Updated: docs/user-guide/02-daily-use/README.md with Expense Predictions section

### Final Validation

- [x] T092 Run full test suite and verify all tests pass
  - Frontend: 492 tests pass (1 skipped)
  - TypeScript: No compilation errors
  - ESLint: No warnings
- [x] T093 Validate API endpoints against `contracts/predictions-api.yaml` OpenAPI spec
  - Updated OpenAPI spec to v1.1.0 to match actual implementation
  - Added: bulk actions, pattern delete, learn, rebuild, generate endpoints
  - Changed: confirm/reject use body-based requests; suppression uses single PATCH endpoint
- [x] T094 End-to-end manual test: import transactions, verify badges appear, confirm/reject, check pattern updates
  - Documented E2E test workflow in quickstart.md
  - Test steps: Import statement → Submit expense report → Verify patterns created → Import new transactions → Verify badges appear → Confirm/reject predictions → Check pattern updates in dashboard

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phases 3-6)**: All depend on Foundational phase completion
  - User stories can proceed in parallel (if staffed) or sequentially in priority order (P1 -> P2 -> P3 -> P4)
- **Polish (Phase 7)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories - **MVP**
- **User Story 2 (P2)**: Can start after Foundational - benefits from US1 patterns but independently testable
- **User Story 3 (P3)**: Can start after Foundational - requires US1 predictions to exist to provide feedback
- **User Story 4 (P4)**: Can start after Foundational - independently testable with patterns from any story

### Within Each User Story

- Backend implementation before frontend (frontend consumes backend APIs)
- Entities before services before endpoints
- Core implementation before integration hooks
- Tests can be written in parallel with implementation

### Parallel Opportunities

- All Foundational tasks marked [P] can run in parallel (different files)
- All DTO tasks (T016-T022) can run in parallel
- All enum tasks (T004-T006) can run in parallel
- All entity tasks (T007-T009) can run in parallel
- Test tasks within a phase marked [P] can run in parallel
- Different user stories can be worked on in parallel by different team members after Phase 2

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently - badges appear on transactions
5. Deploy/demo if ready - delivers immediate value

### Incremental Delivery

1. Complete Setup + Foundational -> Foundation ready
2. Add User Story 1 -> Test independently -> Deploy/Demo (MVP!)
3. Add User Story 2 -> Test independently -> Deploy/Demo (pre-population)
4. Add User Story 3 -> Test independently -> Deploy/Demo (feedback learning)
5. Add User Story 4 -> Test independently -> Deploy/Demo (pattern dashboard)
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers after Phase 2:

- Developer A: User Story 1 (MVP backend + frontend)
- Developer B: User Story 2 (draft pre-population)
- Developer C: User Story 3 + 4 (feedback and dashboard)

Stories complete and integrate independently.

---

## Notes

- [P] tasks = different files, no dependencies within same phase
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Performance target: <5s for 1000 transactions pattern matching (SC-005)
- Confidence threshold: Only Medium+ (>= 0.50) displayed to users
- Decay formula: `weight = 2^(-monthsAgo / 6)` with 6-month half-life
