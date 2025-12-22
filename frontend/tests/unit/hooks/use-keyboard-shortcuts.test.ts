import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook } from '@testing-library/react';
import { useKeyboardShortcuts } from '@/hooks/ui/use-keyboard-shortcuts';

describe('useKeyboardShortcuts', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  const simulateKeyDown = (key: string, options: Partial<KeyboardEvent> = {}) => {
    const event = new KeyboardEvent('keydown', {
      key,
      bubbles: true,
      cancelable: true,
      ...options,
    });
    document.dispatchEvent(event);
  };

  it('should call handler for matching key', () => {
    const handler = vi.fn();
    renderHook(() => useKeyboardShortcuts({ a: handler }));

    simulateKeyDown('a');

    expect(handler).toHaveBeenCalledTimes(1);
  });

  it('should handle uppercase letters as lowercase', () => {
    const handler = vi.fn();
    renderHook(() => useKeyboardShortcuts({ a: handler }));

    simulateKeyDown('A');

    expect(handler).toHaveBeenCalledTimes(1);
  });

  it('should not call handler when disabled', () => {
    const handler = vi.fn();
    renderHook(() =>
      useKeyboardShortcuts({ a: handler }, { enabled: false })
    );

    simulateKeyDown('a');

    expect(handler).not.toHaveBeenCalled();
  });

  it('should ignore events from input elements by default', () => {
    const handler = vi.fn();
    renderHook(() => useKeyboardShortcuts({ a: handler }));

    // Create and focus an input element
    const input = document.createElement('input');
    document.body.appendChild(input);
    input.focus();

    // Dispatch event with input as target
    const event = new KeyboardEvent('keydown', {
      key: 'a',
      bubbles: true,
      cancelable: true,
    });
    Object.defineProperty(event, 'target', { value: input });
    document.dispatchEvent(event);

    expect(handler).not.toHaveBeenCalled();

    // Cleanup
    document.body.removeChild(input);
  });

  it('should call handler for events from inputs when ignoreInputs is false', () => {
    const handler = vi.fn();
    renderHook(() =>
      useKeyboardShortcuts({ a: handler }, { ignoreInputs: false })
    );

    // The handler should be called even from input contexts
    simulateKeyDown('a');

    expect(handler).toHaveBeenCalled();
  });

  it('should handle arrow keys', () => {
    const leftHandler = vi.fn();
    const rightHandler = vi.fn();
    renderHook(() =>
      useKeyboardShortcuts({
        ArrowLeft: leftHandler,
        ArrowRight: rightHandler,
      })
    );

    simulateKeyDown('ArrowLeft');
    simulateKeyDown('ArrowRight');

    expect(leftHandler).toHaveBeenCalledTimes(1);
    expect(rightHandler).toHaveBeenCalledTimes(1);
  });

  it('should handle escape key', () => {
    const handler = vi.fn();
    renderHook(() => useKeyboardShortcuts({ Escape: handler }));

    simulateKeyDown('Escape');

    expect(handler).toHaveBeenCalledTimes(1);
  });

  it('should cleanup event listener on unmount', () => {
    const handler = vi.fn();
    const { unmount } = renderHook(() =>
      useKeyboardShortcuts({ a: handler })
    );

    unmount();

    simulateKeyDown('a');

    expect(handler).not.toHaveBeenCalled();
  });

  it('should handle multiple shortcuts', () => {
    const handlers = {
      a: vi.fn(),
      b: vi.fn(),
      c: vi.fn(),
    };
    renderHook(() => useKeyboardShortcuts(handlers));

    simulateKeyDown('a');
    simulateKeyDown('b');
    simulateKeyDown('c');
    simulateKeyDown('d'); // Should not match anything

    expect(handlers.a).toHaveBeenCalledTimes(1);
    expect(handlers.b).toHaveBeenCalledTimes(1);
    expect(handlers.c).toHaveBeenCalledTimes(1);
  });

  it('should update shortcuts when they change', () => {
    const handler1 = vi.fn();
    const handler2 = vi.fn();

    const { rerender } = renderHook(
      ({ shortcuts }) => useKeyboardShortcuts(shortcuts),
      { initialProps: { shortcuts: { a: handler1 } } }
    );

    simulateKeyDown('a');
    expect(handler1).toHaveBeenCalledTimes(1);

    rerender({ shortcuts: { a: handler2 } });

    simulateKeyDown('a');
    expect(handler2).toHaveBeenCalledTimes(1);
    expect(handler1).toHaveBeenCalledTimes(1); // Not called again
  });
});
