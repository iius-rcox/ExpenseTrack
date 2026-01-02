/**
 * Test Wrapper Component with Providers
 *
 * Wraps components with all necessary providers for integration testing:
 * - TanStack Query (QueryClientProvider)
 * - Theme (ThemeProvider)
 * - MSAL Auth (mocked)
 *
 * Usage:
 *   import { renderWithProviders } from '@/test-utils/render-with-providers'
 *   const { getByText } = renderWithProviders(<MyComponent />)
 */

/* eslint-disable react-refresh/only-export-components */
// This is a test utility file, not a component file - Fast Refresh doesn't apply

import React from 'react'
import { render, RenderOptions, RenderResult } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ThemeProvider } from 'next-themes'

// =============================================================================
// Query Client Factory
// =============================================================================

/**
 * Creates a new QueryClient configured for testing
 * - Disables retries for predictable test behavior
 * - Disables refetch on window focus
 * - Sets short stale time for faster tests
 */
export function createTestQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        refetchOnWindowFocus: false,
        staleTime: 0,
        gcTime: 0,
      },
      mutations: {
        retry: false,
      },
    },
  })
}

// =============================================================================
// Provider Wrapper Component
// =============================================================================

interface TestProviderProps {
  children: React.ReactNode
  queryClient?: QueryClient
}

/**
 * All-in-one provider wrapper for integration tests
 */
function TestProviders({
  children,
  queryClient = createTestQueryClient(),
}: TestProviderProps): React.JSX.Element {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider
        attribute="class"
        defaultTheme="light"
        enableSystem={false}
        disableTransitionOnChange
      >
        {children}
      </ThemeProvider>
    </QueryClientProvider>
  )
}

// =============================================================================
// Custom Render Function
// =============================================================================

interface CustomRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  queryClient?: QueryClient
}

/**
 * Custom render function that wraps components with test providers
 *
 * @example
 * ```tsx
 * // Basic usage
 * const { getByText } = renderWithProviders(<MyComponent />)
 *
 * // With custom query client
 * const queryClient = createTestQueryClient()
 * const { getByText } = renderWithProviders(<MyComponent />, { queryClient })
 *
 * // Access query client for assertions
 * const queryClient = createTestQueryClient()
 * renderWithProviders(<MyComponent />, { queryClient })
 * expect(queryClient.getQueryState(['key'])).toBeDefined()
 * ```
 */
export function renderWithProviders(
  ui: React.ReactElement,
  {
    queryClient = createTestQueryClient(),
    ...renderOptions
  }: CustomRenderOptions = {}
): RenderResult & { queryClient: QueryClient } {
  const Wrapper = ({ children }: { children: React.ReactNode }) => (
    <TestProviders queryClient={queryClient}>{children}</TestProviders>
  )

  return {
    ...render(ui, { wrapper: Wrapper, ...renderOptions }),
    queryClient,
  }
}

// =============================================================================
// Exports
// =============================================================================

export { TestProviders }
export default renderWithProviders
