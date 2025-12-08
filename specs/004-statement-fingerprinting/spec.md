# Feature Specification: Statement Import & Fingerprinting

**Feature Branch**: `004-statement-fingerprinting`
**Created**: 2025-12-05
**Status**: Draft
**Input**: User description: "Sprint 4 - Statement Import & Fingerprinting: Users can import credit card statements with automatic column detection for known sources"

## Clarifications

### Session 2025-12-05

- Q: Should fingerprints be user-scoped, org-scoped, or globally shared? → A: User-scoped - each user has their own fingerprints; system pre-configured fingerprints (Chase/Amex) are global
- Q: How should duplicate transactions be handled during import? → A: Skip duplicates automatically, import only new transactions, show summary of skipped items
- Q: What happens when AI column inference fails or is unavailable? → A: Block import entirely; user must retry later when AI is available
- Q: When both user and system fingerprints match, which takes precedence? → A: Show both options and let user choose which mapping to use
- Q: What are the minimum required fields for a transaction to be imported? → A: All three required (date, amount, description); rows missing any field are skipped

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Import Known Statement Format (Priority: P1)

A user downloads their monthly Chase credit card statement as a CSV file and wants to import it into ExpenseFlow. Since Chase is a pre-configured source, the system automatically recognizes the statement format and imports all transactions without requiring the user to map columns.

**Why this priority**: This is the primary value proposition - zero-friction import for common statement formats. Users should be able to import statements from major card providers without any configuration.

**Independent Test**: Can be fully tested by uploading a Chase CSV file and verifying transactions appear correctly with proper dates, amounts, and descriptions.

**Acceptance Scenarios**:

1. **Given** a user with an uploaded Chase Business Card CSV, **When** the system analyzes the file, **Then** the system automatically detects the Chase format and displays the detected mapping with high confidence
2. **Given** a detected statement format with high confidence, **When** the user confirms the import, **Then** all transactions are imported with correctly parsed dates, amounts, and descriptions
3. **Given** a statement with negative amounts representing charges (Chase convention), **When** importing, **Then** the system correctly interprets negative values as expenses

---

### User Story 2 - Import Unknown Statement Format (Priority: P2)

A user has a credit card statement from an unfamiliar bank that the system hasn't seen before. The system uses AI to infer the column mapping, presents it for user confirmation, and saves the mapping for future imports.

**Why this priority**: Enables the system to handle any statement format, not just pre-configured ones. Critical for supporting diverse financial institutions.

**Independent Test**: Can be fully tested by uploading a CSV with non-standard column headers and verifying the AI correctly infers and the user can confirm/correct the mapping.

**Acceptance Scenarios**:

1. **Given** a CSV with unknown column headers, **When** the user uploads it, **Then** the system uses AI to infer column mappings and presents them for confirmation
2. **Given** an AI-inferred mapping displayed to the user, **When** the user corrects any misidentified columns, **Then** the system uses the corrected mapping for import
3. **Given** a confirmed mapping for a new source, **When** the import completes, **Then** the system saves the fingerprint for automatic detection on future uploads

---

### User Story 3 - Handle American Express Format (Priority: P2)

A user uploads an American Express statement where positive amounts represent charges (opposite of Chase). The system correctly interprets the amount sign convention.

**Why this priority**: Major card providers use different conventions. Supporting Amex alongside Chase covers a significant portion of business card usage.

**Independent Test**: Can be fully tested by uploading an Amex CSV and verifying positive amounts are correctly interpreted as expenses.

**Acceptance Scenarios**:

1. **Given** an American Express CSV where positive amounts are charges, **When** imported, **Then** positive amounts are correctly recorded as expenses
2. **Given** a statement with credits/refunds, **When** imported, **Then** credits are correctly differentiated from charges

---

### User Story 4 - Column Mapping Correction (Priority: P3)

The system misidentifies a column (e.g., confuses "Post Date" with "Transaction Date"), and the user needs to correct the mapping before import.

**Why this priority**: Provides safety net for edge cases where automatic detection fails. Users need control to ensure accurate imports.

**Independent Test**: Can be fully tested by triggering a misidentified mapping and using the UI to correct it.

**Acceptance Scenarios**:

1. **Given** a proposed column mapping with an error, **When** the user selects the correct mapping for a column, **Then** the UI updates to show the corrected mapping
2. **Given** a corrected mapping, **When** the user confirms, **Then** the corrected mapping is saved as the fingerprint for this source

---

### User Story 5 - Track Import Source Statistics (Priority: P4)

The system tracks whether imports used cached fingerprints (efficient) or required AI inference (costly) for monitoring and optimization purposes.

**Why this priority**: Enables cost monitoring and helps identify opportunities to expand pre-configured formats.

**Independent Test**: Can be fully tested by importing files and checking that usage metrics are recorded.

**Acceptance Scenarios**:

