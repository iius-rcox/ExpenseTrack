# Feature Specification: Analytics Dashboard API Endpoints

**Feature Branch**: `019-analytics-dashboard`
**Created**: 2025-12-31
**Status**: Draft
**Input**: User description: "Implement analytics dashboard with spending trends, category breakdowns, merchant insights, and subscription tracking"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Spending Trends Over Time (Priority: P1)

As a user reviewing my expenses, I need to see how my spending changes over time with different granularities (daily, weekly, monthly), so that I can identify patterns and anomalies in my expense behavior.

**Why this priority**: Spending trends are the core analytics feature that provides the foundation for all other insights. Users need to see their spending patterns to make informed decisions about expense management.

**Independent Test**: Can be tested by requesting spending trends for a date range and verifying the aggregated data matches transaction totals.

**Acceptance Scenarios**:

1. **Given** expenses exist for the last 30 days, **When** the user requests spending trends with daily granularity, **Then** they receive an array of data points with date, amount, and transaction count for each day
2. **Given** expenses exist for the last 90 days, **When** the user requests spending trends with weekly granularity, **Then** they receive weekly aggregates with consistent ISO week boundaries
3. **Given** no expenses exist in the requested date range, **When** the user requests spending trends, **Then** they receive an empty array with a 200 status (not an error)
4. **Given** a date range spanning multiple months, **When** the user requests monthly granularity, **Then** each month's total is correctly calculated regardless of partial months at boundaries

---

### User Story 2 - View Spending by Category (Priority: P1)

As a user analyzing my expenses, I need to see a breakdown of spending by category for any date range, so that I can understand where my money goes and identify areas for budget management.

**Why this priority**: Category breakdown is essential for expense analysis and is required by the analytics dashboard. It directly supports the frontend's category distribution visualizations.

**Independent Test**: Can be tested by requesting category breakdown for a period and verifying the percentages sum to 100% and amounts match transaction data.

**Acceptance Scenarios**:

1. **Given** expenses exist across multiple categories, **When** the user requests spending by category, **Then** they receive a list sorted by amount with category name, total amount, transaction count, and percentage of total
2. **Given** some expenses have no category assigned, **When** the breakdown is generated, **Then** they are grouped under "Uncategorized" and included in the totals
3. **Given** a narrow date range with only one category, **When** the breakdown is generated, **Then** that category shows 100% of spending
4. **Given** startDate and endDate query parameters, **When** the request is made, **Then** only transactions within that inclusive date range are included

---

### User Story 3 - View Top Merchants Analysis (Priority: P2)

As a user reviewing my spending habits, I need to see my top merchants by spending amount with trend indicators, so that I can identify my biggest expense sources and track changes in merchant relationships.

**Why this priority**: Merchant analysis helps users understand their spending patterns at a vendor level, which is valuable for identifying duplicate charges, tracking subscription costs, and budget planning.

**Independent Test**: Can be tested by requesting top merchants and verifying the ranking matches actual transaction aggregations.

**Acceptance Scenarios**:

1. **Given** transactions from multiple vendors, **When** the user requests merchant analytics, **Then** they receive the top N merchants sorted by total amount with name, amount, transaction count, and percentage of total
2. **Given** a comparison period is requested, **When** the analytics are generated, **Then** each merchant includes previous period amount and percentage change
3. **Given** a new vendor appears in the current period, **When** listed in new merchants section, **Then** it shows the vendor name, amount, and indicates no prior history
4. **Given** the topCount parameter is set to 10, **When** the request is made, **Then** exactly 10 merchants are returned (or fewer if fewer exist)

---

### User Story 4 - View Subscription Detection Results (Priority: P2)

As a user monitoring recurring expenses, I need to view detected subscriptions through the analytics API, so that the frontend can display subscription tracking in the analytics dashboard context.

**Why this priority**: Subscription detection data exists but is exposed under `/api/subscriptions`. The frontend expects it at `/api/analytics/subscriptions`. This story enables the unified analytics experience.

**Independent Test**: Can be tested by calling the analytics endpoint and verifying it returns the same subscription data as the subscriptions controller.

**Acceptance Scenarios**:

1. **Given** detected subscriptions exist for the user, **When** the user requests analytics subscriptions, **Then** they receive a list with merchant name, frequency, amount, confidence level, and dates
2. **Given** minConfidence parameter is set to "high", **When** the request is made, **Then** only subscriptions with high confidence are returned
3. **Given** a subscription is acknowledged, **When** includeAcknowledged is false, **Then** that subscription is excluded from results
4. **Given** the user triggers a subscription analysis, **When** POST to analyze endpoint succeeds, **Then** new subscriptions are detected and the count is returned

---

### User Story 5 - View Spending by Vendor (Priority: P2)

As a user analyzing expense patterns, I need to see spending grouped by vendor for any date range, so that I can identify which vendors account for the most expenses.

**Why this priority**: Vendor-level spending data complements category analysis and provides a different view for expense optimization.

