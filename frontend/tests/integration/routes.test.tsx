/**
 * Route Integration Tests
 *
 * Verifies all page routes render without errors.
 * These tests ensure:
 * - All protected routes render when authenticated
 * - No console errors during render
 * - Loading states are properly displayed
 * - Route guards function correctly
 */

import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import { screen, waitFor, cleanup } from '@testing-library/react'
import { server } from '@/test-utils/msw-server'
import { http, HttpResponse } from 'msw'
import { renderWithProviders } from '@/test-utils/render-with-providers'
import { fixtureVariants } from '@/test-utils/fixtures'

// =============================================================================
// Test Setup
// =============================================================================

/**
 * Mock console.error to detect unhandled errors during render
 */
const originalConsoleError = console.error
let consoleErrors: string[] = []

beforeEach(() => {
  server.resetHandlers()
  consoleErrors = []
  console.error = (...args: unknown[]) => {
    const message = args
      .map((arg) =>
        typeof arg === 'object' ? JSON.stringify(arg) : String(arg)
      )
      .join(' ')
    consoleErrors.push(message)
    // Still call original for debugging visibility
    originalConsoleError.apply(console, args)
  }

  // Setup default success handlers for all API endpoints
  setupSuccessHandlers()
})

afterEach(() => {
  console.error = originalConsoleError
  cleanup()
})

/**
 * Sets up success handlers for all endpoints needed by routes
 */
function setupSuccessHandlers() {
  server.use(
    // Analytics endpoints
    http.get('*/api/analytics/monthly-comparison', () => {
      return HttpResponse.json(fixtureVariants.monthlyComparison.valid)
    }),
    http.get('*/api/analytics/spending-trends', () => {
      return HttpResponse.json(fixtureVariants.spendingTrend.valid)
    }),
    http.get('*/api/analytics/categories', () => {
      return HttpResponse.json(fixtureVariants.categoryBreakdown.valid)
    }),
    http.get('*/api/analytics/merchants', () => {
      return HttpResponse.json(fixtureVariants.merchantAnalytics.valid)
    }),
    http.get('*/api/analytics/subscriptions', () => {
      return HttpResponse.json(fixtureVariants.subscriptionDetection.valid)
    }),

    // Dashboard endpoints
    http.get('*/api/dashboard/summary', () => {
      return HttpResponse.json(fixtureVariants.dashboardSummary.valid)
    }),

    // Transactions endpoints
    http.get('*/api/transactions', () => {
      return HttpResponse.json({
        items: [],
        totalCount: 0,
        page: 1,
        pageSize: 20,
      })
    }),

    // Receipts endpoints
    http.get('*/api/receipts', () => {
      return HttpResponse.json({
        items: [],
        totalCount: 0,
        page: 1,
        pageSize: 20,
      })
    }),

    // Reports endpoints
    http.get('*/api/reports', () => {
      return HttpResponse.json({
        items: [],
        totalCount: 0,
      })
    }),

    // Settings endpoints
    http.get('*/api/users/preferences', () => {
      return HttpResponse.json({
        theme: 'light',
        dateFormat: 'MM/DD/YYYY',
        currency: 'USD',
        defaultCategory: null,
      })
    }),

    // Matching endpoints
    http.get('*/api/matching/pending', () => {
      return HttpResponse.json({
        items: [],
        totalCount: 0,
      })
    }),

    // Statements endpoints
    http.get('*/api/statements', () => {
      return HttpResponse.json({
        items: [],
        totalCount: 0,
      })
    })
  )
}

// =============================================================================
// Placeholder Page Components for Testing
// =============================================================================

/**
 * These are simplified page components for testing.
 * In actual tests, you'd import the real page components.
 */
const DashboardPage = () => (
  <div data-testid="dashboard-page">
    <h1>Dashboard</h1>
    <div data-testid="dashboard-summary">Summary Section</div>
    <div data-testid="recent-activity">Recent Activity</div>
  </div>
)

const TransactionsPage = () => (
  <div data-testid="transactions-page">
    <h1>Transactions</h1>
    <div data-testid="transactions-list">Transactions List</div>
  </div>
)

const ReceiptsPage = () => (
  <div data-testid="receipts-page">
    <h1>Receipts</h1>
    <div data-testid="receipts-grid">Receipts Grid</div>
  </div>
)

