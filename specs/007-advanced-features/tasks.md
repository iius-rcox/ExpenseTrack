# Tasks: Advanced Features

**Input**: Design documents from `/specs/007-advanced-features/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Tests included per established project patterns (xUnit, FluentAssertions, Moq).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- **Web app**: `backend/src/`, `backend/tests/`
- Paths follow existing project structure from Sprint 5-6

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Database migrations and shared enums/DTOs that all user stories depend on

- [x] T001 [P] Create VendorCategory enum in backend/src/ExpenseFlow.Shared/Enums/VendorCategory.cs
- [x] T002 [P] Create TravelPeriodSource enum in backend/src/ExpenseFlow.Shared/Enums/TravelPeriodSource.cs
- [x] T003 [P] Create SubscriptionStatus enum in backend/src/ExpenseFlow.Shared/Enums/SubscriptionStatus.cs
- [x] T004 [P] Create DetectionSource enum in backend/src/ExpenseFlow.Shared/Enums/DetectionSource.cs
- [x] T005 Add Category property to VendorAlias entity in backend/src/ExpenseFlow.Core/Entities/VendorAlias.cs
- [x] T006 Create EF migration for VendorAlias.Category column with airline/hotel seed data

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core entities and infrastructure that MUST be complete before ANY user story can be implemented

**Note**: No user story work can begin until this phase is complete

- [x] T007 [P] Create TravelPeriod entity in backend/src/ExpenseFlow.Core/Entities/TravelPeriod.cs
- [x] T008 [P] Create DetectedSubscription entity in backend/src/ExpenseFlow.Core/Entities/DetectedSubscription.cs
- [x] T009 [P] Create KnownSubscriptionVendor entity in backend/src/ExpenseFlow.Core/Entities/KnownSubscriptionVendor.cs
- [x] T010 Add UserId property to SplitPattern entity in backend/src/ExpenseFlow.Core/Entities/SplitPattern.cs
- [x] T011 Add DbSet configurations for new entities in backend/src/ExpenseFlow.Infrastructure/Data/ExpenseFlowDbContext.cs
- [x] T012 Create EF migration for TravelPeriod table with indexes
- [x] T013 Create EF migration for DetectedSubscription table with indexes
- [x] T014 Create EF migration for KnownSubscriptionVendor table with seed data
- [x] T015 Create EF migration for SplitPattern.UserId with backfill and composite index
- [x] T016 [P] Create ITravelPeriodRepository interface in backend/src/ExpenseFlow.Core/Interfaces/ITravelPeriodRepository.cs
- [x] T017 [P] Create ISubscriptionRepository interface in backend/src/ExpenseFlow.Core/Interfaces/ISubscriptionRepository.cs
- [x] T018 [P] Create ISplitPatternRepository interface in backend/src/ExpenseFlow.Core/Interfaces/ISplitPatternRepository.cs
- [x] T019 [P] Implement TravelPeriodRepository in backend/src/ExpenseFlow.Infrastructure/Repositories/TravelPeriodRepository.cs
- [x] T020 [P] Implement SubscriptionRepository in backend/src/ExpenseFlow.Infrastructure/Repositories/SubscriptionRepository.cs
- [x] T021 [P] Implement SplitPatternRepository in backend/src/ExpenseFlow.Infrastructure/Repositories/SplitPatternRepository.cs
- [x] T022 Register new repositories in DI container in backend/src/ExpenseFlow.Infrastructure/Extensions/ServiceCollectionExtensions.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Travel Period Detection (Priority: P1)

**Goal**: Automatically detect business trips from flight/hotel receipts and suggest GL code 66300 for related expenses

**Independent Test**: Upload a flight receipt and hotel receipt for the same destination. Verify the system creates a travel period spanning those dates and automatically suggests GL code 66300 for expenses within that date range.

### DTOs for User Story 1

- [x] T023 [P] [US1] Create TravelPeriodDto in backend/src/ExpenseFlow.Shared/DTOs/TravelPeriodDto.cs
- [x] T024 [P] [US1] Create TravelDetectionResultDto in backend/src/ExpenseFlow.Shared/DTOs/TravelDetectionResultDto.cs
- [x] T025 [P] [US1] Create TravelExpenseDto in backend/src/ExpenseFlow.Shared/DTOs/TravelExpenseDto.cs

### Service Interface for User Story 1

- [x] T026 [US1] Create ITravelDetectionService interface in backend/src/ExpenseFlow.Core/Interfaces/ITravelDetectionService.cs

### Implementation for User Story 1

- [x] T027 [US1] Implement TravelDetectionService with vendor pattern matching in backend/src/ExpenseFlow.Infrastructure/Services/TravelDetectionService.cs
- [x] T028 [US1] Add airline vendor pattern recognition (Delta, United, American, Southwest, Alaska, JetBlue) in TravelDetectionService
- [x] T029 [US1] Add hotel vendor pattern recognition (Marriott, Hilton, Hyatt, IHG, Airbnb, VRBO) in TravelDetectionService
- [x] T030 [US1] Implement travel period creation from flight receipts with destination extraction
- [x] T031 [US1] Implement travel period extension from hotel receipts with date range calculation
- [x] T032 [US1] Implement GL code 66300 suggestion for expenses within travel period dates
- [x] T033 [US1] Add AI review flag logic for complex itineraries (FR-006)
- [x] T034 [US1] Register TravelDetectionService in DI container

### Controller for User Story 1

- [x] T035 [US1] Create TravelPeriodsController with CRUD endpoints in backend/src/ExpenseFlow.Api/Controllers/TravelPeriodsController.cs
- [x] T036 [US1] Implement GET /api/travel-periods endpoint with filtering
- [x] T037 [US1] Implement POST /api/travel-periods endpoint for manual creation
- [x] T038 [US1] Implement PUT /api/travel-periods/{id} endpoint for updates
- [x] T039 [US1] Implement DELETE /api/travel-periods/{id} endpoint
- [x] T040 [US1] Implement GET /api/travel-periods/{id}/expenses endpoint
- [x] T041 [US1] Implement POST /api/travel-periods/detect endpoint for receipt-triggered detection

### Integration for User Story 1

- [x] T042 [US1] Hook travel detection into receipt processing pipeline (trigger on airline/hotel vendor match)
- [x] T043 [US1] Add tier usage logging for travel detection operations (FR-020)

### Tests for User Story 1

- [ ] T044 [P] [US1] Create TravelDetectionServiceTests in backend/tests/ExpenseFlow.Tests.Unit/TravelDetectionServiceTests.cs
- [ ] T045 [P] [US1] Create TravelPeriodApiTests in backend/tests/ExpenseFlow.Tests.Integration/TravelPeriodApiTests.cs

**Checkpoint**: User Story 1 complete - travel periods can be detected from receipts and expenses within periods get GL 66300 suggestions

---

## Phase 4: User Story 2 - Subscription Detection (Priority: P2)

**Goal**: Automatically identify recurring subscription charges and alert users when expected subscriptions are missing

**Independent Test**: Import 3 months of statements containing charges from "OpenAI" around the same amount each month. Verify the system identifies this as a subscription and flags it appropriately.

### DTOs for User Story 2

- [ ] T046 [P] [US2] Create SubscriptionDto in backend/src/ExpenseFlow.Core/DTOs/SubscriptionDto.cs
- [ ] T047 [P] [US2] Create SubscriptionAlertDto in backend/src/ExpenseFlow.Core/DTOs/SubscriptionAlertDto.cs
- [ ] T048 [P] [US2] Create SubscriptionDetectionResultDto in backend/src/ExpenseFlow.Core/DTOs/SubscriptionDetectionResultDto.cs

### Service Interface for User Story 2

- [ ] T049 [US2] Create ISubscriptionDetectionService interface in backend/src/ExpenseFlow.Core/Interfaces/ISubscriptionDetectionService.cs

### Implementation for User Story 2

- [ ] T050 [US2] Implement SubscriptionDetectionService with pattern matching in backend/src/ExpenseFlow.Infrastructure/Services/SubscriptionDetectionService.cs
- [ ] T051 [US2] Implement consecutive month pattern detection (2+ months, <$5 variance for detection, ±20% for matching)
- [ ] T052 [US2] Implement known subscription vendor matching from seed data
- [ ] T053 [US2] Implement subscription status transitions (Active, Missing, Flagged)
- [ ] T054 [US2] Implement amount variance flagging (>20% from average)
- [ ] T055 [US2] Register SubscriptionDetectionService in DI container

### Background Job for User Story 2

- [ ] T056 [US2] Create SubscriptionAlertJob for month-end missing subscription detection in backend/src/ExpenseFlow.Infrastructure/Jobs/SubscriptionAlertJob.cs
- [ ] T057 [US2] Register SubscriptionAlertJob as Hangfire recurring job (1st of month at 4 AM) in Program.cs

### Controller for User Story 2

- [ ] T058 [US2] Create SubscriptionsController in backend/src/ExpenseFlow.Api/Controllers/SubscriptionsController.cs
- [ ] T059 [US2] Implement GET /api/subscriptions endpoint with status filtering
- [ ] T060 [US2] Implement GET /api/subscriptions/{id} endpoint with occurrence history
- [ ] T061 [US2] Implement DELETE /api/subscriptions/{id} endpoint (dismiss)
- [ ] T062 [US2] Implement POST /api/subscriptions/{id}/confirm endpoint
- [ ] T063 [US2] Implement GET /api/subscriptions/alerts endpoint
- [ ] T064 [US2] Implement POST /api/subscriptions/alerts/{id}/acknowledge endpoint
- [ ] T065 [US2] Implement POST /api/subscriptions/detect endpoint

### Integration for User Story 2

- [ ] T066 [US2] Hook subscription detection into statement import pipeline
- [ ] T067 [US2] Add tier usage logging for subscription detection operations (FR-020)

### Tests for User Story 2

- [ ] T068 [P] [US2] Create SubscriptionDetectionServiceTests in backend/tests/ExpenseFlow.Tests.Unit/SubscriptionDetectionServiceTests.cs
- [ ] T069 [P] [US2] Create SubscriptionApiTests in backend/tests/ExpenseFlow.Tests.Integration/SubscriptionApiTests.cs

**Checkpoint**: User Story 2 complete - subscriptions detected, alerts generated for missing charges

---

## Phase 5: User Story 3 - Expense Splitting (Priority: P2)

**Goal**: Allow users to split expenses across multiple GL codes/departments with learned pattern suggestions

**Independent Test**: Create an expense for "Amazon" purchase of $500, split it 60%/40% across two GL codes. Confirm the next "Amazon" expense suggests the same split pattern.

### DTOs for User Story 3

- [ ] T070 [P] [US3] Create SplitAllocationDto in backend/src/ExpenseFlow.Core/DTOs/SplitAllocationDto.cs
- [ ] T071 [P] [US3] Create SplitPatternDto in backend/src/ExpenseFlow.Core/DTOs/SplitPatternDto.cs
- [ ] T072 [P] [US3] Create SplitSuggestionDto in backend/src/ExpenseFlow.Core/DTOs/SplitSuggestionDto.cs
- [ ] T073 [P] [US3] Create ApplySplitRequestDto in backend/src/ExpenseFlow.Core/DTOs/ApplySplitRequestDto.cs

### Service Interface for User Story 3

- [ ] T074 [US3] Create IExpenseSplittingService interface in backend/src/ExpenseFlow.Core/Interfaces/IExpenseSplittingService.cs

### Implementation for User Story 3

- [ ] T075 [US3] Implement ExpenseSplittingService in backend/src/ExpenseFlow.Infrastructure/Services/ExpenseSplittingService.cs
- [ ] T076 [US3] Implement split pattern lookup by vendor alias (Tier 1 suggestion)
- [ ] T077 [US3] Implement split allocation validation (must total exactly 100%)
- [ ] T078 [US3] Implement split line creation from allocations
- [ ] T079 [US3] Implement split pattern save with usage tracking
- [ ] T080 [US3] Register ExpenseSplittingService in DI container

### Controller for User Story 3

- [ ] T081 [US3] Create ExpenseSplittingController in backend/src/ExpenseFlow.Api/Controllers/ExpenseSplittingController.cs
- [ ] T082 [US3] Implement GET /api/expenses/{id}/split endpoint (get suggestion)
- [ ] T083 [US3] Implement POST /api/expenses/{id}/split endpoint (apply split)
- [ ] T084 [US3] Implement DELETE /api/expenses/{id}/split endpoint (remove split)
- [ ] T085 [US3] Implement GET /api/split-patterns endpoint
- [ ] T086 [US3] Implement POST /api/split-patterns endpoint
- [ ] T087 [US3] Implement PUT /api/split-patterns/{id} endpoint
- [ ] T088 [US3] Implement DELETE /api/split-patterns/{id} endpoint

### Tests for User Story 3

- [ ] T089 [P] [US3] Create ExpenseSplittingServiceTests in backend/tests/ExpenseFlow.Tests.Unit/ExpenseSplittingServiceTests.cs
- [ ] T090 [P] [US3] Create SplittingApiTests in backend/tests/ExpenseFlow.Tests.Integration/SplittingApiTests.cs

**Checkpoint**: User Story 3 complete - expenses can be split and patterns learned for future suggestions

---

## Phase 6: User Story 4 - Travel Timeline Visualization (Priority: P3)

**Goal**: Provide a visual timeline of travel periods with linked expenses for user verification

**Independent Test**: After uploading travel receipts that create a travel period, retrieve the timeline data and verify it includes trip dates, destination, source documents, and all linked expenses.

### DTOs for User Story 4

- [ ] T091 [P] [US4] Create TravelTimelineDto in backend/src/ExpenseFlow.Core/DTOs/TravelTimelineDto.cs
- [ ] T092 [P] [US4] Create TravelTimelineExpenseDto in backend/src/ExpenseFlow.Core/DTOs/TravelTimelineExpenseDto.cs

### Implementation for User Story 4

- [ ] T093 [US4] Add GetTimelineAsync method to ITravelDetectionService interface
- [ ] T094 [US4] Implement timeline data aggregation in TravelDetectionService
- [ ] T095 [US4] Implement unlinked expense highlighting within travel periods

### Controller for User Story 4

- [ ] T096 [US4] Add GET /api/travel-periods/timeline endpoint to TravelPeriodsController
- [ ] T097 [US4] Add expense linking indicators to timeline response

### Tests for User Story 4

- [ ] T098 [P] [US4] Add timeline tests to TravelPeriodApiTests

**Checkpoint**: User Story 4 complete - timeline view available for travel expense verification

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T099 [P] Add structured logging for all detection operations
- [ ] T100 [P] Update CLAUDE.md with Sprint 7 technologies
- [ ] T101 Run quickstart.md validation scenarios
- [ ] T102 Verify tier usage logging across all detection operations
- [ ] T103 Performance testing: verify 50+ travel periods, 20+ subscriptions per user (SC-008)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup (Phase 1) completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational (Phase 2) completion
  - US1 (Travel) and US2 (Subscription) and US3 (Splitting) can proceed in parallel
  - US4 (Timeline) depends on US1 completion
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Phase 2 - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Phase 2 - No dependencies on other stories
- **User Story 3 (P2)**: Can start after Phase 2 - No dependencies on other stories
- **User Story 4 (P3)**: Depends on User Story 1 (extends travel period functionality)

### Within Each User Story

- DTOs before services
- Service interfaces before implementations
- Services before controllers
- Controllers before integration hooks
- Implementation before tests

### Parallel Opportunities

- All Phase 1 enums can be created in parallel (T001-T004)
- All Phase 2 entity creations marked [P] can run in parallel (T007-T009)
- All Phase 2 repository interfaces [P] can run in parallel (T016-T018)
- All Phase 2 repository implementations [P] can run in parallel (T019-T021)
- DTOs within each user story marked [P] can run in parallel
- Tests marked [P] can run in parallel
- User Stories 1, 2, 3 can be worked on in parallel after Phase 2

---

## Parallel Example: Phase 2 Foundation

```bash
# Launch all entity creations together:
Task: "Create TravelPeriod entity in backend/src/ExpenseFlow.Core/Entities/TravelPeriod.cs"
Task: "Create DetectedSubscription entity in backend/src/ExpenseFlow.Core/Entities/DetectedSubscription.cs"
Task: "Create KnownSubscriptionVendor entity in backend/src/ExpenseFlow.Core/Entities/KnownSubscriptionVendor.cs"