**Independent Test**: Can be tested by requesting vendor breakdown and verifying amounts match transaction data grouped by normalized vendor names.

**Acceptance Scenarios**:

1. **Given** expenses exist from multiple vendors, **When** the user requests spending by vendor, **Then** they receive a list with vendor name, total amount, transaction count, and percentage of total
2. **Given** vendor names with slight variations (e.g., "UBER" and "UBER EATS"), **When** the breakdown is generated, **Then** they remain separate entries (no automatic normalization)
3. **Given** startDate and endDate query parameters, **When** the request is made, **Then** only transactions within that date range are included

---

### Edge Cases

- What happens when the date range is invalid (startDate > endDate)?
- How does the system handle extremely large date ranges (e.g., 5 years)?
- What happens when the same merchant has both positive and negative transactions (refunds)?
- How does category breakdown handle transactions with $0 amounts?
- What happens when requesting monthly granularity for a single day?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a spending trend endpoint at `GET /api/analytics/spending-trend` accepting startDate, endDate, and granularity (day/week/month) parameters
- **FR-002**: System MUST provide a category spending endpoint at `GET /api/analytics/spending-by-category` accepting startDate and endDate parameters
- **FR-003**: System MUST provide a vendor spending endpoint at `GET /api/analytics/spending-by-vendor` accepting startDate and endDate parameters
- **FR-004**: System MUST provide a merchant analytics endpoint at `GET /api/analytics/merchants` accepting startDate, endDate, topCount, and includeComparison parameters
- **FR-005**: System MUST provide subscription analytics endpoints under `/api/analytics/subscriptions` that proxy to existing subscription detection functionality
- **FR-006**: All analytics endpoints MUST require authentication and filter data by the authenticated user
- **FR-007**: All analytics endpoints MUST return consistent error responses using ProblemDetails format
- **FR-008**: Date parameters MUST accept ISO date format (YYYY-MM-DD) and validate ranges
- **FR-009**: Spending trend endpoint MUST support granularity aggregation (day returns daily totals, week returns ISO week totals, month returns monthly totals)
- **FR-010**: Category and vendor breakdowns MUST include percentage of total spending calculated to 2 decimal places
- **FR-011**: Merchant analytics MUST identify new merchants (present in current period but not in comparison period) when comparison is enabled
- **FR-012**: Subscription analytics MUST support filtering by minConfidence (high/medium/low) and frequency types
- **FR-013**: System MUST handle empty result sets gracefully with 200 status and empty arrays (not 404)

### Key Entities *(include if feature involves data)*

- **SpendingTrendItem**: Represents a single data point with period (date string), amount, and transactionCount
- **SpendingByCategoryItem**: Represents category aggregate with category name, amount, transactionCount, and percentageOfTotal
- **SpendingByVendorItem**: Represents vendor aggregate with vendorName, amount, transactionCount, and percentageOfTotal
- **MerchantAnalyticsResponse**: Contains topMerchants, newMerchants, significantChanges arrays plus totalMerchantCount
- **TopMerchant**: Merchant detail with name, totalAmount, transactionCount, averageAmount, percentageOfTotal, previousAmount, changePercent, and trend direction
- **SubscriptionDetectionResponse**: Contains subscriptions list, summary statistics, newSubscriptions, possiblyEnded, and analyzedAt timestamp

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can load the analytics dashboard page without 404 errors on any API endpoint
- **SC-002**: Spending trend requests for 90-day ranges return data within 500ms for users with up to 1000 transactions
- **SC-003**: Category breakdown accurately reflects 100% of spending (percentages sum to 100% ± 0.01% rounding tolerance)
- **SC-004**: Merchant analytics correctly identifies new merchants with 100% accuracy compared to raw transaction data
- **SC-005**: Subscription detection returns results consistent with the existing `/api/subscriptions` endpoints
- **SC-006**: All five core analytics endpoints pass integration tests with valid authentication
- **SC-007**: Invalid date ranges return 400 Bad Request with descriptive error messages

## Scope & Boundaries

### In Scope

- New API endpoints for spending trends, category breakdown, vendor breakdown, and merchant analytics
- Analytics subscription endpoints that integrate with existing subscription detection service
- Query parameter validation and error handling
- Integration with existing ExpenseFlowDbContext and Transaction entities

### Out of Scope

- Frontend changes (frontend already exists and expects these endpoints)
- Database schema changes (using existing Transaction and DetectedSubscription tables)
- AI-powered analytics or predictions (existing rule-based detection is sufficient)
- Export functionality (handled by Reports feature)
- Real-time streaming or WebSocket analytics

## Assumptions

- The existing Transaction entity contains all necessary fields (TransactionDate, Amount, Description, Category)
- Vendor name normalization will use the existing description field (no vendor alias lookup needed for MVP)
- The frontend's expected response shapes match the DTOs defined in this specification
- Performance is acceptable with direct database queries; caching may be added later if needed
- The existing authentication middleware properly populates User claims for user identification
