import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import {
  ConfidenceIndicator,
  ConfidenceInline,
  ConfidenceBadge,
} from '@/components/design-system/confidence-indicator';

describe('ConfidenceIndicator', () => {
  it('should render with correct number of filled dots for high confidence', () => {
    const { container } = render(<ConfidenceIndicator score={0.95} />);

    // 95% confidence should fill approximately 5 dots (0.95 * 5 = 4.75, rounded to 5)
    const dots = container.querySelectorAll('[class*="rounded-full"][class*="transition"]');
    expect(dots.length).toBe(5);

    // Count filled dots (those with bg-confidence-high class)
    const filledDots = container.querySelectorAll('[class*="bg-confidence-high"]');
    expect(filledDots.length).toBe(5);
  });

  it('should render with correct number of filled dots for medium confidence', () => {
    const { container } = render(<ConfidenceIndicator score={0.75} />);

    // 75% confidence should fill approximately 4 dots (0.75 * 5 = 3.75, rounded to 4)
    const filledDots = container.querySelectorAll('[class*="bg-confidence-medium"]');
    expect(filledDots.length).toBe(4);
  });

  it('should render with correct number of filled dots for low confidence', () => {
    const { container } = render(<ConfidenceIndicator score={0.5} />);

    // 50% confidence should fill approximately 3 dots (0.5 * 5 = 2.5, rounded to 3)
    const filledDots = container.querySelectorAll('[class*="bg-confidence-low"]');
    expect(filledDots.length).toBe(3);
  });

  it('should show label when showLabel is true', () => {
    render(<ConfidenceIndicator score={0.85} showLabel />);

    expect(screen.getByText('85%')).toBeInTheDocument();
  });

  it('should not show label by default', () => {
    render(<ConfidenceIndicator score={0.85} />);

    expect(screen.queryByText('85%')).not.toBeInTheDocument();
  });

  it('should have correct aria attributes', () => {
    render(<ConfidenceIndicator score={0.9} ariaLabel="Test confidence" />);

    const meter = screen.getByRole('meter');
    expect(meter).toHaveAttribute('aria-label', 'Test confidence');
    expect(meter).toHaveAttribute('aria-valuenow', '90');
    expect(meter).toHaveAttribute('aria-valuemin', '0');
    expect(meter).toHaveAttribute('aria-valuemax', '100');
  });

  it('should clamp scores to 0-1 range', () => {
    const { rerender, container } = render(<ConfidenceIndicator score={1.5} />);

    // Score > 1 should be clamped to 1 (5 filled dots, high confidence)
    const filledDots = container.querySelectorAll('[class*="bg-confidence-high"]');
    expect(filledDots.length).toBe(5);

    rerender(<ConfidenceIndicator score={-0.5} />);

    // Score < 0 should be clamped to 0 (0 filled dots)
    const allFilledDots = container.querySelectorAll(
      '[class*="bg-confidence-high"], [class*="bg-confidence-medium"], [class*="bg-confidence-low"]'
    );
    expect(allFilledDots.length).toBe(0);
  });

  it('should apply size variants correctly', () => {
    const { rerender, container } = render(
      <ConfidenceIndicator score={0.9} size="sm" />
    );

    // Just check that dots exist for each size variant
    expect(container.querySelectorAll('[class*="rounded-full"]').length).toBeGreaterThan(0);

    rerender(<ConfidenceIndicator score={0.9} size="md" />);
    expect(container.querySelectorAll('[class*="rounded-full"]').length).toBeGreaterThan(0);

    rerender(<ConfidenceIndicator score={0.9} size="lg" />);
    expect(container.querySelectorAll('[class*="rounded-full"]').length).toBeGreaterThan(0);
  });
});

describe('ConfidenceInline', () => {
  it('should render percentage', () => {
    render(<ConfidenceInline score={0.92} />);

    expect(screen.getByText('92%')).toBeInTheDocument();
  });

  it('should render colored dot', () => {
    const { container } = render(<ConfidenceInline score={0.95} />);

    expect(container.querySelector('[class*="bg-confidence-high"]')).toBeInTheDocument();
  });
});

describe('ConfidenceBadge', () => {
  it('should render percentage', () => {
    render(<ConfidenceBadge score={0.88} />);

    expect(screen.getByText('88%')).toBeInTheDocument();
  });

  it('should apply correct color classes for medium confidence', () => {
    const { container } = render(<ConfidenceBadge score={0.75} />);

    // Should have medium confidence styling
    expect(
      container.querySelector('[class*="text-confidence-medium"]')
    ).toBeInTheDocument();
  });
});
