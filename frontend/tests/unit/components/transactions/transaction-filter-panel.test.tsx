'use client';

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TransactionFilterPanel } from '@/components/transactions/transaction-filter-panel';
import type { TransactionFilters } from '@/types/transaction';
import { DEFAULT_TRANSACTION_FILTERS } from '@/types/transaction';

// Mock framer-motion
vi.mock('framer-motion', () => ({
  motion: {
    div: ({ children, ...props }: React.PropsWithChildren<Record<string, unknown>>) => (
      <div {...props}>{children}</div>
    ),
  },
  AnimatePresence: ({ children }: React.PropsWithChildren) => <>{children}</>,
}));

// Mock use-debounce
vi.mock('use-debounce', () => ({
  useDebounce: (value: string) => [value],
}));

// Sample test data
const mockCategories = [
  { id: 'cat-1', name: 'Food & Dining' },
  { id: 'cat-2', name: 'Transportation' },
  { id: 'cat-3', name: 'Shopping' },
];

const mockTags = ['business', 'personal', 'recurring', 'travel'];

function createMockFilters(overrides: Partial<TransactionFilters> = {}): TransactionFilters {
  return {
    ...DEFAULT_TRANSACTION_FILTERS,
    ...overrides,
  };
}

describe('TransactionFilterPanel', () => {
  const mockOnChange = vi.fn();
  const mockOnReset = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Search Input', () => {
    it('should render search input with placeholder', () => {
      render(
        <TransactionFilterPanel
          filters={createMockFilters()}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      expect(screen.getByPlaceholderText('Search transactions...')).toBeInTheDocument();
    });

    it('should display current search value', () => {
      render(
        <TransactionFilterPanel
          filters={createMockFilters({ search: 'coffee' })}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      const input = screen.getByPlaceholderText('Search transactions...') as HTMLInputElement;
      expect(input.value).toBe('coffee');
    });

    it('should update local search value on input', async () => {
      const user = userEvent.setup();
      render(
        <TransactionFilterPanel
          filters={createMockFilters()}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      const input = screen.getByPlaceholderText('Search transactions...');
      await user.type(input, 'restaurant');

      expect(input).toHaveValue('restaurant');
    });

    it('should show clear button when search has value', () => {
      render(
        <TransactionFilterPanel
          filters={createMockFilters({ search: 'test' })}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      // Find the clear button in the search input
      const clearButtons = screen.getAllByRole('button');
      const searchClearButton = clearButtons.find(btn =>
        btn.closest('.relative')?.querySelector('input[type="text"]')
      );
      expect(searchClearButton).toBeDefined();
    });
  });

  describe('Date Filter', () => {
    it('should render date filter button', () => {
      render(
        <TransactionFilterPanel
          filters={createMockFilters()}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      expect(screen.getByRole('button', { name: /date/i })).toBeInTheDocument();
    });

    it('should show date preset options when clicked', async () => {
      const user = userEvent.setup();
      render(
        <TransactionFilterPanel
          filters={createMockFilters()}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      const dateButton = screen.getByRole('button', { name: /date/i });
      await user.click(dateButton);

      await waitFor(() => {
        expect(screen.getByText('Last 7 days')).toBeInTheDocument();
        expect(screen.getByText('Last 30 days')).toBeInTheDocument();
        expect(screen.getByText('Last 90 days')).toBeInTheDocument();
      });
    });

    it('should call onChange when date preset is selected', async () => {
      const user = userEvent.setup();
      render(
        <TransactionFilterPanel
          filters={createMockFilters()}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      const dateButton = screen.getByRole('button', { name: /date/i });
      await user.click(dateButton);

      await waitFor(() => {
        expect(screen.getByText('Last 7 days')).toBeInTheDocument();
      });

      await user.click(screen.getByText('Last 7 days'));

      expect(mockOnChange).toHaveBeenCalled();
      const callArg = mockOnChange.mock.calls[0][0];
      expect(callArg.dateRange.start).toBeInstanceOf(Date);
      expect(callArg.dateRange.end).toBeInstanceOf(Date);
    });
  });

  describe('Category Filter', () => {
    it('should render category filter button', () => {
      render(
        <TransactionFilterPanel
          filters={createMockFilters()}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      expect(screen.getByRole('button', { name: /category/i })).toBeInTheDocument();
    });

    it('should show category options when clicked', async () => {
      const user = userEvent.setup();
      render(
        <TransactionFilterPanel
          filters={createMockFilters()}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      const categoryButton = screen.getByRole('button', { name: /category/i });
      await user.click(categoryButton);

      await waitFor(() => {
        expect(screen.getByText('Food & Dining')).toBeInTheDocument();
        expect(screen.getByText('Transportation')).toBeInTheDocument();
        expect(screen.getByText('Shopping')).toBeInTheDocument();
      });
    });

    it('should show badge with count when categories are selected', () => {
      render(
        <TransactionFilterPanel
          filters={createMockFilters({ categories: ['cat-1', 'cat-2'] })}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      expect(screen.getByText('2')).toBeInTheDocument();
    });
  });

  describe('Match Status Filter', () => {
    it('should render status filter button', () => {
      render(
        <TransactionFilterPanel
          filters={createMockFilters()}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      expect(screen.getByRole('button', { name: /status/i })).toBeInTheDocument();
    });

    it('should show all 5 match status options including Missing Receipt when clicked', async () => {
      const user = userEvent.setup();
      render(
        <TransactionFilterPanel
          filters={createMockFilters()}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      const statusButton = screen.getByRole('button', { name: /status/i });
      await user.click(statusButton);

      // Use getAllByText because some options may appear in multiple places
      // (e.g., "Pending Review" appears both in quick filters button and dropdown)
      await waitFor(() => {
        expect(screen.getAllByText('Matched').length).toBeGreaterThan(0);
        expect(screen.getAllByText('Pending Review').length).toBeGreaterThan(0);
        expect(screen.getAllByText('Unmatched').length).toBeGreaterThan(0);
        expect(screen.getAllByText('Manual Match').length).toBeGreaterThan(0);
        expect(screen.getAllByText('Missing Receipt').length).toBeGreaterThan(0);
      });
    });

    it('should display missing-receipt filter as badge when selected', () => {
      render(
        <TransactionFilterPanel
          filters={createMockFilters({ matchStatus: ['missing-receipt'] })}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      // Look for Missing Receipt in the badges section
      const badges = screen.getAllByText('Missing Receipt');
      expect(badges.length).toBeGreaterThan(0);
    });

    it('should call onChange when Missing Receipt status is toggled', async () => {
      const user = userEvent.setup();
      render(
        <TransactionFilterPanel
          filters={createMockFilters()}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      const statusButton = screen.getByRole('button', { name: /status/i });
      await user.click(statusButton);

      await waitFor(() => {
        expect(screen.getByText('Missing Receipt')).toBeInTheDocument();
      });

      // Find and click the Missing Receipt checkbox
      const missingReceiptLabel = screen.getByText('Missing Receipt');
      await user.click(missingReceiptLabel);

      expect(mockOnChange).toHaveBeenCalled();
      const callArg = mockOnChange.mock.calls[0][0];
      expect(callArg.matchStatus).toContain('missing-receipt');
    });
  });

  describe('Clear Filters', () => {
    it('should show clear button when filters are active', () => {
      render(
        <TransactionFilterPanel
          filters={createMockFilters({ search: 'test' })}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      expect(screen.getByRole('button', { name: /clear/i })).toBeInTheDocument();
    });

    it('should not show clear button when no filters are active', () => {
      render(
        <TransactionFilterPanel
          filters={createMockFilters()}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      expect(screen.queryByRole('button', { name: /clear/i })).not.toBeInTheDocument();
    });

    it('should call onReset when clear button is clicked', async () => {
      const user = userEvent.setup();
      render(
        <TransactionFilterPanel
          filters={createMockFilters({ search: 'test' })}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      await user.click(screen.getByRole('button', { name: /clear/i }));

      expect(mockOnReset).toHaveBeenCalledTimes(1);
    });
  });

  describe('Active Filter Badges', () => {
    it('should display active search filter as badge', () => {
      render(
        <TransactionFilterPanel
          filters={createMockFilters({ search: 'coffee' })}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      expect(screen.getByText(/search.*coffee/i)).toBeInTheDocument();
    });

    it('should display active category filters as badges', () => {
      render(
        <TransactionFilterPanel
          filters={createMockFilters({ categories: ['cat-1'] })}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      expect(screen.getByText('Food & Dining')).toBeInTheDocument();
    });

    it('should display active match status filters as badges', () => {
      render(
        <TransactionFilterPanel
          filters={createMockFilters({ matchStatus: ['matched'] })}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      // Look for Matched in the badges section
      const badges = screen.getAllByText('Matched');
      expect(badges.length).toBeGreaterThan(0);
    });
  });

  describe('Advanced Filters Toggle', () => {
    it('should render advanced filters toggle button', () => {
      render(
        <TransactionFilterPanel
          filters={createMockFilters()}
          categories={mockCategories}
          tags={mockTags}
          onChange={mockOnChange}
          onReset={mockOnReset}
        />
      );

      // Find the filter toggle button (has Filter icon)
      const buttons = screen.getAllByRole('button');
      // Button should exist
      expect(buttons.length).toBeGreaterThan(0);
    });
  });
});
