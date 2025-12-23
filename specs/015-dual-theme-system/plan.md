# Implementation Plan: Dual Theme System

**Branch**: `015-dual-theme-system` | **Date**: 2025-12-23 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/015-dual-theme-system/spec.md`

## Summary

Replace the existing "Refined Intelligence" design system (slate/copper) with a dual-theme system featuring:
- **Light Mode: "Luxury Minimalist"** - Deep emerald (#2d5f4f) accents on off-white backgrounds
- **Dark Mode: "Dark Cyber"** - Cyan (#00bcd4) accents with glassmorphism effects

The project already uses `next-themes` for theme switching infrastructure. Implementation focuses on updating CSS variables, design tokens, and component styles to match the new palettes.

## Technical Context

**Language/Version**: TypeScript 5.7+ with React 18.3+
**Primary Dependencies**: Tailwind CSS 4.x, shadcn/ui (Radix primitives), next-themes, Framer Motion, class-variance-authority
**Storage**: localStorage (theme preference via next-themes)
**Testing**: Vitest (unit), Playwright (E2E)
**Target Platform**: Modern browsers (Chrome 76+, Firefox 67+, Safari 14+, Edge 79+)
**Project Type**: Frontend SPA (React + Vite)
**Performance Goals**: Theme transition < 500ms, no layout shifts during toggle
**Constraints**: WCAG 2.1 AA contrast ratios, graceful degradation for backdrop-filter
**Scale/Scope**: ~50 components, 15 pages, 2 theme palettes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| Use shadcn/ui for components | ✅ Pass | Leveraging existing shadcn components |
| Test-first development | ✅ Pass | Visual regression tests planned |
| Simplicity | ✅ Pass | Uses existing next-themes, no new dependencies |

No violations. Proceeding to Phase 0.

## Phase Status

| Phase | Status | Output |
|-------|--------|--------|
| Phase 0: Research | ✅ Complete | [research.md](./research.md) |
| Phase 1: Design | ✅ Complete | [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md) |
| Phase 2: Tasks | ✅ Complete | [tasks.md](./tasks.md) - 76 tasks across 8 phases |
| Phase 3: Analysis | ✅ Complete | [analysis.md](./analysis.md) - 8.5/10 quality score, 2 minor issues |

## Project Structure

### Documentation (this feature)

```text
specs/015-dual-theme-system/
├── spec.md              # Feature specification ✅
├── plan.md              # This file ✅
├── research.md          # Phase 0 output ✅
├── data-model.md        # Phase 1 output (design tokens) ✅
├── quickstart.md        # Phase 1 output ✅
├── contracts/           # Phase 1 output (CSS variable contracts) ✅
│   ├── css-variables.md # CSS custom property specification ✅
│   └── theme-provider.md # React context/provider contract ✅
├── tasks.md             # Phase 2 output (via /speckit.tasks) ✅
└── analysis.md          # Phase 3 output (via /speckit.analyze) ✅
```

### Source Code (repository root)

```text
frontend/
├── src/
│   ├── components/
│   │   ├── ui/              # shadcn components (theme-aware)
│   │   └── design-system/   # Custom themed components
│   ├── lib/
│   │   └── design-tokens.ts # Color/typography tokens (to be replaced)
│   ├── styles/
│   │   └── themes/          # NEW: Theme-specific CSS
│   │       ├── luxury-minimalist.css
│   │       └── dark-cyber.css
│   └── globals.css          # CSS variables (to be updated)
└── tests/
    └── visual/              # Visual regression tests
```

**Structure Decision**: Frontend-only feature. All changes contained within `frontend/src/`. No backend modifications required.

## Complexity Tracking

No constitution violations to justify.
