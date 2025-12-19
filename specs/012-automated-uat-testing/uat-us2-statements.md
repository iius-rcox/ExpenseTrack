# UAT User Story 2: Statement Import and Transaction Fingerprinting

**User Story**: As a tester using Claude Code, I want to import the Chase bank statement and verify all transactions are fingerprinted correctly, so that I can confirm the statement processing pipeline works.

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
   - `test-data/statements/chase.csv` (484 transactions)
   - Expected values in `test-data/expected-values.json`

3. **Cleanup Executed** (for idempotent runs):
   ```http
   POST /api/test/cleanup
   Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
   Content-Type: application/json

   { "entityTypes": ["transactions", "imports"] }
   ```

---

## Test Procedure

### Step 1: Analyze Statement

Upload the statement for analysis to detect columns and format:

```http
POST /api/statements/analyze
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
Content-Type: multipart/form-data

file: chase.csv
```

**Expected Response** (200 OK):
```json
{
  "analysisId": "uuid",
  "filename": "chase.csv",
  "rowCount": 484,
  "headers": [
    "Transaction Date",
    "Post Date",
    "Description",
    "Category",
    "Type",
    "Amount",
    "Memo"
  ],
  "mappingOptions": {
    "dateColumn": { "suggested": "Transaction Date", "confidence": 0.95 },
    "amountColumn": { "suggested": "Amount", "confidence": 0.98 },
    "descriptionColumn": { "suggested": "Description", "confidence": 0.97 }
  },
  "previewRows": [...]
}
```

**Store**: `analysisId` for import step

---

### Step 2: Import Statement

Submit the import with column mapping:

