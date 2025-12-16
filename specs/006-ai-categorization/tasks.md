# Tasks: AI Categorization (Tiered)

**Input**: Design documents from `/specs/006-ai-categorization/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/api-contracts.md ✓

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1-US6)
- Paths follow web app convention: `backend/src/`, `frontend/src/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, NuGet packages, and configuration

- [x] T001 Add Microsoft.SemanticKernel 1.25.0 to backend/src/ExpenseFlow.Infrastructure/ExpenseFlow.Infrastructure.csproj
- [x] T002 Add Microsoft.SemanticKernel.Connectors.AzureOpenAI 1.25.0 to backend/src/ExpenseFlow.Infrastructure/ExpenseFlow.Infrastructure.csproj
- [ ] T003 [P] Add @tanstack/react-query to frontend/package.json (DEFERRED: frontend not created yet)
- [x] T004 [P] Configure Azure OpenAI settings in backend/src/ExpenseFlow.Api/appsettings.Development.json
- [x] T005 [P] Configure Categorization settings (thresholds) in backend/src/ExpenseFlow.Api/appsettings.Development.json

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Database Migrations

- [x] T006 Create TierUsageLog entity in backend/src/ExpenseFlow.Core/Entities/TierUsageLog.cs
- [x] T007 Extend VendorAlias entity with DefaultGLCode, DefaultDepartment, GLConfirmCount, DeptConfirmCount in backend/src/ExpenseFlow.Core/Entities/VendorAlias.cs
- [x] T008 Extend ExpenseEmbedding entity with ExpiresAt column in backend/src/ExpenseFlow.Core/Entities/ExpenseEmbedding.cs
- [x] T009 Add TierUsageLog DbSet to ExpenseFlowDbContext in backend/src/ExpenseFlow.Infrastructure/Data/ExpenseFlowDbContext.cs
- [x] T010 Configure TierUsageLog entity mapping in backend/src/ExpenseFlow.Infrastructure/Data/Configurations/TierUsageLogConfiguration.cs
- [x] T011 Generate EF Core migration: AddCategorizationEntities

### Core Interfaces

- [x] T012 [P] Create IDescriptionNormalizationService interface in backend/src/ExpenseFlow.Core/Interfaces/IDescriptionNormalizationService.cs
- [x] T013 [P] Create ICategorizationService interface in backend/src/ExpenseFlow.Core/Interfaces/ICategorizationService.cs
- [x] T014 [P] Create IEmbeddingService interface in backend/src/ExpenseFlow.Core/Interfaces/IEmbeddingService.cs
- [x] T015 [P] Create ITierUsageService interface in backend/src/ExpenseFlow.Core/Interfaces/ITierUsageService.cs

### Shared DTOs

- [x] T016 [P] Create CategorizationSuggestionDto in backend/src/ExpenseFlow.Shared/DTOs/CategorizationSuggestionDto.cs
- [x] T017 [P] Create TransactionCategorizationDto in backend/src/ExpenseFlow.Shared/DTOs/TransactionCategorizationDto.cs
- [x] T018 [P] Create TierUsageStatsDto in backend/src/ExpenseFlow.Shared/DTOs/TierUsageStatsDto.cs
- [x] T019 [P] Create NormalizationResultDto in backend/src/ExpenseFlow.Shared/DTOs/NormalizationResultDto.cs

### Service Registration

- [x] T020 Register Semantic Kernel text embedding service in backend/src/ExpenseFlow.Infrastructure/Extensions/ServiceCollectionExtensions.cs
- [x] T021 Register Polly resilience pipeline "ai-calls" with retry and circuit breaker in backend/src/ExpenseFlow.Infrastructure/Extensions/ServiceCollectionExtensions.cs
- [x] T022 Register categorization services (scoped lifetime) in backend/src/ExpenseFlow.Infrastructure/Extensions/ServiceCollectionExtensions.cs

### Repositories

- [x] T023 [P] Create IDescriptionCacheRepository and DescriptionCacheRepository in backend/src/ExpenseFlow.Infrastructure/Repositories/
- [x] T024 [P] Create IExpenseEmbeddingRepository and ExpenseEmbeddingRepository with vector similarity search in backend/src/ExpenseFlow.Infrastructure/Repositories/
- [x] T025 [P] Create ITierUsageRepository and TierUsageRepository in backend/src/ExpenseFlow.Infrastructure/Repositories/

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Description Normalization (Priority: P1) 🎯 MVP

**Goal**: Normalize cryptic bank descriptions to human-readable format with cache-first approach

**Independent Test**: Upload a statement with known descriptions and verify normalized outputs appear correctly

### Implementation for User Story 1

