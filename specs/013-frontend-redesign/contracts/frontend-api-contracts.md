# Frontend API Contracts: Front-End Redesign

**Feature**: 013-frontend-redesign
**Date**: 2025-12-21
**Purpose**: Document the API endpoints consumed by the frontend redesign

---

## Overview

This document describes the existing backend API endpoints that the frontend redesign will consume. Since backend changes are out of scope (per spec), this serves as a reference for the expected API contract.

The frontend uses TanStack Query for data fetching with the following patterns:
- Query keys for caching and invalidation
- Optimistic updates for immediate feedback
- Polling intervals for background refresh

---

## 1. Dashboard Endpoints

### 1.1 Get Dashboard Metrics

**Endpoint**: `GET /api/dashboard/metrics`

**Query Key**: `['dashboard', 'metrics']`

**Polling**: 30 seconds

**Response**:
```json
{
  "monthlyTotal": 4287.50,
  "monthlyChange": 12.5,
  "pendingReviewCount": 23,
  "matchingPercentage": 94,
  "categorizedPercentage": 87,
  "recentActivityCount": 15
}
```

### 1.2 Get Expense Stream

**Endpoint**: `GET /api/dashboard/activity?limit=10`

**Query Key**: `['dashboard', 'activity']`

**Polling**: 30 seconds

**Response**:
```json
{
  "items": [
    {
      "id": "evt-123",
      "type": "receipt",
      "title": "Starbucks Receipt Processed",
      "amount": 12.50,
      "timestamp": "2025-12-21T10:30:00Z",
      "status": "complete",
      "confidence": 0.95
    }
  ]
}
```

### 1.3 Get Action Queue

**Endpoint**: `GET /api/dashboard/actions`

**Query Key**: `['dashboard', 'actions']`

**Polling**: 30 seconds

**Response**:
```json
{
  "items": [
    {
      "id": "act-456",
      "type": "review_match",
      "priority": "high",
      "title": "Review match: Amazon $47.99",
      "description": "AI confidence 85% - verify merchant match",
      "createdAt": "2025-12-21T09:00:00Z",
      "actionUrl": "/matching?id=match-789"
    }
  ]
}
```

---

## 2. Receipt Endpoints

### 2.1 Upload Receipt

**Endpoint**: `POST /api/receipts/upload`

**Content-Type**: `multipart/form-data`

**Request**:
```
file: <binary>
```

**Response**:
```json
{
  "id": "rcpt-123",
  "status": "processing",
  "uploadedAt": "2025-12-21T10:30:00Z"
}
```

**Mutations**: Invalidates `['receipts']`, `['dashboard']`

### 2.2 Get Receipt List

**Endpoint**: `GET /api/receipts?page=1&limit=20&status=all`

**Query Key**: `['receipts', { page, limit, status }]`

**Response**:
```json
{
  "items": [
    {
      "id": "rcpt-123",
      "imageUrl": "https://storage.../rcpt-123.jpg",
      "thumbnailUrl": "https://storage.../rcpt-123-thumb.jpg",
      "uploadedAt": "2025-12-21T10:30:00Z",
      "status": "complete",
      "extractedFields": [
        {
          "key": "merchant",
          "value": "Starbucks",
          "confidence": 0.95,
          "isEdited": false
        },
        {
          "key": "amount",
          "value": 12.50,
          "confidence": 0.98,
          "isEdited": false
        }
      ],
      "matchedTransactionId": null
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 156
  }
}
```

### 2.3 Get Receipt Detail

**Endpoint**: `GET /api/receipts/{id}`

**Query Key**: `['receipts', id]`

**Response**: Single receipt object (same structure as list item)

### 2.4 Update Receipt Field

**Endpoint**: `PATCH /api/receipts/{id}/fields`

**Request**:
```json
{
  "field": "merchant",
  "value": "Starbucks Coffee"
}
```

**Response**:
```json
{
  "success": true,
  "field": {
    "key": "merchant",
    "value": "Starbucks Coffee",
    "confidence": 0.95,
    "isEdited": true,
    "originalValue": "Starbucks"
  }
}
```

**Optimistic Update**: Update cache immediately, rollback on error

### 2.5 Delete Receipt

**Endpoint**: `DELETE /api/receipts/{id}`

