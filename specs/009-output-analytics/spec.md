# Feature Specification: Output Generation & Analytics

**Feature Branch**: `009-output-analytics`
**Created**: 2025-12-16
**Status**: Draft
**Input**: User description: "Sprint 9: Output Generation & Analytics - Excel export matching AP template, PDF receipts with placeholders, MoM dashboard"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Export Expense Report to Excel (Priority: P1)

As a user submitting expenses for reimbursement, I need to export my expense report to an Excel file that exactly matches the AP department's required template format, so that my submission is accepted without manual reformatting.

**Why this priority**: The primary deliverable of ExpenseFlow is generating expense reports for reimbursement. Without Excel export in the correct format, users cannot submit their reports to AP, making all other features useless.

**Independent Test**: Can be tested by generating an Excel file from a draft report with multiple expense lines and verifying it matches the AP template structure with functional formulas.

**Acceptance Scenarios**:

1. **Given** a finalized expense report with 10 expense lines, **When** the user requests Excel export, **Then** an Excel file is generated with all lines in the correct template format within 5 seconds
2. **Given** an expense report with calculated totals, **When** exported to Excel, **Then** all formulas (e.g., `=IF(ISBLANK...)`) are preserved and functional, not replaced with static values
3. **Given** an expense report for January 2025, **When** exported to Excel, **Then** the header section displays the employee name and period correctly
4. **Given** expense lines with dates, GL codes, departments, descriptions, units, and amounts, **When** exported, **Then** each column maps to the exact position required by the AP template

---

### User Story 2 - Download Consolidated Receipt PDF (Priority: P1)

As a user submitting expenses, I need to download a single PDF containing all receipts in the order they appear on my expense report, so that I can provide supporting documentation with my Excel submission.

**Why this priority**: AP requires receipts as supporting documentation. Users need both Excel report and receipt PDF together for a complete submission.

**Independent Test**: Can be tested by generating a PDF from an expense report with multiple receipts and verifying the correct ordering and content.

**Acceptance Scenarios**:

1. **Given** an expense report with 8 matched receipts, **When** the user downloads the receipt PDF, **Then** a single PDF file is generated containing all 8 receipts in expense line order
2. **Given** a multi-page receipt (e.g., hotel folio), **When** included in the PDF, **Then** all pages of that receipt are kept together in sequence
3. **Given** receipts of varying formats (PDF, JPG, PNG), **When** consolidated, **Then** all formats are converted and included as PDF pages

---

### User Story 3 - Generate Missing Receipt Placeholders (Priority: P1)

As a user with expenses that lack receipts, I need placeholder pages generated for each missing receipt so that my receipt PDF is complete and AP knows which items require justification.

**Why this priority**: Many legitimate expenses (digital subscriptions, small purchases) don't have receipts. Without placeholders, the receipt PDF wouldn't match the expense report line count.

**Independent Test**: Can be tested by creating an expense report with missing receipts and verifying placeholder pages are generated with correct information.

**Acceptance Scenarios**:

1. **Given** an expense line without a matched receipt, **When** the receipt PDF is generated, **Then** a placeholder page is created showing: expense date, vendor name, amount, and description
2. **Given** a missing receipt requiring justification, **When** the placeholder is generated, **Then** it includes a justification field with the user's selected reason
3. **Given** justification options, **When** a user selects one, **Then** the options include: "Not provided by vendor", "Lost", "Digital subscription - no receipt issued", and "Other (custom text)"

---

### User Story 4 - View Month-over-Month Spending Comparison (Priority: P2)

As a user reviewing my expenses, I need to compare my current month's spending against the previous month to identify new vendors, missing recurring charges, and significant changes.

**Why this priority**: Helps users catch errors (missed subscriptions, unusual charges) before submitting reports. Important for accuracy but not blocking submission.

**Independent Test**: Can be tested by comparing two months of expense data and verifying the anomaly detection identifies known differences.

**Acceptance Scenarios**:

1. **Given** expense data for January 2025 and December 2024, **When** the user views the comparison, **Then** they see total amounts, percentage change, and direction indicator for each month
2. **Given** a new vendor appearing in January that wasn't in December, **When** displayed in "New Vendors" section, **Then** it shows the vendor name and amount
3. **Given** a recurring vendor (e.g., Adobe) present in December but missing in January, **When** displayed in "Missing Recurring" section, **Then** it shows the vendor name and expected amount
4. **Given** a vendor with spending change exceeding 50%, **When** displayed in "Significant Changes" section, **Then** it shows current amount, previous amount, and percentage change

---

