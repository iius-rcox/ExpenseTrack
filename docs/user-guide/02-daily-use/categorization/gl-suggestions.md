# GL Code Suggestions

← [Back to Daily Use](../README.md) | Related: [Bulk Operations](../transactions/bulk-operations.md)

Understand and respond to AI-powered categorization suggestions.

## Overview

ExpenseFlow suggests [GL codes](../../04-reference/glossary.md#gl-code) (General Ledger codes) for your expenses based on vendor, amount, and historical patterns. This guide explains how to review, confirm, modify, or skip these suggestions.

## How Suggestions Work

### AI Analysis

For each expense, the AI considers:

| Factor | Weight | Example |
|--------|--------|---------|
| **Vendor pattern** | High | "Starbucks" → Meals & Entertainment |
| **Amount range** | Medium | $500+ → May not be meals |
| **Historical choices** | High | Your past categorizations |
| **Company standards** | Medium | Default GL mappings |

### Suggestion Confidence

Like match proposals, categorization suggestions have confidence levels:

| Level | Meaning |
|-------|---------|
| **High (90%+)** | Strong pattern match, likely correct |
| **Medium (70-89%)** | Good guess, worth reviewing |
| **Low (<70%)** | Uncertain, needs your input |

## Viewing Suggestions

### In Action Queue

Categorization suggestions appear in your [Action Queue](../../01-getting-started/dashboard-overview.md#action-queue):

- Category icon indicates categorization needed
- Priority based on expense age and amount
- Click to review suggestion

### In Transaction List

Uncategorized transactions show:

- "Needs Category" indicator
- Suggested GL code (if available)
- One-click accept option

![GL suggestions panel](../../images/categorization/gl-suggestions.png)
*Caption: GL code suggestion with confidence and alternatives*

### Suggestion Panel Contents

Each suggestion shows:

| Element | Description |
|---------|-------------|
| **Suggested GL** | Recommended code and name |
| **Confidence** | How certain the AI is |
| **Reason** | Why this was suggested |
| **Alternatives** | Other possible codes |
| **Actions** | Accept, Modify, Skip |

## Responding to Suggestions

### Accept Suggestion

If the suggestion is correct:

1. Click **Accept** or **Confirm**
2. GL code is applied immediately
3. Next suggestion appears (if reviewing queue)

**Keyboard shortcut**: `A` in review mode

### Modify Suggestion

If the suggestion is close but wrong:

1. Click **Modify** or the GL dropdown
2. Search or browse for correct code
3. Select the right GL code
4. Click **Save** or **Apply**

**Tips for finding codes**:
- Type GL code number directly
- Search by category name
- Browse by category hierarchy
- View recently used codes

### Skip Suggestion

If you need more information:

1. Click **Skip** or **Later**
2. Expense remains uncategorized
3. Returns to queue for future review

**When to skip**:
- Need to check receipt
- Need to consult with finance
- Unsure about proper categorization

![Skip workflow](../../images/categorization/skip-workflow.png)
*Caption: Skip option for deferred categorization*

### Reject Suggestion

If the suggestion is completely wrong:

1. Click **Reject** or **Wrong**
2. AI learns to avoid this pattern
3. You're prompted to select correct code

## Bulk Categorization

### Selecting Multiple

For similar expenses:

1. Select multiple transactions
2. Click **Categorize Selected**
3. Choose a GL code
4. Apply to all selected

### Filter-Based Categorization

Efficient for patterns:

1. Filter transactions (by vendor, date, etc.)
2. Select all filtered results
3. Apply category to batch

## Understanding GL Codes

### Code Structure

GL codes typically follow a pattern:

```
XXXX-YY-ZZZ
│    │   └── Sub-account
│    └────── Department
└─────────── Main category
```

### Common Categories

| Category | Typical Expenses |
|----------|------------------|
| **Travel** | Flights, hotels, mileage |
| **Meals** | Business meals, per diem |
| **Office** | Supplies, equipment |
| **Software** | Subscriptions, licenses |
| **Professional** | Consulting, legal fees |

### Category Hierarchy

Navigate the hierarchy:

1. Expand main categories
2. Drill into sub-categories
3. Select most specific code
4. More specific = better reporting

## Learning from Your Choices

### How AI Learns

Your categorization decisions train the AI:

- **Confirmations** reinforce patterns
- **Modifications** teach corrections
- **Vendor-specific patterns** develop over time

### Improving Suggestions

Help the AI learn:

| Action | Impact |
|--------|--------|
| **Be consistent** | Same vendor → same category |
| **Be specific** | Use most specific GL code available |
| **Review carefully** | Don't accept incorrect suggestions |
| **Modify thoughtfully** | Select the truly correct code |

## Default Categories

### Setting Defaults

Pre-set categories for known vendors:

1. Go to **Settings** → **Category Defaults**
2. Add vendor → category mappings
3. Future expenses auto-categorize

### Department Defaults

If your department has standards:

- Check for company-wide mappings
- Your defaults override company defaults
- Per-vendor settings override all

## Troubleshooting

### "No Suggestion Available"

If AI has no suggestion:

1. New or unusual vendor
2. Ambiguous expense type
3. Not enough history

→ Manually select a category

### "Suggestion Seems Wrong"

If suggestions are consistently incorrect:

1. Modify to correct code
2. AI learns from your correction
3. Check for similar vendor name confusion

### "Can't Find Right GL Code"

If the code you need doesn't appear:

1. Contact finance/admin
2. Code may need to be added
3. Use closest available temporarily

## Best Practices

### Review Regularly

- Process categorization queue daily
- Don't let uncategorized expenses accumulate
- Batch similar items for efficiency

### Be Precise

- Use the most specific code available
- Don't default to generic categories
- Proper categorization improves reporting

### Train the AI

- Consistent choices improve suggestions
- Spend time early to save time later
- Review and correct mistakes promptly

## What's Next

After understanding categorization:

- [Filtering Transactions](../transactions/filtering.md) - Find uncategorized items
- [Bulk Operations](../transactions/bulk-operations.md) - Categorize in batches
- [Analytics Dashboard](../../03-monthly-close/analytics/dashboard.md) - See category breakdown

