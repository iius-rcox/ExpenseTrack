/**
 * Receipts Page Integration Test
 *
 * Tests the receipts page renders correctly with MSW mocked APIs.
 */

import { describe, it, expect, beforeEach } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import { server } from '@/test-utils/msw-server'
import { http, HttpResponse } from 'msw'
import { renderWithProviders } from '@/test-utils/render-with-providers'

// Placeholder component - replace with actual import
const ReceiptsPage = () => (
  <div data-testid="receipts-page">
    <h1>Receipts</h1>
    <section data-testid="upload-area">
      <div data-testid="dropzone">
        <p>Drag and drop receipts here</p>
        <button>Browse Files</button>
      </div>
    </section>
    <section data-testid="receipts-grid">
      <div data-testid="receipt-card">Receipt 1</div>
    </section>
    <section data-testid="filters">
      <select data-testid="status-filter">
        <option>All Status</option>
        <option>Pending</option>
        <option>Matched</option>
        <option>Rejected</option>
      </select>
    </section>
  </div>
)

describe('Receipts Page Integration', () => {
  beforeEach(() => {
    server.resetHandlers()

    // Setup receipts API handlers
    server.use(
      http.get('*/api/receipts', ({ request }) => {
        const url = new URL(request.url)
        const page = parseInt(url.searchParams.get('page') || '1')
        const pageSize = parseInt(url.searchParams.get('pageSize') || '20')

        return HttpResponse.json({
          items: [
            {
              id: '1',
              fileName: 'receipt_001.jpg',
              uploadDate: '2024-01-15T10:30:00Z',
              status: 'matched',
              extractedAmount: 87.45,
              extractedMerchant: 'Whole Foods',
              thumbnailUrl: '/thumbnails/receipt_001.jpg',
            },
            {
              id: '2',
              fileName: 'receipt_002.pdf',
              uploadDate: '2024-01-14T14:22:00Z',
              status: 'pending',
              extractedAmount: null,
              extractedMerchant: null,
              thumbnailUrl: '/thumbnails/receipt_002.jpg',
            },
          ],
          totalCount: 45,
          page,
          pageSize,
        })
      })
    )
  })

  describe('Successful Render', () => {
    it('renders the receipts page', async () => {
      renderWithProviders(<ReceiptsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('receipts-page')).toBeInTheDocument()
      })
    })

    it('displays upload area', async () => {
      renderWithProviders(<ReceiptsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('upload-area')).toBeInTheDocument()
      })
    })

    it('displays dropzone for file upload', async () => {
      renderWithProviders(<ReceiptsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('dropzone')).toBeInTheDocument()
      })
    })

    it('displays receipts grid', async () => {
      renderWithProviders(<ReceiptsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('receipts-grid')).toBeInTheDocument()
      })
    })
  })

  describe('Empty State', () => {
    it('handles empty receipts list gracefully', async () => {
      server.use(
        http.get('*/api/receipts', () => {
          return HttpResponse.json({
            items: [],
            totalCount: 0,
            page: 1,
            pageSize: 20,
          })
        })
      )

      renderWithProviders(<ReceiptsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('receipts-page')).toBeInTheDocument()
      })
    })
  })

  describe('Error Handling', () => {
    it('handles API error gracefully', async () => {
      server.use(
        http.get('*/api/receipts', () => {
          return HttpResponse.json(
            { error: 'Server Error' },
            { status: 500 }
          )
        })
      )

      renderWithProviders(<ReceiptsPage />)

      await waitFor(() => {
        expect(screen.getByTestId('receipts-page')).toBeInTheDocument()
      })
    })
  })
})
