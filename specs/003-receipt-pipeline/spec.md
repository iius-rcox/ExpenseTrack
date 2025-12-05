# Feature Specification: Receipt Upload Pipeline

**Feature Branch**: `003-receipt-pipeline`
**Created**: 2025-12-05
**Status**: Draft
**Sprint**: 3 (Weeks 5-6)
**Input**: ExpenseFlow Sprint Plan - Sprint 3: Receipt Upload Pipeline

## Clarifications

### Session 2025-12-05

- Q: What confidence score threshold triggers Review Required status? → A: 60% (more permissive)
- Q: How should malware scanning be implemented? → A: Microsoft Defender for Storage
- Q: How long should receipts be retained? → A: 1 month
- Q: How to handle Document Intelligence service unavailability? → A: Fail immediately, require re-upload
- Q: How should HEIC to JPG conversion be implemented? → A: Magick.NET library (SkiaSharp cannot read HEIC)

## Overview

Enable users to upload receipts (PDF, JPG, PNG, HEIC), process them through Azure Document Intelligence for data extraction, and store them with extracted metadata in Azure Blob Storage. This sprint builds on the Core Backend established in Sprint 2.

## User Scenarios & Testing

### User Story 1 - Upload Single Receipt (Priority: P1)

A user takes a photo of a restaurant receipt and uploads it through the web interface. The system accepts the image, stores it securely, and automatically extracts the vendor name, date, total amount, and line items using Azure Document Intelligence.

**Why this priority**: Core functionality - without receipt upload, no expense tracking is possible. This is the foundational action for the entire application.

**Independent Test**: Upload a single JPG receipt, verify it appears in the receipt list with Processing status, then Ready status with extracted vendor/date/amount within 30 seconds.

**Acceptance Scenarios**:

1. **Given** a logged-in user, **When** they upload a JPG receipt file, **Then** the receipt is stored in Blob Storage and appears in their receipt list with status Processing
2. **Given** a receipt in Processing status, **When** Document Intelligence completes extraction, **Then** the receipt status becomes Ready with vendor, date, and amount populated
3. **Given** a user uploads a receipt, **When** the upload completes, **Then** the file is stored in `receipts/{userId}/{year}/{month}/` path structure

---

### User Story 2 - Batch Upload Multiple Receipts (Priority: P1)

A user returns from a business trip with 15 receipts. They select all receipt images at once and upload them in a single batch operation. Each receipt is processed independently, allowing them to continue working while processing occurs in the background.

**Why this priority**: Real-world usage requires batch upload - users rarely have single receipts. Critical for user adoption.

**Independent Test**: Upload 5 receipts simultaneously via drag-drop, verify all 5 appear in list, and each processes independently within 2 minutes.

**Acceptance Scenarios**:

1. **Given** a logged-in user, **When** they drag-drop 5 receipt files, **Then** all 5 files are uploaded concurrently with individual progress indicators
2. **Given** a batch upload in progress, **When** one receipt fails extraction, **Then** other receipts continue processing successfully
3. **Given** multiple receipts uploaded, **When** viewing the receipt list, **Then** each receipt shows its individual processing status

---

### User Story 3 - View Receipt List (Priority: P2)

A user wants to see all their uploaded receipts with thumbnails and extraction results. They can filter by status (Processing, Ready, Error, Matched, Unmatched) and date range to find specific receipts.

**Why this priority**: Users need visibility into their uploaded receipts to verify extraction quality and manage their expense documentation.

**Independent Test**: After uploading 3 receipts, view the receipt list and verify thumbnails, vendor names, amounts, and statuses are displayed correctly.

**Acceptance Scenarios**:

1. **Given** a user with uploaded receipts, **When** they view the receipt list, **Then** they see thumbnails, vendor, date, amount, and status for each receipt
2. **Given** a receipt list, **When** filtering by Unmatched status, **Then** only receipts without matching transactions are shown
3. **Given** a receipt in the list, **When** clicking on it, **Then** a detail view shows the full receipt image and all extracted data

---

### User Story 4 - View Unmatched Receipts (Priority: P2)

A user wants to see receipts that have not been matched to any credit card transaction yet. This helps them identify receipts that need manual attention or transactions that might be missing.

