# Quickstart: ExpenseFlow End User Guide

**Feature**: 018-end-user-guide
**Date**: 2025-12-30
**Purpose**: Writing guide for documentation authors implementing the user guide

## Prerequisites

Before starting implementation:

1. **Understand the specification**: Read [spec.md](./spec.md) for complete requirements
2. **Review the plan**: See [plan.md](./plan.md) for document structure
3. **Check research decisions**: See [research.md](./research.md) for style guide
4. **Know the hierarchy**: See [data-model.md](./data-model.md) for cross-references

## Writing Principles

### Voice and Tone

| Do | Don't |
|----|-------|
| "You can upload receipts by..." | "Users can upload receipts by..." |
| "Click the **Upload** button" | "Click the upload button" |
| "Your receipt appears in the queue" | "The receipt will appear in the queue" |
| "If the upload fails, try again" | "In case of upload failure, retry the operation" |

### Structure Every Page

```markdown
# [Page Title]

[1-2 sentence overview of what this page covers]

## Overview

[Brief explanation - what is this feature and why use it]

## [Primary Action]

[Numbered steps for the main task]

1. Navigate to **[Location]**
2. Click the **[Button]** button
3. [Next action]

![Description](../images/section/screenshot-name.png)
*Caption: What the screenshot shows*

> **Tip**: [Helpful hint for better results]

## [Secondary Topics] (if applicable)

[Additional content organized by subtask]

> **Warning**: [Important caution if applicable]

## What's Next

- [Related topic 1](./related-1.md) - Brief description
- [Related topic 2](../other-section/related-2.md) - Brief description
```

### Screenshot Standards

1. **Capture at 1200px width** for desktop views
2. **Use numbered callouts** (1, 2, 3) pointing to key UI elements
3. **Write descriptive alt text** for accessibility
4. **Include captions** explaining what the screenshot shows
5. **Save to appropriate subfolder** in `images/`

Example:
```markdown
![Dashboard showing action queue with 3 pending items](../images/dashboard/action-queue-detail.png)
*Caption: The action queue (1) shows pending tasks. Click any item (2) to take action.*
```

### Glossary Term Usage

