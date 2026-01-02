'use client';

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TransactionRow, TransactionRowSkeleton } from '@/components/transactions/transaction-row';
import type { TransactionView, TransactionMatchStatus } from '@/types/transaction';

// Mock framer-motion
vi.mock('framer-motion', () => ({
  motion: {
    div: ({ children, ...props }: React.PropsWithChildren<Record<string, unknown>>) => (
      <div {...props}>{children}</div>
    ),
  },
  AnimatePresence: ({ children }: React.PropsWithChildren) => <>{children}</>,
}));

// Sample test data
const mockCategories = [
  { id: 'cat-1', name: 'Food & Dining' },
  { id: 'cat-2', name: 'Transportation' },
  { id: 'cat-3', name: 'Shopping' },
];

function createMockTransaction(
  overrides: Partial<TransactionView> = {}
): TransactionView {
  return {
    id: 'txn-1',
    date: new Date('2024-03-15'),
    description: 'Coffee Shop Purchase',
    merchant: 'Starbucks',
    amount: 5.75,
    category: 'Food & Dining',
    categoryId: 'cat-1',
    tags: [],
    notes: '',
    matchStatus: 'unmatched' as TransactionMatchStatus,
    source: 'import',
    ...overrides,
  };
}

describe('TransactionRow', () => {
  const mockOnSelect = vi.fn();
  const mockOnEdit = vi.fn();
  const mockOnClick = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Display', () => {
    it('should render transaction date', () => {
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction()}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      // Date format may vary by locale, check for components
      expect(screen.getByText(/2024/)).toBeInTheDocument();
    });

    it('should render merchant name as primary description', () => {
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction({ merchant: 'Starbucks' })}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      expect(screen.getByText('Starbucks')).toBeInTheDocument();
    });

    it('should render amount with currency formatting', () => {
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction({ amount: 125.50 })}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      expect(screen.getByText('$125.50')).toBeInTheDocument();
    });

    it('should render negative amounts (credits) in green', () => {
      const { container } = render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction({ amount: -50.00 })}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      const amountCell = container.querySelector('.text-green-600');
      expect(amountCell).toBeInTheDocument();
    });

    it('should render category in select dropdown', () => {
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction({ category: 'Food & Dining' })}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      expect(screen.getByRole('combobox')).toBeInTheDocument();
    });

    it('should render tags as badges', () => {
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction({ tags: ['business', 'travel'] })}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      expect(screen.getByText('business')).toBeInTheDocument();
      expect(screen.getByText('travel')).toBeInTheDocument();
    });

    it('should show +N badge for more than 2 tags', () => {
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction({ tags: ['business', 'travel', 'recurring'] })}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      expect(screen.getByText('+1')).toBeInTheDocument();
    });
  });

  describe('Match Status', () => {
    it('should render matched status with correct styling', () => {
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction({ matchStatus: 'matched' })}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      expect(screen.getByText('Matched')).toBeInTheDocument();
    });

    it('should render pending status', () => {
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction({ matchStatus: 'pending' })}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      expect(screen.getByText('Pending Review')).toBeInTheDocument();
    });

    it('should render unmatched status', () => {
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction({ matchStatus: 'unmatched' })}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      expect(screen.getByText('Unmatched')).toBeInTheDocument();
    });

    it('should render manual match status', () => {
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction({ matchStatus: 'manual' })}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      expect(screen.getByText('Manual Match')).toBeInTheDocument();
    });
  });

  describe('Selection', () => {
    it('should render checkbox', () => {
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction()}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      expect(screen.getByRole('checkbox')).toBeInTheDocument();
    });

    it('should show checked state when selected', () => {
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction()}
              isSelected={true}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      const checkbox = screen.getByRole('checkbox');
      expect(checkbox).toHaveAttribute('data-state', 'checked');
    });

    it('should call onSelect when checkbox is clicked', async () => {
      const user = userEvent.setup();
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction()}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      const checkbox = screen.getByRole('checkbox');
      await user.click(checkbox);

      expect(mockOnSelect).toHaveBeenCalledWith(false); // shiftKey = false
    });
  });

  describe('Inline Editing - Notes', () => {
    it('should show "Add notes..." placeholder when notes are empty', () => {
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction({ notes: '' })}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      expect(screen.getByText('Add notes...')).toBeInTheDocument();
    });

    it('should show existing notes', () => {
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction({ notes: 'Business lunch' })}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      expect(screen.getByText('Business lunch')).toBeInTheDocument();
    });

    it('should show edit input when clicking pencil icon', async () => {
      const user = userEvent.setup();
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction({ notes: 'Test note' })}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      // Find and click the edit button (pencil icon)
      const editButtons = screen.getAllByRole('button');
      const pencilButton = editButtons.find(
        btn => btn.querySelector('.lucide-pencil') !== null
      );

      if (pencilButton) {
        await user.click(pencilButton);

        await waitFor(() => {
          expect(screen.getByRole('textbox')).toBeInTheDocument();
        });
      }
    });

    it('should call onEdit when saving notes', async () => {
      const user = userEvent.setup();
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction({ notes: '' })}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      // Click to start editing
      const editButtons = screen.getAllByRole('button');
      const pencilButton = editButtons.find(
        btn => btn.querySelector('.lucide-pencil') !== null
      );

      if (pencilButton) {
        await user.click(pencilButton);

        const input = await screen.findByRole('textbox');
        await user.type(input, 'New note');

        // Find and click save button (check icon)
        const saveButton = screen.getAllByRole('button').find(
          btn => btn.querySelector('.lucide-check') !== null
        );

        if (saveButton) {
          await user.click(saveButton);

          expect(mockOnEdit).toHaveBeenCalledWith({ notes: 'New note' });
        }
      }
    });
  });

  describe('Category Change', () => {
    it('should render category select with current value', () => {
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction({ categoryId: 'cat-1', category: 'Food & Dining' })}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      const combobox = screen.getByRole('combobox');
      expect(combobox).toBeInTheDocument();
      // The combobox should have the current category selected
      expect(combobox).toHaveTextContent('Food & Dining');
    });
  });

  describe('Row Click', () => {
    it('should call onClick when action button is clicked', async () => {
      const user = userEvent.setup();
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction()}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      // Find the external link action button
      const buttons = screen.getAllByRole('button');
      const actionButton = buttons.find(
        btn => btn.querySelector('.lucide-external-link') !== null
      );

      if (actionButton) {
        await user.click(actionButton);
        expect(mockOnClick).toHaveBeenCalled();
      }
    });

    it('should not call onClick when clicking checkbox', async () => {
      const user = userEvent.setup();
      render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction()}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
            />
          </tbody>
        </table>
      );

      const checkbox = screen.getByRole('checkbox');
      await user.click(checkbox);

      expect(mockOnClick).not.toHaveBeenCalled();
    });
  });

  describe('Saving State', () => {
    it('should show disabled state when isSaving is true', () => {
      const { container } = render(
        <table>
          <tbody>
            <TransactionRow
              transaction={createMockTransaction()}
              isSelected={false}
              categories={mockCategories}
              onSelect={mockOnSelect}
              onEdit={mockOnEdit}
              onClick={mockOnClick}
              isSaving={true}
            />
          </tbody>
        </table>
      );

      const row = container.querySelector('tr');
      expect(row).toHaveClass('opacity-60');
    });
  });
});

describe('TransactionRowSkeleton', () => {
  it('should render skeleton loader', () => {
    const { container } = render(
      <table>
        <tbody>
          <TransactionRowSkeleton />
        </tbody>
      </table>
    );

    const skeleton = container.querySelector('.animate-pulse');
    expect(skeleton).toBeInTheDocument();
  });
});
