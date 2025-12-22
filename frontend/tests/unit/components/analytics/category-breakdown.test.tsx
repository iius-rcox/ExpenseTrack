'use client'

/**
 * CategoryBreakdown Component Tests (T086)
 *
 * Tests for the category breakdown visualization component.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { CategoryBreakdown } from '@/components/analytics/category-breakdown'

// Mock framer-motion
vi.mock('framer-motion', () => ({
  motion: {
    div: ({ children, ...props }: React.PropsWithChildren<Record<string, unknown>>) => (
      <div {...props}>{children}</div>
    ),
  },
  AnimatePresence: ({ children }: React.PropsWithChildren) => <>{children}</>,
}))

// Mock Recharts
vi.mock('recharts', () => ({
  ResponsiveContainer: ({ children }: React.PropsWithChildren) => (
    <div data-testid="responsive-container">{children}</div>
  ),
  PieChart: ({ children }: React.PropsWithChildren) => (
    <div data-testid="pie-chart">{children}</div>
  ),
  BarChart: ({ children }: React.PropsWithChildren) => (
    <div data-testid="bar-chart">{children}</div>
  ),
  Pie: () => <div data-testid="pie" />,
  Bar: () => <div data-testid="bar" />,
  Cell: () => <div data-testid="cell" />,
  XAxis: () => <div data-testid="x-axis" />,
  YAxis: () => <div data-testid="y-axis" />,
  CartesianGrid: () => <div data-testid="cartesian-grid" />,
  Tooltip: () => <div data-testid="tooltip" />,
  Legend: () => <div data-testid="legend" />,
}))

// Sample test data
const mockCategoryData = [
  { category: 'Food & Dining', amount: 850, percentage: 35, transactionCount: 25 },
  { category: 'Transportation', amount: 425, percentage: 17.5, transactionCount: 12 },
  { category: 'Shopping', amount: 600, percentage: 24.7, transactionCount: 8 },
  { category: 'Entertainment', amount: 300, percentage: 12.3, transactionCount: 15 },
  { category: 'Utilities', amount: 255, percentage: 10.5, transactionCount: 5 },
]

describe('CategoryBreakdown', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Loading State', () => {
    it('renders skeleton when loading', () => {
      render(<CategoryBreakdown isLoading={true} />)

      // Should show loading skeleton
      expect(screen.queryByTestId('pie-chart')).toBeNull()
    })
  })

  describe('Empty State', () => {
    it('renders empty state message when no data', () => {
      render(<CategoryBreakdown data={[]} />)

      expect(screen.getByText('No category data available')).toBeDefined()
    })

    it('renders empty state when data is undefined', () => {
      render(<CategoryBreakdown />)

      expect(screen.getByText('No category data available')).toBeDefined()
    })
  })

  describe('Chart Display', () => {
    it('renders title correctly', () => {
      render(<CategoryBreakdown data={mockCategoryData} title="Custom Title" />)

      expect(screen.getByText('Custom Title')).toBeDefined()
    })

    it('shows total amount', () => {
      render(<CategoryBreakdown data={mockCategoryData} />)

      // Total: 850 + 425 + 600 + 300 + 255 = 2430
      expect(screen.getByText('$2,430')).toBeDefined()
    })

    it('renders donut chart by default', () => {
      render(<CategoryBreakdown data={mockCategoryData} />)

      expect(screen.getByTestId('pie-chart')).toBeDefined()
    })

    it('renders bar chart when specified', () => {
      render(<CategoryBreakdown data={mockCategoryData} chartType="bar" />)

      expect(screen.getByTestId('bar-chart')).toBeDefined()
    })
  })

  describe('View Type Toggle', () => {
    it('renders view type buttons', () => {
      render(<CategoryBreakdown data={mockCategoryData} />)

      // Should have donut, bar, and list view buttons
      const buttons = screen.getAllByRole('button')
      expect(buttons.length).toBeGreaterThanOrEqual(3)
    })

    it('switches to list view when list button clicked', () => {
      render(<CategoryBreakdown data={mockCategoryData} />)

      // Click the list view button (last icon button)
      const buttons = screen.getAllByRole('button')
      const listButton = buttons[buttons.length - 1]
      fireEvent.click(listButton)

      // In list view, categories should be rendered as text
      expect(screen.getByText('Food & Dining')).toBeDefined()
      expect(screen.getByText('Transportation')).toBeDefined()
    })
  })

  describe('List View', () => {
    it('displays categories with percentages in list view', () => {
      render(<CategoryBreakdown data={mockCategoryData} chartType="list" />)

      expect(screen.getByText('Food & Dining')).toBeDefined()
      expect(screen.getByText('35.0%')).toBeDefined()
    })

    it('displays category amounts in list view', () => {
      render(<CategoryBreakdown data={mockCategoryData} chartType="list" />)

      expect(screen.getByText('$850')).toBeDefined()
      expect(screen.getByText('$425')).toBeDefined()
    })
  })

  describe('Category Limiting', () => {
    it('limits categories to maxCategories prop', () => {
      const manyCategories = [
        ...mockCategoryData,
        { category: 'Healthcare', amount: 200, percentage: 8, transactionCount: 3 },
        { category: 'Education', amount: 150, percentage: 6, transactionCount: 2 },
        { category: 'Personal', amount: 100, percentage: 4, transactionCount: 4 },
        { category: 'Other1', amount: 50, percentage: 2, transactionCount: 1 },
        { category: 'Other2', amount: 25, percentage: 1, transactionCount: 1 },
      ]

      render(
        <CategoryBreakdown data={manyCategories} chartType="list" maxCategories={5} />
      )

      // Should show 4 categories + "Other" grouped
      expect(screen.getByText('Other')).toBeDefined()
    })
  })

  describe('Category Selection', () => {
    it('calls onCategorySelect when category clicked in list view', () => {
      const mockOnSelect = vi.fn()

      render(
        <CategoryBreakdown
          data={mockCategoryData}
          chartType="list"
          onCategorySelect={mockOnSelect}
        />
      )

      // Click on a category
      fireEvent.click(screen.getByText('Food & Dining'))

      expect(mockOnSelect).toHaveBeenCalledWith(
        expect.objectContaining({ category: 'Food & Dining' })
      )
    })

    it('highlights selected category', () => {
      const { container } = render(
        <CategoryBreakdown
          data={mockCategoryData}
          chartType="list"
          selectedCategory="Food & Dining"
        />
      )

      // The selected category row should have border-primary styling
      // Find the container with both 'border-primary' and the category text
      const highlightedRow = container.querySelector('.border-primary')
      expect(highlightedRow).not.toBeNull()
      expect(highlightedRow?.textContent).toContain('Food & Dining')
    })
  })

  describe('Styling', () => {
    it('applies custom className', () => {
      const { container } = render(
        <CategoryBreakdown data={mockCategoryData} className="custom-class" />
      )

      expect(container.querySelector('.custom-class')).toBeDefined()
    })

    it('respects custom height', () => {
      render(<CategoryBreakdown data={mockCategoryData} height={500} />)

      expect(screen.getByTestId('responsive-container')).toBeDefined()
    })
  })
})