# Then launch all repository interfaces together:
Task: "Create ITravelPeriodRepository interface"
Task: "Create ISubscriptionRepository interface"
Task: "Create ISplitPatternRepository interface"
```

## Parallel Example: User Story 1 DTOs

```bash
# Launch all US1 DTOs together:
Task: "Create TravelPeriodDto in backend/src/ExpenseFlow.Core/DTOs/TravelPeriodDto.cs"
Task: "Create TravelDetectionResultDto in backend/src/ExpenseFlow.Core/DTOs/TravelDetectionResultDto.cs"
Task: "Create TravelExpenseDto in backend/src/ExpenseFlow.Core/DTOs/TravelExpenseDto.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (enums)
2. Complete Phase 2: Foundational (entities, migrations, repositories)
3. Complete Phase 3: User Story 1 (Travel Period Detection)
4. **STOP and VALIDATE**: Test travel detection independently
5. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy (MVP!)
3. Add User Story 2 → Test independently → Deploy
4. Add User Story 3 → Test independently → Deploy
5. Add User Story 4 → Test independently → Deploy
6. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers after Phase 2:
- Developer A: User Story 1 (Travel Detection)
- Developer B: User Story 2 (Subscription Detection)
- Developer C: User Story 3 (Expense Splitting)
- Then: Developer A continues to User Story 4 (Timeline, depends on US1)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- All AI operations must use Tier 1 (rule-based) first per constitution
- Tier usage logging required for all detection operations (FR-020)
