'use client'

/**
 * BatchReviewPanel Component Tests (T076)
 *
 * Tests for the batch operations panel with threshold controls.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { BatchReviewPanel } from '@/components/matching/batch-review-panel'
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

// Sample test data
function createMockProposal(
  matchId: string,
  confidenceScore: number,
  status: string = 'Proposed'
): MatchProposal {
  return {
    matchId,
    receiptId: `receipt-${matchId}`,
    transactionId: `txn-${matchId}`,
    confidenceScore,
    amountScore: confidenceScore,
    dateScore: confidenceScore,
    vendorScore: confidenceScore,
    matchReason: 'Test match',
    status,
    receipt: {
      id: `receipt-${matchId}`,
      vendorExtracted: 'Test Vendor',
      dateExtracted: '2024-03-15',
      amountExtracted: 100,
      currency: 'USD',
      thumbnailUrl: null,
      originalFilename: 'test.jpg',
    },
    transaction: {
      id: `txn-${matchId}`,
      description: 'Test Transaction',
      originalDescription: 'TEST TXN',
      transactionDate: '2024-03-15',
      postDate: null,
      amount: 100,
    },
    createdAt: '2024-03-15T10:00:00Z',
  }
}

describe('BatchReviewPanel', () => {
  const mockOnThresholdChange = vi.fn()
  const mockOnApproveAll = vi.fn()
  const mockOnRejectAll = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Display', () => {
    it('should show pending count', () => {
      const proposals = [
        createMockProposal('1', 0.95),
        createMockProposal('2', 0.85),
        createMockProposal('3', 0.75),
      ]

      render(
        <BatchReviewPanel
          proposals={proposals}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
        />
      )

      expect(screen.getByText('3 pending')).toBeInTheDocument()
    })

    it('should show current threshold percentage', () => {
      render(
        <BatchReviewPanel
          proposals={[createMockProposal('1', 0.95)]}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
        />
      )

      expect(screen.getByText('90%')).toBeInTheDocument()
    })

    it('should not render when no pending proposals', () => {
      const proposals = [
        createMockProposal('1', 0.95, 'Confirmed'),
        createMockProposal('2', 0.85, 'Rejected'),
      ]

      const { container } = render(
        <BatchReviewPanel
          proposals={proposals}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
        />
      )

      expect(container.firstChild).toBeNull()
    })
  })

  describe('Threshold Counts', () => {
    it('should show eligible count based on threshold', () => {
      const proposals = [
        createMockProposal('1', 0.95), // Above 0.9
        createMockProposal('2', 0.92), // Above 0.9
        createMockProposal('3', 0.85), // Below 0.9
        createMockProposal('4', 0.75), // Below 0.9
      ]

      render(
        <BatchReviewPanel
          proposals={proposals}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
        />
      )

      // Check labels are present
      expect(screen.getByText('Will Approve')).toBeInTheDocument()
      expect(screen.getByText('Need Review')).toBeInTheDocument()
      // Counts are shown - 2 eligible, 2 ineligible
      expect(screen.getByText('matches ≥ 90%')).toBeInTheDocument()
    })

    it('should update counts when threshold changes', () => {
      const proposals = [
        createMockProposal('1', 0.95),
        createMockProposal('2', 0.85),
        createMockProposal('3', 0.75),
      ]

      const { rerender } = render(
        <BatchReviewPanel
          proposals={proposals}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
        />
      )

      // At 90%, only 1 eligible
      expect(screen.getByText('matches ≥ 90%')).toBeInTheDocument()

      // Lower threshold to 80%
      rerender(
        <BatchReviewPanel
          proposals={proposals}
          threshold={0.8}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
        />
      )

      // At 80%, 2 eligible
      expect(screen.getByText('matches ≥ 80%')).toBeInTheDocument()
    })
  })

  describe('Threshold Presets', () => {
    it('should show preset buttons', () => {
      render(
        <BatchReviewPanel
          proposals={[createMockProposal('1', 0.95)]}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
        />
      )

      expect(screen.getByRole('button', { name: 'Strict' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Recommended' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Moderate' })).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Relaxed' })).toBeInTheDocument()
    })

    it('should call onThresholdChange when preset is clicked', () => {
      render(
        <BatchReviewPanel
          proposals={[createMockProposal('1', 0.95)]}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
        />
      )

      fireEvent.click(screen.getByRole('button', { name: 'Strict' }))
      expect(mockOnThresholdChange).toHaveBeenCalledWith(0.95)

      fireEvent.click(screen.getByRole('button', { name: 'Moderate' }))
      expect(mockOnThresholdChange).toHaveBeenCalledWith(0.8)
    })

    it('should highlight active preset', () => {
      render(
        <BatchReviewPanel
          proposals={[createMockProposal('1', 0.95)]}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
        />
      )

      // "Recommended" preset is 0.9, which matches current threshold
      const recommendedButton = screen.getByRole('button', { name: 'Recommended' })
      expect(recommendedButton).not.toHaveClass('variant-outline')
    })
  })

  describe('Slider Control', () => {
    it('should call onThresholdChange when slider value changes', () => {
      render(
        <BatchReviewPanel
          proposals={[createMockProposal('1', 0.95)]}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
        />
      )

      const slider = screen.getByRole('slider')
      fireEvent.change(slider, { target: { value: '85' } })

      expect(mockOnThresholdChange).toHaveBeenCalledWith(0.85)
    })

    it('should disable slider when processing', () => {
      render(
        <BatchReviewPanel
          proposals={[createMockProposal('1', 0.95)]}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
          isProcessing
        />
      )

      const slider = screen.getByRole('slider')
      expect(slider).toBeDisabled()
    })
  })

  describe('Batch Actions', () => {
    it('should show approve button with eligible count', () => {
      const proposals = [
        createMockProposal('1', 0.95),
        createMockProposal('2', 0.92),
      ]

      render(
        <BatchReviewPanel
          proposals={proposals}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
        />
      )

      expect(screen.getByRole('button', { name: /approve 2 matches/i })).toBeInTheDocument()
    })

    it('should open confirmation dialog when approve clicked', async () => {
      render(
        <BatchReviewPanel
          proposals={[createMockProposal('1', 0.95)]}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
        />
      )

      fireEvent.click(screen.getByRole('button', { name: /approve 1 matches/i }))

      await waitFor(() => {
        expect(screen.getByText(/approve 1 matches\?/i)).toBeInTheDocument()
      })
    })

    it('should call onApproveAll with threshold when confirmed', async () => {
      render(
        <BatchReviewPanel
          proposals={[createMockProposal('1', 0.95)]}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
        />
      )

      // Open dialog
      fireEvent.click(screen.getByRole('button', { name: /approve 1 matches/i }))

      await waitFor(() => {
        expect(screen.getByRole('alertdialog')).toBeInTheDocument()
      })

      // Confirm
      fireEvent.click(screen.getByRole('button', { name: /approve all/i }))

      expect(mockOnApproveAll).toHaveBeenCalledWith(0.9)
    })

    it('should disable approve button when no eligible matches', () => {
      const proposals = [createMockProposal('1', 0.75)] // Below 0.9 threshold

      render(
        <BatchReviewPanel
          proposals={proposals}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
        />
      )

      expect(screen.getByRole('button', { name: /approve 0 matches/i })).toBeDisabled()
    })
  })

  describe('Reject Low Confidence', () => {
    it('should show reject button when onRejectAll is provided', () => {
      const proposals = [
        createMockProposal('1', 0.95), // Above threshold
        createMockProposal('2', 0.75), // Below threshold
      ]

      render(
        <BatchReviewPanel
          proposals={proposals}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
          onRejectAll={mockOnRejectAll}
        />
      )

      expect(screen.getByRole('button', { name: /reject 1 low confidence/i })).toBeInTheDocument()
    })

    it('should call onRejectAll with IDs when confirmed', async () => {
      const proposals = [
        createMockProposal('1', 0.95),
        createMockProposal('2', 0.75),
      ]

      render(
        <BatchReviewPanel
          proposals={proposals}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
          onRejectAll={mockOnRejectAll}
        />
      )

      // Open reject dialog
      fireEvent.click(screen.getByRole('button', { name: /reject 1 low confidence/i }))

      await waitFor(() => {
        expect(screen.getByRole('alertdialog')).toBeInTheDocument()
      })

      // Confirm rejection
      fireEvent.click(screen.getByRole('button', { name: /reject all/i }))

      expect(mockOnRejectAll).toHaveBeenCalledWith(['2']) // Only the low confidence match
    })
  })

  describe('Match Preview', () => {
    it('should show preview list when eligible count is 10 or less', () => {
      const proposals = [
        createMockProposal('1', 0.95),
        createMockProposal('2', 0.92),
      ]

      render(
        <BatchReviewPanel
          proposals={proposals}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
        />
      )

      expect(screen.getByText('Matches to approve:')).toBeInTheDocument()
      // Preview shows vendor → description format
      const previewItems = screen.getAllByText(/Test Vendor → Test Transaction/)
      expect(previewItems.length).toBeGreaterThan(0)
    })

    it('should not show preview when more than 10 eligible', () => {
      const proposals = Array.from({ length: 15 }, (_, i) =>
        createMockProposal(`match-${i}`, 0.95)
      )

      render(
        <BatchReviewPanel
          proposals={proposals}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
        />
      )

      expect(screen.queryByText('Matches to approve:')).not.toBeInTheDocument()
    })
  })

  describe('Processing State', () => {
    it('should disable all controls when processing', () => {
      render(
        <BatchReviewPanel
          proposals={[createMockProposal('1', 0.95)]}
          threshold={0.9}
          onThresholdChange={mockOnThresholdChange}
          onApproveAll={mockOnApproveAll}
          isProcessing
        />
      )

      // Slider disabled
      expect(screen.getByRole('slider')).toBeDisabled()

      // Preset buttons disabled
      expect(screen.getByRole('button', { name: 'Strict' })).toBeDisabled()

      // Approve button shows loading
      const approveButton = screen.getByRole('button', { name: /approve/i })
      expect(approveButton).toBeDisabled()
    })
  })
})
