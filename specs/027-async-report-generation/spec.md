# Feature Specification: Async Report Generation

**Feature Branch**: `027-async-report-generation`
**Created**: 2026-01-05
**Status**: Draft
**Input**: User description: "Background job processing for report generation with progress tracking and rate limit handling (Phases 1-5 from production readiness analysis)"

## Clarifications

### Session 2026-01-05

- Q: How long should job history be retained for audit and troubleshooting? → A: 30 days (covers one billing cycle)
- Q: What failure rate threshold should trigger operations alerts? → A: 10% (double the baseline 5% expected failure rate)

## Problem Statement

Currently, expense report generation is handled as a synchronous HTTP request that can take 2-3+ minutes to complete due to:
- AI-powered categorization for each expense line (GL codes, departments)
- AI-powered description normalization
- External API rate limiting causing retries and delays

This causes poor user experience (spinning indicators with no feedback), timeout failures, and unreliable report generation. Users perceive the system as "hung" or broken.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Generate Report with Progress Visibility (Priority: P1)

As an expense submitter, I want to initiate report generation and see real-time progress so that I know the system is working and can estimate when my report will be ready.

**Why this priority**: This is the core problem - users currently wait 2-3 minutes with no feedback, leading to timeout failures and poor UX. This directly addresses the production-readiness blocker.

**Independent Test**: Can be fully tested by triggering report generation and verifying progress updates appear every few seconds, with final report accessible upon completion.

**Acceptance Scenarios**:

1. **Given** I have matched transactions for a billing period, **When** I click "Generate Report", **Then** I see immediate confirmation that generation has started with an estimated completion time
2. **Given** report generation is in progress, **When** I view the reports page, **Then** I see a progress indicator showing "Processing line X of Y" updated every 2-5 seconds
3. **Given** report generation completes successfully, **When** I return to the reports page, **Then** I see the completed report ready for review with all lines categorized
4. **Given** I navigate away from the page during generation, **When** I return, **Then** I can still see the progress or completed report

---

### User Story 2 - Graceful Handling of Processing Delays (Priority: P2)

As an expense submitter, I want the system to handle external service slowdowns gracefully so that my report eventually completes even when AI services are rate-limited.

**Why this priority**: Rate limiting is causing 1000+ retry errors per report. Without graceful handling, reports fail or take excessively long. This is the second most impactful issue.

**Independent Test**: Can be tested by generating a report during high-load periods and verifying it completes (even if slowly) without user-visible errors.

**Acceptance Scenarios**:

1. **Given** external AI services are experiencing rate limits, **When** I generate a report, **Then** the system automatically retries with appropriate delays without failing the entire report
2. **Given** some expense lines fail categorization after all retries, **When** the report completes, **Then** those lines are included with default/fallback values and flagged for manual review
3. **Given** rate limiting is slowing processing, **When** I view progress, **Then** the estimated completion time adjusts dynamically to reflect actual processing speed

---

### User Story 3 - View Generation History and Status (Priority: P3)

As an expense submitter, I want to see a history of my report generation attempts so that I can track what happened if something went wrong.

**Why this priority**: Provides transparency and helps users understand system behavior. Lower priority because P1 and P2 solve the core problems.

**Independent Test**: Can be tested by generating multiple reports and viewing the job history page.

**Acceptance Scenarios**:

1. **Given** I have previously generated reports, **When** I view my generation history, **Then** I see a list of all generation jobs with status, start time, and duration
2. **Given** a generation job failed, **When** I view its details, **Then** I see a user-friendly explanation of what went wrong and suggested next steps
3. **Given** a generation job is queued behind others, **When** I view its status, **Then** I see my position in the queue and estimated start time

---

### User Story 4 - Cancel In-Progress Generation (Priority: P4)

As an expense submitter, I want to cancel a report generation that is taking too long so that I can start fresh or try again later.

**Why this priority**: Nice-to-have for user control, but most users will simply wait. Lower priority.

**Independent Test**: Can be tested by starting generation and clicking cancel, then verifying no partial report is created.

**Acceptance Scenarios**:

