# AI Extraction

Understand how ExpenseFlow reads your receipts and how to correct any errors.

## Overview

When you upload a receipt, ExpenseFlow's AI analyzes the image to extract key information automatically. This process, called [AI Extraction](../../04-reference/glossary.md#ai-extraction), identifies vendor names, dates, amounts, and more.

## Extracted Fields

The AI attempts to extract these fields from each receipt:

| Field | Description | Example |
|-------|-------------|---------|
| **Vendor** | Business or merchant name | "Starbucks Coffee" |
| **Date** | Transaction date | "Dec 15, 2025" |
| **Amount** | Total amount paid | "$47.82" |
| **Currency** | Currency code | "USD" |
| **Tax** | Tax amount (if visible) | "$3.42" |
| **Payment Method** | How you paid (if visible) | "Visa ****1234" |
| **Category** | Suggested expense type | "Meals & Entertainment" |

## Understanding Confidence Scores

Each extracted field shows a [Confidence Score](../../04-reference/glossary.md#confidence-score) indicating how certain the AI is about its accuracy.

![Confidence indicators](../../images/receipts/ai-extraction-confidence.png)
*Caption: Fields with green (high), amber (medium), and red (low) confidence indicators*

### Confidence Levels

| Color | Score | Meaning | Recommended Action |
|-------|-------|---------|-------------------|
| **Green** | 90-100% | High confidence | Quick visual verify |
| **Amber** | 70-89% | Medium confidence | Review carefully |
| **Red** | 0-69% | Low confidence | Manual verification required |

### Why Confidence Varies

The AI's confidence depends on:

- **Image quality**: Clear, well-lit photos score higher
- **Receipt format**: Standard retail formats are easier to read
- **Handwriting**: Handwritten notes reduce confidence
- **Fading**: Old or thermal receipts may be faded
- **Language**: Non-English receipts may have lower confidence

## Reviewing Extracted Data

### Quick Review (High Confidence)

For fields with green indicators:

1. Glance at the extracted value
2. Compare briefly to the receipt image
3. Move on if it looks correct

### Careful Review (Medium Confidence)

For amber fields:

1. Look at the receipt image closely
2. Compare each character in the extracted value
3. Correct any mistakes (see below)

### Manual Entry (Low Confidence)

For red fields:

1. The AI is unsure, so verify completely
2. Click the field to edit
3. Enter the correct value from the receipt
4. Your correction improves future extractions

## Editing Extracted Fields

### Inline Editing

To correct a field:

1. Click the field you want to edit
2. The field becomes editable

   ![Edit mode](../../images/receipts/extraction-edit-mode.png)
   *Caption: Field in edit mode ready for correction*

3. Type the correct value
4. Press **Enter** or click outside to save
5. The field updates immediately

### Keyboard Navigation

When editing:

| Key | Action |
|-----|--------|
| **Tab** | Move to next field |
| **Shift+Tab** | Move to previous field |
| **Enter** | Save and exit edit |
| **Escape** | Cancel changes |

### Undo/Redo

Made a mistake while editing?

- **Undo**: Press **Ctrl+Z** (Windows) or **Cmd+Z** (Mac)
- **Redo**: Press **Ctrl+Y** (Windows) or **Cmd+Shift+Z** (Mac)

You can undo up to 10 previous edits. See [Undo Stack](../../04-reference/glossary.md#undo-stack) in the glossary.

## When Extraction Fails

Sometimes the AI can't extract any meaningful data:

### Complete Extraction Failure

**Symptoms**: All fields show red or no data is extracted

**Causes**:
- Very blurry image
- Receipt completely faded
- Non-receipt document uploaded
- Unusual receipt format

**Solutions**:
1. Try re-uploading a clearer photo
2. Manually enter all fields
3. See [Troubleshooting](../../04-reference/troubleshooting.md#ai-extraction-shows-all-fields-as-low-confidence)

### Partial Extraction Failure

**Symptoms**: Some fields extracted, others blank or wrong

**Solutions**:
1. Fill in missing fields manually
2. Correct incorrect fields
3. The partial data still saves time

## How Your Corrections Help

When you correct AI extraction errors:

1. Your correction is saved immediately
2. The system learns from your corrections
3. Future receipts from the same vendor may extract better
4. Pattern recognition improves over time

> **Tip**: Consistent corrections (always using "Starbucks" vs "Starbucks Coffee") help the AI learn faster.

## Best Practices

### For Better Extraction

- Upload high-quality images
- Ensure good lighting when capturing
- Include the full receipt in frame
- Avoid uploading partial receipts

### For Efficient Review

- Review red fields first (most likely wrong)
- Spot-check green fields occasionally
- Build a routine for daily receipt review
- Correct consistently for better AI learning

### When to Skip Review

If you're short on time:
- High-confidence extractions are usually correct
- Focus on amber and red fields
- You can always review again later

## What's Next

After reviewing extractions:

- [Image Viewer](./image-viewer.md) - Zoom in for hard-to-read receipts
- [Matching](../matching/review-modes.md) - Connect receipts to transactions
- [Troubleshooting](../../04-reference/troubleshooting.md) - Resolve extraction issues
