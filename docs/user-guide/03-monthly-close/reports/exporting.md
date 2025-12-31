# Exporting Reports

Download expense reports as PDF or Excel files.

## Overview

Export your expense reports for record-keeping, submission to external systems, or offline review. ExpenseFlow supports PDF and Excel formats.

## Export Formats

### PDF Export

**Best for**:
- Printing
- Email attachments
- Formal submissions
- Archive records

**Includes**:
- Report summary
- Detailed expense list
- Receipt images
- Category totals
- Signatures (if applicable)

### Excel Export

**Best for**:
- Data analysis
- Import to other systems
- Custom calculations
- Detailed review

**Includes**:
- All expense data in rows/columns
- Category codes
- Dates and amounts
- Transaction references
- NO receipt images (data only)

## How to Export

### From Report Details

1. Open the report you want to export
2. Click **Export** or the download icon
3. Select format: **PDF** or **Excel**

![Export dropdown](../../images/reports/export-dropdown.png)
*Caption: Export format selection dropdown*

4. Click to download
5. File saves to your downloads folder

### From Reports List

1. Go to the **Reports** page
2. Find the report to export
3. Click the **...** (more actions) menu
4. Select **Export as PDF** or **Export as Excel**

## PDF Contents

### Page 1: Summary

- Report title and date range
- Your name and department
- Total amount
- Expense count
- Category breakdown pie chart

### Subsequent Pages: Details

- Line-by-line expense list
- Date, vendor, amount, category
- Notes for each expense

### Final Pages: Receipts

- Receipt images (one or more per page)
- Linked to expense line items
- Scaled to fit page

## Excel Contents

### Columns Included

| Column | Description |
|--------|-------------|
| Date | Transaction date |
| Vendor | Merchant name |
| Description | Transaction description |
| Amount | Expense amount |
| Category | Expense category |
| GL Code | General ledger code |
| Department | Cost center |
| Project | Project allocation |
| Status | Match status |
| Receipt | Receipt filename reference |

### Multiple Sheets (if applicable)

- **Expenses**: Main expense data
- **Summary**: Totals by category
- **Allocations**: Split details (if used)

## Export All Receipts

To export just the receipt images:

1. Open the report
2. Look for **Export Receipts** or **Download Images**
3. Receipts download as a ZIP file
4. Extract to view individual images

## Batch Export

Export multiple reports at once (if available):

1. Go to **Reports** page
2. Select multiple reports (checkboxes)
3. Click **Export Selected**
4. Choose format
5. Files download as ZIP or combined PDF

## Use Cases

### For Your Records

- Export approved reports as PDF
- Store in personal archives
- Reference for tax purposes

### For External Systems

- Export as Excel
- Import to corporate expense system
- Integrate with accounting software

### For Manager Review

- Export draft as PDF
- Share for informal review
- Get feedback before submitting

### For Auditing

- Export all reports for a period
- Include receipts in PDF
- Maintain complete records

## Troubleshooting

### Download Doesn't Start

- Check browser pop-up blockers
- Try a different browser
- Ensure report has content

### PDF Missing Receipts

- Some receipts may not render
- Try exporting receipts separately
- Check original receipt upload quality

### Excel File Won't Open

- Try opening in Google Sheets
- Check for file corruption
- Re-export the file

## What's Next

After exporting:

- [Generating Reports](./generating.md) - Create new reports
- [Status Workflow](./status-workflow.md) - Track approvals
- [Analytics](../analytics/dashboard.md) - Analyze spending
