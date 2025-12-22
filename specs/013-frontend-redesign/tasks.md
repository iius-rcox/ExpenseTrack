# Tasks: Front-End Redesign with Refined Intelligence Design System

**Input**: Design documents from `/specs/013-frontend-redesign/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/frontend-api-contracts.md ✓, quickstart.md ✓

**Tests**: Unit tests with Vitest + React Testing Library, E2E tests with Playwright (as specified in quickstart.md)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Frontend**: `frontend/src/` (per plan.md project structure)
- **Tests**: `frontend/tests/unit/`, `frontend/tests/e2e/`

---

## Phase 1: Setup (Project Initialization) ✅ COMPLETE

**Purpose**: Install dependencies and create directory structure for redesign

- [x] T001 Install new dependencies: `npm install framer-motion @tanstack/react-virtual use-debounce` in frontend/
- [x] T002 [P] Create directory structure: `frontend/src/components/{dashboard,transactions,matching,analytics,design-system}/`
- [x] T003 [P] Create hooks directory: `frontend/src/hooks/ui/`
- [x] T004 [P] Add custom fonts to `frontend/index.html` (Playfair Display, Plus Jakarta Sans, JetBrains Mono)
- [x] T005 [P] Create `frontend/tests/unit/` and `frontend/tests/e2e/` directories
- [x] T006 Configure Vitest in `frontend/vitest.config.ts` (per quickstart.md)
- [x] T007 [P] Create `frontend/tests/setup.ts` with testing-library/jest-dom

---

## Phase 2: Foundational (Design System & Shared Infrastructure) ✅ COMPLETE

**Purpose**: Core design system and hooks that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Design Tokens & Theme (from research.md Section 5)

- [x] T008 Create design tokens in `frontend/src/lib/design-tokens.ts` with ColorTokens, TypographyTokens, AnimationTokens (from data-model.md Section 1.1)
- [x] T009 Extend Tailwind config in `frontend/tailwind.config.ts` with accent colors, confidence colors, fonts, animations (per quickstart.md Section 2.2)
- [x] T010 [P] Add CSS custom properties to `frontend/src/index.css` for theme tokens (per research.md Section 5)
- [x] T011 [P] Create animation presets in `frontend/src/lib/animations.ts` with fadeIn, slideUp, staggerChildren, confidenceGlow variants (per quickstart.md Section 4)

### Core UI Hooks (from data-model.md Section 7)

- [x] T012 [P] Create `frontend/src/hooks/ui/use-undo.ts` hook for undo stack (per research.md Section 3)
- [x] T013 [P] Create `frontend/src/hooks/ui/use-keyboard-shortcuts.ts` for global shortcuts (per research.md Section 7)
- [x] T014 [P] Create `frontend/src/hooks/ui/use-polling.ts` for 30-second background refresh (per research.md Section 2)

### Base Design System Components

- [x] T015 Create `frontend/src/components/design-system/confidence-indicator.tsx` - signature visual element (per quickstart.md Section 3.2, data-model.md Section 1.2)
- [x] T016 [P] Create `frontend/src/components/design-system/stat-card.tsx` with trend indicators (per data-model.md Section 2.2)
- [x] T017 [P] Create `frontend/src/components/design-system/empty-state.tsx` for zero-data scenarios (per FR-031)
- [x] T018 [P] Create `frontend/src/components/design-system/loading-skeleton.tsx` for async operations (per FR-032)
- [x] T019 [P] Create confidence glow CSS in `frontend/src/components/design-system/confidence-glow.css` (per research.md Section 8)

### Foundational Unit Tests

- [x] T020 [P] Unit test for `use-undo` hook in `frontend/tests/unit/hooks/use-undo.test.ts`
- [x] T021 [P] Unit test for `use-keyboard-shortcuts` hook in `frontend/tests/unit/hooks/use-keyboard-shortcuts.test.ts`
- [x] T022 [P] Unit test for `ConfidenceIndicator` in `frontend/tests/unit/components/confidence-indicator.test.tsx`

**Checkpoint**: Design system foundation ready - user story implementation can now begin ✅

---

## Phase 3: User Story 1 - Dashboard Overview (Priority: P1) 🎯 MVP ✅ COMPLETE

**Goal**: Command center dashboard displaying real-time expense activity, pending items, matching status, and spending trends

**Independent Test**: Login and view dashboard with sample expense data. Verify metrics display within 2 seconds per SC-001.

**Acceptance Criteria** (from spec.md):
- Display monthly spending total, pending review count, matching percentage, category breakdown within 2 seconds
- Show action queue with priority-sorted pending items
- Update expense stream without page refresh (30-second polling)

### API Integration for US1

- [x] T023 Create TanStack Query hook `frontend/src/hooks/queries/use-dashboard-metrics.ts` for `GET /api/dashboard/metrics` (per contracts Section 1.1)
- [x] T024 [P] Create TanStack Query hook `frontend/src/hooks/queries/use-expense-stream.ts` for `GET /api/dashboard/activity` with 30s polling (per contracts Section 1.2)
- [x] T025 [P] Create TanStack Query hook `frontend/src/hooks/queries/use-action-queue.ts` for `GET /api/dashboard/actions` (per contracts Section 1.3)

### Types for US1 (from data-model.md Section 2)

- [x] T026 [P] Create `frontend/src/types/dashboard.ts` with DashboardMetrics, ExpenseStreamItem, ActionQueueItem, CategoryBreakdown types

### Components for US1 (from data-model.md Section 2.2, quickstart.md Section 7.2)

- [x] T027 Create `frontend/src/components/dashboard/metrics-row.tsx` with StatCard composition for key metrics (FR-001)
- [x] T028 Create `frontend/src/components/dashboard/expense-stream.tsx` for activity feed with real-time updates (FR-004)
- [x] T029 Create `frontend/src/components/dashboard/action-queue.tsx` for priority-sorted pending items (FR-002)
- [x] T030 Create `frontend/src/components/dashboard/category-breakdown.tsx` with visualization (uses Recharts)
- [x] T031 Create `frontend/src/components/dashboard/dashboard-layout.tsx` orchestrating all dashboard components (FR-003)

### Route Integration for US1

- [x] T032 Update `frontend/src/routes/_authenticated/dashboard.tsx` to use new dashboard components

### Tests for US1

- [x] T033 [P] Unit test for MetricsRow in `frontend/tests/unit/components/dashboard/metrics-row.test.tsx`
- [x] T034 [P] Unit test for ExpenseStream in `frontend/tests/unit/components/dashboard/expense-stream.test.tsx`
- [x] T035 Unit tests for ActionQueue, CategoryBreakdown in `frontend/tests/unit/components/dashboard/`

**Checkpoint**: User Story 1 complete - dashboard provides expense overview at a glance (SC-001 achievable)

---

## Phase 4: User Story 2 - Receipt Upload and Intelligence (Priority: P1) ✅ COMPLETE

**Goal**: Upload receipts with AI extraction, confidence indicators, and inline editing with undo

**Independent Test**: Upload a sample receipt image and verify extraction results display with confidence scores. Correct a field and verify undo works.

**Acceptance Criteria** (from spec.md):
- Drag-and-drop upload with processing indicator, extracted data within 5 seconds
- Confidence indicators for each AI-extracted field
- Immediate save on correction with undo capability
- Batch upload with queue management

### Types for US2 (from data-model.md Section 3)

- [x] T036 Create `frontend/src/types/receipt.ts` with ExtractedField, ReceiptPreview, ReceiptUploadState types

### API Integration for US2

- [x] T037 Create upload mutation `frontend/src/hooks/queries/use-receipt-upload.ts` for `POST /api/receipts/upload` (per contracts Section 2.1)
- [x] T038 [P] Create TanStack Query hook `frontend/src/hooks/queries/use-receipts.ts` for receipt list and detail (per contracts Section 2.2, 2.3)
- [x] T039 [P] Create field update mutation `frontend/src/hooks/queries/use-receipt-field-update.ts` with optimistic update (per contracts Section 2.4)

### Components for US2 (from data-model.md Section 3.2)

- [x] T040 Create `frontend/src/components/receipts/receipt-upload-dropzone.tsx` with drag-drop, file validation (FR-005, JPEG/PNG/HEIC/PDF, 20MB per research.md Section 4)
- [x] T041 Create `frontend/src/components/receipts/extracted-field.tsx` with confidence indicator and inline edit (FR-006, FR-007, FR-008)
- [x] T042 Create `frontend/src/components/receipts/receipt-intelligence-panel.tsx` with side-by-side layout and undo (FR-006)
- [x] T043 Create `frontend/src/components/receipts/batch-upload-queue.tsx` for multi-receipt progress (FR-009)
- [x] T044 Create `frontend/src/components/receipts/receipt-card.tsx` for list/grid display

### Route Integration for US2

- [x] T045 Create or update `frontend/src/routes/_authenticated/receipts.tsx` with new receipt components

### Tests for US2

- [x] T046 [P] Unit test for file validation in `frontend/tests/unit/components/receipts/receipt-upload-dropzone.test.tsx`
- [x] T047 [P] Unit test for ExtractedField inline edit in `frontend/tests/unit/components/receipts/extracted-field.test.tsx`
- [ ] T048 E2E test: Receipt upload and extraction in `frontend/tests/e2e/receipt-upload.spec.ts`

**Checkpoint**: User Story 2 complete - receipts can be uploaded, viewed, and corrected (SC-002 achievable) ✅

---

## Phase 5: User Story 3 - Transaction Exploration and Search (Priority: P2) ✅ COMPLETE

**Goal**: Searchable, filterable transaction grid with inline editing and bulk operations

**Independent Test**: Search for transactions with various filters and verify results. Edit a transaction inline and verify undo works.

**Acceptance Criteria** (from spec.md):
- Search term matching with highlighting
- Filters for date range, category, amount, match status without page reload
- Inline editing for category, notes, tags with auto-save and undo
- Bulk operations on multi-selected transactions

### Types for US3 (from data-model.md Section 4)

- [x] T049 Create `frontend/src/types/transaction.ts` with TransactionView, TransactionFilters, TransactionSortConfig, TransactionSelectionState types

### API Integration for US3

- [x] T050 Create TanStack Query hook `frontend/src/hooks/queries/use-transactions.ts` for `GET /api/transactions` with filters (per contracts Section 3.1)
- [x] T051 [P] Create update mutation `frontend/src/hooks/queries/use-transaction-update.ts` with optimistic update (per contracts Section 3.2)
- [x] T052 [P] Create bulk update mutation `frontend/src/hooks/queries/use-transactions-bulk.ts` for batch operations (per contracts Section 3.3)
- [x] T053 [P] Create export mutation `frontend/src/hooks/queries/use-transactions-export.ts` (per contracts Section 3.4)

### Components for US3 (from data-model.md Section 4.2)

- [x] T054 Create `frontend/src/components/transactions/transaction-filter-panel.tsx` with date, category, amount, status filters (FR-012)
- [x] T055 Create `frontend/src/components/transactions/transaction-row.tsx` with inline editing capability (FR-014)
- [x] T056 Create `frontend/src/components/transactions/transaction-grid.tsx` with sorting, filtering, virtualization for >100 items (FR-010, FR-011, per research.md Section 6)
- [x] T057 Create `frontend/src/components/transactions/bulk-actions-bar.tsx` for multi-select operations (FR-013)

### Route Integration for US3

- [x] T058 Create or update `frontend/src/routes/_authenticated/transactions.tsx` with new transaction components

### Tests for US3

- [x] T059 [P] Unit test for TransactionFilterPanel in `frontend/tests/unit/components/transactions/filter-panel.test.tsx`
- [x] T060 [P] Unit test for TransactionRow inline edit in `frontend/tests/unit/components/transactions/transaction-row.test.tsx`
- [ ] T061 E2E test: Transaction search and filter in `frontend/tests/e2e/transactions.spec.ts`

**Checkpoint**: User Story 3 complete - transactions are discoverable and manageable (SC-003 achievable) ✅

---

## Phase 6: User Story 4 - Match Review Workspace (Priority: P2) ✅ COMPLETE

**Goal**: Split-pane comparison view with keyboard navigation for efficient match review

**Independent Test**: Open match queue, approve/reject matches using keyboard, verify batch mode works.

**Acceptance Criteria** (from spec.md):
- Split view with receipt and transaction side-by-side
- AI confidence score and matching factors visible
- Keyboard shortcuts: A=approve, R=reject, arrows for navigation
- Batch mode for threshold-based approval

### Types for US4 (from data-model.md Section 5)

- [x] T062 Create `frontend/src/types/match.ts` with MatchSuggestion, MatchingFactor, MatchReviewState types

### API Integration for US4

- [x] T063 Create TanStack Query hook `frontend/src/hooks/queries/use-matching.ts` for match proposals (per contracts Section 4.1)
- [x] T064 [P] Create approve mutation in `use-matching.ts` with cache invalidation (per contracts Section 4.2)
- [x] T065 [P] Create reject mutation in `use-matching.ts` (per contracts Section 4.3)
- [x] T066 [P] Create manual match mutation in `use-matching.ts` (per contracts Section 4.4)
- [x] T067 [P] Create batch approve mutation in `use-matching.ts` (per contracts Section 4.5)

### Components for US4 (from data-model.md Section 5.2)

- [x] T068 Create `frontend/src/components/matching/match-proposal-card.tsx` with side-by-side display (FR-015, FR-016)
- [x] T069 Create `frontend/src/components/matching/matching-factors.tsx` to highlight why items matched
- [x] T070 Create `frontend/src/components/matching/match-review-workspace.tsx` with keyboard navigation (FR-017, per research.md Section 7)
- [x] T071 Create `frontend/src/components/matching/batch-review-panel.tsx` for threshold-based batch approval (FR-018)
- [x] T072 Create `frontend/src/components/matching/manual-match-dialog.tsx` for user-initiated matching (FR-019)

### Route Integration for US4

- [x] T073 Create or update `frontend/src/routes/_authenticated/matching/index.tsx` with match workspace

### Tests for US4

- [x] T074 [P] Unit test for MatchingFactors in `frontend/tests/unit/components/matching/matching-factors.test.tsx`
- [x] T075 [P] Unit test for MatchReviewWorkspace in `frontend/tests/unit/components/matching/match-review-workspace.test.tsx`
- [x] T076 [P] Unit test for BatchReviewPanel in `frontend/tests/unit/components/matching/batch-review-panel.test.tsx`
- [ ] T076b E2E test: Match review workflow in `frontend/tests/e2e/matching.spec.ts`

**Checkpoint**: User Story 4 complete - matches can be reviewed efficiently (SC-004 achievable) ✅

---

## Phase 7: User Story 5 - Analytics and Reporting (Priority: P3) ✅ COMPLETE

**Goal**: Visual spending breakdowns and exportable expense reports

**Independent Test**: View analytics with sample data, generate and export a report.

**Acceptance Criteria** (from spec.md):
- Spending breakdown visualizations (category treemap, time-series trends)
- Date range selector updates all visualizations
- Formatted report generation with summary and line items
- Export in standard formats

### Types for US5 (from data-model.md Section 6)

- [x] T077 Create `frontend/src/types/analytics.ts` with SpendingTrend, TopMerchant, SubscriptionDetection, AnalyticsDateRange types

### API Integration for US5

- [x] T078 Create consolidated TanStack Query hooks in `frontend/src/hooks/queries/use-analytics.ts` for all analytics endpoints (spending trends, categories, merchants, subscriptions) with date range utilities

### Components for US5 (from data-model.md Section 6.2)

- [x] T079 Create `frontend/src/components/analytics/spending-trend-chart.tsx` using Recharts with area/line/bar chart types (FR-021)
- [x] T080 Create `frontend/src/components/analytics/category-breakdown.tsx` with donut chart, bar chart, and list views (FR-020)
- [x] T081 Create `frontend/src/components/analytics/merchant-analytics.tsx` with trend indicators, search, and sorting (FR-022)
- [x] T082 Create `frontend/src/components/analytics/subscription-detector.tsx` for detected recurring charges with confidence scoring
- [x] T083 Create `frontend/src/components/analytics/date-range-picker.tsx` with presets (week, month, quarter, year) and comparison ranges

### Route Integration for US5

- [x] T084 Update `frontend/src/routes/_authenticated/analytics/index.tsx` with full analytics dashboard integrating all components

### Tests for US5

- [x] T085 Unit test for SpendingTrendChart in `frontend/tests/unit/components/analytics/spending-trend-chart.test.tsx` (13 tests)
- [x] T086 Unit test for CategoryBreakdown in `frontend/tests/unit/components/analytics/category-breakdown.test.tsx` (16 tests)
- [x] T087 Unit test for MerchantAnalytics in `frontend/tests/unit/components/analytics/merchant-analytics.test.tsx` (24 tests)
- [x] T088 Unit test for SubscriptionDetector in `frontend/tests/unit/components/analytics/subscription-detector.test.tsx` (19 tests)
- [ ] T089 E2E test: Analytics view and report generation in `frontend/tests/e2e/analytics.spec.ts`

**Checkpoint**: User Story 5 complete - expense analytics and reports available (SC-005 achievable) ✅

---

## Phase 8: User Story 6 - Responsive Mobile Experience (Priority: P3)

**Goal**: Mobile-optimized interface with touch-friendly controls

**Independent Test**: Access application on mobile device, upload receipt via camera, verify all core functions accessible.

**Acceptance Criteria** (from spec.md):
- Layout adapts for desktop (1024px+), tablet (768px-1023px), mobile (<768px)
- Camera accessible for receipt capture on mobile
- Swipe actions on transactions for quick operations
- Touch targets minimum 44x44 points

### Mobile-Specific Components

- [x] T095 Create `frontend/src/components/mobile/mobile-nav.tsx` for bottom navigation on mobile viewports (FR-025) ✅
- [x] T096 Create `frontend/src/components/mobile/swipe-action-row.tsx` for swipe-to-reveal actions (FR-027) ✅
- [x] T097 Create `frontend/src/components/mobile/camera-capture.tsx` for direct camera receipt capture ✅

### Responsive Adaptations

- [x] T098 Add responsive breakpoints to `frontend/src/components/dashboard/dashboard-layout.tsx` (FR-025) ✅
- [x] T099 [P] Add responsive grid to `frontend/src/components/transactions/transaction-grid.tsx` (FR-026) ✅
- [x] T100 [P] Add responsive layout to `frontend/src/components/matching/match-review-workspace.tsx` (FR-026) ✅
- [x] T101 Update `frontend/src/components/receipts/receipt-upload-dropzone.tsx` to use camera on mobile (spec acceptance scenario 2) ✅
- [x] T102 Apply touch target sizing (44x44pt minimum) across all interactive components (FR-027) ✅

### Tests for US6

- [x] T103 [P] Unit tests for mobile components in `frontend/tests/unit/components/mobile/*.test.tsx` ✅
- [x] T104 E2E test: Mobile viewport interactions in `frontend/tests/e2e/mobile-viewport.spec.ts` (using Playwright device emulation) ✅

**Checkpoint**: ✅ User Story 6 complete - mobile users have full access to core features (SC-011 achievable)

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

### Performance Optimization

- [ ] T105 Implement lazy loading for route components with React.lazy and Suspense
- [ ] T106 [P] Add image optimization for receipt thumbnails
- [ ] T107 [P] Bundle size analysis and optimization (target <20MB per constraints)

### Animation & Micro-Interactions

- [ ] T108 Add Framer Motion page transitions to route changes (per research.md Section 1)
- [ ] T109 [P] Add staggered reveal animations to list components (expense stream, transactions)
- [ ] T110 [P] Add hover states and micro-interactions to interactive elements (FR-030)

### Error Handling & Edge Cases

- [ ] T111 Implement error boundary components for graceful failure handling
- [ ] T112 [P] Add toast notifications for user feedback on mutations
- [ ] T113 [P] Handle session timeout with work preservation (per edge case in spec.md)

### Documentation & Cleanup

- [ ] T114 Run quickstart.md verification checklist
- [ ] T115 [P] Code cleanup: remove unused imports, format with Prettier
- [ ] T116 [P] Update component exports in barrel files

### Final E2E Validation

- [ ] T117 Run full E2E test suite across all user stories
- [ ] T118 Performance audit: verify 3s load time (SC-009) and 100ms feedback (SC-010)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-8)**: All depend on Foundational phase completion
  - P1 stories (US1, US2) should complete before P2 stories (US3, US4)
  - P3 stories (US5, US6) can start after P2 or in parallel if staffed
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### User Story Dependencies

| Story | Priority | Can Start After | Integrates With |
|-------|----------|-----------------|-----------------|
| US1 - Dashboard | P1 | Foundational (Phase 2) | None (standalone) |
| US2 - Receipts | P1 | Foundational (Phase 2) | None (standalone) |
| US3 - Transactions | P2 | Foundational (Phase 2) | US1 for dashboard links |
| US4 - Matching | P2 | Foundational (Phase 2) | US2, US3 for data |
| US5 - Analytics | P3 | Foundational (Phase 2) | US3 for transaction data |
| US6 - Mobile | P3 | Foundational (Phase 2) | All stories (adds responsiveness) |

### Within Each User Story

1. Types before API hooks
2. API hooks before components
3. Components before route integration
4. Core implementation before tests

### Parallel Opportunities

**Phase 2 (Foundational)** - Can run in parallel:
```bash
# Launch all design token tasks together:
Task: T008 Create design-tokens.ts
Task: T010 Add CSS custom properties
Task: T011 Create animations.ts

# Launch all hooks together:
Task: T012 use-undo.ts
Task: T013 use-keyboard-shortcuts.ts
Task: T014 use-polling.ts

# Launch all base components together:
Task: T016 stat-card.tsx
Task: T017 empty-state.tsx
Task: T018 loading-skeleton.tsx
Task: T019 confidence-glow.css
```

**Phase 3 (US1)** - Can run in parallel:
```bash
# Launch all API hooks together:
Task: T024 use-expense-stream.ts
Task: T025 use-action-queue.ts

# Launch unit tests together:
Task: T033 metrics-row.test.tsx
Task: T034 expense-stream.test.tsx
```

**Cross-Story Parallelism** - After Foundational completes:
```bash
# Developer A: User Story 1 (Dashboard)
# Developer B: User Story 2 (Receipts)
# Both can work simultaneously - no shared dependencies
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 - Dashboard (P1)
4. Complete Phase 4: User Story 2 - Receipts (P1)
5. **STOP and VALIDATE**: Test both stories independently
6. Deploy/demo if ready - users can view expenses and upload receipts

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add US1 (Dashboard) → Test → Deploy (visibility MVP!)
3. Add US2 (Receipts) → Test → Deploy (data entry complete)
4. Add US3 (Transactions) → Test → Deploy (full exploration)
5. Add US4 (Matching) → Test → Deploy (reconciliation)
6. Add US5 (Analytics) → Test → Deploy (insights)
7. Add US6 (Mobile) → Test → Deploy (mobile access)
8. Each story adds value without breaking previous stories

### Parallel Team Strategy

With 2-3 developers after Foundational:

| Developer | Week 1 | Week 2 | Week 3 |
|-----------|--------|--------|--------|
| Dev A | US1 Dashboard | US3 Transactions | US5 Analytics |
| Dev B | US2 Receipts | US4 Matching | US6 Mobile |
| Dev C | Foundational support | Tests + Polish | Performance |

---

## Notes

- [P] tasks = different files, no dependencies - can run in parallel
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Design system components (Phase 2) are shared across all stories
- Mobile responsiveness (US6) can be incrementally added to existing components
