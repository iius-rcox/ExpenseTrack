# TC-006: Draft Report Generation

**Test Case ID**: TC-006
**Feature**: Draft Report Generation
**Sprint**: 8
**Priority**: Critical
**Status**: Not Started

## Description
Validate the draft expense report generation feature that creates structured reports from categorized expenses with line-item detail, GL coding, and approval-ready formatting.

## Preconditions
1. User is authenticated and logged into the application
2. Categorized transactions available for report generation
3. Matched receipts for at least some transactions
4. Reference data (GL codes, departments, cost centers) configured

## Test Data Required
- At least 20 categorized transactions
- At least 10 matched receipt-transaction pairs
- Mix of expense categories (travel, meals, supplies, etc.)
- At least one travel period (for per diem handling)

## Test Scenarios

### Scenario 1: Generate New Draft Report
**Steps**:
1. Navigate to Reports page
2. Click "Create New Report"
3. Select date range for report (e.g., last month)
4. Choose expenses to include (all or filtered)
5. Generate the draft report

**Expected Result**:
- Report generation completes successfully
- Draft report appears in report list
- Report status shows "Draft"
- All selected expenses included in report

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 2: Review Report Line Items
**Steps**:
1. Open a generated draft report
2. Review the list of expense line items
3. Verify each line shows: date, description, amount, category, GL code
4. Verify receipt attachment indicator for matched items

**Expected Result**:
- All expenses listed as line items
- Correct data displayed for each field
- Receipt indicator (attached/missing) accurate
- Line items sortable by date/amount/category

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 3: Edit Draft Report
**Steps**:
1. Open a draft report
2. Edit report header (title, purpose, project code)
3. Edit a line item (change category or GL code)
4. Add a note/comment to a line item
5. Save changes

**Expected Result**:
- Report header editable
- Line item fields editable
- Notes/comments saved
- All changes persisted correctly

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 4: Add/Remove Expenses from Report
**Steps**:
1. Open a draft report
2. Remove an expense line from the report
3. Add a previously unincluded expense
4. Verify report totals update

**Expected Result**:
- Expense removal successful
- Removed expense returns to "unreported" pool
- New expense added to report
- Report total recalculated correctly

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 5: Report Total Calculations
**Steps**:
1. Open a draft report with multiple expenses
2. Verify subtotals by category
3. Verify grand total
4. Compare manual calculation to reported total

**Expected Result**:
- Category subtotals accurate
- Grand total equals sum of all lines
- No rounding errors
- Currency formatting consistent

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 6: Submit Report for Approval
**Steps**:
1. Open a completed draft report
2. Verify all required fields completed
3. Click "Submit for Approval" or similar action
4. Confirm submission
5. Verify report status changes

**Expected Result**:
- Pre-submission validation runs
- Missing receipt warnings shown (if any)
- Report submitted successfully
- Status changes to "Submitted" or "Pending Approval"

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 7: Recall Submitted Report
**Steps**:
1. Find a submitted report (not yet approved)
2. Request to recall the report
3. Confirm recall action
4. Verify report returns to draft status

**Expected Result**:
- Recall option available for pending reports
- Recall confirmation required
- Report status returns to "Draft"
- Report editable again after recall

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 8: Report with Travel Period
**Steps**:
1. Generate report including travel period expenses
2. Verify travel period is displayed
3. Check per diem calculations in report
4. Verify all trip expenses grouped appropriately

**Expected Result**:
- Travel period section in report
- Trip details shown (dates, destination)
- Per diem vs actual meals displayed
- All trip expenses grouped together

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

## Summary

| Scenario | Result | Notes |
|----------|--------|-------|
| 1. Generate Draft | - | - |
| 2. Review Line Items | - | - |
| 3. Edit Draft | - | - |
| 4. Add/Remove Expenses | - | - |
| 5. Total Calculations | - | - |
| 6. Submit for Approval | - | - |
| 7. Recall Report | - | - |
| 8. Travel Period | - | - |

**Overall Status**: Not Started

**Executed By**: _[Name]_
**Execution Date**: _[Date]_

## Linked Defects
- _None yet_

## Notes
_[Any observations during testing]_
