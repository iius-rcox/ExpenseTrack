# UAT User Story 4: End-to-End Workflow Validation (Report Generation)

**User Story**: As a tester using Claude Code, I want to execute a complete test workflow from upload to report generation, so that I can validate the entire system integration.

**Priority**: P2
**Independent Test**: No - requires US3 (matches confirmed) to complete first

---

## Prerequisites

1. **Environment Variables Set**:
   ```bash
   EXPENSEFLOW_API_URL=https://staging.expense.ii-us.com/api
   EXPENSEFLOW_USER_TOKEN=<your-user-token>
   ```

2. **Prior User Stories Completed**:
   - ✅ US1: Receipts uploaded and processed
   - ✅ US2: Transactions imported and fingerprinted
   - ✅ US3: At least one match confirmed

---

## Dependencies

This test phase depends on successful completion of:

| Dependency | Required State |
|------------|----------------|
| US-3.3 | At least 1 match confirmed |

**If dependencies fail**: Skip US4 tests with reason "Dependency failed"

---

## Test Procedure

### Step 1: Request Draft Report

Generate a draft expense report for the test period:

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

**Expected Response** (201 Created):
```json
{
  "reportId": "uuid",
  "status": "Draft",
  "title": "UAT Test Report",
  "period": {
    "startDate": "2025-11-01",
    "endDate": "2025-12-31"
  },
  "summary": {
    "totalExpenses": 1500.00,
    "matchedReceipts": 1,
    "unmatchedReceipts": 18,
    "categorySummary": {
      "Travel": 84.00,
      "Food & Drink": 500.00,
      "Shopping": 916.00
    }
  }
}
```

---

### Step 2: Retrieve Report Details

Get full report contents:

```http
GET /api/reports/{reportId}
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
```

**Expected Response** (200 OK):
```json
{
  "id": "uuid",
  "status": "Draft",
  "title": "UAT Test Report",
  "period": {
    "startDate": "2025-11-01",
    "endDate": "2025-12-31"
  },
  "lines": [
    {
      "id": "uuid",
      "transactionId": "uuid",
      "receiptId": "uuid",
      "description": "RDUAA PUBLIC PARKING",
      "amount": 84.00,
      "date": "2025-12-11",
      "category": "Travel",
      "hasReceipt": true,
      "receiptFilename": "20251211_212334.jpg"
    }
  ],
  "categorySummary": [
    { "category": "Travel", "total": 84.00, "count": 1 },
    { "category": "Food & Drink", "total": 500.00, "count": 15 }
  ],
  "unmatchedReceipts": [
    {
      "id": "uuid",
      "filename": "20251103_075948.jpg",
      "amount": 25.00,
      "date": "2025-11-03",
      "status": "Unmatched"
    }
  ]
}
```

---

## Specific Test Cases

### US-4.1: Draft Report Generation

**Given**: Matched receipts and categorized transactions
**When**: Claude Code requests a draft expense report
**Then**: A report should be generated containing all matched expenses

**Assertions**:
```json
[
  { "field": "status", "expected": "Draft" },
  { "field": "reportId", "expected": "not null" },
  { "field": "lines.length", "expected": ">= 1" }
]
```

---

### US-4.2: Report Content Validation

**Given**: The generated report
**When**: Claude Code retrieves report details
**Then**: It should include matched expenses

**Assertions**:
```json
[
  { "field": "lines", "expected": "includes matched RDU transaction" },
  { "field": "lines[*].hasReceipt", "expected": true, "for": "matched items" }
]
```

**Validation Logic**:
```pseudocode
rduLine = lines.find(l => l.description.includes("RDUAA"))
assert rduLine != null
assert rduLine.hasReceipt == true
assert rduLine.amount == 84.00
```

---

### US-4.3: Category Summary Validation

**Given**: The generated report
**When**: Claude Code retrieves report details
**Then**: It should include expense summaries grouped by category

**Assertions**:
```json
[
  { "field": "categorySummary.length", "expected": ">= 1" },
  { "field": "categorySummary[*].category", "expected": "Travel exists" },
  { "field": "categorySummary[Travel].total", "expected": 84.00 }
]
```

---

### US-4.4: Unmatched Receipt Flagging

**Given**: Unmatched receipts exist
**When**: Generating the report
**Then**: Unmatched receipts should be flagged for review

**Assertions**:
```json
[
  { "field": "unmatchedReceipts.length", "expected": ">= 1" },
  { "field": "unmatchedReceipts[*].status", "expected": "Unmatched" }
]
```

**Note**: With 19 receipts and only 1 RDU match expected, at least 18 should be in `unmatchedReceipts`.

---

## Error Handling

### Dependency Failure

If US3 tests failed (no confirmed matches):

```json
{
  "name": "Draft Report Generation",
  "scenario": "US-4.1",
  "status": "skipped",
  "skipReason": "Dependency 'Match Confirmation (US-3.3)' failed"
}
```

### Empty Report

If no expenses fall within the date range:

```json
{
  "name": "Report Content Validation",
  "scenario": "US-4.2",
  "status": "failed",
  "error": {
    "message": "Report contains no expense lines",
    "expected": ">= 1 expense line",
    "actual": "0 lines"
  }
}
```

---

## Test Output Format

```json
{
  "scenario": "US-4",
  "name": "End-to-End Workflow Validation",
  "status": "passed",
  "durationMs": 2500,
  "results": [
    {
      "name": "Draft Report Generation",
      "scenario": "US-4.1",
      "status": "passed",
      "durationMs": 1200,
      "assertions": [
        { "field": "status", "expected": "Draft", "actual": "Draft", "passed": true },
        { "field": "reportId", "expected": "not null", "actual": "uuid-value", "passed": true }
      ]
    },
    {
      "name": "Report Content Validation",
      "scenario": "US-4.2",
      "status": "passed",
      "durationMs": 500,
      "assertions": [
        { "field": "lines.length", "expected": ">= 1", "actual": 1, "passed": true },
        { "field": "rduLine.hasReceipt", "expected": true, "actual": true, "passed": true }
      ]
    },
    {
      "name": "Category Summary Validation",
      "scenario": "US-4.3",
      "status": "passed",
      "durationMs": 300,
      "assertions": [
        { "field": "categorySummary.length", "expected": ">= 1", "actual": 3, "passed": true },
        { "field": "Travel.total", "expected": 84.00, "actual": 84.00, "passed": true }
      ]
    },
    {
      "name": "Unmatched Receipt Flagging",
      "scenario": "US-4.4",
      "status": "passed",
      "durationMs": 500,
      "assertions": [
        { "field": "unmatchedReceipts.length", "expected": ">= 1", "actual": 18, "passed": true }
      ]
    }
  ],
  "summary": {
    "total": 4,
    "passed": 4,
    "failed": 0,
    "skipped": 0
  }
}
```
