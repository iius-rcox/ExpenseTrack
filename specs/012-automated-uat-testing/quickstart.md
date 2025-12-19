# UAT Testing Quickstart Guide

**Feature Branch**: `012-automated-uat-testing`
**Date**: 2025-12-19

## Overview

This guide provides step-by-step instructions for Claude Code to execute automated UAT tests against the ExpenseFlow staging API.

## Detailed UAT Documentation

For detailed test procedures, see individual user story documents:

| User Story | Document | Description |
|------------|----------|-------------|
| US-1 | [uat-us1-receipts.md](./uat-us1-receipts.md) | Receipt Upload and OCR Processing |
| US-2 | [uat-us2-statements.md](./uat-us2-statements.md) | Statement Import and Fingerprinting |
| US-3 | [uat-us3-matching.md](./uat-us3-matching.md) | Receipt-Transaction Matching |
| US-4 | [uat-us4-reports.md](./uat-us4-reports.md) | Report Generation |
| US-5 | [uat-us5-results.md](./uat-us5-results.md) | Test Results Format |
| Complete | [uat-complete.md](./uat-complete.md) | Unified Execution Guide |

---

## Prerequisites

### Environment Variables

Ensure these environment variables are set before running tests:

```bash
EXPENSEFLOW_API_URL=https://staging.expense.ii-us.com/api
EXPENSEFLOW_API_KEY=<your-api-key>
EXPENSEFLOW_USER_TOKEN=<your-user-token>
```

| Variable | Required | Description |
|----------|----------|-------------|
| `EXPENSEFLOW_API_URL` | Yes | Base URL for the ExpenseFlow API (staging environment) |
| `EXPENSEFLOW_API_KEY` | No | API key for service-to-service authentication (if applicable) |
| `EXPENSEFLOW_USER_TOKEN` | Yes | JWT bearer token for authenticated user requests |

**Obtaining a User Token**:
1. Authenticate via Entra ID through the frontend application
2. Retrieve the access token from browser developer tools
3. Token format: `eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...`

**Token Expiration**: Tokens typically expire after 1 hour. Refresh before long test runs.

### Test Data Location

```
test-data/
├── receipts/           # 19 receipt images
├── statements/
│   └── chase.csv       # 484 transactions
└── expected-values.json # Golden assertion data
```

---

## Test Execution Flow

### Phase 1: Setup

1. **Clean up existing test data**
   ```
   POST /api/test/cleanup
   Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
   Content-Type: application/json

   {}
   ```

   Expected response:
   ```json
   {
     "success": true,
     "deletedCounts": { ... },
     "durationMs": ...
   }
   ```

2. **Load expected values**
   - Read `test-data/expected-values.json`
   - Parse and store for assertions

### Phase 2: Receipt Upload Tests (P1)

Execute for each receipt file in `test-data/receipts/`:

1. **Upload receipt**
   ```
   POST /api/receipts
   Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
   Content-Type: multipart/form-data

   files: [receipt file]
   ```

2. **Poll for completion** (5-second intervals, 2-minute timeout)
   ```
   GET /api/receipts/{receiptId}
   Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
   ```

   Poll until `status` is one of: `Ready`, `Unmatched`, `Error`

3. **Validate against expected values**
   - Check `status` matches `expectedStatus`
   - Check `amountExtracted` within `amountTolerance` of `expectedAmount`
   - Check `dateExtracted` matches `expectedDate`
   - Check confidence scores >= `minConfidence`

4. **Record result**
   ```json
   {
     "name": "Receipt Upload - {filename}",
     "status": "passed|failed",
     "durationMs": ...,
     "assertions": [...]
   }
   ```

### Phase 3: Statement Import Tests (P1)

1. **Analyze statement**
   ```
   POST /api/statements/analyze
   Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
   Content-Type: multipart/form-data

   file: chase.csv
   ```

2. **Validate column detection**
   - Check `headers` contains expected columns
   - Check `rowCount` matches expected (484)
   - Check `mappingOptions` includes fingerprint or AI inference

3. **Import statement**
   ```
   POST /api/statements/import
   Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
   Content-Type: application/json

   {
     "analysisId": "{analysisId from analyze}",
     "columnMapping": {
       "Transaction Date": "date",
       "Amount": "amount",
       "Description": "description",
       "Post Date": "postDate"
     },
     "dateFormat": "MM/dd/yyyy",
     "amountSign": "negative_charges"
   }
   ```

4. **Validate import results**
   - Check `imported` count matches expected
   - Check `skipped` within acceptable range
   - Check no unexpected duplicates

### Phase 4: Matching Tests (P2)

**Prerequisite**: Phase 2 and Phase 3 must pass

