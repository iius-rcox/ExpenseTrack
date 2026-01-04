# Feature Specification: Extraction Editor with Model Training

**Feature Branch**: `024-extraction-editor-training`
**Created**: 2026-01-03
**Status**: Draft
**Input**: User description: "Editor UI that allows me to edit extracted information and train the model with my changes"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Edit Extracted Receipt Fields (Priority: P1)

A user views a receipt with AI-extracted data and notices the merchant name was incorrectly extracted as "AMZN*Marketplace" when it should be "Amazon Marketplace". The user clicks on the merchant field, types the corrected value, and saves the change. The system updates the receipt and marks the field as user-corrected.

**Why this priority**: This is the core functionality - without editing capabilities, users cannot correct extraction errors, which directly impacts expense report accuracy and user trust in the system.

**Independent Test**: Can be fully tested by uploading a receipt, viewing extracted fields, editing one or more fields, and verifying the changes are saved correctly.

**Acceptance Scenarios**:

1. **Given** a receipt with extracted fields displayed, **When** the user clicks the edit icon on a field, **Then** the field becomes editable with the current value pre-populated
2. **Given** a field in edit mode, **When** the user enters a new value and clicks save, **Then** the field displays the updated value and shows a visual indicator that it was manually edited
3. **Given** a field in edit mode, **When** the user presses Escape or clicks cancel, **Then** the edit is discarded and the original value is restored
4. **Given** an edited field, **When** the user clicks undo, **Then** the field reverts to its original AI-extracted value
5. **Given** multiple edited fields, **When** the user clicks "Save All", **Then** all changes are persisted simultaneously

---

### User Story 2 - View Extraction Confidence Scores (Priority: P1)

A user reviews a receipt and wants to know which extracted fields might need manual review. The system displays confidence scores for each extracted field, using color-coded indicators (green for high confidence, yellow for medium, red for low). The user focuses their attention on low-confidence fields first.

**Why this priority**: Confidence scores guide users to fields most likely needing correction, making the editing workflow efficient and reducing time spent reviewing accurate extractions.

**Independent Test**: Can be fully tested by viewing a processed receipt and verifying each field displays an appropriate confidence indicator with correct visual styling.

**Acceptance Scenarios**:

1. **Given** a receipt with extracted fields, **When** displayed to the user, **Then** each field shows a confidence score indicator
2. **Given** a field with confidence ≥90%, **When** displayed, **Then** the indicator shows green/high confidence styling
3. **Given** a field with confidence 70-89%, **When** displayed, **Then** the indicator shows yellow/medium confidence styling
4. **Given** a field with confidence <70%, **When** displayed, **Then** the indicator shows red/low confidence styling

---

### User Story 3 - Submit Corrections as Training Feedback (Priority: P2)

After editing one or more fields on a receipt, the user saves their changes. The system captures these corrections as training feedback, recording the original AI extraction, the user's correction, and relevant context. This feedback is stored for future model improvement cycles.

**Why this priority**: Training feedback enables continuous improvement of the extraction model, reducing future manual corrections. However, the system functions without this - editing alone provides immediate value.

**Independent Test**: Can be fully tested by editing a receipt field, saving changes, and verifying a training feedback record is created with the original and corrected values.

**Acceptance Scenarios**:

1. **Given** a user edits an extracted field and saves, **When** the save completes, **Then** a training feedback record is created linking the receipt, field, original value, and corrected value
2. **Given** multiple fields are edited on one receipt, **When** saved, **Then** separate feedback records are created for each corrected field
3. **Given** a user undoes an edit and then saves, **When** the feedback is recorded, **Then** only the final corrected values (not intermediate changes) are submitted as training data
4. **Given** a field is edited to the same value as the original, **When** saved, **Then** no training feedback is recorded for that field (no-op correction)

---

### User Story 4 - Side-by-Side Document and Fields View (Priority: P2)

A user needs to verify extracted data against the original receipt image. The system displays the receipt document (image or PDF) alongside the extracted fields panel, allowing the user to view both simultaneously. When viewing on smaller screens, the layout adapts responsively.

**Why this priority**: Visual verification against the source document is essential for accurate corrections, but users can still edit fields without this specific layout enhancement.

**Independent Test**: Can be fully tested by opening a receipt detail page and verifying the document and extracted fields display side-by-side on desktop, with appropriate responsive behavior on mobile.

**Acceptance Scenarios**:

1. **Given** a receipt detail page on a desktop viewport, **When** displayed, **Then** the document viewer shows on the left and extracted fields panel on the right
2. **Given** a PDF receipt, **When** displayed in the document viewer, **Then** the PDF renders correctly with zoom and scroll capabilities
3. **Given** an image receipt, **When** displayed, **Then** the image renders with zoom, pan, and rotation controls
4. **Given** a mobile viewport, **When** the page is displayed, **Then** the layout stacks vertically with document above extracted fields

