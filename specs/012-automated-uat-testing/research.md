# Research: Automated UAT Testing for Claude Code

**Feature Branch**: `012-automated-uat-testing`
**Date**: 2025-12-19

## Overview

This document captures research findings for implementing automated UAT testing that Claude Code can execute against the ExpenseFlow staging API.

---

## Decision 1: Test Data Cleanup Strategy

**Decision**: Implement a dedicated `/api/test/cleanup` endpoint that removes test data by user context or tag prefix.

**Rationale**:
- Test isolation is critical for idempotent re-runs (SC-007)
- Cleanup by user context ensures multi-tenant safety—one test run can't affect another user's data
- Tag/prefix-based deletion allows targeted cleanup without affecting non-test data
- Aligns with ASP.NET Core integration testing patterns where test fixtures manage their own data lifecycle

**Alternatives Considered**:
1. **Timestamp-based isolation**: Each run creates data with unique timestamps, old data ignored
   - Rejected: Leads to data accumulation; doesn't clean up blob storage
2. **Manual cleanup required**: Tester manually clears data before re-running
   - Rejected: Violates idempotency requirement; error-prone for automated execution
3. **Database transaction rollback**: Wrap entire test in transaction
   - Rejected: Not feasible for multi-request workflows with background jobs

**Implementation Notes**:
- Endpoint should be protected by authorization (same as other API endpoints)
- Should delete: Receipts, Transactions, Matches, StatementImports for the user
- Should clean up blob storage for deleted receipts
- Should return count of deleted items for verification

---

## Decision 2: Expected Values File Format

**Decision**: Use JSON format for `test-data/expected-values.json` with structured schema for receipts, transactions, and matches.

**Rationale**:
- JSON is natively parseable by Claude Code without additional dependencies
- Structured schema enables precise assertions (amount, date, vendor, category)
- Separates test data from test logic—easy to update expected values without changing execution docs
- Supports both exact match and tolerance-based assertions (e.g., confidence > 70%)

**Alternatives Considered**:
1. **YAML format**: More human-readable
   - Rejected: Less native to .NET ecosystem; JSON sufficient for this use case
2. **Embedded in markdown**: Expected values in test documentation
   - Rejected: Harder to parse programmatically; couples data to docs
3. **CSV format**: Tabular data
   - Rejected: Doesn't support nested structures (e.g., match pairs, confidence thresholds)

**Schema Structure**:
```json
{
  "version": "1.0",
  "receipts": {
    "<filename>": {
      "expectedAmount": 84.00,
      "expectedDate": "2025-12-11",
      "expectedVendor": "RDU Airport Parking",
      "minConfidence": 0.70
    }
  },
  "transactions": {
    "rduParking": {
      "description": "RDUAA PUBLIC PARKING",
      "amount": -84.00,
      "date": "2025-12-11",
      "expectedCategory": "Travel",
      "expectedNormalizedVendor": "RDU Airport Parking"
    }
  },
  "expectedMatches": [
    {
      "receiptFile": "20251211_212334.jpg",
      "transactionDescription": "RDUAA PUBLIC PARKING",
      "minConfidence": 0.80
    }
  ]
}
```

---

## Decision 3: Test Execution Model

**Decision**: Claude Code executes tests via direct HTTP API calls, with test logic defined in documentation (quickstart.md).

**Rationale**:
- Claude Code has native HTTP capabilities—no need for separate test framework
- Documentation-driven approach allows natural language test descriptions
- JSON output format (FR-010) enables structured result capture
- Aligns with how Claude Code naturally operates—reading docs, making API calls, validating responses

**Alternatives Considered**:
1. **xUnit test project**: Traditional .NET integration tests
   - Rejected: Claude Code can't execute compiled tests; requires build step
2. **PowerShell script**: Scripted test execution
   - Rejected: Adds dependency; Claude Code prefers HTTP over shell for API testing
3. **Postman/Newman collection**: API test collection
   - Rejected: Requires Newman installation; less natural for Claude Code

**Execution Flow**:
1. Claude Code reads `quickstart.md` for test execution instructions
2. Calls cleanup endpoint to reset test state
3. Loads `expected-values.json` for assertion data
4. Executes test scenarios via HTTP API calls
5. Validates responses against expected values
6. Outputs structured JSON results

