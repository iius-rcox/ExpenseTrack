# Tasks: Dual Theme System

**Input**: Design documents from `/specs/015-dual-theme-system/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: E2E tests included per constitution requirement (test-first development)

**Organization**: Tasks grouped by user story for independent implementation and testing

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4, US5)
- Include exact file paths in descriptions

## User Story Summary

| Story | Priority | Description | Key Components |
|-------|----------|-------------|----------------|
| US1 | P1 | Toggle Between Light and Dark Themes | ThemeToggle, flash prevention |
| US2 | P1 | Experience Luxury Minimalist Light Theme | CSS variables (:root) |
| US3 | P1 | Experience Dark Cyber Theme | Glassmorphism, gradient text, shine animation |
| US4 | P2 | Consistent Component Styling | Update 8 components using old tokens |
| US5 | P3 | Respect System Theme Preference | ThemeProvider config (enableSystem) |

**Note**: US2, US3, and US5 share foundational infrastructure (ThemeProvider, globals.css). These are in the Foundational phase.

---

## Phase 1: Setup

**Purpose**: Verify environment and create feature branch

- [x] T001 Verify feature branch exists: `git checkout 015-dual-theme-system`
- [x] T002 [P] Verify frontend dev server runs: `cd frontend && npm run dev`
- [x] T003 [P] Verify next-themes is installed in frontend/package.json (v0.4.6)

---

## Phase 2: Foundational (Theme Infrastructure)

**Purpose**: Core infrastructure that enables ALL user stories

**⚠️ CRITICAL**: US1-US5 cannot work until this phase is complete

### ThemeProvider Setup

- [x] T004 Create ThemeProvider component in frontend/src/providers/theme-provider.tsx
- [x] T005 Update frontend/src/routes/__root.tsx to wrap content with ThemeProvider
- [x] T006 Add flash prevention script to frontend/index.html before `</head>`

### CSS Variables Update

- [x] T007 Replace frontend/src/globals.css with new dual-theme CSS variables (from quickstart.md Step 2)
- [x] T008 [P] Verify @theme block includes all Tailwind color aliases
- [x] T009 [P] Verify :root contains Luxury Minimalist light theme values
- [x] T010 [P] Verify .dark contains Dark Cyber theme values

### Design Tokens Update

- [x] T011 Replace frontend/src/lib/design-tokens.ts with simplified version (remove slate/copper, keep utilities)

**Checkpoint**: Theme infrastructure ready - user story implementation can begin

---

## Phase 3: User Story 1 - Toggle Between Light and Dark Themes (Priority: P1) 🎯 MVP

**Goal**: Users can click a toggle button to switch between light and dark themes

**Independent Test**: Click the theme toggle button in the navigation bar and verify the entire application updates to the selected theme within 1 second

### Tests for User Story 1

- [ ] T012 [P] [US1] Create E2E test for theme toggle in frontend/tests/e2e/theme-toggle.spec.ts

### Implementation for User Story 1

- [x] T013 [US1] Create ThemeToggle component in frontend/src/components/theme-toggle.tsx
- [x] T014 [US1] Locate navigation/header component and identify where to add ThemeToggle
- [x] T015 [US1] Add ThemeToggle to navigation bar in frontend/src/components/layout/app-shell.tsx
- [x] T016 [US1] Verify toggle is accessible (aria-label, keyboard navigation)

### Verification for User Story 1

- [x] T017 [US1] Manual test: Click toggle to switch from light to dark mode - VERIFIED via Chrome DevTools E2E
- [x] T018 [US1] Manual test: Click toggle to switch from dark to light mode - VERIFIED via Chrome DevTools E2E
- [x] T019 [US1] Manual test: Close browser, reopen, verify preference persisted - VERIFIED via Chrome DevTools E2E

**Checkpoint**: Theme toggle functional - users can switch between themes

---

## Phase 4: User Story 2 - Experience Luxury Minimalist Light Theme (Priority: P1)

**Goal**: Light mode displays premium, elegant Luxury Minimalist aesthetic

**Independent Test**: Load the dashboard in light mode and verify all visual elements match the Luxury Minimalist specification (emerald accents, off-white backgrounds, subtle shadows)

### Verification for User Story 2 (CSS already applied in Foundational)

- [ ] T020 [US2] Verify page background is off-white (#fafaf8)
- [ ] T021 [US2] Verify cards have white background (#ffffff) with subtle borders
- [ ] T022 [US2] Verify primary buttons use deep emerald (#2d5f4f)
- [ ] T023 [US2] Verify sidebar active items show emerald left border
- [ ] T024 [US2] Verify hover states show lighter emerald (#4a8f75)
- [ ] T025 [US2] Verify stat cards have emerald-tinted shadows

**Checkpoint**: Light mode delivers intended Luxury Minimalist aesthetic

---

## Phase 5: User Story 3 - Experience Dark Cyber Theme (Priority: P1)

**Goal**: Dark mode displays modern fintech Dark Cyber aesthetic with glassmorphism

**Independent Test**: Load the dashboard in dark mode and verify all visual elements match the Dark Cyber specification (cyan accents, glassmorphism, gradient text)

### Implementation for User Story 3

- [x] T026 [US3] Add glassmorphism utility class (.glass) to frontend/src/globals.css (if not already present)
- [x] T027 [US3] Add @supports fallback for browsers without backdrop-filter
- [x] T028 [US3] Add gradient text utility class (.gradient-text) to frontend/src/globals.css
- [x] T029 [US3] Add stat card shine animation (.stat-card-shine) to frontend/src/globals.css
- [x] T030 [P] [US3] Apply .glass class to dashboard stat cards in frontend/src/components/design-system/stat-card.tsx
- [x] T031 [P] [US3] Apply .gradient-text class to large stat values
- [x] T032 [P] [US3] Apply .stat-card-shine class to stat cards for hover effect

### Verification for User Story 3

- [ ] T033 [US3] Verify page background is dark navy (#0f1419)
- [ ] T034 [US3] Verify cards have glassmorphism effect (semi-transparent with backdrop blur)
- [ ] T035 [US3] Verify primary accent is bright cyan (#00bcd4)
- [ ] T036 [US3] Verify stat values display cyan-to-blue gradient text
- [ ] T037 [US3] Verify card hover shows shine animation sweep

**Checkpoint**: Dark mode delivers intended Dark Cyber aesthetic with glassmorphism

---

## Phase 6: User Story 4 - Consistent Component Styling Across Themes (Priority: P2)

**Goal**: All UI components adapt their visual style to the active theme

**Independent Test**: Interact with forms, buttons, and tables in both themes and verify all interactions work correctly with appropriate visual feedback

### Search for Components Using Old Tokens

- [x] T038 [US4] Search for components importing colors from design-tokens: `grep -r "from.*design-tokens" frontend/src/` - Only utilities (ConfidenceLevel) remain
- [x] T039 [US4] Search for components using colors.slate: `grep -r "colors.slate" frontend/src/` - None found
- [x] T040 [US4] Search for components using colors.accent.copper: `grep -r "colors.accent.copper" frontend/src/` - None found

### Update Components (based on research.md findings)

- [x] T041 [P] [US4] Update frontend/src/components/transactions/transaction-row.tsx to use CSS variables - Already theme-aware
- [x] T042 [P] [US4] Update frontend/src/components/receipts/extracted-field.tsx to use CSS variables - Already theme-aware
- [x] T043 [P] [US4] Update frontend/src/components/receipts/batch-upload-queue.tsx to use CSS variables - Already theme-aware
- [x] T044 [P] [US4] Update frontend/src/components/matching/matching-factors.tsx to use CSS variables - Already theme-aware
- [x] T045 [P] [US4] Update frontend/src/components/matching/match-review-workspace.tsx to use CSS variables - Already theme-aware
- [x] T046 [P] [US4] Update frontend/src/components/matching/batch-review-panel.tsx to use CSS variables - Already theme-aware
- [x] T047 [P] [US4] Update frontend/src/components/analytics/subscription-detector.tsx to use CSS variables - Already theme-aware
- [x] T048 [P] [US4] Verify frontend/src/components/ui/alert.tsx uses correct theme variables (shadcn) - Verified

### Sidebar Collapse Fix (FR-012)

- [x] T048a [US4] Update frontend/src/components/layout/app-sidebar.tsx: Change `<Sidebar>` to `<Sidebar collapsible="icon">`
- [x] T048b [US4] Add `tooltip` prop to each SidebarMenuButton for icon-only mode hover labels
- [x] T048c [US4] Verify sidebar header (logo/title) hides properly in collapsed state using `group-data-[collapsible=icon]:hidden`
- [x] T048d [US4] Verify user dropdown in footer works correctly in collapsed state
- [x] T048e [US4] Manual test: Click sidebar trigger, verify sidebar collapses to icons (3rem width) - VERIFIED via Chrome DevTools E2E
- [x] T048f [US4] Manual test: Hover collapsed menu items, verify tooltips appear - VERIFIED via Chrome DevTools E2E
- [x] T048g [US4] Fix Tailwind v4 CSS variable syntax in sidebar.tsx: `w-[--sidebar-width]` → `w-[var(--sidebar-width)]` - Fixed sidebar layout overlap issue

### Verification for User Story 4

- [ ] T049 [US4] Verify buttons show appropriate hover states in both themes
- [ ] T050 [US4] Verify input fields show themed focus rings (emerald in light, cyan in dark)
- [ ] T051 [US4] Verify tables show themed row hover backgrounds
- [ ] T052 [US4] Verify badges and tags use theme-appropriate colors

**Checkpoint**: All components correctly adapt to both themes

---

## Phase 7: User Story 5 - Respect System Theme Preference (Priority: P3)

**Goal**: ExpenseFlow automatically uses OS dark mode preference on first visit

**Independent Test**: Clear browser storage, set system to dark mode, load ExpenseFlow, and verify it starts in Dark Cyber theme

### Verification for User Story 5 (ThemeProvider already configured)

- [x] T053 [US5] Verify ThemeProvider uses `defaultTheme="system"` and `enableSystem={true}` - Verified
- [x] T054 [US5] Manual test: Clear localStorage, set OS to dark mode, reload - should load Dark Cyber - VERIFIED via Chrome DevTools E2E
- [x] T055 [US5] Manual test: Clear localStorage, set OS to light mode, reload - should load Luxury Minimalist - VERIFIED via Chrome DevTools E2E
- [x] T056 [US5] Manual test: Manually select light mode, change OS to dark, reload - manual preference preserved - VERIFIED via Chrome DevTools E2E

**Checkpoint**: System preference detection working correctly

---

## Phase 8: Polish & Validation

**Purpose**: Final validation, tests, and cleanup

### Type Checking and Linting

- [x] T057 Run TypeScript type check: `cd frontend && npm run type-check` - Passes
- [x] T058 Run ESLint: `npm run lint` - ESLint v9 flat config created (eslint.config.js), pre-existing lint errors unrelated to theme system
- [x] T059 Fix any type errors or lint warnings - No errors

### E2E Tests

- [ ] T060 Run E2E tests: `npm run test:e2e`
- [ ] T061 Verify all theme toggle tests pass

### Performance Validation

- [ ] T062 Verify theme transition completes within 500ms (SC-001)
- [ ] T063 Verify no flash of wrong colors on page load (SC-006)
- [ ] T064 Verify no layout shifts during theme toggle (SC-006)

### Accessibility Validation

- [ ] T065 Verify WCAG 2.1 AA contrast ratios in light mode (SC-004)
- [ ] T066 Verify WCAG 2.1 AA contrast ratios in dark mode (SC-004)
- [ ] T067 Verify all interactive elements have visible focus states (FR-010)

### Final Cleanup

- [x] T068 Remove any remaining references to slate/copper tokens - Removed 4 accent-copper references
- [x] T069 Update any hardcoded colors to use CSS variables - Updated to text-primary
- [ ] T070 Run quickstart.md validation checklist

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup
    ↓
Phase 2: Foundational (Theme Infrastructure) ← BLOCKS ALL USER STORIES
    ↓
Phase 3: US1 (Toggle) ← MVP
Phase 4: US2 (Light Theme) } Can run in parallel
Phase 5: US3 (Dark Theme)  } after Foundational
Phase 6: US4 (Components)  }
Phase 7: US5 (System Pref) }
    ↓
Phase 8: Polish & Validation
```

