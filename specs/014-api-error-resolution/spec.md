# Feature Specification: API Error Resolution

**Feature Branch**: `014-api-error-resolution`
**Created**: 2025-12-22
**Status**: Draft
**Input**: User description: "Error resolution for API 401/404 errors on staging dashboard"

## Problem Statement

The ExpenseFlow staging dashboard (staging.expense.ii-us.com) displays "Failed to load" messages for multiple data components despite successful Microsoft OAuth authentication. Investigation revealed:

1. **401 Unauthorized errors**: Dashboard endpoints (`/api/dashboard/activity`, `/api/dashboard/metrics`) reject requests even after successful OAuth token acquisition
2. **404 Not Found errors**: Certain endpoints (`/api/dashboard/actions`, `/api/analytics/categories`) don't exist on the staging server

This creates a broken user experience where users successfully log in but cannot view any expense data.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Dashboard Metrics (Priority: P1)

As a logged-in user, I want to see my expense metrics on the dashboard so that I can understand my spending at a glance.

**Why this priority**: The dashboard is the primary landing page after login. Without metrics, users see only error messages, making the application appear broken.

**Independent Test**: Can be fully tested by logging into the staging environment and verifying that metric cards display actual data instead of "Failed to load" messages.

**Acceptance Scenarios**:

1. **Given** a user has successfully authenticated via Microsoft OAuth, **When** the dashboard loads, **Then** the metrics cards display expense totals, receipt counts, and pending matches
2. **Given** a user has no expense data, **When** the dashboard loads, **Then** appropriate empty state messages appear (not error messages)
3. **Given** the backend is temporarily unavailable, **When** the dashboard loads, **Then** a user-friendly error message with retry option appears

---

### User Story 2 - View Recent Activity (Priority: P1)

As a logged-in user, I want to see my recent expense activity so that I can track what I've been spending on.

**Why this priority**: Activity stream is essential for users to understand their expense history and verify uploaded receipts.

**Independent Test**: Can be tested by uploading a receipt and verifying it appears in the activity stream on the dashboard.

**Acceptance Scenarios**:

1. **Given** a user has expense activity, **When** viewing the dashboard, **Then** recent transactions appear in chronological order
2. **Given** a user's session is valid, **When** the activity API is called, **Then** the request includes proper authentication headers and succeeds

---

### User Story 3 - Access Analytics Categories (Priority: P2)

As a logged-in user, I want to view my expense categories breakdown so that I can understand where my money goes.

**Why this priority**: Category analytics provides valuable insights but is secondary to basic dashboard functionality.

**Independent Test**: Can be tested by navigating to the analytics section and verifying category data loads.

**Acceptance Scenarios**:

1. **Given** a user has categorized expenses, **When** viewing analytics, **Then** the category breakdown chart displays accurate data
2. **Given** the analytics endpoint is called, **When** the user is authenticated, **Then** the response returns category data or appropriate empty state

---

### User Story 4 - View Pending Actions Queue (Priority: P2)

As a logged-in user, I want to see my pending actions (matches to review, categorization suggestions) so that I can manage my expense workflow.

**Why this priority**: Actions queue helps users complete their expense management tasks but is secondary to viewing data.

**Independent Test**: Can be tested by creating pending matches and verifying they appear in the actions queue.

**Acceptance Scenarios**:

1. **Given** a user has pending matches to review, **When** viewing the dashboard, **Then** the actions queue displays actionable items
2. **Given** the actions endpoint is implemented, **When** called with valid authentication, **Then** it returns pending action items

---

### Edge Cases

- What happens when the OAuth access token expires mid-session? (Should trigger silent refresh or re-authentication prompt)
- How does the system handle network interruptions during API calls? (Should show retry option, not permanent error state)
- What happens when a user has partial data (some endpoints succeed, others fail)? (Should display available data with targeted error messages for failed sections)
- How should the system behave when switching between authenticated and unauthenticated states? (Should clear stale data and redirect appropriately)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST include valid authentication tokens in all API requests to protected endpoints
- **FR-002**: System MUST handle 401 responses by triggering token refresh before retrying the request
- **FR-003**: System MUST implement all dashboard API endpoints referenced by the frontend (`/api/dashboard/activity`, `/api/dashboard/metrics`, `/api/dashboard/actions`, `/api/analytics/categories`)
- **FR-004**: System MUST return appropriate empty responses (not errors) when a user has no data for a given endpoint
- **FR-005**: System MUST validate that the access token audience/scope matches backend API requirements
- **FR-006**: System MUST provide meaningful error responses that the frontend can translate to user-friendly messages
- **FR-007**: Frontend MUST display section-specific error states rather than full-page errors when individual API calls fail
- **FR-008**: System MUST log authentication failures with sufficient detail for debugging (without exposing sensitive tokens)

### Key Entities

- **AccessToken**: The OAuth bearer token used for API authentication; includes audience, scopes, and expiration
- **DashboardMetrics**: Aggregated expense data (totals, counts, trends) for the current user
- **ActivityItem**: Individual expense events (uploads, matches, categorizations) for the activity stream
- **PendingAction**: Actionable items requiring user review (match confirmations, categorization suggestions)
- **CategoryBreakdown**: Expense distribution across categories for analytics views

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All authenticated dashboard API requests succeed with 200 status (zero 401 errors for valid sessions)
- **SC-002**: All frontend dashboard endpoints have corresponding backend implementations (zero 404 errors for documented endpoints)
- **SC-003**: Users can view their dashboard data within 3 seconds of page load
- **SC-004**: Token refresh occurs transparently without requiring user re-authentication during normal session duration
- **SC-005**: When API errors occur, users see actionable error messages with retry options rather than generic "Failed to load" text

## Assumptions

- Microsoft OAuth authentication flow is working correctly (confirmed: token acquisition succeeds with 200 response)
- Backend infrastructure (AKS, database) is operational
- The frontend is correctly configured to use the staging API URL
- Required API endpoints are defined in the existing API specification but may not be fully implemented
- Token audience/scope configuration may have environment-specific differences between development and staging

## Out of Scope

- Changes to the OAuth provider configuration in Azure AD
- New dashboard features beyond fixing existing broken functionality
- Performance optimization of API endpoints
- Mobile-specific error handling (covered by existing responsive design)
