'use client';

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ExtractedField, ExtractedFieldSkeleton } from '@/components/receipts/extracted-field';
import type { ExtractedField as ExtractedFieldType } from '@/types/receipt';

// Mock framer-motion
vi.mock('framer-motion', () => ({
  motion: {
    div: ({ children, ...props }: React.PropsWithChildren<Record<string, unknown>>) => (
      <div {...props}>{children}</div>
    ),
  },
  AnimatePresence: ({ children }: React.PropsWithChildren) => <>{children}</>,
}));

function createMockField(overrides: Partial<ExtractedFieldType> = {}): ExtractedFieldType {
  return {
    key: 'merchant',
    value: 'Coffee Shop',
    confidence: 0.95,
    isEdited: false,
    ...overrides,
  };
}

describe('ExtractedField', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Display Mode', () => {
    it('should render field label', () => {
      const field = createMockField({ key: 'merchant' });
      render(<ExtractedField field={field} />);

      expect(screen.getByText('Merchant')).toBeInTheDocument();
    });

    it('should render amount with currency formatting', () => {
      const field = createMockField({ key: 'amount', value: 125.50 });
      render(<ExtractedField field={field} />);

      expect(screen.getByText('$125.50')).toBeInTheDocument();
    });

    it('should render date with locale formatting', () => {
      const field = createMockField({ key: 'date', value: '2024-03-15' });
      render(<ExtractedField field={field} />);

      // Date format varies by locale, so check for the year
      expect(screen.getByText(/2024/)).toBeInTheDocument();
    });

    it('should render text fields as-is', () => {
      const field = createMockField({ key: 'merchant', value: 'Coffee Shop' });
      render(<ExtractedField field={field} />);

      expect(screen.getByText('Coffee Shop')).toBeInTheDocument();
    });

    it('should render dash for null values', () => {
      const field = createMockField({ key: 'tip', value: null });
      render(<ExtractedField field={field} />);

      expect(screen.getByText('—')).toBeInTheDocument();
    });

    it('should show confidence indicator by default', () => {
      const field = createMockField({ confidence: 0.95 });
      render(<ExtractedField field={field} showConfidence />);

      // ConfidenceIndicator should be rendered
      expect(screen.getByText('95%')).toBeInTheDocument();
    });

    it('should hide confidence indicator when showConfidence is false', () => {
      const field = createMockField({ confidence: 0.95 });
      render(<ExtractedField field={field} showConfidence={false} />);

      expect(screen.queryByText('95%')).not.toBeInTheDocument();
    });

    it('should show edited indicator when field is edited', () => {
      const field = createMockField({ isEdited: true });
      render(<ExtractedField field={field} />);

      expect(screen.getByText('(edited)')).toBeInTheDocument();
    });

    it('should apply edited styling when field is edited', () => {
      const field = createMockField({ isEdited: true });
      const { container } = render(<ExtractedField field={field} />);

      const fieldContainer = container.querySelector('.border-amber-500\\/50');
      expect(fieldContainer).toBeInTheDocument();
    });
  });

  describe('Edit Mode', () => {
    it('should enter edit mode on pencil button click', async () => {
      const user = userEvent.setup();
      const field = createMockField({ key: 'merchant', value: 'Coffee Shop' });
      render(<ExtractedField field={field} />);

      // Hover to show edit button (it's opacity-0 by default)
      const editButton = screen.getByTitle('Edit field');
      await user.click(editButton);

      expect(screen.getByRole('textbox')).toBeInTheDocument();
    });

    it('should pre-populate input with current value', async () => {
      const user = userEvent.setup();
      const field = createMockField({ key: 'merchant', value: 'Coffee Shop' });
      render(<ExtractedField field={field} />);

      const editButton = screen.getByTitle('Edit field');
      await user.click(editButton);

      expect(screen.getByRole('textbox')).toHaveValue('Coffee Shop');
    });

    it('should use number input for amount fields', async () => {
      const user = userEvent.setup();
      const field = createMockField({ key: 'amount', value: 125.50 });
      render(<ExtractedField field={field} />);

      const editButton = screen.getByTitle('Edit field');
      await user.click(editButton);

      const input = screen.getByRole('spinbutton');
      expect(input).toHaveAttribute('type', 'number');
      expect(input).toHaveAttribute('step', '0.01');
    });

    it('should use date input for date fields', async () => {
      const user = userEvent.setup();
      const field = createMockField({ key: 'date', value: '2024-03-15' });
      render(<ExtractedField field={field} />);

      const editButton = screen.getByTitle('Edit field');
      await user.click(editButton);

      const input = screen.getByDisplayValue('2024-03-15');
      expect(input).toHaveAttribute('type', 'date');
    });

    it('should save on Enter key', async () => {
      const user = userEvent.setup();
      const onUpdate = vi.fn();
      const field = createMockField({ key: 'merchant', value: 'Coffee Shop' });
      render(<ExtractedField field={field} onUpdate={onUpdate} />);

      const editButton = screen.getByTitle('Edit field');
      await user.click(editButton);

      const input = screen.getByRole('textbox');
      await user.clear(input);
      await user.type(input, 'Tea House{Enter}');

      expect(onUpdate).toHaveBeenCalledWith('merchant', 'Tea House');
    });

    it('should cancel on Escape key', async () => {
      const user = userEvent.setup();
      const onUpdate = vi.fn();
      const field = createMockField({ key: 'merchant', value: 'Coffee Shop' });
      render(<ExtractedField field={field} onUpdate={onUpdate} />);

      const editButton = screen.getByTitle('Edit field');
      await user.click(editButton);

      const input = screen.getByRole('textbox');
      await user.clear(input);
      await user.type(input, 'New Value');
      await user.keyboard('{Escape}');

      expect(onUpdate).not.toHaveBeenCalled();
      expect(screen.getByText('Coffee Shop')).toBeInTheDocument();
    });

    it('should save on check button click', async () => {
      const user = userEvent.setup();
      const onUpdate = vi.fn();
      const field = createMockField({ key: 'merchant', value: 'Coffee Shop' });
      render(<ExtractedField field={field} onUpdate={onUpdate} />);

      const editButton = screen.getByTitle('Edit field');
      await user.click(editButton);

      const input = screen.getByRole('textbox');
      await user.clear(input);
      await user.type(input, 'Tea House');

      // Click the check/save button
      const saveButton = screen.getAllByRole('button')[0];
      await user.click(saveButton);

      expect(onUpdate).toHaveBeenCalledWith('merchant', 'Tea House');
    });

    it('should cancel on X button click', async () => {
      const user = userEvent.setup();
      const onUpdate = vi.fn();
      const field = createMockField({ key: 'merchant', value: 'Coffee Shop' });
      render(<ExtractedField field={field} onUpdate={onUpdate} />);

      const editButton = screen.getByTitle('Edit field');
      await user.click(editButton);

      const input = screen.getByRole('textbox');
      await user.clear(input);
      await user.type(input, 'New Value');

      // Click the X/cancel button (second button)
      const cancelButton = screen.getAllByRole('button')[1];
      await user.click(cancelButton);

      expect(onUpdate).not.toHaveBeenCalled();
    });
  });

  describe('Validation', () => {
    it('should handle empty number field as null', async () => {
      const user = userEvent.setup();
      const onUpdate = vi.fn();
      const field = createMockField({ key: 'amount', value: 125.50 });
      render(<ExtractedField field={field} onUpdate={onUpdate} />);

      const editButton = screen.getByTitle('Edit field');
      await user.click(editButton);

      const input = screen.getByRole('spinbutton');
      await user.clear(input);

      // Save empty field - should be valid as null
      const saveButton = screen.getAllByRole('button')[0];
      await user.click(saveButton);

      expect(onUpdate).toHaveBeenCalledWith('amount', null);
    });

    it('should allow empty value (null)', async () => {
      const user = userEvent.setup();
      const onUpdate = vi.fn();
      const field = createMockField({ key: 'tip', value: 5.00 });
      render(<ExtractedField field={field} onUpdate={onUpdate} />);

      const editButton = screen.getByTitle('Edit field');
      await user.click(editButton);

      const input = screen.getByRole('spinbutton');
      await user.clear(input);

      // Save with empty value
      const saveButton = screen.getAllByRole('button')[0];
      await user.click(saveButton);

      expect(onUpdate).toHaveBeenCalledWith('tip', null);
    });

    it('should parse currency strings correctly', async () => {
      const user = userEvent.setup();
      const onUpdate = vi.fn();
      const field = createMockField({ key: 'amount', value: 125.50 });
      render(<ExtractedField field={field} onUpdate={onUpdate} />);

      const editButton = screen.getByTitle('Edit field');
      await user.click(editButton);

      const input = screen.getByRole('spinbutton');
      await user.clear(input);
      await user.type(input, '99.99{Enter}');

      expect(onUpdate).toHaveBeenCalledWith('amount', 99.99);
    });
  });

  describe('Undo Support', () => {
    it('should show undo button when field is edited and canUndo is true', () => {
      const field = createMockField({ isEdited: true });
      render(<ExtractedField field={field} canUndo />);

      expect(screen.getByTitle('Undo edit')).toBeInTheDocument();
    });

    it('should not show undo button when canUndo is false', () => {
      const field = createMockField({ isEdited: true });
      render(<ExtractedField field={field} canUndo={false} />);

      expect(screen.queryByTitle('Undo edit')).not.toBeInTheDocument();
    });

    it('should call onUndo when undo button clicked', async () => {
      const user = userEvent.setup();
      const onUndo = vi.fn();
      const field = createMockField({ isEdited: true });
      render(<ExtractedField field={field} canUndo onUndo={onUndo} />);

      const undoButton = screen.getByTitle('Undo edit');
      await user.click(undoButton);

      expect(onUndo).toHaveBeenCalled();
    });
  });

  describe('Read-Only Mode', () => {
    it('should not show edit button in read-only mode', () => {
      const field = createMockField();
      render(<ExtractedField field={field} readOnly />);

      expect(screen.queryByTitle('Edit field')).not.toBeInTheDocument();
    });

    it('should not enter edit mode on click in read-only mode', async () => {
      const user = userEvent.setup();
      const field = createMockField({ key: 'merchant', value: 'Coffee Shop' });
      const { container } = render(<ExtractedField field={field} readOnly />);

      const fieldContainer = container.firstChild as HTMLElement;
      await user.click(fieldContainer);

      expect(screen.queryByRole('textbox')).not.toBeInTheDocument();
    });
  });

  describe('Size Variants', () => {
    it('should apply small size styles', () => {
      const field = createMockField();
      const { container } = render(<ExtractedField field={field} size="sm" />);

      expect(container.querySelector('.py-1\\.5')).toBeInTheDocument();
    });

    it('should apply medium size styles by default', () => {
      const field = createMockField();
      const { container } = render(<ExtractedField field={field} />);

      expect(container.querySelector('.py-2')).toBeInTheDocument();
    });

    it('should apply large size styles', () => {
      const field = createMockField();
      const { container } = render(<ExtractedField field={field} size="lg" />);

      expect(container.querySelector('.py-3')).toBeInTheDocument();
    });
  });

  describe('Custom Label', () => {
    it('should use custom label when provided', () => {
      const field = createMockField({ key: 'amount' });
      render(<ExtractedField field={field} label="Total Cost" />);

      expect(screen.getByText('Total Cost')).toBeInTheDocument();
      expect(screen.queryByText('Amount')).not.toBeInTheDocument();
    });
  });

  describe('Bounding Box Indicator', () => {
    it('should show indicator when boundingBox is present', () => {
      const field = createMockField({
        boundingBox: { x: 10, y: 20, width: 100, height: 50 },
      });
      const { container } = render(<ExtractedField field={field} />);

      expect(container.querySelector('.animate-pulse')).toBeInTheDocument();
    });

    it('should not show indicator when boundingBox is absent', () => {
      const field = createMockField({ boundingBox: undefined });
      const { container } = render(<ExtractedField field={field} />);

      expect(container.querySelector('.animate-pulse')).not.toBeInTheDocument();
    });
  });
});

describe('ExtractedFieldSkeleton', () => {
  it('should render skeleton loader', () => {
    const { container } = render(<ExtractedFieldSkeleton />);

    expect(container.querySelector('.animate-pulse')).toBeInTheDocument();
  });

  it('should apply size variant', () => {
    const { container } = render(<ExtractedFieldSkeleton size="lg" />);

    expect(container.querySelector('.h-16')).toBeInTheDocument();
  });
});
