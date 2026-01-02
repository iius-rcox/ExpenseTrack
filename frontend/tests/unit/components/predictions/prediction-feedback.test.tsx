import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import {
  PredictionFeedback,
  PredictionFeedbackSkeleton,
} from '@/components/predictions/prediction-feedback';

/**
 * Unit tests for PredictionFeedback component (T070)
 *
 * Tests:
 * - Rendering of confirm/reject thumb buttons
 * - Click handlers for feedback actions
 * - Loading states for individual actions
 * - Disabled state during processing
 * - Size variants
 * - Accessibility attributes
 */

describe('PredictionFeedback', () => {
  const defaultProps = {
    predictionId: 'pred-123',
  };

  describe('Rendering', () => {
    it('should render confirm and reject buttons', () => {
      render(<PredictionFeedback {...defaultProps} />);

      const buttons = screen.getAllByRole('button');
      expect(buttons).toHaveLength(2);
    });

    it('should have accessible labels on buttons', () => {
      render(<PredictionFeedback {...defaultProps} />);

      expect(screen.getByLabelText('Confirm as expense')).toBeInTheDocument();
      expect(screen.getByLabelText('Not an expense')).toBeInTheDocument();
    });

    it('should apply custom className', () => {
      const { container } = render(
        <PredictionFeedback {...defaultProps} className="custom-class" />
      );

      const wrapper = container.firstChild;
      expect(wrapper).toHaveClass('custom-class');
    });
  });

  describe('Click Handlers', () => {
    it('should call onConfirm when confirm button is clicked', () => {
      const onConfirm = vi.fn();
      render(<PredictionFeedback {...defaultProps} onConfirm={onConfirm} />);

      const confirmButton = screen.getByLabelText('Confirm as expense');
      fireEvent.click(confirmButton);

      expect(onConfirm).toHaveBeenCalledWith('pred-123');
    });

    it('should call onReject when reject button is clicked', () => {
      const onReject = vi.fn();
      render(<PredictionFeedback {...defaultProps} onReject={onReject} />);

      const rejectButton = screen.getByLabelText('Not an expense');
      fireEvent.click(rejectButton);

      expect(onReject).toHaveBeenCalledWith('pred-123');
    });

    it('should stop event propagation when clicking buttons', () => {
      const onConfirm = vi.fn();
      const parentClick = vi.fn();

      render(
        <div onClick={parentClick}>
          <PredictionFeedback {...defaultProps} onConfirm={onConfirm} />
        </div>
      );

      const confirmButton = screen.getByLabelText('Confirm as expense');
      fireEvent.click(confirmButton);

      expect(onConfirm).toHaveBeenCalled();
      expect(parentClick).not.toHaveBeenCalled();
    });

    it('should not call handlers when disabled', () => {
      const onConfirm = vi.fn();
      const onReject = vi.fn();

      render(
        <PredictionFeedback
          {...defaultProps}
          onConfirm={onConfirm}
          onReject={onReject}
          disabled
        />
      );

      const confirmButton = screen.getByLabelText('Confirm as expense');
      const rejectButton = screen.getByLabelText('Not an expense');

      fireEvent.click(confirmButton);
      fireEvent.click(rejectButton);

      expect(onConfirm).not.toHaveBeenCalled();
      expect(onReject).not.toHaveBeenCalled();
    });
  });

  describe('Loading States', () => {
    it('should disable buttons when isConfirming is true', () => {
      render(<PredictionFeedback {...defaultProps} isConfirming />);

      const buttons = screen.getAllByRole('button');
      buttons.forEach((button) => {
        expect(button).toBeDisabled();
      });
    });

    it('should disable buttons when isRejecting is true', () => {
      render(<PredictionFeedback {...defaultProps} isRejecting />);

      const buttons = screen.getAllByRole('button');
      buttons.forEach((button) => {
        expect(button).toBeDisabled();
      });
    });

    it('should show spinner on confirm button when isConfirming', () => {
      const { container } = render(
        <PredictionFeedback {...defaultProps} isConfirming />
      );

      // Should have animate-spin class for loading spinner
      const spinner = container.querySelector('[class*="animate-spin"]');
      expect(spinner).toBeInTheDocument();
    });

    it('should show spinner on reject button when isRejecting', () => {
      const { container } = render(
        <PredictionFeedback {...defaultProps} isRejecting />
      );

      // Should have animate-spin class for loading spinner
      const spinner = container.querySelector('[class*="animate-spin"]');
      expect(spinner).toBeInTheDocument();
    });
  });

  describe('Size Variants', () => {
    it('should render small size correctly', () => {
      const { container } = render(
        <PredictionFeedback {...defaultProps} size="sm" />
      );

      // Small buttons should have h-6 w-6 class
      const button = container.querySelector('[class*="h-6"]');
      expect(button).toBeInTheDocument();
    });

    it('should render medium size by default', () => {
      const { container } = render(<PredictionFeedback {...defaultProps} />);

      // Medium buttons should have h-8 w-8 class
      const button = container.querySelector('[class*="h-8"]');
      expect(button).toBeInTheDocument();
    });

    it('should render large size correctly', () => {
      const { container } = render(
        <PredictionFeedback {...defaultProps} size="lg" />
      );

      // Large buttons should have h-10 w-10 class
      const button = container.querySelector('[class*="h-10"]');
      expect(button).toBeInTheDocument();
    });
  });

  describe('Disabled State', () => {
    it('should disable all buttons when disabled prop is true', () => {
      render(<PredictionFeedback {...defaultProps} disabled />);

      const buttons = screen.getAllByRole('button');
      buttons.forEach((button) => {
        expect(button).toBeDisabled();
      });
    });

    it('should disable buttons when any action is processing', () => {
      const { rerender } = render(
        <PredictionFeedback {...defaultProps} isConfirming />
      );

      let buttons = screen.getAllByRole('button');
      buttons.forEach((button) => {
        expect(button).toBeDisabled();
      });

      rerender(<PredictionFeedback {...defaultProps} isRejecting />);

      buttons = screen.getAllByRole('button');
      buttons.forEach((button) => {
        expect(button).toBeDisabled();
      });
    });
  });
});

describe('PredictionFeedbackSkeleton', () => {
  it('should render loading skeleton', () => {
    const { container } = render(<PredictionFeedbackSkeleton />);

    // Should have animate-pulse class for loading state
    const skeleton = container.querySelector('[class*="animate-pulse"]');
    expect(skeleton).toBeInTheDocument();
  });

  it('should render two skeleton buttons', () => {
    const { container } = render(<PredictionFeedbackSkeleton />);

    // Should have two skeleton buttons
    const skeletonButtons = container.querySelectorAll('.rounded-md');
    expect(skeletonButtons.length).toBe(2);
  });

  it('should respect size variant', () => {
    const { container } = render(<PredictionFeedbackSkeleton size="lg" />);

    // Large buttons should have h-10 w-10 class
    const button = container.querySelector('[class*="h-10"]');
    expect(button).toBeInTheDocument();
  });
});