**Response**: `204 No Content`

**Mutations**: Invalidates `['receipts']`, `['dashboard']`

---

## 3. Transaction Endpoints

### 3.1 Get Transactions

**Endpoint**: `GET /api/transactions`

**Query Parameters**:
- `page`: number (default: 1)
- `limit`: number (default: 50)
- `search`: string (optional)
- `startDate`: ISO date (optional)
- `endDate`: ISO date (optional)
- `categories`: comma-separated IDs (optional)
- `minAmount`: number (optional)
- `maxAmount`: number (optional)
- `matchStatus`: matched|pending|unmatched|manual (optional)
- `sortBy`: date|amount|merchant|category (default: date)
- `sortDir`: asc|desc (default: desc)

**Query Key**: `['transactions', filters]`

**Response**:
```json
{
  "items": [
    {
      "id": "txn-123",
      "date": "2025-12-20",
      "description": "AMZN MKTP US*RT4X90WN3",
      "merchant": "Amazon",
      "amount": 47.99,
      "category": "Shopping",
      "categoryId": "cat-shopping",
      "tags": ["online", "household"],
      "notes": "",
      "matchStatus": "matched",
      "matchedReceiptId": "rcpt-456",
      "matchConfidence": 0.92,
      "source": "import"
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 50,
    "total": 1247
  }
}
```

### 3.2 Update Transaction

**Endpoint**: `PATCH /api/transactions/{id}`

**Request**:
```json
{
  "category": "cat-food",
  "tags": ["lunch", "work"],
  "notes": "Team lunch expense"
}
```

**Response**: Updated transaction object

**Optimistic Update**: Update cache immediately with undo support

### 3.3 Bulk Update Transactions

**Endpoint**: `PATCH /api/transactions/bulk`

**Request**:
```json
{
  "ids": ["txn-123", "txn-456", "txn-789"],
  "updates": {
    "category": "cat-food",
    "tags": ["add:lunch"]
  }
}
```

**Response**:
```json
{
  "updated": 3,
  "failed": 0
}
```

### 3.4 Export Transactions

**Endpoint**: `POST /api/transactions/export`

**Request**:
```json
{
  "ids": ["txn-123", "txn-456"],
  "format": "csv"
}
```

**Response**: File download (CSV or Excel)

---

## 4. Match Endpoints

### 4.1 Get Pending Matches

**Endpoint**: `GET /api/matching/pending?page=1&limit=20`

**Query Key**: `['matching', 'pending']`

**Response**:
```json
{
  "items": [
    {
      "id": "match-123",
      "receipt": { /* ReceiptPreview object */ },
      "transaction": { /* TransactionView object */ },
      "confidence": 0.87,
      "matchingFactors": [
        {
          "type": "amount",
          "weight": 0.4,
          "receiptValue": "$47.99",
          "transactionValue": "$47.99",
          "isExactMatch": true
        },
        {
          "type": "date",
          "weight": 0.3,
          "receiptValue": "2025-12-20",
          "transactionValue": "2025-12-20",
          "isExactMatch": true
        },
        {
          "type": "merchant",
          "weight": 0.3,
          "receiptValue": "Amazon",
          "transactionValue": "AMZN MKTP",
          "isExactMatch": false
        }
      ],
      "status": "pending"
    }
  ],
  "pagination": {
    "page": 1,
    "limit": 20,
    "total": 23
  }
}
```

### 4.2 Approve Match

**Endpoint**: `POST /api/matching/{id}/approve`

**Response**:
```json
{
  "success": true,
  "matchId": "match-123",
  "receiptId": "rcpt-456",
  "transactionId": "txn-789"
}
```

**Mutations**: Invalidates `['matching']`, `['receipts']`, `['transactions']`, `['dashboard']`

### 4.3 Reject Match

**Endpoint**: `POST /api/matching/{id}/reject`

**Response**:
```json
{
  "success": true,
  "matchId": "match-123"
}
```

### 4.4 Manual Match

**Endpoint**: `POST /api/matching/manual`

**Request**:
```json
{
  "receiptId": "rcpt-456",
  "transactionId": "txn-789"
}
```

**Response**:
```json
{
  "success": true,
  "matchId": "match-new-123"
}
```

### 4.5 Batch Approve

