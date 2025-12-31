# Data Model: ExpenseFlow End User Guide

**Feature**: 018-end-user-guide
**Date**: 2025-12-30
**Purpose**: Define document hierarchy, section structure, and content mapping

## Document Hierarchy

For a documentation project, the "data model" represents the document structure rather than database entities.

```
docs/user-guide/
├── README.md                          # Root navigation hub
│
├── 01-getting-started/                # PHASE: Onboarding
│   ├── README.md                      # Section intro + navigation
│   ├── signing-in.md                  # US-1
│   ├── dashboard-overview.md          # US-1, US-10
│   └── quick-start.md                 # US-1
│
├── 02-daily-use/                      # PHASE: Regular workflows
│   ├── README.md                      # Section intro + navigation
│   ├── receipts/
│   │   ├── uploading.md               # US-2: FR-002
│   │   ├── ai-extraction.md           # US-2: FR-003, FR-004
│   │   └── image-viewer.md            # US-11: FR-034
│   ├── statements/
│   │   ├── importing.md               # US-3: FR-005
│   │   ├── column-mapping.md          # US-3: FR-006, FR-007
│   │   └── fingerprints.md            # US-3: FR-008
│   ├── transactions/
│   │   ├── filtering.md               # US-3: FR-009
│   │   ├── bulk-operations.md         # US-3: FR-010
│   │   └── sorting.md                 # FR-038
│   ├── matching/
│   │   ├── review-modes.md            # US-4: FR-012
│   │   ├── confidence-scores.md       # US-4: FR-011
│   │   ├── manual-matching.md         # US-4: FR-014
│   │   └── improving-accuracy.md      # US-4: FR-015, FR-016
│   ├── splitting/
│   │   ├── expense-splitting.md       # US-7: FR-028
│   │   └── split-patterns.md          # US-7: FR-028
│   ├── categorization/
│   │   └── gl-suggestions.md          # US-12: FR-036
│   └── keyboard-shortcuts.md          # US-11: FR-033
│
├── 03-monthly-close/                  # PHASE: Periodic tasks
│   ├── README.md                      # Section intro + navigation
│   ├── reports/
│   │   ├── generating.md              # US-5: FR-017
│   │   ├── status-workflow.md         # US-5: FR-018
│   │   └── exporting.md               # US-5: FR-019
│   ├── analytics/
│   │   ├── dashboard.md               # US-6: FR-020
│   │   ├── trends.md                  # US-6: FR-021
│   │   └── categories.md              # US-6: FR-020
│   ├── subscriptions/
│   │   ├── detection.md               # US-8: FR-022, FR-029
│   │   ├── alerts.md                  # US-8: FR-029
│   │   └── manual-entry.md            # US-8: FR-029
│   └── travel/
│       ├── periods.md                 # US-9: FR-030
│       └── timeline.md                # US-9: FR-030
│
├── 04-reference/                      # PHASE: Lookup resources
│   ├── README.md                      # Section intro + navigation
│   ├── troubleshooting.md             # US-13: FR-026
│   ├── settings.md                    # US-14: FR-023
│   ├── mobile.md                      # US-15: FR-024, FR-037
│   ├── glossary.md                    # FR-025
│   └── keyboard-shortcuts.md          # FR-033 (quick reference card)
│
└── images/                            # Screenshot assets
    ├── dashboard/
    ├── receipts/
    ├── statements/
    ├── matching/
    ├── reports/
    ├── analytics/
    └── mobile/
```

## Section Definitions

### Root: README.md

**Purpose**: Primary entry point and navigation hub
**Content**:
- Welcome message
- Table of contents with descriptions
- Quick links to common tasks
- Version/last updated info

**Template**:
```markdown
# ExpenseFlow User Guide

Welcome to ExpenseFlow, your AI-powered expense management system.

## Quick Links
- [Upload your first receipt](01-getting-started/quick-start.md)
- [Import a bank statement](02-daily-use/statements/importing.md)
- [Generate an expense report](03-monthly-close/reports/generating.md)

## Contents

### [Getting Started](01-getting-started/README.md)
Set up your account and learn the basics.

### [Daily Use](02-daily-use/README.md)
Upload receipts, import statements, and manage expenses.

### [Monthly Close](03-monthly-close/README.md)
Generate reports, analyze spending, and track subscriptions.

### [Reference](04-reference/README.md)
Troubleshooting, settings, and glossary.
```

---

### Section: 01-getting-started

| File | Purpose | User Stories | FRs | Screenshots |
|------|---------|--------------|-----|-------------|
| README.md | Section overview | - | - | None |
| signing-in.md | Microsoft SSO authentication | US-1 | FR-001 | Sign-in screen |
| dashboard-overview.md | Dashboard components explained | US-1, US-10 | FR-001, FR-032 | Dashboard full, action queue, activity feed |
| quick-start.md | First receipt upload walkthrough | US-1 | FR-001, FR-002 | Upload dialog, processing, extracted data |

**Section Prerequisites**: None (entry point)
**Estimated Reading Time**: 10-15 minutes

---

### Section: 02-daily-use

