'use client'

/**
 * SpendingTrendChart Component Tests (T085)
 *
 * Tests for the spending trend visualization component.
 */

import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen } from '@testing-library/react'
import { SpendingTrendChart } from '@/components/analytics/spending-trend-chart'

// Mock framer-motion
vi.mock('framer-motion', () => ({
  motion: {
    div: ({ children, ...props }: React.PropsWithChildren<Record<string, unknown>>) => (
      <div {...props}>{children}</div>
    ),
  },
  AnimatePresence: ({ children }: React.PropsWithChildren) => <>{children}</>,
}))

// Mock Recharts - renders static divs for testing
vi.mock('recharts', () => ({
  ResponsiveContainer: ({ children }: React.PropsWithChildren) => (
    <div data-testid="responsive-container">{children}</div>
  ),
  AreaChart: ({ children }: React.PropsWithChildren) => (
    <div data-testid="area-chart">{children}</div>
  ),
  LineChart: ({ children }: React.PropsWithChildren) => (
    <div data-testid="line-chart">{children}</div>
  ),
  BarChart: ({ children }: React.PropsWithChildren) => (
    <div data-testid="bar-chart">{children}</div>
  ),
  Area: () => <div data-testid="area" />,
  Line: () => <div data-testid="line" />,
  Bar: () => <div data-testid="bar" />,
  XAxis: () => <div data-testid="x-axis" />,
  YAxis: () => <div data-testid="y-axis" />,
  CartesianGrid: () => <div data-testid="cartesian-grid" />,
  Tooltip: () => <div data-testid="tooltip" />,
  Legend: () => <div data-testid="legend" />,
}))

// Sample test data
const mockTrendData = [
  { period: '2024-03-01', amount: 1500, transactionCount: 12 },
  { period: '2024-03-02', amount: 800, transactionCount: 8 },
  { period: '2024-03-03', amount: 1200, transactionCount: 15 },
  { period: '2024-03-04', amount: 950, transactionCount: 10 },
  { period: '2024-03-05', amount: 2100, transactionCount: 18 },
]

describe('SpendingTrendChart', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  describe('Loading State', () => {
    it('renders skeleton when loading', () => {
      const { container } = render(<SpendingTrendChart isLoading={true} />)

      // Should show loading skeleton elements (animate-pulse class indicates loading)
      const skeletonElements = container.querySelectorAll('.animate-pulse')
      expect(skeletonElements.length).toBeGreaterThan(0)
    })
  })

  describe('Empty State', () => {
    it('renders empty state message when no data', () => {
      render(<SpendingTrendChart data={[]} />)

      expect(screen.getByText('No spending data available for this period')).toBeDefined()
    })

    it('renders empty state when data is undefined', () => {
      render(<SpendingTrendChart />)

      expect(screen.getByText('No spending data available for this period')).toBeDefined()
    })
  })

  describe('Chart Display', () => {
    it('renders title correctly', () => {
      render(<SpendingTrendChart data={mockTrendData} title="Custom Title" />)

      expect(screen.getByText('Custom Title')).toBeDefined()
    })

    it('shows period badge with data count', () => {
      render(<SpendingTrendChart data={mockTrendData} />)

      expect(screen.getByText('5 periods')).toBeDefined()
    })

    it('renders area chart by default', () => {
      render(<SpendingTrendChart data={mockTrendData} />)

      expect(screen.getByTestId('area-chart')).toBeDefined()
    })

    it('renders line chart when specified', () => {
      render(<SpendingTrendChart data={mockTrendData} chartType="line" />)

      expect(screen.getByTestId('line-chart')).toBeDefined()
    })

    it('renders bar chart when specified', () => {
      render(<SpendingTrendChart data={mockTrendData} chartType="bar" />)

      expect(screen.getByTestId('bar-chart')).toBeDefined()
    })
  })

  describe('Trend Summary', () => {
    it('displays total spending', () => {
      render(<SpendingTrendChart data={mockTrendData} />)

      // Total: 1500 + 800 + 1200 + 950 + 2100 = 6550
      expect(screen.getByText('$6,550')).toBeDefined()
    })

    it('displays average spending', () => {
      render(<SpendingTrendChart data={mockTrendData} />)

      // Average: 6550 / 5 = 1310
      expect(screen.getByText('$1,310')).toBeDefined()
    })

    it('displays peak spending', () => {
      render(<SpendingTrendChart data={mockTrendData} />)

      // Peak: 2100
      expect(screen.getByText('$2,100')).toBeDefined()
    })
  })

  describe('Chart Configuration', () => {
    it('applies custom className', () => {
      const { container } = render(
        <SpendingTrendChart data={mockTrendData} className="custom-class" />
      )

      expect(container.querySelector('.custom-class')).toBeDefined()
    })

    it('shows legend when enabled', () => {
      render(<SpendingTrendChart data={mockTrendData} showLegend={true} />)

      expect(screen.getByTestId('legend')).toBeDefined()
    })
  })
})
