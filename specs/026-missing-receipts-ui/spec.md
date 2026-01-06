# Feature Specification: Missing Receipts UI

**Feature Branch**: `026-missing-receipts-ui`
**Created**: 2026-01-05
**Status**: Draft
**Input**: User description: "Create a UI for missing receipts which are transactions believed to be reimbursable without a matching receipt. It should also have a way to store the URL for fetching that receipt optionally"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Missing Receipts List (Priority: P1)

As an expense report user, I want to see a list of all transactions that are flagged as reimbursable but don't have matching receipts, so I can identify which receipts I still need to obtain or upload.

**Why this priority**: This is the core functionality - users cannot take action on missing receipts if they cannot see them. This forms the foundation for all other features.

**Independent Test**: Can be fully tested by navigating to the Missing Receipts page and verifying that all predicted reimbursable transactions without matched receipts appear in a clear, organized list.

**Acceptance Scenarios**:

1. **Given** a user has 5 transactions predicted as reimbursable without receipts, **When** they navigate to the Missing Receipts page, **Then** they see all 5 transactions listed with date, vendor, and amount
2. **Given** a user has no missing receipts, **When** they navigate to the Missing Receipts page, **Then** they see a helpful empty state indicating all receipts are accounted for
3. **Given** a user has missing receipts spanning multiple months, **When** they view the list, **Then** transactions are grouped or sortable by date for easy organization

---

### User Story 2 - Add Receipt Retrieval URL (Priority: P2)

As an expense report user, I want to store a URL where I can retrieve a missing receipt (such as an airline confirmation page or hotel booking portal), so I can quickly access the source when I'm ready to download and upload the receipt.

**Why this priority**: Many receipts are available online but users forget where to find them. Storing the URL provides immediate value by helping users track receipt sources.

**Independent Test**: Can be fully tested by selecting a missing receipt, adding a URL, saving it, and verifying the URL is persisted and accessible on subsequent visits.

**Acceptance Scenarios**:

1. **Given** a user views a missing receipt entry, **When** they click "Add Receipt URL", **Then** they can enter and save any text as the URL
2. **Given** a user has saved a URL for a missing receipt, **When** they return to view that entry, **Then** they see the stored URL displayed with a clickable link
3. **Given** a user saves an empty URL field, **When** they save, **Then** the URL field is cleared (null)

---

### User Story 3 - Upload Receipt from Missing Receipts View (Priority: P2)

As an expense report user, I want to upload a receipt directly from the missing receipts list, so I can resolve missing receipt items without navigating away to the main receipts page.

**Why this priority**: Streamlines the workflow by allowing users to resolve missing receipts in-place, reducing friction and improving task completion rates.

**Independent Test**: Can be fully tested by clicking upload on a missing receipt, selecting a file, and verifying the receipt is uploaded and the transaction is removed from the missing receipts list.

**Acceptance Scenarios**:

1. **Given** a user views a missing receipt, **When** they click "Upload Receipt" and select a valid image file, **Then** the receipt is uploaded and processing begins
2. **Given** a user successfully uploads a receipt for a missing item, **When** the receipt is matched to the transaction, **Then** the item is removed from the missing receipts list
3. **Given** a user attempts to upload an invalid file type, **When** they select the file, **Then** they see an error message and the upload is rejected

---

### User Story 4 - Navigate to Saved Receipt URL (Priority: P3)

As an expense report user, I want to click on a saved receipt URL to open it in a new tab, so I can quickly navigate to the vendor's website to download the receipt.

**Why this priority**: Enhances usability of the URL storage feature, but depends on URL storage being implemented first.

**Independent Test**: Can be fully tested by clicking a saved URL link and verifying it opens in a new browser tab.

**Acceptance Scenarios**:

1. **Given** a missing receipt has a saved URL, **When** the user clicks the URL link, **Then** the URL opens in a new browser tab
2. **Given** a missing receipt has no saved URL, **When** the user views the entry, **Then** they see an option to add a URL instead of a link

---

### User Story 5 - Dismiss/Ignore Missing Receipt (Priority: P3)

As an expense report user, I want to mark a transaction as "not reimbursable" or dismiss it from the missing receipts list, so I can keep the list focused on items I actually need to address.

**Why this priority**: Provides list management capability for edge cases where transactions are incorrectly flagged as reimbursable.

**Independent Test**: Can be fully tested by dismissing a missing receipt and verifying it no longer appears in the list, with the action reversible if needed.

**Acceptance Scenarios**:

1. **Given** a user views a missing receipt that is not actually reimbursable, **When** they click "Dismiss", **Then** the item is removed from the missing receipts list
2. **Given** a user has dismissed a receipt, **When** they want to restore it, **Then** they can access a "Dismissed" filter to view and restore dismissed items

