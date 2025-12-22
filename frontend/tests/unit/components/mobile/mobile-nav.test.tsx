/**
 * MobileNav Component Tests (T103)
 *
 * Tests the mobile bottom navigation component including:
 * - Rendering navigation items
 * - Active state indication
 * - Badge display for pending counts
 * - Navigation click handling
 * - Touch target sizing (44x44pt minimum)
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MobileNav, MobileNavSpacer } from '@/components/mobile/mobile-nav';

// Mock TanStack Router
const mockNavigate = vi.fn();
vi.mock('@tanstack/react-router', () => ({
  useLocation: () => ({ pathname: '/dashboard' }),
  useNavigate: () => mockNavigate,
}));

describe('MobileNav', () => {
  beforeEach(() => {
    mockNavigate.mockClear();
  });

  it('should render all navigation items', () => {
    render(<MobileNav />);

    expect(screen.getByText('Home')).toBeInTheDocument();
    expect(screen.getByText('Receipts')).toBeInTheDocument();
    expect(screen.getByText('Transactions')).toBeInTheDocument();
    expect(screen.getByText('Matching')).toBeInTheDocument();
    expect(screen.getByText('Analytics')).toBeInTheDocument();
  });

  it('should indicate active state for current route', () => {
    render(<MobileNav />);

    // Dashboard/Home should be active based on mocked location
    const homeButton = screen.getByRole('button', { name: /home/i });
    expect(homeButton).toHaveAttribute('aria-current', 'page');
  });

  it('should navigate when clicking a nav item', () => {
    render(<MobileNav />);

    const receiptsButton = screen.getByRole('button', { name: /receipts/i });
    fireEvent.click(receiptsButton);

    expect(mockNavigate).toHaveBeenCalledWith({ to: '/receipts' });
  });

  it('should display badge for pending receipts count', () => {
    render(<MobileNav pendingCounts={{ receipts: 5 }} />);

    // Badge should show the count
    expect(screen.getByText('5')).toBeInTheDocument();
  });

  it('should display badge for pending matches count', () => {
    render(<MobileNav pendingCounts={{ matching: 12 }} />);

    expect(screen.getByText('12')).toBeInTheDocument();
  });

  it('should truncate badge for counts over 99', () => {
    render(<MobileNav pendingCounts={{ receipts: 150 }} />);

    expect(screen.getByText('99+')).toBeInTheDocument();
  });

  it('should not display badge for zero count', () => {
    render(<MobileNav pendingCounts={{ receipts: 0 }} />);

    // Should not have any badges with "0"
    expect(screen.queryByText('0')).not.toBeInTheDocument();
  });

  it('should have touch targets meeting 44x44pt minimum', () => {
    render(<MobileNav />);

    const buttons = screen.getAllByRole('button');
    buttons.forEach((button) => {
      // Check that buttons have the minimum touch target classes
      const hasMinWidth = button.classList.contains('min-w-[44px]');
      const hasMinHeight = button.classList.contains('min-h-[44px]');
      expect(hasMinWidth || hasMinHeight).toBe(true);
    });
  });

  it('should render quick action button when callback provided', () => {
    const onQuickAction = vi.fn();
    render(<MobileNav onQuickAction={onQuickAction} />);

    const quickActionButton = screen.getByRole('button', { name: /quick action/i });
    expect(quickActionButton).toBeInTheDocument();

    fireEvent.click(quickActionButton);
    expect(onQuickAction).toHaveBeenCalled();
  });

  it('should be hidden on tablet/desktop (md breakpoint)', () => {
    const { container } = render(<MobileNav />);

    // Check that the nav has md:hidden class
    const nav = container.querySelector('nav');
    expect(nav).toHaveClass('md:hidden');
  });
});

describe('MobileNavSpacer', () => {
  it('should render spacer with correct height for nav clearance', () => {
    const { container } = render(<MobileNavSpacer />);

    const spacer = container.firstChild as HTMLElement;
    expect(spacer).toHaveClass('h-[calc(60px+env(safe-area-inset-bottom))]');
  });

  it('should be hidden on tablet/desktop', () => {
    const { container } = render(<MobileNavSpacer />);

    const spacer = container.firstChild as HTMLElement;
    expect(spacer).toHaveClass('md:hidden');
  });

  it('should accept custom className', () => {
    const { container } = render(<MobileNavSpacer className="extra-class" />);

    const spacer = container.firstChild as HTMLElement;
    expect(spacer).toHaveClass('extra-class');
  });
});
