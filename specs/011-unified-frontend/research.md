# Research: Unified Frontend Experience

**Feature Branch**: `011-unified-frontend`
**Date**: 2025-12-18

This document captures technology research and decisions for the unified frontend implementation.

## 1. Routing: TanStack Router

### Decision
Use **TanStack Router** with file-based routing via Vite plugin.

### Rationale
- **Type-safe routing**: Full TypeScript inference for route params and search params
- **File-based routing**: Automatic code-splitting with Vite plugin (`@tanstack/router-plugin/vite`)
- **Search param validation**: Native Zod integration via `@tanstack/zod-adapter` for type-safe URL state
- **Loader pattern**: `beforeLoad` and `loader` functions for data fetching before render
- **Context support**: Router context enables passing auth state and queryClient to all routes

### Alternatives Considered
| Alternative | Rejected Because |
|-------------|------------------|
| React Router v6 | Less type-safe, no native search param validation, no built-in loader pattern |
| Next.js App Router | Requires server components, overkill for SPA consuming existing backend |
| Wouter | Too minimal, lacks search param handling and loaders |

### Implementation Pattern

**Router Context with MSAL Integration:**
```tsx
// src/routes/__root.tsx
import { createRootRouteWithContext, Outlet } from '@tanstack/react-router'
import type { QueryClient } from '@tanstack/react-query'
import type { IPublicClientApplication, AccountInfo } from '@azure/msal-browser'

interface RouterContext {
  queryClient: QueryClient
  msalInstance: IPublicClientApplication
  account: AccountInfo | null
  isAuthenticated: boolean
}

export const Route = createRootRouteWithContext<RouterContext>()({
  component: RootLayout,
})
```

**Protected Routes with `beforeLoad`:**
```tsx
// src/routes/_authenticated.tsx
import { createFileRoute, redirect, Outlet } from '@tanstack/react-router'

export const Route = createFileRoute('/_authenticated')({
  beforeLoad: ({ context, location }) => {
    if (!context.isAuthenticated) {
      throw redirect({
        to: '/login',
        search: { redirect: location.href },
      })
    }
  },
  component: AuthenticatedLayout,
})
```

**Search Params with Zod:**
```tsx
// src/routes/_authenticated/transactions/index.tsx
import { z } from 'zod'
import { zodSearchValidator } from '@tanstack/zod-adapter'

const transactionSearchSchema = z.object({
  page: z.number().optional().default(1),
  pageSize: z.number().optional().default(50),
  startDate: z.string().optional(),
  endDate: z.string().optional(),
  matched: z.boolean().optional(),
})

export const Route = createFileRoute('/_authenticated/transactions/')({
  validateSearch: zodSearchValidator(transactionSearchSchema),
  loaderDeps: ({ search }) => search,
  loader: ({ context, deps }) => {
    return context.queryClient.ensureQueryData(
      transactionsQueryOptions(deps)
    )
  },
})
```

## 2. Server State: TanStack Query

### Decision
Use **TanStack Query (React Query)** for all server state management.

### Rationale
- **Caching**: Automatic request deduplication and stale-while-revalidate
- **Background sync**: Automatic refetching on window focus, reconnect
- **Optimistic updates**: Built-in support with rollback on error
- **Loader integration**: Works seamlessly with TanStack Router loaders via `ensureQueryData`
- **DevTools**: Excellent debugging experience

### Alternatives Considered
| Alternative | Rejected Because |
|-------------|------------------|
| SWR | Less feature-rich, no built-in optimistic updates |
| RTK Query | Requires Redux, adds complexity for no benefit |
| Apollo Client | Designed for GraphQL, our backend is REST |

### Implementation Pattern

**Query Options Factory:**
```tsx
// src/services/receipts.ts
import { queryOptions } from '@tanstack/react-query'
import { api } from './api'

export const receiptsQueryOptions = (params: ReceiptListParams) =>
  queryOptions({
    queryKey: ['receipts', params],
    queryFn: () => api.getReceipts(params),
    staleTime: 30_000, // 30 seconds
  })

export const receiptQueryOptions = (id: string) =>
  queryOptions({
    queryKey: ['receipt', id],
    queryFn: () => api.getReceipt(id),
    staleTime: 60_000, // 1 minute
  })
```

**Optimistic Updates for Mutations (SC-008):**
```tsx
// src/hooks/use-matching.ts
export function useConfirmMatch() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (matchId: string) => api.confirmMatch(matchId),
    onMutate: async (matchId) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: ['proposals'] })

      // Snapshot for rollback
      const previousProposals = queryClient.getQueryData(['proposals'])

      // Optimistically remove from proposals list
      queryClient.setQueryData(['proposals'], (old) => ({
        ...old,
        items: old.items.filter((p) => p.matchId !== matchId),
        totalCount: old.totalCount - 1,
      }))

      return { previousProposals }
    },
    onError: (err, matchId, context) => {
      // Rollback on error
      queryClient.setQueryData(['proposals'], context.previousProposals)
    },
    onSettled: () => {
      // Always refetch to ensure consistency
      queryClient.invalidateQueries({ queryKey: ['proposals'] })
      queryClient.invalidateQueries({ queryKey: ['matching', 'stats'] })
    },
  })
}
```

