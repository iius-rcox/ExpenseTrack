# Feature Specification: Transaction Group Matching

**Feature Branch**: `028-group-matching`
**Created**: 2026-01-07
**Status**: Draft
**Input**: User description: "Implement group matching in the matching engine so receipts can match against transaction group totals"

## Overview

Currently, ExpenseFlow's receipt matching engine only considers individual transactions when proposing matches. When users group multiple transactions together (e.g., 3 Twilio charges that correspond to a single receipt), the matching engine cannot match a receipt against the group's combined total.

This feature extends the matching engine to treat transaction groups as first-class match candidates, using the group's combined amount and display date for matching calculations.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Auto-Match Receipt to Transaction Group (Priority: P1)

A user has grouped 3 related transactions (e.g., multiple charges from the same vendor that appear on one receipt). When the auto-matching engine runs, it considers the group's combined total alongside individual transactions and proposes a match when the receipt amount aligns with the group total.

**Why this priority**: This is the core functionality - without auto-matching to groups, users must manually match every multi-transaction receipt, defeating the purpose of the matching engine.

**Independent Test**: Upload a receipt for $50.00, create a transaction group with three transactions totaling $50.00, run auto-match, and verify the receipt is proposed as a match to the group.

**Acceptance Scenarios**:

1. **Given** a receipt with extracted amount of $50.00, **When** auto-matching runs and a transaction group exists with CombinedAmount of $50.00, **Then** the system proposes a match between the receipt and the group with an appropriate confidence score.

2. **Given** a receipt with extracted amount of $50.00, **When** auto-matching runs and both an individual transaction of $50.00 and a group with CombinedAmount of $50.00 exist within the date range, **Then** the system evaluates both as candidates and proposes the higher-scoring match.

3. **Given** a receipt with extracted amount of $50.00 and date of January 5, **When** auto-matching runs and a transaction group with CombinedAmount of $50.00 and DisplayDate of January 5 exists, **Then** the system scores the date match using the group's DisplayDate (not individual transaction dates).

---

### User Story 2 - Manual Match Receipt to Group (Priority: P2)

A user wants to manually match a receipt to a transaction group when auto-matching didn't propose the correct match or when they want to override an existing proposal.

**Why this priority**: Users need a fallback when auto-matching fails or makes incorrect proposals. Manual matching is essential for edge cases but secondary to automatic matching.

**Independent Test**: Create a transaction group, navigate to an unmatched receipt, select the group from available match candidates, confirm the match, and verify both receipt and group show as matched.

**Acceptance Scenarios**:

1. **Given** an unmatched receipt, **When** the user views match candidates, **Then** transaction groups within the date range appear as selectable options alongside individual transactions.

2. **Given** an unmatched receipt displayed with match candidates, **When** the user selects a transaction group and confirms, **Then** the match is recorded and both the receipt and group reflect matched status.

3. **Given** a receipt matched to a transaction group, **When** the user views the receipt details, **Then** they see the group name and combined amount (not individual transactions).

---

### User Story 3 - Exclude Grouped Transactions from Individual Matching (Priority: P2)

When transactions are part of a group, they should not be considered as individual match candidates. This prevents confusing scenarios where a receipt might be proposed to match both a group AND one of its member transactions.

**Why this priority**: This is a data integrity concern that prevents duplicate/conflicting matches. It's essential for correct behavior but is more about preventing errors than enabling new functionality.

**Independent Test**: Create a group with 3 transactions, run auto-match on a receipt that matches one individual transaction's amount, and verify that transaction is NOT proposed as a match candidate (only the group would be).

**Acceptance Scenarios**:

1. **Given** transactions A, B, and C are grouped, **When** auto-matching runs, **Then** A, B, and C are NOT evaluated as individual match candidates.

2. **Given** transaction A (amount $20) is part of a group, and a receipt for $20 exists, **When** auto-matching runs, **Then** the receipt is NOT proposed to match transaction A individually.

3. **Given** a user is manually matching a receipt, **When** viewing available transactions, **Then** transactions that belong to groups are not shown as individual options.

---

### User Story 4 - Unmatch and Re-Match Groups (Priority: P3)

A user needs to unmatch a receipt from a transaction group (if matched incorrectly) and potentially re-match to a different group or individual transaction.

**Why this priority**: This is an error correction flow - important for usability but not part of the primary matching workflow.

**Independent Test**: Create a receipt matched to a group, unmatch it, verify both return to unmatched status, then match the receipt to a different transaction.

