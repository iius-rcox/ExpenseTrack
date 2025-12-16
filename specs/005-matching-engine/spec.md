# Feature Specification: Matching Engine

**Feature Branch**: `005-matching-engine`
**Created**: 2025-12-15
**Status**: Draft
**Sprint**: 5 (Weeks 9-10)
**Phase**: Intelligence
**Input**: Sprint 5: Matching Engine - Automatically match receipts to transactions using vendor aliases with learning from confirmations

## Overview

The Matching Engine automatically pairs uploaded receipts with imported credit card transactions. It uses vendor aliases (pattern matching) and fuzzy matching to find likely matches, calculates confidence scores, and learns from user confirmations to improve future accuracy. This is a Tier 1 (cache-only) operation with no AI API calls required.

### User Stories Addressed
- US-005: Automatic Receipt Matching
- US-006: Vendor Aliasing

## Clarifications

### Session 2025-12-15

- Q: What states should a proposed match transition through? → A: Proposed → Confirmed/Rejected (both states retained for audit)
- Q: How should concurrent match conflicts be handled? → A: First-write-wins (optimistic locking, second user sees error and retries)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Auto-Match Receipts to Transactions (Priority: P1)

As an expense report user, I want the system to automatically match my uploaded receipts to my credit card transactions so that I don't have to manually search for and link each receipt.

**Why this priority**: This is the core value of the matching engine - reducing manual effort by 85%+ through automatic receipt-transaction pairing.

**Independent Test**: Upload 10 receipts with extracted vendor/date/amount data, import a statement with matching transactions, run auto-match, and verify that high-confidence matches are proposed.

**Acceptance Scenarios**:

1. **Given** a receipt with extracted vendor "Delta Airlines", date "2025-01-10", and amount $425.00, **When** auto-match runs against transactions containing a Delta charge of $425.00 on 2025-01-10, **Then** the system proposes a match with confidence score >= 90%.

2. **Given** a receipt with amount $425.00 and a transaction with amount $425.05, **When** auto-match runs, **Then** the system still proposes a match (within $0.10 tolerance) with appropriate confidence adjustment.

3. **Given** a receipt dated 2025-01-10 and a transaction dated 2025-01-12, **When** auto-match runs, **Then** the system proposes a match (within 3-day tolerance) with reduced confidence for the date component.

4. **Given** multiple potential transaction matches for a single receipt, **When** auto-match runs, **Then** the system returns the single best match (highest confidence) or flags as ambiguous if multiple scores are within 5%.

---

### User Story 2 - Review and Confirm Proposed Matches (Priority: P1)

As an expense report user, I want to review the system's proposed matches and confirm or reject them so that I maintain control over my expense report accuracy.

**Why this priority**: User confirmation is essential for accuracy and creates the feedback loop that improves future matching.

**Independent Test**: Display a list of proposed matches, allow user to confirm one match, verify the receipt and transaction are linked, and verify the vendor alias is created/updated.

**Acceptance Scenarios**:

1. **Given** a proposed match with 85% confidence, **When** the user views the match review screen, **Then** they see the receipt thumbnail, transaction details, confidence score, and match reasoning (amount difference, date difference, vendor match type).

2. **Given** a proposed match, **When** the user confirms the match, **Then** the receipt is linked to the transaction, both are marked as "Matched", and a vendor alias is created or updated.

3. **Given** a proposed match, **When** the user rejects the match, **Then** the receipt remains unmatched and is available for manual matching or future auto-match runs.

4. **Given** an unmatched receipt, **When** the user manually selects a transaction to match, **Then** the system creates the link and learns a new vendor alias from this pairing.

---

### User Story 3 - Vendor Alias Learning (Priority: P2)

As an expense report user, I want the system to learn vendor patterns from my confirmations so that future receipts from the same vendor are matched more accurately.

**Why this priority**: Learning improves accuracy over time and reduces user effort in subsequent months.

**Independent Test**: Confirm a match for a new vendor pattern, verify alias is created, upload another receipt from same vendor, verify auto-match finds it with higher confidence.

**Acceptance Scenarios**:

1. **Given** a confirmed match between receipt "DELTA AIR 0062363598531" and a transaction, **When** the confirmation is processed, **Then** a vendor alias is created with pattern "DELTA AIR" → canonical name "Delta Airlines".

2. **Given** an existing vendor alias "DELTA AIR" → "Delta Airlines" with default GL code 66300, **When** a new receipt with vendor "DELTA AIR LINES INC" is uploaded, **Then** fuzzy matching recognizes the pattern and suggests the same canonical vendor.

3. **Given** a vendor alias is matched, **When** the match is confirmed, **Then** the alias match_count is incremented and last_matched_at is updated.

4. **Given** a vendor alias with default_gl_code set, **When** a match is confirmed, **Then** the linked expense line is pre-populated with that GL code.

---

### User Story 4 - View Unmatched Items (Priority: P2)

As an expense report user, I want to see which receipts and transactions remain unmatched so that I can take action before generating my expense report.

**Why this priority**: Visibility into unmatched items prevents incomplete reports and missing documentation.

**Independent Test**: After running auto-match, query unmatched receipts and unmatched transactions separately, verify counts and lists are accurate.

**Acceptance Scenarios**:

1. **Given** 10 receipts where 7 matched automatically, **When** I view unmatched receipts, **Then** I see a list of the 3 unmatched receipts with their extracted details.

2. **Given** 15 transactions where 7 matched to receipts, **When** I view unmatched transactions, **Then** I see a list of the 8 unmatched transactions.

