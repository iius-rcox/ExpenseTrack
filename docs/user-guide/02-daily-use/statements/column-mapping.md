# Column Mapping

Configure how ExpenseFlow reads your bank statement format.

## Overview

[Column Mapping](../../04-reference/glossary.md#column-mapping) tells ExpenseFlow which columns in your statement file contain which data. Once configured, you can save the mapping as a template for future imports.

## The Mapping Interface

![Column mapper interface](../../images/statements/column-mapper.png)
*Caption: The column mapping interface with sample data preview*

The interface shows:
- **Your Columns**: Dropdown menus for each required field
- **Sample Data**: Preview of your file's data
- **Field Descriptions**: Help text explaining each field

## Required Mappings

### Date Column

**Purpose**: Identifies when each transaction occurred

**What to look for**:
- Column named "Date", "Trans Date", "Transaction Date", "Posted", "Posting Date"
- Values should be dates (e.g., "12/15/2025" or "2025-12-15")

**Common formats**:
| Format | Example |
|--------|---------|
| MM/DD/YYYY | 12/15/2025 |
| DD/MM/YYYY | 15/12/2025 |
| YYYY-MM-DD | 2025-12-15 |
| Month DD, YYYY | December 15, 2025 |

ExpenseFlow auto-detects most date formats.

### Amount Column

**Purpose**: Captures the dollar value of each transaction

**Single amount column**:
- Look for "Amount", "Transaction Amount", "Value"
- Configure whether negative means charge or credit

**Separate debit/credit columns**:
- Some banks split into "Debit" and "Credit" columns
- Map both columns separately
- ExpenseFlow combines them automatically

### Amount Sign Convention

Critical setting to get right:

| Your Bank Shows | Setting to Choose |
|-----------------|-------------------|
| Purchases as -$50.00 | Negative = Charges |
| Purchases as $50.00 | Positive = Charges |
| Credits as negative | Negative = Credits |

> **Tip**: Check the sample data preview to verify amounts display correctly after configuration.

### Description Column

**Purpose**: Transaction details for matching and identification

**What to look for**:
- "Description", "Memo", "Details", "Transaction Description"
- Contains merchant names, reference numbers, etc.

**Quality matters**:
- Detailed descriptions improve AI matching
- "STARBUCKS COFFEE #1234" is better than "POS PURCHASE"
- If your bank provides minimal descriptions, matching accuracy may be lower

## Optional Mappings

These fields aren't required but improve accuracy:

### Merchant/Vendor

If your bank provides a separate merchant column:
- Map it for better matching accuracy
- The AI uses this for vendor identification
- More reliable than parsing from description

### Category

Bank-provided transaction categories:
- Pre-populates expense category suggestions
- Saves time during categorization
- Can be overridden by your selections

### Reference Number

Transaction IDs or reference numbers:
- Helps with duplicate detection
- Useful for reconciliation
- Typically unique per transaction

### Posted vs Transaction Date

Some statements show both:
- **Transaction Date**: When the purchase occurred
- **Posted Date**: When it appeared on your statement
- Map the date most useful for matching receipts (usually transaction date)

## Visual Verification

### Sample Data Preview

The preview table shows how your mapping affects the data:

1. Check the first 5-10 rows
2. Verify dates look like dates
3. Confirm amounts are reasonable
4. Ensure descriptions are readable

### Warning Indicators

The mapper warns about:
- **Missing data**: Required column has empty values
- **Format issues**: Values don't match expected format
- **Unusual patterns**: Potential mapping errors

## Common Mapping Scenarios

### Standard Bank Statement

```
Date       | Description          | Amount
12/15/2025 | STARBUCKS COFFEE    | -5.75
12/15/2025 | AMAZON.COM          | -47.99
```

**Mapping**:
- Date → Date column
- Description → Description column
- Amount → Amount column
- Sign: Negative = Charges

### Separate Debit/Credit

```
Date       | Description    | Debit  | Credit
12/15/2025 | STARBUCKS     | 5.75   |
12/16/2025 | PAYMENT       |        | 500.00
```

**Mapping**:
- Date → Date column
- Description → Description column
- Debit → Debit Amount
- Credit → Credit Amount

### Credit Card with Merchant Column

```
Trans Date | Post Date  | Merchant      | Description     | Amount
12/15/25   | 12/16/25   | STARBUCKS     | COFFEE #1234    | 5.75
```

**Mapping**:
- Trans Date → Date column
- Merchant → Merchant column
- Description → Description column
- Amount → Amount column
- Sign: Positive = Charges

## Troubleshooting Mapping

### Can't Find My Columns

1. Scroll through the dropdown completely
2. Check if your file has a header row
3. Some columns may have unusual names
4. Try opening the file in Excel to verify structure

### Dates Display Wrong

1. Check the date format in your file
2. Verify you selected the correct column
3. Try a different date column if multiple exist

### Amounts All Zero or Wrong

1. Verify amount column selection
2. Check sign convention setting
3. If using debit/credit, ensure both are mapped

### Description Missing

1. Your file may not have descriptions
2. Try mapping a different column
3. Minimal descriptions affect matching accuracy

## What's Next

After mapping columns:

- [Fingerprints](./fingerprints.md) - Save your mapping as a template
- [Importing Statements](./importing.md) - Complete the import process
- [Filtering](../transactions/filtering.md) - Work with imported transactions
