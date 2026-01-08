# Tasks: HTML Receipt Parsing

**Input**: Design documents from `/specs/029-html-receipt-parsing/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Not explicitly requested - test tasks omitted per spec.md (no TDD requirement specified)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Web app**: `backend/src/`, `frontend/src/`
- Backend structure per plan.md: `ExpenseFlow.Api/`, `ExpenseFlow.Core/`, `ExpenseFlow.Infrastructure/`, `ExpenseFlow.Shared/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Install dependencies and configure HTML processing infrastructure

- [x] T001 Install PuppeteerSharp NuGet package in backend/src/ExpenseFlow.Infrastructure/ExpenseFlow.Infrastructure.csproj
- [x] T002 [P] Install HtmlSanitizer NuGet package in backend/src/ExpenseFlow.Infrastructure/ExpenseFlow.Infrastructure.csproj
- [x] T003 [P] Install HtmlAgilityPack NuGet package in backend/src/ExpenseFlow.Infrastructure/ExpenseFlow.Infrastructure.csproj
- [x] T004 Add HTML configuration section to backend/src/ExpenseFlow.Api/appsettings.json (ReceiptProcessing.Html settings)
- [x] T005 [P] Update Docker image to include Chromium dependencies in backend/Dockerfile

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core interfaces and DTOs that ALL user stories depend on

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T006 Create IHtmlSanitizationService interface in backend/src/ExpenseFlow.Core/Interfaces/IHtmlSanitizationService.cs
- [x] T007 [P] Create IHtmlReceiptExtractionService interface in backend/src/ExpenseFlow.Core/Interfaces/IHtmlReceiptExtractionService.cs
- [x] T008 [P] Create IHtmlThumbnailService interface in backend/src/ExpenseFlow.Core/Interfaces/IHtmlThumbnailService.cs
- [x] T009 [P] Create HtmlExtractionMetricsDto in backend/src/ExpenseFlow.Shared/DTOs/HtmlExtractionMetricsDto.cs
- [x] T010 [P] Create HtmlExtractionRequestDto in backend/src/ExpenseFlow.Shared/DTOs/HtmlExtractionRequestDto.cs
- [x] T011 Implement HtmlSanitizationService in backend/src/ExpenseFlow.Infrastructure/Services/HtmlSanitizationService.cs
- [x] T012 Register HTML services in backend/src/ExpenseFlow.Infrastructure/Extensions/ServiceCollectionExtensions.cs
- [x] T013 Add text/html to AllowedContentTypes in ReceiptService configuration (via appsettings.json)
- [x] T014 Add HTML magic byte validation (<!DOCTYPE, <html, <?xml) in backend/src/ExpenseFlow.Core/Services/ReceiptService.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Upload HTML Receipt Email (Priority: P1) MVP

**Goal**: Users can upload .html/.htm receipt files and see AI-extracted vendor, date, and amount

**Independent Test**: Upload a sample HTML receipt file (Amazon order confirmation) and verify extracted vendor, date, and amount appear correctly in the receipt detail view

### Implementation for User Story 1

- [x] T015 [US1] Implement HtmlReceiptExtractionService with Azure OpenAI integration in backend/src/ExpenseFlow.Infrastructure/Services/HtmlReceiptExtractionService.cs
- [x] T016 [US1] Create AI extraction prompt template for HTML receipts in backend/src/ExpenseFlow.Infrastructure/Services/HtmlReceiptExtractionService.cs
- [x] T017 [US1] Implement ExtractWithMetricsAsync method with logging in backend/src/ExpenseFlow.Infrastructure/Services/HtmlReceiptExtractionService.cs
- [x] T018 [US1] Extend ProcessReceiptJob to handle text/html content type in backend/src/ExpenseFlow.Infrastructure/Jobs/ProcessReceiptJob.cs
- [x] T019 [US1] Add HTML extraction path in ProcessReceiptJob that calls HtmlReceiptExtractionService in backend/src/ExpenseFlow.Infrastructure/Jobs/ProcessReceiptJob.cs
- [x] T020 [US1] Implement failed extraction storage (raw HTML + metrics) to blob storage in backend/src/ExpenseFlow.Infrastructure/Services/HtmlReceiptExtractionService.cs
- [x] T021 [US1] Add extraction metrics structured logging (confidence scores, processing time, field counts) in backend/src/ExpenseFlow.Infrastructure/Services/HtmlReceiptExtractionService.cs
- [x] T022 [US1] Update ReceiptsController to accept text/html content type in backend/src/ExpenseFlow.Api/Controllers/ReceiptsController.cs

