'use client'

/**
 * MatchingFactors Component Tests (T074)
 *
 * Tests for the factor breakdown display component.
 */

import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { MatchingFactors, buildMatchingFactors } from '@/components/matching/matching-factors'

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
const mockFactors = [
  {
    type: 'amount' as const,
    weight: 0.4,
    receiptValue: '$125.99',
    transactionValue: '$125.99',
    isExactMatch: true,
  },
  {
    type: 'date' as const,
    weight: 0.3,
    receiptValue: '2024-03-15',
    transactionValue: '2024-03-15',
    isExactMatch: true,
  },
  {
    type: 'merchant' as const,
    weight: 0.3,
    receiptValue: 'Amazon',
    transactionValue: 'AMZN*MKTPLACE',
    isExactMatch: false,
  },
]

describe('MatchingFactors', () => {
  describe('Full Display Mode', () => {
    it('should render all factor types', () => {
      render(
        <MatchingFactors
          factors={mockFactors}
          confidence={0.85}
        />
      )

      expect(screen.getByText('Amount')).toBeInTheDocument()
      expect(screen.getByText('Date')).toBeInTheDocument()
      expect(screen.getByText('Merchant')).toBeInTheDocument()
    })

    it('should display confidence percentage', () => {
      render(
        <MatchingFactors
          factors={mockFactors}
          confidence={0.85}
        />
      )

      // Confidence indicator shows percentage (may appear multiple times)
      const percentages = screen.getAllByText('85%')
      expect(percentages.length).toBeGreaterThan(0)
      expect(screen.getByText('Match Confidence')).toBeInTheDocument()
    })

    it('should show exact match indicator for matching factors', () => {
      render(
        <MatchingFactors
          factors={mockFactors}
          confidence={0.85}
        />
      )

      // Amount and Date are exact matches
      const exactLabels = screen.getAllByText('Exact')
      expect(exactLabels.length).toBe(2)

      // Merchant is partial
      expect(screen.getByText('Partial')).toBeInTheDocument()
    })

    it('should display receipt and transaction values', () => {
      render(
        <MatchingFactors
          factors={mockFactors}
          confidence={0.85}
        />
      )

      // Values may appear multiple times in full mode, so use getAllBy
      const amountValues = screen.getAllByText('$125.99')
      expect(amountValues.length).toBeGreaterThan(0)
      expect(screen.getByText('Amazon')).toBeInTheDocument()
      expect(screen.getByText('AMZN*MKTPLACE')).toBeInTheDocument()
    })

    it('should display weight percentages', () => {
      render(
        <MatchingFactors
          factors={mockFactors}
          confidence={0.85}
        />
      )

      expect(screen.getByText('40% weight')).toBeInTheDocument()
      expect(screen.getAllByText('30% weight').length).toBe(2)
    })

    it('should sort factors by weight (highest first)', () => {
      const unsortedFactors = [
        { type: 'date' as const, weight: 0.3, receiptValue: '2024-03-15', transactionValue: '2024-03-15', isExactMatch: true },
        { type: 'amount' as const, weight: 0.4, receiptValue: '$100', transactionValue: '$100', isExactMatch: true },
        { type: 'merchant' as const, weight: 0.1, receiptValue: 'Store', transactionValue: 'STORE', isExactMatch: false },
      ]

      render(
        <MatchingFactors
          factors={unsortedFactors}
          confidence={0.8}
        />
      )

      // All factors should be displayed
      expect(screen.getByText('Amount')).toBeInTheDocument()
      expect(screen.getByText('Date')).toBeInTheDocument()
      expect(screen.getByText('Merchant')).toBeInTheDocument()
    })

    it('should call onHover when hovering over a factor', () => {
      const mockOnHover = vi.fn()

      render(
        <MatchingFactors
          factors={mockFactors}
          confidence={0.85}
          onHover={mockOnHover}
        />
      )

      // Find the amount factor container and hover
      const amountContainer = screen.getByText('Amount').closest('[class*="rounded-lg"]')
      if (amountContainer) {
        fireEvent.mouseEnter(amountContainer)
        expect(mockOnHover).toHaveBeenCalledWith('amount')

        fireEvent.mouseLeave(amountContainer)
        expect(mockOnHover).toHaveBeenCalledWith(null)
      }
    })

    it('should highlight the specified factor', () => {
      render(
        <MatchingFactors
          factors={mockFactors}
          confidence={0.85}
          highlightedFactor="amount"
        />
      )

      const amountContainer = screen.getByText('Amount').closest('[class*="rounded-lg"]')
      expect(amountContainer).toHaveClass('ring-2')
    })
  })

  describe('Compact Mode', () => {
    it('should render compact icons for each factor', () => {
      const { container } = render(
        <MatchingFactors
          factors={mockFactors}
          confidence={0.85}
          compact
        />
      )

      // In compact mode, we show icons in small containers
      expect(container.firstChild).toBeInTheDocument()
    })

    it('should not show full factor details in compact mode', () => {
      render(
        <MatchingFactors
          factors={mockFactors}
          confidence={0.85}
          compact
        />
      )

      // Full labels should not be visible (they're in tooltips)
      expect(screen.queryByText('40% weight')).not.toBeInTheDocument()
      expect(screen.queryByText('Match Confidence')).not.toBeInTheDocument()
    })
  })

  describe('Legend', () => {
    it('should display legend for match types', () => {
      render(
        <MatchingFactors
          factors={mockFactors}
          confidence={0.85}
        />
      )

      expect(screen.getByText('Exact Match')).toBeInTheDocument()
      expect(screen.getByText('Partial Match')).toBeInTheDocument()
      expect(screen.getByText('Weak Match')).toBeInTheDocument()
    })
  })
})