1. **Given** report generation is in progress, **When** I click "Cancel Generation", **Then** processing stops within 10 seconds and no partial report is created
2. **Given** I cancel a generation, **When** I view my history, **Then** I see the job marked as "Cancelled by user" with the reason

---

### User Story 5 - Pre-warmed Categorization Cache (Priority: P5)

As an expense submitter, I want my frequently-used vendors and categories to be instantly recognized so that report generation is faster for my typical expenses.

**Why this priority**: Optimization that improves speed for repeat users. Lower priority because it's an enhancement, not a blocker.

**Independent Test**: Can be tested by generating a report for the same vendors twice and measuring the second is significantly faster.

**Acceptance Scenarios**:

1. **Given** I regularly expense from the same vendors, **When** I generate a new report, **Then** those vendors are categorized instantly without waiting for AI processing
2. **Given** new vendor aliases are learned, **When** the nightly cache warming runs, **Then** my future reports benefit from pre-computed categorizations

---

### Edge Cases

- What happens when generation fails mid-way through processing? → Partial results are saved; user can retry to complete remaining lines
- What happens if the user logs out during generation? → Processing continues in background; results available when user returns
- What happens if the same period is generated twice simultaneously? → Second request is rejected with message to wait for first to complete
- What happens if no transactions exist for the selected period? → Immediate feedback with no background job created
- What happens if the background job system is unavailable? → Graceful degradation message asking user to try again later

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST process report generation asynchronously in a background job, returning immediately to the user
- **FR-002**: System MUST provide real-time progress updates showing current line number and total lines
- **FR-003**: System MUST persist job status so users can navigate away and return to check progress
- **FR-004**: System MUST implement exponential backoff with jitter for external API rate limit errors
- **FR-005**: System MUST limit retry attempts per line to prevent infinite loops (max 3 retries per line)
- **FR-006**: System MUST fall back to original descriptions and flag lines when AI categorization fails after all retries
- **FR-007**: System MUST prevent duplicate generation jobs for the same user and billing period
- **FR-008**: System MUST support job cancellation that stops processing within 10 seconds
- **FR-009**: System MUST retain job history for 30 days for audit and troubleshooting purposes
- **FR-010**: System MUST update estimated completion time based on actual processing speed
- **FR-011**: System MUST run a nightly job to pre-compute categorizations for recently-imported transactions
- **FR-012**: System MUST alert operations team when job failure rate exceeds 10% over a rolling 1-hour window

### Key Entities

- **ReportGenerationJob**: Represents a background job for generating an expense report. Tracks user, billing period, status (queued/processing/completed/failed/cancelled), progress (lines processed/total), timestamps, and error information.
- **JobProgress**: Real-time progress information for an active job, including current line, total lines, estimated completion time, and processing rate.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users receive immediate feedback (under 2 seconds) when initiating report generation
- **SC-002**: Progress updates are visible to users within 5 seconds of any line being processed
- **SC-003**: 95% of reports complete successfully without user-visible errors, even during rate limiting
- **SC-004**: Users can navigate away and return without losing visibility into job status
- **SC-005**: Report generation for 300 expense lines completes within 5 minutes under normal conditions
- **SC-006**: Report generation for 300 expense lines completes within 10 minutes during heavy rate limiting
- **SC-007**: Cached vendors are categorized in under 100ms per line (no AI call required)
- **SC-008**: Zero HTTP 499 (client timeout) errors for report generation requests

## Assumptions

- The existing Hangfire background job infrastructure is available and configured
- The current AI categorization and normalization services will be reused with added retry logic
- Users have stable browser sessions and can poll for updates (no WebSocket requirement for MVP)
- Rate limiting is the primary cause of slow processing, not database or compute constraints
- The nightly cache warming job can run during low-usage hours without impacting system performance

## Out of Scope

- Real-time push notifications (WebSocket/SignalR) - polling is sufficient for MVP
- Batch processing multiple periods in a single job
- Priority queuing based on user tier or subscription level
- Mobile app notifications for job completion
- Automatic retry of failed jobs without user action
