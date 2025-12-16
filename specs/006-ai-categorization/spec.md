# Feature Specification: AI Categorization (Tiered)

**Feature Branch**: `006-ai-categorization`
**Created**: 2025-12-16
**Status**: Draft
**Input**: Sprint 6 from ExpenseFlow Sprint Plan - Tiered GL code and department suggestions using cache, embeddings, and GPT-4o-mini

## Clarifications

### Session 2025-12-16

- Q: What happens when AI service rate limits are exhausted or service is down for extended periods? → A: Allow user to skip AI suggestion and manually categorize (Option B)
- Q: What is the retention policy for unverified embeddings? → A: Auto-purge unverified embeddings older than 6 months (Option B)

## Overview

This feature implements a cost-optimized, tiered approach for suggesting GL codes and departments for expense transactions. The system prioritizes cached results over AI-generated suggestions, minimizing API costs while maintaining high accuracy. The three-tier hierarchy is: Tier 1 (cache/vendor aliases) → Tier 2 (embedding similarity) → Tier 3 (GPT-4o-mini inference).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Description Normalization (Priority: P1)

A finance user reviews an expense transaction with a cryptic bank description like "DELTA AIR 0062363598531" and the system automatically normalizes it to a human-readable format like "Delta Airlines - Flight Purchase" without requiring AI calls for previously seen descriptions.

**Why this priority**: Description normalization is foundational - it improves data quality for all downstream features (matching, categorization, reporting) and directly addresses user pain points with unreadable bank descriptions. Cost savings are immediate as most descriptions repeat monthly.

**Independent Test**: Can be fully tested by uploading a statement with known descriptions and verifying normalized outputs appear correctly, delivering cleaner expense data immediately.

**Acceptance Scenarios**:

1. **Given** a transaction with a raw description that exists in the description cache, **When** the system processes the transaction, **Then** the normalized description is retrieved from cache (Tier 1) with zero AI calls.
2. **Given** a transaction with a raw description NOT in the cache, **When** the system processes the transaction, **Then** the AI normalizes the description (Tier 3) and the result is stored in cache for future use.
3. **Given** a previously unseen description that gets normalized, **When** the same description appears in a future transaction, **Then** the cached normalization is used (Tier 1) instead of calling AI again.

---

### User Story 2 - GL Code Suggestion (Priority: P1)

A finance user categorizing an expense receives an intelligent GL code suggestion based on the vendor and expense description. The system first checks if the vendor has a default GL code, then looks for similar past expenses, and only calls AI as a last resort.

**Why this priority**: GL code accuracy directly impacts financial reporting and audit compliance. The tiered approach ensures consistent categorization while dramatically reducing AI costs for routine expenses.

**Independent Test**: Can be tested by creating expense lines with known vendors and verifying appropriate GL codes are suggested with correct tier attribution.

**Acceptance Scenarios**:

1. **Given** a transaction from a vendor with a known default GL code in the vendor alias table, **When** the user requests GL suggestions, **Then** the system returns the default GL code with high confidence (Tier 1).
2. **Given** a transaction with no vendor alias but similar verified expenses exist, **When** the user requests GL suggestions, **Then** the system returns GL codes from similar expenses using embedding similarity (Tier 2).
3. **Given** a transaction with no vendor alias and no similar verified expenses, **When** the user requests GL suggestions, **Then** the system calls AI to infer the appropriate GL code (Tier 3) based on the expense description and available GL account options.
4. **Given** any GL suggestion is displayed, **When** the user views the suggestion, **Then** the tier used (1, 2, or 3) and confidence score are clearly indicated.

---

### User Story 3 - Department Suggestion (Priority: P2)

A finance user categorizing an expense receives an intelligent department suggestion following the same tiered approach as GL codes. This helps maintain consistent departmental allocation across similar expenses.

**Why this priority**: Department allocation is important for cost center tracking but slightly less critical than GL codes for financial accuracy. The same tiered infrastructure can serve both needs.

**Independent Test**: Can be tested by creating expense lines for known vendors with default departments and verifying correct suggestions appear.

**Acceptance Scenarios**:

1. **Given** a transaction from a vendor with a default department in the vendor alias table, **When** the user requests department suggestions, **Then** the system returns the default department with high confidence (Tier 1).
2. **Given** a transaction with similar verified expenses containing department assignments, **When** the user requests department suggestions, **Then** the system returns departments from similar expenses (Tier 2).
3. **Given** a transaction with no cached department information, **When** the user requests department suggestions, **Then** the system calls AI to infer the appropriate department (Tier 3).

---

### User Story 4 - User Selection Creates Verified Embeddings (Priority: P2)

When a user confirms or corrects a GL code or department suggestion, the system stores this as a verified embedding that improves future suggestions. This creates a learning loop where user feedback improves accuracy over time.