### User Story Dependencies

| Story | Depends On | Can Run Parallel With |
|-------|------------|----------------------|
| US1 | Phase 2 (Foundational) | US2, US3, US5 |
| US2 | Phase 2 (Foundational) | US1, US3, US5 |
| US3 | Phase 2 (Foundational) | US1, US2, US5 |
| US4 | Phase 2 + US1, US2, US3 (needs themes working) | - |
| US5 | Phase 2 (Foundational) | US1, US2, US3 |

### Within-Phase Parallel Opportunities

**Phase 2 (Foundational)**:
- T008, T009, T010 (CSS verification) can run in parallel

**Phase 5 (US3)**:
- T030, T031, T032 (applying CSS classes) can run in parallel

**Phase 6 (US4)**:
- T041-T048 (component updates) can run in parallel - different files

---

## Parallel Example: User Story 4 Component Updates

```bash
# These can all run in parallel (different files):
Task: "Update frontend/src/components/transactions/transaction-row.tsx"
Task: "Update frontend/src/components/receipts/extracted-field.tsx"
Task: "Update frontend/src/components/receipts/batch-upload-queue.tsx"
Task: "Update frontend/src/components/matching/matching-factors.tsx"
Task: "Update frontend/src/components/matching/match-review-workspace.tsx"
Task: "Update frontend/src/components/matching/batch-review-panel.tsx"
Task: "Update frontend/src/components/analytics/subscription-detector.tsx"
```