| Subsection | Files | User Stories | FRs |
|------------|-------|--------------|-----|
| receipts/ | uploading.md, ai-extraction.md, image-viewer.md | US-2, US-11 | FR-002, FR-003, FR-004, FR-034, FR-035 |
| statements/ | importing.md, column-mapping.md, fingerprints.md | US-3 | FR-005, FR-006, FR-007, FR-008 |
| transactions/ | filtering.md, bulk-operations.md, sorting.md | US-3 | FR-009, FR-010, FR-038 |
| matching/ | review-modes.md, confidence-scores.md, manual-matching.md, improving-accuracy.md | US-4 | FR-011, FR-012, FR-013, FR-014, FR-015, FR-016 |
| splitting/ | expense-splitting.md, split-patterns.md | US-7 | FR-028 |
| categorization/ | gl-suggestions.md | US-12 | FR-036 |
| (root) | keyboard-shortcuts.md | US-11 | FR-033 |

**Section Prerequisites**: Signed in (01-getting-started/signing-in.md)
**Estimated Reading Time**: 30-45 minutes (full section)

---

### Section: 03-monthly-close

| Subsection | Files | User Stories | FRs |
|------------|-------|--------------|-----|
| reports/ | generating.md, status-workflow.md, exporting.md | US-5 | FR-017, FR-018, FR-019 |
| analytics/ | dashboard.md, trends.md, categories.md | US-6 | FR-020, FR-021 |
| subscriptions/ | detection.md, alerts.md, manual-entry.md | US-8 | FR-022, FR-029 |
| travel/ | periods.md, timeline.md | US-9 | FR-030 |

**Section Prerequisites**: Some matched expenses exist
**Estimated Reading Time**: 20-30 minutes (full section)

---

### Section: 04-reference

| File | Purpose | User Stories | FRs |
|------|---------|--------------|-----|
| troubleshooting.md | Common issues and resolutions | US-13 | FR-026, FR-031 |
| settings.md | All settings options explained | US-14 | FR-023 |
| mobile.md | Mobile-specific features | US-15 | FR-024, FR-037 |
| glossary.md | Term definitions A-Z | - | FR-025 |
| keyboard-shortcuts.md | Quick reference card | US-11 | FR-033 |

**Section Prerequisites**: None (reference lookup)
**Estimated Reading Time**: Variable (lookup-based)

---

## Cross-Reference Map

Documents should link to related content. This map shows required cross-references:

| Source Document | Links To |
|-----------------|----------|
| signing-in.md | dashboard-overview.md |
| dashboard-overview.md | quick-start.md, receipts/uploading.md, matching/review-modes.md |
| quick-start.md | receipts/ai-extraction.md, statements/importing.md |
| receipts/uploading.md | receipts/ai-extraction.md, troubleshooting.md |
| receipts/ai-extraction.md | receipts/image-viewer.md, matching/confidence-scores.md |
| statements/importing.md | statements/column-mapping.md |
| statements/column-mapping.md | statements/fingerprints.md, transactions/filtering.md |
| statements/fingerprints.md | settings.md |
| transactions/filtering.md | transactions/bulk-operations.md |
| matching/review-modes.md | matching/confidence-scores.md, keyboard-shortcuts.md |
| matching/confidence-scores.md | matching/improving-accuracy.md |
| matching/manual-matching.md | matching/review-modes.md |
| splitting/expense-splitting.md | splitting/split-patterns.md |
| reports/generating.md | reports/status-workflow.md |
| reports/status-workflow.md | reports/exporting.md |
| analytics/dashboard.md | analytics/trends.md, subscriptions/detection.md |
| subscriptions/detection.md | subscriptions/alerts.md |
| travel/periods.md | travel/timeline.md |
| troubleshooting.md | (links to all relevant sections for specific issues) |
| glossary.md | (inline links from term usages throughout guide) |

## Content Templates

### Standard Page Template

```markdown
# [Page Title]

[1-2 sentence description of what this page covers]

## Overview

[Brief explanation of the feature/concept]

## [Primary Action or Concept]

[Step-by-step instructions or explanation]

1. [Step 1]
2. [Step 2]
3. [Step 3]

![Description](../images/{section}/{image-name}.png)
*Caption: What the screenshot shows*

> **Tip**: [Helpful hint for better usage]

## [Secondary Topic] (if applicable)

[Additional content]

> **Warning**: [Important caution if applicable]

## What's Next

- [Related topic 1](./related-1.md) - Brief description
- [Related topic 2](../other-section/related-2.md) - Brief description
```

### Glossary Entry Template

```markdown
### [Term]

[Definition in 1-2 sentences]

**Example**: [Concrete example of the term in use]

**See also**: [Related term 1], [Related term 2]
```

### Troubleshooting Entry Template

```markdown
### [Issue Title]

**Symptoms**: What the user sees or experiences

**Cause**: Why this happens

**Solution**:
1. [Step 1]
2. [Step 2]

**Prevention**: How to avoid this in the future (if applicable)
```

## Validation Rules

Each document must satisfy:

1. **Title**: Matches filename (e.g., `uploading.md` → "# Uploading Receipts")
2. **Overview**: Present and under 100 words
3. **Screenshots**: All referenced images exist in images/ directory
4. **Cross-references**: All internal links resolve to existing files
5. **What's Next**: 2-3 links to logical next steps
6. **Glossary terms**: Bold on first use, linked to glossary
7. **FR coverage**: All assigned FRs have corresponding content

## Estimated Metrics

| Metric | Target | Based On |
|--------|--------|----------|
| Total pages | 35 | Document hierarchy count |
| Total words | 15,000-20,000 | ~500 words/page average |
| Screenshots | 45 | research.md inventory |
| Glossary terms | 25 | research.md candidates |
| Troubleshooting entries | 12 | research.md candidates |
| Cross-reference links | 50+ | Cross-reference map |