- [x] T026 [US1] Implement DescriptionNormalizationService with Tier 1 cache lookup in backend/src/ExpenseFlow.Infrastructure/Services/DescriptionNormalizationService.cs
- [x] T027 [US1] Implement Tier 3 GPT-4o-mini normalization fallback in DescriptionNormalizationService
- [x] T028 [US1] Implement cache storage for successful AI normalizations (hash-based lookup key)
- [x] T029 [US1] Add hit count increment on cache retrieval
- [x] T030 [US1] Implement TierUsageService for logging tier usage in backend/src/ExpenseFlow.Infrastructure/Services/TierUsageService.cs
- [x] T031 [US1] Create DescriptionController with POST /descriptions/normalize endpoint in backend/src/ExpenseFlow.Api/Controllers/DescriptionController.cs
- [x] T032 [US1] Add request validation for empty/invalid descriptions
- [x] T033 [US1] Add error handling for AI service unavailability (503 with retryAfter)

**Checkpoint**: Description normalization fully functional - can verify cache hits vs AI calls

---

## Phase 4: User Story 2 - GL Code Suggestion (Priority: P1) 🎯 MVP

**Goal**: Provide intelligent GL code suggestions using tiered approach (vendor alias → embedding similarity → AI)

**Independent Test**: Create expense lines with known vendors and verify appropriate GL codes are suggested

### Implementation for User Story 2

- [x] T034 [US2] Implement EmbeddingService for vector generation via Azure OpenAI in backend/src/ExpenseFlow.Infrastructure/Services/EmbeddingService.cs
- [x] T035 [US2] Implement pgvector similarity search in EmbeddingService (cosine similarity, 0.92 threshold)
- [x] T036 [US2] Implement CategorizationService Tier 1: vendor alias GL lookup in backend/src/ExpenseFlow.Infrastructure/Services/CategorizationService.cs
- [x] T037 [US2] Implement CategorizationService Tier 2: embedding similarity GL lookup
- [x] T038 [US2] Implement CategorizationService Tier 3: GPT-4o-mini GL inference with GL account context
- [x] T039 [US2] Add tier attribution and confidence scores to all GL suggestions
- [x] T040 [US2] Create CategorizationController in backend/src/ExpenseFlow.Api/Controllers/CategorizationController.cs
- [x] T041 [US2] Implement GET /categorization/transactions/{transactionId}/gl-suggestions endpoint
- [x] T042 [US2] Log tier usage for every GL suggestion operation

**Checkpoint**: GL code suggestions functional with tier tracking

---

## Phase 5: User Story 3 - Department Suggestion (Priority: P2)

**Goal**: Provide intelligent department suggestions using the same tiered infrastructure as GL codes

**Independent Test**: Create expense lines for known vendors with default departments and verify correct suggestions

### Implementation for User Story 3

- [x] T043 [US3] Extend CategorizationService with Tier 1: vendor alias department lookup
- [x] T044 [US3] Extend CategorizationService with Tier 2: embedding similarity department lookup
- [x] T045 [US3] Extend CategorizationService with Tier 3: GPT-4o-mini department inference
- [x] T046 [US3] Implement GET /categorization/transactions/{transactionId}/dept-suggestions endpoint
- [x] T047 [US3] Implement GET /categorization/transactions/{transactionId} combined endpoint (GL + department)
- [x] T048 [US3] Log tier usage for department suggestion operations

**Checkpoint**: Department suggestions functional alongside GL suggestions

---

## Phase 6: User Story 4 - Verified Embeddings (Priority: P2)

**Goal**: Create learning loop where user confirmations improve future suggestions

**Independent Test**: Make categorization selections and verify similar new expenses receive the selected values

### Implementation for User Story 4

- [x] T049 [US4] Implement POST /categorization/transactions/{transactionId}/confirm endpoint
- [x] T050 [US4] Create verified embedding on user confirmation in CategorizationService
- [x] T051 [US4] Track vendor GL confirmations and update vendor alias default when count >= 3
- [x] T052 [US4] Track vendor department confirmations and update vendor alias default when count >= 3
- [x] T053 [US4] Implement POST /categorization/transactions/{transactionId}/skip endpoint for graceful degradation
- [x] T054 [US4] Prioritize verified embeddings over unverified in similarity search results
- [x] T055 [US4] Set ExpiresAt = CreatedAt + 6 months for unverified embeddings
- [x] T056 [US4] Set ExpiresAt = NULL for verified embeddings (never expire)

**Checkpoint**: Learning loop functional - user confirmations improve future suggestions

---

## Phase 7: User Story 5 - Cost Monitoring (Priority: P3)

**Goal**: Enable administrators to monitor tier usage and identify optimization opportunities

**Independent Test**: Process a batch of transactions and review tier usage statistics

### Implementation for User Story 5

