# API Contracts: AI Categorization

**Feature**: 006-ai-categorization
**Date**: 2025-12-16
**Base Path**: `/api/v1`

## Authentication

All endpoints require Entra ID Bearer token authentication.

```
Authorization: Bearer {access_token}
```

---

## Description Normalization

### POST /descriptions/normalize

Normalizes a raw bank description to human-readable format.

**Request**:
```json
{
  "rawDescription": "DELTA AIR 0062363598531"
}
```

**Response** (200 OK):
```json
{
  "rawDescription": "DELTA AIR 0062363598531",
  "normalizedDescription": "Delta Airlines - Flight Purchase",
  "extractedVendor": "Delta Airlines",
  "tier": 1,
  "cacheHit": true,
  "confidence": 1.0
}
```

**Response** (200 OK - Tier 3 fallback):
```json
{
  "rawDescription": "NEWVENDOR#12345",
  "normalizedDescription": "New Vendor - Purchase",
  "extractedVendor": "New Vendor",
  "tier": 3,
  "cacheHit": false,
  "confidence": 0.85
}
```

**Error Responses**:
- `400 Bad Request`: Empty or invalid description
- `503 Service Unavailable`: AI service unavailable (includes `retryAfter` header)

---

## GL Code Suggestions

### GET /categorization/transactions/{transactionId}/gl-suggestions

Gets GL code suggestions for a specific transaction.

**Path Parameters**:
- `transactionId` (UUID): Transaction ID

**Response** (200 OK):
```json
{
  "transactionId": "550e8400-e29b-41d4-a716-446655440000",
  "suggestions": [
    {
      "glCode": "66300",
      "glName": "Travel - Airfare",
      "confidence": 0.95,
      "tier": 1,
      "source": "vendor_alias",
      "explanation": "From vendor default: Delta Airlines"
    },
    {
      "glCode": "66400",
      "glName": "Travel - Other",
      "confidence": 0.72,
      "tier": 2,
      "source": "embedding_similarity",
      "explanation": "Similar to: 'United Airlines Flight' (92% match)"
    }
  ],
  "topSuggestion": {
    "glCode": "66300",
    "glName": "Travel - Airfare",
    "confidence": 0.95,
    "tier": 1
  }
}
```

**Response** (200 OK - No suggestions available):
```json
{
  "transactionId": "550e8400-e29b-41d4-a716-446655440000",
  "suggestions": [],
  "topSuggestion": null,
  "message": "No suggestions available. Please categorize manually.",
  "serviceStatus": "degraded"
}
```

**Error Responses**:
- `404 Not Found`: Transaction not found or not owned by user

---

## Department Suggestions

### GET /categorization/transactions/{transactionId}/dept-suggestions

Gets department suggestions for a specific transaction.

**Path Parameters**:
- `transactionId` (UUID): Transaction ID

**Response** (200 OK):
```json
{
  "transactionId": "550e8400-e29b-41d4-a716-446655440000",
  "suggestions": [
    {
      "deptCode": "07",
      "deptName": "Engineering",
      "confidence": 0.95,
      "tier": 1,
      "source": "vendor_alias",
      "explanation": "From vendor default: Delta Airlines"
    }
  ],
  "topSuggestion": {
    "deptCode": "07",
    "deptName": "Engineering",
    "confidence": 0.95,
    "tier": 1
  }
}
```

---

## Combined Categorization

### GET /categorization/transactions/{transactionId}

Gets both GL and department suggestions in a single call.

**Path Parameters**:
- `transactionId` (UUID): Transaction ID

**Response** (200 OK):
```json
{
  "transactionId": "550e8400-e29b-41d4-a716-446655440000",
  "normalizedDescription": "Delta Airlines - Flight to NYC",
  "vendor": "Delta Airlines",
  "gl": {
    "topSuggestion": {
      "glCode": "66300",
      "glName": "Travel - Airfare",
      "confidence": 0.95,
      "tier": 1
    },
    "alternatives": [
      {
        "glCode": "66400",
        "glName": "Travel - Other",
        "confidence": 0.72,
        "tier": 2
      }
    ]
  },
  "department": {
    "topSuggestion": {
      "deptCode": "07",
      "deptName": "Engineering",
      "confidence": 0.95,
      "tier": 1
    },
    "alternatives": []
  }
}
```

---

## User Confirmations

### POST /categorization/transactions/{transactionId}/confirm

Confirms user's categorization selection, triggering learning loop.

**Path Parameters**:
- `transactionId` (UUID): Transaction ID

**Request**:
```json
{
  "glCode": "66300",
  "departmentCode": "07",
  "acceptedSuggestion": true
}
```

