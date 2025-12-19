# UAT User Story 3: Automated Receipt-Transaction Matching

**User Story**: As a tester using Claude Code, I want to trigger automatic matching between receipts and transactions, so that I can verify the matching engine correctly pairs receipts with their corresponding bank transactions.

**Priority**: P2
**Independent Test**: No - requires US1 (receipts) and US2 (transactions) to complete first

---

## Prerequisites

1. **Environment Variables Set**:
   ```bash
   EXPENSEFLOW_API_URL=https://staging.expense.ii-us.com/api
   EXPENSEFLOW_USER_TOKEN=<your-user-token>
   ```

2. **Prior User Stories Completed**:
   - ✅ US1: 19 receipts uploaded and processed
   - ✅ US2: 484 transactions imported and fingerprinted

3. **Expected Values Available**:
   - `test-data/expected-values.json` with `expectedMatches` array

---

## Dependencies

This test phase depends on successful completion of:

| Dependency | Required State |
|------------|----------------|
| US-1.1 | At least 1 receipt in "Ready" or "Unmatched" status |
| US-2.1 | At least 1 transaction imported |

**If dependencies fail**: Skip US3 tests with reason "Dependency failed"

---

## Test Procedure

### Step 1: Trigger Auto-Match

Request automatic matching between receipts and transactions:

```http
POST /api/matching/auto
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
Content-Type: application/json

{}
```

**Expected Response** (200 OK):
```json
{
  "matchingJobId": "uuid",
  "status": "completed",
  "summary": {
    "receiptsProcessed": 19,
    "proposalsCreated": 1,
    "highConfidenceMatches": 1
  }
}
```

---

### Step 2: Retrieve Match Proposals

Get the list of proposed matches:

```http
GET /api/matching/proposals
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
```

**Expected Response** (200 OK):
```json
{
  "proposals": [
    {
      "id": "uuid",
      "receiptId": "uuid",
      "transactionId": "uuid",
      "confidenceScore": 0.92,
      "matchReasons": [
        "Amount match: $84.00",
        "Date proximity: 0 days",
        "Vendor similarity: 85%"
      ],
      "receipt": {
        "filename": "20251211_212334.jpg",
        "amount": 84.00,
        "date": "2025-12-11"
      },
      "transaction": {
        "description": "RDUAA PUBLIC PARKING",
        "amount": -84.00,
        "date": "2025-12-11"
      }
    }
  ],
  "total": 1
}
```

---

### Step 3: Validate Expected Matches

For each entry in `expectedMatches` from `expected-values.json`:

```json
{
  "receiptFile": "20251211_212334.jpg",
  "transactionDescription": "RDUAA PUBLIC PARKING",
  "minConfidence": 0.80
}
```

**Find matching proposal and validate**:

#### 3.1 Match Exists Validation

```json
{
  "field": "matchExists",
  "expected": true,
  "actual": true,
  "passed": true
}
```

#### 3.2 Confidence Score Validation

```json
{
  "field": "confidenceScore",
  "expected": ">= 0.80",
  "actual": 0.92,
  "passed": true
}
```

---

### Step 4: Confirm a Match

Confirm the RDU parking match to validate the confirmation flow:

```http
POST /api/matching/{matchId}/confirm
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
Content-Type: application/json

{}
```

**Expected Response** (200 OK):
```json
{
  "success": true,
  "match": {
    "id": "uuid",
    "status": "Confirmed",
    "confirmedAt": "2025-12-19T10:30:00Z"
  }
}
```

---

### Step 5: Verify Receipt Status Update

After match confirmation, verify the receipt status changed:

```http
GET /api/receipts/{receiptId}
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
```

**Expected Response**:
```json
{
  "id": "uuid",
  "status": "Matched",
  "matchedTransactionId": "uuid"
}
```

---

## Specific Test Cases

### US-3.1: Auto-Match Execution

**Given**: Processed receipts and imported transactions
**When**: Claude Code triggers auto-matching
**Then**: At least one receipt-transaction match should be proposed

**Assertions**:
```json
[
  { "field": "status", "expected": "completed" },
  { "field": "proposalsCreated", "expected": ">= 1" }
]
```

---

### US-3.2: RDU Receipt-Transaction Match

**Given**: The $84.00 RDU parking receipt and the $84.00 RDUAA PUBLIC PARKING transaction
**When**: Auto-matching runs
**Then**: These should be matched with high confidence (>80%)

**Expected Match**:
```json
{
  "receiptFile": "20251211_212334.jpg",
  "transactionDescription": "RDUAA PUBLIC PARKING",
  "transactionAmount": -84.00,
  "transactionDate": "2025-12-11",
  "minConfidence": 0.80
}
```

