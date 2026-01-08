# Feature Specification: HTML Receipt Parsing

**Feature Branch**: `029-html-receipt-parsing`
**Created**: 2026-01-08
**Status**: Draft
**Input**: User description: "add the ability to parse and extract receipt data from html receipts"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Upload HTML Receipt Email (Priority: P1)

A user receives a receipt via email (e.g., from Amazon, Uber, DoorDash, airline booking confirmations) and wants to upload it directly to ExpenseFlow without having to screenshot or print-to-PDF first. The user saves the email as an HTML file or uses the "Forward as attachment" option, then uploads the .html or .htm file to ExpenseFlow.

**Why this priority**: This is the core value proposition - enabling a new input format that many digital receipts naturally come in. Without this, users must convert HTML to PDF/image first, adding friction.

**Independent Test**: Can be fully tested by uploading a sample HTML receipt file and verifying the extracted vendor, date, and amount appear correctly in the receipt detail view.

**Acceptance Scenarios**:

1. **Given** a user has an HTML receipt file saved from an email, **When** they upload the .html file via the receipt upload dialog, **Then** the system accepts the file and begins processing.

2. **Given** an HTML receipt is uploaded, **When** processing completes, **Then** the receipt detail view displays the extracted vendor name, transaction date, and total amount.

3. **Given** an HTML receipt with multiple items, **When** processing completes, **Then** line items are extracted and displayed in the receipt details.

---

### User Story 2 - HTML Receipt Thumbnail Generation (Priority: P2)

After uploading an HTML receipt, the user can see a visual preview/thumbnail in the receipts list, similar to how image and PDF receipts display thumbnails.

**Why this priority**: Visual previews help users quickly identify receipts in the list. While not essential for functionality, it significantly improves the user experience.

**Independent Test**: Can be tested by uploading an HTML receipt and verifying a visual thumbnail appears in the receipts list view.

**Acceptance Scenarios**:

1. **Given** an HTML receipt has been processed, **When** the user views the receipts list, **Then** a visual thumbnail representation of the receipt is displayed.

2. **Given** an HTML receipt thumbnail is displayed, **When** the user clicks to view the receipt, **Then** the full HTML content is rendered correctly.

---

### User Story 3 - HTML Receipt Viewing (Priority: P2)

When viewing an HTML receipt's details, the user can see the original HTML content rendered properly, preserving the original formatting, logos, and layout from the email.

**Why this priority**: Users need to verify the extracted data against the original receipt content. Proper rendering builds trust in the extraction accuracy.

**Independent Test**: Can be tested by uploading an HTML receipt and viewing it in the receipt detail view, confirming the layout matches the original email appearance.

**Acceptance Scenarios**:

1. **Given** an HTML receipt has been uploaded, **When** the user views the receipt detail page, **Then** the HTML content is rendered with preserved formatting and styling.

2. **Given** an HTML receipt contains inline images or logos, **When** viewing the receipt, **Then** external images are displayed if they're still accessible, or placeholder indicators are shown if unavailable.

---

### Edge Cases

- What happens when the HTML file is malformed or corrupted? System displays an error and marks the receipt for manual review.
- What happens when the HTML contains no recognizable receipt data? System marks the receipt as "Review Required" with low confidence scores.
- What happens when the HTML references external stylesheets or scripts? System renders with inline styles only; external resources are not loaded for security.
- What happens when the HTML is extremely large (e.g., full email thread)? System extracts only the relevant receipt portion, up to a reasonable size limit.
- What happens when the HTML contains multiple receipts? System extracts data from the first/most prominent receipt and flags for potential manual review.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST accept .html and .htm file uploads in the receipt upload flow
- **FR-002**: System MUST extract vendor/merchant name from HTML receipt content
- **FR-003**: System MUST extract transaction date from HTML receipt content
- **FR-004**: System MUST extract total amount from HTML receipt content
- **FR-005**: System MUST extract line items when present in the HTML receipt
- **FR-006**: System MUST generate a visual thumbnail for HTML receipts displayed in the receipts list
- **FR-007**: System MUST render HTML receipts in the receipt detail view with original formatting preserved
- **FR-008**: System MUST sanitize HTML content to prevent XSS and other security vulnerabilities before rendering
- **FR-009**: System MUST provide confidence scores for extracted fields, consistent with existing image/PDF receipts
- **FR-010**: System MUST mark HTML receipts for review when extraction confidence is below the configured threshold
- **FR-011**: System MUST support common email receipt formats (Amazon, Uber, Lyft, DoorDash, airline confirmations, hotel bookings)
- **FR-012**: System MUST handle HTML files up to 5MB in size
- **FR-013**: System MUST block execution of scripts, external resource loading, and form submissions within rendered HTML for security
- **FR-014**: System MUST log extraction metrics (confidence scores, processing time, field counts) and retain raw HTML content for receipts with failed or low-confidence extractions to support debugging and re-processing

### Key Entities

- **Receipt**: Extended to support `text/html` content type; stores the raw HTML content or reference to stored HTML file
- **ReceiptExtractionResult**: Existing entity used to capture extracted vendor, date, amount, and line items from HTML parsing

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can upload an HTML receipt and see extracted data within 30 seconds of upload completion
- **SC-002**: HTML receipt extraction achieves at least 80% accuracy for vendor name, date, and amount on common email receipt formats (Amazon, Uber, Lyft, major airlines)
- **SC-003**: 90% of users can successfully upload and view an HTML receipt on their first attempt without errors
- **SC-004**: HTML receipts display thumbnails that are visually recognizable and load within 2 seconds in the receipts list
- **SC-005**: Zero security vulnerabilities related to HTML rendering (no XSS, no external script execution, no data exfiltration)

## Clarifications

### Session 2026-01-08

- Q: How should receipt data be extracted from HTML content? → A: AI-based extraction (send HTML text to LLM for interpretation)
- Q: How should extraction failures be logged for operational monitoring? → A: Log metrics + store raw HTML for failed/low-confidence extractions for later analysis

## Assumptions

- HTML receipts will primarily come from email clients (saved as .html files) or forwarded email attachments
- The existing receipt processing pipeline (upload → process → ready/review) will be extended rather than replaced
- HTML data extraction will use AI/LLM-based interpretation of the HTML text content (consistent with existing Azure OpenAI integration)
- Common receipt email templates from major vendors (Amazon, Uber, airlines, etc.) follow recognizable patterns
- Users expect similar extraction accuracy from HTML receipts as they get from image/PDF receipts
