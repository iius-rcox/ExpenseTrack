# API Endpoints Contract: Unified Frontend

**Feature Branch**: `011-unified-frontend`
**Date**: 2025-12-18

This document defines the API endpoints consumed by the frontend, mapping to existing backend controllers.

## Base URL

```
Production: https://api.expenseflow.internal/api
Development: http://localhost:5000/api
```

All endpoints require Bearer token authentication via MSAL ID token.

---

## 1. Receipts API

**Base Path**: `/api/receipts`

### Upload Receipts
```http
POST /api/receipts
Content-Type: multipart/form-data

Request: FormData with files[] (max 20 files, 25MB each)

Response 201:
{
  "receipts": [ReceiptSummary],
  "failed": [{ "filename": string, "error": string }],
  "totalUploaded": number
}
```

### List Receipts
```http
GET /api/receipts?pageNumber=1&pageSize=20&status=Unmatched&fromDate=2025-01-01&toDate=2025-01-31

Response 200:
{
  "items": [ReceiptSummary],
  "totalCount": number,
  "pageNumber": number,
  "pageSize": number
}
```

### Get Receipt Detail
```http
GET /api/receipts/{id}

Response 200: ReceiptDetail
Response 404: ProblemDetails
```

### Delete Receipt
```http
DELETE /api/receipts/{id}

Response 204: No Content
Response 404: ProblemDetails
```

### Get Receipt Download URL
```http
GET /api/receipts/{id}/download

Response 200: { "url": string }  // SAS URL valid for 1 hour
Response 404: ProblemDetails
```

### Get Receipt Status Counts
```http
GET /api/receipts/counts

Response 200:
{
  "counts": { "Pending": 5, "Processing": 2, ... },
  "total": number
}
```

### Retry Failed Receipt
```http
POST /api/receipts/{id}/retry

Response 200: ReceiptSummary
Response 400: ProblemDetails (if not in Error status or max retries exceeded)
Response 404: ProblemDetails
```

### Update Receipt (Manual Corrections)
```http
PUT /api/receipts/{id}
Content-Type: application/json

Request:
{
  "vendor": string?,
  "amount": number?,
  "date": string?,  // ISO date
  "tax": number?,
  "category": string?
}

Response 200: ReceiptDetail
Response 404: ProblemDetails
```

---

## 2. Transactions API

**Base Path**: `/api/transactions`

### List Transactions
```http
GET /api/transactions?page=1&pageSize=50&startDate=2025-01-01&endDate=2025-01-31&matched=false&importId=uuid

Response 200:
{
  "transactions": [TransactionSummary],
  "totalCount": number,
  "page": number,
  "pageSize": number,
  "unmatchedCount": number
}
```

### Get Transaction Detail
```http
GET /api/transactions/{id}

Response 200: TransactionDetail
Response 404: ProblemDetails
```

### Delete Transaction
```http
DELETE /api/transactions/{id}

Response 204: No Content
Response 404: ProblemDetails
```

---

## 3. Matching API

**Base Path**: `/api/matching`

### Run Auto-Match
```http
POST /api/matching/auto
Content-Type: application/json

Request (optional):
{
  "receiptIds": [string]?  // null = all unmatched
}

Response 200:
{
  "proposedCount": number,
  "processedCount": number,
  "ambiguousCount": number,
  "durationMs": number,
  "proposals": [MatchProposal]
}
```

### Get Match Proposals
```http
GET /api/matching/proposals?page=1&pageSize=20

Response 200:
{
  "items": [MatchProposal],
  "totalCount": number,
  "page": number,
  "pageSize": number
}
```

### Confirm Match
```http
POST /api/matching/{matchId}/confirm
Content-Type: application/json

Request (optional):
{
  "vendorDisplayName": string?,
  "defaultGLCode": string?,
  "defaultDepartment": string?
}

Response 200: MatchDetail
Response 404: ProblemDetails
Response 409: ProblemDetails (concurrent modification)
```

### Reject Match
```http
POST /api/matching/{matchId}/reject

Response 200: MatchDetail
Response 404: ProblemDetails
Response 409: ProblemDetails
```

### Create Manual Match
```http
POST /api/matching/manual
Content-Type: application/json

Request:
{
  "receiptId": string,
  "transactionId": string,
  "vendorDisplayName": string?,
  "defaultGLCode": string?,
  "defaultDepartment": string?
}

Response 201: MatchDetail
Response 404: ProblemDetails (receipt or transaction not found)
Response 409: ProblemDetails (already matched)
```

### Get Match Detail
```http
GET /api/matching/{matchId}

Response 200: MatchDetail
Response 404: ProblemDetails
```

### Get Matching Statistics
```http
GET /api/matching/stats

Response 200:
{
  "matchedCount": number,
  "proposedCount": number,
  "unmatchedReceiptsCount": number,
  "unmatchedTransactionsCount": number,
  "autoMatchRate": number,
  "averageConfidence": number
}
```

### Get Unmatched Receipts
```http
GET /api/matching/receipts/unmatched?page=1&pageSize=20

Response 200:
{
  "items": [MatchReceiptSummary],
  "totalCount": number,
  "page": number,
  "pageSize": number
}
```

### Get Unmatched Transactions
```http
GET /api/matching/transactions/unmatched?page=1&pageSize=20

Response 200:
{
  "items": [MatchTransactionSummary],
  "totalCount": number,
  "page": number,
  "pageSize": number
}
```

---

## 4. Reports API

**Base Path**: `/api/reports`

