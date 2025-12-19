# UAT User Story 1: Receipt Upload and OCR Processing

**User Story**: As a tester using Claude Code, I want to upload all test receipt images and verify they are processed correctly, so that I can confirm the receipt ingestion pipeline works end-to-end.

**Priority**: P1 (MVP)
**Independent Test**: Yes - can be tested without other user stories

---

## Prerequisites

1. **Environment Variables Set**:
   ```bash
   EXPENSEFLOW_API_URL=https://staging.expense.ii-us.com/api
   EXPENSEFLOW_USER_TOKEN=<your-user-token>
   ```

2. **Test Data Available**:
   - 19 receipt files in `test-data/receipts/`
   - Expected values in `test-data/expected-values.json`

3. **Cleanup Executed** (for idempotent runs):
   ```http
   POST /api/test/cleanup
   Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
   Content-Type: application/json

   {}
   ```

---

## Test Procedure

### Step 1: Upload All Receipts

For each file in `test-data/receipts/`:

```http
POST /api/receipts
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
Content-Type: multipart/form-data

files: [receipt file]
```

**Expected Response** (201 Created):
```json
{
  "receipts": [
    {
      "id": "uuid",
      "filename": "20251211_212334.jpg",
      "status": "Uploaded",
      "blobUrl": "https://..."
    }
  ],
  "failed": []
}
```

**Store**: Receipt IDs for polling

---

### Step 2: Poll for Completion

For each uploaded receipt, poll status at **5-second intervals** with **2-minute timeout**:

```http
GET /api/receipts/{receiptId}
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
```

**Polling Logic**:
```pseudocode
start_time = now()
timeout = 120 seconds
poll_interval = 5 seconds

while (now() - start_time < timeout):
    response = GET /api/receipts/{receiptId}

    if response.status in ["Ready", "Unmatched", "Error"]:
        return response  # Terminal status reached

    wait(poll_interval)

return TIMEOUT_ERROR
```

**Terminal Statuses**:
- `Ready` - Processing complete, data extracted
- `Unmatched` - Processing complete, no matching transaction
- `Error` - Processing failed

---

### Step 3: Validate Extraction Results

For each receipt that reaches terminal status, validate against `expected-values.json`:

#### 3.1 Status Validation

```json
{
  "field": "status",
  "expected": "Ready",
  "actual": "{response.status}",
  "passed": true
}
```

#### 3.2 Amount Validation (if expectedAmount not null)

```json
{
  "field": "amount",
  "expected": 84.00,
  "actual": "{response.amountExtracted}",
  "tolerance": 0.01,
  "passed": true
}
```

**Tolerance Logic**:
```
passed = abs(expected - actual) <= tolerance
```

#### 3.3 Date Validation (if expectedDate not null)

```json
{
  "field": "date",
  "expected": "2025-12-11",
  "actual": "{response.dateExtracted}",
  "passed": true
}
```

#### 3.4 Confidence Validation

For each confidence score field:
```json
{
  "field": "amountConfidence",
  "expected": ">= 0.70",
  "actual": "{response.confidenceScores.amount}",
  "passed": true
}
```

---

## Specific Test Cases

### US-1.1: Bulk Receipt Upload

**Given**: 19 receipt files in `test-data/receipts/`
**When**: Claude Code uploads each receipt
**Then**: All 19 uploads succeed with 201 status

**Assertions**:
- `response.receipts.length == 1` for each upload
- `response.failed.length == 0` for each upload
- Each receipt has `status == "Uploaded"`

---

### US-1.2: OCR Extraction Accuracy

**Given**: Uploaded receipts are processing
**When**: OCR extraction completes
**Then**: Each receipt should have extracted data with confidence >= 70%

**Assertions**:
- For each receipt where `expectedAmount != null`:
  - `|amountExtracted - expectedAmount| <= amountTolerance`
- For each receipt where `expectedDate != null`:
  - `dateExtracted == expectedDate`
- For all receipts:
  - `confidenceScores.amount >= 0.70` (where amount extracted)
  - `confidenceScores.date >= 0.70` (where date extracted)

