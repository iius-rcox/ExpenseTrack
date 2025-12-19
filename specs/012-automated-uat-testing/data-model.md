# Data Model: Automated UAT Testing

**Feature Branch**: `012-automated-uat-testing`
**Date**: 2025-12-19

## Overview

This document defines the data structures for UAT testing: the expected values file schema, test result output schema, and cleanup request/response contracts.

---

## Expected Values File Schema

**File**: `test-data/expected-values.json`

This file contains golden data that Claude Code uses to validate API responses.

### Root Structure

```json
{
  "$schema": "./expected-values.schema.json",
  "version": "1.0",
  "metadata": {
    "createdAt": "2025-12-19",
    "description": "Expected values for ExpenseFlow UAT testing"
  },
  "receipts": { /* Receipt expectations */ },
  "transactions": { /* Transaction expectations */ },
  "expectedMatches": [ /* Match pair expectations */ ],
  "statementImport": { /* Statement import expectations */ }
}
```

### Receipt Expectations

```typescript
interface ReceiptExpectation {
  // Filename as key (e.g., "20251211_212334.jpg")
  [filename: string]: {
    // Expected extracted values (null if unknown/varies)
    expectedAmount: number | null;
    expectedDate: string | null;  // ISO date format: "YYYY-MM-DD"
    expectedVendor: string | null;
    expectedCurrency: string;     // Default: "USD"

    // Validation thresholds
    minConfidence: number;        // 0.0-1.0, default 0.70

    // Expected terminal status
    expectedStatus: "Ready" | "Unmatched" | "Error";

    // For amount comparison tolerance
    amountTolerance: number;      // Default: 0.01
  };
}
```

### Transaction Expectations

```typescript
interface TransactionExpectation {
  // Logical name as key (e.g., "rduParking", "spotifySubscription")
  [name: string]: {
    // Matching criteria (from CSV)
    description: string;          // Original description to find
    amount: number;               // Negative for charges
    date: string;                 // ISO date format

    // Expected processing results
    expectedCategory: string;
    expectedNormalizedVendor: string;
  };
}
```

### Expected Matches

```typescript
interface ExpectedMatch {
  receiptFile: string;           // Filename from test-data/receipts/
  transactionDescription: string; // Description pattern to match
  minConfidence: number;          // Minimum acceptable confidence (0.0-1.0)
}
```

### Statement Import Expectations

```typescript
interface StatementImportExpectation {
  filename: string;               // "chase.csv"
  expectedRowCount: number;       // 486
  expectedSkipped: number;        // Expected skipped rows (date validation failures)
  expectedFormat: {
    dateColumn: string;           // "Transaction Date"
    amountColumn: string;         // "Amount"
    descriptionColumn: string;    // "Description"
  };
}
```

---

## Test Result Output Schema

**Output**: Structured JSON written to stdout or file

### Test Run Summary

```typescript
interface TestRun {
  testRun: {
    id: string;                   // UUID for this test run
    startTime: string;            // ISO datetime
    endTime: string;              // ISO datetime
    durationMs: number;
    environment: string;          // "staging"
    summary: TestSummary;
  };
  results: TestResult[];
}

interface TestSummary {
  total: number;
  passed: number;
  failed: number;
  skipped: number;                // Tests skipped due to dependency failure
}
```

### Individual Test Result

```typescript
interface TestResult {
  name: string;                   // Human-readable test name
  scenario: string;               // User story reference (e.g., "US-1.1")
  status: "passed" | "failed" | "skipped";
  durationMs: number;

  // For passed/failed tests
  assertions?: Assertion[];

  // For failed tests
  error?: {
    message: string;
    expected: any;
    actual: any;
  };

  // For skipped tests
  skipReason?: string;            // e.g., "Dependency US-1.1 failed"
}

interface Assertion {
  field: string;                  // Field being validated
  expected: any;
  actual: any;
  passed: boolean;
  tolerance?: number;             // For numeric comparisons
}
```

---

## Cleanup Endpoint Data Model

### CleanupRequest

```typescript
interface CleanupRequest {
  // Optional: specific entity types to clean
  // If omitted, cleans all test data for the user
  entityTypes?: ("receipts" | "transactions" | "matches" | "imports")[];

  // Optional: only delete items created after this timestamp
  createdAfter?: string;          // ISO datetime
}
```

