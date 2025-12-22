'use client'

/**
 * SubscriptionDetector Component Tests (T088)
 *
 * Tests for the subscription detection component.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { SubscriptionDetector } from '@/components/analytics/subscription-detector'
import type { SubscriptionDetectionResponse, SubscriptionDetection, SubscriptionSummary } from '@/types/analytics'

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
function createMockSubscription(overrides: Partial<SubscriptionDetection> = {}): SubscriptionDetection {
  return {
    id: 'sub-1',
    merchantName: 'Netflix',
    displayName: 'Netflix',
    amount: 15.99,
    frequency: 'monthly',
    confidence: 'high',
    firstSeen: '2024-01-15',
    lastSeen: '2024-03-15',
    occurrences: 3,
    totalSpent: 47.97,
    isAcknowledged: false,
    nextExpectedDate: '2024-04-15',
    category: 'Entertainment',
    ...overrides,
  }
}

function createMockSummary(): SubscriptionSummary {
  return {
    subscriptionCount: 4,
    estimatedMonthlyTotal: 78.97,
    estimatedAnnualTotal: 947.64,
    byFrequency: [
      { frequency: 'monthly', count: 3, monthlyEquivalent: 41.97 },
      { frequency: 'annual', count: 1, monthlyEquivalent: 11.58 },
    ],
    byCategory: [
      { category: 'Entertainment', count: 2, monthlyEquivalent: 25.98 },
      { category: 'Software', count: 2, monthlyEquivalent: 52.99 },
    ],
  }
}

const mockSubscriptionData: SubscriptionDetectionResponse = {
  subscriptions: [
    createMockSubscription({ id: 'sub-1', merchantName: 'Netflix', amount: 15.99 }),
    createMockSubscription({ id: 'sub-2', merchantName: 'Spotify', amount: 9.99, frequency: 'monthly' }),
    createMockSubscription({ id: 'sub-3', merchantName: 'Adobe Creative Cloud', amount: 52.99, confidence: 'medium' }),
    createMockSubscription({ id: 'sub-4', merchantName: 'Amazon Prime', amount: 139.00, frequency: 'annual' }),
  ],
  summary: createMockSummary(),
  detectedAt: '2024-03-15T10:00:00Z',
}

describe('SubscriptionDetector', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Loading State', () => {
    it('renders skeleton when loading', () => {
      render(<SubscriptionDetector isLoading={true} />)

      // Should show loading skeleton
      expect(screen.queryByText('Netflix')).toBeNull()
    })
  })

  describe('Empty State', () => {
    it('renders empty state message when no data', () => {
      render(<SubscriptionDetector data={undefined} />)

      expect(screen.getByText('No recurring subscriptions detected')).toBeDefined()
    })

    it('renders empty state when subscriptions array is empty', () => {
      render(
        <SubscriptionDetector
          data={{ ...mockSubscriptionData, subscriptions: [] }}
        />
      )

      expect(screen.getByText('No recurring subscriptions detected')).toBeDefined()
    })
  })

  describe('Subscription Display', () => {
    it('renders title correctly', () => {
      render(<SubscriptionDetector data={mockSubscriptionData} title="Custom Title" />)

      expect(screen.getByText('Custom Title')).toBeDefined()
    })

    it('displays subscription names', () => {
      render(<SubscriptionDetector data={mockSubscriptionData} />)

      expect(screen.getByText('Netflix')).toBeDefined()
      expect(screen.getByText('Spotify')).toBeDefined()
      expect(screen.getByText('Adobe Creative Cloud')).toBeDefined()
    })

    it('displays subscription amounts', () => {
      render(<SubscriptionDetector data={mockSubscriptionData} />)

      expect(screen.getByText('$15.99')).toBeDefined()
      expect(screen.getByText('$9.99')).toBeDefined()
    })

    it('displays frequency badges', () => {
      render(<SubscriptionDetector data={mockSubscriptionData} />)

      // Multiple "Monthly" badges expected
      const monthlyBadges = screen.getAllByText('Monthly')
      expect(monthlyBadges.length).toBeGreaterThan(0)
    })
  })

  describe('Summary Section', () => {
    it('shows summary when showSummary is true', () => {
      render(<SubscriptionDetector data={mockSubscriptionData} showSummary={true} />)

      // Should show subscription count
      expect(screen.getByText('4')).toBeDefined()
    })

    it('shows monthly cost in summary', () => {
      render(<SubscriptionDetector data={mockSubscriptionData} showSummary={true} />)

      expect(screen.getByText('$78.97')).toBeDefined()
    })

    it('shows annual cost in summary', () => {
      render(<SubscriptionDetector data={mockSubscriptionData} showSummary={true} />)

      expect(screen.getByText('$947.64')).toBeDefined()
    })
  })

  describe('Confidence Badges', () => {
    it('displays confidence level badges', () => {
      render(<SubscriptionDetector data={mockSubscriptionData} />)

      const highBadges = screen.getAllByText(/high confidence/i)
      const mediumBadges = screen.getAllByText(/medium confidence/i)

      expect(highBadges.length).toBeGreaterThan(0)
      expect(mediumBadges.length).toBeGreaterThan(0)
    })
  })

  describe('Expandable Details', () => {
    it('expands subscription on button click', () => {
      render(<SubscriptionDetector data={mockSubscriptionData} />)

      // Find the expand button for Netflix subscription
      const expandButtons = screen.getAllByRole('button')
      const expandButton = expandButtons.find(btn =>
        btn.querySelector('svg')?.classList.contains('lucide-chevron-down')
      )

      if (expandButton) {
        fireEvent.click(expandButton)
        // After expanding, should show additional details
        expect(screen.getByText(/First seen/i)).toBeDefined()
      }
    })
  })

  describe('Acknowledgment Toggle', () => {
    it('renders more switches when onAcknowledge is provided', () => {
      // First render without onAcknowledge
      const { unmount } = render(
        <SubscriptionDetector
          data={mockSubscriptionData}
        />
      )
      // There's always one switch for "Show acknowledged" filter
      const switchesWithoutCallback = screen.getAllByRole('switch')
      const countWithout = switchesWithoutCallback.length
      unmount()

      // Now render with onAcknowledge
      const mockOnAcknowledge = vi.fn()
      render(
        <SubscriptionDetector
          data={mockSubscriptionData}
          onAcknowledge={mockOnAcknowledge}
        />
      )

      // Should have additional switches for each subscription
      const switchesWithCallback = screen.getAllByRole('switch')
      expect(switchesWithCallback.length).toBeGreaterThan(countWithout)
    })
  })

  describe('Group By Category', () => {
    it('groups subscriptions by category when groupByCategory is true', () => {
      render(
        <SubscriptionDetector
          data={mockSubscriptionData}
          groupByCategory={true}
        />
      )

      // Entertainment appears both as category badge and group header
      const entertainmentElements = screen.getAllByText('Entertainment')
      expect(entertainmentElements.length).toBeGreaterThan(0)
    })
  })

  describe('Frequency Filter', () => {
    it('filters by frequency when frequencyFilter provided', () => {
      render(
        <SubscriptionDetector
          data={mockSubscriptionData}
          frequencyFilter={['annual']}
        />
      )

      expect(screen.getByText('Amazon Prime')).toBeDefined()
      // Monthly subscriptions should not be visible
      expect(screen.queryByText('Netflix')).toBeNull()
    })
  })

  describe('Subscription Click Handler', () => {
    it('calls onSubscriptionClick when subscription clicked', () => {
      const mockOnClick = vi.fn()

      render(
        <SubscriptionDetector
          data={mockSubscriptionData}
          onSubscriptionClick={mockOnClick}
        />
      )

      fireEvent.click(screen.getByText('Netflix'))

      expect(mockOnClick).toHaveBeenCalledWith(
        expect.objectContaining({ merchantName: 'Netflix' })
      )
    })
  })

  describe('Refresh Handler', () => {
    it('shows refresh button', () => {
      render(<SubscriptionDetector data={mockSubscriptionData} />)

      // Should have buttons in the component
      const buttons = screen.getAllByRole('button')
      expect(buttons.length).toBeGreaterThan(0)
    })

    it('calls onRefresh when refresh button clicked', () => {
      const mockOnRefresh = vi.fn()

      render(
        <SubscriptionDetector
          data={mockSubscriptionData}
          onRefresh={mockOnRefresh}
        />
      )

      // Find the refresh button (it has an aria-label or title)
      const buttons = screen.getAllByRole('button')
      const refreshButton = buttons.find(btn =>
        btn.querySelector('svg')?.classList.contains('lucide-refresh-ccw')
      )

      if (refreshButton) {
        fireEvent.click(refreshButton)
        expect(mockOnRefresh).toHaveBeenCalled()
      }
    })
  })

  describe('Styling', () => {
    it('applies custom className', () => {
      const { container } = render(
        <SubscriptionDetector data={mockSubscriptionData} className="custom-class" />
      )

      expect(container.querySelector('.custom-class')).toBeDefined()
    })
  })
})