---

### US-1.3: RDU Parking Receipt Specific Values

**Given**: The RDU parking receipt (`20251211_212334.jpg`)
**When**: Processing completes
**Then**: Extracted values match known values

**File**: `20251211_212334.jpg`
**Expected Values**:
```json
{
  "expectedAmount": 84.00,
  "expectedDate": "2025-12-11",
  "expectedVendor": "RDU Airport Parking",
  "amountTolerance": 0.01
}
```

**Assertions**:
```json
[
  { "field": "amount", "expected": 84.00, "tolerance": 0.01 },
  { "field": "date", "expected": "2025-12-11" },
  { "field": "vendor", "expected": "RDU Airport Parking" }
]
```

---

### US-1.4: PDF Receipt Processing

**Given**: A PDF receipt (`Receipt 2768.pdf`)
**When**: Uploaded and processed
**Then**: System handles PDF format and extracts data

**File**: `Receipt 2768.pdf`
**Expected Values**:
```json
{
  "expectedStatus": "Ready",
  "minConfidence": 0.70
}
```

**Assertions**:
- `status` in `["Ready", "Unmatched"]` (not "Error")
- Processing completes within 2 minutes
- PDF is converted and readable

---

## Error Case Handling

### Corrupted/Unreadable Images

**Scenario**: A receipt image is corrupted or unreadable
**Expected Behavior**:
- Status should be `Error`
- `errorMessage` should contain descriptive text

**Assertions**:
```json
{
  "name": "Error Status Check",
  "status": "passed",
  "assertions": [
    { "field": "status", "expected": "Error", "passed": true },
    { "field": "errorMessage", "expected": "not empty", "passed": true }
  ]
}
```

### Timeout Handling

**Scenario**: Receipt processing exceeds 2-minute timeout
**Expected Behavior**:
- Test marks receipt as failed
- Error includes timeout information

**Test Result**:
```json
{
  "name": "Receipt Upload - timeout.jpg",
  "status": "failed",
  "error": {
    "message": "Timeout waiting for receipt processing",
    "expected": "Ready or Unmatched within 2 minutes",
    "actual": "Still Processing after 120000ms"
  }
}
```

---

## Duplicate Receipt Detection

### US-1.5: Duplicate Receipt Upload Detection

**Given**: A receipt has already been uploaded
**When**: The same file is uploaded again
**Then**: System should detect and warn about potential duplicate

**Test Procedure**:
1. Upload a receipt file
2. Wait for processing to complete
3. Upload the same file again
4. Check response for duplicate warning

**Expected Response**:
- Upload may succeed (duplicates are allowed)
- Response should include duplicate detection hint if available
- Or: Receipt list should show matching original upload date

**Assertions**:
- If duplicate detection is implemented:
  - `response.warnings` contains duplicate message
- If not implemented:
  - Document as expected behavior (duplicates allowed)

---

## Success Criteria Validation

| Criterion | Target | Measurement |
|-----------|--------|-------------|
| SC-001 | All 19 receipts processed in 5 minutes | Sum of individual processing times < 300,000ms |
| SC-002 | ≥90% reach Ready/Unmatched | (Ready + Unmatched) / 19 >= 0.90 |

---

## Test Output Format

```json
{
  "scenario": "US-1",
  "name": "Receipt Upload and OCR Processing",
  "status": "passed",
  "durationMs": 145000,
  "results": [
    {
      "name": "Receipt Upload - 20251211_212334.jpg",
      "scenario": "US-1.3",
      "status": "passed",
      "durationMs": 12340,
      "assertions": [
        { "field": "status", "expected": "Ready", "actual": "Ready", "passed": true },
        { "field": "amount", "expected": 84.00, "actual": 84.00, "tolerance": 0.01, "passed": true },
        { "field": "date", "expected": "2025-12-11", "actual": "2025-12-11", "passed": true }
      ]
    }
  ],
  "summary": {
    "total": 19,
    "passed": 18,
    "failed": 1,
    "skipped": 0
  }
}
```
