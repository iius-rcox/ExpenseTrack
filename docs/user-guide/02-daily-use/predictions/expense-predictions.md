# Expense Predictions

ExpenseFlow learns from your approved expense reports to automatically identify likely business expenses in new transactions.

## How It Works

1. **Learning Phase**: When you submit expense reports, ExpenseFlow analyzes the vendors and amounts to build patterns
2. **Prediction Phase**: New imported transactions are matched against learned patterns
3. **Feedback Loop**: Your confirm/reject actions improve future predictions

## Expense Badges

Transactions that match learned patterns display an **Expense Badge**:

| Badge Color | Confidence | Meaning |
|-------------|------------|---------|
| Green (Emerald) | High (75%+) | Strong match to historical patterns |
| Amber | Medium (50-74%) | Moderate match, verify manually |

Low-confidence predictions (below 50%) are not shown to reduce noise.

## Interacting with Predictions

### Confirming a Prediction

When a transaction is correctly identified as an expense:
- Click the **checkmark (✓)** button on the badge
- The pattern's accuracy improves for future predictions

### Rejecting a Prediction

When a transaction is incorrectly identified:
- Click the **X** button on the badge
- The system learns from this feedback
- Patterns with too many rejections are automatically suppressed

## Auto-Suggested Expenses

When generating a draft expense report, high-confidence predictions are automatically included:

- Look for the **violet "Auto-suggested"** badge on expense lines
- Single-click the remove button to exclude incorrectly suggested items
- The summary shows how many items were auto-suggested vs manually added

## Managing Expense Patterns

Navigate to **Analytics > Expense Patterns** to view and manage learned patterns:

### Pattern Dashboard Features

- **View all patterns**: See vendors, categories, average amounts, and accuracy rates
- **Suppress patterns**: Toggle off patterns you don't want to generate predictions
- **Delete patterns**: Permanently remove patterns that are no longer relevant
- **Rebuild patterns**: Re-learn all patterns from your approved expense reports

### Pattern Statistics

| Metric | Description |
|--------|-------------|
| Occurrences | How many times this vendor appeared in expense reports |
| Accuracy | Percentage of predictions that were confirmed (vs rejected) |
| Avg Amount | Typical transaction amount for this vendor |
| Status | Active (generating predictions) or Suppressed |

## Best Practices

1. **Submit reports consistently** - More approved reports = better predictions
2. **Provide feedback** - Confirm or reject predictions to improve accuracy
3. **Review patterns periodically** - Suppress patterns that no longer apply
4. **Use categories** - Consistent categorization helps the system learn

## Troubleshooting

### No predictions appearing

- You need at least one submitted expense report
- Check that patterns exist in the Pattern Dashboard
- Verify the vendor names match between old and new transactions

### Too many incorrect predictions

- Reject incorrect predictions to train the system
- Patterns with high rejection rates are auto-suppressed
- Consider manually suppressing problematic vendors

### Pattern was auto-suppressed

Patterns are automatically suppressed when:
- More than 3 rejections occur
- Confirmation rate drops below 30%

You can manually re-enable suppressed patterns from the Pattern Dashboard.
