# Tasks: Statement Import & Fingerprinting

**Input**: Design documents from `/specs/004-statement-fingerprinting/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/statements-api.yaml

**Tests**: Tests NOT explicitly requested in spec - test tasks excluded.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1-US5)
- All paths relative to repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add required packages and create shared DTOs

- [X] T001 Add CsvHelper package to backend/src/ExpenseFlow.Infrastructure/ExpenseFlow.Infrastructure.csproj
- [X] T002 Add ClosedXML package to backend/src/ExpenseFlow.Infrastructure/ExpenseFlow.Infrastructure.csproj
- [X] T003 [P] Create ColumnMappingDto in backend/src/ExpenseFlow.Shared/DTOs/ColumnMappingDto.cs
- [X] T004 [P] Create StatementAnalyzeResponse in backend/src/ExpenseFlow.Shared/DTOs/StatementAnalyzeResponse.cs
- [X] T005 [P] Create StatementImportRequest in backend/src/ExpenseFlow.Shared/DTOs/StatementImportRequest.cs
- [X] T006 [P] Create StatementImportResponse in backend/src/ExpenseFlow.Shared/DTOs/StatementImportResponse.cs
- [X] T007 [P] Create MappingOptionDto in backend/src/ExpenseFlow.Shared/DTOs/MappingOptionDto.cs
- [X] T008 [P] Create TransactionDto and TransactionListResponse in backend/src/ExpenseFlow.Shared/DTOs/TransactionDto.cs
- [X] T009 [P] Create FingerprintSummaryDto in backend/src/ExpenseFlow.Shared/DTOs/FingerprintSummaryDto.cs
- [X] T010 [P] Create ImportSummaryDto in backend/src/ExpenseFlow.Shared/DTOs/ImportSummaryDto.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core entities, migrations, and services that ALL user stories depend on

**CRITICAL**: No user story work can begin until this phase is complete

- [X] T011 Extend StatementFingerprint entity with HitCount, LastUsedAt, nullable UserId in backend/src/ExpenseFlow.Core/Entities/StatementFingerprint.cs
- [X] T012 [P] Create Transaction entity in backend/src/ExpenseFlow.Core/Entities/Transaction.cs
- [X] T013 [P] Create StatementImport entity in backend/src/ExpenseFlow.Core/Entities/StatementImport.cs
- [X] T014 Update StatementFingerprintConfiguration for nullable UserId and new fields in backend/src/ExpenseFlow.Infrastructure/Data/Configurations/StatementFingerprintConfiguration.cs
- [X] T015 [P] Create TransactionConfiguration in backend/src/ExpenseFlow.Infrastructure/Data/Configurations/TransactionConfiguration.cs
- [X] T016 [P] Create StatementImportConfiguration in backend/src/ExpenseFlow.Infrastructure/Data/Configurations/StatementImportConfiguration.cs
- [X] T017 Add DbSets for Transaction and StatementImport in backend/src/ExpenseFlow.Infrastructure/Data/ExpenseFlowDbContext.cs
- [X] T018 Create EF migration for new entities: dotnet ef migrations add AddStatementImportTables
- [X] T019 Apply migration to database: dotnet ef database update
- [X] T020 [P] Create IStatementParsingService interface in backend/src/ExpenseFlow.Core/Interfaces/IStatementParsingService.cs
- [X] T021 [P] Create ITransactionRepository interface in backend/src/ExpenseFlow.Core/Interfaces/ITransactionRepository.cs
- [X] T022 [P] Create IStatementImportRepository interface in backend/src/ExpenseFlow.Core/Interfaces/IStatementImportRepository.cs
- [X] T023 Create TransactionRepository in backend/src/ExpenseFlow.Infrastructure/Repositories/TransactionRepository.cs
- [X] T024 Create StatementImportRepository in backend/src/ExpenseFlow.Infrastructure/Repositories/StatementImportRepository.cs
- [X] T025 Seed Chase Business Card system fingerprint (UserId=NULL) via migration or seed script
- [X] T026 Seed American Express system fingerprint (UserId=NULL) via migration or seed script
- [X] T027 Register new services in DI container in backend/src/ExpenseFlow.Infrastructure/Extensions/ServiceCollectionExtensions.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Import Known Statement Format (Priority: P1) MVP

**Goal**: Users can upload Chase/Amex CSV and import transactions automatically via cached fingerprint (Tier 1)

**Independent Test**: Upload chase_sample.csv, verify fingerprint detection, confirm import, check transactions created

### Implementation for User Story 1

- [X] T028 [US1] Implement StatementParsingService with CSV parsing (CsvHelper) in backend/src/ExpenseFlow.Infrastructure/Services/StatementParsingService.cs
- [X] T029 [US1] Add Excel parsing to StatementParsingService using ClosedXML in backend/src/ExpenseFlow.Infrastructure/Services/StatementParsingService.cs
- [X] T030 [US1] Implement header hash computation (SHA-256 of normalized sorted headers) in StatementParsingService
- [X] T031 [US1] Implement encoding detection (UTF-8 with BOM detection, Latin-1 fallback) in StatementParsingService
- [X] T031a [US1] Add unit test for encoding fallback: verify Latin-1 file parses correctly when UTF-8 fails
- [X] T032 [US1] Extend IStatementFingerprintService with GetByHashAsync for fingerprint lookup in backend/src/ExpenseFlow.Core/Interfaces/IStatementFingerprintService.cs
- [X] T033 [US1] Implement GetByHashAsync (check user fingerprints + system fingerprints) in backend/src/ExpenseFlow.Infrastructure/Services/StatementFingerprintService.cs
- [X] T034 [US1] Create StatementsController with POST /api/statements/analyze endpoint in backend/src/ExpenseFlow.Api/Controllers/StatementsController.cs
- [X] T035 [US1] Implement analysis session cache (in-memory, 30-minute expiry) for file data between analyze and import
- [X] T036 [US1] Create POST /api/statements/import endpoint in StatementsController
- [X] T037 [US1] Implement transaction parsing with column mapping application in import endpoint
- [X] T038 [US1] Implement duplicate detection (DuplicateHash check before insert) in import logic
- [X] T039 [US1] Implement row validation (skip rows missing date/amount/description) in import logic
- [X] T039a [US1] Implement date parsing with format fallback: try detected format, then common formats (ISO, US, EU), then AI inference for ambiguous dates
- [X] T040 [US1] Create StatementImport audit record on successful import
- [X] T041 [US1] Increment HitCount and update LastUsedAt on fingerprint use

**Checkpoint**: US1 complete - Chase CSV uploads auto-detect and import via Tier 1

---

## Phase 4: User Story 2 - Import Unknown Statement Format (Priority: P2)

**Goal**: Users can upload unknown CSV formats, AI infers column mapping, confirmed mapping saved as fingerprint

**Independent Test**: Upload unknown_bank.csv (non-standard headers), verify AI inference returns mapping, confirm and save fingerprint

### Implementation for User Story 2

- [X] T042 [P] [US2] Create IColumnMappingInferenceService interface in backend/src/ExpenseFlow.Core/Interfaces/IColumnMappingInferenceService.cs
- [X] T043 [US2] Implement ColumnMappingInferenceService using Semantic Kernel + GPT-4o-mini in backend/src/ExpenseFlow.Infrastructure/Services/ColumnMappingInferenceService.cs
- [X] T044 [US2] Create AI prompt template for column mapping inference per research.md specification
- [X] T045 [US2] Add confidence score calculation to AI inference response
- [X] T046 [US2] Extend analyze endpoint to call AI inference when no fingerprint match found
- [X] T047 [US2] Handle AI service unavailable (503 error) when inference fails
- [X] T048 [US2] Implement fingerprint save on import confirm when saveAsFingerprint=true
- [X] T049 [US2] Support custom fingerprintName in save logic
- [X] T050 [US2] Register ColumnMappingInferenceService in DI container

**Checkpoint**: US2 complete - Unknown formats handled via AI (Tier 3), fingerprints saved for future

---

## Phase 5: User Story 3 - Handle American Express Format (Priority: P2)

**Goal**: Support Amex positive_charges convention where positive amounts = expenses

**Independent Test**: Upload amex_sample.csv, verify positive amounts recorded as expenses, credits as negative

### Implementation for User Story 3

- [X] T051 [US3] Implement amount sign convention handling in StatementParsingService
- [X] T052 [US3] Apply amountSign from fingerprint during transaction amount parsing
- [X] T053 [US3] Handle credits/refunds (negative with negative_charges, positive with positive_charges)
- [X] T054 [US3] Verify Amex system fingerprint seed includes positive_charges setting

**Checkpoint**: US3 complete - Both Chase (negative=expense) and Amex (positive=expense) work correctly

---

## Phase 6: User Story 4 - Column Mapping Correction (Priority: P3)

**Goal**: Users can correct misidentified column mappings in the UI before import

**Independent Test**: Upload file, modify detected mapping, confirm import uses corrected mapping

### Implementation for User Story 4

- [ ] T055 [P] [US4] Create statementService.ts API client in frontend/src/services/statementService.ts
- [ ] T056 [P] [US4] Create StatementUpload component in frontend/src/components/statements/StatementUpload.tsx
- [ ] T057 [US4] Create ColumnMappingEditor component in frontend/src/components/statements/ColumnMappingEditor.tsx
- [ ] T058 [US4] Implement column dropdown selectors (date, post_date, description, amount, category, memo, reference, ignore)
- [ ] T059 [US4] Display sample rows preview in mapping editor for context
- [ ] T060 [US4] Implement mapping option selection when multiple options available (user vs system fingerprint)
- [ ] T061 [US4] Create ImportSummary component in frontend/src/components/statements/ImportSummary.tsx
- [ ] T062 [US4] Create StatementImportPage wizard in frontend/src/pages/StatementImportPage.tsx
- [ ] T063 [US4] Wire frontend wizard to analyze → edit mapping → import flow

**Checkpoint**: US4 complete - Users can correct and confirm mappings via UI

---

## Phase 7: User Story 5 - Track Import Source Statistics (Priority: P4)

**Goal**: Track Tier 1 vs Tier 3 usage for cost monitoring, expose import history and fingerprint stats

**Independent Test**: Import files via both tiers, verify StatementImport records show correct TierUsed values

### Implementation for User Story 5

- [X] T064 [US5] Log tier usage on each import (TierUsed field in StatementImport)
- [X] T065 [US5] Create GET /api/statements/imports endpoint for import history
- [X] T066 [US5] Create GET /api/statements/fingerprints endpoint for fingerprint list
- [X] T067 [US5] Include isSystem flag in fingerprint list response (UserId == null)
- [X] T068 [US5] Include hitCount and lastUsedAt in fingerprint stats

**Checkpoint**: US5 complete - Tier usage tracked and queryable for cost analysis

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Additional endpoints, edge cases, and validation

- [X] T069 [P] Create TransactionsController in backend/src/ExpenseFlow.Api/Controllers/TransactionsController.cs
- [X] T070 [P] Implement GET /api/transactions with pagination and filters in TransactionsController
- [X] T071 [P] Implement GET /api/transactions/{id} in TransactionsController
- [X] T072 [P] Implement DELETE /api/transactions/{id} in TransactionsController
- [X] T073 Handle duplicate column names (append numeric suffix) in StatementParsingService
- [X] T074 Handle no header row error (return 400 with clear message)
- [X] T075 Implement batch processing for large files (>500 rows) with progress tracking
- [X] T076 Add date validation (must be within last 2 years) in transaction import
- [X] T077 Add amount validation (must not be zero) in transaction import
- [X] T078 Run quickstart.md validation scenarios end-to-end (code complete, test files created, requires API deployment for runtime testing)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phases 3-7)**: All depend on Foundational phase completion
  - US1 (P1): Primary MVP - complete first
  - US2 (P2): Adds AI inference - builds on US1
  - US3 (P2): Adds Amex support - independent of US2
  - US4 (P3): Frontend - can parallel with US2/US3 after US1
  - US5 (P4): Statistics - can parallel after US1
- **Polish (Phase 8)**: Depends on core user stories being complete

### User Story Dependencies

- **US1 (P1)**: After Foundational - No dependencies on other stories
- **US2 (P2)**: After US1 (extends analyze endpoint) - Independently testable
- **US3 (P2)**: After Foundational - Independent of US2, can parallel
- **US4 (P3)**: After US1 (needs API to call) - Frontend work independent
- **US5 (P4)**: After US1 (needs imports to track) - Independently testable

### Parallel Opportunities

**Within Setup (Phase 1)**:
```
Parallel: T003, T004, T005, T006, T007, T008, T009, T010 (all DTOs)
```

**Within Foundational (Phase 2)**:
```
Parallel: T012, T013 (entities)
Parallel: T015, T016 (configurations)
Parallel: T020, T021, T022 (interfaces)
```

**Across User Stories (after Foundational)**:
```
US3 can parallel with US2
US4 frontend can parallel with US2/US3 backend
US5 can parallel with US3/US4
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (10 tasks)
2. Complete Phase 2: Foundational (17 tasks)
3. Complete Phase 3: User Story 1 (14 tasks)
4. **STOP and VALIDATE**:
   - Upload chase_sample.csv
   - Verify auto-detection (Tier 1)
   - Confirm import
   - Check transactions in database
