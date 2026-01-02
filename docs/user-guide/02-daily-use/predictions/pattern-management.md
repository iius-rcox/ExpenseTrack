# Pattern Management

The Pattern Dashboard lets you view and manage the expense patterns that ExpenseFlow has learned from your historical expense reports.

## Accessing the Pattern Dashboard

1. Navigate to **Analytics** from the main navigation
2. Click the **Expense Patterns** button in the header

## Understanding Patterns

Each pattern represents a vendor that has appeared in your approved expense reports:

| Column | Description |
|--------|-------------|
| Vendor | The display name of the vendor (e.g., "STARBUCKS") |
| Category | The expense category assigned to this vendor |
| Avg Amount | Average transaction amount for this vendor |
| Occurrences | How many times this vendor appeared in reports |
| Accuracy | Percentage of predictions that were confirmed |
| Status | Active or Suppressed |

## Suppressing Patterns

Sometimes you want to stop predictions for a specific vendor:

1. Find the pattern in the list
2. Toggle the **Active** switch to off
3. The pattern is now suppressed and won't generate predictions

**When to suppress:**
- Personal transactions that occasionally appear on your business card
- One-time vendors you won't use again
- Vendors with consistently wrong predictions

## Deleting Patterns

To permanently remove a pattern:

1. Click the **trash icon** in the Actions column
2. Confirm the deletion in the dialog

**Warning**: Deleting a pattern cannot be undone. If you just want to stop predictions temporarily, use suppression instead.

## Rebuilding Patterns

If your patterns seem out of date or incorrect, you can rebuild them:

1. Click the **Rebuild Patterns** button in the header
2. Wait for the process to complete
3. All patterns are recreated from your approved expense reports

**Note**: This preserves your feedback (confirms/rejects) but recalculates amounts and frequencies.

## Filtering Patterns

Use the search box to filter patterns by vendor name.

Toggle **Show Suppressed** to include or hide suppressed patterns in the list.

## Pattern Accuracy

The accuracy percentage shows how well predictions for this vendor perform:

| Accuracy | Meaning |
|----------|---------|
| 80%+ | Excellent - predictions are almost always correct |
| 50-79% | Good - most predictions are correct |
| Below 50% | Poor - consider suppressing this pattern |

Patterns with very low accuracy are automatically suppressed when:
- More than 3 rejections occur
- Confirmation rate drops below 30%

## Tips for Better Patterns

1. **Consistent vendor names**: The system normalizes vendor names, but similar naming helps
2. **Regular submissions**: Submit expense reports monthly to keep patterns current
3. **Accurate categorization**: Use the same category for the same vendor
4. **Prompt feedback**: Confirm or reject predictions while they're fresh