---

### User Story 5 - View Training Feedback History (Priority: P3)

An administrator or power user wants to review the corrections that have been submitted across all receipts. The system provides a view showing recent training feedback submissions, including the original extraction, user correction, submission timestamp, and user who made the correction.

**Why this priority**: Feedback visibility enables quality assurance and helps identify systematic extraction issues, but is not required for core editing and training functionality.

**Independent Test**: Can be fully tested by navigating to the feedback history view and verifying it displays correction records with appropriate filtering and sorting.

**Acceptance Scenarios**:

1. **Given** the feedback history view, **When** loaded, **Then** recent feedback submissions are displayed in reverse chronological order
2. **Given** the feedback list, **When** a user filters by field type (e.g., "merchant"), **Then** only feedback for that field type is shown
3. **Given** multiple users have submitted feedback, **When** an admin views the history, **Then** corrections from all users are visible with attribution

---

### Edge Cases

- What happens when the user edits a field to an invalid value (e.g., negative amount, future date)?
  - System validates input and displays inline error message, preventing save until corrected
- How does the system handle concurrent edits to the same receipt by different users?
  - Last-write-wins with optimistic concurrency; user is notified if their base data was stale
- What happens when editing a receipt that is currently being reprocessed?
  - System prevents editing during processing; fields display as read-only with processing indicator
- How are bulk edits handled (multiple fields at once)?
  - All edits are collected locally and submitted as a batch on "Save All", creating individual feedback records per field
- What if the receipt image/PDF fails to load while editing?
  - Extracted fields remain editable; user is shown error message for document viewer with retry option

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow users to edit any AI-extracted field on a receipt (merchant, amount, date, tax, currency, and existing line items; adding/deleting line items is out of scope)
- **FR-002**: System MUST display confidence scores for each extracted field using color-coded visual indicators
- **FR-003**: System MUST support inline editing with immediate visual feedback (edit mode, validation errors)
- **FR-004**: System MUST validate field edits based on field type (numeric for amounts, valid date format, non-empty for required fields)
- **FR-005**: System MUST provide undo capability for individual field edits before saving
- **FR-006**: System MUST save all pending edits as a batch operation ("Save All" action)
- **FR-007**: System MUST persist user corrections to the receipt record immediately upon save
- **FR-008**: System MUST record training feedback for each corrected field, capturing original extraction and user correction
- **FR-009**: System MUST display receipt documents (images and PDFs) alongside extracted fields in a side-by-side layout
- **FR-010**: System MUST indicate which fields have been manually edited vs. AI-extracted
- **FR-011**: System MUST prevent editing of receipts currently in processing state
- **FR-012**: System MUST handle optimistic concurrency for simultaneous edits with appropriate user notification
- **FR-013**: System MUST allow users to edit any receipt visible to them (editing not restricted to uploader)

### Key Entities

- **Receipt**: The uploaded document with extracted fields (existing entity, extended with edit tracking)
- **ExtractedField** *(UI component)*: React component representing an individual extracted field with value, confidence score, and edit state
- **ExtractionCorrection**: Training feedback record linking receipt, field, original value, corrected value, timestamp, and user; retained indefinitely as permanent training corpus

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete editing a receipt field and save changes in under 10 seconds
- **SC-002**: 95% of users successfully edit a low-confidence field on first attempt
- **SC-003**: Training feedback is captured for 100% of user corrections (no silent data loss)
- **SC-004**: Confidence indicators accurately reflect extraction quality (validated against manual review sample)
- **SC-005**: Side-by-side view loads within 2 seconds including document rendering
- **SC-006**: Mobile users can complete field edits as effectively as desktop users (same task completion rate)
- **SC-007**: System handles 100 concurrent editing sessions without degradation

## Clarifications

### Session 2026-01-03

- Q: Can users edit only receipts they uploaded, or can they edit any visible receipt? → A: Any visible receipt (no restriction)
- Q: How long should correction/training feedback records be retained? → A: Indefinitely (permanent training corpus)
- Q: What level of line item editing is required? → A: Edit existing line items only (no add/delete)

## Assumptions

- The existing receipt processing pipeline already extracts field confidence scores
- Users can edit any receipt visible to them (no ownership restriction on editing)
- Azure Form Recognizer (or equivalent service) provides per-field confidence scores in extraction results
- Training feedback will be used in periodic model fine-tuning cycles (not real-time learning)
- The existing `ReceiptIntelligencePanel` and `ExtractedField` components provide a foundation for the UI
