# TC-005: Travel Period Detection

**Test Case ID**: TC-005
**Feature**: Travel Period Detection
**Sprint**: 7
**Priority**: Medium
**Status**: Not Started

## Description
Validate the travel period detection feature that identifies clusters of travel-related expenses (flights, hotels, car rentals) and groups them into coherent travel periods with location inference.

## Preconditions
1. User is authenticated and logged into the application
2. Imported transactions include travel-related expenses
3. Transactions span at least one trip (flight + hotel + other travel expenses)
4. Categorization complete for travel transactions

## Test Data Required
- Transaction set representing a business trip:
  - Flight booking (e.g., "UNITED AIRLINES")
  - Hotel stay (e.g., "MARRIOTT CHICAGO")
  - Car rental (e.g., "HERTZ RENTAL CAR")
  - Meals/expenses during trip dates
- Transaction set with non-travel expenses for contrast

## Test Scenarios

### Scenario 1: Automatic Travel Period Detection
**Steps**:
1. Navigate to Travel Periods or Expense Analysis page
2. Trigger travel detection analysis (if manual) or view detected periods
3. Verify system identifies a travel period from test transactions
4. Review detected period dates and location

**Expected Result**:
- Travel period detected automatically
- Start date matches earliest travel expense (flight/check-in)
- End date matches latest travel expense (return flight/checkout)
- Location inferred from hotel or destination

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 2: Expense Assignment to Travel Period
**Steps**:
1. View a detected travel period
2. Check which expenses are assigned to the period
3. Verify hotel, flights, car rental are included
4. Verify meals/expenses within date range are included

**Expected Result**:
- All travel-related expenses assigned
- Meals during travel dates included
- Expenses before/after travel period excluded
- Total travel cost calculated correctly

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 3: Manual Travel Period Adjustment
**Steps**:
1. Find a detected travel period
2. Edit the travel period dates
3. Extend end date by one day
4. Save changes and verify expense assignment updates

**Expected Result**:
- Travel period dates editable
- Date changes saved successfully
- Expense assignment recalculated
- Newly included expenses appear in period

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 4: Per Diem Calculation
**Steps**:
1. View a travel period with meals
2. Check per diem or daily allowance calculation
3. Verify calculation based on travel dates and location
4. Compare actual meal expenses to per diem allowance

**Expected Result**:
- Per diem calculated based on location/dates
- Actual meal expenses totaled
- Variance (over/under per diem) shown
- Per diem rates configurable per policy

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 5: Overlapping Trip Detection
**Steps**:
1. Have transaction data with two trips close in time
2. Verify system detects them as separate travel periods
3. Check that expenses are assigned to correct periods
4. Verify no overlap between period assignments

**Expected Result**:
- Two distinct travel periods detected
- Each period has correct date range
- Expenses assigned to appropriate period
- No duplicate expense assignments

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 6: Create Manual Travel Period
**Steps**:
1. Navigate to Travel Periods page
2. Click "Create Travel Period"
3. Enter dates, destination, purpose
4. Save the new travel period
5. Manually assign expenses to the period

**Expected Result**:
- Manual creation form available
- Required fields validated
- Travel period created successfully
- Manual expense assignment works

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

## Summary

| Scenario | Result | Notes |
|----------|--------|-------|
| 1. Auto Detection | - | - |
| 2. Expense Assignment | - | - |
| 3. Manual Adjustment | - | - |
| 4. Per Diem Calc | - | - |
| 5. Overlapping Trips | - | - |
| 6. Manual Creation | - | - |

**Overall Status**: Not Started

**Executed By**: _[Name]_
**Execution Date**: _[Date]_

## Linked Defects
- _None yet_

## Notes
_[Any observations during testing]_
