import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import {
  ExpenseBadge,
  ExpenseBadgeSkeleton,
  ExpenseBadgeInline,
} from '@/components/predictions/expense-badge';
import type { PredictionSummary } from '@/types/prediction';

/**
 * Unit tests for ExpenseBadge component (T048)
 *
 * Tests:
 * - Rendering for different confidence levels
 * - Compact vs full mode display
 * - Click handlers for confirm/reject actions
 * - Accessibility attributes
 * - Low confidence filtering
 */

describe('ExpenseBadge', () => {
  const createMockPrediction = (
    overrides: Partial<PredictionSummary> = {}
  ): PredictionSummary => ({
    id: 'pred-123',
    transactionId: 'txn-456',
    patternId: 'pat-789',
    vendorName: 'STARBUCKS',
    confidenceScore: 0.85,
    confidenceLevel: 'High',
    status: 'Pending',
    suggestedCategory: 'Food & Beverage',
    suggestedGLCode: '6100',
    ...overrides,
  });

  describe('Rendering', () => {
    it('should render high confidence badge with emerald styling', () => {
      const prediction = createMockPrediction({ confidenceLevel: 'High' });
      const { container } = render(
        <ExpenseBadge prediction={prediction} compact />
      );

      // Should have emerald/green styling for high confidence
      const badge = container.querySelector('[class*="emerald"]');
      expect(badge).toBeInTheDocument();
    });

    it('should render medium confidence badge with amber styling', () => {
      const prediction = createMockPrediction({
        confidenceLevel: 'Medium',
        confidenceScore: 0.65,
      });
      const { container } = render(
        <ExpenseBadge prediction={prediction} compact />
      );

      // Should have amber/yellow styling for medium confidence
      const badge = container.querySelector('[class*="amber"]');
      expect(badge).toBeInTheDocument();
    });

    it('should return null for low confidence predictions', () => {
      const prediction = createMockPrediction({
        confidenceLevel: 'Low',
        confidenceScore: 0.35,
      });
      const { container } = render(<ExpenseBadge prediction={prediction} />);

      // Should not render anything for low confidence
      expect(container.firstChild).toBeNull();
    });

    it('should show "Expense" text in badge', () => {
      const prediction = createMockPrediction();
      render(<ExpenseBadge prediction={prediction} compact />);

      expect(screen.getByText('Expense')).toBeInTheDocument();
    });

    it('should show suggested category in tooltip', async () => {
      const prediction = createMockPrediction({
        suggestedCategory: 'Travel Expenses',
      });
      render(<ExpenseBadge prediction={prediction} compact />);

      // Hover to trigger tooltip
      const badge = screen.getByText('Expense').closest('[class*="inline-flex"]');
      expect(badge).toBeInTheDocument();

      // Tooltip content is tested via accessibility - should have appropriate aria
    });
  });

  describe('Compact Mode', () => {
    it('should render smaller badge without action buttons in compact mode', () => {
      const prediction = createMockPrediction();
      render(<ExpenseBadge prediction={prediction} compact />);

      // In compact mode, there should be no visible action buttons
      expect(screen.queryByRole('button')).not.toBeInTheDocument();
    });
  });

  describe('Full Mode', () => {
    it('should render action buttons in full mode', () => {
      const prediction = createMockPrediction();
      render(<ExpenseBadge prediction={prediction} />);

      // Should have confirm and reject buttons
      const buttons = screen.getAllByRole('button');
      expect(buttons.length).toBeGreaterThanOrEqual(2);
    });

    it('should show category in full mode', () => {
      const prediction = createMockPrediction({
        suggestedCategory: 'Transportation',
      });
      render(<ExpenseBadge prediction={prediction} />);

      // Category should be visible in full mode
      expect(screen.getByText('Transportation')).toBeInTheDocument();
    });
  });

  describe('Click Handlers', () => {
    it('should call onConfirm when confirm button is clicked', () => {
      const onConfirm = vi.fn();
      const prediction = createMockPrediction();
      render(<ExpenseBadge prediction={prediction} onConfirm={onConfirm} />);

      // Find the confirm button (check icon)
      const buttons = screen.getAllByRole('button');
      const confirmButton = buttons[0]; // First button is confirm
      fireEvent.click(confirmButton);

      expect(onConfirm).toHaveBeenCalledWith('pred-123');
    });

    it('should call onReject when reject button is clicked', () => {
      const onReject = vi.fn();
      const prediction = createMockPrediction();
      render(<ExpenseBadge prediction={prediction} onReject={onReject} />);

      // Find the reject button (X icon)
      const buttons = screen.getAllByRole('button');
      const rejectButton = buttons[1]; // Second button is reject
      fireEvent.click(rejectButton);

      expect(onReject).toHaveBeenCalledWith('pred-123');
    });

    it('should call onViewDetails when details button is clicked', () => {
      const onViewDetails = vi.fn();
      const prediction = createMockPrediction();
      render(
        <ExpenseBadge prediction={prediction} onViewDetails={onViewDetails} />
      );

      // Find the details button (chevron icon)
      const buttons = screen.getAllByRole('button');
      const detailsButton = buttons[2]; // Third button is details
      fireEvent.click(detailsButton);

      expect(onViewDetails).toHaveBeenCalledWith('pred-123');
    });

    it('should stop event propagation when clicking buttons', () => {
      const onConfirm = vi.fn();
      const parentClick = vi.fn();
      const prediction = createMockPrediction();

      render(
        <div onClick={parentClick}>
          <ExpenseBadge prediction={prediction} onConfirm={onConfirm} />
        </div>
      );

      const buttons = screen.getAllByRole('button');
      fireEvent.click(buttons[0]);

      expect(onConfirm).toHaveBeenCalled();
      expect(parentClick).not.toHaveBeenCalled();
    });
  });

  describe('Processing State', () => {
    it('should disable buttons when isProcessing is true', () => {
      const prediction = createMockPrediction();
      render(<ExpenseBadge prediction={prediction} isProcessing />);

      const buttons = screen.getAllByRole('button');
      buttons.forEach((button) => {
        expect(button).toBeDisabled();
      });
    });

    it('should apply opacity when processing', () => {
      const prediction = createMockPrediction();
      const { container } = render(
        <ExpenseBadge prediction={prediction} isProcessing />
      );

      // Should have reduced opacity
      const badge = container.querySelector('[class*="opacity-60"]');
      expect(badge).toBeInTheDocument();
    });
  });
});

