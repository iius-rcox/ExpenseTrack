# Tasks: Analytics Dashboard API Endpoints

**Input**: Design documents from `/specs/019-analytics-dashboard/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅, quickstart.md ✅

**Tests**: No test tasks included (not explicitly requested in feature specification).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Web app**: `backend/src/`, `frontend/src/`
- Backend follows Clean Architecture: Api → Infrastructure → Core → Shared

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create DTOs and service interface that all endpoints depend on

- [x] T001 [P] Create SpendingTrendItemDto record in `backend/src/ExpenseFlow.Shared/DTOs/AnalyticsDtos.cs`
- [x] T002 [P] Create SpendingByCategoryItemDto record in `backend/src/ExpenseFlow.Shared/DTOs/AnalyticsDtos.cs`
- [x] T003 [P] Create SpendingByVendorItemDto record in `backend/src/ExpenseFlow.Shared/DTOs/AnalyticsDtos.cs`
- [x] T004 [P] Create TopMerchantDto record in `backend/src/ExpenseFlow.Shared/DTOs/AnalyticsDtos.cs`
- [x] T005 [P] Create MerchantAnalyticsResponseDto record in `backend/src/ExpenseFlow.Shared/DTOs/AnalyticsDtos.cs`
- [x] T006 [P] Create AnalyticsDateRangeDto record in `backend/src/ExpenseFlow.Shared/DTOs/AnalyticsDtos.cs`
- [x] T007 [P] Create AnalyticsSubscriptionResponseDto record in `backend/src/ExpenseFlow.Shared/DTOs/AnalyticsDtos.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T008 Create IAnalyticsService interface in `backend/src/ExpenseFlow.Core/Interfaces/IAnalyticsService.cs` with methods for all 5 endpoints
- [x] T009 Create AnalyticsService class skeleton in `backend/src/ExpenseFlow.Infrastructure/Services/AnalyticsService.cs` implementing IAnalyticsService
- [x] T010 Register IAnalyticsService in DI container in `backend/src/ExpenseFlow.Infrastructure/Extensions/ServiceCollectionExtensions.cs`
- [x] T011 Create AnalyticsValidation static class in `backend/src/ExpenseFlow.Api/Validators/AnalyticsValidators.cs` with ValidateDateRange method (5-year max, startDate <= endDate)
- [x] T012 Add DeriveCategory helper method to AnalyticsService (port from existing AnalyticsController pattern-matching logic)

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - View Spending Trends Over Time (Priority: P1) 🎯 MVP

**Goal**: Provide spending trends with day/week/month granularity so users can identify patterns in expense behavior

**Independent Test**: Request spending trends for a date range and verify aggregated data matches transaction totals

### Implementation for User Story 1

- [x] T013 [US1] Implement GetSpendingTrendAsync method in `backend/src/ExpenseFlow.Infrastructure/Services/AnalyticsService.cs` with database-side GroupBy aggregation
- [x] T014 [US1] Add ISO week calculation helper for weekly granularity in AnalyticsService (use CalendarWeekRule.FirstFourDayWeek)
- [x] T015 [US1] Add GetSpendingTrend endpoint in `backend/src/ExpenseFlow.Api/Controllers/AnalyticsController.cs` at `GET /api/analytics/spending-trend`
- [x] T016 [US1] Add date validation and error handling to spending-trend endpoint using AnalyticsValidation.ValidateDateRange

**Checkpoint**: User Story 1 should be fully functional - users can view spending trends with day/week/month granularity

---

## Phase 4: User Story 2 - View Spending by Category (Priority: P1)

**Goal**: Provide category breakdown so users can understand where their money goes

**Independent Test**: Request category breakdown for a period and verify percentages sum to 100% and amounts match transaction data

### Implementation for User Story 2

- [x] T017 [US2] Implement GetSpendingByCategoryAsync method in `backend/src/ExpenseFlow.Infrastructure/Services/AnalyticsService.cs` using DeriveCategory for pattern-based classification
- [x] T018 [US2] Calculate percentageOfTotal in GetSpendingByCategoryAsync (2 decimal places, handle empty result gracefully)
- [x] T019 [US2] Add GetSpendingByCategory endpoint in `backend/src/ExpenseFlow.Api/Controllers/AnalyticsController.cs` at `GET /api/analytics/spending-by-category`
- [x] T020 [US2] Add date validation to spending-by-category endpoint using AnalyticsValidation.ValidateDateRange

