# Review Modes

← [Back to Daily Use](../README.md) | [Matching Section](./confidence-scores.md)

Choose how to review match proposals between receipts and transactions.

## Overview

ExpenseFlow offers two modes for reviewing [Match Proposals](../../04-reference/glossary.md#match-proposal): Review Mode for focused one-at-a-time processing, and List Mode for scanning multiple proposals at once.

## Review Mode

The default keyboard-driven interface for efficient match review.

![Review mode split-pane](../../images/matching/review-mode-split-pane.png)
*Caption: Review Mode showing receipt image on left, transaction details on right*

### Layout

The screen is split into two panes:
- **Left pane**: Receipt image with zoom controls
- **Right pane**: Transaction details and matching factors

### How It Works

1. One match proposal displays at a time
2. Review the receipt image and transaction
3. Press **A** to approve or **R** to reject
4. The next proposal loads automatically

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| **A** | Approve match |
| **R** | Reject match |
| **J** or **↓** | Skip to next (without action) |
| **K** or **↑** | Go to previous |
| **M** | Open manual match dialog |
| **?** | Show keyboard shortcuts help |
| **Esc** | Close help or exit mode |

See [Keyboard Shortcuts](../keyboard-shortcuts.md) for the complete list.

### Advantages

- **Focused**: One proposal at a time prevents distraction
- **Fast**: Keyboard shortcuts enable rapid processing
- **Detailed**: Full-size receipt image for verification
- **Efficient**: Flow from one proposal to the next automatically

### Best For

- Processing many proposals quickly
- Power users comfortable with keyboard shortcuts
- Careful review of each match
- Learning how matching works

## List Mode

A card-based view showing all proposals at once.

### Layout

- Grid of match proposal cards
- Each card shows:
  - Receipt thumbnail
  - Transaction summary
  - Confidence score
  - Quick action buttons

### How It Works

1. All proposals visible on screen
2. Scroll to browse through proposals
3. Click action buttons on individual cards
4. Or select multiple for bulk actions

### Action Buttons

Each card has:
- **✓** (Approve): Confirm the match
- **✗** (Reject): Decline the match
- **...** (More): Additional options

### Advantages

- **Overview**: See all pending matches at once
- **Comparison**: Compare multiple proposals visually
- **Scanning**: Quick visual identification of issues
- **Bulk**: Select multiple for batch approval

### Best For

- Getting an overview of pending work
- Visual comparison of similar matches
- Users who prefer mouse over keyboard
- Bulk approval with threshold

## Switching Between Modes

Toggle between modes:

1. Look for the **View** toggle at the top of the Matching page
2. Click **Review** or **List** to switch
3. Your position is preserved when switching

## Batch Approval

Available in both modes, batch approval lets you approve multiple matches at once:

1. Click **Batch Approve**
2. Set a confidence threshold (e.g., 85%)
3. Preview how many matches qualify
4. Click **Approve All**
5. All matches at or above the threshold are confirmed

See [Confidence Scores](./confidence-scores.md) to understand threshold selection.

## Choosing Your Mode

| Scenario | Recommended Mode |
|----------|------------------|
| First time reviewing matches | Review Mode |
| Learning the system | Review Mode |
| Many similar matches | List Mode + Batch |
| High-value transactions | Review Mode |
| Quick daily check | List Mode |
| Detailed verification needed | Review Mode |

## Tips for Efficient Review

### In Review Mode

- Keep one hand on A/R keys
- Use J/K to skip uncertain matches
- Handle skipped items later with manual match

### In List Mode

- Sort by confidence (highest first)
- Use batch approval for high-confidence matches
- Manually review low-confidence items

### General

- Process daily to prevent backlog
- Set aside dedicated review time
- Trust high-confidence matches
- Take extra care with large amounts

## What's Next

After understanding review modes:

- [Confidence Scores](./confidence-scores.md) - Interpret match quality
- [Manual Matching](./manual-matching.md) - Create matches yourself
- [Improving Accuracy](./improving-accuracy.md) - Train the AI