1. **Given** a fingerprint-matched import, **When** completed, **Then** the system logs this as a "Tier 1" (cached) hit
2. **Given** an AI-inferred import, **When** completed, **Then** the system logs this as a "Tier 3" (AI) usage

---

### Edge Cases

- What happens when a CSV file has no header row? System displays an error indicating the file format is not supported and requires a header row.
- How does the system handle a statement with duplicate column names? System appends a numeric suffix to distinguish columns and allows user to map them individually.
- What happens when a statement uses a date format the system doesn't recognize? System falls back to AI inference for date parsing and prompts user to confirm the detected format.
- How does the system handle statements with multiple currencies? System imports amounts as-is with currency indicator if present; currency conversion is out of scope for this feature.
- What happens when the same statement is imported twice (duplicate detection)? System identifies duplicate transactions by date+amount+description hash, automatically skips duplicates, imports only new transactions, and displays a summary showing how many were imported vs skipped.
- How does the system handle very large statements (10,000+ transactions)? System processes in batches to avoid timeouts, with progress indication to user.
- What happens when a CSV uses non-standard encoding (Latin-1 vs UTF-8)? System auto-detects encoding and falls back to Latin-1 if UTF-8 parsing fails.
- What happens when AI inference fails or is unavailable for unknown formats? System displays an error message indicating AI service is temporarily unavailable; user must retry later. Known fingerprinted formats continue to work without AI.
- What happens when both a user fingerprint and system fingerprint match the same header? System presents both mapping options to the user and lets them choose which to use for the import.
- What happens when a transaction row is missing date, amount, or description? System skips incomplete rows and includes them in the import summary as "skipped due to missing required fields."

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST parse CSV and Excel statement files uploaded by users
- **FR-002**: System MUST compute a unique fingerprint from the header row of each statement
- **FR-003**: System MUST check uploaded statements against stored fingerprints to detect known sources
- **FR-004**: System MUST support pre-configured fingerprints for Chase Business Card and American Express formats
- **FR-005**: System MUST use AI to infer column mappings when no fingerprint match is found
- **FR-006**: System MUST present detected/inferred column mappings to users for confirmation before import
- **FR-007**: System MUST allow users to correct any misidentified columns in the mapping UI
- **FR-008**: System MUST save confirmed mappings as new fingerprints for future automatic detection
- **FR-009**: System MUST store imported transactions with date, amount, description, and source reference
- **FR-010**: System MUST handle different amount sign conventions (negative=charge vs positive=charge)
- **FR-011**: System MUST detect file encoding and support both UTF-8 and Latin-1 encoded files
- **FR-012**: System MUST parse dates according to the format specified in the fingerprint configuration
- **FR-013**: System MUST log whether each import used cached fingerprint (Tier 1) or AI inference (Tier 3)
- **FR-014**: System MUST deduplicate transactions based on date, amount, and description hash to prevent duplicate imports
- **FR-015**: System MUST validate that each transaction row has date, amount, and description; rows missing any required field are skipped with summary reporting

### Key Entities

- **Transaction**: Represents a single credit card transaction. Contains transaction date, post date (optional), description, amount, and source statement reference. Links to matched receipts when available.
- **Statement Fingerprint**: Configuration for a known statement source. Contains header hash for detection, column-to-field mapping, date format specification, and amount sign convention. User-created fingerprints are private to each user; system pre-configured fingerprints (Chase/Amex) are available to all users.
- **Statement Import**: Audit record of each statement import. Tracks source file, user, timestamp, transaction count, and whether fingerprint or AI was used.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can import a pre-configured statement format (Chase/Amex) in under 30 seconds with no manual column mapping
- **SC-002**: AI-inferred column mappings are correct without user correction at least 80% of the time
- **SC-003**: 95% of imported transactions have correctly parsed dates and amounts
- **SC-004**: Re-importing the same statement source automatically uses saved fingerprint with no user intervention
- **SC-005**: System processes statement files with up to 1,000 transactions in under 10 seconds
- **SC-006**: Known statement formats (Tier 1) require zero AI calls, reducing per-import cost to effectively zero
- **SC-007**: Users can correct column mappings and complete import in under 2 minutes even for unknown formats

## Assumptions

- Users have access to download CSV or Excel statement files from their card providers
- Statement files contain a header row with column names
- Date formats within a single statement are consistent
- Transaction amounts are numeric (may include currency symbols that need stripping)
- Users import statements monthly, with typical statement sizes of 20-200 transactions
- The system has access to an AI service for column inference when fingerprints don't match
- Pre-configured fingerprints for Chase and Amex cover the majority of business card usage

## Dependencies

- Receipt Pipeline (Sprint 3) - Transactions will eventually link to matched receipts
- Core Backend & Auth (Sprint 2) - User authentication and database tables required
- Azure Blob Storage - Statement files may be stored temporarily during processing