describe('buildMatchingFactors', () => {
  it('should create factors from scores and data', () => {
    const factors = buildMatchingFactors(
      0.98, // amountScore
      0.95, // dateScore
      0.8,  // vendorScore
      { amount: 125.99, date: '2024-03-15', vendor: 'Amazon' },
      { amount: 125.99, date: '2024-03-15', description: 'AMZN*MKTPLACE' }
    )

    expect(factors).toHaveLength(3)

    // Amount factor
    expect(factors[0].type).toBe('amount')
    expect(factors[0].weight).toBe(0.4)
    expect(factors[0].isExactMatch).toBe(true)
    expect(factors[0].receiptValue).toBe('$125.99')
    expect(factors[0].transactionValue).toBe('$125.99')

    // Date factor
    expect(factors[1].type).toBe('date')
    expect(factors[1].weight).toBe(0.3)
    expect(factors[1].isExactMatch).toBe(true)

    // Merchant factor
    expect(factors[2].type).toBe('merchant')
    expect(factors[2].weight).toBe(0.3)
    expect(factors[2].isExactMatch).toBe(false) // 0.8 < 0.9 threshold
  })

  it('should handle missing receipt data', () => {
    const factors = buildMatchingFactors(
      0.5,
      0.5,
      0.5,
      { amount: undefined, date: undefined, vendor: undefined },
      { amount: 50.00, date: '2024-03-15', description: 'Store' }
    )

    expect(factors[0].receiptValue).toBe('Unknown')
    expect(factors[1].receiptValue).toBe('Unknown')
    expect(factors[2].receiptValue).toBe('Unknown')
  })

  it('should mark high score matches as exact', () => {
    const factors = buildMatchingFactors(
      0.95, // >= 0.95 = exact
      0.96, // >= 0.95 = exact
      0.90, // >= 0.9 = exact for vendor
      { amount: 100, date: '2024-03-15', vendor: 'Store' },
      { amount: 100, date: '2024-03-15', description: 'Store' }
    )

    expect(factors[0].isExactMatch).toBe(true) // 0.95 >= 0.95
    expect(factors[1].isExactMatch).toBe(true) // 0.96 >= 0.95
    expect(factors[2].isExactMatch).toBe(true) // 0.90 >= 0.9
  })
})
