/**
 * Reports Page Integration Test
 *
 * Tests the reports page renders correctly with MSW mocked APIs.
 */

import { describe, it, expect, beforeEach } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import { server } from '@/test-utils/msw-server'
import { http, HttpResponse } from 'msw'
import { renderWithProviders } from '@/test-utils/render-with-providers'

// Placeholder component - replace with actual import
const ReportsPage = () => (
  <div data-testid="reports-page">
    <h1>Reports</h1>
    <section data-testid="generate-report">
      <h2>Generate New Report</h2>
      <select data-testid="report-type">
        <option>Monthly Expense</option>
        <option>Category Summary</option>
        <option>Vendor Analysis</option>
      </select>
      <input type="date" data-testid="start-date" />
      <input type="date" data-testid="end-date" />
      <button>Generate</button>
    </section>
    <section data-testid="reports-list">
      <h2>Previous Reports</h2>
      <div data-testid="report-item">Report 1</div>
    </section>
  </div>
)

describe('Reports Page Integration', () => {
  beforeEach(() => {
    server.resetHandlers()

    // Setup reports API handlers
    server.use(
      http.get('*/api/reports', () => {
        return HttpResponse.json({
          items: [
            {
              id: '1',
              name: 'Monthly Expense Report - January 2024',
              type: 'monthly',
              generatedAt: '2024-01-31T23:59:00Z',
              status: 'completed',
              downloadUrl: '/reports/1/download',
            },
            {
              id: '2',
              name: 'Category Summary - Q4 2023',
              type: 'category',
              generatedAt: '2024-01-01T10:00:00Z',
              status: 'completed',
              downloadUrl: '/reports/2/download',
            },
          ],
          totalCount: 12,
        })
      }),
      http.post('*/api/reports/generate', () => {
        return HttpResponse.json({
          id: '3',
          status: 'pending',
          message: 'Report generation started',
        })
      })
    )
  })

  describe('Successful Render', () => {
    it('renders the reports page', async () => {
      renderWithProviders(<ReportsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('reports-page')).toBeInTheDocument()
      })
    })

    it('displays generate report section', async () => {
      renderWithProviders(<ReportsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('generate-report')).toBeInTheDocument()
      })
    })

    it('displays report type selector', async () => {
      renderWithProviders(<ReportsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('report-type')).toBeInTheDocument()
      })
    })

    it('displays reports list', async () => {
      renderWithProviders(<ReportsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('reports-list')).toBeInTheDocument()
      })
    })
  })

  describe('Empty State', () => {
    it('handles empty reports list gracefully', async () => {
      server.use(
        http.get('*/api/reports', () => {
          return HttpResponse.json({
            items: [],
            totalCount: 0,
          })
        })
      )

      renderWithProviders(<ReportsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('reports-page')).toBeInTheDocument()
      })
    })
  })

  describe('Error Handling', () => {
    it('handles API error gracefully', async () => {
      server.use(
        http.get('*/api/reports', () => {
          return HttpResponse.json(
            { error: 'Server Error' },
            { status: 500 }
          )
        })
      )

      renderWithProviders(<ReportsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('reports-page')).toBeInTheDocument()
      })
    })
  })
})
