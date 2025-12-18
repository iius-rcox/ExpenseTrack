# ExpenseFlow UAT Testing Guide

**Version:** 1.0
**Date:** 2025-12-18
**Environment:** Staging (https://staging.expense.ii-us.com)

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Test Environment Setup](#test-environment-setup)
3. [Authentication Tests](#authentication-tests)
4. [Receipt Pipeline Tests](#receipt-pipeline-tests)
5. [Statement Import Tests](#statement-import-tests)
6. [Transaction Management Tests](#transaction-management-tests)
7. [Matching Engine Tests](#matching-engine-tests)
8. [AI Categorization Tests](#ai-categorization-tests)
9. [Subscription Detection Tests](#subscription-detection-tests)
10. [Travel Detection Tests](#travel-detection-tests)
11. [Expense Splitting Tests](#expense-splitting-tests)
12. [Report Generation Tests](#report-generation-tests)
13. [Export Tests](#export-tests)
14. [Performance Tests](#performance-tests)
15. [Test Sign-Off](#test-sign-off)

---

## Prerequisites

### Required Access
- [ ] Azure AD account with access to ExpenseFlow
- [ ] Test user credentials (contact admin if needed)
- [ ] Access to staging environment: https://staging.expense.ii-us.com

### Test Data Requirements
- [ ] Sample receipt images (JPG, PNG, PDF) - at least 5 different receipts
- [ ] Sample bank statement CSV file (Chase, Bank of America, or similar)
- [ ] Sample credit card statement Excel file (American Express format)
- [ ] Sample receipts with various amounts ($10-$5000 range)

### Tools Needed
- Modern web browser (Chrome, Edge, or Firefox)
- API testing tool (Postman, curl, or browser dev tools)
- Excel or Google Sheets (for export verification)
- PDF reader (for export verification)

---

## Test Environment Setup

### Step 1: Verify Environment Health

```bash
# Check API health
curl https://staging.expense.ii-us.com/health
```

**Expected Response:**
```json
{"status":"healthy","timestamp":"...","version":"1.0.0.0"}
```

### Step 2: Access the Application

1. Navigate to https://staging.expense.ii-us.com
2. You should see the ExpenseFlow login page or main application
3. Verify the page loads without errors (check browser console)

### Step 3: Authenticate

1. Click "Sign In" or navigate to login
2. Use your Azure AD credentials
3. Complete MFA if prompted
4. Verify redirect back to application

---

## Authentication Tests

### TC-AUTH-001: Azure AD Login

| Field | Value |
|-------|-------|
| **Priority** | Critical |
| **Precondition** | User has valid Azure AD account |

**Steps:**
1. Navigate to https://staging.expense.ii-us.com
2. Click "Sign In" button
3. Enter Azure AD email
4. Enter password
5. Complete MFA if required

**Expected Result:**
- User is redirected to main application
- User name/email displayed in header
- No authentication errors in console

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-AUTH-002: Token Refresh

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | User is logged in |

**Steps:**
1. Log in to application
2. Wait 30+ minutes (or manually expire token in dev tools)
3. Perform any API action (load transactions, etc.)

**Expected Result:**
- Token refreshes automatically
- No re-login required
- Action completes successfully

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-AUTH-003: Logout

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | User is logged in |

**Steps:**
1. Click user menu/profile
2. Click "Sign Out" or "Logout"
3. Attempt to access protected page

**Expected Result:**
- User is logged out
- Redirected to login page
- Cannot access protected resources

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

## Receipt Pipeline Tests

### TC-REC-001: Upload Single Receipt (Image)

| Field | Value |
|-------|-------|
| **Priority** | Critical |
| **Precondition** | User is logged in |

**Steps:**
1. Navigate to Receipt Upload page
2. Click "Upload Receipt" or drag-and-drop area
3. Select a JPG or PNG receipt image
4. Wait for upload and processing

**Expected Result:**
- Upload progress indicator shown
- Receipt appears in list within 30 seconds
- OCR extracts: vendor name, date, total amount
- Thumbnail preview available

**Pass:** [ ] **Fail:** [ ]

**Extracted Data:**
| Field | Expected | Actual |
|-------|----------|--------|
| Vendor | | |
| Date | | |
| Amount | | |

**Notes:**
```
_________________________________________________
```

---

### TC-REC-002: Upload Receipt (PDF)

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | User is logged in |

**Steps:**
1. Navigate to Receipt Upload page
2. Upload a PDF receipt (1-3 pages)
3. Wait for processing

**Expected Result:**
- PDF converted and processed
- Data extracted from first page
- Processing completes within 60 seconds

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-REC-003: Upload Multiple Receipts (Batch)

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | User is logged in |

**Steps:**
1. Navigate to Receipt Upload page
2. Select 5 receipt images at once
3. Monitor upload progress

**Expected Result:**
- All 5 receipts upload successfully
- Progress shown for each
- All appear in receipt list
- No timeouts or failures

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-REC-004: Receipt with Poor Image Quality

| Field | Value |
|-------|-------|
| **Priority** | Medium |
| **Precondition** | User is logged in, have blurry receipt image |

**Steps:**
1. Upload a blurry or low-contrast receipt image
2. Check extraction results

**Expected Result:**
- System attempts extraction
- Low confidence scores displayed
- User can manually correct fields
- No system crash

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-REC-005: View Receipt Details

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | At least one receipt uploaded |

**Steps:**
1. Navigate to Receipts list
2. Click on a receipt to view details
3. Verify all extracted data displayed

**Expected Result:**
- Full receipt image viewable
- All extracted fields shown
- Edit capability available
- Matching status visible

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

## Statement Import Tests

### TC-STMT-001: Import CSV Statement (Chase Format)

| Field | Value |
|-------|-------|
| **Priority** | Critical |
| **Precondition** | User is logged in, have Chase CSV statement |

**Steps:**
1. Navigate to Statement Import page
2. Click "Upload Statement"
3. Select Chase CSV file
4. Review auto-detected column mapping
5. Confirm mapping and import

**Expected Result:**
- File uploads successfully
- Columns auto-mapped correctly:
  - Transaction Date
  - Description
  - Amount
- Preview shows sample rows
- Import completes with count displayed

**Pass:** [ ] **Fail:** [ ]

**Import Summary:**
| Metric | Value |
|--------|-------|
| Rows Imported | |
| Rows Skipped | |
| Duplicates | |
| Tier Used | |

**Notes:**
```
_________________________________________________
```

---

### TC-STMT-002: Import Excel Statement

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | User is logged in, have Excel statement |

**Steps:**
1. Navigate to Statement Import
2. Upload .xlsx file
3. Select correct sheet if multiple
4. Map columns manually if needed
5. Complete import

**Expected Result:**
- Excel file parsed correctly
- Sheet selection works
- All columns mappable
- Transactions imported

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-STMT-003: Column Mapping - Manual Override

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | Upload a statement with non-standard columns |

**Steps:**
1. Upload statement with unusual column names
2. System shows mapping options
3. Manually change column mappings
4. Verify sample data updates
5. Complete import

**Expected Result:**
- All columns can be remapped
- Sample preview updates on change
- Import uses manual mapping
- Mapping can be saved for future

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-STMT-004: Fingerprint Recognition (Tier 1)

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | Previously imported a statement from same source |

**Steps:**
1. Import a statement from same bank
2. Check if fingerprint is recognized
3. Verify auto-mapping applied

**Expected Result:**
- "Saved Mapping Found" indicator shown
- Mapping automatically applied
- Import shows "Tier 1" used
- Faster processing than first import

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-STMT-005: Duplicate Transaction Detection

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | Import same statement twice |

**Steps:**
1. Import a statement
2. Import the same statement again
3. Check import summary

**Expected Result:**
- Duplicates detected
- Duplicate count shown in summary
- Original transactions not duplicated
- Only new transactions added

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

## Transaction Management Tests

### TC-TXN-001: View Transaction List

| Field | Value |
|-------|-------|
| **Priority** | Critical |
| **Precondition** | Transactions exist in system |

**Steps:**
1. Navigate to Transactions page
2. Verify list loads
3. Check pagination if > 50 transactions

**Expected Result:**
- Transactions displayed in table
- Columns: Date, Description, Amount, Category, Status
- Sorting works on columns
- Pagination functions correctly

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-TXN-002: Filter Transactions by Date

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | Transactions span multiple dates |

**Steps:**
1. Open date filter
2. Select date range (e.g., last 30 days)
3. Apply filter

**Expected Result:**
- Only transactions in date range shown
- Filter indicator visible
- Count updates
- Clear filter restores all

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-TXN-003: Search Transactions

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | Transactions with various descriptions |

**Steps:**
1. Enter search term (e.g., "Amazon")
2. Press Enter or click Search
3. Review results

**Expected Result:**
- Results filter in real-time or on submit
- Matching descriptions highlighted
- Case-insensitive search
- Partial match works

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-TXN-004: Edit Transaction Category

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | Transaction exists |

**Steps:**
1. Click on a transaction
2. Change category dropdown
3. Save changes

**Expected Result:**
- Category options available
- Change saves successfully
- Category persists on refresh
- Audit trail updated (if visible)

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

## Matching Engine Tests

### TC-MATCH-001: Auto-Match Receipt to Transaction

| Field | Value |
|-------|-------|
| **Priority** | Critical |
| **Precondition** | Receipt and matching transaction exist (same amount, close date) |

**Steps:**
1. Upload receipt with clear amount and date
2. Ensure matching transaction exists
3. Wait for matching engine to run (or trigger manually)
4. Check match status

**Expected Result:**
- Match detected automatically
- Match confidence score shown
- Receipt linked to transaction
- Both show "Matched" status

**Pass:** [ ] **Fail:** [ ]

**Match Details:**
| Field | Receipt | Transaction | Match? |
|-------|---------|-------------|--------|
| Amount | | | |
| Date | | | |
| Vendor | | | |
| Confidence | | N/A | |

**Notes:**
```
_________________________________________________
```

---

### TC-MATCH-002: Manual Match Receipt

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | Unmatched receipt exists |

**Steps:**
1. Navigate to unmatched receipt
2. Click "Match to Transaction"
3. Search/select target transaction
4. Confirm match

**Expected Result:**
- Transaction search works
- Match can be created manually
- Both items update to "Matched"
- Manual match indicator shown

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-MATCH-003: Unmatch Receipt

| Field | Value |
|-------|-------|
| **Priority** | Medium |
| **Precondition** | Matched receipt exists |

**Steps:**
1. Navigate to matched receipt
2. Click "Unmatch" or "Remove Match"
3. Confirm action

**Expected Result:**
- Match removed
- Receipt returns to "Unmatched"
- Transaction returns to "Unmatched"
- Available for re-matching

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-MATCH-004: View Matching Statistics

| Field | Value |
|-------|-------|
| **Priority** | Medium |
| **Precondition** | Mix of matched/unmatched items |

**Steps:**
1. Navigate to Matching Stats or Dashboard
2. Review statistics

**Expected Result:**
- Total receipts count
- Matched count/percentage
- Unmatched count
- Average confidence score

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

## AI Categorization Tests

### TC-CAT-001: Auto-Categorization on Import

| Field | Value |
|-------|-------|
| **Priority** | Critical |
| **Precondition** | Import transactions with clear descriptions |

**Steps:**
1. Import statement with transactions like:
   - "UBER TRIP" (Travel)
   - "STARBUCKS" (Meals)
   - "AMAZON WEB SERVICES" (Software)
2. Check assigned categories

**Expected Result:**
- Categories auto-assigned
- Confidence scores shown
- Reasonable category matches
- No blank categories

**Pass:** [ ] **Fail:** [ ]

**Categorization Results:**
| Description | Expected Category | Actual Category | Confidence |
|-------------|-------------------|-----------------|------------|
| UBER TRIP | Travel | | |
| STARBUCKS | Meals | | |
| AWS | Software | | |

**Notes:**
```
_________________________________________________
```

---

### TC-CAT-002: Category Override and Learning

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | Transaction with incorrect category |

**Steps:**
1. Find transaction with wrong category
2. Change to correct category
3. Import similar transaction later
4. Check if system learned

**Expected Result:**
- Category change saves
- Future similar transactions use learned category
- Confidence improves over time

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-CAT-003: Bulk Categorization

| Field | Value |
|-------|-------|
| **Priority** | Medium |
| **Precondition** | Multiple uncategorized transactions |

**Steps:**
1. Select multiple transactions
2. Choose "Bulk Categorize" action
3. Select category
4. Apply

**Expected Result:**
- All selected transactions updated
- Single operation for all
- Success confirmation shown

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

## Subscription Detection Tests

### TC-SUB-001: Detect Recurring Subscription

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | 3+ monthly transactions from same vendor (e.g., Netflix) |

**Steps:**
1. Import statements with recurring charges:
   - Netflix $15.99 (Jan, Feb, Mar)
2. Navigate to Subscriptions page
3. Check detection

**Expected Result:**
- Subscription detected automatically
- Vendor name identified
- Monthly amount shown
- Billing cycle detected (monthly)

**Pass:** [ ] **Fail:** [ ]

**Detected Subscriptions:**
| Vendor | Amount | Frequency | Status |
|--------|--------|-----------|--------|
| | | | |
| | | | |

**Notes:**
```
_________________________________________________
```

---

### TC-SUB-002: Subscription Alert

| Field | Value |
|-------|-------|
| **Priority** | Medium |
| **Precondition** | Active subscription near renewal |

**Steps:**
1. Check for subscription alerts
2. Verify upcoming renewal notifications

**Expected Result:**
- Alert shown for upcoming renewals
- Date and amount displayed
- Dismiss/acknowledge option

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-SUB-003: Mark Subscription Inactive

| Field | Value |
|-------|-------|
| **Priority** | Medium |
| **Precondition** | Detected subscription exists |

**Steps:**
1. Navigate to subscription
2. Mark as "Cancelled" or "Inactive"
3. Verify status update

**Expected Result:**
- Status changes to inactive
- No longer in active subscriptions list
- Historical data preserved

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

## Travel Detection Tests

### TC-TRAV-001: Detect Travel Period

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | Transactions from different city/country |

**Steps:**
1. Import transactions with out-of-town merchants:
   - Hotel in New York
   - Restaurant in New York
   - Uber in New York
   - All within same 3-day period
2. Check Travel Periods

**Expected Result:**
- Travel period detected
- Location identified
- Date range shown
- Related transactions grouped

**Pass:** [ ] **Fail:** [ ]

**Detected Travel:**
| Location | Start Date | End Date | Transaction Count |
|----------|------------|----------|-------------------|
| | | | |

**Notes:**
```
_________________________________________________
```

---

### TC-TRAV-002: Manual Travel Period

| Field | Value |
|-------|-------|
| **Priority** | Medium |
| **Precondition** | Transactions exist |

**Steps:**
1. Create manual travel period
2. Set destination and dates
3. Associate transactions

**Expected Result:**
- Travel period created
- Transactions can be linked
- Period appears in list

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

## Expense Splitting Tests

### TC-SPLIT-001: Split Transaction by Percentage

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | Transaction exists |

**Steps:**
1. Select transaction to split
2. Choose "Split Expense"
3. Set split: 60% Project A, 40% Project B
4. Save split

**Expected Result:**
- Split saved successfully
- Original amount unchanged
- Allocations shown (60%, 40%)
- Total equals 100%

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-SPLIT-002: Split Transaction by Amount

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | Transaction of $100 exists |

**Steps:**
1. Select $100 transaction
2. Split by amount: $75, $25
3. Assign to different departments/projects
4. Save

**Expected Result:**
- Amounts sum to original
- Each allocation has correct amount
- Departments/projects assigned

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-SPLIT-003: Remove Split

| Field | Value |
|-------|-------|
| **Priority** | Medium |
| **Precondition** | Split transaction exists |

**Steps:**
1. Find split transaction
2. Remove split/consolidate
3. Confirm action

**Expected Result:**
- Split removed
- Transaction returns to single entry
- Original amount preserved

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

## Report Generation Tests

### TC-RPT-001: Generate Expense Report

| Field | Value |
|-------|-------|
| **Priority** | Critical |
| **Precondition** | Transactions and receipts exist |

**Steps:**
1. Navigate to Reports
2. Click "Create New Report"
3. Select date range
4. Add transactions/receipts
5. Generate report

**Expected Result:**
- Report created successfully
- All selected items included
- Totals calculated correctly
- Report ID assigned

**Pass:** [ ] **Fail:** [ ]

**Report Summary:**
| Field | Value |
|-------|-------|
| Report ID | |
| Date Range | |
| Total Amount | |
| Item Count | |

**Notes:**
```
_________________________________________________
```

---

### TC-RPT-002: Edit Draft Report

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | Draft report exists |

**Steps:**
1. Open draft report
2. Add/remove items
3. Update description
4. Save changes

**Expected Result:**
- Changes save successfully
- Totals recalculate
- Audit trail updated

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-RPT-003: Submit Report

| Field | Value |
|-------|-------|
| **Priority** | Critical |
| **Precondition** | Complete draft report |

**Steps:**
1. Open complete draft report
2. Click "Submit" or "Submit for Approval"
3. Confirm submission

**Expected Result:**
- Status changes to "Submitted"
- No further edits allowed
- Submission timestamp recorded
- Confirmation displayed

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

## Export Tests

### TC-EXP-001: Export to Excel

| Field | Value |
|-------|-------|
| **Priority** | Critical |
| **Precondition** | Report or transactions exist |

**Steps:**
1. Select report or transactions
2. Click "Export to Excel"
3. Download file
4. Open in Excel

**Expected Result:**
- .xlsx file downloads
- File opens without errors
- All data present
- Formatting preserved
- Formulas work (totals)

**Pass:** [ ] **Fail:** [ ]

**Excel Verification:**
| Check | Pass? |
|-------|-------|
| File opens | |
| Column headers correct | |
| Data matches source | |
| Totals correct | |
| Date format correct | |

**Notes:**
```
_________________________________________________
```

---

### TC-EXP-002: Export to PDF

| Field | Value |
|-------|-------|
| **Priority** | Critical |
| **Precondition** | Report exists |

**Steps:**
1. Select report
2. Click "Export to PDF"
3. Download file
4. Open in PDF reader

**Expected Result:**
- .pdf file downloads
- File opens without errors
- Professional formatting
- All data visible
- Receipt images included (if applicable)

**Pass:** [ ] **Fail:** [ ]

**PDF Verification:**
| Check | Pass? |
|-------|-------|
| File opens | |
| Header/title correct | |
| All transactions listed | |
| Totals shown | |
| Images render | |

**Notes:**
```
_________________________________________________
```

---

### TC-EXP-003: Export Filtered Data

| Field | Value |
|-------|-------|
| **Priority** | Medium |
| **Precondition** | Transactions with applied filters |

**Steps:**
1. Apply filters (date range, category, etc.)
2. Export filtered results
3. Verify export contains only filtered data

**Expected Result:**
- Export respects active filters
- Only filtered data exported
- Filter criteria noted in export

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

## Performance Tests

### TC-PERF-001: Page Load Time

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | Logged in user |

**Steps:**
1. Clear browser cache
2. Navigate to main pages:
   - Dashboard
   - Transactions
   - Receipts
   - Reports
3. Measure load time (browser dev tools)

**Expected Result:**
- Each page loads < 3 seconds
- No timeout errors
- All content renders

**Pass:** [ ] **Fail:** [ ]

**Load Times:**
| Page | Time (seconds) | Pass? |
|------|----------------|-------|
| Dashboard | | |
| Transactions | | |
| Receipts | | |
| Reports | | |

**Notes:**
```
_________________________________________________
```

---

### TC-PERF-002: Large Data Set Handling

| Field | Value |
|-------|-------|
| **Priority** | High |
| **Precondition** | Account with 500+ transactions |

**Steps:**
1. Load transactions page
2. Scroll through full list
3. Apply filters
4. Generate report with all items

**Expected Result:**
- Page remains responsive
- Pagination works
- No browser crashes
- Filters apply quickly

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

### TC-PERF-003: Concurrent Operations

| Field | Value |
|-------|-------|
| **Priority** | Medium |
| **Precondition** | Multiple browser tabs |

**Steps:**
1. Open application in 3 browser tabs
2. Perform different operations simultaneously:
   - Tab 1: Upload receipt
   - Tab 2: Import statement
   - Tab 3: Generate report
3. Verify all complete

**Expected Result:**
- All operations complete
- No conflicts or errors
- Data consistent across tabs

**Pass:** [ ] **Fail:** [ ]

**Notes:**
```
_________________________________________________
```

---

## Test Sign-Off

### Summary

| Category | Total Tests | Passed | Failed | Blocked |
|----------|-------------|--------|--------|---------|
| Authentication | 3 | | | |
| Receipt Pipeline | 5 | | | |
| Statement Import | 5 | | | |
| Transaction Management | 4 | | | |
| Matching Engine | 4 | | | |
| AI Categorization | 3 | | | |
| Subscription Detection | 3 | | | |
| Travel Detection | 2 | | | |
| Expense Splitting | 3 | | | |
| Report Generation | 3 | | | |
| Export | 3 | | | |
| Performance | 3 | | | |
| **TOTAL** | **41** | | | |

### Critical Defects Found

| ID | Test Case | Description | Severity |
|----|-----------|-------------|----------|
| | | | |
| | | | |
| | | | |

### UAT Sign-Off

**Testing Completed By:**

| Name | Role | Date | Signature |
|------|------|------|-----------|
| | Tester | | |
| | Business Owner | | |
| | Product Manager | | |

**UAT Status:** [ ] APPROVED / [ ] APPROVED WITH CONDITIONS / [ ] REJECTED

**Conditions/Comments:**
```
_________________________________________________
_________________________________________________
_________________________________________________
```

---

## Appendix A: API Endpoints Reference

For API-level testing, use these endpoints:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Health check |
| `/api/receipts` | GET/POST | Receipt management |
| `/api/transactions` | GET/POST | Transaction management |
| `/api/statements/analyze` | POST | Statement analysis |
| `/api/statements/import` | POST | Statement import |
| `/api/matching/stats` | GET | Matching statistics |
| `/api/categorization/stats` | GET | Categorization statistics |
| `/api/subscriptions` | GET | Detected subscriptions |
| `/api/reports` | GET/POST | Report management |
| `/api/reports/{id}/export/excel` | GET | Excel export |
| `/api/reports/{id}/export/pdf` | GET | PDF export |

---

## Appendix B: Test Data Templates

### Sample CSV Statement (Chase Format)
```csv
Transaction Date,Post Date,Description,Category,Type,Amount,Memo
12/01/2025,12/02/2025,AMAZON.COM*123ABC,Shopping,Sale,-49.99,
12/02/2025,12/03/2025,UBER *TRIP,Travel,Sale,-23.45,
12/03/2025,12/04/2025,STARBUCKS STORE 123,Food & Drink,Sale,-6.75,
```

### Sample Receipt Data
| Vendor | Date | Amount | Category |
|--------|------|--------|----------|
| Staples | 12/01/2025 | $45.67 | Office Supplies |
| Delta Airlines | 12/05/2025 | $425.00 | Travel |
| Marriott Hotel | 12/05/2025 | $189.00 | Lodging |

---

*End of UAT Testing Guide*
