# Confidence Scores

Understand why ExpenseFlow proposes matches and how confident the AI is.

## Overview

Every [Match Proposal](../../04-reference/glossary.md#match-proposal) includes a [Confidence Score](../../04-reference/glossary.md#confidence-score) indicating how certain the AI is that the receipt and transaction belong together.

## Score Interpretation

### Score Ranges

| Score | Color | Meaning | Action |
|-------|-------|---------|--------|
| **90-100%** | Green | High confidence | Usually correct, quick verify |
| **70-89%** | Amber | Medium confidence | Review before approving |
| **50-69%** | Red | Low confidence | Careful verification needed |
| **Below 50%** | Red | Very low confidence | Likely incorrect, may reject |

### Visual Display

![Confidence score display](../../images/matching/match-confidence-factors.png)
*Caption: Confidence score with breakdown of contributing factors*

The score displays:
- **Overall percentage**: The combined confidence
- **Color indicator**: Quick visual reference
- **Factors breakdown**: What contributed to the score

## Matching Factors

The AI considers multiple factors when proposing matches:

### Amount Match

**Weight**: High

How closely the receipt and transaction amounts match:

| Match Quality | Impact |
|---------------|--------|
| Exact match | +High |
| Within $0.50 | +Medium |
| Within $5.00 | +Low |
| Large difference | Negative |

### Date Proximity

**Weight**: High

How close the transaction date is to the receipt date:

| Proximity | Impact |
|-----------|--------|
| Same day | +High |
| 1-2 days | +Medium |
| 3-7 days | +Low |
| Over 7 days | May not propose |

### Vendor Similarity

**Weight**: Medium

How well the receipt vendor matches the transaction description:

| Match Quality | Impact |
|---------------|--------|
| Exact vendor name | +High |
| Partial match | +Medium |
| No match found | Neutral |

### Payment Method

**Weight**: Low

If the receipt shows a payment method matching your card:

| Match Quality | Impact |
|---------------|--------|
| Card last 4 digits match | +Medium |
| Card type matches | +Low |
| No info available | Neutral |

### Historical Patterns

**Weight**: Medium

Based on your previous matching decisions:

| Pattern | Impact |
|---------|--------|
| You've matched this vendor before | +Medium |
| Similar transactions matched | +Low |
| New vendor/pattern | Neutral |

## Reading the Factors Breakdown

The factors breakdown shows why a match was proposed:

```
Match Factors:
✓ Amount: Exact match ($47.82)        +35%
✓ Date: Same day (Dec 15)             +30%
✓ Vendor: "Starbucks" matched         +20%
○ Payment: Card info not available     +0%
✓ History: Similar matches confirmed  +10%
───────────────────────────────────────
Total Confidence:                      95%
```

**Key**:
- **✓** = Positive factor
- **○** = Neutral (no contribution)
- **✗** = Negative factor

## Using Scores for Decisions

### High Confidence (90%+)

- **Trust level**: Usually correct
- **Review approach**: Quick visual verification
- **Action**: Approve unless something looks wrong

### Medium Confidence (70-89%)

- **Trust level**: Likely correct but uncertain
- **Review approach**: Check key details
- **Focus on**:
  - Amount matches exactly?
  - Vendor name makes sense?
  - Date is reasonable?
- **Action**: Approve if details match

### Low Confidence (Below 70%)

- **Trust level**: May be incorrect
- **Review approach**: Careful verification
- **Check**:
  - Is this actually the same purchase?
  - Could there be a better match?
  - Is the receipt readable?
- **Action**: Approve only if verified, otherwise reject

## Batch Approval Thresholds

When using batch approval, choose a threshold:

| Threshold | Matches Approved | Risk Level |
|-----------|------------------|------------|
| **95%** | Only very high confidence | Very low risk |
| **90%** | High confidence matches | Low risk |
| **85%** | Recommended default | Balanced |
| **80%** | More matches, some review needed | Moderate |
| **Below 80%** | Not recommended | Higher risk |

### Recommended Approach

1. Set threshold to **85%** or higher
2. Batch approve qualifying matches
3. Manually review remaining proposals
4. Reject or manually match as needed

## Why Scores Vary

Scores depend on:

1. **Receipt image quality**: Clear images extract better data
2. **Transaction descriptions**: Detailed descriptions match better
3. **Timing**: Uploading receipts promptly helps date matching
4. **Vendor consistency**: Established patterns improve scores

## Improving Your Scores

See [Improving Accuracy](./improving-accuracy.md) for tips on:
- Better receipt uploads
- Optimal import timing
- Training the AI through confirmations

## What's Next

After understanding confidence scores:

- [Manual Matching](./manual-matching.md) - When auto-matching doesn't work
- [Improving Accuracy](./improving-accuracy.md) - Make matching better
- [Review Modes](./review-modes.md) - Process proposals efficiently