**Acceptance Scenarios**:

1. **Given** a receipt matched to a transaction group, **When** the user unmatches them, **Then** both the receipt and group return to unmatched status.

2. **Given** a receipt that was just unmatched from a group, **When** the user views match candidates, **Then** the original group appears again as a candidate (if still within date range).

---

### Edge Cases

- What happens when a group is matched to a receipt and then a transaction is removed from the group?
  - The match remains, but the group's combined amount updates. A warning toast notification should be shown if the new total no longer aligns with the receipt amount (differs by more than the ±$1.00 tolerance). The warning should display: "Group total changed to $X.XX - receipt amount is $Y.YY. Consider re-matching."

- What happens when a group is matched to a receipt and then the group is deleted?
  - The receipt returns to unmatched status. The individual transactions remain and become available for individual matching.

- What happens when all transactions in a group are from different vendors?
  - Vendor matching uses the group's display name (derived from primary transaction) for scoring, not individual vendor names.

- What happens when the receipt amount is between the highest individual transaction amount and the group total?
  - Both candidates are evaluated and scored independently. The system may propose the group if it's within tolerance, or may propose no match if neither meets the threshold.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST include transaction groups as match candidates during auto-matching, evaluating them alongside individual transactions.

- **FR-002**: System MUST use the group's CombinedAmount (sum of member transaction amounts) for amount scoring during matching.

- **FR-003**: System MUST use the group's DisplayDate for date scoring during matching.

- **FR-004**: System MUST exclude transactions that belong to a group from being considered as individual match candidates.

- **FR-005**: System MUST allow users to manually match a receipt to a transaction group.

- **FR-006**: System MUST allow users to unmatch a receipt from a transaction group.

- **FR-007**: System MUST update group match status when a match is confirmed, proposed, or removed.

- **FR-008**: System MUST use the same scoring algorithm (amount, date, vendor) for groups as for individual transactions.

- **FR-009**: System MUST display transaction groups as distinct match candidates in the UI, showing group name and combined amount.

- **FR-010**: When a group is deleted while matched to a receipt, the system MUST return the receipt to unmatched status.

- **FR-011**: When a transaction is removed from a matched group, the system MUST display a warning if the updated group total differs from the matched receipt by more than the amount tolerance.

### Key Entities

- **TransactionGroup**: Represents a user-created collection of related transactions. Key attributes: CombinedAmount (sum of member amounts), DisplayDate (for matching), MatchStatus (Unmatched/Proposed/Matched), MatchedReceiptId (foreign key to matched receipt).

- **ReceiptTransactionMatch**: Junction entity linking receipts to either individual transactions OR transaction groups. Contains: ReceiptId, TransactionId (nullable), TransactionGroupId (nullable), ConfidenceScore, AmountScore, DateScore, VendorScore, Status (Proposed/Confirmed/Rejected).

- **Receipt**: Document image with extracted metadata. Relevant attributes: AmountExtracted, ReceiptDate, NormalizedMerchant, MatchStatus.

- **Transaction**: Individual financial transaction. Relevant attributes: Amount, TransactionDate, GroupId (nullable - links to parent group when grouped).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Receipts matching transaction group totals are correctly proposed as matches with the same accuracy rate as individual transaction matches (existing benchmark: greater than 85% of correct matches proposed).

- **SC-002**: Users can complete a manual group match in under 30 seconds (same as individual transaction matching).

- **SC-003**: Zero duplicate match proposals occur (a receipt should never be proposed to both a group AND one of its member transactions simultaneously).

- **SC-004**: Match proposal processing time remains under 2 seconds per receipt when groups are included as candidates.

- **SC-005**: Group matches appear correctly in expense reports and analytics (no double-counting of grouped transactions).

## Assumptions

- The existing scoring algorithm (amount, date, vendor weights) is appropriate for group matching without modification to weights.
- The amount tolerance used for individual transactions is also appropriate for group totals.
- Groups have a meaningful "vendor" for scoring purposes - this will use the group's name (typically derived from the primary merchant).
- The UI already distinguishes groups from individual transactions in the transaction list; extending this to match candidate display is straightforward.
- The database schema already supports group matching (TransactionGroupId column exists in ReceiptTransactionMatch table).

## Out of Scope

- Automatic group creation based on matching patterns (groups must be manually created by users)
- Splitting a single receipt across multiple transactions or groups
- Partial matching (matching a receipt to a subset of a group's transactions)
- Changing the scoring weights for groups vs. individual transactions