### Generate Draft Report
```http
POST /api/reports/draft
Content-Type: application/json

Request:
{
  "period": "2025-01"  // YYYY-MM format
}

Response 201: ExpenseReport
Response 400: ProblemDetails (invalid period)
```

### Check Existing Draft
```http
GET /api/reports/draft/exists?period=2025-01

Response 200:
{
  "exists": boolean,
  "reportId": string?
}
```

### Get Report by ID
```http
GET /api/reports/{reportId}

Response 200: ExpenseReport
Response 404: ProblemDetails
```

### List Reports
```http
GET /api/reports?status=Draft&period=2025-01&page=1&pageSize=20

Response 200:
{
  "items": [ReportSummary],
  "totalCount": number,
  "page": number,
  "pageSize": number
}
```

### Update Expense Line
```http
PATCH /api/reports/{reportId}/lines/{lineId}
Content-Type: application/json

Request:
{
  "category": string?,
  "glCode": string?,
  "department": string?,
  "project": string?,
  "notes": string?,
  "missingReceiptJustification": string?,
  "splitAllocations": [{
    "department": string,
    "project": string?,
    "percentage": number
  }]?
}

Response 200: ExpenseLine
Response 404: ProblemDetails
```

### Delete Report
```http
DELETE /api/reports/{reportId}

Response 204: No Content
Response 404: ProblemDetails
```

### Export Report to Excel
```http
GET /api/reports/{reportId}/export/excel

Response 200: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
  Headers:
    Content-Disposition: attachment; filename="ExpenseReport_2025-01_20250118.xlsx"
Response 404: ProblemDetails
Response 503: ProblemDetails (template not configured)
```

### Export Report Receipts PDF
```http
GET /api/reports/{reportId}/export/receipts

Response 200: application/pdf
  Headers:
    Content-Disposition: attachment; filename="ExpenseReport_2025-01_Receipts.pdf"
    X-Page-Count: number
    X-Placeholder-Count: number
Response 404: ProblemDetails
```

---

## 5. Analytics API

**Base Path**: `/api/analytics`

### Get Month-over-Month Comparison
```http
GET /api/analytics/comparison?currentPeriod=2025-01&previousPeriod=2024-12

Response 200: MonthlyComparison
Response 400: ProblemDetails (invalid period format)
```

### Get Cache Statistics
```http
GET /api/analytics/cache-stats?period=2025-01&groupBy=tier

Response 200: CacheStatisticsResponse
Response 400: ProblemDetails (invalid period or groupBy)
```

---

## 6. Statements API

**Base Path**: `/api/statements`

### Upload Statement
```http
POST /api/statements/upload
Content-Type: multipart/form-data

Request: FormData with file (CSV, XLS, XLSX)

Response 200:
{
  "importId": string,
  "fileName": string,
  "preview": {
    "columns": [string],
    "sampleRows": [[string]],
    "totalRows": number
  },
  "fingerprintMatch": {
    "id": string,
    "name": string,
    "confidence": number
  }?
}
```

### Apply Column Mapping
```http
POST /api/statements/{importId}/mapping
Content-Type: application/json

Request:
{
  "columnMappings": {
    "date": string,
    "description": string,
    "amount": string,
    "postDate": string?
  },
  "saveFingerprintAs": string?  // Save as new fingerprint
}

Response 200:
{
  "importId": string,
  "transactionCount": number,
  "duplicateCount": number,
  "status": "Completed"
}
```

### Get Import Status
```http
GET /api/statements/{importId}

Response 200: StatementImport
Response 404: ProblemDetails
```

### List Fingerprints
```http
GET /api/statements/fingerprints

Response 200: [StatementFingerprint]
```

### Delete Fingerprint
```http
DELETE /api/statements/fingerprints/{id}

Response 204: No Content
Response 404: ProblemDetails
```

### Rename Fingerprint
```http
PATCH /api/statements/fingerprints/{id}
Content-Type: application/json

Request:
{
  "name": string
}

Response 200: StatementFingerprint
Response 404: ProblemDetails
```

---

## 7. Reference Data API

**Base Path**: `/api/reference`

### Get GL Codes
```http
GET /api/reference/gl-codes

Response 200: [{ "code": string, "description": string }]
```

### Get Departments
```http
GET /api/reference/departments

Response 200: [{ "code": string, "name": string }]
```

### Get Projects
```http
GET /api/reference/projects

Response 200: [{ "code": string, "name": string, "department": string }]
```

---

## 8. User API

**Base Path**: `/api/users`

### Get Current User
```http
GET /api/users/me

Response 200:
{
  "id": string,
  "email": string,
  "displayName": string,
  "preferences": {
    "defaultDepartment": string?,
    "defaultProject": string?,
    "theme": "light" | "dark" | "system"
  }
}
```

### Update User Preferences
```http
PATCH /api/users/me/preferences
Content-Type: application/json

Request:
{
  "defaultDepartment": string?,
  "defaultProject": string?,
  "theme": "light" | "dark" | "system"
}

Response 200: UserPreferences
```

---

## Error Response Format

All error responses follow RFC 7807 Problem Details format:

```json
{
  "title": "Not Found",
  "detail": "Receipt with ID abc123 was not found",
  "status": 404,
  "type": "https://httpstatuses.com/404",
  "instance": "/api/receipts/abc123"
}
```

## Authentication

All requests require:
```http
Authorization: Bearer {id_token}
```

Token is obtained via MSAL `acquireTokenSilent()` using scopes: `['openid', 'profile', 'email']`

On 401 response, frontend redirects to MSAL login.
