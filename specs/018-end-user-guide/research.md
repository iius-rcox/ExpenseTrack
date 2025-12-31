# Research: ExpenseFlow End User Guide

**Feature**: 018-end-user-guide
**Date**: 2025-12-30
**Purpose**: Resolve unknowns and establish documentation standards before implementation

## 1. Document Structure and Section Ordering

### Decision: Workflow-Based Sequential Organization

**Structure**: Getting Started → Daily Use → Monthly Close → Reference

**Rationale**:
- Matches natural user adoption journey (learn basics → use daily → close month → look up info)
- Clarified in specification (Session 2025-12-30)
- Each section is self-contained for users who enter at different points

**Alternatives Considered**:
- **Task-based** ("How do I..."): Rejected - harder to build mental model of system
- **Feature-based** (by app area): Rejected - doesn't reflect actual workflow
- **Hybrid**: Rejected - adds complexity without clear benefit

### Section Dependencies

```
01-getting-started/
├── signing-in.md           # Prerequisite for everything
├── dashboard-overview.md   # Depends on: signing-in
└── quick-start.md          # Depends on: signing-in, dashboard-overview

02-daily-use/
├── receipts/               # Independent entry point
├── statements/             # Independent entry point
├── transactions/           # Depends on: receipts OR statements
├── matching/               # Depends on: receipts AND statements
├── splitting/              # Depends on: transactions
├── categorization/         # Depends on: transactions
└── keyboard-shortcuts.md   # Independent (reference)

03-monthly-close/
├── reports/                # Depends on: matching
├── analytics/              # Depends on: transactions (data needed)
├── subscriptions/          # Depends on: transactions (detection source)
└── travel/                 # Depends on: receipts

04-reference/
├── troubleshooting.md      # Independent (lookup)
├── settings.md             # Independent (lookup)
├── mobile.md               # Independent (lookup)
├── glossary.md             # Independent (lookup)
└── keyboard-shortcuts.md   # Independent (quick reference)
```

## 2. Screenshot Requirements

### Decision: Annotated Screenshots with Numbered Callouts

**Standard**: Each screenshot includes:
1. Numbered callouts (1, 2, 3...) pointing to UI elements
2. Brief caption explaining what's shown
3. Consistent dimensions (1200px width max for desktop, 400px for mobile)

**Rationale**:
- Clarified in specification (Session 2025-12-30)
- Callouts prevent users from searching for elements
- Consistent sizing ensures readability across devices

### Screenshot Inventory by Section

| Section | Screenshots Required | Priority |
|---------|---------------------|----------|
| **01-getting-started** | | |
| signing-in.md | Sign-in screen, MFA prompt (if applicable) | High |
| dashboard-overview.md | Full dashboard, action queue detail, activity feed detail, metrics row | High |
| quick-start.md | Receipt upload dialog, processing status, extracted data view | High |
| **02-daily-use/receipts** | | |
| uploading.md | Drag-drop zone, file browser, camera button (mobile) | High |
| ai-extraction.md | Extracted fields with confidence colors, edit mode | High |
| image-viewer.md | Zoom controls, rotate button, fullscreen mode | Medium |
| **02-daily-use/statements** | | |
| importing.md | Upload step, progress indicator | High |
| column-mapping.md | Column mapper with sample data, dropdown selections | High |
| fingerprints.md | Save template dialog, template selection dropdown | Medium |
| **02-daily-use/transactions** | | |
| filtering.md | Filter panel expanded, date picker, category multi-select | High |
| bulk-operations.md | Selection checkboxes, floating action bar | High |
| sorting.md | Column headers with sort indicators | Low |
| **02-daily-use/matching** | | |
| review-modes.md | Review mode split-pane, list mode cards | High |
| confidence-scores.md | Match proposal with factors breakdown | High |
| manual-matching.md | Manual match dialog with search | Medium |
| improving-accuracy.md | Confirmation feedback (toast), matching factors highlight | Medium |
| **02-daily-use/splitting** | | |
| expense-splitting.md | Split allocation editor, percentage inputs | Medium |
| split-patterns.md | Pattern save dialog, pattern selection | Medium |
| **02-daily-use/categorization** | | |
| gl-suggestions.md | GL code suggestions with confidence, skip button | Medium |
| **02-daily-use** | | |
| keyboard-shortcuts.md | Shortcuts help panel overlay | Low |
| **03-monthly-close/reports** | | |
| generating.md | Report generation dialog, month selector | High |
| status-workflow.md | Report cards showing different statuses | Medium |
| exporting.md | Export dropdown (PDF/Excel options) | Low |
| **03-monthly-close/analytics** | | |
| dashboard.md | Full analytics view with all tabs | High |
| trends.md | Trend chart with date range selector | Medium |
| categories.md | Category breakdown chart (pie/bar) | Medium |
| **03-monthly-close/subscriptions** | | |
| detection.md | Subscriptions tab with detected items | Medium |
| alerts.md | Alert badge, acknowledge button | Medium |
| manual-entry.md | Create subscription dialog | Low |
| **03-monthly-close/travel** | | |
| periods.md | Travel period creation dialog | Low |
| timeline.md | Timeline view with linked expenses | Low |
| **04-reference** | | |
| troubleshooting.md | Error toast examples, retry button | Medium |
| settings.md | Settings page with all sections visible | High |
| mobile.md | Mobile dashboard, bottom nav, swipe action | High |

**Total Screenshots**: ~45 annotated screenshots required

### Screenshot Naming Convention

