# Implementation Plan: ExpenseFlow End User Guide

**Branch**: `018-end-user-guide` | **Date**: 2025-12-30 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/018-end-user-guide/spec.md`

## Summary

Create a comprehensive end user guide for ExpenseFlow organized as workflow-based sequential guides (Getting Started → Daily Use → Monthly Close → Reference). The documentation will be delivered as Markdown files in the repository with annotated screenshots for complex workflows. The guide covers 15 user stories and 39 functional requirements spanning receipt processing, statement import, matching, expense splitting, subscriptions, travel periods, analytics, and mobile usage.

## Technical Context

**Language/Version**: Markdown (GitHub Flavored Markdown)
**Primary Dependencies**: None (documentation only)
**Storage**: Repository files in `docs/user-guide/` directory
**Testing**: Manual review against acceptance scenarios; screenshot validation
**Target Platform**: GitHub-rendered Markdown, compatible with any Markdown viewer
**Project Type**: Documentation (no source code changes)
**Performance Goals**: N/A (static documentation)
**Constraints**:
- Screenshots must be captured from production UI
- All 20+ glossary terms must be defined
- 10+ troubleshooting entries required
**Scale/Scope**: 15 user stories, 39 functional requirements, 16 edge cases

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Applies to Documentation? | Status |
|-----------|---------------------------|--------|
| I. Clean Architecture | No (code-only) | N/A |
| II. Test-First Development | No (no code) | N/A |
| III. ERP Integration | Reference only (document Vista-synced fields) | PASS |
| IV. API Design | No (no new APIs) | N/A |
| V. Observability | No (no logging) | N/A |
| VI. Security | Document auth flow for users | PASS |

**Gate Result**: PASS - Documentation project does not introduce code changes. Constitution principles are not violated.

## Project Structure

### Documentation (this feature)

```text
specs/018-end-user-guide/
├── plan.md              # This file
├── research.md          # Document structure research
├── data-model.md        # Document hierarchy and section definitions
├── quickstart.md        # Writing guide for documentation authors
├── checklists/
│   └── requirements.md  # Spec quality checklist (already exists)
└── tasks.md             # Task breakdown (created by /speckit.tasks)
```

### Source Code (documentation deliverables)

```text
docs/
└── user-guide/
    ├── README.md                    # Table of contents and navigation
    ├── 01-getting-started/
    │   ├── README.md                # Section overview
    │   ├── signing-in.md            # US-1: Authentication
    │   ├── dashboard-overview.md    # US-1, US-10: Dashboard understanding
    │   └── quick-start.md           # US-1: First receipt upload
    ├── 02-daily-use/
    │   ├── README.md                # Section overview
    │   ├── receipts/
    │   │   ├── uploading.md         # US-2: Upload methods
    │   │   ├── ai-extraction.md     # US-2: Confidence scores, editing
    │   │   └── image-viewer.md      # US-11: Zoom, rotate, pan
    │   ├── statements/
    │   │   ├── importing.md         # US-3: 3-step wizard
    │   │   ├── column-mapping.md    # US-3: Mapping guidance
    │   │   └── fingerprints.md      # US-3: Template management
    │   ├── transactions/
    │   │   ├── filtering.md         # US-3: All filter types
    │   │   ├── bulk-operations.md   # US-3: Categorize, tag, export
    │   │   └── sorting.md           # FR-038: Sort options
    │   ├── matching/
    │   │   ├── review-modes.md      # US-4: Review vs List mode
    │   │   ├── confidence-scores.md # US-4: Interpreting matches
    │   │   ├── manual-matching.md   # US-4: Creating manual links
    │   │   └── improving-accuracy.md # US-4: Training the AI
    │   ├── splitting/
    │   │   ├── expense-splitting.md # US-7: Percentage allocations
    │   │   └── split-patterns.md    # US-7: Reusable patterns
    │   ├── categorization/
    │   │   └── gl-suggestions.md    # US-12: Confirming/skipping
    │   └── keyboard-shortcuts.md    # US-11: Power user reference
    ├── 03-monthly-close/
    │   ├── README.md                # Section overview
    │   ├── reports/
    │   │   ├── generating.md        # US-5: Draft creation
    │   │   ├── status-workflow.md   # US-5: Draft→Submitted→Approved
    │   │   └── exporting.md         # US-5: PDF and Excel
    │   ├── analytics/
    │   │   ├── dashboard.md         # US-6: Components overview
    │   │   ├── trends.md            # US-6: Date ranges, comparison
    │   │   └── categories.md        # US-6: Breakdown analysis
    │   ├── subscriptions/
    │   │   ├── detection.md         # US-8: Auto-detection
    │   │   ├── alerts.md            # US-8: Managing alerts
    │   │   └── manual-entry.md      # US-8: Creating subscriptions
    │   └── travel/
    │       ├── periods.md           # US-9: Creating periods
    │       └── timeline.md          # US-9: Viewing linked expenses
    ├── 04-reference/
    │   ├── README.md                # Section overview
    │   ├── troubleshooting.md       # US-13: Common issues
    │   ├── settings.md              # US-14: All settings sections
    │   ├── mobile.md                # US-15: Mobile-specific features
    │   ├── glossary.md              # FR-025: 20+ terms
    │   └── keyboard-shortcuts.md    # Quick reference card
    └── images/
        ├── dashboard/               # Dashboard screenshots
        ├── receipts/                # Receipt workflow screenshots
        ├── statements/              # Statement import screenshots
        ├── matching/                # Match review screenshots
        ├── reports/                 # Report screenshots
        ├── analytics/               # Analytics screenshots
        └── mobile/                  # Mobile UI screenshots
```

**Structure Decision**: Workflow-based organization matching the clarified specification. Four main sections mirror the user adoption journey: Getting Started (onboarding), Daily Use (regular tasks), Monthly Close (periodic tasks), and Reference (lookup resources).

## Complexity Tracking

> No constitution violations to justify - documentation project.

| Consideration | Decision | Rationale |
|---------------|----------|-----------|
| Single vs. multiple files | Multiple files per topic | Allows targeted updates, better navigation |
| Flat vs. nested structure | Nested by workflow phase | Matches user mental model and specification organization |
| Screenshots inline vs. separate | Separate images/ directory | Easier maintenance, consistent naming |

## Phase 0: Research Outputs

See [research.md](./research.md) for:
- Document section ordering and dependencies
- Screenshot requirements per section
- Glossary term candidates
- Troubleshooting entry candidates

## Phase 1: Design Outputs

- [data-model.md](./data-model.md): Document hierarchy, section definitions, cross-references
- [quickstart.md](./quickstart.md): Writing guide for documentation authors
- No `/contracts/` needed (documentation project)
