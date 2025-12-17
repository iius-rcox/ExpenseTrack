# Feature Specification: Draft Report Generation

**Feature Branch**: `008-draft-report-generation`
**Created**: 2025-12-16
**Status**: Draft
**Input**: User description: "Sprint 8: Draft Report Generation - Auto-generate draft expense reports from matched receipts and transactions"

## Clarifications

### Session 2025-12-16

- Q: What states can an ExpenseReport have beyond "Draft"? → A: Draft only - Reports stay "Draft" in this sprint; finalization workflow deferred to later sprint.
- Q: How long are draft reports retained before cleanup? → A: Retain indefinitely - User must manually delete old drafts.
- Q: Can a user have the same draft open in multiple browser tabs? → A: Last write wins - No locking; most recent save overwrites previous.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Generate Draft Report (Priority: P1)

As an employee, I want to generate a draft expense report for a specific period so that my matched receipts and transactions are automatically compiled with pre-populated categorizations, saving me hours of manual data entry.

**Why this priority**: This is the core value proposition - transforming scattered receipt-transaction pairs into a structured expense report. Without this, users cannot benefit from all the matching and categorization work done in previous sprints.

**Independent Test**: Can be fully tested by selecting a month with matched receipts/transactions and generating a draft, then verifying all matched items appear with pre-populated GL codes and departments.

**Acceptance Scenarios**:

1. **Given** I have 10 matched receipt-transaction pairs for January 2025, **When** I request a draft report for that period, **Then** a draft report is created containing all 10 expense lines with dates, amounts, and vendors populated.
2. **Given** vendor aliases exist with default GL codes and departments, **When** a draft report is generated, **Then** expense lines from those vendors have GL codes and departments pre-populated.
3. **Given** I request a draft report, **When** the system processes each expense, **Then** descriptions are normalized to accounts-payable-readable format.
4. **Given** some transactions have no matched receipts, **When** I generate a draft report, **Then** those transactions are included but clearly flagged as "missing receipt".

---

### User Story 2 - Review and Edit Draft (Priority: P1)

As an employee, I want to review and edit my draft expense report before finalizing so that I can correct any inaccurate categorizations and provide justifications for missing receipts.

**Why this priority**: Auto-generation is only valuable if users can verify and correct the output. This ensures data quality and compliance with expense policies.

**Independent Test**: Can be fully tested by opening a generated draft, modifying GL codes, departments, and descriptions, then saving changes and verifying persistence.

**Acceptance Scenarios**:

1. **Given** I have a draft report with 5 expense lines, **When** I view the report, **Then** I see all lines with editable GL code, department, and description fields.
2. **Given** an expense line has a pre-populated GL code from tier 2 (embedding similarity), **When** I view the line, **Then** I see an indicator showing the suggestion source (e.g., "Similar expense match").
3. **Given** I modify a GL code from the suggested value, **When** I save the change, **Then** the system records this correction to improve future suggestions.
4. **Given** an expense line is flagged as missing receipt, **When** I view the line, **Then** I can select a justification reason from a predefined list.

---

### User Story 3 - Cache Learning from Edits (Priority: P2)

As a system, I want to learn from user edits to improve future categorization suggestions so that the system becomes more accurate over time and reduces manual corrections.

**Why this priority**: This feedback loop is essential for the tiered categorization system to improve. Without learning, users would need to make the same corrections repeatedly.

**Independent Test**: Can be fully tested by making a GL code correction, then generating a new draft with a similar expense and verifying the corrected value is suggested.

**Acceptance Scenarios**:

1. **Given** I correct a GL code on an expense line, **When** I save the change, **Then** the vendor alias is updated with the new default GL code (if confidence threshold met).
2. **Given** I confirm a categorization suggestion, **When** I save the report, **Then** a verified embedding is created for that expense description.
3. **Given** an expense description was not in the cache, **When** I save an edited draft, **Then** the normalized description is added to the description cache.

---

### User Story 4 - View Suggestion Tiers (Priority: P3)

As an employee, I want to see which categorization tier provided each suggestion so that I can understand the confidence level and make informed decisions about accepting or modifying suggestions.

**Why this priority**: Transparency in AI-assisted decisions builds user trust and helps users identify which suggestions may need more scrutiny.

**Independent Test**: Can be fully tested by generating a draft with expenses that hit different tiers, then verifying each line shows the appropriate tier indicator.

**Acceptance Scenarios**:

1. **Given** a GL code was suggested from a vendor alias (Tier 1), **When** I view the expense line, **Then** I see a high-confidence indicator (e.g., "Vendor default").
2. **Given** a GL code was suggested from embedding similarity (Tier 2), **When** I view the expense line, **Then** I see a medium-confidence indicator (e.g., "Similar expense").
3. **Given** a GL code was suggested by AI inference (Tier 3), **When** I view the expense line, **Then** I see a lower-confidence indicator (e.g., "AI suggestion").