5. Deploy/demo if ready

### Incremental Delivery

1. **MVP**: Setup + Foundational + US1 → Known formats import working
2. **AI Support**: Add US2 → Unknown formats handled
3. **Amex Support**: Add US3 → Major card providers covered
4. **Full UI**: Add US4 → User-friendly mapping correction
5. **Monitoring**: Add US5 → Cost tracking enabled
6. **Polish**: Add Phase 8 → Transaction management, edge cases

---

## Task Summary

| Phase | Tasks | Purpose |
|-------|-------|---------|
| Setup | T001-T010 (10) | Packages and DTOs |
| Foundational | T011-T027 (17) | Entities, migrations, core services |
| US1 (P1) | T028-T041 + T031a, T039a (16) | Known format import - MVP |
| US2 (P2) | T042-T050 (9) | AI inference for unknown formats |
| US3 (P2) | T051-T054 (4) | Amex amount convention |
| US4 (P3) | T055-T063 (9) | Frontend mapping editor |
| US5 (P4) | T064-T068 (5) | Tier usage tracking |
| Polish | T069-T078 (10) | Transaction endpoints, edge cases |

**Total Tasks**: 80

---

## Notes

- [P] tasks = different files, no dependencies within same phase
- [US#] label maps task to specific user story
- MVP = Phase 1 + Phase 2 + Phase 3 (US1) = 43 tasks
- Each user story independently testable per quickstart.md scenarios
- Commit after each task or logical group