**Response** (200 OK):
```json
{
  "transactionId": "550e8400-e29b-41d4-a716-446655440000",
  "glCode": "66300",
  "departmentCode": "07",
  "embeddingCreated": true,
  "vendorAliasUpdated": false,
  "message": "Categorization confirmed. Learning applied."
}
```

**Response** (200 OK - Vendor alias updated):
```json
{
  "transactionId": "550e8400-e29b-41d4-a716-446655440000",
  "glCode": "66300",
  "departmentCode": "07",
  "embeddingCreated": true,
  "vendorAliasUpdated": true,
  "vendorAliasMessage": "Delta Airlines default GL updated to 66300 (3+ confirmations)",
  "message": "Categorization confirmed. Vendor alias updated for future suggestions."
}
```

**Error Responses**:
- `400 Bad Request`: Invalid GL code or department
- `404 Not Found`: Transaction not found

---

### POST /categorization/transactions/{transactionId}/skip

Skips AI suggestion and allows manual categorization (graceful degradation).

**Path Parameters**:
- `transactionId` (UUID): Transaction ID

**Request**:
```json
{
  "reason": "ai_unavailable"
}
```

**Response** (200 OK):
```json
{
  "transactionId": "550e8400-e29b-41d4-a716-446655440000",
  "skipped": true,
  "message": "AI suggestion skipped. Please categorize manually."
}
```

---

## Tier Usage Statistics

### GET /categorization/stats

Gets tier usage statistics for cost monitoring.

**Query Parameters**:
- `startDate` (ISO 8601): Start of date range
- `endDate` (ISO 8601): End of date range
- `operationType` (string, optional): Filter by operation type

**Response** (200 OK):
```json
{
  "period": {
    "start": "2025-12-01T00:00:00Z",
    "end": "2025-12-31T23:59:59Z"
  },
  "summary": {
    "totalOperations": 1250,
    "tier1Count": 875,
    "tier1Percentage": 70.0,
    "tier2Count": 312,
    "tier2Percentage": 25.0,
    "tier3Count": 63,
    "tier3Percentage": 5.0,
    "estimatedCost": 0.15
  },
  "byOperationType": [
    {
      "operationType": "normalization",
      "totalCount": 500,
      "tier1Percentage": 80.0,
      "tier2Percentage": 0.0,
      "tier3Percentage": 20.0
    },
    {
      "operationType": "gl_suggestion",
      "totalCount": 500,
      "tier1Percentage": 60.0,
      "tier2Percentage": 35.0,
      "tier3Percentage": 5.0
    },
    {
      "operationType": "dept_suggestion",
      "totalCount": 250,
      "tier1Percentage": 70.0,
      "tier2Percentage": 28.0,
      "tier3Percentage": 2.0
    }
  ],
  "vendorCandidates": [
    {
      "vendor": "New Coffee Shop",
      "tier3Count": 15,
      "recommendation": "Consider creating vendor alias"
    }
  ]
}
```

---

## Reference Data

### GET /gl-accounts

Gets list of available GL accounts for dropdowns.

**Response** (200 OK):
```json
{
  "accounts": [
    {
      "code": "63300",
      "name": "Software Subscriptions",
      "category": "Technology"
    },
    {
      "code": "66300",
      "name": "Travel - Airfare",
      "category": "Travel"
    },
    {
      "code": "66400",
      "name": "Travel - Other",
      "category": "Travel"
    }
  ],
  "lastSyncedAt": "2025-12-16T08:00:00Z"
}
```

### GET /departments

Gets list of available departments for dropdowns.

**Response** (200 OK):
```json
{
  "departments": [
    {
      "code": "01",
      "name": "Administration"
    },
    {
      "code": "07",
      "name": "Engineering"
    },
    {
      "code": "10",
      "name": "Sales"
    }
  ],
  "lastSyncedAt": "2025-12-16T08:00:00Z"
}
```

---

## WebSocket Events (Optional - Future)

For real-time suggestion updates during bulk categorization:

```
ws://api.expenseflow.com/ws/categorization

// Server → Client: Suggestion ready
{
  "event": "suggestion_ready",
  "transactionId": "...",
  "gl": { ... },
  "department": { ... }
}

// Client → Server: Confirm selection
{
  "action": "confirm",
  "transactionId": "...",
  "glCode": "66300",
  "departmentCode": "07"
}
```

---

## Error Response Format

All error responses follow this format:

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid GL code provided",
    "details": [
      {
        "field": "glCode",
        "message": "GL code '99999' does not exist"
      }
    ]
  },
  "requestId": "req_abc123"
}
```

**Error Codes**:
- `VALIDATION_ERROR`: Invalid request data
- `NOT_FOUND`: Resource not found
- `UNAUTHORIZED`: Authentication required
- `FORBIDDEN`: Insufficient permissions
- `SERVICE_UNAVAILABLE`: AI service temporarily unavailable
- `RATE_LIMITED`: Too many requests
