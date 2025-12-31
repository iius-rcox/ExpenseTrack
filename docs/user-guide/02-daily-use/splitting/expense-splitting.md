# Expense Splitting

← [Back to Daily Use](../README.md) | [Splitting Section](./split-patterns.md)

Divide expenses across multiple departments, projects, or [GL codes](../../04-reference/glossary.md#gl-code).

## Overview

[Expense Splitting](../../04-reference/glossary.md#expense-splitting) lets you allocate a single expense to multiple cost centers. Use this when an expense benefits multiple departments or should be charged to different projects.

## When to Split Expenses

Common splitting scenarios:

| Scenario | Example |
|----------|---------|
| **Shared team expenses** | Lunch for two project teams |
| **Multi-department purchases** | Software license used by IT and Marketing |
| **Project allocation** | Travel benefiting multiple projects |
| **Cost center distribution** | Office supplies for shared space |

## Accessing the Split Feature

### From Transaction List

1. Select a transaction
2. Click **Split** or the split icon (scissor)
3. The split editor opens

### From Match Confirmation

1. When confirming a match
2. Click **Split Before Matching** option
3. Complete the split, then confirm

### From Expense Detail

1. Open expense details
2. Click **Edit Allocations** or **Split**
3. Add allocation rows

## Using the Split Editor

![Split allocation editor](../../images/splitting/split-allocation-editor.png)
*Caption: The split allocation editor showing percentage and amount fields*

### Interface Elements

| Element | Description |
|---------|-------------|
| **Original Amount** | The full expense amount (read-only) |
| **Allocation Rows** | Each destination for a portion |
| **Percentage Field** | % of total for this allocation |
| **Amount Field** | Dollar amount (auto-calculated) |
| **Department/Project** | Where this portion is charged |
| **Add Row** | Add another allocation |
| **Remove** | Delete an allocation row |
| **Total** | Must equal 100% / original amount |

## Creating a Split

### Step 1: Open Split Editor

Click **Split** on the expense you want to divide.

### Step 2: Define First Allocation

1. Select **Department** or **Project** from dropdown
2. Enter **Percentage** (e.g., 60%)
3. Amount auto-calculates (e.g., $60 of $100)
4. Or enter **Amount** and percentage auto-calculates

### Step 3: Add Additional Allocations

1. Click **Add Row** or **+**
2. Repeat for each destination
3. Continue until you've allocated 100%

### Step 4: Verify Totals

Before saving, check:

- ✓ All percentages sum to 100%
- ✓ All amounts sum to original total
- ✓ Each allocation has a valid destination

### Step 5: Save Split

1. Click **Save** or **Apply Split**
2. The expense now shows as split
3. Each portion appears in its department/project

## Split Methods

### By Percentage

Use percentages when:

- You want even or proportional splits
- The amounts should adjust if expense changes
- You're following a standard allocation formula

**Example**:
- Marketing: 40%
- Sales: 35%
- Operations: 25%

### By Amount

Use fixed amounts when:

- Each portion has a specific dollar value
- You're matching to a known budget
- Precision is more important than ratio

**Example** (Total $150):
- Project A: $75.00
- Project B: $50.00
- Project C: $25.00

### Mixed Method

You can combine:

1. Enter percentage for some rows
2. Enter amount for others
3. System calculates remaining to reach 100%

## Editing Existing Splits

### Modify Allocations

1. Open the split expense
2. Click **Edit Split**
3. Adjust percentages or amounts
4. Save changes

### Remove a Split

1. Open the split expense
2. Click **Remove Split** or delete allocation rows
3. Leave only one allocation at 100%
4. Save to restore single allocation

## Split Validation

### Automatic Checks

The system validates:

| Check | Error if Failed |
|-------|-----------------|
| Sum to 100% | "Allocations must total 100%" |
| Valid destinations | "Select department/project" |
| Positive amounts | "Amounts must be positive" |
| No duplicates | "Duplicate allocation destination" |

### Rounding

For splits that don't divide evenly:

- System handles penny rounding
- Remainder goes to the last allocation
- Example: $100 / 3 = $33.33 + $33.33 + $33.34

## Viewing Split Expenses

### In Transaction List

Split expenses show:

- Split indicator icon
- Number of allocations (e.g., "3 allocations")
- Primary allocation first

### In Reports

Split expenses appear:

- As separate line items per allocation
- Or grouped with subtotal (depends on report format)
- Category shows for each portion

### In Analytics

- Each allocation counts toward its department/project
- Category breakdowns reflect split portions
- Filters work on individual allocations

## Best Practices

### Consistent Splitting

- Use the same ratios for similar expenses
- Save patterns for [reuse](./split-patterns.md)
- Document your allocation rules

### Avoid Over-Splitting

- Don't split expenses under $20
- Limit to 4-5 allocations maximum
- Simple splits are easier to audit

### Pre-Plan Allocations

- Know your split before uploading
- Use saved patterns when possible
- Review with finance for complex allocations

## Troubleshooting

### "Allocations Don't Sum to 100%"

1. Check for rounding errors
2. Use percentage mode for easier balancing
3. Adjust the last row to compensate

### "Cannot Split Already-Reported Expense"

1. Expense is in a submitted/approved report
2. Request report rejection if changes needed
3. Or note for next period

### "Department Not Available"

1. Check if department is still active
2. Verify your permissions
3. Contact admin if needed

## What's Next

After understanding splitting:

- [Split Patterns](./split-patterns.md) - Save and reuse allocation templates
- [Bulk Operations](../transactions/bulk-operations.md) - Apply splits to multiple items
- [Generating Reports](../../03-monthly-close/reports/generating.md) - Reports include split allocations

