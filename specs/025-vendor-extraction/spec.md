# Feature Specification: Vendor Name Extraction

**Feature Branch**: `025-vendor-extraction`
**Created**: 2026-01-05
**Status**: Implemented
**Input**: User description: "Extract vendor names from transaction descriptions using pattern matching and normalization"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Clean Vendor Names in Transaction List (Priority: P1)

When a user views their transactions, they see a clean, recognizable vendor name (e.g., "Amazon" instead of "AMZN MKTP US*2K7XY9Z03") making it easier to understand and categorize expenses.

**Why this priority**: This is the core value proposition - users need to quickly identify vendors to manage their expenses. Without readable vendor names, users waste time deciphering cryptic bank descriptions.

**Independent Test**: Can be fully tested by importing a bank statement and verifying that transactions display human-readable vendor names instead of raw bank codes.

**Acceptance Scenarios**:

1. **Given** a transaction with description "AMZN MKTP US*2K7XY9Z03", **When** the transaction is displayed, **Then** the vendor shows as "Amazon"
2. **Given** a transaction with description "UBER *TRIP HELP.UBER.COM", **When** the transaction is displayed, **Then** the vendor shows as "Uber"
3. **Given** a transaction with description "SQ *STARBUCKS #12345", **When** the transaction is displayed, **Then** the vendor shows as "Starbucks"
4. **Given** a transaction with an unrecognized description "JONES HARDWARE SUPPLIES", **When** the transaction is displayed, **Then** the vendor shows the original description (graceful fallback)

---

### User Story 2 - Improved Expense Categorization Accuracy (Priority: P2)

When the system recognizes a vendor, it can leverage historical categorization patterns for that vendor to provide better GL code and department suggestions.

**Why this priority**: Accurate vendor identification directly improves categorization suggestions, reducing manual correction time. This builds on P1's vendor extraction.

**Independent Test**: Can be tested by submitting a transaction from a known vendor and verifying the system suggests the GL code/department that user previously confirmed for that vendor.

**Acceptance Scenarios**:

1. **Given** a user has categorized 3 Amazon transactions to GL code "6100" (Office Supplies), **When** a new Amazon transaction appears, **Then** the system suggests GL code "6100" with high confidence
2. **Given** a vendor is extracted from a transaction, **When** the vendor has default categorization in the system, **Then** those defaults are surfaced as suggestions

---

### User Story 3 - Analytics and Reporting by Vendor (Priority: P3)

Users can view spending analytics grouped by vendor name, enabling them to understand spending patterns with specific merchants.

**Why this priority**: Once vendors are consistently extracted, users gain visibility into spending patterns. This is a downstream benefit that requires P1 to be working.

**Independent Test**: Can be tested by viewing the analytics dashboard and verifying spending totals are grouped by clean vendor names rather than raw descriptions.

**Acceptance Scenarios**:

1. **Given** multiple transactions from the same vendor with different description formats, **When** viewing analytics, **Then** all transactions are grouped under the same vendor name
2. **Given** transactions from "AMZN MKTP US*ABC" and "AMAZON.COM*XYZ", **When** viewing vendor spending, **Then** both appear under "Amazon" with combined totals

---

### Edge Cases

- What happens when multiple patterns could match the same description? → Use the highest confidence match, then highest match count
- What happens when a vendor pattern partially matches? → Use PostgreSQL ILIKE for substring matching; no minimum threshold needed
- How does the system handle international vendor names with special characters? → Case-insensitive matching handles most cases; Unicode normalization out of scope for v1
- What happens when the same vendor uses vastly different description formats across banks? → Multiple VendorAlias rows with same CanonicalName support different patterns

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST extract a clean vendor name from transaction descriptions when a matching VendorAlias pattern exists
- **FR-002**: System MUST fall back to the original description when no vendor pattern matches
- **FR-003**: System MUST support multiple alias patterns per vendor (e.g., "AMZN", "AMAZON.COM", "AMZN MKTP" all map to "Amazon")
- **FR-004**: System MUST use the existing VendorAlias infrastructure for pattern matching
- **FR-005**: System MUST prioritize matches by confidence score, then by match count (most frequently matched patterns first)
- **FR-006**: System MUST perform case-insensitive pattern matching
- **FR-007**: System MUST populate the Vendor field in TransactionCategorizationDto with the extracted DisplayName
- **FR-008**: System MUST increment the MatchCount on VendorAlias when a pattern matches
- **FR-009**: System MUST update LastMatchedAt timestamp when a pattern matches

### Key Entities

- **VendorAlias**: Maps alias patterns to canonical vendor names. Key attributes: CanonicalName (internal ID), DisplayName (human-readable), AliasPattern (matching pattern), Confidence (match quality score), MatchCount (usage frequency)
- **Transaction**: Contains the raw Description field that needs vendor extraction
- **TransactionCategorizationDto**: Contains the Vendor field that should be populated with extracted vendor name

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 80% of transactions from the top 50 vendors (by transaction volume in the system) display clean vendor names instead of raw descriptions
- **SC-002**: Users can identify the vendor at a glance without reading the full bank description
- **SC-003**: Vendor extraction adds no perceptible delay to transaction display (under 100ms additional processing)
- **SC-004**: Analytics reports accurately group transactions by vendor for known vendors

## Assumptions

- The existing VendorAlias table already contains patterns for common vendors (seeded in migration 20260103000000)
- Pattern matching uses PostgreSQL ILIKE for case-insensitive substring matching
- The VendorAliasService.FindMatchingAliasAsync() method already implements the core matching logic
- The change is primarily about calling the existing service from CategorizationService and populating the Vendor field
- The current implementation uses transaction.Description as a temporary placeholder; this feature replaces that with proper extraction