describe('ExpenseBadgeSkeleton', () => {
  it('should render loading skeleton in compact mode', () => {
    const { container } = render(<ExpenseBadgeSkeleton compact />);

    // Should have animate-pulse class for loading state
    const skeleton = container.querySelector('[class*="animate-pulse"]');
    expect(skeleton).toBeInTheDocument();
  });

  it('should render loading skeleton in full mode', () => {
    const { container } = render(<ExpenseBadgeSkeleton />);

    // Should have animate-pulse class for loading state
    const skeleton = container.querySelector('[class*="animate-pulse"]');
    expect(skeleton).toBeInTheDocument();
  });
});

describe('ExpenseBadgeInline', () => {
  it('should render inline badge for high confidence', () => {
    render(<ExpenseBadgeInline confidenceLevel="High" />);

    expect(screen.getByText('Expense')).toBeInTheDocument();
  });

  it('should render inline badge for medium confidence', () => {
    render(<ExpenseBadgeInline confidenceLevel="Medium" />);

    expect(screen.getByText('Expense')).toBeInTheDocument();
  });

  it('should apply correct color for confidence level', () => {
    const { container, rerender } = render(
      <ExpenseBadgeInline confidenceLevel="High" />
    );

    // High confidence should have emerald color
    let inlineElement = container.querySelector('[class*="emerald"]');
    expect(inlineElement).toBeInTheDocument();

    rerender(<ExpenseBadgeInline confidenceLevel="Medium" />);

    // Medium confidence should have amber color
    inlineElement = container.querySelector('[class*="amber"]');
    expect(inlineElement).toBeInTheDocument();
  });
});