**Checkpoint**: User Story 2 should be fully functional - users can view category spending breakdown

---

## Phase 5: User Story 3 - View Top Merchants Analysis (Priority: P2)

**Goal**: Provide merchant analytics with trends so users can identify biggest expense sources and track vendor relationships

**Independent Test**: Request top merchants and verify ranking matches actual transaction aggregations

### Implementation for User Story 3

- [x] T021 [US3] Implement GetMerchantAnalyticsAsync method in `backend/src/ExpenseFlow.Infrastructure/Services/AnalyticsService.cs` with topCount and includeComparison support
- [x] T022 [US3] Add comparison period calculation in GetMerchantAnalyticsAsync (same duration, immediately preceding current period)
- [x] T023 [US3] Calculate trend direction (increasing/decreasing/stable) based on changePercent threshold
- [x] T024 [US3] Identify new merchants (present in current period but not in comparison period)
- [x] T025 [US3] Identify significant changes (>50% change in spending)
- [x] T026 [US3] Add GetMerchantAnalytics endpoint in `backend/src/ExpenseFlow.Api/Controllers/AnalyticsController.cs` at `GET /api/analytics/merchants`
- [x] T027 [US3] Add topCount parameter validation (1-100, default 10) to merchants endpoint

**Checkpoint**: User Story 3 should be fully functional - users can view top merchants with trend comparisons

---

## Phase 6: User Story 4 - View Subscription Detection Results (Priority: P2)

**Goal**: Expose subscription detection through analytics API so frontend can display subscription tracking in analytics dashboard context

**Independent Test**: Call analytics subscription endpoint and verify it returns same data as subscriptions controller

### Implementation for User Story 4

- [x] T028 [US4] Inject ISubscriptionDetectionService into AnalyticsService constructor
- [x] T029 [US4] Implement GetSubscriptionsAsync proxy method in `backend/src/ExpenseFlow.Infrastructure/Services/AnalyticsService.cs` delegating to ISubscriptionDetectionService
- [x] T030 [US4] Implement AnalyzeSubscriptionsAsync proxy method in AnalyticsService
- [x] T031 [US4] Implement AcknowledgeSubscriptionAsync proxy method in AnalyticsService
- [x] T032 [US4] Add GetSubscriptions endpoint in `backend/src/ExpenseFlow.Api/Controllers/AnalyticsController.cs` at `GET /api/analytics/subscriptions`
- [x] T033 [US4] Add AnalyzeSubscriptions endpoint at `POST /api/analytics/subscriptions/analyze`
- [x] T034 [US4] Add AcknowledgeSubscription endpoint at `POST /api/analytics/subscriptions/{subscriptionId}/acknowledge`
- [x] T035 [US4] Add minConfidence and frequency filter support to GetSubscriptions endpoint

**Checkpoint**: User Story 4 should be fully functional - subscription data accessible via analytics API

---

## Phase 7: User Story 5 - View Spending by Vendor (Priority: P2)

**Goal**: Provide vendor-level spending data so users can identify which vendors account for most expenses

**Independent Test**: Request vendor breakdown and verify amounts match transaction data grouped by normalized vendor names

### Implementation for User Story 5

- [x] T036 [US5] Implement GetSpendingByVendorAsync method in `backend/src/ExpenseFlow.Infrastructure/Services/AnalyticsService.cs` using description field for vendor grouping
- [x] T037 [US5] Calculate percentageOfTotal in GetSpendingByVendorAsync (2 decimal places)
- [x] T038 [US5] Add GetSpendingByVendor endpoint in `backend/src/ExpenseFlow.Api/Controllers/AnalyticsController.cs` at `GET /api/analytics/spending-by-vendor`
- [x] T039 [US5] Add date validation to spending-by-vendor endpoint using AnalyticsValidation.ValidateDateRange

