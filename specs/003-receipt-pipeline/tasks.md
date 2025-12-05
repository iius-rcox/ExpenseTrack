# Tasks: Receipt Upload Pipeline

**Feature Branch**: `003-receipt-pipeline`
**Generated**: 2025-12-05
**Source**: [spec.md](./spec.md), [plan.md](./plan.md), [data-model.md](./data-model.md)

## Task Overview

| Phase | Description | Tasks |
|-------|-------------|-------|
| 0 | Setup & Configuration | 6 |
| 1 | Foundational (Entities, Migrations, Infrastructure Services) | 8 |
| 2 | US1: Upload Single Receipt (P1) | 6 |
| 3 | US2: Batch Upload Multiple Receipts (P1) | 4 |
| 4 | US3: View Receipt List (P2) | 4 |
| 5 | US4: View Unmatched Receipts (P2) | 2 |
| 6 | US5: Multi-Page Receipt Handling (P3) | 2 |
| 7 | US6: Error Recovery (P3) | 3 |
| 8 | Polish & Cross-Cutting | 4 |
| **Total** | | **39** |

---

## Phase 0: Setup & Configuration

### Task 0.1: Create Feature Branch
- [X] Create branch `003-receipt-pipeline` from `main`
- [X] Verify Sprint 2 code is merged and working

**Acceptance**: Branch created, `git status` shows clean working tree

---

### Task 0.2: Add NuGet Packages
- [X] Add `Azure.AI.DocumentIntelligence` v1.0.0 to Infrastructure project
- [X] Add `Magick.NET-Q16-AnyCPU` to Infrastructure project (HEIC conversion)
- [X] Add `Microsoft.Extensions.Http.Polly` to Infrastructure project (retry policies)

**Commands**:
```bash
cd backend/src/ExpenseFlow.Infrastructure
dotnet add package Azure.AI.DocumentIntelligence --version 1.0.0
dotnet add package Magick.NET-Q16-AnyCPU
dotnet add package Microsoft.Extensions.Http.Polly
```

**Acceptance**: `dotnet restore` succeeds, packages appear in csproj

---

### Task 0.3: Configure App Settings
- [X] Add `DocumentIntelligence` section to `appsettings.json`
- [X] Add `BlobStorage` section with container names
- [X] Add `ReceiptProcessing` section with limits and thresholds

**Reference**: See `quickstart.md` Step 2 for configuration schema

**Acceptance**: Settings load without errors on startup

---

### Task 0.4: Configure Azure Blob Lifecycle Policy
- [X] Create lifecycle policy for 30-day auto-deletion of receipts

**Commands**:
```bash
az storage account management-policy create \
  --account-name ccproctemp2025 \
  --resource-group rg_prod \
  --policy @infrastructure/storage/lifecycle-policy.json
```

**Acceptance**: `az storage account management-policy show` returns the policy

---

### Task 0.5: Update Kubernetes Secrets
- [ ] Add Document Intelligence API key to Kubernetes secrets
- [ ] Verify secrets are accessible from pods

**Acceptance**: `kubectl get secret expenseflow-secrets -o yaml` shows doc-intelligence-key

---

### Task 0.6: Verify Microsoft Defender for Storage
- [ ] Verify Microsoft Defender for Storage is enabled on `ccproctemp2025`
- [ ] Confirm malware scanning is active for blob uploads

**Commands**:
```bash
az security pricing show --name StorageAccounts --query pricingTier
az storage account show --name ccproctemp2025 --resource-group rg_prod --query "properties.azureFilesIdentityBasedAuthentication"
```

**Requirements**: FR-027

**Acceptance**: Defender for Storage shows "Standard" tier enabled

---

## Phase 1: Foundational (Entities, Migrations, Infrastructure Services)

### Task 1.1: Create ReceiptStatus Enum
**File**: `backend/src/ExpenseFlow.Shared/Enums/ReceiptStatus.cs`
- [ ] Create `ReceiptStatus` enum with values: Uploaded, Processing, Ready, ReviewRequired, Error, Unmatched, Matched

**Reference**: `data-model.md` lines 82-93

**Acceptance**: Enum compiles, all 7 statuses defined

---

### Task 1.2: Create ReceiptLineItem Class
**File**: `backend/src/ExpenseFlow.Core/Entities/ReceiptLineItem.cs`
- [ ] Create `ReceiptLineItem` class with: Description, Quantity, UnitPrice, TotalPrice, Confidence

**Reference**: `data-model.md` lines 233-240

