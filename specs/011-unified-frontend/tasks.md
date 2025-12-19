# Tasks: Unified Frontend Experience

**Input**: Design documents from `/specs/011-unified-frontend/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/api-endpoints.md, quickstart.md

**Tests**: Tests are NOT explicitly requested in spec.md. Test tasks are omitted but can be added on request.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

**Existing Functionality**: The Statements import page (`/statements`) already exists in the current frontend and will be integrated into the new navigation without reimplementation. See `frontend/src/` for existing statement components.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Web app frontend**: `frontend/src/`
- **Tests**: `tests/` at repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, dependencies, and configuration

- [X] T001 Install TanStack Router and related dependencies per quickstart.md in frontend/package.json
- [X] T002 Install TanStack Query dependencies in frontend/package.json
- [X] T002a Install recharts for analytics visualizations in frontend/package.json
- [X] T003 Install Tailwind CSS, PostCSS, and Autoprefixer per quickstart.md in frontend/package.json
- [X] T004 Initialize Tailwind CSS configuration in frontend/tailwind.config.ts
- [X] T005 Initialize shadcn/ui with New York style in frontend/components.json
- [X] T006 Update Vite configuration with TanStack Router plugin in frontend/vite.config.ts
- [X] T007 Update TypeScript configuration with path aliases in frontend/tsconfig.json
- [X] T008 Create globals.css with Tailwind directives and shadcn theme variables in frontend/src/globals.css

**Checkpoint**: Dependencies installed, build tools configured

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**CRITICAL**: No user story work can begin until this phase is complete

### Type Definitions

- [X] T009 [P] Create API response types (Receipt, Transaction, Match) in frontend/src/types/api.ts
- [X] T010 [P] Create route-specific types and search param schemas in frontend/src/types/routes.ts

### Core Infrastructure

- [X] T011 Create QueryClient configuration with stale times in frontend/src/lib/query-client.ts
- [X] T012 Create utility functions (cn, formatters) in frontend/src/lib/utils.ts
- [X] T013 Create API client with MSAL token acquisition in frontend/src/services/api.ts
- [X] T014 Create router configuration with context types in frontend/src/router.ts

### shadcn/ui Base Components

- [X] T015 [P] Add shadcn button component via CLI to frontend/src/components/ui/button.tsx
- [X] T016 [P] Add shadcn card component via CLI to frontend/src/components/ui/card.tsx
- [X] T017 [P] Add shadcn sidebar component via CLI to frontend/src/components/ui/sidebar.tsx
- [X] T018 [P] Add shadcn skeleton component via CLI to frontend/src/components/ui/skeleton.tsx
- [X] T019 [P] Add shadcn alert component via CLI to frontend/src/components/ui/alert.tsx
- [X] T020 [P] Add shadcn toast/sonner component via CLI to frontend/src/components/ui/sonner.tsx

### Root Routes

- [X] T021 Create root route with router context in frontend/src/routes/__root.tsx
- [X] T022 Create authenticated layout route with beforeLoad auth guard (minimal shell, Outlet only) in frontend/src/routes/_authenticated.tsx
- [X] T023 Create login route for unauthenticated access in frontend/src/routes/login.tsx
- [X] T024 Update main.tsx with providers (MSAL, QueryClient, Router) in frontend/src/main.tsx

**Note**: T022 creates minimal auth guard with just `<Outlet/>`; T039 (Phase 3) enhances with full AuthenticatedLayout after layout components are built.

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Dashboard and Navigation (Priority: P1) MVP

**Goal**: Central dashboard and navigation system for accessing all expense tracking features

**Independent Test**: Sign in, view dashboard summary cards, navigate to each section via sidebar, verify breadcrumbs

### shadcn/ui Components for US1

- [X] T025 [P] [US1] Add shadcn breadcrumb component via CLI to frontend/src/components/ui/breadcrumb.tsx
- [X] T026 [P] [US1] Add shadcn dropdown-menu component via CLI to frontend/src/components/ui/dropdown-menu.tsx
- [X] T027 [P] [US1] Add shadcn avatar component via CLI to frontend/src/components/ui/avatar.tsx
- [X] T028 [P] [US1] Add shadcn separator component via CLI to frontend/src/components/ui/separator.tsx

### Layout Components

- [X] T029 [P] [US1] Create AppSidebar component with nav items in frontend/src/components/layout/app-sidebar.tsx
- [X] T030 [P] [US1] Create Header component with user menu in frontend/src/components/layout/header.tsx
- [X] T031 [P] [US1] Create Breadcrumb wrapper component in frontend/src/components/layout/breadcrumbs.tsx
- [X] T032 [US1] Create AuthenticatedLayout component combining sidebar, header, breadcrumbs in frontend/src/components/layout/authenticated-layout.tsx

### Dashboard Components

- [X] T033 [P] [US1] Create SummaryCard component for dashboard metrics in frontend/src/components/dashboard/summary-card.tsx
- [X] T034 [P] [US1] Create QuickActions component for common tasks in frontend/src/components/dashboard/quick-actions.tsx
- [X] T035 [P] [US1] Create RecentActivity component for activity feed in frontend/src/components/dashboard/recent-activity.tsx

### API & Hooks

- [X] T036 [P] [US1] Create dashboard API functions (summary, activity) in frontend/src/services/dashboard.ts
- [X] T037 [US1] Create useDashboard hooks with queryOptions in frontend/src/hooks/use-dashboard.ts

### Routes

- [X] T038 [US1] Create dashboard index route with loader in frontend/src/routes/_authenticated/index.tsx
- [X] T039 [US1] Update _authenticated.tsx to use AuthenticatedLayout component
- [X] T039a [US1] Create statements route integrating existing import flow in frontend/src/routes/_authenticated/statements/index.tsx

**Checkpoint**: Dashboard displays summary cards, navigation works (including Statements link), breadcrumbs show location (FR-001 to FR-007)

---

## Phase 4: User Story 2 - Receipt Management (Priority: P1)

**Goal**: Upload, view, and manage receipts for expense documentation

**Independent Test**: Upload receipt image, view in list with thumbnail, click for detail view, delete receipt

### shadcn/ui Components for US2

- [X] T040 [P] [US2] Add shadcn input component via CLI to frontend/src/components/ui/input.tsx
- [X] T041 [P] [US2] Add shadcn dialog component via CLI to frontend/src/components/ui/dialog.tsx
- [X] T042 [P] [US2] Add shadcn progress component via CLI to frontend/src/components/ui/progress.tsx
- [X] T043 [P] [US2] Add shadcn badge component via CLI to frontend/src/components/ui/badge.tsx
- [X] T044 [P] [US2] Add shadcn alert-dialog component via CLI to frontend/src/components/ui/alert-dialog.tsx

### Receipt Components

- [X] T045 [P] [US2] Create ReceiptUploader component with drag-drop in frontend/src/components/receipts/receipt-uploader.tsx
- [X] T046 [P] [US2] Create ReceiptCard component for list item display in frontend/src/components/receipts/receipt-card.tsx
- [X] T047 [P] [US2] Create ReceiptList component with filtering in frontend/src/components/receipts/receipt-list.tsx
- [X] T048 [P] [US2] Create ReceiptDetail component showing image and data in frontend/src/components/receipts/receipt-detail.tsx
- [X] T049 [US2] Create DeleteReceiptDialog confirmation component in frontend/src/components/receipts/delete-receipt-dialog.tsx

### API & Hooks

- [X] T050 [P] [US2] Create receipts API functions (list, get, upload, delete) in frontend/src/services/receipts.ts
- [X] T051 [US2] Create useReceipts hooks with mutations in frontend/src/hooks/use-receipts.ts

### Routes

- [X] T052 [US2] Create receipts list route with search params in frontend/src/routes/_authenticated/receipts/index.tsx
- [X] T053 [US2] Create receipt detail route with loader in frontend/src/routes/_authenticated/receipts/$receiptId.tsx

**Checkpoint**: Receipts can be uploaded, viewed in list, clicked for detail, and deleted (FR-008 to FR-013)

---

## Phase 5: User Story 3 - Transaction List and Search (Priority: P1)

**Goal**: View imported transactions with filtering and search capabilities

**Independent Test**: View transaction list, apply date/category filters, search by merchant, click to see receipt link

### shadcn/ui Components for US3

- [X] T054 [P] [US3] Add shadcn table component via CLI to frontend/src/components/ui/table.tsx
- [X] T055 [P] [US3] Add shadcn select component via CLI to frontend/src/components/ui/select.tsx
- [X] T056 [P] [US3] Add shadcn label component via CLI to frontend/src/components/ui/label.tsx

### Transaction Components

- [X] T057 [P] [US3] Create TransactionFilters component with date/category/amount in frontend/src/components/transactions/transaction-filters.tsx
- [X] T058 [P] [US3] Create TransactionTable component with sortable columns in frontend/src/components/transactions/transaction-table.tsx
- [X] T059 [P] [US3] Create TransactionRow component with match indicator in frontend/src/components/transactions/transaction-row.tsx
- [X] T060 [US3] Create TransactionDetail component with linked receipt in frontend/src/components/transactions/transaction-detail.tsx
- [X] T061 [US3] Create TransactionSearch component with debounce in frontend/src/components/transactions/transaction-search.tsx

### API & Hooks

- [X] T062 [P] [US3] Create transactions API functions (list, get, delete) in frontend/src/services/transactions.ts
- [X] T063 [US3] Create useTransactions hooks with search params in frontend/src/hooks/use-transactions.ts

### Routes

- [X] T064 [US3] Create transactions list route with Zod search validation (page/pageSize params) in frontend/src/routes/_authenticated/transactions/index.tsx
- [X] T065 [US3] Create transaction detail route with loader in frontend/src/routes/_authenticated/transactions/$transactionId.tsx

**Checkpoint**: Transactions display with filters, search works with highlighting, detail shows receipt link (FR-014 to FR-018)

---

## Phase 6: User Story 4 - Match Review Interface (Priority: P2)

**Goal**: Review AI-suggested matches and manually match receipts to transactions

**Independent Test**: View pending matches, confirm a suggestion, reject a suggestion, manually match an unmatched receipt

### shadcn/ui Components for US4

- [X] T066 [P] [US4] Add shadcn tooltip component via CLI to frontend/src/components/ui/tooltip.tsx
- [X] T067 [P] [US4] Add shadcn scroll-area component via CLI to frontend/src/components/ui/scroll-area.tsx

### Matching Components

- [X] T068 [P] [US4] Create MatchProposalCard component with confidence score in frontend/src/components/matching/match-proposal-card.tsx
- [X] T069 [P] [US4] Create MatchProposalList component with pagination in frontend/src/components/matching/match-proposal-list.tsx
- [X] T070 [P] [US4] Create ConfirmMatchButton with optimistic update in frontend/src/components/matching/confirm-match-button.tsx
- [X] T071 [P] [US4] Create RejectMatchButton with optimistic update in frontend/src/components/matching/reject-match-button.tsx
- [X] T072 [US4] Create ManualMatchDialog with transaction search in frontend/src/components/matching/manual-match-dialog.tsx
- [X] T073 [US4] Create MatchStatsSummary component for overview in frontend/src/components/matching/match-stats-summary.tsx

### API & Hooks

- [X] T074 [P] [US4] Create matching API functions (proposals, confirm, reject, manual) in frontend/src/services/matching.ts
- [X] T075 [US4] Create useMatching hooks with optimistic mutations (SC-008) in frontend/src/hooks/use-matching.ts

### Routes

- [X] T076 [US4] Create matching review route with stats loader in frontend/src/routes/_authenticated/matching/index.tsx

**Checkpoint**: Match proposals display, confirm/reject work with optimistic UI, manual match dialog functional (FR-019 to FR-023, SC-008)

---

## Phase 7: User Story 5 - Expense Report Generation (Priority: P2)

**Goal**: Generate expense reports for specific date ranges with PDF/Excel export

**Independent Test**: Configure report parameters, preview transactions, generate report, download PDF and Excel

### shadcn/ui Components for US5

- [X] T077 [P] [US5] Add shadcn tabs component via CLI to frontend/src/components/ui/tabs.tsx
- [X] T078 [P] [US5] Add shadcn checkbox component via CLI to frontend/src/components/ui/checkbox.tsx
- [X] T079 [P] [US5] Add shadcn textarea component via CLI to frontend/src/components/ui/textarea.tsx

### Report Components

- [X] T080 [P] [US5] Create ReportConfigForm component with date/category selection in frontend/src/components/reports/report-config-form.tsx
- [X] T081 [P] [US5] Create ReportPreview component showing included transactions in frontend/src/components/reports/report-preview.tsx
- [X] T082 [P] [US5] Create ReportCard component for history list in frontend/src/components/reports/report-card.tsx
- [X] T083 [US5] Create ReportExportButtons component (PDF/Excel) in frontend/src/components/reports/report-export-buttons.tsx
- [X] T084 [US5] Create ReportLineEditor component for expense line edits in frontend/src/components/reports/report-line-editor.tsx

### API & Hooks

- [X] T085 [P] [US5] Create reports API functions (list, create, get, export) in frontend/src/services/reports.ts
- [X] T086 [US5] Create useReports hooks with download handling in frontend/src/hooks/use-reports.ts

### Routes

- [X] T087 [US5] Create reports list route with history in frontend/src/routes/_authenticated/reports/index.tsx
- [X] T088 [US5] Create new report route with config form in frontend/src/routes/_authenticated/reports/new.tsx
- [X] T089 [US5] Create report detail route with preview/edit in frontend/src/routes/_authenticated/reports/$reportId.tsx

**Checkpoint**: Reports can be configured, previewed, generated, and downloaded as PDF/Excel (FR-024 to FR-028, SC-006)

---

## Phase 8: User Story 6 - Analytics Dashboard (Priority: P3)

**Goal**: Spending analytics and trends visualization

**Independent Test**: View spending by category chart, see month-over-month trends, drill down to transactions

### Analytics Components

- [X] T090 [P] [US6] Create SpendingByCategory pie/bar chart component in frontend/src/components/analytics/spending-by-category.tsx
- [X] T091 [P] [US6] Create SpendingTrends line chart component in frontend/src/components/analytics/spending-trends.tsx
- [X] T092 [US6] Create CategoryDrilldown component with transaction list in frontend/src/components/analytics/category-drilldown.tsx
- [X] T093 [US6] Create AnalyticsSummary component with key metrics in frontend/src/components/analytics/analytics-summary.tsx

### API & Hooks

- [X] T094 [P] [US6] Create analytics API functions (comparison, cache-stats) in frontend/src/services/analytics.ts
- [X] T095 [US6] Create useAnalytics hooks with period params in frontend/src/hooks/use-analytics.ts

### Routes

- [X] T096 [US6] Create analytics route with chart visualizations in frontend/src/routes/_authenticated/analytics.tsx

**Checkpoint**: Analytics displays category breakdown, trends, and drill-down functionality (FR-029 to FR-031)

---

## Phase 9: User Story 7 - Settings and Preferences (Priority: P3)

**Goal**: Account settings, fingerprint management, and customization

**Independent Test**: View profile info, manage fingerprints (view/rename/delete), modify preferences

### Settings Components

- [X] T097 [P] [US7] Create ProfileCard component showing Entra ID info in frontend/src/components/settings/profile-card.tsx
- [X] T098 [P] [US7] Create FingerprintList component with management actions in frontend/src/components/settings/fingerprint-list.tsx
- [X] T099 [US7] Create FingerprintActions component (rename, delete) in frontend/src/components/settings/fingerprint-actions.tsx
- [X] T100 [US7] Create PreferencesForm component for user preferences in frontend/src/components/settings/preferences-form.tsx

### API & Hooks

- [X] T101 [P] [US7] Create reference data API (gl-codes, departments) in frontend/src/services/reference.ts
- [X] T102 [P] [US7] Create user API functions (me, preferences) in frontend/src/services/user.ts
- [X] T103 [P] [US7] Create statements API functions (fingerprints) in frontend/src/services/statements.ts
- [X] T104 [US7] Create useSettings hooks combining user and reference in frontend/src/hooks/use-settings.ts

### Routes

- [X] T105 [US7] Create settings route with tabs for profile/fingerprints/preferences in frontend/src/routes/_authenticated/settings.tsx

**Checkpoint**: Settings page shows profile, allows fingerprint management, preferences save correctly (FR-032 to FR-033)

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

### Error Handling (FR-034, FR-035)

- [X] T106 Create LayoutErrorBoundary component in frontend/src/components/layout/error-boundary.tsx
- [X] T107 Add errorComponent to all route files for graceful error display
- [X] T108 Create NotFound component for 404 routes in frontend/src/components/layout/not-found.tsx

### Loading States

- [X] T109 [P] Create PageSkeleton component for route loading in frontend/src/components/layout/page-skeleton.tsx
- [X] T110 [P] Create TableSkeleton component for list loading in frontend/src/components/layout/table-skeleton.tsx
- [X] T111 Add pendingComponent to routes with data loaders

### Empty States

- [X] T112 [P] Create EmptyState component for empty lists in frontend/src/components/shared/empty-state.tsx
- [X] T113 Add empty state handling to all list components

### Responsive Design (SC-007)

- [X] T114 Verify sidebar collapses correctly on mobile (320px+)
- [ ] T115 Test all pages on viewports from 320px to 2560px
- [X] T116 Add useIsMobile hook if not provided by shadcn in frontend/src/hooks/use-mobile.ts

### Performance

- [X] T117 Verify TanStack Query stale times match performance goals (SC-003, SC-004)
- [X] T118 Add debounce to search inputs (500ms per SC-004)
- [X] T119 Verify route code-splitting via Vite bundle analysis

### Accessibility (FR-004a)

- [ ] T120 Run axe-core accessibility audit on all routes and fix violations in frontend/src/
- [ ] T121 Verify keyboard navigation for all interactive elements (sidebar, forms, dialogs)
- [ ] T122 Test with screen reader (NVDA/VoiceOver) on critical flows (login, upload, match review)

### Final Validation

- [ ] T123 Run quickstart.md verification checklist
- [ ] T124 Verify all navigation within 2 clicks (SC-001)
- [ ] T125 Test match review workflow under 3 clicks (SC-005)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-9)**: All depend on Foundational phase completion
  - P1 stories (US1, US2, US3) should complete before P2 stories
  - P2 stories (US4, US5) should complete before P3 stories
  - Within same priority, stories can proceed in parallel
- **Polish (Phase 10)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational - No dependencies on other stories
- **US2 (P1)**: Can start after Foundational - Independent of US1
- **US3 (P1)**: Can start after Foundational - Independent of US1, US2
- **US4 (P2)**: Can start after Foundational - Uses receipt/transaction data from US2/US3 but independently testable
- **US5 (P2)**: Can start after Foundational - Uses transaction data from US3 but independently testable
- **US6 (P3)**: Can start after Foundational - Uses analytics API, independently testable
- **US7 (P3)**: Can start after Foundational - Uses user/reference APIs, independently testable

### Within Each User Story

- shadcn components [P] can run in parallel
- Custom components [P] can run in parallel
- API services before hooks
- Components before routes
- Routes integrate all pieces

### Parallel Opportunities

- All Setup tasks can run sequentially (package.json updates best done together)
- All Foundational type tasks (T009, T010) can run in parallel
- All shadcn component additions within a phase can run in parallel
- Custom components within a story can run in parallel if no dependencies
- Different user stories can be worked on in parallel by different developers

---

## Parallel Example: User Story 2

```bash
# Launch all shadcn components together:
Task: "T040 [P] [US2] Add shadcn input component via CLI"
Task: "T041 [P] [US2] Add shadcn dialog component via CLI"
Task: "T042 [P] [US2] Add shadcn progress component via CLI"
Task: "T043 [P] [US2] Add shadcn badge component via CLI"
Task: "T044 [P] [US2] Add shadcn alert-dialog component via CLI"

