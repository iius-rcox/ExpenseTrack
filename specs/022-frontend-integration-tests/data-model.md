# Data Model: Frontend Integration Tests

**Feature**: 022-frontend-integration-tests
**Date**: 2026-01-02

## Overview

This feature is testing infrastructure, not application data. The "data model" defines **test fixtures** - the shapes of mock data used in integration tests.

## Test Fixture Schemas

### Authentication Fixtures

#### MockAccount

Represents a simulated authenticated user for testing.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| homeAccountId | string | Yes | Unique account identifier |
| username | string | Yes | User's email/UPN |
| name | string | Yes | Display name |
| localAccountId | string | Yes | Azure AD local ID |
| tenantId | string | Yes | Azure AD tenant |

#### AuthState

Represents MSAL authentication state in tests.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| isAuthenticated | boolean | Yes | Whether user is logged in |
| account | MockAccount \| null | No | Current account if authenticated |
| accessToken | string \| null | No | Mock JWT token |
| expiresAt | Date \| null | No | Token expiration time |

---

### Analytics API Fixtures

#### MonthlyComparisonResponse

Mock response for `/api/analytics/monthly-comparison`.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| currentTotal | number | Yes | Current period spending |
| previousTotal | number | Yes | Previous period spending |
| percentageChange | number | Yes | Change percentage |
| newVendors | VendorChange[] | No | New vendors this period |
| missingRecurring | VendorChange[] | No | Expected vendors not seen |
| significantChanges | VendorChange[] | No | Notable changes |

#### SpendingTrendItem

Mock item for `/api/analytics/spending-trend`.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| date | string (ISO) | Yes | Period date |
| amount | number | Yes | Total spending |
| transactionCount | number | Yes | Number of transactions |

#### CategoryBreakdownItem

Mock item for `/api/analytics/spending-by-category`.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| category | string | Yes | Category name |
| amount | number | Yes | Total in category |
| percentageOfTotal | number | Yes | Percentage of all spending |
| transactionCount | number | Yes | Transactions in category |

#### MerchantAnalyticsResponse

Mock response for `/api/analytics/merchants`.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| merchants | MerchantItem[] | Yes | Top merchants list |
| totalMerchants | number | Yes | Total unique merchants |

#### SubscriptionDetectionResponse

Mock response for `/api/analytics/subscriptions`.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| subscriptions | SubscriptionItem[] | Yes | Detected subscriptions |
| totalMonthlyEstimate | number | Yes | Estimated monthly cost |

---

### Error State Fixtures

#### ApiErrorResponse

Mock error response matching ProblemDetails (RFC 7807).

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| type | string | Yes | Error type URI |
| title | string | Yes | Short error title |
| status | number | Yes | HTTP status code |
| detail | string | No | Detailed description |
| instance | string | No | Request path |

---

## Test Fixture Factory Functions

```typescript
// Factory pattern for creating test fixtures
interface FixtureFactories {
  // Auth fixtures
  createMockAccount(overrides?: Partial<MockAccount>): MockAccount
  createAuthState(isAuthenticated: boolean, overrides?: Partial<AuthState>): AuthState

  // Analytics fixtures
  createMonthlyComparison(overrides?: Partial<MonthlyComparisonResponse>): MonthlyComparisonResponse
  createSpendingTrend(days: number): SpendingTrendItem[]
  createCategoryBreakdown(count: number): CategoryBreakdownItem[]
  createMerchantAnalytics(count: number): MerchantAnalyticsResponse
  createSubscriptions(count: number): SubscriptionDetectionResponse

  // Error fixtures
  createApiError(status: number, title: string): ApiErrorResponse
}
```

## Fixture Variants

Each fixture type should have these variants for edge case testing:

| Variant | Purpose |
|---------|---------|
| `valid` | Standard successful response |
| `empty` | Empty arrays/null optionals |
| `minimal` | Only required fields |
| `malformed` | Invalid data shapes for error testing |
| `error_401` | Unauthorized response |
| `error_500` | Server error response |

## State Transitions (Auth Flow)

```
[Unauthenticated] --loginRedirect--> [Redirecting] --callback--> [Authenticated]
       ^                                                              |
       |                                                              |
       +------------------------logout--------------------------------+
       |                                                              |
       +--------------------tokenExpired------------------------------+
```

## Relationships

```
AuthState
  └── MockAccount (1:0..1)

MonthlyComparisonResponse
  ├── VendorChange[] (newVendors)
  ├── VendorChange[] (missingRecurring)
  └── VendorChange[] (significantChanges)

MerchantAnalyticsResponse
  └── MerchantItem[] (merchants)

SubscriptionDetectionResponse
  └── SubscriptionItem[] (subscriptions)
```

## Validation Rules

| Rule | Applies To | Constraint |
|------|------------|------------|
| Amount non-negative | All monetary values | `amount >= 0` |
| Percentage 0-100 | percentageOfTotal, percentageChange | `0 <= value <= 100` |
| Date format | All dates | ISO 8601 string |
| Array not null | All arrays | Empty array, not undefined |