**Checkpoint**: User Story 1 complete - HTML receipts can be uploaded and data extracted via AI. Test with sample Amazon/Uber HTML receipts.

---

## Phase 4: User Story 2 - HTML Receipt Thumbnail Generation (Priority: P2)

**Goal**: HTML receipts display visual thumbnails in the receipts list view

**Independent Test**: Upload an HTML receipt and verify a visual thumbnail appears in the receipts list

### Implementation for User Story 2

- [x] T023 [US2] Implement HtmlThumbnailService with PuppeteerSharp in backend/src/ExpenseFlow.Infrastructure/Services/HtmlThumbnailService.cs
- [x] T024 [US2] Add Chromium browser initialization and pooling in backend/src/ExpenseFlow.Infrastructure/Services/HtmlThumbnailService.cs
- [x] T025 [US2] Implement GenerateThumbnailAsync (800x600 viewport, crop to 200x200) in backend/src/ExpenseFlow.Infrastructure/Services/HtmlThumbnailService.cs
- [x] T026 [US2] Implement IsAvailableAsync health check for Chromium availability in backend/src/ExpenseFlow.Infrastructure/Services/HtmlThumbnailService.cs
- [x] T027 [US2] Integrate thumbnail generation into ProcessReceiptJob for HTML receipts in backend/src/ExpenseFlow.Infrastructure/Jobs/ProcessReceiptJob.cs
- [x] T028 [US2] Add thumbnail fallback (generate placeholder image) if Chromium unavailable in backend/src/ExpenseFlow.Infrastructure/Jobs/ProcessReceiptJob.cs

**Checkpoint**: User Story 2 complete - HTML receipts show visual thumbnails in list view. Test with various HTML receipt formats.

---

## Phase 5: User Story 3 - HTML Receipt Viewing (Priority: P2)

**Goal**: Users can view the full sanitized HTML content of a receipt in the detail view

**Independent Test**: Upload an HTML receipt and view it in the receipt detail view, confirming layout matches original email appearance with scripts blocked

### Implementation for User Story 3

- [x] T029 [US3] Add sanitizedHtmlUrl property to ReceiptDto in backend/src/ExpenseFlow.Shared/DTOs/ReceiptDto.cs (SKIPPED: Using dedicated endpoint instead of DTO property - cleaner architecture)
- [x] T030 [US3] Implement GET /api/receipts/{id}/html endpoint in backend/src/ExpenseFlow.Api/Controllers/ReceiptsController.cs
- [x] T031 [US3] Add HTML content retrieval from blob storage in GET /api/receipts/{id}/html endpoint in backend/src/ExpenseFlow.Api/Controllers/ReceiptsController.cs
- [x] T032 [US3] Apply HtmlSanitizationService to returned content in GET /api/receipts/{id}/html in backend/src/ExpenseFlow.Api/Controllers/ReceiptsController.cs
- [x] T033 [US3] Return 400 Bad Request for non-HTML receipts in GET /api/receipts/{id}/html in backend/src/ExpenseFlow.Api/Controllers/ReceiptsController.cs
- [x] T034 [US3] Update receipt-viewer.tsx to render sanitized HTML content in frontend/src/components/ui/document-viewer.tsx
- [x] T035 [US3] Add iframe sandbox for HTML rendering with script execution blocked in frontend/src/components/ui/document-viewer.tsx
- [x] T036 [US3] Add loading state and error handling for HTML content fetch in frontend/src/components/ui/document-viewer.tsx

