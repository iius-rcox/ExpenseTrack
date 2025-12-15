# Tasks: Matching Engine

**Input**: Design documents from `/specs/005-matching-engine/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Tests are OPTIONAL - include unit tests for core matching algorithm per plan.md testing section.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Web app**: `backend/src/`, `backend/tests/`
- Solution: `ExpenseFlow.sln` with projects: Api, Core, Infrastructure, Shared

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add NuGet package and shared types needed by all user stories

- [X] T001 Add F23.StringSimilarity NuGet package to backend/src/ExpenseFlow.Infrastructure/ExpenseFlow.Infrastructure.csproj
- [X] T002 [P] Create MatchStatus enum in backend/src/ExpenseFlow.Shared/Enums/MatchStatus.cs with values Unmatched=0, Proposed=1, Matched=2
- [X] T003 [P] Create IFuzzyMatchingService interface in backend/src/ExpenseFlow.Core/Interfaces/IFuzzyMatchingService.cs
- [X] T004 [P] Create IMatchingService interface in backend/src/ExpenseFlow.Core/Interfaces/IMatchingService.cs
- [X] T005 [P] Create IMatchRepository interface in backend/src/ExpenseFlow.Core/Interfaces/IMatchRepository.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Entity, migration, and repository that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T006 Create ReceiptTransactionMatch entity in backend/src/ExpenseFlow.Core/Entities/ReceiptTransactionMatch.cs with all fields per data-model.md (Id, ReceiptId, TransactionId, UserId, Status, ConfidenceScore, AmountScore, DateScore, VendorScore, MatchReason, MatchedVendorAliasId, IsManualMatch, CreatedAt, ConfirmedAt, ConfirmedByUserId, RowVersion)
- [X] T007 Add MatchedTransactionId (uuid, nullable) and MatchStatus (smallint, default 0) columns to Receipt entity in backend/src/ExpenseFlow.Core/Entities/Receipt.cs
- [X] T008 Add MatchStatus (smallint, default 0) column to Transaction entity in backend/src/ExpenseFlow.Core/Entities/Transaction.cs
- [X] T009 Create ReceiptTransactionMatchConfiguration in backend/src/ExpenseFlow.Infrastructure/Data/Configurations/ReceiptTransactionMatchConfiguration.cs with indexes, FK relationships, optimistic locking via xmin, and partial unique indexes for one-to-one constraint
- [X] T010 Update ReceiptConfiguration in backend/src/ExpenseFlow.Infrastructure/Data/Configurations/ReceiptConfiguration.cs to add MatchedTransactionId FK and MatchStatus column
- [X] T011 Update TransactionConfiguration in backend/src/ExpenseFlow.Infrastructure/Data/Configurations/TransactionConfiguration.cs to add MatchStatus column and index
- [X] T012 Add DbSet<ReceiptTransactionMatch> to ExpenseFlowDbContext
- [X] T013 Create EF Core migration AddReceiptTransactionMatch in backend/src/ExpenseFlow.Infrastructure/Data/Migrations/ with all tables, columns, indexes, and constraints per data-model.md
- [X] T014 Implement FuzzyMatchingService in backend/src/ExpenseFlow.Infrastructure/Services/FuzzyMatchingService.cs using F23.StringSimilarity NormalizedLevenshtein, with method CalculateSimilarity(string a, string b) returning double 0.0-1.0
- [X] T015 Implement MatchRepository in backend/src/ExpenseFlow.Infrastructure/Repositories/MatchRepository.cs with CRUD operations, GetByReceiptId, GetByTransactionId, GetProposedByUserId, GetUnmatchedReceipts, GetUnmatchedTransactions

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Auto-Match Receipts to Transactions (Priority: P1) 🎯 MVP

**Goal**: Run auto-match for all unmatched receipts, calculate confidence scores, create proposed matches with confidence >= 70%

**Independent Test**: Upload 10 receipts with extracted vendor/date/amount data, import a statement with matching transactions, run auto-match via POST /api/matching/auto, verify that high-confidence matches are proposed

### DTOs for User Story 1

- [X] T016 [P] [US1] Create AutoMatchRequestDto in backend/src/ExpenseFlow.Shared/DTOs/AutoMatchRequestDto.cs with optional List<Guid> ReceiptIds
- [X] T017 [P] [US1] Create AutoMatchResponseDto in backend/src/ExpenseFlow.Shared/DTOs/AutoMatchResponseDto.cs with ProposedCount, ProcessedCount, AmbiguousCount, DurationMs, List<MatchProposalDto> Proposals
- [X] T018 [P] [US1] Create MatchProposalDto in backend/src/ExpenseFlow.Shared/DTOs/MatchProposalDto.cs with all fields per contracts/matching-api.yaml MatchProposal schema
- [X] T019 [P] [US1] Create ReceiptSummaryDto in backend/src/ExpenseFlow.Shared/DTOs/ReceiptSummaryDto.cs with Id, VendorExtracted, DateExtracted, AmountExtracted, Currency, ThumbnailUrl, OriginalFilename
- [X] T020 [P] [US1] Create TransactionSummaryDto in backend/src/ExpenseFlow.Shared/DTOs/TransactionSummaryDto.cs with Id, Description, OriginalDescription, TransactionDate, PostDate, Amount

### Core Matching Service for User Story 1

- [X] T021 [US1] Implement MatchingService.CalculateConfidenceScore in backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs with amount scoring (40pts: exact ±$0.10=40, near ±$1.00=20, else=0), date scoring (35pts: same day=35, ±1=30, ±2-3=25, ±4-7=10, else=0), vendor scoring (25pts: alias match=25, fuzzy >70%=15, else=0)
- [X] T022 [US1] Implement MatchingService.FindBestMatch in backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs that iterates unmatched transactions for a receipt, calculates scores, returns best match or flags ambiguous if multiple within 5%
- [X] T023 [US1] Implement MatchingService.RunAutoMatchAsync in backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs that processes all unmatched receipts (or specific IDs), creates ReceiptTransactionMatch records with Status=Proposed for confidence >= 70%, updates Receipt.MatchStatus to Proposed
- [X] T024 [US1] Implement vendor pattern extraction helper ExtractVendorPattern(string description) in MatchingService that removes trailing reference numbers, keeps first 3 words, handles AMAZON.COM*, SQ *, PAYPAL * patterns

### Controller Endpoint for User Story 1

- [X] T025 [US1] Create MatchingController in backend/src/ExpenseFlow.Api/Controllers/MatchingController.cs with [Authorize] attribute, inject IMatchingService
- [X] T026 [US1] Implement POST /api/matching/auto endpoint in MatchingController that calls RunAutoMatchAsync, returns AutoMatchResponseDto with timing

### Service Registration for User Story 1

- [X] T027 [US1] Register IMatchingService, IMatchRepository, IFuzzyMatchingService in Program.cs DI container

**Checkpoint**: At this point, User Story 1 should be fully functional - POST /api/matching/auto creates proposed matches

---

## Phase 4: User Story 2 - Review and Confirm Proposed Matches (Priority: P1)

**Goal**: User reviews proposed matches, confirms or rejects, vendor alias created/updated on confirm

**Independent Test**: GET /api/matching/proposals returns list, POST /api/matching/{id}/confirm links receipt/transaction and creates vendor alias, POST /api/matching/{id}/reject marks as rejected

### DTOs for User Story 2

- [X] T028 [P] [US2] Create ProposalListResponseDto in backend/src/ExpenseFlow.Shared/DTOs/ProposalListResponseDto.cs with Items, TotalCount, Page, PageSize
- [X] T029 [P] [US2] Create MatchDetailResponseDto in backend/src/ExpenseFlow.Shared/DTOs/MatchDetailResponseDto.cs extending MatchProposalDto with ConfirmedAt, IsManualMatch, VendorAliasSummary
- [X] T030 [P] [US2] Create ConfirmMatchRequestDto in backend/src/ExpenseFlow.Shared/DTOs/ConfirmMatchRequestDto.cs with optional VendorDisplayName, DefaultGLCode, DefaultDepartment
- [X] T031 [P] [US2] Create VendorAliasSummaryDto in backend/src/ExpenseFlow.Shared/DTOs/VendorAliasSummaryDto.cs with Id, CanonicalName, DisplayName, AliasPattern, DefaultGLCode, DefaultDepartment, MatchCount

### Service Methods for User Story 2

- [X] T032 [US2] Implement MatchingService.GetProposalsAsync in backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs with pagination, ordered by confidence descending
- [X] T033 [US2] Implement MatchingService.ConfirmMatchAsync in backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs that: validates match exists and is Proposed, sets Status=Confirmed, sets ConfirmedAt/ConfirmedByUserId, links Receipt.MatchedTransactionId and Transaction.MatchedReceiptId, sets both MatchStatus=Matched, creates/updates VendorAlias with pattern extraction, increments MatchCount/LastMatchedAt, handles DbUpdateConcurrencyException for optimistic locking
- [X] T034 [US2] Implement MatchingService.RejectMatchAsync in backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs that: validates match is Proposed, sets Status=Rejected, sets ConfirmedAt/ConfirmedByUserId, resets Receipt.MatchStatus to Unmatched

### Controller Endpoints for User Story 2

- [X] T035 [US2] Implement GET /api/matching/proposals endpoint in MatchingController with pagination parameters, returns ProposalListResponseDto
- [X] T036 [US2] Implement POST /api/matching/{matchId}/confirm endpoint in MatchingController with ConfirmMatchRequestDto body, returns MatchDetailResponseDto, returns 409 Conflict on concurrency error
- [X] T037 [US2] Implement POST /api/matching/{matchId}/reject endpoint in MatchingController, returns MatchDetailResponseDto, returns 409 Conflict on concurrency error
- [X] T038 [US2] Implement GET /api/matching/{matchId} endpoint in MatchingController to get single match details

**Checkpoint**: At this point, User Stories 1 AND 2 work - auto-match creates proposals, user can confirm/reject

---

## Phase 5: User Story 3 - Vendor Alias Learning (Priority: P2)

**Goal**: System learns vendor patterns from confirmations, fuzzy matching finds similar patterns

**Independent Test**: Confirm a match for "DELTA AIR 123456", verify alias created with pattern "DELTA AIR" → "Delta Airlines", run auto-match on new receipt with "DELTA AIRLINES INC", verify fuzzy match finds vendor

### Service Methods for User Story 3

- [X] T039 [US3] Implement VendorAliasService.FindByPattern in backend/src/ExpenseFlow.Infrastructure/Services/VendorAliasService.cs to search VendorAliases using LIKE/ILIKE pattern matching on AliasPattern column
- [X] T040 [US3] Implement VendorAliasService.FindByFuzzyMatch in backend/src/ExpenseFlow.Infrastructure/Services/VendorAliasService.cs that loads all aliases, uses IFuzzyMatchingService to find best match with similarity > 70%
- [X] T041 [US3] Implement VendorAliasService.CreateOrUpdateFromMatch in backend/src/ExpenseFlow.Infrastructure/Services/VendorAliasService.cs that extracts pattern from transaction description, creates new alias if none exists, updates MatchCount/LastMatchedAt/DefaultGLCode/DefaultDepartment if exists
- [X] T042 [US3] Update MatchingService.ConfirmMatchAsync to call VendorAliasService.CreateOrUpdateFromMatch passing the transaction description and optional overrides from ConfirmMatchRequestDto

**Checkpoint**: Vendor alias learning is functional - confirmations create/update aliases, fuzzy matching works

---

## Phase 6: User Story 4 - View Unmatched Items (Priority: P2)

**Goal**: User can view unmatched receipts and transactions with summary stats

**Independent Test**: After auto-match, GET /api/receipts/unmatched returns receipts with MatchStatus=Unmatched, GET /api/transactions/unmatched returns unmatched transactions, GET /api/matching/stats returns counts

### DTOs for User Story 4

- [X] T043 [P] [US4] Create UnmatchedReceiptsResponseDto in backend/src/ExpenseFlow.Shared/DTOs/UnmatchedReceiptsResponseDto.cs with Items (List<ReceiptSummaryDto>), TotalCount, Page, PageSize
- [X] T044 [P] [US4] Create UnmatchedTransactionsResponseDto in backend/src/ExpenseFlow.Shared/DTOs/UnmatchedTransactionsResponseDto.cs with Items (List<TransactionSummaryDto>), TotalCount, Page, PageSize
- [X] T045 [P] [US4] Create MatchingStatsResponseDto in backend/src/ExpenseFlow.Shared/DTOs/MatchingStatsResponseDto.cs with MatchedCount, ProposedCount, UnmatchedReceiptsCount, UnmatchedTransactionsCount, AutoMatchRate, AverageConfidence

### Service Methods for User Story 4

- [X] T046 [US4] Implement MatchingService.GetUnmatchedReceiptsAsync in backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs with pagination, returns receipts where MatchStatus=Unmatched and has extracted data
- [X] T047 [US4] Implement MatchingService.GetUnmatchedTransactionsAsync in backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs with pagination, returns transactions where MatchStatus=Unmatched
- [X] T048 [US4] Implement MatchingService.GetStatsAsync in backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs that counts confirmed matches, proposed matches, unmatched receipts, unmatched transactions, calculates auto-match rate and average confidence

### Controller Endpoints for User Story 4

- [X] T049 [US4] Implement GET /api/receipts/unmatched endpoint in ReceiptsController (or MatchingController) with pagination, returns UnmatchedReceiptsResponseDto
- [X] T050 [US4] Implement GET /api/transactions/unmatched endpoint in TransactionsController (or MatchingController) with pagination, returns UnmatchedTransactionsResponseDto
- [X] T051 [US4] Implement GET /api/matching/stats endpoint in MatchingController, returns MatchingStatsResponseDto

**Checkpoint**: All visibility features work - users can see unmatched items and stats

---

## Phase 7: User Story 5 - Alias Confidence Decay (Priority: P3)

**Goal**: Background job reduces confidence of aliases unused for 6+ months

**Independent Test**: Create alias with LastMatchedAt 7 months ago, run decay job via Hangfire dashboard or direct invocation, verify Confidence reduced by 10%

### Background Job for User Story 5

- [X] T052 [US5] Create AliasConfidenceDecayJob in backend/src/ExpenseFlow.Infrastructure/Jobs/AliasConfidenceDecayJob.cs extending JobBase, inject ExpenseFlowDbContext
- [X] T053 [US5] Implement AliasConfidenceDecayJob.ExecuteAsync that: queries VendorAliases where LastMatchedAt < 6 months ago AND Confidence > 0.5, reduces Confidence by 10% (multiply by 0.9), saves changes, logs count of decayed aliases
- [X] T054 [US5] Register AliasConfidenceDecayJob as recurring Hangfire job in Program.cs with Cron.Weekly(DayOfWeek.Sunday, 2, 0) schedule

**Checkpoint**: Maintenance job is functional - stale aliases have confidence decayed

---

## Phase 8: User Story 6 - Manual Matching (Priority: P2)

**Goal**: User can manually match unmatched receipt to unmatched transaction

**Independent Test**: POST /api/matching/manual with receiptId and transactionId, verify match created as Confirmed, receipt and transaction linked, vendor alias created

### DTOs for User Story 6

- [X] T055 [P] [US6] Create ManualMatchRequestDto in backend/src/ExpenseFlow.Shared/DTOs/ManualMatchRequestDto.cs with required ReceiptId, TransactionId, optional VendorDisplayName, DefaultGLCode, DefaultDepartment

### Service Method for User Story 6

- [X] T056 [US6] Implement MatchingService.CreateManualMatchAsync in backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs that: validates receipt and transaction exist and are unmatched, creates ReceiptTransactionMatch with Status=Confirmed and IsManualMatch=true, links receipt/transaction, sets MatchStatus=Matched on both, creates vendor alias from transaction description

### Controller Endpoint for User Story 6

- [X] T057 [US6] Implement POST /api/matching/manual endpoint in MatchingController with ManualMatchRequestDto body, returns MatchDetailResponseDto with 201 Created, returns 400 BadRequest if already matched, 404 NotFound if receipt/transaction not found

**Checkpoint**: Manual matching is functional - users can link items that auto-match missed

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Testing, error handling, and optimization across all stories

- [X] T058 [P] Create FuzzyMatchingServiceTests in backend/tests/ExpenseFlow.Infrastructure.Tests/Services/FuzzyMatchingServiceTests.cs with tests for similarity calculations (exact match=1.0, similar strings >0.7, different strings <0.5)
- [X] T059 [P] Create MatchingServiceTests in backend/tests/ExpenseFlow.Infrastructure.Tests/Services/MatchingServiceTests.cs with tests for confidence scoring (amount, date, vendor components), threshold enforcement, ambiguous detection
- [X] T060 Add structured logging to MatchingService for auto-match runs (receipts processed, matches proposed, duration), confirmations, rejections
- [X] T061 Add input validation to all controller endpoints using FluentValidation or DataAnnotations
- [X] T062 Verify optimistic locking works by testing concurrent confirm/reject scenario
- [X] T063 Run quickstart.md validation - test all endpoints via curl commands
- [X] T064 Update Swagger documentation with examples for all matching endpoints

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-8)**: All depend on Foundational phase completion
  - User Story 1 (P1): Foundation only - no other story dependencies
  - User Story 2 (P1): Depends on US1 for proposals to exist
  - User Story 3 (P2): Can start after Foundation, integrates with US2 confirm flow
  - User Story 4 (P2): Can start after Foundation, independent of US1-3
  - User Story 5 (P3): Can start after Foundation, independent of US1-4
  - User Story 6 (P2): Can start after Foundation, independent but similar to US2
- **Polish (Phase 9)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (Auto-Match)**: Foundation only → Creates proposals
- **US2 (Confirm/Reject)**: US1 creates proposals → US2 processes them
- **US3 (Vendor Learning)**: Integrates into US2 confirmation flow
- **US4 (Unmatched View)**: Foundation only → Independent read operations
- **US5 (Decay Job)**: Foundation only → Independent background job
- **US6 (Manual Match)**: Foundation only → Alternative to auto-match

### Parallel Opportunities

Within Setup (Phase 1):
- T002, T003, T004, T005 can run in parallel (different files)

Within Foundational (Phase 2):
- T007, T008 can run in parallel (different entities)
- T009, T010, T011 can run in parallel (different configurations)

Within User Story 1 (Phase 3):
- T016, T017, T018, T019, T020 can run in parallel (different DTOs)

Within User Story 2 (Phase 4):
- T028, T029, T030, T031 can run in parallel (different DTOs)

Within User Story 4 (Phase 6):
- T043, T044, T045 can run in parallel (different DTOs)

Within Polish (Phase 9):
- T058, T059 can run in parallel (different test files)

---

## Parallel Example: User Story 1 DTOs

```bash
# Launch all DTOs for User Story 1 together:
Task: "Create AutoMatchRequestDto in backend/src/ExpenseFlow.Shared/DTOs/AutoMatchRequestDto.cs"
Task: "Create AutoMatchResponseDto in backend/src/ExpenseFlow.Shared/DTOs/AutoMatchResponseDto.cs"
Task: "Create MatchProposalDto in backend/src/ExpenseFlow.Shared/DTOs/MatchProposalDto.cs"
Task: "Create ReceiptSummaryDto in backend/src/ExpenseFlow.Shared/DTOs/ReceiptSummaryDto.cs"
Task: "Create TransactionSummaryDto in backend/src/ExpenseFlow.Shared/DTOs/TransactionSummaryDto.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 + 2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Auto-Match)
4. Complete Phase 4: User Story 2 (Confirm/Reject)
5. **STOP and VALIDATE**: Test auto-match → propose → confirm flow end-to-end
6. Deploy/demo if ready - this is the core matching functionality!

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add US1 + US2 → Auto-match and confirm working → Deploy (MVP!)
3. Add US3 → Vendor learning active → Deploy
4. Add US4 → Dashboard visibility → Deploy
5. Add US5 → Maintenance job active → Deploy
6. Add US6 → Manual fallback available → Deploy

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 + 2 (core flow)
   - Developer B: User Story 4 + 6 (supporting features)
   - Developer C: User Story 3 + 5 (learning + maintenance)
3. Integrate and test together

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable (except US2 needs US1 proposals)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Optimistic locking (xmin) is critical for FR-017 concurrent conflict handling
- All matching is Tier 1 only - no AI API calls per constitution principle I