**Endpoint**: `POST /api/matching/batch-approve`

**Request**:
```json
{
  "ids": ["match-123", "match-456"],
  "minConfidence": 0.9
}
```

**Response**:
```json
{
  "approved": 2,
  "skipped": 0
}
```

---

## 5. Analytics Endpoints

### 5.1 Get Spending Trends

**Endpoint**: `GET /api/analytics/trends?startDate=2025-01-01&endDate=2025-12-31&granularity=month`

**Query Key**: `['analytics', 'trends', dateRange]`

**Response**:
```json
{
  "trends": [
    {
      "period": "2025-12-01",
      "amount": 4287.50,
      "transactionCount": 156,
      "categoryBreakdown": [
        { "category": "Food", "amount": 1200, "percentage": 28 },
        { "category": "Shopping", "amount": 980, "percentage": 23 }
      ]
    }
  ]
}
```

### 5.2 Get Category Breakdown

**Endpoint**: `GET /api/analytics/categories?startDate=2025-12-01&endDate=2025-12-31`

**Query Key**: `['analytics', 'categories', dateRange]`

**Response**:
```json
{
  "categories": [
    {
      "category": "Food & Dining",
      "amount": 1200,
      "percentage": 28,
      "transactionCount": 45,
      "color": "#10b981"
    }
  ]
}
```

### 5.3 Get Top Merchants

**Endpoint**: `GET /api/analytics/merchants?startDate=2025-12-01&endDate=2025-12-31&limit=10`

**Query Key**: `['analytics', 'merchants', dateRange]`

**Response**:
```json
{
  "merchants": [
    {
      "merchant": "Amazon",
      "totalAmount": 456.78,
      "transactionCount": 12,
      "averageAmount": 38.07,
      "trend": 15.5
    }
  ]
}
```

### 5.4 Get Subscription Detection

**Endpoint**: `GET /api/analytics/subscriptions`

**Query Key**: `['analytics', 'subscriptions']`

**Response**:
```json
{
  "subscriptions": [
    {
      "merchant": "Netflix",
      "estimatedAmount": 15.99,
      "frequency": "monthly",
      "lastCharge": "2025-12-15",
      "nextExpected": "2025-01-15",
      "confidence": 0.95
    }
  ]
}
```

---

## 6. Report Endpoints

### 6.1 Generate Report

**Endpoint**: `POST /api/reports/generate`

**Request**:
```json
{
  "startDate": "2025-12-01",
  "endDate": "2025-12-31",
  "categories": ["all"],
  "includeReceipts": true,
  "format": "pdf"
}
```

**Response**:
```json
{
  "id": "rpt-123",
  "status": "generating",
  "estimatedTime": 30
}
```

### 6.2 Get Report Status

**Endpoint**: `GET /api/reports/{id}`

**Query Key**: `['reports', id]`

**Polling**: While status === 'generating'

**Response**:
```json
{
  "id": "rpt-123",
  "status": "complete",
  "downloadUrl": "https://storage.../rpt-123.pdf",
  "generatedAt": "2025-12-21T10:35:00Z",
  "summary": {
    "totalAmount": 4287.50,
    "transactionCount": 156,
    "dateRange": {
      "start": "2025-12-01",
      "end": "2025-12-31"
    }
  }
}
```

---

## Query Key Hierarchy

```text
dashboard
├── metrics          # Polling: 30s
├── activity         # Polling: 30s
└── actions          # Polling: 30s

receipts
├── [list params]    # Paginated list
└── {id}             # Single receipt

transactions
└── [filter params]  # Filtered, paginated list

matching
└── pending          # Paginated queue

analytics
├── trends
├── categories
├── merchants
└── subscriptions

reports
└── {id}             # Report status/download
```

---

## Error Responses

All endpoints return standard error format:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "File size exceeds maximum allowed (20MB)",
    "details": {
      "field": "file",
      "maxSize": 20971520,
      "actualSize": 25000000
    }
  }
}
```

**Common Error Codes**:
- `VALIDATION_ERROR` (400)
- `UNAUTHORIZED` (401)
- `FORBIDDEN` (403)
- `NOT_FOUND` (404)
- `RATE_LIMITED` (429)
- `SERVER_ERROR` (500)