**Checkpoint**: User Story 3 complete - HTML receipts render with original formatting, scripts blocked. Test XSS prevention with malicious HTML.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [x] T037 [P] Add sample HTML receipts for testing in test-data/html-receipts/ (amazon-order.html, uber-ride.html, airline-booking.html)
- [x] T038 [P] Create malformed.html and no-receipt-data.html for edge case testing in test-data/html-receipts/
- [x] T039 Update quickstart.md verification checklist based on implementation in specs/029-html-receipt-parsing/quickstart.md
- [ ] T040 Run full verification: upload HTML receipt, verify extraction, thumbnail, and HTML viewing

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion
  - US1 can start immediately after Phase 2
  - US2 and US3 can run in parallel after Phase 2 (no cross-dependencies)
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Phase 2 - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Phase 2 - Independent of US1 (thumbnails don't need extraction)
- **User Story 3 (P2)**: Can start after Phase 2 - Independent of US1 and US2 (HTML viewing is separate)

### Within Each User Story

- Models/DTOs before services
- Services before API endpoints
- Backend before frontend integration
- Core implementation before integration points

### Parallel Opportunities

**Phase 1 (Setup)**:
```
T002, T003, T005 can run in parallel with T001
```

**Phase 2 (Foundational)**:
```
T007, T008, T009, T010 can run in parallel with T006
```

**After Phase 2 completes**:
```
US1 (Phase 3), US2 (Phase 4), US3 (Phase 5) can all start in parallel
```

---

## Parallel Example: Foundational Phase

```bash
# Launch all interface and DTO tasks together:
Task: "Create IHtmlSanitizationService interface in backend/src/ExpenseFlow.Core/Interfaces/IHtmlSanitizationService.cs"
Task: "Create IHtmlReceiptExtractionService interface in backend/src/ExpenseFlow.Core/Interfaces/IHtmlReceiptExtractionService.cs"
Task: "Create IHtmlThumbnailService interface in backend/src/ExpenseFlow.Core/Interfaces/IHtmlThumbnailService.cs"
Task: "Create HtmlExtractionMetricsDto in backend/src/ExpenseFlow.Shared/DTOs/HtmlExtractionMetricsDto.cs"
Task: "Create HtmlExtractionRequestDto in backend/src/ExpenseFlow.Shared/DTOs/HtmlExtractionRequestDto.cs"
```

## Parallel Example: User Stories After Foundation

```bash
# With multiple developers, all three user stories can proceed simultaneously:
Developer A: User Story 1 (T015-T022) - AI Extraction
Developer B: User Story 2 (T023-T028) - Thumbnail Generation
Developer C: User Story 3 (T029-T036) - HTML Viewing
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (install packages, configure settings)
2. Complete Phase 2: Foundational (interfaces, DTOs, sanitization service)
3. Complete Phase 3: User Story 1 (AI extraction pipeline)
4. **STOP and VALIDATE**: Upload test HTML receipts, verify extraction accuracy
5. Deploy/demo if ready - users can upload HTML receipts even without thumbnails

### Incremental Delivery

1. **MVP (Phase 1-3)**: HTML upload + AI extraction = Core value delivered
2. **+Thumbnails (Phase 4)**: Add visual previews in receipts list
3. **+Viewing (Phase 5)**: Add full HTML rendering in detail view
4. Each story adds UX polish without breaking core functionality

### Suggested MVP Scope

**User Story 1 only (Tasks T001-T022)**:
- Users can upload HTML receipts
- AI extracts vendor, date, amount, line items
- Extracted data displays in receipt detail view
- Thumbnail shows placeholder (generic HTML icon)
- HTML content not rendered yet (just extracted text fields shown)

This delivers the core value proposition with minimal effort. Stories 2 and 3 are UX enhancements.

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Security: HtmlSanitizationService MUST be complete before any HTML rendering (T011 blocks T032)