On **first use** in each document:
- Bold the term: **Match Proposal**
- Link to glossary: [Match Proposal](../04-reference/glossary.md#match-proposal)

Subsequent uses in the same document:
- No bold, no link: "Review the match proposal to confirm the link."

### Cross-References

Use relative paths for all internal links:

| From | To | Link |
|------|------|------|
| `01-getting-started/signing-in.md` | `01-getting-started/dashboard-overview.md` | `[Dashboard](./dashboard-overview.md)` |
| `02-daily-use/receipts/uploading.md` | `02-daily-use/matching/review-modes.md` | `[Review Mode](../matching/review-modes.md)` |
| `02-daily-use/matching/manual-matching.md` | `04-reference/troubleshooting.md` | `[Troubleshooting](../../04-reference/troubleshooting.md)` |

## Implementation Order

### Phase 1: Foundation (Do First)

1. **Create directory structure**
   ```bash
   mkdir -p docs/user-guide/{01-getting-started,02-daily-use/{receipts,statements,transactions,matching,splitting,categorization},03-monthly-close/{reports,analytics,subscriptions,travel},04-reference,images/{dashboard,receipts,statements,matching,reports,analytics,mobile}}
   ```

2. **Write root README.md**
   - Table of contents
   - Quick links to common tasks
   - Welcome message

3. **Write section README.md files**
   - Brief intro to each section
   - Links to all pages in section

### Phase 2: Getting Started Section

1. `signing-in.md` - Microsoft SSO flow
2. `dashboard-overview.md` - Components explanation
3. `quick-start.md` - First receipt upload walkthrough

**Why first**: These establish foundation for all other content.

### Phase 3: Daily Use Section (Core Workflows)

**Receipts** (do first - independent entry point):
1. `uploading.md`
2. `ai-extraction.md`
3. `image-viewer.md`

**Statements** (do second - independent entry point):
1. `importing.md`
2. `column-mapping.md`
3. `fingerprints.md`

**Transactions** (depends on receipts OR statements):
1. `filtering.md`
2. `bulk-operations.md`
3. `sorting.md`

**Matching** (depends on receipts AND statements):
1. `review-modes.md`
2. `confidence-scores.md`
3. `manual-matching.md`
4. `improving-accuracy.md`

**Supporting topics**:
1. `splitting/expense-splitting.md`
2. `splitting/split-patterns.md`
3. `categorization/gl-suggestions.md`
4. `keyboard-shortcuts.md`

### Phase 4: Monthly Close Section

**Reports**:
1. `generating.md`
2. `status-workflow.md`
3. `exporting.md`

**Analytics**:
1. `dashboard.md`
2. `trends.md`
3. `categories.md`

**Subscriptions**:
1. `detection.md`
2. `alerts.md`
3. `manual-entry.md`

**Travel**:
1. `periods.md`
2. `timeline.md`

### Phase 5: Reference Section

1. `glossary.md` - All 25 terms
2. `troubleshooting.md` - All 12 issues
3. `settings.md` - All settings options
4. `mobile.md` - Mobile-specific features
5. `keyboard-shortcuts.md` - Quick reference card

### Phase 6: Screenshots

Capture screenshots **after** writing content:
1. Identify all `![...]` placeholders
2. Navigate to feature in staging environment
3. Capture at correct dimensions
4. Add callout annotations
5. Save with correct naming convention

## Quality Checklist

Before marking any page complete:

- [ ] Title matches filename (e.g., `uploading.md` → "# Uploading Receipts")
- [ ] Overview present and under 100 words
- [ ] Steps are numbered (not bulleted)
- [ ] UI elements are **bold**
- [ ] Screenshots have alt text and captions
- [ ] All internal links resolve
- [ ] "What's Next" section has 2-3 links
- [ ] Glossary terms linked on first use
- [ ] No passive voice in instructions
- [ ] Tested navigation flow works

## Common Patterns

### Describing a Multi-Step Wizard

```markdown
## Importing a Statement

The import wizard guides you through three steps:

### Step 1: Upload File

1. Click **Import Statement** on the Statements page
2. Drag your CSV or Excel file to the upload zone
3. Wait for the file to upload

![Upload zone accepting a CSV file](../images/statements/import-upload-zone.png)
*Caption: Drag your statement file to the highlighted area*

### Step 2: Map Columns

1. Review the detected columns
2. Match each column to the correct field:
   - **Date**: Transaction date
   - **Description**: Merchant or memo
   - **Amount**: Transaction amount
3. Click **Continue**

> **Tip**: Save your mapping as a fingerprint to reuse next time.

### Step 3: Confirm Import

1. Review the sample transactions
2. Verify the data looks correct
3. Click **Import**
```

### Explaining a UI Component

```markdown
## Understanding the Action Queue

The **Action Queue** appears on your dashboard and shows tasks that need your attention.

![Action queue showing three pending items](../images/dashboard/action-queue-overview.png)
*Caption: The action queue prioritizes your pending tasks*

Each item shows:
- **Icon** (1): Type of action (receipt, match, report)
- **Title** (2): Brief description of what needs attention
- **Time** (3): When the item was added
- **Action button** (4): Quick action without leaving the dashboard

Click any item to navigate directly to that task.
```

### Troubleshooting Format

```markdown
### Upload Fails with "File Too Large"

**Symptoms**: You see an error message when uploading a receipt.

**Cause**: The file exceeds the 25MB size limit.

**Solution**:
1. Compress the image using your phone's photo editor
2. If it's a PDF, split it into separate pages
3. Try uploading again

**Prevention**: Enable "Optimize photos" in your camera settings before capturing receipts.
```

## File Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Pages | `kebab-case.md` | `ai-extraction.md` |
| Screenshots | `{feature}-{description}.png` | `upload-drag-drop-zone.png` |
| Directories | `kebab-case` or `nn-section-name` | `02-daily-use`, `receipts` |

## Testing Your Documentation

1. **Link validation**: Use a Markdown linter to check for broken links
2. **Screenshot verification**: Confirm all referenced images exist
3. **Navigation test**: Follow "What's Next" links through entire workflow
4. **Glossary coverage**: Verify all 25 terms are defined and linked
5. **FR coverage**: Cross-reference with spec.md to ensure all requirements are met

## Getting Help

- **Specification questions**: Review [spec.md](./spec.md)
- **Structure questions**: Review [data-model.md](./data-model.md)
- **Style questions**: Review [research.md](./research.md)
- **Feature behavior**: Test in staging environment or review frontend code