---

### Edge Cases

- What happens when a transaction is matched to a receipt after being added to missing receipts? (Auto-removed from list)
- How does the system handle very long URLs that may not display well? (Truncate with tooltip showing full URL)
- What happens if a user uploads a receipt that doesn't match the transaction amount/date? (Show warning but allow upload)
- How are duplicate URLs handled if the same URL is used for multiple transactions? (Allowed - same vendor portal may have multiple receipts)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display a "Missing Receipts" summary widget on the Matching page (`/matching`) with a link to a dedicated full list page (`/missing-receipts`)
- **FR-001a**: The widget MUST display: total missing receipt count, up to 3 most recent missing receipts (date, vendor, amount), and a quick upload button for each item
- **FR-002**: System MUST show all transactions that are predicted reimbursable but have no matched receipt
- **FR-003**: Each missing receipt entry MUST display: transaction date, vendor/description, amount, and days since transaction
- **FR-004**: Users MUST be able to add, edit, and remove a receipt retrieval URL for each missing receipt entry
- **FR-005**: Receipt URLs are stored as plain text strings without validation (user responsibility to ensure correctness)
- **FR-006**: Users MUST be able to upload a receipt directly from the missing receipts view
- **FR-007**: When a receipt is successfully matched to a transaction, the system MUST automatically remove it from the missing receipts list
- **FR-008**: Users MUST be able to dismiss/ignore transactions that are incorrectly flagged
- **FR-009**: System MUST provide a way to view and restore dismissed items
- **FR-010**: Missing receipts list MUST support sorting by date, amount, or vendor name
- **FR-011**: Saved receipt URLs MUST open in a new browser tab when clicked
- **FR-012**: System MUST show an empty state with helpful guidance when no missing receipts exist
- **FR-013**: Full list page MUST paginate results at 25 items per page with standard page navigation

### Key Entities

- **MissingReceiptEntry** *(view model, not persisted)*: Computed at query time from Transactions where reimbursable (user override takes precedence over AI prediction) AND no matched receipt exists AND not dismissed. Contains: transaction reference, optional receipt URL, dismissed status
- **Transaction** *(extended)*: Existing entity extended with two new nullable fields:
  - `ReceiptUrl`: Optional URL where receipt can be retrieved (e.g., airline portal, hotel booking confirmation)
  - `ReceiptDismissed`: Boolean flag indicating transaction should be excluded from missing receipts list

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can view all missing receipts within 2 seconds of page load
- **SC-002**: Users can add a receipt URL in under 30 seconds
- **SC-003**: Users can upload a receipt from the missing receipts view in ≤3 clicks (click upload → select file → confirm)
- **SC-004**: 80% of users who visit the missing receipts page take action on at least one item (add URL, upload, or dismiss)
- **SC-005**: The missing receipts count is reduced by 50% within the first week of feature launch for active users
- **SC-006**: Zero missing receipts are incorrectly removed from the list (only removed when actually matched or explicitly dismissed)

## Constraints & Tradeoffs

- **No URL validation**: Receipt URLs are stored as plain strings without format or reachability validation. This tradeoff accepts potential user error in exchange for simplicity and avoiding false negatives (many receipt portals require authentication, causing valid URLs to fail reachability checks).
- **Computed view over materialized table**: Missing receipts are computed at query time rather than synced to a dedicated table. This avoids sync complexity but means the list freshness depends on query performance.

## Assumptions

- The system already has expense prediction functionality that identifies reimbursable transactions (Feature 023)
- Receipt upload and matching functionality already exists in the main receipts flow
- Users are authenticated and can only see their own missing receipts
- The existing Transaction entity can be extended or linked to store receipt URLs
- Receipt URLs are plain text storage - no verification that the URL actually contains a receipt

## Clarifications

### Session 2026-01-05

- Q: Should missing receipts be a computed view or materialized entity? → A: Computed view - Missing receipts derived at query time from Transactions + Predictions; URL stored on Transaction entity
- Q: Where should Missing Receipts UI be placed in navigation? → A: Dashboard widget on Matching page (/matching) with link to full list page
- Q: What should the widget display and what quick actions? → A: Count + top 3 most recent items with quick upload button per item
- Q: How to determine reimbursability when user override and AI prediction differ? → A: User override takes precedence; fall back to AI prediction if no override exists
- Q: What scale should we design for? → A: Medium scale (20-100 per user), paginated list at 25 per page with standard indexed queries
- Q: What URL validation approach for receipt URLs? → A: No validation - accept any string; user responsibility to ensure correctness
