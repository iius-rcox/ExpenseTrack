# Feature Specification: Automated UAT Testing for Claude Code

**Feature Branch**: `012-automated-uat-testing`
**Created**: 2025-12-19
**Status**: Draft
**Input**: User description: "Create automated UAT testing for Claude Code to complete using @test-data receipts and statement from chase.csv"

## Overview

This feature enables automated User Acceptance Testing (UAT) that Claude Code can execute against the ExpenseFlow application. The test suite validates the complete expense management workflow using real test data: 19 receipt images and a Chase bank statement CSV containing 486 transactions. This creates a repeatable, comprehensive validation process for the entire receipt-to-report pipeline.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Receipt Upload and OCR Processing (Priority: P1)

As a tester using Claude Code, I want to upload all test receipt images and verify they are processed correctly, so that I can confirm the receipt ingestion pipeline works end-to-end.

**Why this priority**: Receipt processing is the foundation of the entire expense workflow. Without working receipt upload and OCR, no other features can function.

**Independent Test**: Can be fully tested by uploading the 19 test receipts from `test-data/receipts/` and verifying each reaches "Ready" status with extracted data.

**Acceptance Scenarios**:

1. **Given** a set of 19 test receipt images in `test-data/receipts/`, **When** Claude Code uploads each receipt via the API, **Then** each receipt should reach "Ready" or "Unmatched" status within 2 minutes.

2. **Given** uploaded receipts are processing, **When** OCR extraction completes, **Then** each receipt should have extracted amount, date, and vendor (where detectable) with confidence scores above 70%.

3. **Given** the RDU parking receipt (20251211_212334.jpg), **When** processing completes, **Then** the extracted amount should be $84.00 and date should be December 11, 2025.

4. **Given** a PDF receipt (Receipt 2768.pdf), **When** uploaded and processed, **Then** the system should handle the PDF format and extract data successfully.

---

### User Story 2 - Statement Import and Transaction Fingerprinting (Priority: P1)

As a tester using Claude Code, I want to import the Chase bank statement and verify all transactions are fingerprinted correctly, so that I can confirm the statement processing pipeline works.

**Why this priority**: Statement import enables transaction matching - the core value proposition of ExpenseFlow. This is equally critical as receipt processing.

**Independent Test**: Can be fully tested by importing `test-data/statements/chase.csv` and verifying 486 transactions are created with proper categorization.

**Acceptance Scenarios**:

1. **Given** the Chase statement CSV file with 486 transactions, **When** Claude Code imports the statement via the API, **Then** all 486 transactions should be created in the system.

2. **Given** imported transactions, **When** fingerprinting completes, **Then** each transaction should have a normalized vendor name and category assigned.

3. **Given** the RDUAA PUBLIC PARKING transaction ($84.00 on 12/11/2025), **When** fingerprinting completes, **Then** it should be categorized as "Travel" with normalized vendor "RDU Airport Parking".

4. **Given** Amazon marketplace transactions, **When** fingerprinting completes, **Then** they should be normalized to "Amazon" as the vendor regardless of the specific marketplace identifier.

---

### User Story 3 - Automated Receipt-Transaction Matching (Priority: P2)

As a tester using Claude Code, I want to trigger automatic matching between receipts and transactions, so that I can verify the matching engine correctly pairs receipts with their corresponding bank transactions.

**Why this priority**: Matching is the key differentiator of ExpenseFlow, but depends on both receipts and transactions being properly imported first.

**Independent Test**: Can be fully tested after P1 scenarios complete by triggering auto-match and verifying the RDU parking receipt matches the RDUAA transaction.

**Acceptance Scenarios**:

1. **Given** processed receipts and imported transactions, **When** Claude Code triggers auto-matching, **Then** at least one receipt-transaction match should be proposed.

2. **Given** the $84.00 RDU parking receipt and the $84.00 RDUAA PUBLIC PARKING transaction, **When** auto-matching runs, **Then** these should be matched with high confidence (>80%).

3. **Given** proposed matches, **When** Claude Code confirms a match, **Then** the receipt status should change to "Matched" and the transaction should reference the receipt.

4. **Given** receipts with no matching transactions, **When** auto-matching completes, **Then** those receipts should remain in "Unmatched" status for manual review.

---

### User Story 4 - End-to-End Workflow Validation (Priority: P2)

As a tester using Claude Code, I want to execute a complete test workflow from upload to report generation, so that I can validate the entire system integration.

**Why this priority**: This validates system integration after individual components are confirmed working.

**Independent Test**: Can be tested by running the complete workflow and generating a draft expense report.

**Acceptance Scenarios**:

1. **Given** matched receipts and categorized transactions, **When** Claude Code requests a draft expense report, **Then** a report should be generated containing all matched expenses.

2. **Given** the generated report, **When** Claude Code retrieves report details, **Then** it should include expense summaries grouped by category.

3. **Given** unmatched receipts exist, **When** generating the report, **Then** unmatched receipts should be flagged for review.

---

### User Story 5 - Test Results Validation and Reporting (Priority: P3)

As a tester using Claude Code, I want clear pass/fail reporting for each test case, so that I can quickly identify any regressions.