---

### Edge Cases

- What happens when there are no matched receipts/transactions for the selected period? System displays a message indicating no expenses found and suggests checking earlier steps (matching, import).
- What happens when a user tries to generate a draft for a period that already has a draft? System offers options: create new draft (replacing old), or open existing draft.
- How does system handle expenses that need to be split across multiple GL codes? Lines with existing split patterns show the suggested split; users can apply, modify, or create new splits.
- What happens if the GL code or department reference data is outdated? System validates against current reference data and flags invalid codes for user correction.
- How does system handle very long descriptions that exceed display limits? Descriptions are truncated in list view with full text available on hover/click.
- What happens if a user edits the same draft in multiple browser tabs? Last write wins - no locking is applied; the most recent save overwrites any previous changes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST create a new expense report in "Draft" status when user requests draft generation for a period
- **FR-002**: System MUST include all matched receipt-transaction pairs for the specified period in the draft report
- **FR-003**: System MUST include unmatched transactions (without receipts) in the draft report, clearly flagged as missing receipts
- **FR-004**: System MUST pre-populate GL codes using the tiered categorization system (cache, embeddings, AI) established in Sprint 6
- **FR-005**: System MUST pre-populate departments using the same tiered approach
- **FR-006**: System MUST normalize all expense descriptions before including them in the draft
- **FR-007**: System MUST display the tier source (1, 2, or 3) for each categorization suggestion
- **FR-008**: System MUST allow users to edit GL code, department, and description for any expense line
- **FR-009**: System MUST persist user edits immediately upon save
- **FR-010**: System MUST update vendor aliases when users correct GL code or department (subject to confidence threshold rules)
- **FR-011**: System MUST create verified embeddings when users confirm categorization suggestions
- **FR-012**: System MUST add new description-to-normalized mappings to the cache when users approve normalized descriptions
- **FR-013**: System MUST provide a predefined list of justification reasons for missing receipts ("Not provided", "Lost", "Digital subscription - no receipt", "Under threshold", "Other")
- **FR-014**: System MUST track whether each expense line originated from a receipt-transaction match or an unmatched transaction
- **FR-015**: System MUST calculate and display summary statistics (total amount, line count, missing receipt count, tier hit rates)
- **FR-016**: System MUST allow users to delete their own draft reports; drafts are retained indefinitely until user deletes them

### Key Entities

- **ExpenseReport**: Represents an expense report for a specific period, containing metadata (period, status, creation date, user) and summary statistics. Status is "Draft" only in this sprint; finalization states deferred to future sprint.
- **ExpenseLine**: Individual expense item within a report, linking to receipt and/or transaction, with categorization (GL code, department), description (original and normalized), amount, and missing receipt justification if applicable
- **MissingReceiptJustification**: Predefined reason explaining why a receipt is not attached to an expense line

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can generate a draft expense report in under 30 seconds for a typical month (20-50 expenses)
- **SC-002**: At least 70% of expense lines have GL codes pre-populated correctly (requiring no user correction)
- **SC-003**: At least 70% of expense lines have departments pre-populated correctly
- **SC-004**: Users can review and edit a 30-line draft report in under 10 minutes
- **SC-005**: Missing receipts are clearly identifiable - 100% of transactions without matched receipts are visually flagged
- **SC-006**: User corrections improve future suggestions - cache hit rate increases by at least 5% after first month of usage
- **SC-007**: Draft reports include all matched expenses - 100% of receipt-transaction matches for the period appear in the draft

## Assumptions

- Users have already completed receipt uploads and statement imports for the reporting period (Sprints 3-4)
- Receipt-transaction matching has been run and confirmed for the period (Sprint 5)
- Tiered categorization services are operational and accessible (Sprint 6)
- Reference data (GL accounts, departments) is synchronized and available (Sprint 2)
- A reporting period is defined as a calendar month (e.g., "2025-01" for January 2025)
- Users generate reports for themselves only (no delegation or approval workflow in this sprint)
- The justification list for missing receipts is fixed and does not require admin configuration

## Dependencies

- Sprint 5: Matching Engine (ReceiptTransactionMatch entity and services)
- Sprint 6: AI Categorization (CategorizationService, DescriptionNormalizationService, tiered suggestion infrastructure)
- Sprint 2: Reference Data (GLAccounts, Departments tables)
- Sprint 2: Cache Tables (DescriptionCache, VendorAliases, ExpenseEmbeddings)

## Out of Scope

- Excel export and PDF generation (Sprint 9)
- Report approval workflow
- Multi-user delegation
- Recurring/scheduled draft generation
- Report templates beyond the standard format
