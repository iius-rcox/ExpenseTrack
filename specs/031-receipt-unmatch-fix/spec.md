# Feature Specification: Receipt Unmatch & Transaction Match Display Fix

**Feature Branch**: `031-receipt-unmatch-fix`
**Created**: 2026-01-12
**Status**: Draft
**Input**: User description: "Add unmatch functionality to receipt detail page and fix transaction detail page incorrectly showing no matched receipt"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Unmatch Receipt from Receipt Page (Priority: P1)

A user viewing a matched receipt needs the ability to unmatch it directly from the receipt detail page, rather than having to navigate to the transaction page first. This provides workflow symmetry - users can unmatch from either side of the match relationship.

**Why this priority**: Core functionality gap. The unmatch feature exists on the transaction page but not on the receipt page, creating an inconsistent user experience and forcing unnecessary navigation.

**Independent Test**: Can be fully tested by navigating to a matched receipt detail page, clicking the unmatch button, confirming the action, and verifying both the receipt and associated transaction return to unmatched status.

**Acceptance Scenarios**:

1. **Given** a user is viewing a receipt that is matched to a transaction, **When** they click the "Unmatch" button and confirm, **Then** the match is removed, the receipt shows as unmatched, and the user receives a success notification.

2. **Given** a user is viewing a receipt that is matched to a transaction, **When** they click "Unmatch" but cancel the confirmation dialog, **Then** the match remains intact and no changes occur.

3. **Given** a user is viewing a receipt that is NOT matched to any transaction, **When** they view the receipt detail page, **Then** no unmatch button is displayed (only shown for matched receipts).

4. **Given** a user has unmatched a receipt, **When** they navigate to the previously linked transaction, **Then** that transaction also shows as unmatched.

---

### User Story 2 - View Matched Transaction on Receipt Page (Priority: P1)

A user viewing a matched receipt needs to see information about the linked transaction and have a quick way to navigate to it. This mirrors the existing functionality on the transaction page which shows the linked receipt.

**Why this priority**: Essential for understanding the full context of a receipt. Users need to see what transaction a receipt is matched to without navigating away.

**Independent Test**: Can be fully tested by navigating to a matched receipt and verifying the matched transaction information is displayed with a link to view the transaction details.

**Acceptance Scenarios**:

1. **Given** a user is viewing a receipt that is matched to a transaction, **When** the page loads, **Then** a "Linked Transaction" section displays showing the transaction date, amount, description, and merchant.

2. **Given** a user is viewing a receipt matched to a transaction, **When** they click "View Transaction", **Then** they are navigated to the transaction detail page.

3. **Given** a user is viewing a receipt that is NOT matched, **When** the page loads, **Then** the "Linked Transaction" section shows "No transaction matched yet" with suggestions for next steps.

---

### User Story 3 - Fix Transaction Match Status Display (Priority: P1)

A user viewing a transaction detail page must see accurate match status information. Currently, there is a bug where the page may incorrectly show "Unmatched" even when a receipt is linked, caused by inconsistent use of `hasMatchedReceipt` boolean vs `matchedReceipt` object.

**Why this priority**: This is a data integrity display bug that causes user confusion and may lead to duplicate matching attempts.

**Independent Test**: Can be fully tested by viewing a transaction that has a matched receipt and verifying the badge shows "Matched", the receipt info section shows the linked receipt, and clicking "View Receipt" navigates correctly.

**Acceptance Scenarios**:

1. **Given** a transaction has a linked receipt (matchedReceipt object exists), **When** the user views the transaction detail page, **Then** the badge shows "Matched" and the linked receipt section displays receipt details.

2. **Given** a transaction has no linked receipt (matchedReceipt is null), **When** the user views the transaction detail page, **Then** the badge shows "Unmatched" and the linked receipt section shows "No receipt matched yet".

3. **Given** the backend returns inconsistent data (hasMatchedReceipt=false but matchedReceipt exists), **When** the transaction detail page renders, **Then** the page uses the presence of matchedReceipt object as the source of truth for display.

---

### Edge Cases

- What happens when an unmatch operation fails (network error)?
  - Display error toast with retry suggestion, match remains intact
- What happens if the linked transaction/receipt was deleted?
  - Show "Linked item no longer exists" with option to clear the stale match
- What happens during concurrent unmatch from both transaction and receipt pages?
  - First request succeeds, second fails gracefully with "Already unmatched" message
- What happens if unmatch is clicked while another operation is in progress?
  - Unmatch button is disabled during pending operations

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Receipt detail page MUST display a "Linked Transaction" section when the receipt is matched to a transaction
- **FR-002**: Receipt detail page MUST show transaction date, amount, description, merchant, and match confidence in the linked section
- **FR-003**: Receipt detail page MUST provide a "View Transaction" button that navigates to the linked transaction's detail page
- **FR-004**: Receipt detail page MUST display an "Unmatch" button when a receipt is matched to a transaction
- **FR-005**: System MUST show a confirmation dialog before executing the unmatch operation
- **FR-006**: System MUST display a success notification when unmatch completes successfully
- **FR-007**: System MUST display an error notification if unmatch fails, with the match remaining intact
- **FR-008**: Receipt detail page MUST show "No transaction matched yet" with helpful guidance when unmatched
- **FR-009**: Transaction detail page MUST use the presence of `matchedReceipt` object (not `hasMatchedReceipt` boolean) as the source of truth for match status display
- **FR-010**: System MUST invalidate and refresh both receipt and transaction caches after unmatch operation
- **FR-011**: Unmatch button MUST be disabled while any mutation operation is pending
- **FR-012**: Receipt detail page MUST display match confidence as a percentage badge

### Key Entities *(include if feature involves data)*

- **Receipt**: Document uploaded by user that may be matched to a transaction. Key attributes: id, vendor, date, amount, status, matchedTransaction (new display field)
- **Transaction**: Financial transaction imported from statements. Key attributes: id, description, amount, date, matchedReceipt, hasMatchedReceipt
- **Match**: Link between a receipt and transaction. Key attributes: matchId, receiptId, transactionId, status, confidenceScore

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can unmatch a receipt from the receipt detail page in under 5 seconds (same efficiency as transaction page)
- **SC-002**: 100% of matched receipts display their linked transaction information correctly
- **SC-003**: 100% of matched transactions display their linked receipt information correctly (bug fix verification)
- **SC-004**: Zero instances of match status inconsistency between badge display and linked item section
- **SC-005**: Users can navigate between linked receipts and transactions in a single click from either detail page

## Assumptions

- The existing `useUnmatch` hook and `/matching/{matchId}/unmatch` API endpoint work correctly and can be reused
- The receipt detail API response can be extended to include matched transaction information (or a separate query can fetch it)
- The match confidence score is available in the match data and can be displayed on both receipt and transaction detail pages
- Users have permission to unmatch any matches they can view (no additional authorization required)
