# API Contracts: Vendor Name Extraction

**Feature**: 025-vendor-extraction
**Date**: 2026-01-05

## No New Contracts Required

This feature does not introduce any new API endpoints. It modifies the response of an existing endpoint by populating the `Vendor` field with extracted vendor names instead of raw descriptions.

## Existing Endpoint (No Changes)

### GET /api/categorization/{transactionId}

**Response**: `TransactionCategorizationDto`

```json
{
  "transactionId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "normalizedDescription": "Amazon Marketplace Purchase",
  "vendor": "Amazon",  // ← This field now contains extracted vendor name
  "gl": {
    "topSuggestion": { ... },
    "alternatives": [ ... ]
  },
  "department": {
    "topSuggestion": { ... },
    "alternatives": [ ... ]
  }
}
```

## Behavior Change

| Field | Before | After |
|-------|--------|-------|
| `vendor` | `"AMZN MKTP US*2K7XY9Z03"` | `"Amazon"` |

The field type, structure, and all other response fields remain unchanged. This is a backward-compatible enhancement - consumers expecting a vendor string will still receive one.