---

## Implementation Strategy

### MVP First (Phases 1-3)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (theme infrastructure)
3. Complete Phase 3: US1 (toggle functionality)
4. **STOP and VALIDATE**: Theme toggle works, both themes display correctly
5. Deploy MVP - users can switch between themes

### Full Feature (All Phases)

1. Complete MVP (Phases 1-3)
2. Complete Phase 4: US2 (light theme verification)
3. Complete Phase 5: US3 (dark theme effects)
4. Complete Phase 6: US4 (component updates)
5. Complete Phase 7: US5 (system preference)
6. Complete Phase 8: Polish & Validation
7. All success criteria met

### Time Estimates

| Phase | Estimated Time | Cumulative |
|-------|---------------|------------|
| Setup | 5 min | 5 min |
| Foundational | 30 min | 35 min |
| US1 (Toggle) | 25 min | 1 hour |
| US2 (Light Theme) | 15 min | 1h 15m |
| US3 (Dark Theme) | 30 min | 1h 45m |
| US4 (Components) | 30 min | 2h 15m |
| US5 (System Pref) | 10 min | 2h 25m |
| Polish | 25 min | 2h 50m |

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story
- US1 is MVP - toggle functionality is the core feature
- US2 and US3 are verification-heavy (CSS already in Foundational)
- US4 has most parallel opportunities (8 independent component updates)
- US5 is mostly configuration verification
- Verify theme transition < 500ms per SC-001
- Use CSS variables, not hardcoded colors
- Run `npm run typecheck` after each phase
- **Tailwind v4 CSS Variable Syntax**: Use `w-[var(--sidebar-width)]` not `w-[--sidebar-width]` - Tailwind v4 requires explicit `var()` wrapper
