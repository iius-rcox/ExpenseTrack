# Feature Specification: Missing API Endpoints Implementation

**Feature Branch**: `021-missing-api-endpoints`
**Created**: 2026-01-01
**Status**: Draft
**Input**: User description: "Implement missing endpoints identified by contract tests"

## Overview

Contract tests identified 17 endpoints that were either not implemented or had different paths than expected. Upon analysis, many are **naming convention differences** (e.g., `/trends` vs `/spending-trend`) rather than missing functionality. This specification addresses the **genuinely missing endpoints** that would add new capabilities.

### Endpoint Classification

| Category | Contract Test Path | Actual Path | Status |
|----------|-------------------|-------------|--------|
| Analytics | `/spending-summary` | `/categories` | ✅ Different name, same function |
| Analytics | `/category-breakdown` | `/spending-by-category` | ✅ Different name, same function |
| Analytics | `/trends` | `/spending-trend` | ✅ Different name, same function |
| Analytics | `/vendor-insights` | `/spending-by-vendor` + `/merchants` | ✅ Different name, same function |
| Analytics | `/budget-comparison` | `/comparison` | ✅ Different name, same function |
| Analytics | `/export` | N/A | **❌ Missing** |
| Reports | `POST /` | `POST /draft` | ✅ Different workflow |
| Reports | `/{id}/generate` | N/A | **❌ Missing** (finalize report) |
| Reports | `/{id}/pdf` | `/{id}/export/receipts` | ✅ Different name |
| Reports | `/{id}/submit` | N/A | **❌ Missing** (submit for approval) |
| Reports | `/{id}/items` | Included in `GET /{id}` | ✅ Different design |
| Receipts | `/{id}/image` | `/{id}/download` | ✅ Different name |
| Receipts | `/{id}/reprocess` | `/{id}/retry` | ✅ Different name |
| Transactions | `POST /` | N/A | ⚠️ By design (import-only) |
| Transactions | `PUT /{id}` | N/A | ⚠️ By design (immutable) |
| Transactions | `/{id}/categorize` | `/categorization/transactions/{id}/confirm` | ✅ Different controller |

**Legend**: ✅ Exists with different path | **❌ Missing** | ⚠️ Intentionally not implemented

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Export Analytics Data (Priority: P2)

As a user reviewing my expenses, I need to export analytics data (spending trends, category breakdowns) to CSV or Excel format so that I can perform custom analysis or share reports with accountants.

**Why this priority**: Analytics export provides value for users who need to process data in external tools or share with stakeholders. It complements the existing report export functionality but operates at the analytics level.

**Independent Test**: Can be tested by requesting an analytics export with date range and format parameters, verifying the downloaded file contains the expected data structure.

**Acceptance Scenarios**:

1. **Given** I have expense data in my account, **When** I request an analytics export with CSV format, **Then** I receive a properly formatted CSV file with spending data for the specified date range
2. **Given** I have expense data in my account, **When** I request an analytics export with Excel format, **Then** I receive an Excel workbook with multiple sheets (trends, categories, vendors)
3. **Given** I request an export for a date range with no data, **When** the export is generated, **Then** I receive an empty file with headers only (not an error)

---

### User Story 2 - Finalize Draft Report (Priority: P1)

As a user preparing expense reports, I need to finalize a draft report to lock its contents and mark it as ready for review, so that I can track which reports are complete and prevent accidental edits.

**Why this priority**: The finalize/generate step is critical for the expense report workflow. Users need a clear transition from "draft" to "ready for review" states.

**Independent Test**: Can be tested by creating a draft report, calling the generate endpoint, and verifying the report status changes to "Generated" and becomes read-only.

**Acceptance Scenarios**:

1. **Given** I have a draft report with at least one expense line, **When** I call the generate endpoint, **Then** the report status changes to "Generated" and the generation timestamp is recorded
2. **Given** I have a draft report with validation errors, **When** I call the generate endpoint, **Then** I receive a 400 error listing the validation issues (e.g., missing categories, zero/negative amounts, missing receipts)
3. **Given** I have an already-generated report, **When** I call the generate endpoint again, **Then** I receive a 409 Conflict indicating the report is already finalized
4. **Given** I have a generated report, **When** I try to update a line item, **Then** I receive a 400 error indicating the report is locked

---

### User Story 3 - Submit Report for Tracking (Priority: P3)

As a user submitting expense reports, I need to mark a finalized report as submitted so that I have an audit trail and can track which reports are complete.

**Why this priority**: While report submission is important for a complete workflow, the current system primarily generates reports for export. Approval workflow can be added incrementally.

**Independent Test**: Can be tested by generating a report and calling the submit endpoint, verifying the report status changes to "Submitted".