```
images/{section}/{feature}-{description}.png

Examples:
images/dashboard/dashboard-full-view.png
images/receipts/upload-drag-drop-zone.png
images/matching/review-mode-split-pane.png
images/mobile/bottom-nav-with-badges.png
```

## 3. Glossary Term Candidates

### Decision: 25 Core Terms

Based on specification requirements (FR-025: minimum 20 terms) and feature exploration:

| Term | Definition Summary |
|------|-------------------|
| **Receipt** | A digital image or PDF of a purchase document uploaded for expense tracking |
| **Transaction** | A financial record imported from a bank/credit card statement |
| **Match** | A confirmed link between a receipt and a transaction |
| **Match Proposal** | An AI-suggested link between a receipt and transaction awaiting user confirmation |
| **Confidence Score** | A percentage (0-100%) indicating AI certainty about extracted data or match quality |
| **Fingerprint** | A saved column mapping template for repeated statement imports from the same source |
| **Split Pattern** | A reusable allocation template for dividing expenses across multiple cost centers |
| **Expense Report** | A collection of matched expenses grouped for submission and approval |
| **GL Code** | General Ledger account code used for expense categorization and accounting |
| **Department** | An organizational unit synced from Vista ERP for expense allocation |
| **Project** | A work assignment synced from Vista ERP for expense allocation |
| **Cost Center** | A department or project to which expenses are allocated |
| **Subscription** | A recurring charge detected or manually tracked in the system |
| **Subscription Alert** | A notification about missing expected charges or unexpected new subscriptions |
| **Travel Period** | A defined date range grouping expenses from a business trip |
| **AI Extraction** | The automated process of reading receipt images to identify vendor, date, and amount |
| **Column Mapping** | The process of assigning statement file columns to standard transaction fields |
| **Batch Approval** | Confirming multiple match proposals at once using a confidence threshold |
| **Action Queue** | A prioritized list of pending tasks shown on the dashboard |
| **Activity Feed** | A chronological list of recent expense events (uploads, matches, reports) |
| **Review Mode** | A keyboard-driven interface for efficiently reviewing match proposals |
| **List Mode** | A card-based interface showing all match proposals at once |
| **Swipe Action** | A mobile gesture revealing action buttons (approve, reject, edit) |
| **Undo Stack** | The history of field edits allowing reversal of corrections |
| **Vista ERP** | Viewpoint Vista, the enterprise system providing reference data |

## 4. Troubleshooting Entry Candidates

### Decision: 12 Common Issues

Based on edge cases in specification and real user pain points:

| Issue | Category | Resolution Summary |
|-------|----------|-------------------|
| **Upload fails with "file too large"** | Receipts | Max size is 25MB. Compress image or split multi-page PDFs |
| **Upload fails with "unsupported format"** | Receipts | Accepted: JPEG, PNG, HEIC, PDF. Convert other formats first |
| **Receipt stuck in "Processing"** | Receipts | Wait 2-3 minutes. If still stuck, use Retry button |
| **AI extraction shows all fields as low confidence** | Receipts | Re-upload clearer image or manually enter data |
| **Statement import shows "Invalid CSV"** | Statements | Check encoding (UTF-8), ensure headers present, verify delimiter |
| **Column mapping doesn't show my columns** | Statements | Scroll dropdown, check if file has consistent structure |
| **Duplicate transactions imported** | Statements | System auto-detects duplicates. If missed, delete manually |
| **No match proposals appearing** | Matching | Run "Auto-Match" button. Check that both receipts and transactions exist |
| **Match rejected by accident** | Matching | Use Manual Match to create the correct link |
| **Split allocations won't save** | Splitting | Percentages must sum to exactly 100% |
| **Session expired during upload** | General | Sign in again. Partially uploaded files may need re-upload |
| **Network error during operation** | General | Check connection and retry. Most operations are resumable |

## 5. Writing Standards

### Decision: Concise, Action-Oriented Style

**Voice**: Second person ("You can...", "Click the...")
**Tone**: Helpful, professional, not condescending
**Structure**:
- Lead with the user goal
- Number sequential steps
- Use callouts for warnings/tips
- End with "What's Next" links

**Example Format**:

```markdown
## Uploading Receipts

You can upload receipts by dragging files or using your camera.

### Drag and Drop

1. Navigate to the **Receipts** page
2. Drag your receipt image onto the upload zone
3. Wait for the processing indicator to complete
4. Review the extracted data

> **Tip**: Upload multiple files at once by selecting them together.

### What's Next
- [Understanding AI Extraction](./ai-extraction.md)
- [Matching Receipts to Transactions](../matching/review-modes.md)
```

## 6. Cross-Reference Strategy

### Decision: Relative Links with "What's Next" Sections

**Internal Links**: Use relative Markdown links (`[text](../path/file.md)`)
**What's Next**: Each page ends with 2-3 relevant next steps
**See Also**: Include related topics when helpful

**Rationale**:
- Relative links work in GitHub and local Markdown viewers
- "What's Next" guides users through the workflow naturally
- Avoids broken links when directory structure changes

## Summary

| Research Area | Decision | Key Outputs |
|---------------|----------|-------------|
| Structure | Workflow-based (4 sections) | Section dependency map |
| Screenshots | Annotated with callouts | 45 screenshots, naming convention |
| Glossary | 25 core terms | Term definitions |
| Troubleshooting | 12 common issues | Issue-resolution pairs |
| Writing | Action-oriented, 2nd person | Style guide, example format |
| Cross-references | Relative links + "What's Next" | Navigation pattern |