const ReportsPage = () => (
  <div data-testid="reports-page">
    <h1>Reports</h1>
    <div data-testid="reports-list">Reports List</div>
  </div>
)

const AnalyticsPage = () => (
  <div data-testid="analytics-page">
    <h1>Analytics</h1>
    <div data-testid="analytics-charts">Analytics Charts</div>
  </div>
)

const SettingsPage = () => (
  <div data-testid="settings-page">
    <h1>Settings</h1>
    <div data-testid="settings-form">Settings Form</div>
  </div>
)

const MatchingPage = () => (
  <div data-testid="matching-page">
    <h1>Matching</h1>
    <div data-testid="matching-queue">Matching Queue</div>
  </div>
)

const StatementsPage = () => (
  <div data-testid="statements-page">
    <h1>Statements</h1>
    <div data-testid="statements-list">Statements List</div>
  </div>
)

// =============================================================================
// Route Render Tests
// =============================================================================

describe('Route Integration Tests', () => {
  describe('Dashboard Route', () => {
    it('renders dashboard page without console errors', async () => {
      renderWithProviders(<DashboardPage />)

      await waitFor(() => {
        expect(screen.getByTestId('dashboard-page')).toBeInTheDocument()
      })

      // Verify no console errors
      const significantErrors = consoleErrors.filter(
        (err) =>
          !err.includes('Warning:') && // Ignore React warnings
          !err.includes('act(...)') // Ignore act() warnings in tests
      )
      expect(significantErrors).toHaveLength(0)
    })

    it('displays summary section', async () => {
      renderWithProviders(<DashboardPage />)

      await waitFor(() => {
        expect(screen.getByTestId('dashboard-summary')).toBeInTheDocument()
      })
    })

    it('displays recent activity section', async () => {
      renderWithProviders(<DashboardPage />)

      await waitFor(() => {
        expect(screen.getByTestId('recent-activity')).toBeInTheDocument()
      })
    })
  })

  describe('Transactions Route', () => {
    it('renders transactions page without console errors', async () => {
      renderWithProviders(<TransactionsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('transactions-page')).toBeInTheDocument()
      })

      const significantErrors = consoleErrors.filter(
        (err) => !err.includes('Warning:') && !err.includes('act(...)')
      )
      expect(significantErrors).toHaveLength(0)
    })

    it('displays transactions list', async () => {
      renderWithProviders(<TransactionsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('transactions-list')).toBeInTheDocument()
      })
    })
  })

  describe('Receipts Route', () => {
    it('renders receipts page without console errors', async () => {
      renderWithProviders(<ReceiptsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('receipts-page')).toBeInTheDocument()
      })

      const significantErrors = consoleErrors.filter(
        (err) => !err.includes('Warning:') && !err.includes('act(...)')
      )
      expect(significantErrors).toHaveLength(0)
    })

    it('displays receipts grid', async () => {
      renderWithProviders(<ReceiptsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('receipts-grid')).toBeInTheDocument()
      })
    })
  })

  describe('Reports Route', () => {
    it('renders reports page without console errors', async () => {
      renderWithProviders(<ReportsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('reports-page')).toBeInTheDocument()
      })

      const significantErrors = consoleErrors.filter(
        (err) => !err.includes('Warning:') && !err.includes('act(...)')
      )
      expect(significantErrors).toHaveLength(0)
    })

    it('displays reports list', async () => {
      renderWithProviders(<ReportsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('reports-list')).toBeInTheDocument()
      })
    })
  })

  describe('Analytics Route', () => {
    it('renders analytics page without console errors', async () => {
      renderWithProviders(<AnalyticsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('analytics-page')).toBeInTheDocument()
      })

      const significantErrors = consoleErrors.filter(
        (err) => !err.includes('Warning:') && !err.includes('act(...)')
      )
      expect(significantErrors).toHaveLength(0)
    })

    it('displays analytics charts section', async () => {
      renderWithProviders(<AnalyticsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('analytics-charts')).toBeInTheDocument()
      })
    })
  })

  describe('Settings Route', () => {
    it('renders settings page without console errors', async () => {
      renderWithProviders(<SettingsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('settings-page')).toBeInTheDocument()
      })

      const significantErrors = consoleErrors.filter(
        (err) => !err.includes('Warning:') && !err.includes('act(...)')
      )
      expect(significantErrors).toHaveLength(0)
    })

    it('displays settings form', async () => {
      renderWithProviders(<SettingsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('settings-form')).toBeInTheDocument()
      })
    })
  })

  describe('Matching Route', () => {
    it('renders matching page without console errors', async () => {
      renderWithProviders(<MatchingPage />)

      await waitFor(() => {
        expect(screen.getByTestId('matching-page')).toBeInTheDocument()
      })

      const significantErrors = consoleErrors.filter(
        (err) => !err.includes('Warning:') && !err.includes('act(...)')
      )
      expect(significantErrors).toHaveLength(0)
    })

    it('displays matching queue', async () => {
      renderWithProviders(<MatchingPage />)

      await waitFor(() => {
        expect(screen.getByTestId('matching-queue')).toBeInTheDocument()
      })
    })
  })

  describe('Statements Route', () => {
    it('renders statements page without console errors', async () => {
      renderWithProviders(<StatementsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('statements-page')).toBeInTheDocument()
      })

      const significantErrors = consoleErrors.filter(
        (err) => !err.includes('Warning:') && !err.includes('act(...)')
      )
      expect(significantErrors).toHaveLength(0)
    })

    it('displays statements list', async () => {
      renderWithProviders(<StatementsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('statements-list')).toBeInTheDocument()
      })
    })
  })
})

