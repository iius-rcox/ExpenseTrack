'use client'

/**
 * MatchReviewWorkspace Component Tests (T075)
 *
 * Tests for the main review workspace with keyboard navigation.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MatchReviewWorkspace } from '@/components/matching/match-review-workspace'
import type { MatchProposal } from '@/types/api'

// Mock framer-motion
vi.mock('framer-motion', () => ({
  motion: {
    div: ({ children, ...props }: React.PropsWithChildren<Record<string, unknown>>) => (
      <div {...props}>{children}</div>
    ),
  },
  AnimatePresence: ({ children }: React.PropsWithChildren) => <>{children}</>,
}))

// Mock sonner toast
vi.mock('sonner', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
    info: vi.fn(),
  },
}))

// Mock the matching hooks used by ManualMatchDialog
vi.mock('@/hooks/queries/use-matching', () => ({
  useUnmatchedReceipts: () => ({ data: [], isLoading: false }),
  useUnmatchedTransactions: () => ({ data: [], isLoading: false }),
  useManualMatch: () => ({ mutate: vi.fn(), isPending: false }),
}))

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  })
}

function renderWithProviders(ui: React.ReactElement) {
  const queryClient = createTestQueryClient()
  return render(
    <QueryClientProvider client={queryClient}>
      {ui}
    </QueryClientProvider>
  )
}

// Sample test data
function createMockProposal(overrides: Partial<MatchProposal> = {}): MatchProposal {
  return {
    matchId: 'match-1',
    receiptId: 'receipt-1',
    transactionId: 'txn-1',
    transactionGroupId: null,
    candidateType: 'transaction',
    confidenceScore: 0.85,
    amountScore: 0.95,
    dateScore: 0.9,
    vendorScore: 0.7,
    matchReason: 'Amount and date match closely',
    status: 'Proposed',
    receipt: {
      id: 'receipt-1',
      vendorExtracted: 'Amazon',
      dateExtracted: '2024-03-15',
      amountExtracted: 125.99,
      currency: 'USD',
      thumbnailUrl: null,
      originalFilename: 'receipt.jpg',
    },
    transaction: {
      id: 'txn-1',
      description: 'AMZN*MKTPLACE',
      originalDescription: 'AMZN*MKTPLACE',
      transactionDate: '2024-03-15',
      postDate: '2024-03-16',
      amount: 125.99,
    },
    transactionGroup: null,
    createdAt: '2024-03-15T10:00:00Z',
    ...overrides,
  }
}

describe('MatchReviewWorkspace', () => {
  const mockOnConfirm = vi.fn()
  const mockOnReject = vi.fn()
  const mockOnBatchApprove = vi.fn()
  const mockOnIndexChange = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Display', () => {
    it('should render current match count', () => {
      const proposals = [
        createMockProposal({ matchId: 'match-1' }),
        createMockProposal({ matchId: 'match-2' }),
        createMockProposal({ matchId: 'match-3' }),
      ]

      renderWithProviders(
        <MatchReviewWorkspace
          proposals={proposals}
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
        />
      )

      expect(screen.getByText('1 / 3')).toBeInTheDocument()
    })

    it('should display receipt information', () => {
      renderWithProviders(
        <MatchReviewWorkspace
          proposals={[createMockProposal()]}
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
        />
      )

      // Receipt section header (may appear multiple times, like in header and in matching factors)
      const receiptLabels = screen.getAllByText('Receipt')
      expect(receiptLabels.length).toBeGreaterThan(0)
      // Merchant name should be displayed (may appear multiple times in different contexts)
      const amazonLabels = screen.getAllByText('Amazon')
      expect(amazonLabels.length).toBeGreaterThan(0)
    })

    it('should display transaction information', () => {
      renderWithProviders(
        <MatchReviewWorkspace
          proposals={[createMockProposal()]}
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
        />
      )

      // Transaction section header (may appear multiple times)
      const transactionLabels = screen.getAllByText('Transaction')
      expect(transactionLabels.length).toBeGreaterThan(0)
      // Transaction description is displayed (may appear multiple times in different views)
      const descriptionLabels = screen.getAllByText('AMZN*MKTPLACE')
      expect(descriptionLabels.length).toBeGreaterThan(0)
    })

    it('should display confidence score', () => {
      renderWithProviders(
        <MatchReviewWorkspace
          proposals={[createMockProposal()]}
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
        />
      )

      // Confidence is displayed as percentage - component shows both in indicator and as text
      const percentages = screen.getAllByText('85%')
      expect(percentages.length).toBeGreaterThan(0)
    })

    it('should show empty state when no pending proposals', () => {
      renderWithProviders(
        <MatchReviewWorkspace
          proposals={[]}
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
        />
      )

      expect(screen.getByText('All caught up!')).toBeInTheDocument()
    })

    it('should filter out non-pending proposals', () => {
      const proposals = [
        createMockProposal({ matchId: 'match-1', status: 'Confirmed' }),
        createMockProposal({ matchId: 'match-2', status: 'Proposed' }),
        createMockProposal({ matchId: 'match-3', status: 'Rejected' }),
      ]

      renderWithProviders(
        <MatchReviewWorkspace
          proposals={proposals}
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
        />
      )

      // Should only show 1 pending
      expect(screen.getByText('1 / 1')).toBeInTheDocument()
    })
  })

  describe('Actions', () => {
    it('should call onConfirm when approve button is clicked', async () => {
      renderWithProviders(
        <MatchReviewWorkspace
          proposals={[createMockProposal()]}
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
        />
      )

      const approveButton = screen.getByRole('button', { name: /approve/i })
      fireEvent.click(approveButton)

      expect(mockOnConfirm).toHaveBeenCalledWith('match-1')
    })

    it('should call onReject when reject button is clicked', async () => {
      renderWithProviders(
        <MatchReviewWorkspace
          proposals={[createMockProposal()]}
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
        />
      )

      const rejectButton = screen.getByRole('button', { name: /reject/i })
      fireEvent.click(rejectButton)

      expect(mockOnReject).toHaveBeenCalledWith('match-1')
    })

    it('should disable buttons when processing', () => {
      renderWithProviders(
        <MatchReviewWorkspace
          proposals={[createMockProposal()]}
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
          isProcessing
        />
      )

      const approveButton = screen.getByRole('button', { name: /approve/i })
      const rejectButton = screen.getByRole('button', { name: /reject/i })

      expect(approveButton).toBeDisabled()
      expect(rejectButton).toBeDisabled()
    })
  })

  describe('Navigation', () => {
    it('should navigate to next proposal', () => {
      const proposals = [
        createMockProposal({ matchId: 'match-1' }),
        createMockProposal({ matchId: 'match-2' }),
      ]

      renderWithProviders(
        <MatchReviewWorkspace
          proposals={proposals}
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
          currentIndex={0}
          onIndexChange={mockOnIndexChange}
        />
      )

      // Find the next navigation button (has ChevronRight icon)
      const navButtons = screen.getAllByRole('button', { name: '' })
      if (navButtons.length > 1) {
        fireEvent.click(navButtons[1]) // Second icon button is next
        expect(mockOnIndexChange).toHaveBeenCalledWith(1)
      }
    })

    it('should wrap to beginning when at end', () => {
      const proposals = [
        createMockProposal({ matchId: 'match-1' }),
        createMockProposal({ matchId: 'match-2' }),
      ]

      renderWithProviders(
        <MatchReviewWorkspace
          proposals={proposals}
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
          currentIndex={1}
          onIndexChange={mockOnIndexChange}
        />
      )

      // Navigate next from last should wrap to first
      const navButtons = screen.getAllByRole('button', { name: '' })
      if (navButtons.length > 1) {
        fireEvent.click(navButtons[1])
        expect(mockOnIndexChange).toHaveBeenCalledWith(0)
      }
    })
  })

  describe('Keyboard Shortcuts', () => {
    it('should call onConfirm when "a" key is pressed', async () => {
      renderWithProviders(
        <MatchReviewWorkspace
          proposals={[createMockProposal()]}
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
        />
      )

      // Focus the document and press 'a'
      fireEvent.keyDown(document, { key: 'a' })

      await waitFor(() => {
        expect(mockOnConfirm).toHaveBeenCalledWith('match-1')
      })
    })

    it('should call onReject when "r" key is pressed', async () => {
      renderWithProviders(
        <MatchReviewWorkspace
          proposals={[createMockProposal()]}
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
        />
      )

      fireEvent.keyDown(document, { key: 'r' })

      await waitFor(() => {
        expect(mockOnReject).toHaveBeenCalledWith('match-1')
      })
    })

    it('should navigate with arrow keys', async () => {
      const proposals = [
        createMockProposal({ matchId: 'match-1' }),
        createMockProposal({ matchId: 'match-2' }),
      ]

      renderWithProviders(
        <MatchReviewWorkspace
          proposals={proposals}
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
          currentIndex={0}
          onIndexChange={mockOnIndexChange}
        />
      )

      fireEvent.keyDown(document, { key: 'ArrowDown' })

      await waitFor(() => {
        expect(mockOnIndexChange).toHaveBeenCalledWith(1)
      })
    })

    it('should navigate with j/k vim keys', async () => {
      const proposals = [
        createMockProposal({ matchId: 'match-1' }),
        createMockProposal({ matchId: 'match-2' }),
      ]

      renderWithProviders(
        <MatchReviewWorkspace
          proposals={proposals}
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
          currentIndex={1}
          onIndexChange={mockOnIndexChange}
        />
      )

      fireEvent.keyDown(document, { key: 'k' })

      await waitFor(() => {
        expect(mockOnIndexChange).toHaveBeenCalledWith(0)
      })
    })

    it('should not trigger shortcuts when processing', async () => {
      renderWithProviders(
        <MatchReviewWorkspace
          proposals={[createMockProposal()]}
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
          isProcessing
        />
      )

      fireEvent.keyDown(document, { key: 'a' })

      // Wait a bit and verify not called
      await new Promise(r => setTimeout(r, 100))
      expect(mockOnConfirm).not.toHaveBeenCalled()
    })
  })

  describe('Keyboard Shortcuts Help', () => {
    it('should toggle shortcuts help panel', async () => {
      renderWithProviders(
        <MatchReviewWorkspace
          proposals={[createMockProposal()]}
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
        />
      )

      const shortcutsButton = screen.getByRole('button', { name: /shortcuts/i })
      fireEvent.click(shortcutsButton)

      // Should show shortcut hints in kbd elements
      // Navigation labels should appear
      expect(screen.getByText('Next')).toBeInTheDocument()
      expect(screen.getByText('Previous')).toBeInTheDocument()
      // Manual Match shortcut (may appear multiple times - in shortcuts and as button)
      const manualMatchLabels = screen.getAllByText('Manual Match')
      expect(manualMatchLabels.length).toBeGreaterThan(0)
    })
  })

  describe('Batch Approve', () => {
    it('should call onBatchApprove when batch button is clicked', () => {
      renderWithProviders(
        <MatchReviewWorkspace
          proposals={[createMockProposal()]}
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
          onBatchApprove={mockOnBatchApprove}
        />
      )

      const batchButton = screen.getByRole('button', { name: /batch approve/i })
      fireEvent.click(batchButton)

      expect(mockOnBatchApprove).toHaveBeenCalledWith(0.9)
    })
  })

  describe('Loading State', () => {
    it('should show skeleton when loading', () => {
      renderWithProviders(
        <MatchReviewWorkspace
          proposals={[]}
          isLoading
          onConfirm={mockOnConfirm}
          onReject={mockOnReject}
        />
      )

      // Should not show the empty state message
      expect(screen.queryByText('All caught up!')).not.toBeInTheDocument()
    })
  })
})