**Why this priority**: The learning mechanism is essential for improving Tier 2 accuracy over time and reducing reliance on expensive Tier 3 calls.

**Independent Test**: Can be tested by making categorization selections and then checking that similar new expenses receive the selected values as suggestions.

**Acceptance Scenarios**:

1. **Given** a user selects or confirms a GL code for an expense, **When** the selection is saved, **Then** the system creates a verified embedding record linking the normalized description to the chosen GL code.
2. **Given** a new expense with a description similar to a previously verified expense, **When** the user requests GL suggestions, **Then** the verified expense appears in Tier 2 similarity results.
3. **Given** multiple verified embeddings exist for similar descriptions, **When** Tier 2 suggestions are generated, **Then** the most similar verified embedding's categorization is suggested first.

---

### User Story 5 - Cost Optimization Monitoring (Priority: P3)

System administrators can monitor the tier usage metrics to understand cost optimization effectiveness and identify opportunities to improve cache hit rates.

**Why this priority**: While not user-facing, monitoring is essential for validating the cost optimization strategy and identifying when cache warming or alias updates are needed.

**Independent Test**: Can be tested by processing a batch of transactions and reviewing tier usage statistics in the monitoring dashboard.

**Acceptance Scenarios**:

1. **Given** categorization operations occur throughout the day, **When** an admin views tier usage metrics, **Then** they see counts and percentages for Tier 1, 2, and 3 usage per operation type.
2. **Given** a high proportion of Tier 3 calls for a specific vendor, **When** an admin reviews the metrics, **Then** they can identify candidates for manual vendor alias creation.

---

### User Story 6 - Categorization UI (Priority: P2)

A finance user views an interface showing categorization suggestions for their expenses, including the suggested GL code, department, confidence score, and which tier provided the suggestion. The user can accept suggestions or select alternatives.

**Why this priority**: The UI is essential for users to interact with suggestions and provide feedback that improves the learning system.

**Independent Test**: Can be tested by navigating to the categorization interface and verifying suggestions display correctly with all metadata.

**Acceptance Scenarios**:

1. **Given** a user opens the categorization interface, **When** they view an expense, **Then** they see the suggested GL code, department, confidence score, and tier indicator.
2. **Given** a user disagrees with a suggestion, **When** they select an alternative GL code or department, **Then** the system accepts their selection and creates a verified embedding.
3. **Given** the suggestion tier is displayed, **When** the user hovers or clicks for details, **Then** they see an explanation of what the tier means (e.g., "From vendor default", "From similar expenses", "AI suggestion").

---

### Edge Cases

- What happens when the GL account list is empty or not synced? System should return an error prompting admin to sync GL accounts before categorization is available.
- How does the system handle descriptions that are empty or contain only numbers? System should skip normalization and flag the transaction for manual review.
- What happens when embedding generation fails? System should fall back to Tier 3 (AI inference) directly and log the embedding failure.
- How does the system handle very long descriptions (>500 characters)? System should truncate to 500 characters before processing.
- What happens when the AI service is unavailable or rate-limited? System should attempt retry with exponential backoff; if service remains unavailable, allow user to skip AI suggestion and manually categorize without blocking their workflow.
- How does the system handle multiple similar verified embeddings with conflicting GL codes? System should return the highest-confidence match as primary suggestion and list alternatives.
- What happens when a user categorizes the same vendor differently on different occasions? System should track frequency and suggest the most commonly used categorization.
- How does the system handle transactions with no vendor information? System should use description-only matching for Tier 2 and Tier 3 without vendor context.

## Requirements *(mandatory)*

### Functional Requirements

**Description Normalization**

- **FR-001**: System MUST check the description cache before calling AI for normalization.
- **FR-002**: System MUST store successfully normalized descriptions in the cache with a hash of the raw description as the lookup key.
- **FR-003**: System MUST increment the hit count on cache entries when they are retrieved.
- **FR-004**: System MUST preserve the original raw description alongside the normalized version.

**GL Code Suggestion**

- **FR-005**: System MUST check vendor aliases for a default GL code before using embedding similarity (Tier 1 → Tier 2 order).
- **FR-006**: System MUST use embedding similarity search when vendor alias has no default GL code.
- **FR-007**: System MUST only call AI inference (Tier 3) when Tier 1 and Tier 2 produce no results above the confidence threshold.
- **FR-008**: System MUST return the tier used (1, 2, or 3) with every GL suggestion.
- **FR-009**: System MUST return a confidence score (0.00-1.00) with every GL suggestion.
- **FR-010**: System MUST support retrieving GL suggestions for a specific transaction by ID.

**Department Suggestion**

