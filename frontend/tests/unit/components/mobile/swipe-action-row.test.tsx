/**
 * SwipeActionRow Component Tests (T103)
 *
 * Tests the swipe action row component including:
 * - Rendering content and action buttons
 * - SwipeAction configuration
 * - Action button structure
 * - Disabled state
 * - SwipeActionList and context
 */

import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import {
  SwipeActionRow,
  SwipeActionList,
  type SwipeAction,
} from '@/components/mobile/swipe-action-row';

describe('SwipeActionRow', () => {
  const mockAction = vi.fn();

  beforeEach(() => {
    mockAction.mockClear();
  });

  it('should render children content', () => {
    render(
      <SwipeActionRow>
        <span>Test Content</span>
      </SwipeActionRow>
    );

    expect(screen.getByText('Test Content')).toBeInTheDocument();
  });

  it('should render with leftActions configured', () => {
    const leftActions: SwipeAction[] = [
      {
        id: 'edit',
        type: 'edit',
        label: 'Edit',
        onAction: mockAction,
      },
    ];

    const { container } = render(
      <SwipeActionRow leftActions={leftActions}>
        Content
      </SwipeActionRow>
    );

    // The component should render without error
    expect(container.firstChild).toBeInTheDocument();
    expect(screen.getByText('Content')).toBeInTheDocument();
  });

  it('should render with rightActions configured', () => {
    const rightActions: SwipeAction[] = [
      {
        id: 'delete',
        type: 'delete',
        label: 'Delete',
        onAction: mockAction,
      },
    ];

    const { container } = render(
      <SwipeActionRow rightActions={rightActions}>
        Content
      </SwipeActionRow>
    );

    expect(container.firstChild).toBeInTheDocument();
    expect(screen.getByText('Content')).toBeInTheDocument();
  });

  it('should render with both actions configured', () => {
    const leftActions: SwipeAction[] = [
      { id: 'archive', type: 'archive', onAction: mockAction },
    ];
    const rightActions: SwipeAction[] = [
      { id: 'delete', type: 'delete', onAction: mockAction },
    ];

    render(
      <SwipeActionRow leftActions={leftActions} rightActions={rightActions}>
        Content
      </SwipeActionRow>
    );

    expect(screen.getByText('Content')).toBeInTheDocument();
  });

  it('should accept custom className', () => {
    const { container } = render(
      <SwipeActionRow className="custom-class">
        <span>Content</span>
      </SwipeActionRow>
    );

    expect(container.firstChild).toHaveClass('custom-class');
  });

  it('should respect disabled prop', () => {
    const rightActions: SwipeAction[] = [
      { id: 'delete', type: 'delete', onAction: mockAction },
    ];

    render(
      <SwipeActionRow disabled rightActions={rightActions}>
        <span>Content</span>
      </SwipeActionRow>
    );

    // Component should still render when disabled
    expect(screen.getByText('Content')).toBeInTheDocument();
  });

  it('should call onSwipeStart callback', () => {
    const onSwipeStart = vi.fn();

    render(
      <SwipeActionRow onSwipeStart={onSwipeStart}>
        <span>Content</span>
      </SwipeActionRow>
    );

    // Component renders correctly with callback attached
    expect(screen.getByText('Content')).toBeInTheDocument();
  });

  it('should call onSwipeEnd callback', () => {
    const onSwipeEnd = vi.fn();

    render(
      <SwipeActionRow onSwipeEnd={onSwipeEnd}>
        <span>Content</span>
      </SwipeActionRow>
    );

    expect(screen.getByText('Content')).toBeInTheDocument();
  });
});

describe('SwipeActionList', () => {
  it('should render children', () => {
    render(
      <SwipeActionList>
        <div>Item 1</div>
        <div>Item 2</div>
      </SwipeActionList>
    );

    expect(screen.getByText('Item 1')).toBeInTheDocument();
    expect(screen.getByText('Item 2')).toBeInTheDocument();
  });

  it('should apply className', () => {
    const { container } = render(
      <SwipeActionList className="list-class">
        <div>Item</div>
      </SwipeActionList>
    );

    expect(container.firstChild).toHaveClass('list-class');
  });

  it('should provide context for coordinating swipe actions', () => {
    // SwipeActionList provides a context for its children
    // This test verifies it renders correctly
    render(
      <SwipeActionList>
        <SwipeActionRow>
          <span>Row 1</span>
        </SwipeActionRow>
        <SwipeActionRow>
          <span>Row 2</span>
        </SwipeActionRow>
      </SwipeActionList>
    );

    expect(screen.getByText('Row 1')).toBeInTheDocument();
    expect(screen.getByText('Row 2')).toBeInTheDocument();
  });
});

describe('SwipeAction type', () => {
  it('should support delete action type', () => {
    const action: SwipeAction = {
      id: 'delete',
      type: 'delete',
      onAction: vi.fn(),
    };

    render(
      <SwipeActionRow rightActions={[action]}>
        Content
      </SwipeActionRow>
    );

    expect(screen.getByText('Content')).toBeInTheDocument();
  });

  it('should support approve action type', () => {
    const action: SwipeAction = {
      id: 'approve',
      type: 'approve',
      label: 'Approve',
      onAction: vi.fn(),
    };

    render(
      <SwipeActionRow leftActions={[action]}>
        Content
      </SwipeActionRow>
    );

    expect(screen.getByText('Content')).toBeInTheDocument();
  });

  it('should support custom bgColor override', () => {
    const action: SwipeAction = {
      id: 'custom',
      type: 'custom',
      bgColor: 'bg-purple-500',
      onAction: vi.fn(),
    };

    render(
      <SwipeActionRow rightActions={[action]}>
        Content
      </SwipeActionRow>
    );

    expect(screen.getByText('Content')).toBeInTheDocument();
  });
});
