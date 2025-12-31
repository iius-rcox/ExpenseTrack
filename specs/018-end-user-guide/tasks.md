# Tasks: ExpenseFlow End User Guide

**Input**: Design documents from `/specs/018-end-user-guide/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: N/A - Documentation project (no automated tests)

**Organization**: Tasks are grouped by user story to enable independent writing and validation of each section.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Documentation**: `docs/user-guide/` at repository root
- **Screenshots**: `docs/user-guide/images/{section}/`

---

## Phase 1: Setup (Directory Structure)

**Purpose**: Create documentation structure and navigation framework

- [x] T001 Create documentation directory structure per data-model.md hierarchy in docs/user-guide/
- [x] T002 [P] Create root README.md with table of contents and quick links in docs/user-guide/README.md
- [x] T003 [P] Create 01-getting-started section README.md in docs/user-guide/01-getting-started/README.md
- [x] T004 [P] Create 02-daily-use section README.md in docs/user-guide/02-daily-use/README.md
- [x] T005 [P] Create 03-monthly-close section README.md in docs/user-guide/03-monthly-close/README.md
- [x] T006 [P] Create 04-reference section README.md in docs/user-guide/04-reference/README.md

---

## Phase 2: Foundational (Reference Section - Shared Resources)

**Purpose**: Create glossary and troubleshooting resources that ALL other sections will reference

**⚠️ CRITICAL**: Glossary terms and troubleshooting entries must exist before user story pages can link to them

- [x] T007 Create glossary with 25 terms per research.md in docs/user-guide/04-reference/glossary.md
- [x] T008 Create troubleshooting guide with 12 entries per research.md in docs/user-guide/04-reference/troubleshooting.md

**Checkpoint**: Foundation ready - user story sections can now be written with proper cross-references

---

## Phase 3: User Story 1 - New User Getting Started (Priority: P1) 🎯 MVP

**Goal**: Enable new users to sign in, understand the dashboard, and upload their first receipt within 15 minutes

**Independent Test**: New user can complete first-time experience using only the Getting Started section

### Implementation for User Story 1

- [x] T009 [US1] Write signing-in.md covering Microsoft SSO authentication in docs/user-guide/01-getting-started/signing-in.md
- [x] T010 [US1] Write dashboard-overview.md explaining all dashboard components in docs/user-guide/01-getting-started/dashboard-overview.md
- [x] T011 [US1] Write quick-start.md for first receipt upload walkthrough in docs/user-guide/01-getting-started/quick-start.md
- [ ] T012 [P] [US1] Capture sign-in screenshot in docs/user-guide/images/dashboard/sign-in-screen.png
- [ ] T013 [P] [US1] Capture dashboard full view screenshot in docs/user-guide/images/dashboard/dashboard-full-view.png
- [ ] T014 [P] [US1] Capture action queue detail screenshot in docs/user-guide/images/dashboard/action-queue-detail.png
- [ ] T015 [P] [US1] Capture activity feed screenshot in docs/user-guide/images/dashboard/activity-feed-detail.png
- [x] T016 [US1] Add cross-references and What's Next links to all US1 pages
- [x] T017 [US1] Validate navigation flow through Getting Started section

**Checkpoint**: User Story 1 complete - new users can onboard independently

---

## Phase 4: User Story 2 - Receipt Upload and AI Processing (Priority: P1)

**Goal**: Enable users to upload receipts, understand AI extraction, and correct errors

**Independent Test**: User can upload 5 receipts, review extractions, and correct errors

### Implementation for User Story 2

- [x] T018 [US2] Write uploading.md covering all upload methods in docs/user-guide/02-daily-use/receipts/uploading.md
- [x] T019 [US2] Write ai-extraction.md explaining confidence scores and editing in docs/user-guide/02-daily-use/receipts/ai-extraction.md
- [ ] T020 [P] [US2] Capture drag-drop zone screenshot in docs/user-guide/images/receipts/upload-drag-drop-zone.png
- [ ] T021 [P] [US2] Capture extracted fields with confidence colors screenshot in docs/user-guide/images/receipts/ai-extraction-confidence.png
- [ ] T022 [P] [US2] Capture edit mode screenshot in docs/user-guide/images/receipts/extraction-edit-mode.png
- [x] T023 [US2] Add cross-references to troubleshooting for upload failures
- [x] T024 [US2] Validate receipt upload documentation flow

**Checkpoint**: User Story 2 complete - users can process receipts independently

---

## Phase 5: User Story 3 - Statement Import and Transaction Management (Priority: P1)

**Goal**: Enable users to import statements, map columns, save fingerprints, and manage transactions

**Independent Test**: User can import CSV, complete mapping, save fingerprint, and filter transactions

### Implementation for User Story 3

- [x] T025 [US3] Write importing.md for 3-step wizard in docs/user-guide/02-daily-use/statements/importing.md
- [x] T026 [US3] Write column-mapping.md with visual examples in docs/user-guide/02-daily-use/statements/column-mapping.md
- [x] T027 [US3] Write fingerprints.md covering template management in docs/user-guide/02-daily-use/statements/fingerprints.md
- [x] T028 [US3] Write filtering.md for all filter types in docs/user-guide/02-daily-use/transactions/filtering.md
- [x] T029 [US3] Write bulk-operations.md for multi-select actions in docs/user-guide/02-daily-use/transactions/bulk-operations.md
- [x] T030 [US3] Write sorting.md for column sort options in docs/user-guide/02-daily-use/transactions/sorting.md
- [ ] T031 [P] [US3] Capture upload progress screenshot in docs/user-guide/images/statements/import-upload-step.png
- [ ] T032 [P] [US3] Capture column mapper screenshot in docs/user-guide/images/statements/column-mapper.png
- [ ] T033 [P] [US3] Capture fingerprint save dialog screenshot in docs/user-guide/images/statements/fingerprint-save.png
- [ ] T034 [P] [US3] Capture filter panel screenshot in docs/user-guide/images/transactions/filter-panel.png
- [ ] T035 [P] [US3] Capture bulk actions bar screenshot in docs/user-guide/images/transactions/bulk-actions-bar.png
- [x] T036 [US3] Add cross-references between statement and transaction pages
- [x] T037 [US3] Validate statement import documentation flow

**Checkpoint**: User Story 3 complete - users can import and manage transactions independently

---

## Phase 6: User Story 4 - Receipt-Transaction Matching (Priority: P2)

**Goal**: Enable users to review matches, understand confidence, and train the AI

**Independent Test**: User can review 10 matches, confirm/reject, manually match, and understand AI training

### Implementation for User Story 4

- [x] T038 [US4] Write review-modes.md explaining Review and List modes in docs/user-guide/02-daily-use/matching/review-modes.md
- [x] T039 [US4] Write confidence-scores.md with matching factors breakdown in docs/user-guide/02-daily-use/matching/confidence-scores.md
- [x] T040 [US4] Write manual-matching.md for unmatched pairs in docs/user-guide/02-daily-use/matching/manual-matching.md
- [x] T041 [US4] Write improving-accuracy.md explaining AI training and best practices in docs/user-guide/02-daily-use/matching/improving-accuracy.md
- [ ] T042 [P] [US4] Capture review mode split-pane screenshot in docs/user-guide/images/matching/review-mode-split-pane.png
- [ ] T043 [P] [US4] Capture match proposal with factors screenshot in docs/user-guide/images/matching/match-confidence-factors.png
- [ ] T044 [P] [US4] Capture manual match dialog screenshot in docs/user-guide/images/matching/manual-match-dialog.png
- [x] T045 [US4] Add cross-references to keyboard shortcuts for match review
- [x] T046 [US4] Validate matching documentation flow

**Checkpoint**: User Story 4 complete - users can review and improve matching independently

---

## Phase 7: User Story 5 - Expense Report Generation (Priority: P2)

**Goal**: Enable users to generate reports, understand status workflow, and export

**Independent Test**: User can generate report, review status states, and export to PDF/Excel

### Implementation for User Story 5

- [x] T047 [US5] Write generating.md for report creation dialog in docs/user-guide/03-monthly-close/reports/generating.md
- [x] T048 [US5] Write status-workflow.md explaining Draft/Submitted/Approved/Rejected in docs/user-guide/03-monthly-close/reports/status-workflow.md
- [x] T049 [US5] Write exporting.md covering PDF and Excel options in docs/user-guide/03-monthly-close/reports/exporting.md
- [ ] T050 [P] [US5] Capture report generation dialog screenshot in docs/user-guide/images/reports/report-generation-dialog.png
- [ ] T051 [P] [US5] Capture report status cards screenshot in docs/user-guide/images/reports/report-status-cards.png
- [ ] T052 [P] [US5] Capture export dropdown screenshot in docs/user-guide/images/reports/export-dropdown.png
- [x] T053 [US5] Add cross-references from reports to matching section
- [x] T054 [US5] Validate report generation documentation flow

**Checkpoint**: User Story 5 complete - users can generate and export reports independently

---

## Phase 8: User Story 6 - Spending Analytics (Priority: P2)

**Goal**: Enable users to understand analytics dashboard, trends, and category breakdowns

**Independent Test**: User can access analytics, select date ranges, and compare periods

### Implementation for User Story 6

- [x] T055 [US6] Write dashboard.md for analytics components overview in docs/user-guide/03-monthly-close/analytics/dashboard.md
- [x] T056 [US6] Write trends.md for date ranges and comparisons in docs/user-guide/03-monthly-close/analytics/trends.md
- [x] T057 [US6] Write categories.md for breakdown analysis in docs/user-guide/03-monthly-close/analytics/categories.md
- [ ] T058 [P] [US6] Capture full analytics view screenshot in docs/user-guide/images/analytics/analytics-full-view.png
- [ ] T059 [P] [US6] Capture trend chart screenshot in docs/user-guide/images/analytics/trend-chart.png
- [ ] T060 [P] [US6] Capture category breakdown screenshot in docs/user-guide/images/analytics/category-breakdown.png
- [x] T061 [US6] Add cross-references to subscriptions section
- [x] T062 [US6] Validate analytics documentation flow

**Checkpoint**: User Story 6 complete - users can analyze spending independently

---

## Phase 9: User Story 7 - Expense Splitting (Priority: P2)

**Goal**: Enable users to split expenses and save reusable patterns

**Independent Test**: User can split expense, save pattern, and reuse pattern

### Implementation for User Story 7

- [x] T063 [US7] Write expense-splitting.md for percentage allocations in docs/user-guide/02-daily-use/splitting/expense-splitting.md
- [x] T064 [US7] Write split-patterns.md for pattern management in docs/user-guide/02-daily-use/splitting/split-patterns.md
- [ ] T065 [P] [US7] Capture split allocation editor screenshot in docs/user-guide/images/splitting/split-allocation-editor.png
- [ ] T066 [P] [US7] Capture pattern save dialog screenshot in docs/user-guide/images/splitting/pattern-save-dialog.png
- [x] T067 [US7] Add cross-references from transactions to splitting
- [x] T068 [US7] Validate expense splitting documentation flow

**Checkpoint**: User Story 7 complete - users can split expenses independently

---

## Phase 10: User Story 8 - Subscription Management (Priority: P2)

**Goal**: Enable users to view detected subscriptions, manage alerts, and create manual entries

**Independent Test**: User can view subscriptions, acknowledge alert, and create manual subscription

### Implementation for User Story 8

- [x] T069 [US8] Write detection.md for auto-detection explanation in docs/user-guide/03-monthly-close/subscriptions/detection.md
- [x] T070 [US8] Write alerts.md for alert triggers and acknowledgment in docs/user-guide/03-monthly-close/subscriptions/alerts.md
- [x] T071 [US8] Write manual-entry.md for creating subscriptions in docs/user-guide/03-monthly-close/subscriptions/manual-entry.md
- [ ] T072 [P] [US8] Capture subscriptions tab screenshot in docs/user-guide/images/subscriptions/subscriptions-tab.png
- [ ] T073 [P] [US8] Capture alert badge screenshot in docs/user-guide/images/subscriptions/alert-badge.png
- [ ] T074 [P] [US8] Capture create subscription dialog screenshot in docs/user-guide/images/subscriptions/create-subscription.png
- [x] T075 [US8] Add cross-references from analytics to subscriptions
- [x] T076 [US8] Validate subscription management documentation flow

**Checkpoint**: User Story 8 complete - users can manage subscriptions independently

---

## Phase 11: User Story 9 - Travel Period Organization (Priority: P3)

**Goal**: Enable users to create travel periods and view linked expenses

**Independent Test**: User can create travel period and view timeline

### Implementation for User Story 9

- [x] T077 [US9] Write periods.md for travel period creation in docs/user-guide/03-monthly-close/travel/periods.md
- [x] T078 [US9] Write timeline.md for viewing linked expenses in docs/user-guide/03-monthly-close/travel/timeline.md
- [ ] T079 [P] [US9] Capture travel period creation dialog screenshot in docs/user-guide/images/travel/period-creation.png
- [ ] T080 [P] [US9] Capture timeline view screenshot in docs/user-guide/images/travel/timeline-view.png
- [x] T081 [US9] Add cross-references from receipts to travel
- [x] T082 [US9] Validate travel period documentation flow

**Checkpoint**: User Story 9 complete - users can organize travel expenses independently

---

## Phase 12: User Story 10 - Dashboard Understanding (Priority: P2)

**Goal**: Enable users to understand action queue, activity feed, and metrics

**Independent Test**: User can interpret queue priorities and activity events

Note: Some dashboard content may already be covered in US1. This phase adds depth to action queue and activity feed specifics.

### Implementation for User Story 10

- [x] T083 [US10] Enhance dashboard-overview.md with detailed action queue priority explanations in docs/user-guide/01-getting-started/dashboard-overview.md
- [x] T084 [US10] Add activity feed event type reference to dashboard documentation
- [x] T085 [US10] Add metrics interpretation section to dashboard documentation
- [ ] T086 [P] [US10] Capture metrics row screenshot in docs/user-guide/images/dashboard/metrics-row.png
- [x] T087 [US10] Validate enhanced dashboard documentation

**Checkpoint**: User Story 10 complete - users understand dashboard in depth

---

## Phase 13: User Story 11 - Keyboard Shortcuts and Power User Features (Priority: P3)

**Goal**: Enable power users to work efficiently with shortcuts and advanced controls

**Independent Test**: User can navigate match review with keyboard and use image viewer controls

### Implementation for User Story 11

- [x] T088 [US11] Write keyboard-shortcuts.md comprehensive reference in docs/user-guide/02-daily-use/keyboard-shortcuts.md
- [x] T089 [US11] Write image-viewer.md for zoom, rotate, pan controls in docs/user-guide/02-daily-use/receipts/image-viewer.md
- [x] T090 [US11] Write keyboard-shortcuts.md quick reference card in docs/user-guide/04-reference/keyboard-shortcuts.md
- [ ] T091 [P] [US11] Capture shortcuts help panel screenshot in docs/user-guide/images/receipts/shortcuts-help-panel.png
- [ ] T092 [P] [US11] Capture image viewer controls screenshot in docs/user-guide/images/receipts/image-viewer-controls.png
- [x] T093 [US11] Add keyboard shortcut callouts to matching documentation
- [x] T094 [US11] Validate keyboard shortcuts documentation

**Checkpoint**: User Story 11 complete - power users have efficiency reference

---

## Phase 14: User Story 12 - Categorization Confirmation (Priority: P3)

**Goal**: Enable users to understand and respond to GL code suggestions

**Independent Test**: User can confirm, modify, and skip categorization suggestions

### Implementation for User Story 12

- [x] T095 [US12] Write gl-suggestions.md for categorization workflow in docs/user-guide/02-daily-use/categorization/gl-suggestions.md
- [ ] T096 [P] [US12] Capture GL suggestions panel screenshot in docs/user-guide/images/categorization/gl-suggestions.png
- [ ] T097 [P] [US12] Capture skip workflow screenshot in docs/user-guide/images/categorization/skip-workflow.png
- [x] T098 [US12] Add cross-references from transactions to categorization
- [x] T099 [US12] Validate categorization documentation flow

**Checkpoint**: User Story 12 complete - users can handle categorization suggestions

---

## Phase 15: User Story 13 - Troubleshooting and Recovery (Priority: P3)

**Goal**: Enable users to resolve common issues independently

**Independent Test**: User can follow troubleshooting steps for upload failure and retry

Note: Base troubleshooting.md created in Phase 2. This phase adds additional entries and validates completeness.

### Implementation for User Story 13

- [x] T100 [US13] Expand troubleshooting.md with all 12 issue entries per research.md in docs/user-guide/04-reference/troubleshooting.md
- [ ] T101 [P] [US13] Capture error toast example screenshot in docs/user-guide/images/troubleshooting/error-toast.png
- [ ] T102 [P] [US13] Capture retry button screenshot in docs/user-guide/images/troubleshooting/retry-button.png
- [x] T103 [US13] Add deep links to troubleshooting from all relevant sections
- [x] T104 [US13] Validate troubleshooting covers all edge cases from spec.md

**Checkpoint**: User Story 13 complete - users can self-service common issues

---

## Phase 16: User Story 14 - Settings and Personalization (Priority: P3)

**Goal**: Enable users to customize theme, defaults, and categories

**Independent Test**: User can change theme and set default department

### Implementation for User Story 14

- [x] T105 [US14] Write settings.md covering all settings sections in docs/user-guide/04-reference/settings.md
- [ ] T106 [P] [US14] Capture settings page full view screenshot in docs/user-guide/images/settings/settings-full-view.png
- [ ] T107 [P] [US14] Capture theme selector screenshot in docs/user-guide/images/settings/theme-selector.png
- [x] T108 [US14] Add cross-references from fingerprints to settings for template management
- [x] T109 [US14] Validate settings documentation completeness

**Checkpoint**: User Story 14 complete - users can customize their experience

---

## Phase 17: User Story 15 - Mobile Usage (Priority: P3)

**Goal**: Enable users to capture receipts and use swipe actions on mobile

**Independent Test**: User can capture receipt photo and use swipe actions on mobile device

### Implementation for User Story 15

- [x] T110 [US15] Write mobile.md covering all mobile-specific features in docs/user-guide/04-reference/mobile.md
- [ ] T111 [P] [US15] Capture mobile dashboard screenshot in docs/user-guide/images/mobile/mobile-dashboard.png
- [ ] T112 [P] [US15] Capture bottom navigation screenshot in docs/user-guide/images/mobile/bottom-navigation.png
- [ ] T113 [P] [US15] Capture swipe action screenshot in docs/user-guide/images/mobile/swipe-action.png
- [ ] T114 [P] [US15] Capture upload queue progress screenshot in docs/user-guide/images/mobile/upload-queue.png
- [x] T115 [US15] Add cross-references from receipt upload to mobile section
- [x] T116 [US15] Validate mobile documentation on actual mobile device

**Checkpoint**: User Story 15 complete - mobile users have dedicated guidance

---

## Phase 18: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cross-document consistency

- [x] T117 Validate all internal links resolve correctly across all documents
- [x] T118 Validate all glossary terms are linked on first use in each document
- [ ] T119 Verify all 45 screenshots exist and match filenames in documents (PENDING: Screenshot capture requires staging environment)
- [x] T120 Run spellcheck and grammar check on all documentation files
- [x] T121 Validate "What's Next" sections provide logical navigation paths
- [x] T122 Verify all 39 functional requirements from spec.md are covered
- [x] T123 Validate independent test criteria for each user story
- [x] T124 Update root README.md with final page count and last updated date
- [x] T125 Validate navigation from quick-start through complete monthly close workflow

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup - creates glossary/troubleshooting that other sections link to
- **User Stories (Phase 3-17)**: All depend on Foundational phase completion
  - P1 stories (US1-3) should be prioritized
  - P2 stories (US4-8, US10) can proceed in parallel after P1
  - P3 stories (US9, US11-15) can proceed after P2 or in parallel if capacity allows
- **Polish (Phase 18)**: Depends on all user stories being complete

### User Story Dependencies

| Story | Priority | Depends On | Can Start After |
|-------|----------|------------|-----------------|
| US1 | P1 | Foundational | Phase 2 |
| US2 | P1 | Foundational | Phase 2 |
| US3 | P1 | Foundational | Phase 2 |
| US4 | P2 | US2, US3 (content references) | Phase 4, 5 |
| US5 | P2 | US4 (matched expenses context) | Phase 6 |
| US6 | P2 | Foundational | Phase 2 |
| US7 | P2 | US3 (transaction context) | Phase 5 |
| US8 | P2 | US6 (analytics context) | Phase 8 |
| US9 | P3 | US2 (receipts context) | Phase 4 |
| US10 | P2 | US1 (dashboard base) | Phase 3 |
| US11 | P3 | US4 (matching context) | Phase 6 |
| US12 | P3 | US3 (transactions context) | Phase 5 |
| US13 | P3 | Foundational | Phase 2 |
| US14 | P3 | Foundational | Phase 2 |
| US15 | P3 | US2 (receipts context) | Phase 4 |

### Parallel Opportunities

**Setup Phase (can run in parallel)**:
- T002, T003, T004, T005, T006 - all section README.md files

**Within Each User Story (screenshots can run in parallel)**:
- All screenshot capture tasks marked [P]
- Content writing tasks should be sequential (page → screenshots → cross-refs → validate)

**Across User Stories (if multiple writers)**:
- US1, US2, US3 can all proceed in parallel after Foundational
- US6, US13, US14 have no dependencies on other stories

---

## Parallel Example: Phase 1 Setup

```bash
# Launch all section READMEs together:
Task: "Create root README.md with table of contents in docs/user-guide/README.md"
Task: "Create 01-getting-started section README.md"
Task: "Create 02-daily-use section README.md"
Task: "Create 03-monthly-close section README.md"
Task: "Create 04-reference section README.md"
```

---

## Parallel Example: User Story 2 Screenshots

```bash
# Launch all screenshots for US2 together:
Task: "Capture drag-drop zone screenshot in docs/user-guide/images/receipts/upload-drag-drop-zone.png"
Task: "Capture extracted fields with confidence colors screenshot"
Task: "Capture edit mode screenshot"
```

---

## Implementation Strategy

### MVP First (Getting Started Only)

1. Complete Phase 1: Setup (directory structure)
2. Complete Phase 2: Foundational (glossary, troubleshooting base)
3. Complete Phase 3: User Story 1 (Getting Started section)
4. **STOP and VALIDATE**: New users can onboard using only these docs
5. Deploy/publish if ready

### Incremental Delivery

1. Complete Setup + Foundational → Structure ready
2. Add US1 (Getting Started) → Test independently → Publish (MVP!)
3. Add US2-3 (Receipts, Statements) → Test independently → Publish
4. Add US4-8 (Matching, Reports, Analytics, Splitting, Subscriptions) → Publish
5. Add US9-15 (Travel, Shortcuts, Settings, Mobile) → Publish
6. Complete Polish → Final publication

### Writing Team Strategy

With multiple writers:

1. Team creates Setup + Foundational together
2. Once Foundational is done:
   - Writer A: User Stories 1-3 (Onboarding + Core Data Entry)
   - Writer B: User Stories 4-6 (Matching + Reports + Analytics)
   - Writer C: User Stories 7-8, 13-15 (Advanced Features + Reference)
3. Stories complete and integrate via cross-references

---

## Summary

| Metric | Count |
|--------|-------|
| **Total Tasks** | 125 |
| **Setup Tasks** | 6 |
| **Foundational Tasks** | 2 |
| **User Story Tasks** | 108 |
| **Polish Tasks** | 9 |
| **Parallelizable Tasks** | 48 (38%) |

### Tasks per User Story

| Story | Description | Tasks |
|-------|-------------|-------|
| US1 | New User Getting Started | 9 |
| US2 | Receipt Upload | 7 |
| US3 | Statement Import | 13 |
| US4 | Matching | 9 |
| US5 | Reports | 8 |
| US6 | Analytics | 8 |
| US7 | Splitting | 6 |
| US8 | Subscriptions | 8 |
| US9 | Travel | 6 |
| US10 | Dashboard | 5 |
| US11 | Shortcuts | 7 |
| US12 | Categorization | 5 |
| US13 | Troubleshooting | 5 |
| US14 | Settings | 5 |
| US15 | Mobile | 7 |

### MVP Scope

**Recommended MVP**: Phases 1-3 (User Story 1 only)
- 17 tasks total
- Deliverables: Directory structure, glossary, troubleshooting base, Getting Started section
- Outcome: New users can onboard using only the documentation

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Screenshots should be captured from staging environment
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Follow quickstart.md writing standards for all content