**Why this priority**: Reporting on test results provides visibility but doesn't affect core functionality validation.

**Independent Test**: Can be tested by examining test output format and verifying all assertions are clearly reported.

**Acceptance Scenarios**:

1. **Given** a completed test run, **When** Claude Code summarizes results, **Then** a clear pass/fail count should be provided with specific failure details.

2. **Given** a test failure, **When** reviewing the output, **Then** the specific assertion that failed and actual vs expected values should be visible.

3. **Given** all tests pass, **When** viewing the summary, **Then** confirmation of successful validation should include test execution time.

---

### Edge Cases

- What happens when a receipt image is corrupted or unreadable? System should mark as "Error" with descriptive message.
- How does the system handle duplicate receipt uploads? Should detect and warn about potential duplicates.
- What if the Chase CSV has malformed rows? Should skip bad rows and report count of skipped entries.
- How does matching handle receipts with amounts very close but not exact to transactions? Should use configurable tolerance (default $0.01).
- What if a receipt date doesn't match any transaction within a reasonable window? Should remain unmatched with suggestions.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Claude Code MUST be able to upload receipt images via the receipts API endpoint
- **FR-002**: Claude Code MUST be able to import CSV statement files via the statements API endpoint
- **FR-003**: Claude Code MUST be able to check receipt processing status until completion by polling at 5-second intervals
- **FR-004**: Claude Code MUST be able to trigger auto-matching between receipts and transactions
- **FR-005**: Claude Code MUST be able to confirm or reject proposed matches
- **FR-006**: Claude Code MUST be able to request draft expense report generation
- **FR-007**: The test suite MUST validate OCR extraction accuracy against known receipt values
- **FR-008**: The test suite MUST validate transaction fingerprinting against expected normalizations
- **FR-009**: The test suite MUST validate matching accuracy for known receipt-transaction pairs
- **FR-010**: Test execution MUST provide structured JSON output including test name, status (pass/fail/skipped), duration, and error details
- **FR-011**: On test failure, dependent tests MUST be skipped but independent test paths MUST continue executing
- **FR-012**: The test suite MUST use the existing test-data folder structure (`test-data/receipts/`, `test-data/statements/`)
- **FR-013**: Claude Code MUST authenticate against the staging API using environment variables (`EXPENSEFLOW_API_KEY`, `EXPENSEFLOW_USER_TOKEN`)
- **FR-014**: The staging API MUST provide a `/api/test/cleanup` endpoint to remove test data by tag/prefix for idempotent re-runs
- **FR-015**: Expected test values MUST be maintained in a separate JSON/YAML file (`test-data/expected-values.json`) mapping receipt filenames to expected extraction values

### Key Entities

- **Test Receipt**: A receipt image from the test-data folder with known expected extraction values (amount, date, vendor)
- **Test Statement**: The Chase CSV file containing 486 known transactions with expected categorizations
- **Expected Match**: A known pairing between a test receipt and test transaction (e.g., RDU parking)
- **Expected Values File**: A JSON/YAML file (`test-data/expected-values.json`) containing golden data for test assertions (receipt→expected amount/date/vendor mappings, transaction→expected category mappings, known match pairs)
- **Test Assertion**: A verification comparing actual system output against expected values from the expected values file
- **Test Run**: A complete execution of all test scenarios with aggregated JSON results

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 19 test receipts can be uploaded and processed to terminal status within 5 minutes total
- **SC-002**: At least 90% of receipts reach "Ready" or "Unmatched" status (vs "Error")
- **SC-003**: The Chase statement imports all 486 transactions successfully
- **SC-004**: Known receipt-transaction matches (RDU parking) are correctly proposed by auto-matching
- **SC-005**: The complete UAT workflow executes in under 15 minutes
- **SC-006**: Test results clearly identify any failures with actionable diagnostic information
- **SC-007**: The test suite can be re-run multiple times producing consistent results (idempotent)

## Clarifications

### Session 2025-12-19

- Q: How should Claude Code authenticate against the staging API? → A: Environment variables (`EXPENSEFLOW_API_KEY`, `EXPENSEFLOW_USER_TOKEN`)
- Q: How should test data cleanup work between runs for idempotency? → A: Dedicated cleanup endpoint (`/api/test/cleanup`) to remove test data by tag/prefix
- Q: What progress visibility should Claude Code have during long-running operations? → A: Polling status endpoints at 5-second intervals
- Q: What should happen when a test assertion fails mid-workflow? → A: Skip dependent tests but continue with independent test paths
- Q: What format should test results be output in? → A: Structured JSON (test name, status, duration, error details)
- Q: How should expected test values be maintained? → A: Separate JSON/YAML file mapping receipt filenames to expected extraction values

## Assumptions

- Claude Code authenticates using environment variables: `EXPENSEFLOW_API_KEY` for API access and `EXPENSEFLOW_USER_TOKEN` for user context
- The staging API is accessible at `https://staging.expense.ii-us.com/api` or via port-forward
- Test data in `test-data/` folder represents realistic expense scenarios
- The RDU parking receipt ($84.00) has a corresponding Chase transaction that should match
- OCR confidence thresholds follow existing system defaults (70% for field-level)
- The Chase CSV format matches the expected column structure for the statement import API
