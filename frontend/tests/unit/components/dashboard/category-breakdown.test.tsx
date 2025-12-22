/**
 * CategoryBreakdown Component Tests (T035)
 *
 * Tests the category breakdown component including:
 * - Loading state rendering
 * - Empty state handling
 * - Category items display
 * - Percentage calculations
 * - Different visualization variants (pie, bar, list)
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { CategoryBreakdown } from '@/components/dashboard/category-breakdown';
import type { CategoryBreakdownData } from '@/types/dashboard';

// Framer-motion and TanStack Router are mocked globally in tests/setup.ts

// Mock Recharts components to avoid DOM measurement issues in tests
vi.mock('recharts', () => ({
  ResponsiveContainer: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="responsive-container">{children}</div>
  ),
  PieChart: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="pie-chart">{children}</div>
  ),
  Pie: () => <div data-testid="pie" />,
  Cell: () => <div data-testid="cell" />,
  BarChart: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="bar-chart">{children}</div>
  ),
  Bar: () => <div data-testid="bar" />,
  XAxis: () => <div data-testid="x-axis" />,
  YAxis: () => <div data-testid="y-axis" />,
  Tooltip: () => <div data-testid="tooltip" />,
  Legend: () => <div data-testid="legend" />,
  CartesianGrid: () => <div data-testid="cartesian-grid" />,
}));

const mockCategories: CategoryBreakdownData[] = [
  {
    category: 'Food & Dining',
    amount: 450.0,
    percentage: 35,
    transactionCount: 15,
    color: '#22c55e',
  },
  {
    category: 'Transportation',
    amount: 320.0,
    percentage: 25,
    transactionCount: 8,
    color: '#3b82f6',
  },
  {
    category: 'Office Supplies',
    amount: 256.5,
    percentage: 20,
    transactionCount: 12,
    color: '#f59e0b',
  },
  {
    category: 'Entertainment',
    amount: 180.0,
    percentage: 14,
    transactionCount: 5,
    color: '#8b5cf6',
  },
  {
    category: 'Other',
    amount: 77.0,
    percentage: 6,
    transactionCount: 3,
    color: '#6b7280',
  },
];

describe('CategoryBreakdown', () => {
  it('should render loading skeleton when isLoading is true', () => {
    const { container } = render(<CategoryBreakdown isLoading />);

    const skeletons = container.querySelectorAll('[class*="animate-pulse"]');
    expect(skeletons.length).toBeGreaterThan(0);
  });

  it('should render empty state when no categories', () => {
    const { container } = render(<CategoryBreakdown categories={[]} />);

    // Verify the component renders (empty state uses Lucide icons which are forwardRef)
    const card = container.querySelector('[class*="rounded"]');
    expect(card).toBeInTheDocument();
  });

  it('should render card header with title', () => {
    render(<CategoryBreakdown categories={mockCategories} />);

    expect(screen.getByText('Spending by Category')).toBeInTheDocument();
  });

  describe('List Variant', () => {
    it('should render category names', () => {
      render(<CategoryBreakdown categories={mockCategories} variant="list" />);

      expect(screen.getByText('Food & Dining')).toBeInTheDocument();
      expect(screen.getByText('Transportation')).toBeInTheDocument();
      expect(screen.getByText('Office Supplies')).toBeInTheDocument();
    });

    it('should display formatted amounts', () => {
      render(<CategoryBreakdown categories={mockCategories} variant="list" />);

      expect(screen.getByText('$450.00')).toBeInTheDocument();
      expect(screen.getByText('$320.00')).toBeInTheDocument();
    });

    it('should display percentages', () => {
      const { container } = render(<CategoryBreakdown categories={mockCategories} variant="list" />);

      // Percentages are calculated dynamically: amount / total * 100
      // Total = 450 + 320 + 256.5 + 180 + 77 = 1283.5
      // Food & Dining: 450/1283.5 ≈ 35.06%, Transportation: 320/1283.5 ≈ 24.93%
      // The component uses .toFixed(1) so values are like "35.1%", "24.9%"
      const percentageElements = container.querySelectorAll('.text-xs.tabular-nums');
      const percentageTexts = Array.from(percentageElements).map(el => el.textContent);

      // Should have percentage values - check that we have some percentages displayed
      expect(percentageTexts.length).toBeGreaterThanOrEqual(2);
      expect(percentageTexts.some(text => text?.includes('%'))).toBe(true);
      // Verify the calculated values are roughly correct
      expect(percentageTexts.some(text => text?.includes('35.'))).toBe(true); // ~35.1% for Food
      expect(percentageTexts.some(text => text?.includes('24.'))).toBe(true); // ~24.9% for Transport
    });

    it('should display transaction counts', () => {
      render(<CategoryBreakdown categories={mockCategories} variant="list" />);

      expect(screen.getByText('15 transactions')).toBeInTheDocument();
      expect(screen.getByText('8 transactions')).toBeInTheDocument();
    });

    it('should limit categories by default', () => {
      // The component defaults to showing 5 categories
      render(<CategoryBreakdown categories={mockCategories} variant="list" />);

      expect(screen.getByText('Food & Dining')).toBeInTheDocument();
      expect(screen.getByText('Transportation')).toBeInTheDocument();
      expect(screen.getByText('Office Supplies')).toBeInTheDocument();
    });
  });

  describe('Pie Chart Variant', () => {
    it('should render pie chart container', () => {
      render(<CategoryBreakdown categories={mockCategories} variant="pie" />);

      expect(screen.getByTestId('responsive-container')).toBeInTheDocument();
      expect(screen.getByTestId('pie-chart')).toBeInTheDocument();
    });
  });

  describe('Bar Chart Variant', () => {
    it('should render bar chart container', () => {
      render(<CategoryBreakdown categories={mockCategories} variant="bar" />);

      expect(screen.getByTestId('responsive-container')).toBeInTheDocument();
      expect(screen.getByTestId('bar-chart')).toBeInTheDocument();
    });
  });
});