### User Story 5 - View Cache Performance Statistics (Priority: P3)

As an administrator monitoring system costs, I need to see cache hit rates and tier usage statistics so that I can verify the system is staying within AI cost targets.

**Why this priority**: Cost monitoring is important for ongoing operations but not required for core expense report generation functionality.

**Independent Test**: Can be tested by processing expenses through the tiered system and verifying statistics accurately reflect actual tier usage.

**Acceptance Scenarios**:

1. **Given** expense processing that used various tiers, **When** viewing cache stats, **Then** the dashboard shows hit rates for Tier 1 (cache), Tier 2 (embeddings), and Tier 3 (AI)
2. **Given** a target of 50%+ Tier 1 hit rate, **When** the rate falls below threshold, **Then** the dashboard displays a warning indicator
3. **Given** AI API usage generating costs, **When** viewing the dashboard, **Then** estimated monthly AI costs are displayed based on current usage patterns

---

### Edge Cases

- What happens when exporting a report with zero expense lines?
- How does PDF generation handle a receipt file that is corrupted or unreadable?
- What happens when comparing months where one has no expense data?
- How does the system handle Excel export when the AP template structure changes?
- What happens if PDF generation exceeds memory limits with 100+ receipts?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST generate Excel files that match the AP department's required template structure with columns: Expense Date, GL Acct/Job, Dept/Phas, Expense Description, Units/Mileage, Rate/Amount, Expense Total
- **FR-002**: System MUST preserve Excel formulas in exported files (e.g., `=IF(ISBLANK(E14),"",E14*F14)`) rather than converting to static values
- **FR-003**: System MUST include employee name and report period in the Excel header section
- **FR-004**: System MUST consolidate all receipt images and PDFs into a single PDF document
- **FR-005**: System MUST order receipts in the consolidated PDF to match the expense line sequence
- **FR-006**: System MUST generate placeholder pages for expenses without matched receipts
- **FR-007**: System MUST include on placeholder pages: expense date, vendor name, amount, description, and justification
- **FR-008**: System MUST provide justification options: "Not provided by vendor", "Lost", "Digital subscription - no receipt issued", "Other"
- **FR-009**: System MUST calculate month-over-month comparison including total amounts, percentage change, and variance direction
- **FR-010**: System MUST identify new vendors (appearing in current month but not previous)
- **FR-011**: System MUST identify missing recurring charges (present in previous month but not current, for vendors seen 2+ consecutive months)
- **FR-012**: System MUST highlight significant spending changes (>50% variance for same vendor)
- **FR-013**: System MUST track and display cache tier hit rates (Tier 1, Tier 2, Tier 3)
- **FR-014**: System MUST calculate and display estimated AI costs based on tier usage

### Key Entities

- **ExpenseReport**: Represents a complete expense submission for a given period, containing metadata (period, status, employee) and associated expense lines
- **ExpenseLine**: Individual expense item with date, GL code, department, description, amount, and receipt linkage
- **Receipt**: Supporting document for an expense, including file location, extraction status, and match status
- **MissingReceiptJustification**: User-provided reason for why a receipt is unavailable, linked to an expense line
- **MonthlyComparison**: Calculated comparison between two periods showing totals, new vendors, missing recurring, and significant changes
- **TierUsageStatistics**: Aggregated metrics showing cache hit rates and AI usage patterns over time

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can generate an Excel export in under 5 seconds for reports with up to 50 expense lines
- **SC-002**: 100% of Excel exports match the exact AP template structure and pass AP department validation
- **SC-003**: Users can generate a consolidated receipt PDF in under 30 seconds for reports with up to 50 receipts
- **SC-004**: Receipt PDF file size averages under 20MB for typical monthly reports (10-30 receipts)
- **SC-005**: Missing receipt placeholders contain all required information, reducing AP clarification requests by 80%
- **SC-006**: Month-over-month comparison identifies 95%+ of new vendors and missing recurring charges
- **SC-007**: Cache statistics dashboard displays within 2 seconds and refreshes without page reload
- **SC-008**: Cache statistics dashboard displays estimated monthly AI costs; system targets <$40/month during initial adoption phase (constitution target: <$20/month at steady state Month 6+)

## Assumptions

- The AP template structure is stable and provided as a reference file
- Receipt images are stored in a supported format (PDF, JPG, PNG, HEIC) in blob storage
- Historical expense data exists for month-over-month comparison (at minimum 1 previous month)
- Users have appropriate permissions to access their own expense reports and cache statistics (for admins)
- The existing tiered categorization system logs tier usage metrics that can be aggregated for the dashboard