**Why this priority**: Key for expense accountability - unmatched receipts represent potential compliance gaps.

**Independent Test**: Call GET /api/receipts/unmatched endpoint and verify only receipts with status=Unmatched are returned.

**Acceptance Scenarios**:

1. **Given** receipts with various statuses, **When** calling GET /api/receipts/unmatched, **Then** only receipts with status Unmatched are returned
2. **Given** a receipt that was matched, **When** calling GET /api/receipts/unmatched, **Then** that receipt is NOT included in results

---

### User Story 5 - Multi-Page Receipt Handling (Priority: P3)

A user uploads a hotel folio that spans 3 pages. The system recognizes it as a multi-page document and processes all pages together, extracting the total from the final page while capturing all line items across pages.

**Why this priority**: Common for hotel folios and detailed invoices, but less frequent than single-page receipts.

**Independent Test**: Upload a 3-page PDF receipt, verify single receipt record is created with all line items extracted from all pages.

**Acceptance Scenarios**:

1. **Given** a 3-page PDF receipt, **When** uploaded, **Then** a single receipt record is created (not 3 separate records)
2. **Given** a multi-page receipt, **When** Document Intelligence processes it, **Then** line items from all pages are captured
3. **Given** a multi-page receipt, **When** viewing the detail, **Then** all pages are viewable as a single document

---

### User Story 6 - Error Recovery for Failed Extraction (Priority: P3)

A user uploads a blurry or damaged receipt image. Document Intelligence fails to extract data reliably. The user is notified of the error and can either retry processing or manually enter the receipt details.

**Why this priority**: Error handling is essential for production quality but affects fewer receipts than happy path.

**Independent Test**: Upload an intentionally unreadable image, verify status becomes Error with descriptive message, verify retry button triggers reprocessing.

**Acceptance Scenarios**:

1. **Given** an unreadable receipt image, **When** Document Intelligence fails, **Then** receipt status becomes Error with error message describing the issue
2. **Given** a receipt with Error status, **When** user clicks Retry, **Then** the receipt is requeued for processing
3. **Given** a receipt with Error status, **When** user edits manually, **Then** they can enter vendor, date, amount directly

---

### Edge Cases

- What happens when a user uploads a file that is not a receipt (e.g., a blank page)?
  - Document Intelligence returns low confidence scores; receipt marked as Review Required
- How does the system handle duplicate receipt uploads?
  - System stores both but may flag potential duplicates based on vendor/date/amount matching
- What happens when Blob Storage is temporarily unavailable?
  - Upload fails with retry-able error; user prompted to try again
- How does the system handle very large files (50MB+ PDFs)?
  - Reject files over 25MB with clear error message; configurable limit
- What happens when Document Intelligence quota is exhausted?
  - Jobs queue and retry with exponential backoff; admin alerted
- How are HEIC files from iPhones handled?
  - Server-side conversion to JPG before Document Intelligence processing
- What happens when Document Intelligence service is unavailable?
  - Upload fails immediately with error; user must re-upload when service recovers

## Requirements

### Functional Requirements

**Upload and Storage**
- **FR-001**: System MUST accept receipt uploads in PDF, JPG, PNG, and HEIC formats
- **FR-002**: System MUST convert HEIC files to JPG using Magick.NET library before processing
- **FR-003**: System MUST store receipts in Azure Blob Storage with path pattern `receipts/{userId}/{year}/{month}/{filename}`
- **FR-004**: System MUST reject files exceeding 25MB with clear error message
- **FR-005**: System MUST support concurrent upload of up to 20 files in a single batch
- **FR-006**: System MUST generate unique filenames to prevent overwrites (UUID prefix)

**Processing Pipeline**
- **FR-007**: System MUST queue a Hangfire job (ProcessReceiptJob) immediately upon successful upload
- **FR-008**: System MUST process receipts asynchronously within 30 seconds average
- **FR-009**: System MUST update receipt status throughout processing: Uploaded -> Processing -> Ready/Error
- **FR-010**: System MUST use Azure Document Intelligence prebuilt-receipt model for extraction
- **FR-011**: System MUST extract: vendor name, transaction date, total amount, tax amount, line items
- **FR-012**: System MUST handle multi-page documents as single receipt records
- **FR-013**: System MUST store confidence scores for each extracted field
- **FR-013a**: System MUST mark receipts as "Review Required" when overall extraction confidence is below 60%
- **FR-014**: System MUST retry failed extractions up to 3 times with exponential backoff
- **FR-014a**: System MUST fail upload immediately when Document Intelligence is unavailable (no queuing)