3. **Given** unmatched items exist, **When** I view the matching dashboard, **Then** I see summary counts: "7 matched, 3 receipts unmatched, 8 transactions unmatched".

---

### User Story 5 - Alias Confidence Decay (Priority: P3)

As a system administrator, I want vendor aliases that haven't been used in 6 months to be flagged for review so that outdated patterns don't cause incorrect matches.

**Why this priority**: Maintenance feature to keep alias data quality high over time.

**Independent Test**: Create an alias with last_matched_at > 6 months ago, run decay job, verify alias is flagged or confidence reduced.

**Acceptance Scenarios**:

1. **Given** a vendor alias last used 7 months ago, **When** the confidence decay job runs, **Then** the alias confidence is reduced or the alias is flagged for review.

2. **Given** a vendor alias used within the last 6 months, **When** the confidence decay job runs, **Then** the alias remains unchanged.

---

### Edge Cases

- What happens when a receipt amount is in a different currency than the transaction? System should flag as "Currency mismatch - manual review required".
- How does the system handle duplicate receipts (same vendor, date, amount)? System should warn user and prevent double-matching to the same transaction.
- What if a single transaction should match multiple receipts (e.g., one payment covering multiple invoices)? System matches one-to-one only; user can split transactions in a future feature.
- What happens when receipt extraction failed (no vendor/date/amount)? These receipts are excluded from auto-match and flagged for manual entry.
- How are refunds/credits handled? Negative amounts should still match based on absolute value comparison with appropriate sign handling.
- How are concurrent match conflicts handled? First-write-wins with optimistic locking; if two users attempt to match different receipts to the same transaction, the second user sees an error message and must select a different transaction.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST match receipts to transactions using a confidence-based algorithm considering amount, date, and vendor.
- **FR-002**: System MUST allow amount tolerance of +/-$0.10 for exact matches and +/-$1.00 for near matches with reduced confidence.
- **FR-003**: System MUST allow date tolerance of +/-3 days for matching (receipt date vs. transaction date).
- **FR-004**: System MUST calculate confidence scores from 0-100% based on amount match (40 points max), date match (35 points max), and vendor match (25 points max).
- **FR-005**: System MUST only propose matches with confidence >= 70%.
- **FR-006**: System MUST perform vendor pattern matching against the VendorAliases table using substring/LIKE matching.
- **FR-007**: System MUST perform fuzzy matching (similarity > 70%) when exact vendor alias not found.
- **FR-008**: System MUST create or update vendor aliases when users confirm matches.
- **FR-009**: System MUST track alias usage statistics (match_count, last_matched_at) for each confirmation.
- **FR-010**: System MUST prevent matching one transaction to multiple receipts (one-to-one only).
- **FR-011**: System MUST prevent matching one receipt to multiple transactions.
- **FR-012**: System MUST provide endpoints to list unmatched receipts and unmatched transactions.
- **FR-013**: System MUST allow users to manually match a receipt to a transaction when auto-match fails.
- **FR-014**: System MUST log whether each match was auto-proposed or manually created.
- **FR-015**: System MUST run a background job to reduce confidence of aliases unused for 6+ months.
- **FR-016**: System MUST deduplicate transactions on import using a hash of date+amount+description to prevent duplicate matches from re-imported statements.
- **FR-017**: System MUST use optimistic locking to handle concurrent match attempts; first confirmation wins and subsequent attempts receive a conflict error prompting retry.

### Key Entities

- **ReceiptTransactionMatch**: Links a receipt to a transaction with confidence score, match reasoning, and creation metadata. Lifecycle states: Proposed → Confirmed or Rejected (both states retained for audit trail and alias learning signals).
- **VendorAlias**: Maps vendor patterns (from transaction descriptions) to canonical vendor names, with default GL code, department, usage statistics, and confidence score.
- **MatchResult**: Transient object containing proposed transaction ID, receipt ID, confidence score, and breakdown of scoring components.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Auto-match successfully pairs at least 85% of receipts to the correct transaction on first run (measured by user confirmation rate).
- **SC-002**: Users can review and confirm/reject a proposed match in under 10 seconds per item.
- **SC-003**: Manual matching (for failed auto-matches) takes under 30 seconds per receipt.
- **SC-004**: Vendor alias learning improves match accuracy by 5% month-over-month for returning vendors.
- **SC-005**: All matching operations complete without external AI API calls (Tier 1 only).
- **SC-006**: Batch auto-match for 50 receipts completes in under 60 seconds.
- **SC-007**: Zero false positive matches confirmed by users (all confirmed matches are actually correct pairings).

## Assumptions

- Receipts have already been processed by Document Intelligence (Sprint 3) with vendor, date, and amount extracted.
- Transactions have been imported from credit card statements (Sprint 4) with date, amount, and raw description.
- VendorAliases table exists and may contain seed data from Sprint 2.
- Users interact with the system for a single card/account at a time (multi-card matching is out of scope).
- All amounts are in USD (currency conversion is out of scope for this sprint).

## Out of Scope

- Multi-currency matching
- Matching across multiple users' expenses
- One-to-many or many-to-one matching (e.g., split receipts)
- AI/ML-based matching improvements (Tier 2/3) - this sprint is Tier 1 only
- Automated match confirmation (all matches require user review)

## Dependencies

- Sprint 3: Receipt Pipeline (receipts with extracted data)
- Sprint 4: Statement Import (transactions with parsed data)
- Sprint 2: Core Backend (VendorAliases table, authentication)