**Checkpoint**: User Story 5 should be fully functional - users can view vendor spending breakdown

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T040 Add structured logging (Serilog) with correlation IDs to all AnalyticsService methods
- [x] T041 Add ProblemDetails error responses for all analytics endpoints (FR-007)
- [x] T042 Verify all endpoints return 200 with empty arrays when no data matches (FR-013)
- [x] T043 Verify all endpoints require [Authorize] attribute and filter by authenticated user (FR-006)
- [ ] T044 Run quickstart.md verification checklist against all endpoints
- [ ] T045 Performance test: Verify 500ms response for 90-day range with 1000 transactions (SC-002)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-7)**: All depend on Foundational phase completion
  - US1 and US2 are both P1 priority - complete before P2 stories
  - US3, US4, US5 are P2 priority - can proceed in parallel after P1 stories
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - Uses DeriveCategory from foundation
- **User Story 4 (P2)**: Can start after Foundational (Phase 2) - Depends on ISubscriptionDetectionService (already exists)
- **User Story 5 (P2)**: Can start after Foundational (Phase 2) - No dependencies on other stories

### Within Each User Story

- Service method implementation before controller endpoint
- Validation integrated into endpoint
- Story complete before moving to next priority (but P2 stories can run in parallel)

### Parallel Opportunities

**Phase 1 (All DTOs):**
```
T001, T002, T003, T004, T005, T006, T007 → All can run in parallel
```

**After Foundational Phase (P1 Stories):**
```
US1 (T013-T016) can run in parallel with US2 (T017-T020)
```

**After P1 Stories Complete (P2 Stories):**
```
US3 (T021-T027) can run in parallel with US4 (T028-T035) and US5 (T036-T039)
```

---

## Parallel Example: Phase 1 Setup

```bash
# Launch all DTO tasks together (all write to same file but different records):
# Note: In practice, these can be combined into one task since they're in the same file
Task: T001 "Create SpendingTrendItemDto record"
Task: T002 "Create SpendingByCategoryItemDto record"
Task: T003 "Create SpendingByVendorItemDto record"
Task: T004 "Create TopMerchantDto record"
Task: T005 "Create MerchantAnalyticsResponseDto record"
Task: T006 "Create AnalyticsDateRangeDto record"
Task: T007 "Create AnalyticsSubscriptionResponseDto record"
```

## Parallel Example: P1 User Stories

```bash
# After Foundational phase, launch both P1 stories in parallel:
# Developer A:
Task: T013 "Implement GetSpendingTrendAsync method"
Task: T014 "Add ISO week calculation helper"
Task: T015 "Add GetSpendingTrend endpoint"
Task: T016 "Add date validation"

# Developer B (parallel):
Task: T017 "Implement GetSpendingByCategoryAsync method"
Task: T018 "Calculate percentageOfTotal"
Task: T019 "Add GetSpendingByCategory endpoint"
Task: T020 "Add date validation"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (DTOs)
2. Complete Phase 2: Foundational (IAnalyticsService, AnalyticsService skeleton, validation)
3. Complete Phase 3: User Story 1 (spending-trend endpoint)
4. **STOP and VALIDATE**: Test `GET /api/analytics/spending-trend` endpoint
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 (spending-trend) → Test → Deploy (MVP!)
3. Add User Story 2 (spending-by-category) → Test → Deploy
4. Add User Story 3 (merchants) → Test → Deploy
5. Add User Story 4 (subscriptions proxy) → Test → Deploy
6. Add User Story 5 (spending-by-vendor) → Test → Deploy
7. Polish phase → Final validation

### Recommended Order for Solo Developer

1. **Phase 1**: Setup (all DTOs - ~15 min)
2. **Phase 2**: Foundational (interface, service, DI, validation - ~20 min)
3. **Phase 3**: US1 spending-trend (~30 min)
4. **Phase 4**: US2 spending-by-category (~20 min)
5. **Phase 5**: US3 merchants (~45 min - most complex)
6. **Phase 6**: US4 subscriptions proxy (~30 min)
7. **Phase 7**: US5 spending-by-vendor (~20 min)
8. **Phase 8**: Polish (~20 min)

**Total Estimated Time**: ~3-4 hours

---

## Notes

- [P] tasks = different files or independent sections, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- FR-015 reminder: Keep positive (expense) and negative (refund) transactions as separate line items
- FR-014 reminder: Reject date ranges > 5 years with 400 Bad Request