1. **Trigger auto-match**
   ```
   POST /api/matching/auto
   Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
   Content-Type: application/json

   {}
   ```

2. **Get proposals**
   ```
   GET /api/matching/proposals
   Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
   ```

3. **Validate expected matches**
   - For each entry in `expectedMatches`:
     - Find proposal matching receipt and transaction
     - Check `confidenceScore` >= `minConfidence`

4. **Confirm a match**
   ```
   POST /api/matching/{matchId}/confirm
   Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
   ```

5. **Validate receipt status changed**
   - Check receipt status is now `Matched`

### Phase 5: Report Generation Tests (P2)

**Prerequisite**: Phase 4 must pass

1. **Request draft report**
   ```
   POST /api/reports/draft
   Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
   Content-Type: application/json

   {
     "startDate": "2025-11-01",
     "endDate": "2025-12-31"
   }
   ```

2. **Validate report contents**
   - Check report includes matched expenses
   - Check category summaries present
   - Check unmatched items flagged

---

## Test Result Output Format

After all tests complete, output structured JSON:

```json
{
  "testRun": {
    "id": "uuid",
    "startTime": "2025-12-19T10:00:00Z",
    "endTime": "2025-12-19T10:12:34Z",
    "durationMs": 754000,
    "environment": "staging",
    "summary": {
      "total": 25,
      "passed": 24,
      "failed": 1,
      "skipped": 0
    }
  },
  "results": [
    {
      "name": "Receipt Upload - 20251211_212334.jpg",
      "scenario": "US-1.1",
      "status": "passed",
      "durationMs": 2340,
      "assertions": [
        { "field": "status", "expected": "Ready", "actual": "Ready", "passed": true },
        { "field": "amount", "expected": 84.00, "actual": 84.00, "passed": true, "tolerance": 0.01 }
      ]
    }
  ]
}
```

---

## Error Handling

### Dependency Failures

When a P1 test fails, skip dependent P2 tests:

```json
{
  "name": "Matching - Auto-match",
  "scenario": "US-3.1",
  "status": "skipped",
  "skipReason": "Dependency 'Receipt Upload' failed"
}
```

### API Errors

For non-2xx responses, record the error:

```json
{
  "name": "Receipt Upload - corrupted.jpg",
  "status": "failed",
  "error": {
    "message": "API returned 400 Bad Request",
    "expected": "201 Created",
    "actual": "400 Bad Request",
    "detail": "Invalid file format"
  }
}
```

### Timeouts

For polling timeouts:

```json
{
  "name": "Receipt Upload - large.pdf",
  "status": "failed",
  "error": {
    "message": "Timeout waiting for receipt processing",
    "expected": "Ready or Unmatched within 2 minutes",
    "actual": "Still Processing after 120000ms"
  }
}
```

---

## Test Scenarios Reference

| ID | Scenario | Priority | Dependencies |
|----|----------|----------|--------------|
| US-1.1 | Upload 19 receipts | P1 | None |
| US-1.2 | OCR extraction accuracy | P1 | US-1.1 |
| US-1.3 | RDU parking specific values | P1 | US-1.1 |
| US-1.4 | PDF receipt processing | P1 | US-1.1 |
| US-2.1 | Import Chase statement | P1 | None |
| US-2.2 | Transaction fingerprinting | P1 | US-2.1 |
| US-2.3 | RDU transaction categorization | P1 | US-2.1 |
| US-2.4 | Amazon vendor normalization | P1 | US-2.1 |
| US-3.1 | Auto-match execution | P2 | US-1.1, US-2.1 |
| US-3.2 | RDU receipt-transaction match | P2 | US-3.1 |
| US-3.3 | Match confirmation | P2 | US-3.1 |
| US-3.4 | Unmatched receipts remain | P2 | US-3.1 |
| US-4.1 | Draft report generation | P2 | US-3.1 |
| US-4.2 | Category summaries | P2 | US-4.1 |
| US-4.3 | Unmatched flagging | P2 | US-4.1 |
| US-5.1 | Pass/fail summary | P3 | All |
| US-5.2 | Failure details | P3 | All |
| US-5.3 | Execution time reporting | P3 | All |

---

## Success Criteria Checklist

- [ ] SC-001: All 19 receipts processed within 5 minutes
- [ ] SC-002: ≥90% receipts reach Ready/Unmatched (vs Error)
- [ ] SC-003: All 484 transactions imported
- [ ] SC-004: RDU parking match proposed correctly
- [ ] SC-005: Complete UAT < 15 minutes
- [ ] SC-006: Clear failure diagnostics for any issues
- [ ] SC-007: Test is idempotent (re-runnable)
