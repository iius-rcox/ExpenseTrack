# Feature Specification: Core Backend & Authentication

**Feature Branch**: `002-core-backend-auth`
**Created**: 2025-12-04
**Status**: Draft
**Input**: Sprint 2 from ExpenseFlow Sprint Plan - Core Backend & Auth (Weeks 3-4)

## Clarifications

### Session 2025-12-04

- Q: What authentication provider should be used? → A: Microsoft Entra ID (Azure AD) via JWT tokens
- Q: What background job processor should be used? → A: Hangfire with PostgreSQL storage
- Q: What is the source for GL/Department/Project reference data? → A: Weekly sync from external SQL Server
- Q: How should the external SQL Server connection be established? → A: Managed Identity authentication (passwordless)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Secure API Authentication (Priority: P1)

As an employee accessing the ExpenseFlow system, I need to authenticate using my corporate credentials so that only authorized users can access expense management features and my data is protected.

**Why this priority**: Authentication is the foundation of all security. No other feature can be safely exposed without proper authentication in place. This is the gating requirement for the entire application.

**Independent Test**: Can be fully tested by attempting to access a protected endpoint without authentication (should receive 401), then authenticating with valid corporate credentials and successfully accessing the endpoint (should receive 200).

**Acceptance Scenarios**:

1. **Given** an unauthenticated request to a protected endpoint, **When** no authentication token is provided, **Then** the system returns a 401 Unauthorized response.
2. **Given** a user with valid corporate credentials, **When** they authenticate through the corporate identity provider, **Then** they receive an access token that grants access to protected endpoints.
3. **Given** a user's first successful authentication, **When** the system processes their token, **Then** a user profile is automatically created using claims from the identity token (name, email, department).
4. **Given** an expired or invalid authentication token, **When** the user attempts to access a protected endpoint, **Then** the system returns a 401 response and the user must re-authenticate.

---

### User Story 2 - Cache Tables Foundation (Priority: P1)

As the system, I need cache tables established to store learned patterns from user interactions so that future operations can use cached data instead of expensive AI calls (supporting the Cost-First AI Architecture principle).

**Why this priority**: The cache tables are the foundation for the tiered AI cost optimization strategy. Without these tables, every AI operation would go to expensive Tier 3/4, violating the <$20/month AI cost target.

**Independent Test**: Can be fully tested by inserting test records into each cache table, querying for exact matches, and verifying the caching logic returns cached results on subsequent identical requests.

**Acceptance Scenarios**:

1. **Given** a normalized description entry exists in the description cache, **When** the same raw description is processed again, **Then** the cached normalized version is returned without any external API call.
2. **Given** a vendor alias pattern exists, **When** a transaction description matches that pattern, **Then** the canonical vendor name and default GL code are returned from cache.
3. **Given** a statement fingerprint exists for a user and header combination, **When** the same statement format is imported again, **Then** the saved column mapping is applied automatically.
4. **Given** verified expense embeddings exist, **When** a similar expense description is processed, **Then** the system can find similar entries via vector similarity search.

---

### User Story 3 - Background Job Processing (Priority: P2)

As an administrator, I need a reliable background job processing system so that long-running operations (receipt processing, data sync, report generation) run asynchronously without blocking user interactions.

**Why this priority**: Background processing is essential for user experience but depends on authentication and database being in place. Many features (receipt OCR, report generation) require async processing.

**Independent Test**: Can be fully tested by enqueuing a test job, verifying it appears in the job dashboard, and confirming it executes successfully with logged output.

**Acceptance Scenarios**:

1. **Given** a background job is enqueued, **When** the job processor is running, **Then** the job executes asynchronously and its status can be monitored.
2. **Given** a recurring job is scheduled, **When** the scheduled time arrives, **Then** the job executes automatically.
3. **Given** a job fails during execution, **When** retries are configured, **Then** the job is retried according to the retry policy.
4. **Given** an administrator accesses the job dashboard, **When** they authenticate with admin privileges, **Then** they can view job status, history, and manually trigger jobs.

---

### User Story 4 - Reference Data Synchronization (Priority: P2)

As a user creating expense reports, I need current GL accounts, departments, and project codes available for categorization so that I can properly allocate expenses according to company accounting standards.

**Why this priority**: Reference data is required for expense categorization but is not blocking for authentication or cache table setup. It enables the GL code suggestion features in later sprints.

**Independent Test**: Can be fully tested by triggering a sync job, verifying data is populated in the local tables, and confirming users can query the available GL codes, departments, and projects.

**Acceptance Scenarios**:

1. **Given** the reference data sync job runs, **When** it connects to the external data source, **Then** GL accounts, departments, and project codes are populated in local tables.
2. **Given** reference data exists locally, **When** a user requests available GL codes, **Then** the system returns the current list of valid GL accounts.
3. **Given** the external data source is temporarily unavailable, **When** the sync job runs, **Then** existing local data remains intact and the job logs an error for retry.
4. **Given** GL codes change in the source system, **When** the weekly sync runs, **Then** local data reflects the updated codes.

