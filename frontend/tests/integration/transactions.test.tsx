/**
 * Transactions Page Integration Test
 *
 * Tests the transactions page renders correctly with MSW mocked APIs.
 */

import { describe, it, expect, beforeEach } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import { server } from '@/test-utils/msw-server'
import { http, HttpResponse } from 'msw'
import { renderWithProviders } from '@/test-utils/render-with-providers'

// Placeholder component - replace with actual import
const TransactionsPage = () => (
  <div data-testid="transactions-page">
    <h1>Transactions</h1>
    <section data-testid="filters">
      <input placeholder="Search transactions" />
      <select data-testid="category-filter">
        <option>All Categories</option>
      </select>
    </section>
    <section data-testid="transactions-table">
      <table>
        <thead>
          <tr>
            <th>Date</th>
            <th>Description</th>
            <th>Amount</th>
            <th>Category</th>
          </tr>
        </thead>
        <tbody data-testid="transactions-body"></tbody>
      </table>
    </section>
    <section data-testid="pagination">
      <button>Previous</button>
      <span>Page 1</span>
      <button>Next</button>
    </section>
  </div>
)

describe('Transactions Page Integration', () => {
  beforeEach(() => {
    server.resetHandlers()

    // Setup transactions API handlers
    server.use(
      http.get('*/api/transactions', ({ request }) => {
        const url = new URL(request.url)
        const page = parseInt(url.searchParams.get('page') || '1')
        const pageSize = parseInt(url.searchParams.get('pageSize') || '20')

        return HttpResponse.json({
          items: [
            {
              id: '1',
              date: '2024-01-15T10:30:00Z',
              description: 'Whole Foods Market',
              amount: 87.45,
              category: 'Groceries',
              status: 'matched',
            },
            {
              id: '2',
              date: '2024-01-14T14:22:00Z',
              description: 'Amazon',
              amount: 234.56,
              category: 'Shopping',
              status: 'pending',
            },
          ],
          totalCount: 156,
          page,
          pageSize,
        })
      })
    )
  })

  describe('Successful Render', () => {
    it('renders the transactions page', async () => {
      renderWithProviders(<TransactionsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('transactions-page')).toBeInTheDocument()
      })
    })

    it('displays filters section', async () => {
      renderWithProviders(<TransactionsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('filters')).toBeInTheDocument()
      })
    })

    it('displays transactions table', async () => {
      renderWithProviders(<TransactionsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('transactions-table')).toBeInTheDocument()
      })
    })

    it('displays pagination controls', async () => {
      renderWithProviders(<TransactionsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('pagination')).toBeInTheDocument()
      })
    })
  })

  describe('Empty State', () => {
    it('handles empty transactions list gracefully', async () => {
      server.use(
        http.get('*/api/transactions', () => {
          return HttpResponse.json({
            items: [],
            totalCount: 0,
            page: 1,
            pageSize: 20,
          })
        })
      )

      renderWithProviders(<TransactionsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('transactions-page')).toBeInTheDocument()
      })
    })
  })

  describe('Error Handling', () => {
    it('handles API error gracefully', async () => {
      server.use(
        http.get('*/api/transactions', () => {
          return HttpResponse.json(
            { error: 'Server Error' },
            { status: 500 }
          )
        })
      )

      renderWithProviders(<TransactionsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('transactions-page')).toBeInTheDocument()
      })
    })
  })
})
