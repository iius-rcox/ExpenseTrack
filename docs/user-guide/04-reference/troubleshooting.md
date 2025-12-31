# Troubleshooting

← [Back to Reference](./README.md)

Common issues and step-by-step solutions for ExpenseFlow.

---

## Receipt Upload Issues

### Upload Fails with "File Too Large"

**Symptoms**: You see an error message when uploading a receipt saying the file exceeds the size limit.

**Cause**: The file size exceeds the 25MB maximum.

**Solution**:

1. Check the file size in your file manager
2. If it's a photo, compress it:
   - **iPhone**: Use the Photos app's editing to reduce quality
   - **Android**: Use a photo compression app
   - **Desktop**: Open in an image editor and save with lower quality (80% JPEG)
3. If it's a multi-page PDF, split it into separate pages
4. Try uploading the smaller file

**Prevention**: Enable "Optimize photos" or "Reduce file size" in your camera settings before capturing receipts.

---

### Upload Fails with "Unsupported Format"

**Symptoms**: Error message indicates the file type is not accepted.

**Cause**: ExpenseFlow only accepts specific image formats.

**Solution**:

1. Check the file extension
2. Accepted formats: **JPEG, PNG, HEIC, PDF**
3. If your file is a different format (e.g., .doc, .tiff, .webp):
   - Convert it using an online converter or image editor
   - Save as JPEG or PNG
4. Retry the upload

**Prevention**: When saving or exporting receipts, always choose JPEG, PNG, or PDF format.

---

### Receipt Stuck in "Processing" Status

**Symptoms**: The receipt shows "Processing" status for more than 5 minutes.

**Cause**: AI extraction is taking longer than usual, or there's a temporary system issue.

**Solution**:

1. Wait 2-3 minutes (processing can take time for complex receipts)
2. Refresh your browser (F5 or Cmd+R)
3. If still stuck, click the **Retry** button on the receipt
4. If retry fails, try re-uploading the receipt

**Prevention**: Upload clear, well-lit images. Blurry or dark images take longer to process.

---

### AI Extraction Shows All Fields as Low Confidence

**Symptoms**: All extracted fields show red (low confidence) indicators.

**Cause**: The receipt image is unclear, damaged, or in an unusual format.

**Solution**:

1. Check if the image is:
   - Blurry or out of focus
   - Very dark or overexposed
   - Crumpled or faded
   - Partially cut off
2. If possible, take a new photo with better lighting
3. If original receipt is unavailable, manually enter the data:
   - Click each field to edit
   - Enter vendor, date, and amount manually
4. The system will learn from your corrections

**Prevention**:
- Capture receipts on a flat, contrasting surface
- Ensure good lighting (avoid shadows)
- Keep the entire receipt in frame

---

## Statement Import Issues

### Statement Import Shows "Invalid CSV"

**Symptoms**: Error during file upload step indicating the CSV file is invalid.

**Cause**: The file has encoding issues, missing headers, or incorrect format.

**Solution**:

1. Open the file in a spreadsheet application (Excel, Google Sheets)
2. Check for issues:
   - Are there column headers in the first row?
   - Is the data properly separated into columns?
   - Are there any special characters causing problems?
3. Export/Save As a new CSV file:
   - Choose **UTF-8** encoding
   - Use **comma** as the delimiter
4. Try importing the new file

**Prevention**: When downloading statements, choose "CSV (UTF-8)" format if available.

---

### Column Mapping Doesn't Show My Columns

**Symptoms**: Expected columns don't appear in the dropdown during mapping.

**Cause**: The file structure is inconsistent or headers are not recognized.

**Solution**:

1. Scroll through the entire dropdown list
2. Check if your column headers contain special characters
3. Open the CSV and verify:
   - Headers are in row 1
   - Column names are simple text (no formulas)
   - All rows have the same number of columns
4. If columns are merged or split incorrectly:
   - Clean up the file in Excel
   - Ensure each piece of data is in its own column
5. Re-import the cleaned file

---

### Duplicate Transactions Imported

**Symptoms**: The same transactions appear multiple times after import.

**Cause**: The statement file contains data already in the system.