**Assertions**:
```json
[
  { "field": "matchExists", "expected": true },
  { "field": "confidenceScore", "expected": ">= 0.80" },
  { "field": "receipt.amount", "expected": 84.00 },
  { "field": "transaction.amount", "expected": -84.00 }
]
```

---

### US-3.3: Match Confirmation

**Given**: Proposed matches
**When**: Claude Code confirms a match
**Then**: The receipt status should change to "Matched"

**Assertions**:
```json
[
  { "field": "confirmResponse.success", "expected": true },
  { "field": "receipt.status", "expected": "Matched" },
  { "field": "receipt.matchedTransactionId", "expected": "not null" }
]
```

---

### US-3.4: Unmatched Receipts Remain

**Given**: Receipts with no matching transactions
**When**: Auto-matching completes
**Then**: Those receipts should remain in "Unmatched" status

**Test Approach**:
1. Get list of all receipts
2. Identify receipts not in any match proposal
3. Verify their status is "Unmatched" (not "Matched" or "Error")

**Assertions**:
```json
[
  { "field": "unmatchedReceipts.length", "expected": ">= 0" },
  { "field": "unmatchedReceipts[*].status", "expected": "Unmatched" }
]
```

---

## Amount Tolerance Matching

### US-3.5: Amount Tolerance Test Case

**Scenario**: Receipt and transaction amounts are close but not exact
**Expected Behavior**: Use configurable tolerance (default $0.01)

**Test Approach**:
1. Check if any matches exist with slight amount differences
2. Validate tolerance is applied correctly

**Tolerance Logic**:
```pseudocode
isAmountMatch = abs(receipt.amount - transaction.amount) <= tolerance
where tolerance = 0.01 (default)
```

**Assertions**:
- Matches with exact amounts should have higher confidence
- Matches within tolerance should still be proposed (possibly lower confidence)

---

## Date Window Mismatch Handling

### US-3.6: Date Window Mismatch Test Case

**Scenario**: Receipt date doesn't match any transaction within a reasonable window
**Expected Behavior**: Receipt remains unmatched with no suggestions

**Test Approach**:
1. Identify receipts with dates far from any transaction
2. Verify they remain in "Unmatched" status
3. Verify no match proposals with low confidence were created

**Date Window Logic**:
```pseudocode
dateProximity = abs(receipt.date - transaction.date)
isDateMatch = dateProximity <= 7 days (configurable)
```

**Assertions**:
```json
[
  { "field": "status", "expected": "Unmatched" },
  { "field": "matchProposals.length", "expected": 0 }
]
```

---

## Error Handling

### Dependency Failure

If US1 or US2 tests failed:

```json
{
  "name": "Auto-Match Execution",
  "scenario": "US-3.1",
  "status": "skipped",
  "skipReason": "Dependency 'Receipt Upload (US-1)' failed"
}
```

### No Matches Found

If no matches are proposed (unexpected):

```json
{
  "name": "RDU Receipt-Transaction Match",
  "scenario": "US-3.2",
  "status": "failed",
  "error": {
    "message": "Expected match not found",
    "expected": "Match between 20251211_212334.jpg and RDUAA PUBLIC PARKING",
    "actual": "No matching proposals"
  }
}
```

---

## Success Criteria Validation

| Criterion | Target | Measurement |
|-----------|--------|-------------|
| SC-004 | RDU match proposed | Match exists with confidence >= 0.80 |
| Match quality | Confidence >= 80% | `confidenceScore >= 0.80` |

---

## Test Output Format

```json
{
  "scenario": "US-3",
  "name": "Automated Receipt-Transaction Matching",
  "status": "passed",
  "durationMs": 3500,
  "results": [
    {
      "name": "Auto-Match Trigger",
      "scenario": "US-3.1",
      "status": "passed",
      "durationMs": 1200,
      "assertions": [
        { "field": "status", "expected": "completed", "actual": "completed", "passed": true },
        { "field": "proposalsCreated", "expected": ">= 1", "actual": 1, "passed": true }
      ]
    },
    {
      "name": "RDU Receipt-Transaction Match",
      "scenario": "US-3.2",
      "status": "passed",
      "durationMs": 500,
      "assertions": [
        { "field": "matchExists", "expected": true, "actual": true, "passed": true },
        { "field": "confidenceScore", "expected": ">= 0.80", "actual": 0.92, "passed": true }
      ]
    },
    {
      "name": "Match Confirmation",
      "scenario": "US-3.3",
      "status": "passed",
      "durationMs": 800,
      "assertions": [
        { "field": "success", "expected": true, "actual": true, "passed": true },
        { "field": "receipt.status", "expected": "Matched", "actual": "Matched", "passed": true }
      ]
    },
    {
      "name": "Unmatched Receipts",
      "scenario": "US-3.4",
      "status": "passed",
      "durationMs": 400,
      "assertions": [
        { "field": "unmatchedCount", "expected": ">= 0", "actual": 18, "passed": true }
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