**Acceptance**: Class compiles, properties match data model

---

### Task 1.3: Create Receipt Entity
**File**: `backend/src/ExpenseFlow.Core/Entities/Receipt.cs`
- [ ] Create `Receipt` entity with all 18 properties from data model
- [ ] Add navigation property to `User`
- [ ] Add `ReceiptLineItem` collection as `List<ReceiptLineItem>?`
- [ ] Add `ConfidenceScores` as `Dictionary<string, double>?`

**Reference**: `data-model.md` lines 206-231

**Acceptance**: Entity compiles, all properties match data model

---

### Task 1.4: Create Receipt EF Core Configuration
**File**: `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/ReceiptConfiguration.cs`
- [ ] Create `IEntityTypeConfiguration<Receipt>` implementation
- [ ] Configure table name `receipts`
- [ ] Configure JSONB columns for `LineItems` and `ConfidenceScores`
- [ ] Configure indexes: user_id, status, (user_id, status), created_at DESC
- [ ] Configure FK relationship to User with CASCADE delete

**Reference**: `data-model.md` lines 245-283

**Acceptance**: Configuration compiles, applied in DbContext

---

### Task 1.5: Create EF Core Migration
- [ ] Add migration `CreateReceiptsTable`
- [ ] Verify migration SQL matches data-model.md
- [ ] Apply migration to development database

**Commands**:
```bash
cd backend/src/ExpenseFlow.Api
dotnet ef migrations add CreateReceiptsTable --project ../ExpenseFlow.Infrastructure
dotnet ef database update
```

**Acceptance**: Migration applied, `receipts` table exists in PostgreSQL

---

### Task 1.6: Create BlobStorageService
**File**: `backend/src/ExpenseFlow.Infrastructure/Services/BlobStorageService.cs`
- [ ] Create `IBlobStorageService` interface in Core project
- [ ] Implement `UploadAsync(Stream, string path, string contentType)` returning blob URL
- [ ] Implement `DeleteAsync(string blobUrl)`
- [ ] Implement `GenerateSasUrl(string blobUrl, TimeSpan expiry)` for temporary access
- [ ] Use path convention: `receipts/{userId}/{year}/{month}/{uuid}_{filename}`

**Requirements**: FR-003, FR-006

**Acceptance**: Unit tests pass for upload/delete operations

---

