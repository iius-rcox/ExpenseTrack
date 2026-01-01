# ExpenseFlow API Endpoint Documentation

This document provides a comprehensive reference for all ExpenseFlow API endpoints. It is designed to support testing, integration, and future automation efforts.

**Base URL**: `https://{environment}.expenseflow.io/api`
**Authentication**: All endpoints (except `/api/health`) require a Bearer token (Microsoft Entra ID JWT).

---

## Table of Contents

1. [Health](#health)
2. [Receipts](#receipts)
3. [Statements](#statements)
4. [Transactions](#transactions)
5. [Matching](#matching)
6. [Categorization](#categorization)
7. [Subscriptions](#subscriptions)
8. [Travel Periods](#travel-periods)
9. [Reports](#reports)
10. [Analytics](#analytics)
11. [Dashboard](#dashboard)
12. [Users](#users)
13. [Reference Data](#reference-data)
14. [Expense Splitting](#expense-splitting)
15. [Cache Warming](#cache-warming)
16. [Cache (Admin)](#cache-admin)
17. [Description Normalization](#description-normalization)
18. [Test Cleanup (Staging Only)](#test-cleanup-staging-only)

---

## Health

Health check endpoints for monitoring and load balancer probes.

### GET /api/health
Returns service health status with component checks.

**Authentication**: None required (AllowAnonymous)

**Response**: `200 OK` or `503 Service Unavailable`

```json
{
  "status": "healthy",
  "timestamp": "2025-12-31T12:00:00Z",
  "version": "1.16.0",
  "checks": {
    "database": "healthy"
  }
}
```

---

## Receipts

Receipt upload, processing, and management endpoints.

### POST /api/receipts
Uploads one or more receipt files.

**Request**: `multipart/form-data` with `files` field
**Max Batch Size**: 20 files
**Max File Size**: 25MB per file
**Supported Formats**: JPEG, PNG, HEIC, PDF

**Response**: `201 Created`

```json
{
  "totalUploaded": 1,
  "receipts": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "thumbnailUrl": "https://...",
      "originalFilename": "receipt.jpg",
      "status": "Processing",
      "vendor": null,
      "date": null,
      "amount": null,
      "currency": "USD",
      "createdAt": "2025-12-31T12:00:00Z"
    }
  ],
  "failed": []
}
```

### GET /api/receipts
Gets a paginated list of receipts for the current user.

**Query Parameters**:
- `pageNumber` (int, default: 1): Page number
- `pageSize` (int, default: 20, max: 100): Items per page
- `status` (string, optional): Filter by status (Uploaded, Processing, Processed, Error, Unmatched, Matched)
- `fromDate` (DateTime, optional): Start date filter
- `toDate` (DateTime, optional): End date filter

**Response**: `200 OK`

```json
{
  "items": [...],
  "totalCount": 42,
  "pageNumber": 1,
  "pageSize": 20
}
```

### GET /api/receipts/{id}
Gets a specific receipt by ID.

**Response**: `200 OK` | `404 Not Found`

### DELETE /api/receipts/{id}
Deletes a receipt.

**Response**: `204 No Content` | `404 Not Found`

### GET /api/receipts/{id}/download
Gets a temporary download URL for a receipt (1-hour SAS token).

**Response**: `200 OK`

```json
{
  "url": "https://storage.blob.core.windows.net/..."
}
```

### GET /api/receipts/counts
Gets receipt status counts for the current user.

**Response**: `200 OK`

```json
{
  "counts": {
    "Uploaded": 2,
    "Processing": 1,
    "Processed": 15,
    "Matched": 10,
    "Unmatched": 5,
    "Error": 0
  },
  "total": 33
}
```

### GET /api/receipts/unmatched
Gets unmatched receipts for matching workflow.

**Query Parameters**: Same as GET /api/receipts

### POST /api/receipts/{id}/retry
Retries processing for a failed receipt. Max 3 retries.

**Response**: `200 OK` | `400 Bad Request` | `404 Not Found`

### PUT /api/receipts/{id}
Updates receipt data for manual corrections.

**Request Body**:
```json
{
  "vendor": "Updated Vendor Name",
  "date": "2025-12-31",
  "amount": 42.99
}
```

### POST /api/receipts/{id}/process
Triggers processing for a receipt stuck in Uploaded status.

**Response**: `200 OK` | `400 Bad Request` | `404 Not Found`

---

## Statements

Bank/credit card statement import and fingerprint management.

### POST /api/statements/analyze
Analyzes an uploaded statement file and returns column mapping options.

**Request**: `multipart/form-data` with `file` field
**Supported Formats**: CSV, XLSX, XLS
**Max Size**: 10MB

**Response**: `200 OK`

```json
{
  "analysisId": "550e8400-e29b-41d4-a716-446655440000",
  "fileName": "chase_statement.csv",
  "rowCount": 150,
  "headers": ["Date", "Description", "Amount", "Balance"],
  "sampleRows": [...],
  "mappingOptions": [
    {
      "source": "user_fingerprint",
      "tier": 1,
      "fingerprintId": "...",
      "sourceName": "Chase Visa",
      "columnMapping": {
        "Date": "date",
        "Description": "description",
        "Amount": "amount"
      },
      "dateFormat": "MM/dd/yyyy",
      "amountSign": "positive_charges"
    }
  ]
}
```

### POST /api/statements/import
Imports transactions from an analyzed statement.

**Request Body**:
```json
{
  "analysisId": "550e8400-e29b-41d4-a716-446655440000",
  "columnMapping": {
    "Date": "date",
    "Description": "description",
    "Amount": "amount"
  },
  "dateFormat": "MM/dd/yyyy",
  "amountSign": "positive_charges",
  "saveAsFingerprint": true,
  "fingerprintName": "Chase Visa"
}
```

**Response**: `200 OK`

```json
{
  "importId": "...",
  "tierUsed": 1,
  "imported": 145,
  "skipped": 3,
  "duplicates": 2,
  "fingerprintSaved": true,
  "transactions": [...]
}
```

### GET /api/statements/imports
Gets import history for the current user.

**Query Parameters**:
- `page` (int, default: 1)
- `pageSize` (int, default: 20, max: 100)

### GET /api/statements/fingerprints
Gets available fingerprints (statement templates) for the current user.

---

## Transactions

Transaction management endpoints.

### GET /api/transactions
Gets paginated list of transactions with optional filters.

**Query Parameters**:
- `page` (int, default: 1)
- `pageSize` (int, default: 50, max: 200)
- `startDate` (DateOnly, optional)
- `endDate` (DateOnly, optional)
- `matched` (bool, optional): Filter by receipt match status
- `importId` (Guid, optional): Filter by import batch
- `search` (string, optional): Text search on description

**Response**: `200 OK`

```json
{
  "transactions": [...],
  "totalCount": 500,
  "page": 1,
  "pageSize": 50,
  "unmatchedCount": 25
}
```

### GET /api/transactions/{id}
Gets transaction details by ID.

### DELETE /api/transactions/{id}
Deletes a transaction.

---

## Matching

Receipt-to-transaction matching operations.

### POST /api/matching/auto
Runs auto-match for all unmatched receipts.

**Request Body** (optional):
```json
{
  "receiptIds": ["id1", "id2"]
}
```

**Response**: `200 OK`

```json
{
  "proposedCount": 10,
  "processedCount": 15,
  "ambiguousCount": 2,
  "durationMs": 1234,
  "proposals": [...]
}
```

### GET /api/matching/proposals
Gets all proposed matches for review.

**Query Parameters**:
- `page` (int, default: 1)
- `pageSize` (int, default: 20, max: 100)

### POST /api/matching/{matchId}/confirm
Confirms a proposed match.

**Request Body** (optional):
```json
{
  "vendorDisplayName": "Amazon",
  "defaultGLCode": "5100",
  "defaultDepartment": "100"
}
```

### POST /api/matching/{matchId}/reject
Rejects a proposed match.

### POST /api/matching/manual
Manually matches a receipt to a transaction.

**Request Body**:
```json
{
  "receiptId": "...",
  "transactionId": "...",
  "vendorDisplayName": "Amazon",
  "defaultGLCode": "5100",
  "defaultDepartment": "100"
}
```

### GET /api/matching/{matchId}
Gets match details.

### GET /api/matching/stats
Gets matching statistics.

```json
{
  "matchedCount": 100,
  "proposedCount": 5,
  "unmatchedReceiptsCount": 10,
  "unmatchedTransactionsCount": 25,
  "autoMatchRate": 0.85,
  "averageConfidence": 0.92
}
```

### GET /api/matching/receipts/unmatched
Gets unmatched receipts with extracted data.

### GET /api/matching/transactions/unmatched
Gets unmatched transactions.

---

## Categorization

AI-powered GL code and department categorization.

### GET /api/categorization/transactions/{transactionId}/gl-suggestions
Gets GL code suggestions using tiered approach (Tier 1: vendor alias, Tier 2: embedding similarity, Tier 3: AI).

**Response**: `200 OK`

```json
{
  "transactionId": "...",
  "tier": 1,
  "suggestions": [
    {
      "glCode": "5100",
      "name": "Office Supplies",
      "confidence": 0.95,
      "reason": "Vendor alias default"
    }
  ],
  "message": null
}
```

### GET /api/categorization/transactions/{transactionId}/dept-suggestions
Gets department suggestions.

### GET /api/categorization/transactions/{transactionId}
Gets combined GL and department suggestions.

### POST /api/categorization/transactions/{transactionId}/confirm
Confirms categorization selection, creating verified embedding.

**Request Body**:
```json
{
  "glCode": "5100",
  "departmentCode": "100",
  "acceptedSuggestion": true
}
```

### POST /api/categorization/transactions/{transactionId}/skip
Skips AI suggestion for manual categorization.

**Request Body**:
```json
{
  "reason": "Unusual transaction"
}
```

### GET /api/categorization/stats
Gets tier usage statistics for cost monitoring.

**Query Parameters**:
- `startDate` (DateTime, optional, default: 30 days ago)
- `endDate` (DateTime, optional, default: today)
- `operationType` (string, optional)

---

## Subscriptions

Subscription detection and management.

### GET /api/subscriptions
Gets paginated list of subscriptions.

**Query Parameters**:
- `page` (int, default: 1)
- `pageSize` (int, default: 20, max: 100)
- `status` (string, optional): Active, Cancelled, Paused

### GET /api/subscriptions/{id}
Gets subscription details.

### POST /api/subscriptions
Creates a new manual subscription.

**Request Body**:
```json
{
  "vendorName": "Netflix",
  "expectedAmount": 15.99,
  "expectedDay": 1,
  "frequency": "monthly",
  "glCode": "5200",
  "departmentCode": "100"
}
```

### PUT /api/subscriptions/{id}
Updates an existing subscription.

### DELETE /api/subscriptions/{id}
Deletes a subscription.

### POST /api/subscriptions/detect
Triggers subscription detection from a specific transaction.

**Query Parameters**:
- `transactionId` (Guid, required)

### GET /api/subscriptions/alerts
Gets subscription alerts.

**Query Parameters**:
- `includeAcknowledged` (bool, default: false)

### POST /api/subscriptions/alerts/acknowledge
Acknowledges subscription alerts.

**Request Body**:
```json
{
  "alertIds": ["id1", "id2"]
}
```

### GET /api/subscriptions/summary
Gets subscription monitoring summary dashboard data.

### POST /api/subscriptions/check
Manually triggers a subscription check for a specific month.

**Query Parameters**:
- `month` (string, optional, format: YYYY-MM, default: previous month)

---

## Travel Periods

Travel period detection and management.

### GET /api/travelperiods
Gets paginated list of travel periods.

**Query Parameters**:
- `page` (int, default: 1)
- `pageSize` (int, default: 20, max: 100)
- `startDate` (DateOnly, optional)
- `endDate` (DateOnly, optional)

### GET /api/travelperiods/{id}
Gets travel period details.

### POST /api/travelperiods
Creates a new manual travel period.

**Request Body**:
```json
{
  "description": "Client visit to NYC",
  "startDate": "2025-12-01",
  "endDate": "2025-12-05",
  "destination": "New York, NY"
}
```

### PUT /api/travelperiods/{id}
Updates a travel period.

### DELETE /api/travelperiods/{id}
Deletes a travel period.

### GET /api/travelperiods/{id}/expenses
Gets expenses within a travel period.

### GET /api/travelperiods/timeline
Gets a timeline view of travel periods with linked expenses.

**Query Parameters**:
- `startDate` (DateOnly, optional)
- `endDate` (DateOnly, optional)
- `includeExpenses` (bool, default: true)

### POST /api/travelperiods/detect
Triggers travel detection from a specific receipt.

**Query Parameters**:
- `receiptId` (Guid, required)

---

## Reports

Expense report generation and management.

### POST /api/reports/draft
Generates a draft expense report for a specific period.

**Request Body**:
```json
{
  "period": "2025-12"
}
```

**Response**: `201 Created`

### GET /api/reports/draft/exists
Checks if a draft report exists for a period.

**Query Parameters**:
- `period` (string, required, format: YYYY-MM)

**Response**: `200 OK`

```json
{
  "exists": true,
  "reportId": "..."
}
```

### GET /api/reports/{reportId}
Gets a report by ID with all expense lines.

### GET /api/reports
Gets paginated list of reports.

**Query Parameters**:
- `status` (string, optional): Draft, Submitted, Approved, Rejected
- `period` (string, optional, format: YYYY-MM)
- `page` (int, default: 1)
- `pageSize` (int, default: 20, max: 100)

### PATCH /api/reports/{reportId}/lines/{lineId}
Updates an expense line within a report.

**Request Body**:
```json
{
  "glCode": "5100",
  "departmentCode": "100",
  "description": "Office supplies",
  "justification": "Client meeting supplies"
}
```

### DELETE /api/reports/{reportId}
Deletes a report (soft delete).

### GET /api/reports/{reportId}/export/excel
Exports report to Excel format.

**Response**: `200 OK` with `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`

### GET /api/reports/{reportId}/export/receipts
Exports consolidated PDF of all receipts for a report.

**Response**: `200 OK` with `application/pdf`

**Response Headers**:
- `X-Page-Count`: Number of pages
- `X-Placeholder-Count`: Number of placeholder pages for missing receipts

---

## Analytics

Expense analytics and insights (Feature 019).

### GET /api/analytics/comparison
Gets month-over-month spending comparison.

**Query Parameters**:
- `currentPeriod` (string, required, format: YYYY-MM)
- `previousPeriod` (string, optional, format: YYYY-MM, default: month before current)

**Response**: `200 OK`

```json
{
  "currentPeriod": "2025-12",
  "previousPeriod": "2025-11",
  "currentTotal": 5000.00,
  "previousTotal": 4500.00,
  "percentChange": 11.11,
  "newVendors": [...],
  "missingRecurring": [...],
  "significantChanges": [...]
}
```

### GET /api/analytics/cache-stats
Gets cache tier usage statistics.

**Query Parameters**:
- `period` (string, required, format: YYYY-MM)
- `groupBy` (string, optional): "tier", "operation", or "day" (default: "tier")

### GET /api/analytics/categories
Gets expense breakdown by category for a period.

**Query Parameters**:
- `period` (string, optional, format: YYYY-MM, default: current month)

**Response**: `200 OK`

```json
{
  "period": "2025-12",
  "totalSpending": 5000.00,
  "transactionCount": 100,
  "categories": [
    {
      "category": "Food & Dining",
      "amount": 1200.00,
      "percentage": 24.00,
      "transactionCount": 30
    }
  ]
}
```

### GET /api/analytics/spending-trend
Gets spending trends over time.

**Query Parameters**:
- `startDate` (string, required, format: YYYY-MM-DD)
- `endDate` (string, required, format: YYYY-MM-DD)
- `granularity` (string, optional): "day", "week", "month" (default: "day")

**Validation**:
- Maximum date range: 365 days
- endDate must be after startDate

**Response**: `200 OK`

```json
[
  {
    "date": "2025-12-01",
    "total": 150.00,
    "transactionCount": 5
  }
]
```

### GET /api/analytics/spending-by-category
Gets spending breakdown by category for a date range.

**Query Parameters**:
- `startDate` (string, required, format: YYYY-MM-DD)
- `endDate` (string, required, format: YYYY-MM-DD)

**Response**: `200 OK`

```json
[
  {
    "category": "Food & Dining",
    "amount": 1200.00,
    "percentage": 24.00,
    "transactionCount": 30
  }
]
```

### GET /api/analytics/spending-by-vendor
Gets spending breakdown by vendor for a date range.

**Query Parameters**:
- `startDate` (string, required, format: YYYY-MM-DD)
- `endDate` (string, required, format: YYYY-MM-DD)

### GET /api/analytics/merchants
Gets merchant analytics with optional comparison.

**Query Parameters**:
- `startDate` (string, required, format: YYYY-MM-DD)
- `endDate` (string, required, format: YYYY-MM-DD)
- `topCount` (int, optional, default: 10, range: 1-100)
- `includeComparison` (bool, optional, default: false)

### GET /api/analytics/subscriptions
Gets detected subscriptions with optional filters.

**Query Parameters**:
- `minConfidence` (string, optional): "high", "medium", "low"
- `frequency` (string[], optional): Filter by frequencies
- `includeAcknowledged` (bool, optional, default: true)

### POST /api/analytics/subscriptions/analyze
Triggers subscription analysis for the user.

**Response**: `200 OK`

```json
{
  "detected": 5,
  "new": 2,
  "updated": 3,
  "analysisDate": "2025-12-31T12:00:00Z"
}
```

### POST /api/analytics/subscriptions/{subscriptionId}/acknowledge
Acknowledges or unacknowledges a subscription.

**Request Body**:
```json
{
  "acknowledged": true
}
```

**Response**: `204 No Content` | `404 Not Found`

---

## Dashboard

Dashboard metrics and activity feed.

### GET /api/dashboard/metrics
Gets dashboard metrics for the current user.

**Response**: `200 OK`

```json
{
  "pendingReceiptsCount": 5,
  "unmatchedTransactionsCount": 10,
  "pendingMatchesCount": 3,
  "draftReportsCount": 1,
  "monthlySpending": {
    "currentMonth": 2500.00,
    "previousMonth": 2200.00,
    "percentChange": 13.6
  }
}
```

### GET /api/dashboard/activity
Gets recent activity for the current user.

**Query Parameters**:
- `limit` (int, default: 10, max: 50)

**Response**: `200 OK`

```json
[
  {
    "type": "receipt_uploaded",
    "title": "Receipt uploaded",
    "description": "Starbucks",
    "timestamp": "2025-12-31T12:00:00Z"
  }
]
```

### GET /api/dashboard/actions
Gets pending actions requiring user review.

**Query Parameters**:
- `limit` (int, default: 10, max: 50)

---

## Users

User profile and preferences management.

### GET /api/user/me
Gets the current user's profile including preferences.

**Response**: `200 OK`

```json
{
  "id": "...",
  "email": "user@example.com",
  "displayName": "John Doe",
  "department": "Engineering",
  "createdAt": "2025-01-01T00:00:00Z",
  "lastLoginAt": "2025-12-31T12:00:00Z",
  "preferences": {
    "theme": "system",
    "defaultDepartmentId": "...",
    "defaultProjectId": "..."
  }
}
```

### GET /api/user/preferences
Gets the current user's preferences.

### PATCH /api/user/preferences
Partially updates the current user's preferences.

**Request Body**:
```json
{
  "theme": "dark",
  "defaultDepartmentId": "...",
  "defaultProjectId": "..."
}
```

---

## Reference Data

GL accounts, departments, and projects.

### GET /api/reference/gl-accounts
Gets all GL accounts.

**Query Parameters**:
- `activeOnly` (bool, default: true)

### GET /api/reference/departments
Gets all departments.

**Query Parameters**:
- `activeOnly` (bool, default: true)

### GET /api/reference/projects
Gets all projects.

**Query Parameters**:
- `activeOnly` (bool, default: true)

### POST /api/reference/sync
Triggers a reference data sync (admin only).

**Authorization**: Requires "AdminOnly" policy

**Response**: `202 Accepted`

```json
{
  "jobId": "...",
  "status": "enqueued",
  "enqueuedAt": "2025-12-31T12:00:00Z"
}
```

---

## Expense Splitting

Expense splitting and split pattern management.

### GET /api/expensesplitting/expenses/{expenseId}/split
Gets the split status and suggestion for an expense.

### POST /api/expensesplitting/expenses/{expenseId}/split
Applies a split to an expense.

**Request Body**:
```json
{
  "allocations": [
    {
      "departmentCode": "100",
      "glCode": "5100",
      "percentage": 60.00
    },
    {
      "departmentCode": "200",
      "glCode": "5200",
      "percentage": 40.00
    }
  ],
  "saveAsPattern": true,
  "patternName": "Office/Engineering Split"
}
```

### DELETE /api/expensesplitting/expenses/{expenseId}/split
Removes a split from an expense.

### GET /api/expensesplitting/split-patterns
Gets paginated list of split patterns.

**Query Parameters**:
- `page` (int, default: 1)
- `pageSize` (int, default: 20, max: 100)
- `vendorAliasId` (Guid, optional)

### GET /api/expensesplitting/split-patterns/{id}
Gets a split pattern by ID.

### POST /api/expensesplitting/split-patterns
Creates a new split pattern.

### PUT /api/expensesplitting/split-patterns/{id}
Updates an existing split pattern.

### DELETE /api/expensesplitting/split-patterns/{id}
Deletes a split pattern.

---

## Cache Warming

Historical data import for cache warming.

### POST /api/cachewarming/import
Uploads historical expense data for cache warming.

**Request**: `multipart/form-data` with Excel file (.xlsx)
**Max Size**: 10MB

**Response**: `202 Accepted`

```json
{
  "id": "...",
  "status": "Pending",
  "sourceFileName": "historical_data.xlsx",
  "startedAt": null,
  "completedAt": null,
  "progress": {
    "totalRecords": 0,
    "processedRecords": 0,
    "cachedDescriptions": 0,
    "createdAliases": 0,
    "generatedEmbeddings": 0,
    "skippedRecords": 0,
    "percentComplete": 0
  }
}
```

### GET /api/cachewarming/jobs
Gets paginated list of import jobs.

**Query Parameters**:
- `status` (string, optional): Pending, Processing, Completed, Failed, Cancelled
- `page` (int, default: 1)
- `pageSize` (int, default: 20, max: 100)

### GET /api/cachewarming/jobs/{jobId}
Gets details of a specific import job.

### DELETE /api/cachewarming/jobs/{jobId}
Cancels a pending or processing import job.

### GET /api/cachewarming/jobs/{jobId}/errors
Gets error details for an import job.

**Query Parameters**:
- `page` (int, default: 1)
- `pageSize` (int, default: 50, max: 100)

### GET /api/cache/statistics
Gets overall cache statistics.

**Query Parameters**:
- `period` (string, optional, format: YYYY-MM, default: current month)

### GET /api/cache/statistics/warming-summary
Gets cache warming summary with counts by source.

---

## Cache (Admin)

Cache management endpoints (admin only).

### GET /api/cache/stats
Gets statistics for all cache tables.

**Authorization**: Requires "AdminOnly" policy

---

## Description Normalization

Transaction description normalization using AI.

### POST /api/description/normalize
Normalizes a raw transaction description.

**Request Body**:
```json
{
  "rawDescription": "AMZN MKTP US*2X3Y4Z5"
}
```

**Response**: `200 OK`

```json
{
  "originalDescription": "AMZN MKTP US*2X3Y4Z5",
  "normalizedDescription": "Amazon Marketplace",
  "tier": 1,
  "confidence": 0.95,
  "cacheHit": true
}
```

### GET /api/description/cache-stats
Gets cache statistics for description normalization.

---

## Test Cleanup (Staging Only)

Test data cleanup endpoints. **Only available in DEBUG and STAGING builds.**

### POST /api/test/cleanup
Cleans up test data for the authenticated user.

**Request Body** (optional):
```json
{
  "entityTypes": ["receipts", "transactions", "matches"],
  "createdAfter": "2025-12-01T00:00:00Z"
}
```

**Valid Entity Types**:
- `receipts`
- `transactions`
- `matches`
- `imports`
- `reports`
- `embeddings`
- `tierusage`
- `travel`

**Response**: `200 OK`

```json
{
  "success": true,
  "deletedCounts": {
    "receipts": 10,
    "transactions": 50,
    "matches": 8,
    "imports": 2,
    "expenseLines": 45,
    "expenseReports": 3,
    "tierUsageLogs": 100,
    "expenseEmbeddings": 25,
    "travelPeriods": 2,
    "blobsDeleted": 10
  },
  "durationMs": 1234,
  "warnings": []
}
```

---

## Error Responses

All endpoints return errors using the ProblemDetails format:

```json
{
  "title": "Validation Error",
  "detail": "Invalid period format. Expected YYYY-MM (e.g., 2025-01).",
  "status": 400
}
```

### Common HTTP Status Codes

| Code | Description |
|------|-------------|
| 200 | OK - Request succeeded |
| 201 | Created - Resource created |
| 202 | Accepted - Request accepted for async processing |
| 204 | No Content - Success with no body |
| 400 | Bad Request - Validation error |
| 401 | Unauthorized - Invalid/missing token |
| 403 | Forbidden - Insufficient permissions |
| 404 | Not Found - Resource not found |
| 409 | Conflict - Concurrency conflict |
| 413 | Payload Too Large - File too large |
| 500 | Internal Server Error |
| 503 | Service Unavailable - AI service unavailable |

---

## Testing Examples

### Authentication Header

All authenticated requests require:
```
Authorization: Bearer {jwt_token}
```

### cURL Examples

#### Upload a Receipt
```bash
curl -X POST "https://staging.expenseflow.io/api/receipts" \
  -H "Authorization: Bearer $TOKEN" \
  -F "files=@receipt.jpg"
```

#### Get Dashboard Metrics
```bash
curl -X GET "https://staging.expenseflow.io/api/dashboard/metrics" \
  -H "Authorization: Bearer $TOKEN"
```

#### Get Spending Trend
```bash
curl -X GET "https://staging.expenseflow.io/api/analytics/spending-trend?startDate=2025-12-01&endDate=2025-12-31&granularity=day" \
  -H "Authorization: Bearer $TOKEN"
```

#### Import Statement
```bash
# Step 1: Analyze
curl -X POST "https://staging.expenseflow.io/api/statements/analyze" \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@statement.csv"

# Step 2: Import (use analysisId from step 1)
curl -X POST "https://staging.expenseflow.io/api/statements/import" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "analysisId": "...",
    "columnMapping": {"Date": "date", "Description": "description", "Amount": "amount"},
    "dateFormat": "MM/dd/yyyy",
    "amountSign": "positive_charges"
  }'
```

---

## Version History

| Date | Version | Changes |
|------|---------|---------|
| 2025-12-31 | 1.16.0 | Added Analytics Dashboard API (Feature 019) |
| 2025-12-29 | 1.15.0 | Added User Preferences API (Feature 016) |
| 2025-12-27 | 1.14.0 | Added Dual Theme System (Feature 015) |