**QueryClient Configuration:**
```tsx
// src/lib/query-client.ts
import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      gcTime: 5 * 60 * 1000, // 5 minutes
      retry: 1,
      refetchOnWindowFocus: true,
    },
  },
})
```

## 3. UI Components: shadcn/ui

### Decision
Use **shadcn/ui** components with Tailwind CSS for all UI elements.

### Rationale
- **Copy-paste ownership**: Components are added to the codebase, not imported from node_modules
- **Accessibility**: WCAG 2.1 AA compliant by default (Radix UI primitives)
- **Customization**: Full control over styling via Tailwind utilities
- **Sidebar component**: Built-in collapsible sidebar with cookie persistence (FR-001)
- **MCP server**: Components can be retrieved via shadcn MCP server during implementation

### Alternatives Considered
| Alternative | Rejected Because |
|-------------|------------------|
| Material UI | Heavy bundle, opinionated styling, harder to customize |
| Chakra UI | Runtime CSS-in-JS, larger bundle |
| Headless UI | Too minimal, requires more custom styling work |
| Radix Themes | Less customizable than shadcn's Radix-based approach |

### Implementation Pattern

**Sidebar Structure (FR-001, FR-002):**
```tsx
// src/components/layout/app-sidebar.tsx
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarTrigger,
} from "@/components/ui/sidebar"

const navItems = [
  { title: "Dashboard", icon: Home, href: "/" },
  { title: "Receipts", icon: Receipt, href: "/receipts" },
  { title: "Statements", icon: FileSpreadsheet, href: "/statements" },
  { title: "Transactions", icon: CreditCard, href: "/transactions" },
  { title: "Matching", icon: Link2, href: "/matching" },
  { title: "Reports", icon: FileText, href: "/reports" },
  { title: "Analytics", icon: BarChart3, href: "/analytics" },
  { title: "Settings", icon: Settings, href: "/settings" },
]
```

**Required shadcn/ui Components:**
- `sidebar` - Main navigation (with SidebarProvider for state persistence)
- `button` - Actions, form submissions
- `card` - Dashboard cards, list items
- `table` - Transaction and receipt lists
- `dialog` - Confirmations, modals
- `dropdown-menu` - User menu, actions
- `input`, `select`, `textarea` - Forms
- `badge` - Status indicators
- `skeleton` - Loading states
- `alert` - Error messages
- `breadcrumb` - Navigation context
- `toast` - Notifications (via Sonner)
- `tabs` - Report configuration
- `progress` - Upload progress
- `tooltip` - Accessibility hints

## 4. MSAL Integration with TanStack Router

### Decision
Pass MSAL state via TanStack Router context; use `beforeLoad` for auth guards.

### Rationale
- **No hooks in beforeLoad**: TanStack Router's `beforeLoad` cannot use React hooks
- **Context pattern**: Router context receives MSAL instance and auth state from parent component
- **Redirect pattern**: Use `throw redirect()` for unauthenticated access
- **Token acquisition**: Existing `useApiToken` hook used within components/services

### Alternatives Considered
| Alternative | Rejected Because |
|-------------|------------------|
| Higher-order component wrapping | More complex, less type-safe |
| Route loader with async auth check | Hooks not available in loaders |
| Global auth state (Zustand) | Unnecessary complexity, MSAL already manages state |

### Implementation Pattern

**App Entry Point:**
```tsx
// src/main.tsx
import { RouterProvider } from '@tanstack/react-router'
import { QueryClientProvider } from '@tanstack/react-query'
import { MsalProvider, useMsal, useIsAuthenticated } from '@azure/msal-react'
import { queryClient } from './lib/query-client'
import { router } from './router'
import { msalInstance } from './auth/authConfig'

function InnerApp() {
  const { instance, accounts } = useMsal()
  const isAuthenticated = useIsAuthenticated()

  return (
    <RouterProvider
      router={router}
      context={{
        queryClient,
        msalInstance: instance,
        account: accounts[0] ?? null,
        isAuthenticated,
      }}
    />
  )
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <MsalProvider instance={msalInstance}>
        <InnerApp />
      </MsalProvider>
    </QueryClientProvider>
  )
}
```

**API Client with Token:**
```tsx
// src/services/api.ts
import { msalInstance, apiScopes } from '../auth/authConfig'

async function getAuthHeaders(): Promise<HeadersInit> {
  const accounts = msalInstance.getAllAccounts()
  if (accounts.length === 0) {
    throw new Error('No authenticated account')
  }

  try {
    const response = await msalInstance.acquireTokenSilent({
      scopes: apiScopes.all,
      account: accounts[0],
    })
    return {
      'Authorization': `Bearer ${response.idToken}`,
      'Content-Type': 'application/json',
    }
  } catch (error) {
    // Token expired, redirect to login
    await msalInstance.loginRedirect()
    throw error
  }
}

export async function apiFetch<T>(url: string, options?: RequestInit): Promise<T> {
  const headers = await getAuthHeaders()
  const response = await fetch(`${API_BASE_URL}${url}`, {
    ...options,
    headers: { ...headers, ...options?.headers },
  })

  if (!response.ok) {
    throw new ApiError(response.status, await response.text())
  }

  return response.json()
}
```

