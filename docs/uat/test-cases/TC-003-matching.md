# TC-003: Receipt-to-Transaction Matching

**Test Case ID**: TC-003
**Feature**: Matching Engine
**Sprint**: 5
**Priority**: Critical
**Status**: Not Started

## Description
Validate the matching engine that links uploaded receipts to imported bank/credit card transactions using amount matching, date proximity, and description similarity.

## Preconditions
1. User is authenticated and logged into the application
2. Statement imported (TC-002 complete or equivalent data)
3. Receipts uploaded (TC-001 complete or equivalent data)
4. At least one receipt and transaction pair that should match

## Test Data Required
- Pre-imported transactions from TC-002
- Pre-uploaded receipts from TC-001
- Matching pairs:
  - Receipt for $45.67 from "Safeway" + Transaction $45.67 "SAFEWAY #1234"
  - Receipt for $128.50 from "Marriott" + Transaction $128.50 "MARRIOTT DOWNTOWN"
- Non-matching items for false positive testing

## Test Scenarios

### Scenario 1: Automatic Match by Amount and Date
**Steps**:
1. Navigate to Matching page or Unmatched Receipts view
2. Find a receipt with amount $45.67 from Safeway
3. Verify system suggests matching transaction
4. Accept the suggested match
5. Verify match is recorded

**Expected Result**:
- System suggests correct transaction as match
- Match confidence score displayed
- After acceptance, receipt shows "Matched" status
- Transaction shows linked receipt

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 2: Manual Match Selection
**Steps**:
1. Find a receipt without automatic match suggestion
2. Click "Find Match" or "Search Transactions"
3. Browse/search available transactions
4. Manually select a transaction to match
5. Confirm the match

**Expected Result**:
- Search/filter options for transactions available
- Can filter by date range, amount range
- Manual match successfully recorded
- Both receipt and transaction show linked status

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 3: Fuzzy Description Matching
**Steps**:
1. Upload receipt from "MARRIOTT HOTEL"
2. Have transaction with description "MARRIOTT DOWNTOWN HTL"
3. Trigger matching process
4. Verify fuzzy matching identifies the pair

**Expected Result**:
- System recognizes similar vendor names
- Match suggested despite description differences
- Levenshtein distance or similar algorithm applied
- Confidence score reflects partial match

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 4: Unmatch Previously Matched Items
**Steps**:
1. Find a matched receipt-transaction pair
2. Click "Unmatch" or similar action
3. Confirm the unmatch action
4. Verify both items return to unmatched state

**Expected Result**:
- Unmatch action available for matched pairs
- Confirmation prompt before unmatching
- After unmatch, receipt shows "Unmatched"
- Transaction available for matching again

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 5: Amount Mismatch Handling
**Steps**:
1. Attempt to match receipt ($100.00) with transaction ($99.50)
2. Verify system warns about amount difference
3. Choose to proceed with match anyway
4. Verify match is recorded with discrepancy note

**Expected Result**:
- Warning displayed for amount mismatch
- User can still proceed if within tolerance
- Match recorded with variance flag/note
- Report will show matched item with variance

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 6: Date Range Matching
**Steps**:
1. Upload receipt dated 12/15/2024
2. Have transaction dated 12/17/2024 (2 days later for hotel checkout)
3. Verify system considers date proximity in matching
4. Accept match for transaction within date range

**Expected Result**:
- System considers transactions within date window (e.g., +/- 3 days)
- Hotel/travel purchases allow wider date range
- Match suggestion includes date proximity score
- Match allowed for reasonable date differences

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

## Summary

| Scenario | Result | Notes |
|----------|--------|-------|
| 1. Auto Match | - | - |
| 2. Manual Match | - | - |
| 3. Fuzzy Matching | - | - |
| 4. Unmatch | - | - |
| 5. Amount Mismatch | - | - |
| 6. Date Range | - | - |

**Overall Status**: Not Started

**Executed By**: _[Name]_
**Execution Date**: _[Date]_

## Linked Defects
- _None yet_

## Notes
_[Any observations during testing]_