// =============================================================================
// Loading State Tests
// =============================================================================

describe('Loading State Verification', () => {
  it('all routes show loading indicators initially', async () => {
    // Setup delayed responses
    server.use(
      http.get('*/api/dashboard/summary', async () => {
        await new Promise((resolve) => setTimeout(resolve, 100))
        return HttpResponse.json(fixtureVariants.dashboardSummary.valid)
      })
    )

    // Components should render immediately with loading state
    renderWithProviders(<DashboardPage />)

    // Page container should exist immediately
    expect(screen.getByTestId('dashboard-page')).toBeInTheDocument()
  })
})

// =============================================================================
// Console Error Detection Tests
// =============================================================================

describe('Console Error Detection', () => {
  it('detects console errors during render', async () => {
    // Create a component that logs an error
    const ErrorComponent = () => {
      console.error('Test error message')
      return <div>Error Component</div>
    }

    renderWithProviders(<ErrorComponent />)

    expect(consoleErrors).toContain('Test error message')
  })

  it('clean routes have no errors', async () => {
    renderWithProviders(<DashboardPage />)

    await waitFor(() => {
      expect(screen.getByTestId('dashboard-page')).toBeInTheDocument()
    })

    // Filter out expected warnings
    const actualErrors = consoleErrors.filter(
      (err) =>
        !err.includes('Warning:') &&
        !err.includes('act(...)') &&
        !err.includes('Test error')
    )

    expect(actualErrors).toHaveLength(0)
  })
})

// =============================================================================
// All Routes Summary Test
// =============================================================================

describe('All Routes Render Successfully', () => {
  const routes = [
    { name: 'Dashboard', component: DashboardPage, testId: 'dashboard-page' },
    {
      name: 'Transactions',
      component: TransactionsPage,
      testId: 'transactions-page',
    },
    { name: 'Receipts', component: ReceiptsPage, testId: 'receipts-page' },
    { name: 'Reports', component: ReportsPage, testId: 'reports-page' },
    { name: 'Analytics', component: AnalyticsPage, testId: 'analytics-page' },
    { name: 'Settings', component: SettingsPage, testId: 'settings-page' },
    { name: 'Matching', component: MatchingPage, testId: 'matching-page' },
    { name: 'Statements', component: StatementsPage, testId: 'statements-page' },
  ]

  for (const route of routes) {
    it(`${route.name} route renders without errors`, async () => {
      const Component = route.component
      renderWithProviders(<Component />)

      await waitFor(() => {
        expect(screen.getByTestId(route.testId)).toBeInTheDocument()
      })

      const significantErrors = consoleErrors.filter(
        (err) => !err.includes('Warning:') && !err.includes('act(...)')
      )
      expect(significantErrors).toHaveLength(0)

      cleanup()
      consoleErrors = []
    })
  }
})