**Acceptance Scenarios**:

1. **Given** I have a generated report, **When** I call the submit endpoint, **Then** the report status changes to "Submitted" and the submission timestamp is recorded
2. **Given** I have a draft report that hasn't been generated, **When** I call the submit endpoint, **Then** I receive a 400 error indicating the report must be generated first
3. **Given** I have an already-submitted report, **When** I call the submit endpoint again, **Then** I receive a 409 Conflict indicating the report is already submitted

---

### Edge Cases

- What happens when exporting analytics for extremely large date ranges? → Enforce maximum date range (5 years) and stream large exports
- How does report generation handle reports with zero expense lines? → Return 400 Bad Request; reports must have at least one line
- What happens if a receipt is deleted while attached to a pending report? → Keep receipt reference but mark as "orphaned" in report
- How does the system handle concurrent generate requests for the same report? → Use optimistic concurrency; first request wins, second receives 409

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide analytics export endpoint at `GET /api/analytics/export` accepting startDate, endDate, format (csv/xlsx), and sections (comma-separated list: trends, categories, vendors, transactions) parameters
- **FR-002**: Analytics export MUST include only the sections requested by the user; if no sections specified, default to all aggregated summaries (trends, categories, vendors)
- **FR-003**: System MUST provide report generation endpoint at `POST /api/reports/{id}/generate` to finalize draft reports
- **FR-004**: Report generation MUST validate all expense lines have: (1) assigned category, (2) amount > $0, and (3) attached receipt before finalizing
- **FR-005**: Generated reports MUST be immutable (return 400 for any modification attempts)
- **FR-006**: System MUST provide report submission endpoint at `POST /api/reports/{id}/submit` for tracking/audit purposes (status change only; no approval workflow)
- **FR-007**: Report submission MUST only be allowed for reports in "Generated" status
- **FR-008**: All new endpoints MUST require authentication and filter data by authenticated user
- **FR-009**: All new endpoints MUST return consistent error responses using ProblemDetails format
- **FR-010**: Export endpoints MUST support Content-Disposition header for browser downloads
- **FR-011**: Report status transitions MUST be logged for audit trail

### Key Entities *(include if feature involves data)*

- **ReportStatus** (existing enum): Add `Generated` and `Submitted` values
- **AnalyticsExportRequest**: Contains startDate, endDate, format (csv/xlsx), sections (array of: trends, categories, vendors, transactions)
- **ReportValidationResult**: Contains isValid, errors list, warnings list

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Analytics export endpoint returns properly formatted CSV/XLSX files within 10 seconds for 12-month date ranges
- **SC-002**: Report generation validates all required fields and returns descriptive errors for invalid reports
- **SC-003**: Generated reports correctly reject modification attempts with appropriate 400 responses
- **SC-004**: All 17 contract tests pass (either by implementing missing endpoints or updating contract tests to match actual paths)
- **SC-005**: New endpoints are documented in OpenAPI specification with proper response types

## Scope & Boundaries

### In Scope

- Analytics export endpoint with CSV and Excel format support
- Report generation (finalize) endpoint with validation
- Report submission endpoint with status tracking
- Update contract tests to reflect actual endpoint paths for naming differences

### Out of Scope

- Approval workflow beyond basic submission (approver assignment, approval/rejection)
- Email notifications for report status changes
- Scheduled/automated report generation
- Analytics export scheduling or email delivery
- Transaction creation/update endpoints (intentionally not supported per import-only design)

## Implementation Priority

Given the analysis above, the recommended implementation order is:

1. **Phase 1: Update Contract Tests** (Quick Win)
   - Update contract tests to use actual endpoint paths
   - This will make 12+ tests pass immediately with no code changes

2. **Phase 2: Report Generation Endpoint** (P1)
   - Add `POST /api/reports/{id}/generate` endpoint
   - Add validation logic for draft reports
   - Update ReportStatus enum if needed

3. **Phase 3: Analytics Export** (P2)
   - Add `GET /api/analytics/export` endpoint
   - Implement CSV export using CsvHelper
   - Implement Excel export using ClosedXML

4. **Phase 4: Report Submission** (P3)
   - Add `POST /api/reports/{id}/submit` endpoint
   - Add basic workflow status tracking

## Clarifications

### Session 2026-01-01

- Q: Should analytics export include raw transaction data or only aggregated summaries? → A: Selectable sections via query parameter (user chooses which to include)
- Q: What validation rules apply when generating a report? → A: Strict validation (each line requires category, amount > $0, AND attached receipt)
- Q: Is an approval workflow needed, or is submission just for tracking/audit purposes? → A: Tracking/audit only (status change to "Submitted" for audit trail; no approval actions)
