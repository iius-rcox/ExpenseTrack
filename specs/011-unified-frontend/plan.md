# Implementation Plan: Unified Frontend Experience

**Branch**: `011-unified-frontend` | **Date**: 2025-12-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/011-unified-frontend/spec.md`

## Summary

This feature delivers a unified React frontend experience that consolidates all ExpenseFlow functionality (receipts, transactions, matching, reports, analytics, settings) into a single cohesive application with professional navigation, responsive design, and accessible UI components. The implementation uses TanStack Router for type-safe routing, TanStack Query for server state management, and shadcn/ui components with Tailwind CSS for styling.

## Technical Context

**Language/Version**: TypeScript 5.7+ with React 18.3+
**Primary Dependencies**:
- @tanstack/react-router (file-based routing with Vite plugin)
- @tanstack/react-query (server state management)
- @tanstack/zod-adapter (search param validation)
- shadcn/ui components (via shadcn MCP server)
- Tailwind CSS 3.4+
- @azure/msal-react (existing authentication)
- zod (schema validation)
- date-fns (date formatting/manipulation)
- recharts (analytics visualizations - lightweight, React-native)

**Storage**: N/A (frontend consumes existing backend APIs)
**Testing**: Vitest + React Testing Library for unit/component tests, Playwright for E2E
**Target Platform**: Modern browsers (Chrome 100+, Firefox 100+, Safari 15+, Edge 100+), responsive from 320px to 2560px
**Project Type**: Web application (frontend only - consuming existing .NET 8 backend)
**Performance Goals**:
- First page load (50 items) < 1 second
- Search results appear within 500ms of user stopping typing
- Receipt upload to visibility < 5 seconds for images under 5MB
- Report generation < 30 seconds for up to 500 transactions

**Constraints**:
- WCAG 2.1 Level AA accessibility (leveraging shadcn/ui defaults)
- Navigation reachable within 2 clicks from dashboard
- Match review workflow in under 3 clicks
- Must work on mobile viewports (320px+)

**Scale/Scope**: 10-20 users, 7 main pages (Dashboard, Receipts, Transactions, Matching, Reports, Analytics, Settings)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Cost-First AI Architecture ✅ COMPLIANT
- Frontend does not make direct AI calls
- All AI operations handled by existing backend which implements tiered architecture
- No changes required

### II. Self-Improving System ✅ COMPLIANT
- Frontend enables user interactions that trigger backend learning:
  - Match confirmations → VendorAlias entries
  - Category selections → GL code learning
  - Description confirmations → DescriptionCache entries
- UI will surface these confirmation actions appropriately

### III. Receipt Accountability ✅ COMPLIANT
- Frontend displays receipt status and matching status
- Report generation UI will show missing receipt placeholders
- Justification options exposed in report editing interface

### IV. Infrastructure Optimization ✅ COMPLIANT
- Frontend is static assets deployed via existing AKS infrastructure
- No additional infrastructure costs
- Leverages existing MSAL authentication

### V. Cache-First Design ✅ COMPLIANT
- Frontend uses TanStack Query caching for API responses
- Stale-while-revalidate pattern reduces redundant API calls
- No changes to backend cache architecture required

**Gate Result**: ✅ PASS - All constitution principles satisfied

## Project Structure

### Documentation (this feature)

```text
specs/011-unified-frontend/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
frontend/
├── src/
│   ├── components/
│   │   ├── ui/                    # shadcn/ui components (Button, Card, etc.)
│   │   ├── layout/                # Layout components (Sidebar, Header, Breadcrumb)
│   │   ├── receipts/              # Receipt-specific components
│   │   ├── transactions/          # Transaction-specific components
│   │   ├── matching/              # Match review components
│   │   ├── reports/               # Report generation components
│   │   ├── analytics/             # Analytics charts/visualizations
│   │   └── statements/            # Existing statement components
│   ├── routes/
│   │   ├── __root.tsx             # Root layout with navigation
│   │   ├── _authenticated.tsx     # Auth guard layout
│   │   ├── _authenticated/
│   │   │   ├── index.tsx          # Dashboard (default route)
│   │   │   ├── receipts/
│   │   │   │   ├── index.tsx      # Receipt list
│   │   │   │   └── $receiptId.tsx # Receipt detail
│   │   │   ├── transactions/
│   │   │   │   ├── index.tsx      # Transaction list
│   │   │   │   └── $transactionId.tsx
│   │   │   ├── matching/
│   │   │   │   └── index.tsx      # Match review
│   │   │   ├── reports/
│   │   │   │   ├── index.tsx      # Report list
│   │   │   │   ├── new.tsx        # Create report
│   │   │   │   └── $reportId.tsx  # Report detail/edit
│   │   │   ├── analytics.tsx      # Analytics dashboard
│   │   │   └── settings.tsx       # User settings
│   │   └── login.tsx              # Login page (public)
│   ├── services/
│   │   ├── api.ts                 # API client with auth
│   │   ├── receipts.ts            # Receipt API functions
│   │   ├── transactions.ts        # Transaction API functions
│   │   ├── matching.ts            # Matching API functions
│   │   ├── reports.ts             # Report API functions
│   │   ├── analytics.ts           # Analytics API functions
│   │   └── statements.ts          # Existing statement service
│   ├── hooks/
│   │   ├── use-receipts.ts        # Receipt TanStack Query hooks
│   │   ├── use-transactions.ts    # Transaction hooks
│   │   ├── use-matching.ts        # Matching hooks
│   │   ├── use-reports.ts         # Report hooks
│   │   └── use-analytics.ts       # Analytics hooks
│   ├── lib/
│   │   ├── utils.ts               # Utility functions (cn, formatters)
│   │   └── query-client.ts        # TanStack Query configuration
│   ├── auth/
│   │   ├── authConfig.ts          # Existing MSAL config
│   │   ├── useApiToken.ts         # Existing token hook
│   │   └── auth-context.tsx       # Auth context for router
│   ├── types/
│   │   ├── api.ts                 # API response types (from backend DTOs)
│   │   └── routes.ts              # Route-specific types
│   ├── main.tsx                   # App entry point
│   ├── routeTree.gen.ts           # Generated route tree (TanStack Router)
│   └── globals.css                # Tailwind base + shadcn themes
├── public/
│   └── favicon.ico
├── components.json                # shadcn/ui configuration
├── tailwind.config.ts
├── tsconfig.json
├── vite.config.ts
└── package.json

tests/
├── e2e/                           # Playwright E2E tests
│   ├── auth.spec.ts
│   ├── receipts.spec.ts
│   ├── matching.spec.ts
│   └── reports.spec.ts
├── unit/                          # Vitest unit tests
│   ├── components/
│   └── hooks/
└── setup.ts
```

**Structure Decision**: Single frontend application extending the existing `frontend/` directory. File-based routing with TanStack Router provides automatic code-splitting and type generation. Components organized by feature domain with shared UI components from shadcn/ui.

## Complexity Tracking

No constitution violations requiring justification.