```http
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

**Expected Response** (200 OK):
```json
{
  "importId": "uuid",
  "summary": {
    "total": 484,
    "imported": 484,
    "skipped": 0,
    "duplicates": 0
  },
  "fingerprinting": {
    "categorized": 484,
    "vendorsNormalized": 300
  }
}
```

---

### Step 3: Validate Import Results

#### 3.1 Transaction Count Validation

```json
{
  "field": "imported",
  "expected": 484,
  "actual": "{response.summary.imported}",
  "passed": true
}
```

#### 3.2 Skipped Count Validation

```json
{
  "field": "skipped",
  "expected": 0,
  "actual": "{response.summary.skipped}",
  "passed": true
}
```

---

### Step 4: Validate Specific Transactions

Retrieve transactions and validate fingerprinting for key test cases:

```http
GET /api/transactions?search=RDUAA
Authorization: Bearer {EXPENSEFLOW_USER_TOKEN}
```

For each expected transaction in `expected-values.json`:

#### 4.1 Category Validation

```json
{
  "field": "category",
  "expected": "Travel",
  "actual": "{transaction.category}",
  "passed": true
}
```

#### 4.2 Normalized Vendor Validation

```json
{
  "field": "normalizedVendor",
  "expected": "RDU Airport Parking",
  "actual": "{transaction.normalizedVendor}",
  "passed": true
}
```

---

## Column Mapping Expectations

### Chase CSV Format

| Column | Mapping | Purpose |
|--------|---------|---------|
| Transaction Date | `date` | Primary transaction date |
| Post Date | `postDate` | Bank posting date |
| Description | `description` | Transaction description for fingerprinting |
| Category | (ignored) | Chase's categorization (we use our own) |
| Type | (ignored) | Sale/Payment type |
| Amount | `amount` | Transaction amount (negative = charges) |
| Memo | (ignored) | Optional memo field |

**Date Format**: `MM/dd/yyyy` (e.g., `12/17/2025`)
**Amount Sign**: `negative_charges` - negative values are expenses

---

## Specific Test Cases

### US-2.1: Import Chase Statement

**Given**: The Chase statement CSV file with 484 transactions
**When**: Claude Code imports the statement via the API
**Then**: All 484 transactions should be created in the system

**Assertions**:
```json
[
  { "field": "imported", "expected": 484 },
  { "field": "skipped", "expected": 0 },
  { "field": "duplicates", "expected": 0 }
]
```

---

### US-2.2: Transaction Fingerprinting

**Given**: Imported transactions
**When**: Fingerprinting completes
**Then**: Each transaction should have normalized vendor and category

**Assertions**:
- `fingerprinting.categorized == summary.imported`
- `fingerprinting.vendorsNormalized > 0`

---

### US-2.3: RDU Transaction Categorization

**Given**: The RDUAA PUBLIC PARKING transaction ($84.00 on 12/11/2025)
**When**: Fingerprinting completes
**Then**: It should be categorized as "Travel" with normalized vendor

**Search**: `GET /api/transactions?search=RDUAA PUBLIC PARKING`

**Expected Values**:
```json
{
  "description": "RDUAA PUBLIC PARKING",
  "amount": -84.00,
  "date": "2025-12-11",
  "expectedCategory": "Travel",
  "expectedNormalizedVendor": "RDU Airport Parking"
}
```

**Assertions**:
```json
[
  { "field": "category", "expected": "Travel" },
  { "field": "normalizedVendor", "expected": "RDU Airport Parking" },
  { "field": "amount", "expected": -84.00 }
]
```

---

### US-2.4: Amazon Vendor Normalization

**Given**: Amazon marketplace transactions
**When**: Fingerprinting completes
**Then**: They should be normalized to "Amazon"

**Search**: `GET /api/transactions?search=Amazon`

**Test Transaction**:
```json
{
  "description": "Amazon.com*ON92D6EI3",
  "expectedNormalizedVendor": "Amazon"
}
```

**Assertions**:
- At least one transaction found with `normalizedVendor == "Amazon"`
- Original description pattern `Amazon.com*...` is normalized

---

### US-2.5: Duplicate Detection (Re-import)

**Given**: Statement has already been imported
**When**: The same statement is imported again
**Then**: System should detect duplicates

**Test Procedure**:
1. Import chase.csv (first time)
2. Store import summary
3. Import chase.csv (second time)
4. Verify duplicate detection

**Expected on Second Import**:
```json
{
  "summary": {
    "total": 484,
    "imported": 0,
    "skipped": 0,
    "duplicates": 484
  }
}
```

**Assertions**:
- `duplicates == 484` (all recognized as duplicates)
- OR `skipped == 484` (skipped due to duplicate fingerprints)
- `imported == 0` (no new transactions created)

---

## Malformed CSV Row Handling

### US-2.6: Malformed Row Handling

**Scenario**: CSV contains malformed rows
**Expected Behavior**: Skip bad rows, continue with valid ones

**Test Approach**:
Since our test file is clean, validate the behavior via the import response:

**Assertions**:
- If `skipped > 0`, response should include skip reasons
- System should not fail on minor formatting issues
- `skipReasons` array (if present) should have descriptive messages

**Expected Skip Reasons**:
```json
{
  "skipReasons": [
    { "row": 5, "reason": "Invalid date format" },
    { "row": 12, "reason": "Missing amount value" }
  ]
}
```

**Note**: For the clean chase.csv file, expect `skipped == 0`.

---

## Success Criteria Validation

| Criterion | Target | Measurement |
|-----------|--------|-------------|
| SC-003 | 484 transactions imported | `summary.imported == 484` |
| Transaction accuracy | 0 skipped | `summary.skipped == 0` |

---

## Test Output Format

```json
{
  "scenario": "US-2",
  "name": "Statement Import and Transaction Fingerprinting",
  "status": "passed",
  "durationMs": 8500,
  "results": [
    {
      "name": "Statement Analyze",
      "scenario": "US-2.1",
      "status": "passed",
      "durationMs": 1200,
      "assertions": [
        { "field": "rowCount", "expected": 484, "actual": 484, "passed": true },
        { "field": "headers.length", "expected": 7, "actual": 7, "passed": true }
      ]
    },
    {
      "name": "Statement Import",
      "scenario": "US-2.1",
      "status": "passed",
      "durationMs": 5200,
      "assertions": [
        { "field": "imported", "expected": 484, "actual": 484, "passed": true },
        { "field": "skipped", "expected": 0, "actual": 0, "passed": true }
      ]
    },
    {
      "name": "RDU Transaction Fingerprinting",
      "scenario": "US-2.3",
      "status": "passed",
      "durationMs": 800,
      "assertions": [
        { "field": "category", "expected": "Travel", "actual": "Travel", "passed": true },
        { "field": "normalizedVendor", "expected": "RDU Airport Parking", "actual": "RDU Airport Parking", "passed": true }
      ]
    },
    {
      "name": "Amazon Vendor Normalization",
      "scenario": "US-2.4",
      "status": "passed",
      "durationMs": 500,
      "assertions": [
        { "field": "normalizedVendor", "expected": "Amazon", "actual": "Amazon", "passed": true }
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