- [x] T057 [US5] Implement GET /categorization/stats endpoint with date range filtering
- [x] T058 [US5] Calculate tier usage percentages by operation type
- [x] T059 [US5] Calculate estimated costs based on tier usage
- [x] T060 [US5] Identify vendor candidates with high Tier 3 usage for alias creation
- [x] T061 [US5] Create Hangfire recurring job for monthly stale embedding cleanup in backend/src/ExpenseFlow.Infrastructure/Jobs/EmbeddingCleanupJob.cs
- [x] T062 [US5] Implement PurgeStaleEmbeddingsAsync to delete unverified embeddings older than 6 months

**Checkpoint**: Cost monitoring and automated cleanup operational

---

## Phase 8: User Story 6 - Categorization UI (Priority: P2)

**Goal**: Provide user interface for viewing and confirming categorization suggestions

**Independent Test**: Navigate to categorization interface and verify suggestions display correctly

### Frontend Setup (DEFERRED: frontend not created yet)

- [ ] T063 [P] [US6] Configure React Query provider in frontend/src/main.tsx (DEFERRED)
- [ ] T064 [P] [US6] Create categorizationService API client in frontend/src/services/categorizationService.ts (DEFERRED)

### Components (DEFERRED: frontend not created yet)

- [ ] T065 [US6] Create TierIndicator component with tooltip explanations in frontend/src/components/categorization/TierIndicator.tsx (DEFERRED)
- [ ] T066 [US6] Create SuggestionCard component for GL/department display in frontend/src/components/categorization/SuggestionCard.tsx (DEFERRED)
- [ ] T067 [US6] Create CategorizationPanel component with suggestion display and selection in frontend/src/components/categorization/CategorizationPanel.tsx (DEFERRED)
- [ ] T068 [US6] Implement accept suggestion action with optimistic update (DEFERRED)
- [ ] T069 [US6] Implement reject/modify suggestion action (DEFERRED)
- [ ] T070 [US6] Implement skip suggestion action for manual categorization (DEFERRED)
- [ ] T071 [US6] Create CategorizationPage in frontend/src/pages/CategorizationPage.tsx (DEFERRED)
- [ ] T072 [US6] Add routing for categorization page (DEFERRED)

**Checkpoint**: Full categorization workflow available in UI (DEFERRED: awaiting frontend creation)

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T073 [P] Add structured logging for all categorization operations
- [x] T074 [P] Add request/response logging middleware for categorization endpoints
- [ ] T075 Implement rate limit handling in frontend (retry with backoff) (DEFERRED: frontend not created)
- [ ] T076 Add loading states and error handling in frontend components (DEFERRED: frontend not created)
- [ ] T077 Run quickstart.md validation steps
- [x] T078 Update CLAUDE.md with Sprint 6 technologies

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-8)**: All depend on Foundational phase completion
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### User Story Dependencies

| Story | Priority | Can Start After | Notes |
|-------|----------|-----------------|-------|
| US1 - Description Normalization | P1 | Phase 2 | No other story dependencies |
| US2 - GL Code Suggestion | P1 | Phase 2 | Uses US1 normalized descriptions but independently testable |
| US3 - Department Suggestion | P2 | Phase 2 | Shares infrastructure with US2 |
| US4 - Verified Embeddings | P2 | US2, US3 | Requires suggestion endpoints to confirm |
| US5 - Cost Monitoring | P3 | US1, US2 | Requires tier usage data to report on |
| US6 - Categorization UI | P2 | US2, US3 | Frontend for backend endpoints |

### Recommended Execution Order

**MVP Path (Minimum Viable Product)**:
1. Phase 1: Setup
2. Phase 2: Foundational
3. Phase 3: US1 - Description Normalization ← **First Demo**
4. Phase 4: US2 - GL Code Suggestion ← **Second Demo**

**Full Feature Path**:
5. Phase 5: US3 - Department Suggestion
6. Phase 6: US4 - Verified Embeddings
7. Phase 8: US6 - Categorization UI
8. Phase 7: US5 - Cost Monitoring
9. Phase 9: Polish

### Parallel Opportunities

```bash
# Phase 1 - All can run in parallel:
T003, T004, T005

# Phase 2 - Entities can run in parallel:
T012, T013, T014, T015  # Interfaces
T016, T017, T018, T019  # DTOs
T023, T024, T025        # Repositories

# Phase 8 - Frontend setup can run in parallel:
T063, T064
```

---

## Notes

- [P] tasks = different files, no dependencies
- [USx] label maps task to specific user story for traceability
- Tier 1 = cache/vendor alias (free), Tier 2 = embedding similarity ($0.00002/call), Tier 3 = AI inference ($0.0003-0.0005/call)
- Target: 70%+ Tier 1/2 suggestions after 3 months, <$40/month AI costs
- Similarity threshold: 0.92 (configurable)
- Vendor alias auto-update: 3+ consistent confirmations required
- Unverified embeddings: auto-purge after 6 months