### Task 1.7: Create HeicConversionService
**File**: `backend/src/ExpenseFlow.Infrastructure/Services/HeicConversionService.cs`
- [ ] Create `IHeicConversionService` interface in Core project
- [ ] Implement `ConvertToJpegAsync(Stream heicStream)` returning JPEG stream
- [ ] Use Magick.NET for conversion (NOT SkiaSharp - it doesn't support HEIC)
- [ ] Handle conversion errors gracefully

**Requirements**: FR-002

**Reference**: `research.md` - Magick.NET is required, SkiaSharp cannot read HEIC

**Acceptance**: Can convert sample HEIC file to JPEG

---

### Task 1.8: Create DocumentIntelligenceService
**File**: `backend/src/ExpenseFlow.Infrastructure/Services/DocumentIntelligenceService.cs`
- [ ] Create `IDocumentIntelligenceService` interface in Core project
- [ ] Implement `ExtractReceiptDataAsync(Stream imageStream, string contentType)`
- [ ] Use `Azure.AI.DocumentIntelligence` SDK with prebuilt-receipt model
- [ ] Extract: MerchantName, TransactionDate, Total, SubTotal, TotalTax, Items
- [ ] Return extraction result with confidence scores
- [ ] Check service health before extraction (fail immediately if unavailable per FR-014a)

**Requirements**: FR-010, FR-011, FR-013, FR-014a

**Reference**: `research.md` for SDK usage patterns

**Acceptance**: Integration test with sample receipt image extracts vendor/date/amount

---

## Phase 2: US1 - Upload Single Receipt (P1)

> **User Story**: A user takes a photo of a restaurant receipt and uploads it through the web interface. The system accepts the image, stores it securely, and automatically extracts the vendor name, date, total amount, and line items using Azure Document Intelligence.

### Task 2.1: Create IReceiptRepository Interface
**File**: `backend/src/ExpenseFlow.Core/Interfaces/IReceiptRepository.cs`
- [ ] Define `AddAsync(Receipt receipt)`
- [ ] Define `GetByIdAsync(Guid id, Guid userId)`
- [ ] Define `UpdateAsync(Receipt receipt)`
- [ ] Define `DeleteAsync(Guid id, Guid userId)`

**Acceptance**: Interface compiles

---

### Task 2.2: Implement ReceiptRepository
**File**: `backend/src/ExpenseFlow.Infrastructure/Repositories/ReceiptRepository.cs`
- [ ] Implement `IReceiptRepository`
- [ ] Ensure user_id filtering on all queries (row-level security in code)

**Requirements**: FR-025

**Acceptance**: Repository unit tests pass

---

### Task 2.3: Create ReceiptService
**File**: `backend/src/ExpenseFlow.Core/Services/ReceiptService.cs`
- [ ] Create `IReceiptService` interface
- [ ] Implement `UploadReceiptAsync(Stream, string filename, string contentType, Guid userId)`
  - Validate file size (max 25MB) per FR-004
  - Validate content type matches magic bytes per FR-026
  - Convert HEIC to JPEG if needed per FR-002
  - Upload to Blob Storage per FR-003
  - Create Receipt entity with status=Uploaded
  - Queue ProcessReceiptJob via Hangfire per FR-007

**Requirements**: FR-001, FR-002, FR-003, FR-004, FR-006, FR-007, FR-026

**Acceptance**: Service unit tests pass, receipt created with Uploaded status

---

### Task 2.4: Create ProcessReceiptJob
**File**: `backend/src/ExpenseFlow.Infrastructure/Jobs/ProcessReceiptJob.cs`
- [ ] Create Hangfire job that takes `receiptId` parameter
- [ ] Load receipt from database
- [ ] Set status to Processing
- [ ] Call DocumentIntelligenceService.ExtractReceiptDataAsync
- [ ] Update receipt with extracted data
- [ ] Set status to Ready (confidence >= 60%) or ReviewRequired (confidence < 60%)
- [ ] Handle errors: set status to Error with error message
- [ ] Implement retry logic (max 3 retries per FR-014)

**Requirements**: FR-008, FR-009, FR-010, FR-011, FR-013, FR-013a, FR-014

**Acceptance**: Job processes receipt and updates status/extracted fields

---

### Task 2.5: Create Upload DTOs
**File**: `backend/src/ExpenseFlow.Api/DTOs/Receipts/`
- [ ] Create `ReceiptSummaryDto` (id, thumbnailUrl, originalFilename, status, vendor, date, amount, currency, createdAt)
- [ ] Create `ReceiptDetailDto` extends ReceiptSummaryDto (adds blobUrl, contentType, fileSize, tax, lineItems, confidenceScores, errorMessage, retryCount, processedAt)
- [ ] Create `UploadResponseDto` (receipts[], totalUploaded, failed[])

**Reference**: `contracts/receipts-api.yaml` schemas

**Acceptance**: DTOs match OpenAPI schema

---

### Task 2.6: Create ReceiptsController Upload Endpoint
**File**: `backend/src/ExpenseFlow.Api/Controllers/ReceiptsController.cs`
- [ ] Create `ReceiptsController` with `[Authorize]` attribute
- [ ] Implement `POST /api/receipts` accepting `IFormFileCollection`
- [ ] Validate at least one file provided
- [ ] Call ReceiptService.UploadReceiptAsync for each file
- [ ] Return 201 with `UploadResponseDto`
- [ ] Return 400 for validation errors
- [ ] Return 413 for files exceeding 25MB
- [ ] Return 503 if Document Intelligence unavailable

**Requirements**: FR-018, FR-024

**Reference**: `contracts/receipts-api.yaml` POST /receipts

**Acceptance**: Postman test: upload JPG receipt returns 201 with receipt ID

---

## Phase 3: US2 - Batch Upload Multiple Receipts (P1)

> **User Story**: A user returns from a business trip with 15 receipts. They select all receipt images at once and upload them in a single batch operation.

### Task 3.1: Enhance Upload Endpoint for Batch
- [ ] Validate max 20 files per batch per FR-005
- [ ] Process files in parallel using `Parallel.ForEachAsync`
- [ ] Track individual file failures without failing entire batch
- [ ] Return partial success response with failed[] array

**Requirements**: FR-005

**Acceptance**: Upload 5 files simultaneously, all 5 appear in response

---

### Task 3.2: Add Upload Progress Tracking [DEFERRED]
> **Deferred to future sprint**: SignalR real-time progress tracking adds complexity without blocking core functionality. HTTP response with batch results is sufficient for MVP.

**Status**: Skipped - not required for Sprint 3

---

### Task 3.3: Create Frontend ReceiptUploader Component
**File**: `frontend/src/components/receipts/ReceiptUploader.tsx`
- [ ] Create drag-drop upload zone using react-dropzone
- [ ] Display file previews before upload
- [ ] Show individual progress bars for each file
- [ ] Handle errors per-file without blocking others
- [ ] Call POST /api/receipts with FormData

**Acceptance**: Can drag-drop 5 files and see upload progress

---

### Task 3.4: Add File Type Validation (Frontend)
- [ ] Validate accepted types: .pdf, .jpg, .jpeg, .png, .heic, .heif
- [ ] Show clear error for rejected file types
- [ ] Validate file size client-side (25MB max)

**Acceptance**: Rejected file types show error before upload attempt

---

## Phase 4: US3 - View Receipt List (P2)

> **User Story**: A user wants to see all their uploaded receipts with thumbnails and extraction results.

### Task 4.1: Add List Query to ReceiptRepository
- [ ] Implement `GetPagedAsync(Guid userId, ReceiptStatus? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize)`
- [ ] Return `(IEnumerable<Receipt>, int totalCount)`
- [ ] Apply indexes for efficient filtering

**Requirements**: FR-016, FR-019

**Acceptance**: Repository returns paginated results with correct total count

---

### Task 4.2: Create List Receipts Endpoint
**File**: `backend/src/ExpenseFlow.Api/Controllers/ReceiptsController.cs`
- [ ] Implement `GET /api/receipts` with query parameters: status, fromDate, toDate, page, pageSize
- [ ] Return `ReceiptListResponseDto` with items, page, pageSize, totalCount, totalPages

**Reference**: `contracts/receipts-api.yaml` GET /receipts

**Acceptance**: GET /api/receipts returns paginated list

---

### Task 4.3: Create Get Receipt Detail Endpoint
**File**: `backend/src/ExpenseFlow.Api/Controllers/ReceiptsController.cs`
- [ ] Implement `GET /api/receipts/{id}`
- [ ] Return `ReceiptDetailDto` with all fields
- [ ] Return 404 if receipt not found or not owned by user

**Requirements**: FR-020, FR-025

**Reference**: `contracts/receipts-api.yaml` GET /receipts/{id}

**Acceptance**: GET /api/receipts/{id} returns full receipt detail

---

### Task 4.4: Create Frontend ReceiptList Component
**File**: `frontend/src/components/receipts/ReceiptList.tsx`
- [ ] Display receipt cards with thumbnail, vendor, date, amount, status
- [ ] Implement status filter dropdown
- [ ] Implement date range filter
- [ ] Implement pagination controls
- [ ] Navigate to detail view on card click

**Acceptance**: Receipt list displays with filters and pagination working

---

## Phase 5: US4 - View Unmatched Receipts (P2)

> **User Story**: A user wants to see receipts that have not been matched to any credit card transaction yet.

### Task 5.1: Add Unmatched Query to ReceiptRepository
- [ ] Implement `GetUnmatchedAsync(Guid userId, int page, int pageSize)`
- [ ] Filter where status = 'Unmatched'
- [ ] Use partial index `idx_receipts_unmatched` for efficiency

**Requirements**: FR-021

**Acceptance**: Repository returns only unmatched receipts

---

### Task 5.2: Create Unmatched Receipts Endpoint
**File**: `backend/src/ExpenseFlow.Api/Controllers/ReceiptsController.cs`
- [ ] Implement `GET /api/receipts/unmatched`
- [ ] Return `ReceiptListResponseDto`

**Reference**: `contracts/receipts-api.yaml` GET /receipts/unmatched

**Acceptance**: GET /api/receipts/unmatched returns only unmatched receipts

---

## Phase 6: US5 - Multi-Page Receipt Handling (P3)

> **User Story**: A user uploads a hotel folio that spans 3 pages. The system processes all pages together.

### Task 6.1: Handle Multi-Page PDFs in DocumentIntelligenceService
- [ ] Ensure multi-page PDFs are processed as single document
- [ ] Extract line items from all pages
- [ ] Use final page total as receipt amount

**Requirements**: FR-012

**Acceptance**: 3-page PDF creates single receipt with all line items

---

### Task 6.2: Update Receipt Detail for Multi-Page Display
- [ ] Store page count in receipt metadata
- [ ] Frontend: implement page navigation for multi-page receipts

**Acceptance**: Can view all pages of multi-page PDF in detail view

---

## Phase 7: US6 - Error Recovery (P3)

> **User Story**: A user uploads a blurry receipt. Document Intelligence fails. The user can retry or manually enter data.

### Task 7.1: Create Retry Endpoint
**File**: `backend/src/ExpenseFlow.Api/Controllers/ReceiptsController.cs`
- [ ] Implement `POST /api/receipts/{id}/retry`
- [ ] Validate receipt is in Error status
- [ ] Validate retry_count < 3
- [ ] Reset status to Processing, increment retry_count
- [ ] Queue new ProcessReceiptJob
- [ ] Return 400 if cannot retry (not Error status or max retries reached)

**Requirements**: FR-014, FR-022

**Reference**: `contracts/receipts-api.yaml` POST /receipts/{id}/retry

**Acceptance**: POST /api/receipts/{id}/retry requeues failed receipt

---

### Task 7.2: Create Update Receipt Endpoint
**File**: `backend/src/ExpenseFlow.Api/Controllers/ReceiptsController.cs`
- [ ] Implement `PUT /api/receipts/{id}`
- [ ] Accept `ReceiptUpdateRequestDto` (vendor, date, amount, tax, currency, lineItems)
- [ ] Update receipt fields
- [ ] If status was Error or ReviewRequired, change to Ready

**Requirements**: FR-023

**Reference**: `contracts/receipts-api.yaml` PUT /receipts/{id}

**Acceptance**: PUT /api/receipts/{id} updates receipt data

---

### Task 7.3: Create Delete Receipt Endpoint
**File**: `backend/src/ExpenseFlow.Api/Controllers/ReceiptsController.cs`
- [ ] Implement `DELETE /api/receipts/{id}`
- [ ] Delete blob from storage
- [ ] Delete receipt from database
- [ ] Return 204 on success

**Reference**: `contracts/receipts-api.yaml` DELETE /receipts/{id}

**Acceptance**: DELETE /api/receipts/{id} removes receipt and blob

---

## Phase 8: Polish & Cross-Cutting

### Task 8.1: Add Thumbnail Generation
- [ ] Generate 200x200 thumbnail after upload
- [ ] Store in `thumbnails/` container
- [ ] Update receipt.ThumbnailUrl

**Acceptance**: Receipts have thumbnail URLs, images display in list view

---

### Task 8.2: Implement Magic Byte Validation
**File**: `backend/src/ExpenseFlow.Infrastructure/Services/FileValidationService.cs`
- [ ] Create service to validate file content type matches extension
- [ ] Check magic bytes for PDF, JPEG, PNG, HEIC
- [ ] Reject mismatched files with clear error

**Requirements**: FR-026

**Acceptance**: Renamed .exe file with .jpg extension is rejected

---

### Task 8.3: Add Integration Tests
**File**: `backend/tests/ExpenseFlow.Integration.Tests/`
- [ ] Test full upload flow with TestContainers
- [ ] Test receipt list pagination
- [ ] Test retry flow
- [ ] Test authorization (user can only see own receipts)

**Acceptance**: All integration tests pass

---

### Task 8.4: Update API Documentation
- [ ] Verify OpenAPI spec matches implementation
- [ ] Add example requests/responses
- [ ] Generate Swagger UI documentation

**Acceptance**: Swagger UI shows all receipt endpoints with examples

---

## Validation Checklist

After completing all tasks, verify:

- [ ] All functional requirements (FR-001 through FR-027) are implemented
- [ ] All user stories have passing acceptance scenarios
- [ ] All success criteria (SC-001 through SC-008) are measurable
- [ ] API matches `contracts/receipts-api.yaml`
- [ ] Database matches `data-model.md`
- [ ] Unit test coverage > 80%
- [ ] Integration tests pass
- [ ] Manual testing with `quickstart.md` validation tests passes

---

## Quick Reference

**Key Files**:
- Entity: `backend/src/ExpenseFlow.Core/Entities/Receipt.cs`
- Repository: `backend/src/ExpenseFlow.Infrastructure/Repositories/ReceiptRepository.cs`
- Service: `backend/src/ExpenseFlow.Core/Services/ReceiptService.cs`
- Controller: `backend/src/ExpenseFlow.Api/Controllers/ReceiptsController.cs`
- Job: `backend/src/ExpenseFlow.Infrastructure/Jobs/ProcessReceiptJob.cs`

**Key Thresholds**:
- Max file size: 25MB
- Max batch size: 20 files
- Confidence threshold: 60%
- Max retries: 3
- Data retention: 30 days

**Statuses**: Uploaded → Processing → Ready/ReviewRequired/Error → Unmatched/Matched