## 5. Error Handling Strategy

### Decision
Implement layered error handling with route-level error components and React Error Boundaries.

### Rationale
- **FR-034**: Each route defines `errorComponent` for route-specific errors
- **FR-035**: Layout-level Error Boundaries catch component rendering errors
- **User experience**: Graceful degradation with retry options

### Implementation Pattern

**Route Error Component:**
```tsx
// src/routes/_authenticated/receipts/index.tsx
export const Route = createFileRoute('/_authenticated/receipts/')({
  errorComponent: ({ error, reset }) => (
    <Alert variant="destructive">
      <AlertTitle>Failed to load receipts</AlertTitle>
      <AlertDescription>{error.message}</AlertDescription>
      <Button onClick={reset}>Try Again</Button>
    </Alert>
  ),
})
```

**Layout Error Boundary:**
```tsx
// src/components/layout/error-boundary.tsx
import { ErrorBoundary } from 'react-error-boundary'

function ErrorFallback({ error, resetErrorBoundary }) {
  return (
    <div className="flex flex-col items-center justify-center min-h-[400px]">
      <AlertTriangle className="h-12 w-12 text-destructive mb-4" />
      <h2 className="text-lg font-semibold mb-2">Something went wrong</h2>
      <p className="text-muted-foreground mb-4">{error.message}</p>
      <Button onClick={resetErrorBoundary}>Try Again</Button>
    </div>
  )
}

export function LayoutErrorBoundary({ children }) {
  return (
    <ErrorBoundary FallbackComponent={ErrorFallback}>
      {children}
    </ErrorBoundary>
  )
}
```

## 6. Responsive Design Strategy

### Decision
Mobile-first approach with Tailwind breakpoints; collapsible sidebar on desktop, drawer on mobile.

### Rationale
- **FR-003**: System MUST be usable on mobile devices (320px+)
- **SC-007**: All pages render correctly from 320px to 2560px
- **shadcn/ui Sidebar**: Built-in responsive behavior with `variant="floating"` on mobile

### Implementation Pattern

**Responsive Sidebar:**
```tsx
// src/components/layout/app-sidebar.tsx
import { useIsMobile } from '@/hooks/use-mobile'

export function AppSidebar() {
  const isMobile = useIsMobile()

  return (
    <Sidebar
      variant={isMobile ? "floating" : "sidebar"}
      collapsible={isMobile ? "offcanvas" : "icon"}
    >
      {/* Sidebar content */}
    </Sidebar>
  )
}
```

**Tailwind Breakpoint Usage:**
- `sm:` (640px) - Small tablets
- `md:` (768px) - Tablets, sidebar visible
- `lg:` (1024px) - Desktops, expanded sidebar
- `xl:` (1280px) - Large desktops
- `2xl:` (1536px) - Ultra-wide monitors

## 7. Testing Strategy

### Decision
Use Vitest for unit/component tests; Playwright for E2E tests.

### Rationale
- **Vitest**: Native ESM, fast, excellent Vite integration
- **Playwright**: Cross-browser, reliable, good async handling
- **React Testing Library**: User-centric component testing

### Test Categories

| Category | Tool | Focus |
|----------|------|-------|
| Unit | Vitest | Utility functions, hooks |
| Component | Vitest + RTL | UI components, interactions |
| Integration | Vitest + MSW | API integration, query hooks |
| E2E | Playwright | Full user flows, auth |

### Key E2E Scenarios
1. Login flow with MSAL redirect
2. Receipt upload and processing status
3. Match review (confirm/reject)
4. Report generation and download
5. Navigation accessibility

## Summary of Dependencies

```json
{
  "dependencies": {
    "@azure/msal-browser": "^4.27.0",
    "@azure/msal-react": "^3.0.23",
    "@tanstack/react-query": "^5.60.0",
    "@tanstack/react-router": "^1.91.0",
    "@tanstack/zod-adapter": "^1.91.0",
    "date-fns": "^4.1.0",
    "lucide-react": "^0.468.0",
    "react": "^18.3.1",
    "react-dom": "^18.3.1",
    "sonner": "^1.7.0",
    "zod": "^3.24.0",
    "tailwind-merge": "^2.5.0",
    "clsx": "^2.1.0",
    "class-variance-authority": "^0.7.0"
  },
  "devDependencies": {
    "@tanstack/router-devtools": "^1.91.0",
    "@tanstack/react-query-devtools": "^5.60.0",
    "@tanstack/router-plugin": "^1.91.0",
    "@testing-library/react": "^16.1.0",
    "@vitejs/plugin-react": "^4.3.4",
    "autoprefixer": "^10.4.20",
    "postcss": "^8.4.49",
    "tailwindcss": "^3.4.0",
    "typescript": "^5.7.2",
    "vite": "^6.0.3",
    "vitest": "^2.1.0",
    "@playwright/test": "^1.49.0"
  }
}
```