- **FR-011**: System MUST apply the same tiered approach for department suggestions as for GL codes.
- **FR-012**: System MUST support independent GL and department suggestions (they may come from different tiers).

**Embedding Management**

- **FR-013**: System MUST generate embeddings for expense descriptions when cache misses occur.
- **FR-014**: System MUST perform similarity search against the embedding database with a configurable similarity threshold (default: 0.92).
- **FR-015**: System MUST mark embeddings as "verified" when created from user-confirmed categorizations.
- **FR-016**: System MUST prioritize verified embeddings over unverified ones in similarity results.

**User Feedback Loop**

- **FR-017**: System MUST create a verified embedding when a user confirms or selects a categorization.
- **FR-018**: System MUST update vendor alias default GL/department when user consistently categorizes a vendor the same way (3+ times with same categorization).

**Data Retention**

- **FR-026**: System MUST auto-purge unverified embeddings older than 6 months to maintain search performance.
- **FR-027**: System MUST retain verified embeddings indefinitely (no automatic purge).

**Graceful Degradation**

- **FR-019**: System MUST allow users to skip AI-based suggestions and manually categorize when Tier 3 service is unavailable or rate-limited.
- **FR-020**: System MUST NOT block user workflow when external AI services fail; manual categorization remains available.

**Metrics and Logging**

- **FR-021**: System MUST log the tier used for every categorization operation with timestamp and operation type.
- **FR-022**: System MUST provide an endpoint to retrieve tier usage statistics by date range and operation type.

**User Interface**

- **FR-023**: System MUST display categorization suggestions with GL code, department, confidence score, and tier indicator.
- **FR-024**: System MUST allow users to accept, modify, or reject suggestions.
- **FR-025**: System MUST provide explanatory text for each tier level on user request.

### Key Entities

- **DescriptionCache**: Stores raw-to-normalized description mappings with lookup hash, hit count, and timestamps. Used for Tier 1 description normalization.
- **ExpenseEmbedding**: Vector representation of expense descriptions with associated GL code, department, verification status, and embedding vector. Used for Tier 2 similarity search. Unverified embeddings auto-purge after 6 months; verified embeddings retained indefinitely.
- **VendorAlias**: Canonical vendor names with pattern matching, default GL code, default department, and usage tracking. Extended for Tier 1 categorization defaults.
- **TierUsageLog**: Records of which tier was used for each categorization operation with timestamps, operation type, and transaction reference.
- **GLAccount**: General ledger accounts synced from SQL Server. Reference data for valid GL code options.
- **Department**: Departments synced from SQL Server. Reference data for valid department options.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 50% of description normalizations are served from cache (Tier 1) within the first month of operation.
- **SC-002**: At least 70% of GL code suggestions come from Tier 1 or Tier 2 (non-AI) sources after 3 months of user feedback.
- **SC-003**: Users can retrieve categorization suggestions in under 2 seconds for Tier 1/2 and under 5 seconds for Tier 3.
- **SC-004**: System tracks and reports tier usage with 100% accuracy (every operation logged).
- **SC-005**: User-confirmed categorizations improve Tier 2 accuracy, measured by increasing Tier 2 hit rates month-over-month.
- **SC-006**: Monthly AI costs remain under $40 as measured by tier usage metrics and per-call cost calculations.
- **SC-007**: 90% of users accept or use categorization suggestions (measured via acceptance rate).
- **SC-008**: Users can complete categorization for a single expense in under 30 seconds.

## Assumptions

- GL accounts, departments, and projects are already synced from SQL Server (Sprint 2, Task 2.10).
- Vendor aliases table exists with pattern matching capability (Sprint 2, Task 2.5).
- Expense embeddings table with vector similarity search is operational (Sprint 2, Task 2.8).
- Users have completed receipt matching (Sprint 5) before categorization.
- Embedding similarity threshold will start at 0.92 and be tuned based on accuracy metrics.
- The AI service for Tier 3 supports rate limiting and retry with exponential backoff.
- Users have Entra ID authentication and proper authorization to access their own transactions.

## Dependencies

- **Sprint 2**: DescriptionCache, VendorAliases, ExpenseEmbeddings tables must be created.
- **Sprint 2**: GL accounts sync from SQL Server must be operational.
- **Sprint 5**: Matched receipt-transaction pairs available for categorization.
- **External**: AI service (GPT-4o-mini) access for Tier 3 inference.
- **External**: Embedding generation service for vector creation.

## Out of Scope

- Bulk categorization UI (deferred to Sprint 8 draft report generation).
- Auto-categorization without user confirmation.
- Custom embedding models or fine-tuning.
- Real-time AI inference during data import (categorization happens on-demand).
- Batch processing of historical data (handled in Sprint 10 cache warming).
