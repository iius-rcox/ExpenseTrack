import { createRouter } from '@tanstack/react-router'
import { routeTree } from './routeTree.gen'
import { queryClient } from './lib/query-client'
import type { QueryClient } from '@tanstack/react-query'
import type { IPublicClientApplication, AccountInfo } from '@azure/msal-browser'

// Router context type - passed to all routes
export interface RouterContext {
  queryClient: QueryClient
  msalInstance: IPublicClientApplication
  account: AccountInfo | null
  isAuthenticated: boolean
}

// Create router with context
export const router = createRouter({
  routeTree,
  context: {
    queryClient,
    msalInstance: undefined!, // Will be set in main.tsx
    account: null,
    isAuthenticated: false,
  },
  defaultPreload: 'intent',
  defaultPreloadStaleTime: 0,
})

// Register router type for type-safe route navigation
declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}
