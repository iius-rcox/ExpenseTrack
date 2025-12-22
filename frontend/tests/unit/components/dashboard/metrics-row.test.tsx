/**
 * MetricsRow Component Tests (T033)
 *
 * Tests the dashboard metrics row component including:
 * - Loading state rendering
 * - Error state rendering
 * - Metrics display with correct values
 * - Trend indicators
 * - Responsive compact variant
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MetricsRow, MetricsSummaryBar } from '@/components/dashboard/metrics-row';
import type { DashboardMetrics } from '@/types/api';

// Framer-motion and TanStack Router are mocked globally in tests/setup.ts

const mockMetrics: DashboardMetrics = {
  pendingReceiptsCount: 5,
  unmatchedTransactionsCount: 3,
  matchedTransactionsCount: 97, // 97 matched + 3 unmatched = 100 total, 97% match rate
  pendingMatchesCount: 8,
  draftReportsCount: 2,
  monthlySpending: {
    currentMonth: 4287.5,
    previousMonth: 3812.0,
    percentChange: 12.5,
  },
};

describe('MetricsRow', () => {
  it('should render loading skeleton when isLoading is true', () => {
    render(<MetricsRow isLoading />);

    // Should render skeleton elements
    const skeletons = document.querySelectorAll('[class*="animate-pulse"]');
    expect(skeletons.length).toBeGreaterThan(0);
  });

  it('should render error state when error is provided', () => {
    render(<MetricsRow error={new Error('Failed to fetch')} />);

    const errorElements = screen.getAllByText('Failed to load');
    expect(errorElements.length).toBe(4); // All 4 metric cards
  });

  it('should return null when no metrics and not loading', () => {
    const { container } = render(<MetricsRow />);
    expect(container.firstChild).toBeNull();
  });

  it('should render all metric cards with correct labels', () => {
    render(<MetricsRow metrics={mockMetrics} />);

    expect(screen.getByText('Monthly Spending')).toBeInTheDocument();
    expect(screen.getByText('Pending Review')).toBeInTheDocument();
    expect(screen.getByText('Match Rate')).toBeInTheDocument();
    expect(screen.getByText('Draft Reports')).toBeInTheDocument();
  });

  it('should display formatted currency for monthly spending', () => {
    render(<MetricsRow metrics={mockMetrics} />);

    // Should format as USD currency (minimumFractionDigits: 0 drops trailing zeros)
    expect(screen.getByText('$4,287.5')).toBeInTheDocument();
  });

  it('should calculate pending review count correctly', () => {
    render(<MetricsRow metrics={mockMetrics} />);

    // pendingReceiptsCount (5) + pendingMatchesCount (8) = 13
    expect(screen.getByText('13')).toBeInTheDocument();
  });

  it('should display draft reports count', () => {
    render(<MetricsRow metrics={mockMetrics} />);

    expect(screen.getByText('2')).toBeInTheDocument();
  });

  it('should highlight card when conditions are met', () => {
    const metricsWithHighPending: DashboardMetrics = {
      ...mockMetrics,
      pendingReceiptsCount: 15,
      pendingMatchesCount: 10,
    };

    const { container } = render(<MetricsRow metrics={metricsWithHighPending} />);

    // Check for highlight ring class
    const highlightedCards = container.querySelectorAll('[class*="ring-"]');
    expect(highlightedCards.length).toBeGreaterThan(0);
  });
});

describe('MetricsSummaryBar', () => {
  it('should render loading state with skeleton', () => {
    const { container } = render(<MetricsSummaryBar isLoading metrics={undefined} />);

    const skeletons = container.querySelectorAll('[class*="animate-pulse"]');
    expect(skeletons.length).toBeGreaterThan(0);
  });

  it('should render compact metrics in summary bar', () => {
    render(<MetricsSummaryBar metrics={mockMetrics} isLoading={false} />);

    expect(screen.getByText('Spending')).toBeInTheDocument();
    // Use getAllByText for 'Pending' since it appears multiple times
    expect(screen.getAllByText('Pending').length).toBeGreaterThan(0);
    expect(screen.getByText('Matched')).toBeInTheDocument();
  });

  it('should display pending count', () => {
    render(<MetricsSummaryBar metrics={mockMetrics} isLoading={false} />);

    // 5 + 8 = 13 pending
    expect(screen.getByText('13')).toBeInTheDocument();
  });
});
