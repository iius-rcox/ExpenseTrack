# TC-007: Month-over-Month Comparison

**Test Case ID**: TC-007
**Feature**: Output Generation & Analytics (Month-over-Month Comparison)
**Sprint**: 9
**Priority**: High
**Status**: Not Started

## Description
Validate the month-over-month comparison feature that analyzes spending patterns across time periods, generates variance reports, and exports data in Excel/PDF formats.

## Preconditions
1. User is authenticated and logged into the application
2. Expense data available for at least 2 months
3. Expenses categorized with GL codes and departments
4. Cache statistics available for cost analysis

## Test Data Required
- Categorized expenses for Month 1 (e.g., November 2024)
- Categorized expenses for Month 2 (e.g., December 2024)
- Expenses across multiple categories for comparison
- At least one category with significant variance

## Test Scenarios

### Scenario 1: Generate Month-over-Month Comparison
**Steps**:
1. Navigate to Analytics or Reports page
2. Select "Month-over-Month Comparison" report type
3. Select comparison months (November vs December 2024)
4. Generate the comparison report

**Expected Result**:
- Comparison report generated successfully
- Both months' totals displayed
- Variance calculated ($ and %)
- Categories listed with per-category comparison

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 2: Category-Level Variance Analysis
**Steps**:
1. View the MoM comparison report
2. Drill down into a specific category
3. View line-item detail for each month
4. Verify variance explanation available

**Expected Result**:
- Category breakdown accessible
- Line items shown for each month
- Variance by category accurate
- Ability to identify what changed

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 3: Export to Excel
**Steps**:
1. Generate a MoM comparison report
2. Click "Export to Excel" button
3. Download the Excel file
4. Open and verify contents

**Expected Result**:
- Excel file downloads successfully
- File opens without errors
- Data matches on-screen report
- Proper formatting (headers, totals, formulas)

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 4: Export to PDF
**Steps**:
1. Generate a MoM comparison report
2. Click "Export to PDF" button
3. Download the PDF file
4. Open and verify contents

**Expected Result**:
- PDF file downloads successfully
- File opens without errors
- Data matches on-screen report
- Proper formatting for printing

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 5: Trend Visualization
**Steps**:
1. View MoM comparison with chart/graph option
2. Verify visual representation of spending trends
3. Check chart accuracy against data
4. Interact with chart (hover for details if available)

**Expected Result**:
- Chart/graph renders correctly
- Visual accurately represents data
- Comparison clearly visible
- Interactive elements work (if applicable)

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 6: Multi-Month Trend Analysis
**Steps**:
1. If available, select more than 2 months for comparison
2. Generate multi-month trend report
3. Verify all months included
4. Check trend line or pattern visualization

**Expected Result**:
- Multiple months selectable
- All months included in analysis
- Trend over time visible
- Pattern insights generated

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 7: Cache Cost Savings Report
**Steps**:
1. Navigate to Cache Statistics
2. View cost savings analysis
3. Review Tier 1/2/3 usage breakdown
4. Verify estimated cost savings calculation

**Expected Result**:
- Cache hit rate displayed
- Tier breakdown accurate
- Cost savings estimate shown
- Comparison to no-cache scenario

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 8: Filter by Department/GL Code
**Steps**:
1. Generate MoM comparison
2. Apply filter by department
3. Verify report shows only selected department
4. Apply filter by GL code and verify

**Expected Result**:
- Department filter works correctly
- GL code filter works correctly
- Filtered results accurate
- Totals recalculate for filter

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

## Summary

| Scenario | Result | Notes |
|----------|--------|-------|
| 1. Generate MoM | - | - |
| 2. Category Variance | - | - |
| 3. Export Excel | - | - |
| 4. Export PDF | - | - |
| 5. Trend Visualization | - | - |
| 6. Multi-Month Trend | - | - |
| 7. Cache Cost Savings | - | - |
| 8. Filter by Dept/GL | - | - |

**Overall Status**: Not Started

**Executed By**: _[Name]_
**Execution Date**: _[Date]_

## Linked Defects
- _None yet_

## Notes
_[Any observations during testing]_