# Then launch custom components together:
Task: "T045 [P] [US2] Create ReceiptUploader component"
Task: "T046 [P] [US2] Create ReceiptCard component"
Task: "T047 [P] [US2] Create ReceiptList component"
Task: "T048 [P] [US2] Create ReceiptDetail component"
```

---

## Implementation Strategy

### MVP First (Phase 1-3: Setup + Foundational + US1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 - Dashboard and Navigation
4. **STOP and VALIDATE**: Test navigation and dashboard independently
5. Deploy/demo if ready - users can access the application shell

### P1 Delivery (Add US2, US3)

1. Complete Phase 4: User Story 2 - Receipt Management
2. Complete Phase 5: User Story 3 - Transaction List
3. **VALIDATE**: Full receipt and transaction workflows functional
4. Deploy - core expense tracking functionality complete

### P2 Delivery (Add US4, US5)

1. Complete Phase 6: User Story 4 - Match Review
2. Complete Phase 7: User Story 5 - Report Generation
3. **VALIDATE**: Matching and report workflows complete
4. Deploy - expense documentation and reporting functional

### P3 Delivery (Add US6, US7)

1. Complete Phase 8: User Story 6 - Analytics
2. Complete Phase 9: User Story 7 - Settings
3. Complete Phase 10: Polish
4. **VALIDATE**: All features complete with polish
5. Deploy - full unified frontend experience

### Parallel Team Strategy

With multiple developers after Foundational is done:

- Developer A: User Story 1 (Dashboard/Navigation)
- Developer B: User Story 2 (Receipts)
- Developer C: User Story 3 (Transactions)

Then rotate to P2/P3 stories as P1 completes.

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- shadcn components retrieved via CLI during implementation
- All routes use TanStack Router file-based routing conventions
- Query hooks use TanStack Query with optimistic updates for mutations