**Solution**:

1. ExpenseFlow automatically detects most duplicates
2. If duplicates still appear:
   - Go to the Transactions page
   - Use filters to find the duplicates (same date, amount, description)
   - Select the duplicate transactions
   - Click **Delete** in the bulk actions bar
3. Only delete the extras, not the originals

**Prevention**:
- Import each statement period only once
- Use date ranges when downloading statements to avoid overlap

---

## Matching Issues

### No Match Proposals Appearing

**Symptoms**: The Matching page shows no proposals, even though you have receipts and transactions.

**Cause**: Auto-matching hasn't run, or there are no viable matches.

**Solution**:

1. Click the **Auto-Match** button on the Matching page
2. Wait for the matching process to complete
3. If still no proposals:
   - Verify you have unmatched receipts (check Receipts page)
   - Verify you have unmatched transactions (check Transactions page)
   - Check date ranges overlap (receipt dates should be near transaction dates)
4. If amounts are very different, matches won't be proposed
5. Use [Manual Matching](../02-daily-use/matching/manual-matching.md) to create links yourself

---

### Match Rejected by Accident

**Symptoms**: You rejected a match proposal that was actually correct.

**Cause**: Clicked Reject (R) instead of Approve (A), or made a quick decision.

**Solution**:

1. Rejected proposals cannot be restored
2. Use **Manual Matching** to create the correct link:
   - Go to the Matching page
   - Click **Manual Match**
   - Search for the receipt and transaction
   - Create the link manually
3. The receipt and transaction will be matched

**Prevention**: Take your time reviewing proposals, especially when using keyboard shortcuts.

---

## Expense Splitting Issues

### Split Allocations Won't Save

**Symptoms**: Error when saving a split, or the save button is disabled.

**Cause**: The allocation percentages don't sum to exactly 100%.

**Solution**:

1. Check your percentage values
2. They must total exactly **100%** (not 99% or 101%)
3. Common fixes:
   - If showing 99.9%, adjust one percentage up by 0.1%
   - If showing 100.1%, adjust one percentage down by 0.1%
4. Click **Save** again

**Prevention**: Use round numbers when possible (50/50, 60/40, 33/33/34).

---

## General Issues

### Session Expired During Upload

**Symptoms**: Error about authentication or session, upload may have failed.

**Cause**: Your login session timed out due to inactivity.

**Solution**:

1. Sign in again using the login page
2. Check if the upload completed:
   - Go to the Receipts page
   - Look for your recent upload
3. If the upload didn't complete:
   - Re-upload the file(s)
   - ExpenseFlow will detect duplicates if the upload did complete

**Prevention**:
- Save your work regularly
- Stay active in the application or refresh occasionally during long sessions

---

### Network Error During Operation

**Symptoms**: Error message about network connectivity or timeout.

**Cause**: Internet connection was interrupted or server didn't respond in time.

**Solution**:

1. Check your internet connection:
   - Try loading another website
   - Check WiFi or ethernet connection
2. Refresh the page (F5 or Cmd+R)
3. Retry the operation
4. Most operations are resumable:
   - Uploads: Re-upload the file
   - Imports: Re-run the import
   - Saves: Re-submit the form

**Prevention**: Ensure stable internet connection, especially for file uploads.

---

## Quick Reference

| Issue | Quick Fix |
|-------|-----------|
| File too large | Compress image or split PDF |
| Unsupported format | Convert to JPEG, PNG, or PDF |
| Stuck processing | Click Retry button |
| Low confidence | Re-upload clearer image or edit manually |
| Invalid CSV | Re-export with UTF-8 encoding |
| Missing columns | Check file for header row |
| Duplicates | Delete extras from Transactions page |
| No match proposals | Click Auto-Match button |
| Rejected wrong match | Use Manual Matching |
| Split won't save | Ensure percentages equal 100% |
| Session expired | Sign in again |
| Network error | Check connection and retry |

---

## Still Need Help?

If these solutions don't resolve your issue:

1. Note the exact error message
2. Capture a screenshot if possible
3. Contact your ExpenseFlow administrator

**See also**: [Glossary](./glossary.md) for term definitions
