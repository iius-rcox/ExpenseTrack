# Feature Specification: Receipt Thumbnail Previews

**Feature Branch**: `030-receipt-thumbnails`
**Created**: 2026-01-08
**Status**: Draft
**Input**: User description: "Create thumbnail previews for uploaded receipts. Support PDF and HTML receipt files. Show thumbnails in the expense list view. Clicking a thumbnail opens the full receipt. Generate thumbnails on upload."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Receipt Thumbnails in Expense List (Priority: P1)

As a user reviewing my expenses, I want to see small thumbnail previews of my receipts directly in the expense list, so I can quickly visually identify expenses without opening each receipt individually.

**Why this priority**: This is the core value proposition of the feature. Users spend significant time scrolling through expenses looking for specific receipts. Visual thumbnails dramatically speed up this process and reduce cognitive load.

**Independent Test**: Can be fully tested by uploading a receipt and verifying its thumbnail appears in the expense list. Delivers immediate value by enabling visual expense identification.

**Acceptance Scenarios**:

1. **Given** a user has uploaded receipts for their expenses, **When** they view the expense list, **Then** they see thumbnail images next to each expense that has an associated receipt
2. **Given** an expense has no receipt attached, **When** the user views the expense list, **Then** a placeholder icon indicates no receipt is available
3. **Given** the expense list contains many items, **When** the user scrolls through the list, **Then** thumbnails load progressively without blocking the list display

---

### User Story 2 - Quick Receipt Preview on Click (Priority: P1)

As a user who sees a thumbnail that looks relevant, I want to click it to view the full receipt, so I can verify the expense details without navigating away from my current view.

**Why this priority**: Thumbnails are only useful if users can quickly access the full receipt. This completes the core workflow and is essential for the feature to deliver value.

**Independent Test**: Can be fully tested by clicking any thumbnail and verifying the full receipt displays in a preview modal or expanded view.

**Acceptance Scenarios**:

1. **Given** a user is viewing the expense list with thumbnails, **When** they click on a thumbnail, **Then** the full-size receipt opens in a preview overlay
2. **Given** a user has opened a full receipt preview, **When** they click outside the preview or press Escape, **Then** the preview closes and they return to the expense list
3. **Given** a user is viewing a full receipt preview, **When** they want to see details, **Then** they can zoom in/out and pan across the receipt image

---

### User Story 3 - Automatic Thumbnail Generation for PDF Receipts (Priority: P2)

As a user who uploads PDF receipts, I want the system to automatically generate a thumbnail preview, so I can see a visual representation of my PDF receipts in the expense list.

**Why this priority**: PDFs are one of the most common receipt formats, especially for business expenses (airline tickets, hotel confirmations, software subscriptions). Supporting PDF thumbnails covers a large percentage of user uploads.

**Independent Test**: Can be tested by uploading a PDF receipt and verifying a thumbnail is generated showing the first page of the PDF.

**Acceptance Scenarios**:

1. **Given** a user uploads a PDF receipt, **When** the upload completes, **Then** a thumbnail is automatically generated from the first page of the PDF
2. **Given** a multi-page PDF receipt, **When** thumbnail generation completes, **Then** the thumbnail shows the first page (which typically contains the total and vendor info)
3. **Given** a PDF that cannot be rendered, **When** thumbnail generation fails, **Then** a generic PDF icon is displayed as a fallback

---

### User Story 4 - Automatic Thumbnail Generation for HTML Receipts (Priority: P2)

As a user who uploads HTML email receipts (e.g., from Amazon, online retailers), I want the system to generate a visual thumbnail, so I can preview these receipts the same way as image receipts.

**Why this priority**: HTML receipts from email are increasingly common for e-commerce purchases. This extends thumbnail support to cover the full range of receipt formats in the system.

**Independent Test**: Can be tested by uploading an HTML receipt file and verifying a thumbnail is generated showing the rendered HTML content.

**Acceptance Scenarios**:

1. **Given** a user uploads an HTML receipt, **When** the upload completes, **Then** a thumbnail is generated showing a rendered preview of the HTML content
2. **Given** an HTML receipt with external images, **When** generating the thumbnail, **Then** the thumbnail shows the HTML with available styling (external resources are optional)
3. **Given** an HTML file that cannot be rendered, **When** thumbnail generation fails, **Then** a generic HTML icon is displayed as a fallback

---

### User Story 5 - Thumbnail Generation for Existing Receipts (Priority: P3)

As a user with existing receipts uploaded before this feature, I want thumbnails generated for my historical receipts, so I have a consistent visual experience across all my expenses.

**Why this priority**: While important for user experience consistency, this is a background operation that doesn't block core functionality. New uploads work immediately; historical data can be processed over time.

