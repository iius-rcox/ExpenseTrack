# Quick Start

Upload your first receipt and see ExpenseFlow's AI extraction in action.

## Overview

This guide walks you through uploading a receipt and reviewing the AI-extracted data. By the end, you'll understand the core ExpenseFlow workflow.

**Time needed**: 5 minutes

## Before You Begin

Make sure you have:
- Successfully [signed in](./signing-in.md) to ExpenseFlow
- A receipt ready (photo on your phone, PDF, or image file)

## Step 1: Navigate to Receipts

From your dashboard:

1. Click **Receipts** in the top navigation bar
2. You'll see the receipts page with an upload area

## Step 2: Upload Your Receipt

Choose one of these upload methods:

### Option A: Drag and Drop (Recommended)

1. Open your file manager showing the receipt file
2. Drag the file onto the upload zone
3. Drop it when you see the highlighted area

### Option B: Browse Files

1. Click the **Upload** button or the upload zone
2. Browse to select your receipt file
3. Click **Open** to upload

### Option C: Mobile Camera (Mobile Only)

1. Tap the camera icon in the upload zone
2. Allow camera access if prompted
3. Position the receipt within the frame
4. Tap to capture
5. Review and confirm the photo

> **Tip**: Upload multiple files at once by selecting them together before dropping or clicking Open.

## Step 3: Wait for Processing

After upload:

1. The receipt shows a **"Processing"** status
2. AI extraction typically takes 5-30 seconds
3. The status changes to **"Complete"** when done

![Processing status](../images/receipts/upload-processing.png)
*Caption: Receipt showing processing status while AI extracts data*

## Step 4: Review Extracted Data

Once processing completes:

1. Click the receipt to open the detail view
2. Review the AI-extracted fields:
   - **Vendor**: The merchant name
   - **Date**: Transaction date
   - **Amount**: Total amount
   - **Category**: Suggested expense category

![Extracted data](../images/receipts/ai-extraction-confidence.png)
*Caption: AI-extracted fields with confidence indicators*

### Understanding Confidence Scores

Each field shows a [Confidence Score](../04-reference/glossary.md#confidence-score):

| Color | Confidence | Action |
|-------|------------|--------|
| **Green** | 90%+ (High) | Likely correct, quick verify |
| **Amber** | 70-89% (Medium) | Review recommended |
| **Red** | Below 70% (Low) | Manual review required |

## Step 5: Correct Any Errors

If the AI made mistakes:

1. Click the field you want to edit
2. Enter the correct value
3. Press **Enter** or click away to save
4. The field updates immediately

> **Tip**: Your corrections help train the AI for better future extractions.

## You Did It!

Congratulations! You've:
- Uploaded a receipt
- Reviewed AI-extracted data
- Understood confidence scores
- Learned how to make corrections

## What Happens Next

Your receipt is now ready for matching. When you import a bank statement containing the corresponding transaction, ExpenseFlow will:

1. Analyze the transaction against your receipts
2. Propose a match based on amount, date, and vendor
3. Present the match for your approval

## Common Questions

### What if my receipt fails to upload?

See [Troubleshooting - Upload Issues](../04-reference/troubleshooting.md#upload-fails-with-file-too-large)

### What file types are supported?

ExpenseFlow accepts: **JPEG, PNG, HEIC, PDF** (max 25MB)

### Can I upload multiple receipts?

Yes! Select multiple files or drop them all at once. They'll process in parallel.

### What if extraction completely fails?

You can manually enter all fields. See [AI Extraction](../02-daily-use/receipts/ai-extraction.md) for details.

## What's Next

Continue your ExpenseFlow journey:

- [AI Extraction](../02-daily-use/receipts/ai-extraction.md) - Deep dive into extraction features
- [Importing Statements](../02-daily-use/statements/importing.md) - Add your bank transactions
- [Review Modes](../02-daily-use/matching/review-modes.md) - Match receipts to transactions