---

## Decision 4: Cleanup Endpoint Security

**Decision**: Restrict cleanup endpoint to staging/development environments only; require same authentication as other endpoints.

**Rationale**:
- Cleanup endpoint is inherently destructive—must not exist in production
- Environment-based restriction prevents accidental deployment
- Standard authentication ensures only authorized users can clean up their own data
- No special "test mode" flag needed—endpoint simply doesn't exist in production builds

**Implementation Approach**:
```csharp
#if DEBUG || STAGING
[Route("api/test")]
public class TestCleanupController : ApiControllerBase
{
    // Cleanup endpoint only compiled in non-production builds
}
#endif
```

Alternative: Use configuration-based feature flag (`TestEndpoints:Enabled`).

---

## Decision 5: Polling and Timeout Strategy

**Decision**: Poll receipt status at 5-second intervals with 2-minute timeout per receipt, 15-minute total test timeout.

**Rationale**:
- 5-second interval balances responsiveness with API load (720 requests/hour max per receipt)
- 2-minute per-receipt timeout matches spec (SC-001: 5 minutes for all 19 receipts ≈ 16 seconds avg, with buffer)
- 15-minute total aligns with SC-005 success criterion
- Polling is simpler and more reliable than WebSocket for Claude Code execution

**Status Transitions to Poll For**:
- `Uploaded` → `Processing` → `Ready` (success)
- `Uploaded` → `Processing` → `Unmatched` (success, no matching transaction)
- `Uploaded` → `Processing` → `Error` (failure, log message)

---

## Decision 6: Test Result Output Format

**Decision**: Structured JSON output with test name, status, duration, and error details.

**Rationale**:
- JSON enables programmatic analysis and CI/CD integration
- Structured format allows aggregation (pass/fail counts, total duration)
- Error details include expected vs actual values for debugging
- Claude Code can naturally generate JSON output

**Output Schema**:
```json
{
  "testRun": {
    "startTime": "2025-12-19T10:00:00Z",
    "endTime": "2025-12-19T10:12:34Z",
    "durationMs": 754000,
    "summary": {
      "total": 15,
      "passed": 14,
      "failed": 1,
      "skipped": 0
    }
  },
  "results": [
    {
      "name": "Receipt Upload - RDU Parking",
      "status": "passed",
      "durationMs": 2340,
      "assertions": [
        { "field": "status", "expected": "Ready", "actual": "Ready", "passed": true }
      ]
    }
  ]
}
```

---

## Test Data Analysis

### Receipts (19 files in test-data/receipts/)

| Filename | Known Details | Test Purpose |
|----------|---------------|--------------|
| `20251211_212334.jpg` | RDU parking, $84.00, Dec 11 | Primary match test case |
| `Receipt 2768.pdf` | PDF format | PDF processing validation |
| Other 17 images | Various vendors/amounts | Bulk upload and OCR validation |

### Transactions (486 rows in test-data/statements/chase.csv)

Key test transactions identified:
- **RDUAA PUBLIC PARKING**: $84.00 on 12/11/2025 (Travel) - Match target for RDU receipt
- **Amazon marketplace variants**: Multiple patterns for vendor normalization testing
- **Subscriptions**: SPOTIFY, CLAUDE.AI SUBSCRIPTION - For subscription detection testing

### Known Match Pairs

| Receipt | Transaction | Expected Confidence |
|---------|-------------|---------------------|
| `20251211_212334.jpg` | RDUAA PUBLIC PARKING ($84.00, 12/11) | >80% |

---

## Open Items Resolved

All NEEDS CLARIFICATION items from Technical Context have been resolved:
- ✅ Cleanup strategy: Dedicated endpoint
- ✅ Expected values format: JSON file
- ✅ Test execution model: Claude Code HTTP calls
- ✅ Security approach: Environment-restricted endpoint
- ✅ Polling strategy: 5-second intervals

---

## References

- [ASP.NET Core Integration Tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- ExpenseFlow API Controllers: `ReceiptsController.cs`, `StatementsController.cs`, `MatchingController.cs`
- Spec clarifications from `/speckit.clarify` session (2025-12-19)