**Independent Test**: Can be tested by verifying that receipts uploaded before the feature was deployed eventually display thumbnails after background processing completes.

**Acceptance Scenarios**:

1. **Given** receipts exist without thumbnails, **When** the background processing job runs, **Then** thumbnails are generated for existing receipts
2. **Given** a large number of historical receipts, **When** thumbnail backfill runs, **Then** processing occurs in batches to avoid system overload
3. **Given** thumbnail generation has completed for historical receipts, **When** the user views the expense list, **Then** all receipts display thumbnails consistently

---

### Edge Cases

- What happens when a receipt file is corrupted or cannot be processed? → Display a generic file-type icon as fallback
- What happens when a PDF is password-protected? → Display a "locked PDF" icon and skip thumbnail generation
- What happens when an HTML receipt contains malicious scripts? → HTML is sanitized before rendering; scripts are stripped
- What happens when the thumbnail service is temporarily unavailable? → Receipt upload succeeds; thumbnail generation is queued for retry
- What happens when storage is full for thumbnails? → Alert operations; continue with fallback icons until resolved
- What happens when a very large file is uploaded? → Apply size limits; generate thumbnail asynchronously to avoid timeout

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST generate thumbnail images when a receipt is uploaded
- **FR-002**: System MUST support thumbnail generation for image files (JPEG, PNG, HEIC, WebP)
- **FR-003**: System MUST support thumbnail generation for PDF files (first page rendered as image)
- **FR-004**: System MUST support thumbnail generation for HTML files (rendered as visual snapshot)
- **FR-005**: System MUST display receipt thumbnails in the expense list view
- **FR-006**: System MUST allow users to click a thumbnail to view the full receipt
- **FR-007**: System MUST display a placeholder icon when no receipt is attached to an expense
- **FR-008**: System MUST display a file-type-specific fallback icon when thumbnail generation fails
- **FR-009**: System MUST store thumbnails in a standard web-friendly format (JPEG or PNG)
- **FR-010**: System MUST generate thumbnails at a consistent maximum size (150x150 pixels) using fit-within scaling that preserves aspect ratio
- **FR-011**: System MUST process thumbnail generation asynchronously to avoid blocking uploads
- **FR-012**: System MUST provide a background job to generate thumbnails for existing receipts
- **FR-013**: System MUST sanitize HTML content before rendering to prevent security issues
- **FR-014**: System MUST handle password-protected PDFs gracefully with appropriate fallback
- **FR-015**: System MUST retry failed thumbnail generation up to 3 times using exponential backoff (1min, 5min, 30min) before marking as permanently failed
- **FR-016**: System MUST delete thumbnails immediately when their parent receipt is deleted (cascade delete)

### Key Entities

- **Receipt**: Extended to include thumbnail reference (URL or path to generated thumbnail image)

> **Note**: Thumbnail generation job state is tracked via Hangfire's built-in job management rather than a custom entity.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can visually identify expenses 50% faster when thumbnails are available compared to text-only list
- **SC-002**: 95% of uploaded receipts display a thumbnail within 30 seconds of upload completion
- **SC-003**: Thumbnail generation succeeds for at least 98% of valid receipt files (excluding corrupted/password-protected)
- **SC-004**: Full receipt preview loads within 2 seconds of clicking a thumbnail
- **SC-005**: Expense list with thumbnails loads within 3 seconds for up to 100 expenses
- **SC-006**: User satisfaction score for expense review workflow improves by at least 20%

## Clarifications

### Session 2026-01-08

- Q: How should thumbnails handle varying aspect ratios (receipts are typically portrait)? → A: Fit-within - Scale to fit within dimensions maintaining aspect ratio (may have transparent/white padding)
- Q: How should the system handle failed thumbnail generation retries? → A: Exponential backoff - Retry up to 3 times with increasing delays (1min, 5min, 30min), then mark as permanently failed
- Q: What should happen to thumbnails when receipts are deleted? → A: Cascade delete - Thumbnail is immediately deleted when its parent receipt is deleted

## Assumptions

- The existing receipt upload pipeline will be extended (not replaced) to add thumbnail generation
- Azure Blob Storage (ccproctemp2025) will be used to store thumbnail images alongside original receipts
- The existing receipt viewer component will be enhanced to support click-to-preview from thumbnails
- HTML receipts are already supported in the system (per feature 029-html-receipt-parsing)
- Standard thumbnail dimensions will be 150x150 pixels (adjustable via configuration)
- Thumbnails will be generated in JPEG format for optimal size/quality balance
- Background processing will use the existing Hangfire job infrastructure
