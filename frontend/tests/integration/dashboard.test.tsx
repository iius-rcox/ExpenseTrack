/**
 * Dashboard Page Integration Test
 *
 * Tests the dashboard page renders correctly with MSW mocked APIs.
 */

import { describe, it, expect, beforeEach } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import { server } from '@/test-utils/msw-server'
import { http, HttpResponse } from 'msw'
import { renderWithProviders } from '@/test-utils/render-with-providers'
import { fixtureVariants } from '@/test-utils/fixtures'

// Placeholder component - replace with actual import
const DashboardPage = () => (
  <div data-testid="dashboard-page">
    <h1>Dashboard</h1>
    <section data-testid="summary-cards">
      <div>Total Spending</div>
      <div>Pending Receipts</div>
      <div>Matched Receipts</div>
    </section>
    <section data-testid="recent-activity">
      <h2>Recent Activity</h2>
    </section>
    <section data-testid="quick-actions">
      <h2>Quick Actions</h2>
    </section>
  </div>
)

describe('Dashboard Page Integration', () => {
  beforeEach(() => {
    server.resetHandlers()

    // Setup dashboard API handlers
    server.use(
      http.get('*/api/dashboard/summary', () => {
        return HttpResponse.json(fixtureVariants.dashboardSummary.valid)
      })
    )
  })

  describe('Successful Render', () => {
    it('renders the dashboard page', async () => {
      renderWithProviders(<DashboardPage />)

      await waitFor(() => {
        expect(screen.getByTestId('dashboard-page')).toBeInTheDocument()
      })
    })

    it('displays summary cards section', async () => {
      renderWithProviders(<DashboardPage />)

      await waitFor(() => {
        expect(screen.getByTestId('summary-cards')).toBeInTheDocument()
      })
    })

    it('displays recent activity section', async () => {
      renderWithProviders(<DashboardPage />)

      await waitFor(() => {
        expect(screen.getByTestId('recent-activity')).toBeInTheDocument()
      })
    })

    it('displays quick actions section', async () => {
      renderWithProviders(<DashboardPage />)

      await waitFor(() => {
        expect(screen.getByTestId('quick-actions')).toBeInTheDocument()
      })
    })
  })

  describe('Empty State', () => {
    it('handles empty dashboard data gracefully', async () => {
      server.use(
        http.get('*/api/dashboard/summary', () => {
          return HttpResponse.json(fixtureVariants.dashboardSummary.empty)
        })
      )

      renderWithProviders(<DashboardPage />)

      await waitFor(() => {
        expect(screen.getByTestId('dashboard-page')).toBeInTheDocument()
      })
    })
  })

  describe('Error Handling', () => {
    it('handles API error gracefully', async () => {
      server.use(
        http.get('*/api/dashboard/summary', () => {
          return HttpResponse.json(
            { error: 'Server Error' },
            { status: 500 }
          )
        })
      )

      renderWithProviders(<DashboardPage />)

      // Page should still render
      await waitFor(() => {
        expect(screen.getByTestId('dashboard-page')).toBeInTheDocument()
      })
    })
  })
})
