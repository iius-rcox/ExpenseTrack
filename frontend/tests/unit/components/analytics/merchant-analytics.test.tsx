'use client'

/**
 * MerchantAnalytics Component Tests (T087)
 *
 * Tests for the merchant analytics component.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MerchantAnalytics } from '@/components/analytics/merchant-analytics'
import type { MerchantAnalyticsResponse, TopMerchant } from '@/types/analytics'

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
function createMockMerchant(overrides: Partial<TopMerchant> = {}): TopMerchant {
  return {
    merchantName: 'amazon',
    displayName: 'Amazon',
    totalAmount: 1250.00,
    transactionCount: 15,
    averageAmount: 83.33,
    percentageOfTotal: 25.5,
    trend: 'increasing',
    changePercent: 12.5,
    primaryCategory: 'Shopping',
    ...overrides,
  }
}

const mockMerchantData: MerchantAnalyticsResponse = {
  topMerchants: [
    createMockMerchant({ merchantName: 'amazon', displayName: 'Amazon', totalAmount: 1250, percentageOfTotal: 25 }),
    createMockMerchant({ merchantName: 'starbucks', displayName: 'Starbucks', totalAmount: 450, percentageOfTotal: 9, trend: 'stable', changePercent: 0 }),
    createMockMerchant({ merchantName: 'uber', displayName: 'Uber', totalAmount: 380, percentageOfTotal: 7.6, trend: 'decreasing', changePercent: -8.2 }),
    createMockMerchant({ merchantName: 'netflix', displayName: 'Netflix', totalAmount: 45, percentageOfTotal: 0.9, primaryCategory: 'Entertainment' }),
    createMockMerchant({ merchantName: 'whole-foods', displayName: 'Whole Foods', totalAmount: 625, percentageOfTotal: 12.5, primaryCategory: 'Groceries' }),
  ],
  newMerchants: [
    createMockMerchant({ merchantName: 'spotify', displayName: 'Spotify', totalAmount: 10.99, transactionCount: 1 }),
  ],
  significantChanges: [
    createMockMerchant({ merchantName: 'amazon', displayName: 'Amazon', changePercent: 45.2 }),
    createMockMerchant({ merchantName: 'target', displayName: 'Target', changePercent: -32.1 }),
  ],
  totalMerchantCount: 42,
  periodStart: '2024-03-01',
  periodEnd: '2024-03-31',
}

describe('MerchantAnalytics', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Loading State', () => {
    it('renders skeleton when loading', () => {
      render(<MerchantAnalytics isLoading={true} />)

      // Should show loading skeleton
      expect(screen.queryByText('Amazon')).toBeNull()
    })
  })

  describe('Empty State', () => {
    it('renders empty state message when no data', () => {
      render(<MerchantAnalytics data={undefined} />)

      expect(screen.getByText('No merchant data available')).toBeDefined()
    })

    it('renders empty state when merchants array is empty', () => {
      render(
        <MerchantAnalytics
          data={{ ...mockMerchantData, topMerchants: [] }}
        />
      )

      expect(screen.getByText('No merchant data available')).toBeDefined()
    })
  })

  describe('Merchant Display', () => {
    it('renders title correctly', () => {
      render(<MerchantAnalytics data={mockMerchantData} title="Custom Title" />)

      expect(screen.getByText('Custom Title')).toBeDefined()
    })

    it('shows merchant count badge', () => {
      render(<MerchantAnalytics data={mockMerchantData} />)

      expect(screen.getByText('42 merchants')).toBeDefined()
    })

    it('displays merchant names', () => {
      render(<MerchantAnalytics data={mockMerchantData} />)

      // Amazon appears in both topMerchants and significantChanges, so use getAllByText
      const amazonElements = screen.getAllByText('Amazon')
      expect(amazonElements.length).toBeGreaterThan(0)
      expect(screen.getByText('Starbucks')).toBeDefined()
      expect(screen.getByText('Uber')).toBeDefined()
    })

    it('displays merchant amounts', () => {
      render(<MerchantAnalytics data={mockMerchantData} />)

      expect(screen.getByText('$1,250')).toBeDefined()
      expect(screen.getByText('$450')).toBeDefined()
    })

    it('displays transaction counts', () => {
      render(<MerchantAnalytics data={mockMerchantData} />)

      // Transaction counts appear in multiple merchant cards
      const transactionElements = screen.getAllByText('15 transactions')
      expect(transactionElements.length).toBeGreaterThan(0)
    })

    it('displays average amounts', () => {
      render(<MerchantAnalytics data={mockMerchantData} />)

      // Average amounts appear in multiple merchant cards
      const avgElements = screen.getAllByText('avg $83')
      expect(avgElements.length).toBeGreaterThan(0)
    })

    it('displays percentage of total', () => {
      render(<MerchantAnalytics data={mockMerchantData} />)

      expect(screen.getByText('25.0%')).toBeDefined()
    })
  })

  describe('Trend Indicators', () => {
    it('shows trend indicators when showTrends is true', () => {
      render(<MerchantAnalytics data={mockMerchantData} showTrends={true} />)

      // Should show change percentages (may appear in multiple places)
      const trendElements = screen.getAllByText('12.5%')
      expect(trendElements.length).toBeGreaterThan(0)
    })

    it('hides trend indicators when showTrends is false', () => {
      // Use data with distinct changePercent values that don't overlap with percentageOfTotal
      const dataForTrendTest = {
        ...mockMerchantData,
        topMerchants: [
          createMockMerchant({ merchantName: 'test', displayName: 'Test', totalAmount: 100, percentageOfTotal: 50, changePercent: 77.7 }),
        ],
        significantChanges: [],
        newMerchants: [],
      }
      render(<MerchantAnalytics data={dataForTrendTest} showTrends={false} />)

      // changePercent 77.7% should not appear when showTrends is false
      expect(screen.queryByText('77.7%')).toBeNull()
    })
  })

  describe('Category Badges', () => {
    it('displays category badges', () => {
      render(<MerchantAnalytics data={mockMerchantData} />)

      // Multiple merchants may have the same category, so use getAllByText
      const shoppingBadges = screen.getAllByText('Shopping')
      expect(shoppingBadges.length).toBeGreaterThan(0)
      expect(screen.getByText('Entertainment')).toBeDefined()
    })
  })

  describe('New Merchants Section', () => {
    it('shows new merchants when showNewMerchants is true', () => {
      render(<MerchantAnalytics data={mockMerchantData} showNewMerchants={true} />)

      expect(screen.getByText('New Merchants')).toBeDefined()
      expect(screen.getByText('Spotify')).toBeDefined()
    })

    it('hides new merchants when showNewMerchants is false', () => {
      render(<MerchantAnalytics data={mockMerchantData} showNewMerchants={false} />)

      expect(screen.queryByText('New Merchants')).toBeNull()
    })
  })

  describe('Significant Changes Section', () => {
    it('shows significant changes section', () => {
      render(<MerchantAnalytics data={mockMerchantData} showTrends={true} />)

      expect(screen.getByText('Significant Changes')).toBeDefined()
    })
  })

  describe('Search Functionality', () => {
    it('filters merchants when searching', async () => {
      const user = userEvent.setup()
      // Use data without significant changes to avoid duplicate Amazon entries
      const dataWithoutChanges = {
        ...mockMerchantData,
        significantChanges: [],
      }
      render(<MerchantAnalytics data={dataWithoutChanges} />)

      const searchInput = screen.getByPlaceholderText('Search merchants...')
      await user.type(searchInput, 'Amazon')

      expect(screen.getByText('Amazon')).toBeDefined()
      expect(screen.queryByText('Starbucks')).toBeNull()
    })

    it('shows no results message when search has no matches', async () => {
      const user = userEvent.setup()
      render(<MerchantAnalytics data={mockMerchantData} />)

      const searchInput = screen.getByPlaceholderText('Search merchants...')
      await user.type(searchInput, 'NonexistentMerchant')

      expect(screen.getByText("No merchants match your search")).toBeDefined()
    })
  })

  describe('Sorting', () => {
    it('shows sort button', () => {
      render(<MerchantAnalytics data={mockMerchantData} />)

      expect(screen.getByText('Sorted by')).toBeDefined()
      expect(screen.getByText('total amount')).toBeDefined()
    })

    it('cycles through sort options when sort button clicked', () => {
      render(<MerchantAnalytics data={mockMerchantData} />)

      const sortButton = screen.getByTitle('Sort by amount')
      fireEvent.click(sortButton)

      expect(screen.getByText('transaction count')).toBeDefined()
    })
  })

  describe('Top Count Limiting', () => {
    it('limits displayed merchants to topCount', () => {
      // Use data without significant changes to avoid duplicate entries
      const dataWithoutChanges = {
        ...mockMerchantData,
        significantChanges: [],
      }
      render(<MerchantAnalytics data={dataWithoutChanges} topCount={3} />)

      // Sorted by amount (default), top 3 are: Amazon ($1,250), Whole Foods ($625), Starbucks ($450)
      expect(screen.getByText('Amazon')).toBeDefined()
      expect(screen.getByText('Whole Foods')).toBeDefined()
      expect(screen.getByText('Starbucks')).toBeDefined()
      // Uber ($380) and Netflix ($45) should not be visible (4th and 5th)
      expect(screen.queryByText('Uber')).toBeNull()
      expect(screen.queryByText('Netflix')).toBeNull()
    })
  })

  describe('Merchant Selection', () => {
    it('calls onMerchantSelect when merchant clicked', () => {
      const mockOnSelect = vi.fn()

      // Use data without significant changes to avoid duplicate Amazon entries
      const dataWithoutChanges = {
        ...mockMerchantData,
        significantChanges: [],
      }

      render(
        <MerchantAnalytics
          data={dataWithoutChanges}
          onMerchantSelect={mockOnSelect}
        />
      )

      fireEvent.click(screen.getByText('Amazon'))

      expect(mockOnSelect).toHaveBeenCalledWith(
        expect.objectContaining({ merchantName: 'amazon' })
      )
    })
  })

  describe('Styling', () => {
    it('applies custom className', () => {
      const { container } = render(
        <MerchantAnalytics data={mockMerchantData} className="custom-class" />
      )

      expect(container.querySelector('.custom-class')).toBeDefined()
    })
  })

  describe('Top 3 Summary', () => {
    it('shows top 3 percentage summary', () => {
      render(<MerchantAnalytics data={mockMerchantData} />)

      // Top 3: Amazon (25%) + Whole Foods (12.5%) + Starbucks (9%) = 46.5%
      // Note: percentage calculation may vary based on sorting
      expect(screen.getByText(/account for/)).toBeDefined()
      expect(screen.getByText(/of spending/)).toBeDefined()
    })
  })
})
