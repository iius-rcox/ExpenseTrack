# Implementation Plan: Front-End Redesign with Refined Intelligence Design System

**Branch**: `013-frontend-redesign` | **Date**: 2025-12-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/013-frontend-redesign/spec.md`

## Summary

Redesign the ExpenseFlow frontend with a "Refined Intelligence" design system featuring a command center dashboard, receipt intelligence panel, transaction explorer, match review workspace, and analytics reporting. The implementation enhances the existing React/TypeScript/Tailwind stack with new design tokens, enhanced components, and improved user workflows while maintaining API compatibility.

## Technical Context

**Language/Version**: TypeScript 5.7+ with React 18.3+
**Primary Dependencies**: TanStack Router, TanStack Query, Tailwind CSS 4.x, shadcn/ui, Framer Motion (new), Recharts
**Storage**: N/A (frontend consumes existing backend APIs)
**Testing**: Vitest + React Testing Library, Playwright for E2E
**Target Platform**: Modern browsers (Chrome, Firefox, Safari, Edge - last 2 versions)
**Project Type**: Web application (frontend-only enhancement)
**Performance Goals**: 3s initial load, 100ms interaction feedback, 5s dashboard render
**Constraints**: <20MB bundle size, maintain existing API contracts, preserve MSAL auth flow
**Scale/Scope**: Single-page application, ~15 route pages, 50+ components

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The constitution template is unpopulated (placeholder content only). Proceeding with industry best practices for React frontend development:

| Principle | Compliance | Notes |
|-----------|------------|-------|
| Component-First | ✅ Pass | Building reusable UI components with shadcn/ui foundation |
| Test Coverage | ✅ Pass | Vitest unit tests + Playwright E2E planned |
| Type Safety | ✅ Pass | Full TypeScript coverage with strict mode |
| Accessibility | ⚠️ Deferred | WCAG compliance explicitly out of scope per spec |
| Performance | ✅ Pass | Measurable goals: 3s load, 100ms feedback |

## Project Structure

### Documentation (this feature)

```text
specs/013-frontend-redesign/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── frontend-api-contracts.md
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
frontend/
├── src/
│   ├── components/
│   │   ├── ui/              # shadcn/ui primitives (existing)
│   │   ├── dashboard/       # NEW: Command center components
│   │   ├── receipts/        # Enhanced receipt intelligence
│   │   ├── transactions/    # NEW: Transaction explorer
│   │   ├── matching/        # Enhanced match review
│   │   ├── analytics/       # NEW: Analytics visualizations
│   │   └── design-system/   # NEW: Theme tokens, animations
│   ├── hooks/
│   │   ├── queries/         # TanStack Query hooks (existing)
│   │   └── ui/              # NEW: UI state hooks (undo, polling)
│   ├── lib/
│   │   ├── design-tokens.ts # NEW: Color, typography, spacing
│   │   └── animations.ts    # NEW: Framer Motion presets
│   ├── routes/              # TanStack Router pages (existing)
│   │   └── _authenticated/  # Protected routes
│   └── services/            # API client (existing)
└── tests/
    ├── unit/                # Vitest component tests
    └── e2e/                 # Playwright scenarios
```

**Structure Decision**: Extend existing `frontend/` structure with new component directories for each major module. Design system tokens centralized in `lib/design-tokens.ts`.

## Complexity Tracking

No constitution violations requiring justification. The redesign follows established patterns:
- Extends existing shadcn/ui component library
- Maintains current routing and data fetching patterns
- Adds design tokens without breaking existing styles