### CleanupResponse

```typescript
interface CleanupResponse {
  success: boolean;
  deletedCounts: {
    receipts: number;
    transactions: number;
    matches: number;
    imports: number;
    blobsDeleted: number;         // Storage cleanup count
  };
  durationMs: number;
  warnings?: string[];            // Non-fatal issues during cleanup
}
```

---

## Entity Relationships

```
┌─────────────────────────────────────────────────────────────────┐
│                    TEST DATA FLOW                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  test-data/                                                     │
│  ├── receipts/*.jpg,pdf  ──────┐                               │
│  ├── statements/chase.csv ─────┼──► API Upload ──► Database    │
│  └── expected-values.json ─────┘        │              │        │
│           │                             │              │        │
│           │                             ▼              ▼        │
│           │                      ┌─────────────────────────┐   │
│           │                      │   ExpenseFlow Staging   │   │
│           │                      │   - Receipts table      │   │
│           │                      │   - Transactions table  │   │
│           │                      │   - Matches table       │   │
│           │                      │   - Blob storage        │   │
│           │                      └─────────────────────────┘   │
│           │                                    │                │
│           │                                    ▼                │
│           └──────────────────────► Validation ◄────────────────┤
│                                         │                       │
│                                         ▼                       │
│                               Test Results (JSON)               │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Sample Expected Values File

```json
{
  "$schema": "./expected-values.schema.json",
  "version": "1.0",
  "metadata": {
    "createdAt": "2025-12-19",
    "description": "Expected values for ExpenseFlow UAT testing with 19 receipts and Chase statement"
  },
  "receipts": {
    "20251211_212334.jpg": {
      "expectedAmount": 84.00,
      "expectedDate": "2025-12-11",
      "expectedVendor": "RDU Airport Parking",
      "expectedCurrency": "USD",
      "minConfidence": 0.70,
      "expectedStatus": "Ready",
      "amountTolerance": 0.01
    },
    "Receipt 2768.pdf": {
      "expectedAmount": null,
      "expectedDate": null,
      "expectedVendor": null,
      "expectedCurrency": "USD",
      "minConfidence": 0.70,
      "expectedStatus": "Ready",
      "amountTolerance": 0.01
    }
  },
  "transactions": {
    "rduParking": {
      "description": "RDUAA PUBLIC PARKING",
      "amount": -84.00,
      "date": "2025-12-11",
      "expectedCategory": "Travel",
      "expectedNormalizedVendor": "RDU Airport Parking"
    },
    "spotifySubscription": {
      "description": "SPOTIFY",
      "amount": -12.86,
      "date": "2025-12-15",
      "expectedCategory": "Bills & Utilities",
      "expectedNormalizedVendor": "Spotify"
    }
  },
  "expectedMatches": [
    {
      "receiptFile": "20251211_212334.jpg",
      "transactionDescription": "RDUAA PUBLIC PARKING",
      "minConfidence": 0.80
    }
  ],
  "statementImport": {
    "filename": "chase.csv",
    "expectedRowCount": 486,
    "expectedSkipped": 0,
    "expectedFormat": {
      "dateColumn": "Transaction Date",
      "amountColumn": "Amount",
      "descriptionColumn": "Description"
    }
  }
}
```

---

## Validation Rules

### Receipt Validation

| Field | Rule | Error if Violated |
|-------|------|-------------------|
| status | Must reach terminal status within 2 minutes | Timeout: Receipt {id} did not complete processing |
| amount | Within `amountTolerance` of expected | Amount mismatch: expected {exp}, got {act} |
| confidence | >= `minConfidence` for each field | Low confidence: {field} at {conf}, minimum {min} |

### Transaction Validation

| Field | Rule | Error if Violated |
|-------|------|-------------------|
| count | All 486 rows imported | Import count mismatch: expected 486, got {count} |
| category | Matches expected category | Category mismatch for {desc}: expected {exp}, got {act} |
| vendor | Normalized vendor matches | Vendor normalization failed for {desc} |

### Match Validation

| Field | Rule | Error if Violated |
|-------|------|-------------------|
| exists | Match proposal created | No match proposed for {receipt} ↔ {transaction} |
| confidence | >= `minConfidence` | Match confidence too low: {conf} < {min} |