**Data Storage**
- **FR-015**: System MUST create Receipts table with: id, user_id, blob_url, status, vendor_extracted, date_extracted, amount_extracted, tax_extracted, line_items_json, confidence_scores, error_message, created_at, processed_at
- **FR-016**: System MUST index receipts by user_id and status for efficient filtering
- **FR-017**: System MUST store extracted line items as JSONB array
- **FR-017a**: System MUST auto-delete receipts and blob files after 30 days retention period

**API Endpoints**
- **FR-018**: System MUST expose POST /api/receipts for single/batch upload (multipart/form-data)
- **FR-019**: System MUST expose GET /api/receipts for paginated receipt list with filtering
- **FR-020**: System MUST expose GET /api/receipts/{id} for receipt detail with all extracted data
- **FR-021**: System MUST expose GET /api/receipts/unmatched for receipts pending transaction match
- **FR-022**: System MUST expose POST /api/receipts/{id}/retry to requeue failed receipts
- **FR-023**: System MUST expose PUT /api/receipts/{id} for manual data entry/correction

**Security and Authorization**
- **FR-024**: System MUST require valid Entra ID JWT token for all receipt endpoints
- **FR-025**: System MUST ensure users can only access their own receipts (row-level security)
- **FR-026**: System MUST validate file content type matches extension (magic byte verification)
- **FR-027**: System MUST scan uploaded files for malware using Microsoft Defender for Storage

### Key Entities

- **Receipt**: Represents an uploaded receipt document with extraction results
  - Attributes: id, user_id, blob_url, original_filename, content_type, file_size, status, vendor_extracted, date_extracted, amount_extracted, tax_extracted, currency, line_items, confidence_scores, error_message, retry_count, created_at, processed_at
  - Relationships: belongs to User, will be linked to Transaction (Sprint 5)

- **ReceiptLineItem**: Individual line item extracted from receipt (stored as JSONB within Receipt)
  - Attributes: description, quantity, unit_price, total_price, confidence

## Success Criteria

### Measurable Outcomes

- **SC-001**: Users can upload a receipt and see extraction results within 30 seconds 95% of the time
- **SC-002**: Document Intelligence successfully extracts vendor, date, and amount from 85% of clear receipt images
- **SC-003**: Batch upload of 20 receipts completes initial upload within 10 seconds
- **SC-004**: System handles 100 concurrent receipt uploads without degradation
- **SC-005**: Receipt list loads within 2 seconds for users with up to 1000 receipts
- **SC-006**: Zero receipts accessible by unauthorized users (security audit passing)
- **SC-007**: Failed extractions automatically retry and succeed 50% of the time on retry
- **SC-008**: Storage costs remain under 0.10 USD per 1000 receipts (Blob Storage hot tier)

### Post-MVP Observability (Deferred)

The following success criteria require observability infrastructure not in scope for Sprint 3:
- SC-001: 30-second extraction SLA monitoring (requires Application Insights custom metrics)
- SC-004: 100 concurrent upload load testing (requires dedicated load test environment)

These will be addressed in a future observability sprint.

## Assumptions

1. Azure Document Intelligence is provisioned and accessible from AKS cluster
2. Azure Blob Storage container receipts exists with appropriate CORS settings
3. Users have stable internet connections for file uploads
4. Receipt images are generally legible (not extremely blurry/damaged)
5. Sprint 2 authentication and user management is complete and working

## Dependencies

- **Sprint 2**: Entra ID authentication, User entity, Hangfire configuration
- **Azure Services**: Document Intelligence (prebuilt-receipt), Blob Storage
- **NuGet**: Magick.NET-Q16-AnyCPU for HEIC to JPG conversion

## Out of Scope (Future Sprints)

- Receipt-to-transaction matching (Sprint 5)
- Receipt categorization and GL code suggestion (Sprint 6)
- Receipt sharing between users
- Receipt archival/deletion policies
- Mobile app upload (web only for now)
- OCR for non-English receipts
