# Split Patterns

Save and reuse allocation templates for consistent expense splitting.

## Overview

A [Split Pattern](../../04-reference/glossary.md#split-pattern) is a saved allocation template that you can apply to expenses with one click. Patterns ensure consistent splitting across similar expenses and save time on recurring allocations.

## Why Use Patterns

| Benefit | Description |
|---------|-------------|
| **Consistency** | Same split every time for similar expenses |
| **Speed** | One click vs. manual entry |
| **Accuracy** | Predefined ratios eliminate calculation errors |
| **Compliance** | Follows approved allocation formulas |

## Creating a Pattern

### From an Existing Split

The easiest way to create a pattern:

1. [Split an expense](./expense-splitting.md) as desired
2. Click **Save as Pattern** in the split editor
3. Enter a pattern name (e.g., "Marketing/Sales 60-40")
4. Add optional description
5. Click **Save Pattern**

![Pattern save dialog](../../images/splitting/pattern-save-dialog.png)
*Caption: Saving a new split pattern from an existing allocation*

### From Pattern Manager

1. Go to **Settings** → **Split Patterns**
2. Click **Create New Pattern**
3. Add pattern name
4. Define allocations:
   - Add rows for each destination
   - Set percentages
   - Select departments/projects
5. Save the pattern

## Pattern Structure

Each pattern contains:

| Field | Description | Example |
|-------|-------------|---------|
| **Name** | Display name | "Shared Office Supplies" |
| **Description** | Optional notes | "Use for common area purchases" |
| **Allocations** | Destination list | Admin: 40%, Ops: 30%, IT: 30% |
| **Created By** | Who made it | Your name |
| **Last Used** | Recent usage | Dec 15, 2024 |

## Applying a Pattern

### From Expense Split

1. Open the split editor for an expense
2. Click **Apply Pattern** or select from dropdown
3. Choose your pattern
4. Allocations populate automatically
5. Adjust if needed, then save

### From Transaction List

1. Select one or more transactions
2. Click **Split** → **Apply Pattern**
3. Select pattern
4. Confirm application
5. All selected expenses receive the split

### Quick Apply

For frequently-used patterns:

1. Right-click an expense
2. Select **Quick Split** → **[Pattern Name]**
3. Split applies immediately

## Managing Patterns

### Viewing All Patterns

1. Go to **Settings** → **Split Patterns**
2. See list of all saved patterns
3. Sort by name, usage, or date

### Editing a Pattern

1. Find the pattern in Settings
2. Click **Edit**
3. Modify allocations as needed
4. Save changes

> **Note**: Editing a pattern doesn't change previously-split expenses. Only new applications use the updated pattern.

### Deleting a Pattern

1. Find the pattern in Settings
2. Click **Delete**
3. Confirm deletion

> **Warning**: Deleted patterns cannot be recovered. Previously-split expenses remain unchanged.

### Duplicating a Pattern

1. Find an existing pattern
2. Click **Duplicate**
3. Edit the copy with variations
4. Save as new pattern

## Pattern Examples

### Project Split (Equal)

**Name**: "Joint Project ABC-XYZ"
| Allocation | Percentage |
|------------|------------|
| Project ABC | 50% |
| Project XYZ | 50% |

### Department Split (Proportional)

**Name**: "IT Software License"
| Allocation | Percentage |
|------------|------------|
| Engineering | 45% |
| IT Operations | 30% |
| QA | 15% |
| DevOps | 10% |

### Cost Center Split (Fixed)

**Name**: "Shared Meeting Room"
| Allocation | Percentage |
|------------|------------|
| Admin | 33.34% |
| Operations | 33.33% |
| HR | 33.33% |

## Best Practices

### Naming Conventions

Good pattern names are:

- ✓ Descriptive: "Marketing/Sales 70-30"
- ✓ Purpose-based: "Conference Room Supplies"
- ✓ Clear ratios: "Three-Way Equal Split"

Avoid:

- ✗ Vague: "Split 1"
- ✗ Personal: "John's Pattern"
- ✗ Dated: "2024 Q1 Split"

### Pattern Hygiene

Regular maintenance:

1. Review patterns quarterly
2. Archive unused patterns
3. Update for organizational changes
4. Standardize naming

### Shared vs. Personal

| Type | When to Use |
|------|-------------|
| **Shared** | Company-wide allocation rules |
| **Personal** | Your specific expense patterns |

Check with finance for official allocation patterns.

## Troubleshooting

### "Pattern Doesn't Match Expense"

If pattern categories don't apply:

1. Expense may need different allocation
2. Edit split after applying pattern
3. Or create a new pattern for this type

### "Department in Pattern No Longer Exists"

When organizational structure changes:

1. Edit the pattern
2. Replace outdated departments
3. Update all affected patterns

### "Cannot Edit Shared Pattern"

You may not have permission:

1. Contact finance/admin
2. Request pattern modification
3. Or create a personal copy

## What's Next

After mastering patterns:

- [Expense Splitting](./expense-splitting.md) - Learn manual splitting
- [Bulk Operations](../transactions/bulk-operations.md) - Apply patterns to multiple expenses
- [Categories Analysis](../../03-monthly-close/analytics/categories.md) - See how splits affect reporting

