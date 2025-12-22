/**
 * ExpenseStream Component Tests (T034)
 *
 * Tests the expense activity stream component including:
 * - Loading state rendering
 * - Empty state handling
 * - Activity items display
 * - Confidence indicators
 * - Status badges
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ExpenseStream, ExpenseStreamCompact } from '@/components/dashboard/expense-stream';
import type { ExpenseStreamItem } from '@/types/dashboard';

// Framer-motion and TanStack Router are mocked globally in tests/setup.ts

const mockItems: ExpenseStreamItem[] = [
  {
    id: 'evt-1',
    type: 'receipt',
    title: 'Starbucks Receipt Processed',
    amount: 12.5,
    timestamp: new Date().toISOString(),
    status: 'complete',
    confidence: 0.95,
  },
  {
    id: 'evt-2',
    type: 'match',
    title: 'Amazon Purchase Matched',
    amount: 47.99,
    timestamp: new Date().toISOString(),
    status: 'complete',
    confidence: 0.87,
  },
  {
    id: 'evt-3',
    type: 'transaction',
    title: 'New Transaction Imported',
    amount: 125.0,
    timestamp: new Date().toISOString(),
    status: 'pending',
  },
  {
    id: 'evt-4',
    type: 'category',
    title: 'AI Categorized Expense',
    timestamp: new Date().toISOString(),
    status: 'needs_review',
    confidence: 0.72,
  },
];

describe('ExpenseStream', () => {
  it('should render loading skeleton when isLoading is true', () => {
    const { container } = render(<ExpenseStream isLoading />);

    const skeletons = container.querySelectorAll('[class*="animate-pulse"]');
    expect(skeletons.length).toBeGreaterThan(0);
  });

  it('should render empty state when no items', () => {
    render(<ExpenseStream items={[]} />);

    expect(screen.getByText('No recent activity')).toBeInTheDocument();
    expect(screen.getByText('New expenses will appear here')).toBeInTheDocument();
  });

  it('should render activity items with titles', () => {
    render(<ExpenseStream items={mockItems} />);

    expect(screen.getByText('Starbucks Receipt Processed')).toBeInTheDocument();
    expect(screen.getByText('Amazon Purchase Matched')).toBeInTheDocument();
    expect(screen.getByText('New Transaction Imported')).toBeInTheDocument();
  });

  it('should display formatted amounts', () => {
    render(<ExpenseStream items={mockItems} />);

    expect(screen.getByText('$12.50')).toBeInTheDocument();
    expect(screen.getByText('$47.99')).toBeInTheDocument();
    expect(screen.getByText('$125.00')).toBeInTheDocument();
  });

  it('should render confidence indicators for items with confidence', () => {
    render(<ExpenseStream items={mockItems} />);

    // Check for confidence percentage displays
    expect(screen.getByText('95%')).toBeInTheDocument();
    expect(screen.getByText('87%')).toBeInTheDocument();
  });

  it('should render status badges for non-complete items', () => {
    render(<ExpenseStream items={mockItems} />);

    expect(screen.getByText('pending')).toBeInTheDocument();
    expect(screen.getByText('needs review')).toBeInTheDocument();
  });

  it('should respect maxItems prop', () => {
    render(<ExpenseStream items={mockItems} maxItems={2} />);

    expect(screen.getByText('Starbucks Receipt Processed')).toBeInTheDocument();
    expect(screen.getByText('Amazon Purchase Matched')).toBeInTheDocument();
    expect(screen.queryByText('New Transaction Imported')).not.toBeInTheDocument();
  });

  it('should show View All link when there are more items', () => {
    render(<ExpenseStream items={mockItems} maxItems={2} showViewAll />);

    expect(screen.getByText('View all')).toBeInTheDocument();
  });

  it('should call onItemClick when item is clicked', () => {
    const handleClick = vi.fn();
    render(<ExpenseStream items={mockItems} onItemClick={handleClick} />);

    fireEvent.click(screen.getByText('Starbucks Receipt Processed'));
    expect(handleClick).toHaveBeenCalledWith(mockItems[0]);
  });

  it('should render card header with title', () => {
    render(<ExpenseStream items={mockItems} />);

    expect(screen.getByText('Recent Activity')).toBeInTheDocument();
  });
});

describe('ExpenseStreamCompact', () => {
  it('should render loading state', () => {
    const { container } = render(<ExpenseStreamCompact isLoading items={undefined} />);

    const skeletons = container.querySelectorAll('[class*="animate-pulse"]');
    expect(skeletons.length).toBeGreaterThan(0);
  });

  it('should render empty state when no items', () => {
    render(<ExpenseStreamCompact items={[]} />);

    expect(screen.getByText('No recent activity')).toBeInTheDocument();
  });

  it('should render compact item titles', () => {
    render(<ExpenseStreamCompact items={mockItems} maxItems={3} />);

    expect(screen.getByText('Starbucks Receipt Processed')).toBeInTheDocument();
    expect(screen.getByText('Amazon Purchase Matched')).toBeInTheDocument();
    expect(screen.getByText('New Transaction Imported')).toBeInTheDocument();
  });

  it('should display amounts in compact format', () => {
    render(<ExpenseStreamCompact items={mockItems} />);

    expect(screen.getByText('$12.50')).toBeInTheDocument();
  });

  it('should respect maxItems in compact mode', () => {
    render(<ExpenseStreamCompact items={mockItems} maxItems={2} />);

    expect(screen.getByText('Starbucks Receipt Processed')).toBeInTheDocument();
    expect(screen.queryByText('New Transaction Imported')).not.toBeInTheDocument();
  });
});