---

### Edge Cases

- What happens when the identity provider is temporarily unavailable?
  - The system MUST gracefully handle authentication failures and provide clear error messages indicating the issue is with the identity service.
- How does the system handle users who are removed from the corporate directory?
  - Existing sessions should continue until token expiration, but new authentication attempts should fail.
- What happens if the database connection is lost during a background job?
  - Jobs MUST be retried according to retry policy; failed jobs should be visible in the dashboard.
- How does the system handle duplicate description cache entries?
  - Cache lookups use a unique hash constraint; attempts to insert duplicates should update hit_count instead.
- What happens if the external SQL Server for reference data is unreachable?
  - The sync job fails gracefully with logged error; existing local data remains available.

## Requirements *(mandatory)*

### Functional Requirements

**Authentication & Authorization:**
- **FR-001**: System MUST authenticate users via corporate identity provider (Entra ID) using JWT tokens
- **FR-002**: System MUST reject requests without valid authentication tokens with 401 response
- **FR-003**: System MUST automatically create user profiles on first successful authentication using identity token claims
- **FR-004**: System MUST validate token signatures and expiration on every authenticated request
- **FR-005**: System MUST support role-based access control for administrative functions

**Cache Tables:**
- **FR-006**: System MUST provide a description cache that stores raw-to-normalized description mappings
- **FR-007**: System MUST provide a vendor aliases table that maps transaction patterns to canonical vendors with default GL codes
- **FR-008**: System MUST provide a statement fingerprints table that stores column mappings per user and statement source
- **FR-009**: System MUST provide a split patterns table that stores expense allocation rules
- **FR-010**: System MUST provide an expense embeddings table with vector similarity search capability
- **FR-011**: System MUST track hit counts on cache entries to measure cache effectiveness

**Background Processing:**
- **FR-012**: System MUST process long-running operations asynchronously via background jobs
- **FR-013**: System MUST provide a dashboard for administrators to monitor job status and history
- **FR-014**: System MUST support scheduled/recurring jobs for automated tasks
- **FR-015**: System MUST retry failed jobs according to configurable retry policies
- **FR-016**: System MUST persist job state to survive application restarts

**Reference Data:**
- **FR-017**: System MUST synchronize GL accounts, departments, and project codes from external source
- **FR-018**: System MUST run reference data sync on a weekly schedule
- **FR-019**: System MUST provide query endpoints for available GL codes, departments, and projects
- **FR-020**: System MUST preserve existing reference data if sync fails

**Deployment:**
- **FR-021**: System MUST be deployable to the existing AKS cluster
- **FR-022**: System MUST be accessible via HTTPS using the configured ingress

### Key Entities

- **User**: Represents an authenticated employee; stores identity provider ID, email, name, department, and creation timestamp
- **DescriptionCache**: Maps raw transaction descriptions to normalized versions; includes hash for O(1) lookup and hit_count for metrics
- **VendorAlias**: Maps transaction description patterns to canonical vendor names; includes default GL code, department, and match statistics
- **StatementFingerprint**: Stores user-specific column mappings for recurring statement imports; keyed by user and header hash
- **SplitPattern**: Defines expense allocation rules for vendors that require split accounting
- **ExpenseEmbedding**: Stores vector embeddings for expense descriptions with associated GL/department for similarity-based suggestions
- **GLAccount**: Reference data for valid general ledger accounts
- **Department**: Reference data for company departments
- **Project**: Reference data for project codes

## Assumptions

- Microsoft Entra ID (Azure AD) is configured and accessible for the organization
- The external SQL Server containing GL/Department/Project data is accessible from the AKS cluster network via Managed Identity authentication (passwordless)
- PostgreSQL (Supabase) from Sprint 1 is operational with pgvector extension enabled
- Users already have corporate credentials in Entra ID
- The administrator role will be determined by Entra ID group membership or custom claims
- Weekly sync schedule (every Sunday at 2 AM) is acceptable for reference data freshness

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Authenticated requests to protected endpoints MUST complete within 500ms p95 (measured end-to-end from request receipt to response)
- **SC-002**: Unauthenticated requests receive 401 response within 100ms
- **SC-003**: User profile creation on first login completes within 1 second
- **SC-004**: Cache table lookups by hash return results within 50ms
- **SC-005**: Vector similarity searches across 10,000 embeddings return results within 500ms
- **SC-006**: Background jobs appear in the monitoring dashboard within 5 seconds of being enqueued
- **SC-007**: Failed jobs are retried within the configured retry window (default: 5 minutes)
- **SC-008**: Reference data sync completes for 1,000+ GL codes within 2 minutes
- **SC-009**: System remains accessible during reference data sync operations
- **SC-010**: Application deployment to AKS completes within 5 minutes
