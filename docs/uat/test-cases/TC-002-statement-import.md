# TC-002: Statement Import

**Test Case ID**: TC-002
**Feature**: Statement Import & Fingerprinting
**Sprint**: 4
**Priority**: Critical
**Status**: Not Started

## Description
Validate the complete statement import workflow including file parsing, column mapping inference, duplicate detection via fingerprinting, and transaction creation.

## Preconditions
1. User is authenticated and logged into the application
2. Staging environment is deployed and accessible
3. Cache warming completed (description cache populated)
4. No prior statement imports for test user

## Test Data Required
- Sample statement files:
  - `test-statement-chase.csv` (Chase credit card format)
  - `test-statement-bofa.xlsx` (Bank of America Excel format)
  - `test-statement-duplicate.csv` (duplicate of chase statement for fingerprint test)
  - `test-statement-invalid.csv` (malformed CSV)

## Test Scenarios

### Scenario 1: CSV Statement Import with Column Inference
**Steps**:
1. Navigate to Statement Import page
2. Click "Import Statement" button
3. Select `test-statement-chase.csv`
4. Review auto-detected column mappings
5. Confirm mappings and proceed with import
6. Wait for import to complete

**Expected Result**:
- File uploaded successfully
- Columns auto-detected: Date, Description, Amount
- Preview shows sample transactions
- Import completes with success count
- Transactions appear in transaction list

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 2: Excel Statement Import
**Steps**:
1. Import `test-statement-bofa.xlsx`
2. Verify Excel format is handled correctly
3. Review column mappings
4. Complete import

**Expected Result**:
- Excel file parsed correctly
- Multiple sheets handled (if applicable)
- Column mappings detected
- All transactions imported

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 3: Statement Fingerprint Duplicate Detection
**Steps**:
1. After importing `test-statement-chase.csv`, note the fingerprint
2. Attempt to import `test-statement-duplicate.csv` (same data)
3. Verify duplicate detection warning
4. Confirm or cancel import

**Expected Result**:
- System detects duplicate statement via fingerprint
- Warning message displayed to user
- User can choose to skip or force import
- If skipped, no duplicate transactions created

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 4: Column Mapping Override
**Steps**:
1. Upload a statement file
2. When column mapping UI appears, manually change mappings
3. Swap Date and Amount columns (incorrect)
4. Fix mapping and confirm correct columns
5. Complete import

**Expected Result**:
- Manual column override available
- Preview updates when mappings change
- Incorrect mappings show data validation warnings
- Correct mappings produce valid transactions

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 5: Statement Import Error Handling
**Steps**:
1. Attempt to import `test-statement-invalid.csv` (malformed)
2. Verify error handling behavior
3. Check error details for specific row issues

**Expected Result**:
- Appropriate error message displayed
- Partial import (valid rows) or full rejection
- Error log shows problematic rows
- User can view specific error details

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

## Summary

| Scenario | Result | Notes |
|----------|--------|-------|
| 1. CSV Import | - | - |
| 2. Excel Import | - | - |
| 3. Duplicate Detection | - | - |
| 4. Column Override | - | - |
| 5. Error Handling | - | - |

**Overall Status**: Not Started

**Executed By**: _[Name]_
**Execution Date**: _[Date]_

## Linked Defects
- _None yet_

## Notes
_[Any observations during testing]_
