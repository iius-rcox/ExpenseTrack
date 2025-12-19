# Complete UAT Execution Guide

**Feature Branch**: `012-automated-uat-testing`
**Date**: 2025-12-19

This document provides a unified guide for executing all UAT test scenarios against the ExpenseFlow staging API. It consolidates the individual user story documents into a single executable reference.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Test Execution Order](#test-execution-order)
3. [Phase 0: Cleanup](#phase-0-cleanup)
4. [Phase 1: Receipt Upload (US-1)](#phase-1-receipt-upload-us-1)
5. [Phase 2: Statement Import (US-2)](#phase-2-statement-import-us-2)
6. [Phase 3: Matching (US-3)](#phase-3-matching-us-3)
7. [Phase 4: Reports (US-4)](#phase-4-reports-us-4)
8. [Test Results Output (US-5)](#test-results-output-us-5)
9. [Success Criteria Checklist](#success-criteria-checklist)

---

## Prerequisites

### Environment Configuration

```bash
# Required environment variables
EXPENSEFLOW_API_URL=https://staging.expense.ii-us.com/api
EXPENSEFLOW_USER_TOKEN=<your-jwt-token>
```

### Test Data Files

```
test-data/
├── receipts/               # 19 receipt images (JPG, PNG, PDF)
│   ├── 20251211_212334.jpg  # RDU parking receipt ($84.00)
│   ├── Receipt 2768.pdf     # PDF receipt
│   └── ... (17 more)
├── statements/
│   └── chase.csv           # 484 transactions
└── expected-values.json    # Golden assertion data
```

### Expected Values File

The `test-data/expected-values.json` contains:
- Receipt expected amounts, dates, vendors
- Transaction fingerprinting expectations
- Expected match pairs with confidence thresholds
- Statement import configuration

---

## Test Execution Order

```
┌──────────────────────────────────────────────────────────┐
│                    Phase 0: Cleanup                       │
│                  POST /api/test/cleanup                   │
└────────────────────────┬─────────────────────────────────┘
                         │
         ┌───────────────┴───────────────┐
         │                               │
         ▼                               ▼
┌─────────────────────┐   ┌─────────────────────────┐
│  Phase 1: Receipts  │   │  Phase 2: Statements    │
│       (US-1)        │   │        (US-2)           │
│   Priority: P1      │   │    Priority: P1         │
│   19 files upload   │   │  484 transactions       │
└─────────┬───────────┘   └───────────┬─────────────┘
          │                           │
          └─────────────┬─────────────┘
                        │
                        ▼
            ┌───────────────────────┐
            │   Phase 3: Matching   │
            │        (US-3)         │
            │    Priority: P2       │
            │  Requires US-1 + US-2 │
            └───────────┬───────────┘
                        │
                        ▼
            ┌───────────────────────┐
            │   Phase 4: Reports    │
            │        (US-4)         │
            │    Priority: P2       │
            │   Requires US-3       │
            └───────────┬───────────┘
                        │
                        ▼
            ┌───────────────────────┐
            │  Test Results Output  │
            │        (US-5)         │
            │    Priority: P3       │
            └───────────────────────┘
```

---

## Phase 0: Cleanup

**Purpose**: Ensure idempotent test runs by removing all test data from previous executions.

### Request

```http
POST /api/test/cleanup
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
Content-Type: application/json

{}
```

### Expected Response (200 OK)

```json
{
  "success": true,
  "deletedCounts": {
    "receipts": 19,
    "transactions": 484,
    "matches": 1,
    "imports": 1
  },
  "durationMs": 1500,
  "warnings": []
}
```

### Assertions

| Field | Expected | Description |
|-------|----------|-------------|
| `success` | `true` | Cleanup completed without fatal errors |
| `durationMs` | `< 30000` | Cleanup completes within 30 seconds |

**Note**: First run will have `deletedCounts` of 0 for all entities.

---

## Phase 1: Receipt Upload (US-1)

**Detailed Documentation**: [uat-us1-receipts.md](./uat-us1-receipts.md)

### Step 1.1: Upload All Receipts

For each file in `test-data/receipts/`:

```http
POST /api/receipts
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
Content-Type: multipart/form-data

files: [receipt file]
```

**Store**: Receipt IDs for polling

### Step 1.2: Poll for Completion

Poll each receipt at 5-second intervals (2-minute timeout):

```http
GET /api/receipts/{receiptId}
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
```

**Terminal Statuses**: `Ready`, `Unmatched`, `Error`

### Step 1.3: Validate Key Receipts

#### RDU Parking Receipt (`20251211_212334.jpg`)

```json
{
  "expectedAmount": 84.00,
  "expectedDate": "2025-12-11",
  "expectedVendor": "RDU Airport Parking",
  "amountTolerance": 0.01
}
```

#### PDF Receipt (`Receipt 2768.pdf`)

```json
{
  "expectedStatus": "Ready",
  "minConfidence": 0.70
}
```

### US-1 Test Cases

| ID | Test | Assertions |
|----|------|------------|
| US-1.1 | Bulk Receipt Upload | 19 uploads succeed, 0 failed |
| US-1.2 | OCR Extraction Accuracy | Confidence >= 70% |
| US-1.3 | RDU Parking Values | Amount=84.00, Date=2025-12-11 |
| US-1.4 | PDF Processing | Status != Error |
| US-1.5 | Duplicate Detection | Warning on re-upload |

### US-1 Success Criteria

- [ ] SC-001: All 19 receipts processed within 5 minutes
- [ ] SC-002: ≥90% receipts reach Ready/Unmatched status

---

## Phase 2: Statement Import (US-2)

**Detailed Documentation**: [uat-us2-statements.md](./uat-us2-statements.md)

### Step 2.1: Analyze Statement

```http
POST /api/statements/analyze
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
Content-Type: multipart/form-data

file: chase.csv
```

**Store**: `analysisId` for import

### Step 2.2: Import Statement

```http
POST /api/statements/import
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
Content-Type: application/json

{
  "analysisId": "{analysisId}",
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

### Step 2.3: Validate Import Results

```json
{
  "summary": {
    "total": 484,
    "imported": 484,
    "skipped": 0,
    "duplicates": 0
  }
}
```

### Step 2.4: Validate RDU Transaction

```http
GET /api/transactions?search=RDUAA
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
```

Expected fingerprinting:
- Category: `Travel`
- Normalized Vendor: `RDU Airport Parking`
- Amount: `-84.00`

### US-2 Test Cases

| ID | Test | Assertions |
|----|------|------------|
| US-2.1 | Import Chase Statement | 484 imported, 0 skipped |
| US-2.2 | Transaction Fingerprinting | All categorized |
| US-2.3 | RDU Categorization | Category=Travel |
| US-2.4 | Amazon Normalization | Vendor=Amazon |
| US-2.5 | Duplicate Detection | 484 duplicates on re-import |
| US-2.6 | Malformed Row Handling | Skipped rows reported |

### US-2 Success Criteria

- [ ] SC-003: All 484 transactions imported

---

## Phase 3: Matching (US-3)

**Detailed Documentation**: [uat-us3-matching.md](./uat-us3-matching.md)

**Prerequisites**: US-1 and US-2 must pass

### Step 3.1: Trigger Auto-Match

```http
POST /api/matching/auto
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
Content-Type: application/json

{}
```

### Step 3.2: Retrieve Proposals

```http
GET /api/matching/proposals
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
```

### Step 3.3: Validate RDU Match

Expected match:
- Receipt: `20251211_212334.jpg` ($84.00)
- Transaction: `RDUAA PUBLIC PARKING` (-$84.00)
- Confidence: >= 80%

### Step 3.4: Confirm Match

```http
POST /api/matching/{matchId}/confirm
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
```

### Step 3.5: Verify Receipt Status

```http
GET /api/receipts/{receiptId}
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
```

Expected: `status: "Matched"`

### US-3 Test Cases

| ID | Test | Assertions |
|----|------|------------|
| US-3.1 | Auto-Match Execution | >= 1 proposal created |
| US-3.2 | RDU Match | Confidence >= 80% |
| US-3.3 | Match Confirmation | Success, status=Matched |
| US-3.4 | Unmatched Receipts | Status=Unmatched |
| US-3.5 | Amount Tolerance | Within $0.01 tolerance |
| US-3.6 | Date Window Mismatch | No proposals outside window |

### US-3 Success Criteria

- [ ] SC-004: RDU parking match proposed correctly

---

## Phase 4: Reports (US-4)

**Detailed Documentation**: [uat-us4-reports.md](./uat-us4-reports.md)

**Prerequisites**: US-3 must pass

### Step 4.1: Generate Draft Report

```http
POST /api/reports/draft
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
Content-Type: application/json

{
  "startDate": "2025-11-01",
  "endDate": "2025-12-31",
  "title": "UAT Test Report"
}
```

### Step 4.2: Retrieve Report Details

```http
GET /api/reports/{reportId}
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
```

### Step 4.3: Validate Report Contents

- Contains matched RDU expense
- Category summary includes "Travel"
- Unmatched receipts flagged

### US-4 Test Cases

| ID | Test | Assertions |
|----|------|------------|
| US-4.1 | Draft Report Generation | Status=Draft, reportId present |
| US-4.2 | Report Content | Contains matched expenses |
| US-4.3 | Category Summary | Travel category present |
| US-4.4 | Unmatched Flagging | >= 1 unmatched receipts |

---

## Test Results Output (US-5)

**Detailed Documentation**: [uat-us5-results.md](./uat-us5-results.md)

### Final Output Structure

```json
{
  "testRun": {
    "id": "uuid",
    "startedAt": "2025-12-19T10:00:00Z",
    "completedAt": "2025-12-19T10:05:30Z",
    "durationMs": 330000,
    "environment": {
      "apiUrl": "https://staging.expense.ii-us.com/api"
    }
  },
  "scenarios": [
    {
      "scenario": "US-1",
      "name": "Receipt Upload and OCR Processing",
      "status": "passed",
      "summary": { "total": 19, "passed": 19, "failed": 0, "skipped": 0 }
    },
    {
      "scenario": "US-2",
      "name": "Statement Import and Transaction Fingerprinting",
      "status": "passed",
      "summary": { "total": 4, "passed": 4, "failed": 0, "skipped": 0 }
    },
    {
      "scenario": "US-3",
      "name": "Automated Receipt-Transaction Matching",
      "status": "passed",
      "summary": { "total": 4, "passed": 4, "failed": 0, "skipped": 0 }
    },
    {
      "scenario": "US-4",
      "name": "End-to-End Workflow Validation",
      "status": "passed",
      "summary": { "total": 4, "passed": 4, "failed": 0, "skipped": 0 }
    }
  ],
  "summary": {
    "totalScenarios": 4,
    "passedScenarios": 4,
    "failedScenarios": 0,
    "skippedScenarios": 0,
    "totalTests": 31,
    "passedTests": 31,
    "failedTests": 0,
    "skippedTests": 0,
    "successRate": 1.0
  }
}
```

---

## Success Criteria Checklist

| ID | Criterion | Target | Measurement |
|----|-----------|--------|-------------|
| SC-001 | Receipt Processing Time | All 19 in 5 min | Sum of durations < 300,000ms |
| SC-002 | Receipt Success Rate | ≥90% | (Ready + Unmatched) / 19 >= 0.90 |
| SC-003 | Transaction Import | 484 transactions | summary.imported == 484 |
| SC-004 | RDU Match Quality | Proposed correctly | confidenceScore >= 0.80 |
| SC-005 | Complete UAT Time | < 15 minutes | testRun.durationMs < 900,000 |
| SC-006 | Failure Diagnostics | Clear errors | All failures have error.message |
| SC-007 | Idempotency | Re-runnable | Cleanup works, no duplicates |

---

## Error Handling Summary

### Dependency Failures

When P1 tests fail, skip dependent P2 tests:

```json
{
  "scenario": "US-3",
  "status": "skipped",
  "skipReason": "Dependency 'Receipt Upload (US-1)' failed"
}
```

### API Errors

For non-2xx responses:

```json
{
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
  "error": {
    "message": "Timeout waiting for receipt processing",
    "expected": "Ready or Unmatched within 2 minutes",
    "actual": "Still Processing after 120000ms"
  }
}
```

---

## Quick Reference: API Endpoints

| Action | Method | Endpoint |
|--------|--------|----------|
| Cleanup | POST | `/api/test/cleanup` |
| Upload Receipt | POST | `/api/receipts` |
| Get Receipt | GET | `/api/receipts/{id}` |
| Analyze Statement | POST | `/api/statements/analyze` |
| Import Statement | POST | `/api/statements/import` |
| Search Transactions | GET | `/api/transactions?search=` |
| Trigger Match | POST | `/api/matching/auto` |
| Get Proposals | GET | `/api/matching/proposals` |
| Confirm Match | POST | `/api/matching/{id}/confirm` |
| Create Report | POST | `/api/reports/draft` |
| Get Report | GET | `/api/reports/{id}` |

All endpoints require `Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}` header.
