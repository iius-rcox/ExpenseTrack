# TC-001: Receipt Upload Flow

**Test Case ID**: TC-001
**Feature**: Receipt Upload Pipeline
**Sprint**: 3
**Priority**: Critical
**Status**: Not Started

## Description
Validate the complete receipt upload workflow including file upload, OCR processing, thumbnail generation, and data extraction.

## Preconditions
1. User is authenticated and logged into the application
2. Staging environment is deployed and accessible
3. Azure Document Intelligence service is operational
4. Azure Blob Storage is accessible

## Test Data Required
- Sample receipt images:
  - `test-receipt-grocery.jpg` (standard grocery receipt)
  - `test-receipt-restaurant.pdf` (PDF format)
  - `test-receipt-gas.heic` (HEIC format from iPhone)
  - `test-receipt-hotel.png` (hotel receipt with multiple line items)

## Test Scenarios

### Scenario 1: Basic Receipt Upload
**Steps**:
1. Navigate to Receipt Upload page
2. Click "Upload Receipt" button
3. Select `test-receipt-grocery.jpg` from local files
4. Wait for upload progress to complete
5. Verify receipt appears in receipt list

**Expected Result**:
- Upload progress shows 100%
- Success notification displayed
- Receipt visible in list with thumbnail
- Receipt status shows "Processing" then "Completed"

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 2: PDF Receipt Processing
**Steps**:
1. Upload `test-receipt-restaurant.pdf`
2. Wait for OCR processing to complete
3. View extracted receipt details
4. Verify extracted fields: vendor, date, total, line items

**Expected Result**:
- PDF successfully converted and processed
- Vendor name extracted correctly
- Date extracted and parsed correctly
- Total amount extracted correctly
- At least one line item extracted

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 3: HEIC Format Conversion
**Steps**:
1. Upload `test-receipt-gas.heic`
2. Verify file is converted to processable format
3. Wait for processing to complete
4. View thumbnail and extracted data

**Expected Result**:
- HEIC file accepted without error
- Thumbnail generated correctly
- OCR processing completes successfully
- Data extracted from receipt

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 4: Multiple Line Item Extraction
**Steps**:
1. Upload `test-receipt-hotel.png` (hotel receipt with room, taxes, fees)
2. Wait for processing to complete
3. View extracted line items
4. Verify all line items are captured

**Expected Result**:
- Multiple line items extracted
- Each line item has description and amount
- Line items sum to total (within tolerance)
- Room rate, taxes, and fees separated correctly

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 5: Receipt Upload Error Handling
**Steps**:
1. Attempt to upload a corrupted image file
2. Verify error handling behavior
3. Verify user can retry upload

**Expected Result**:
- Appropriate error message displayed
- No system crash or unhandled exception
- User can attempt another upload
- Failed upload does not appear in list (or shows as Failed)

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

## Summary

| Scenario | Result | Notes |
|----------|--------|-------|
| 1. Basic Upload | - | - |
| 2. PDF Processing | - | - |
| 3. HEIC Conversion | - | - |
| 4. Multiple Line Items | - | - |
| 5. Error Handling | - | - |

**Overall Status**: Not Started

**Executed By**: _[Name]_
**Execution Date**: _[Date]_

## Linked Defects
- _None yet_

## Notes
_[Any observations during testing]_
