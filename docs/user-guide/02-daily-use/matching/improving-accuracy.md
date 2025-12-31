# Improving Accuracy

Help ExpenseFlow learn to make better match proposals.

## Overview

ExpenseFlow's AI learns from your matching decisions. Every time you confirm or reject a match, you're training the system to make better proposals in the future.

## How Learning Works

### What the AI Learns

When you **approve** a match:
- The AI notes successful patterns
- Vendor name associations strengthen
- Amount and date proximity patterns are reinforced
- Future similar matches score higher

When you **reject** a match:
- The AI notes unsuccessful patterns
- That specific pairing won't be proposed again
- Similar incorrect patterns are downweighted
- Future proposals adjust accordingly

### Learning Scope

| Your Action | Impact |
|-------------|--------|
| Confirm high-confidence match | Reinforces existing patterns |
| Confirm low-confidence match | Teaches new patterns |
| Reject incorrect match | Prevents future mistakes |
| Manual match | Creates new pattern associations |

## Best Practices for Training

### Be Consistent

Use the same vendor names when possible:
- If you correct "STRBCKS" to "Starbucks", do it consistently
- The AI learns your preferred naming conventions
- Consistency speeds up learning

### Confirm Good Matches

Don't skip high-confidence matches:
- Approving them reinforces correct patterns
- More confirmations = better future accuracy
- Quick approvals are still valuable training

### Reject Clearly Wrong Matches

Rejections are just as valuable:
- They prevent repeated mistakes
- The AI learns what NOT to match
- Be decisive with incorrect proposals

### Use Manual Matching Wisely

When you manually match:
- The AI creates a new pattern
- Similar future pairs will score higher
- Especially helpful for unusual vendors

## Factors You Can Influence

### Receipt Quality

Better receipts = better extraction = better matching:

- **Clear photos**: Good lighting, no blur
- **Complete images**: Include all edges
- **Legible text**: Avoid faded receipts
- **Standard formats**: Retail receipts extract best

### Upload Timing

Upload receipts promptly:

- Same day: Best for date matching
- Within a week: Good matching
- Later: May need manual matching for date gaps

### Statement Import Frequency

Regular imports improve matching:

- Weekly imports: Transactions arrive while receipts are recent
- Monthly imports: Larger date gaps, more manual work
- Consistent schedule: Predictable matching workflow

## Understanding What Helps

### High-Impact Actions

| Action | Impact on Learning |
|--------|-------------------|
| Confirming matches with vendor name corrections | High |
| Rejecting clearly wrong matches | High |
| Manual matching for unusual vendors | High |
| Consistent vendor naming | High |

### Neutral Actions

| Action | Impact on Learning |
|--------|-------------------|
| Skipping matches (neither approve nor reject) | None |
| Deleting receipts or transactions | None |
| Changing filters or views | None |

## Monitoring Improvement

### Signs of Better Accuracy

- Higher average confidence scores over time
- Fewer manual matches needed
- More "exact vendor" matches
- Fewer rejected proposals

### When Improvement Stalls

If accuracy doesn't improve:

1. Review your rejection patterns - are you being consistent?
2. Check receipt quality - are uploads clear?
3. Verify vendor corrections - are they consistent?
4. Consider timing - are uploads and imports aligned?

## Tips by Scenario

### New Vendor

First time matching a vendor:
1. Carefully verify the first match
2. Correct vendor name if needed
3. Approve to establish the pattern
4. Future matches will be easier

### Recurring Vendor

For regularly-used vendors:
1. Initial matches may need correction
2. After a few approvals, confidence increases
3. Eventually becomes nearly automatic

### Multiple Locations

Same vendor, different locations (e.g., Starbucks #1234 vs #5678):
1. Correct to common name if desired
2. The AI learns to generalize
3. Or keep specific names if you need to distinguish

## What's Next

After understanding how to improve accuracy:

- [Confidence Scores](./confidence-scores.md) - See how factors combine
- [Review Modes](./review-modes.md) - Efficient proposal processing
- [Manual Matching](./manual-matching.md) - Handle edge cases
