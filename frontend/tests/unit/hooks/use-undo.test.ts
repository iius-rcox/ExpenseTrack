import { describe, it, expect } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useUndo, useUndoObject } from '@/hooks/ui/use-undo';

describe('useUndo', () => {
  it('should initialize with the initial value', () => {
    const { result } = renderHook(() => useUndo('initial'));
    expect(result.current.current).toBe('initial');
    expect(result.current.canUndo).toBe(false);
    expect(result.current.canRedo).toBe(false);
  });

  it('should push new values to history', () => {
    const { result } = renderHook(() => useUndo('initial'));

    act(() => {
      result.current.push('second');
    });

    expect(result.current.current).toBe('second');
    expect(result.current.canUndo).toBe(true);
    expect(result.current.canRedo).toBe(false);
  });

  it('should undo to previous value', () => {
    const { result } = renderHook(() => useUndo('initial'));

    act(() => {
      result.current.push('second');
      result.current.push('third');
    });

    expect(result.current.current).toBe('third');

    act(() => {
      result.current.undo();
    });

    expect(result.current.current).toBe('second');
    expect(result.current.canUndo).toBe(true);
    expect(result.current.canRedo).toBe(true);
  });

  it('should redo to next value', () => {
    const { result } = renderHook(() => useUndo('initial'));

    act(() => {
      result.current.push('second');
    });

    act(() => {
      result.current.undo();
    });

    expect(result.current.current).toBe('initial');

    act(() => {
      result.current.redo();
    });

    expect(result.current.current).toBe('second');
    expect(result.current.canRedo).toBe(false);
  });

  it('should clear redo history when pushing after undo', () => {
    const { result } = renderHook(() => useUndo('initial'));

    act(() => {
      result.current.push('second');
    });

    act(() => {
      result.current.push('third');
    });

    act(() => {
      result.current.undo();
    });

    expect(result.current.canRedo).toBe(true);

    act(() => {
      result.current.push('new-branch');
    });

    expect(result.current.current).toBe('new-branch');
    expect(result.current.canRedo).toBe(false);

    // Cannot redo to 'third' anymore
    act(() => {
      result.current.undo();
    });

    expect(result.current.current).toBe('second');
  });

  it('should respect maxHistory option', () => {
    const { result } = renderHook(() =>
      useUndo('initial', { maxHistory: 3 })
    );

    act(() => {
      result.current.push('second');
      result.current.push('third');
      result.current.push('fourth');
    });

    // Should only have last 3 items
    expect(result.current.undoCount).toBeLessThanOrEqual(2);
  });

  it('should reset to new initial value', () => {
    const { result } = renderHook(() => useUndo('initial'));

    act(() => {
      result.current.push('second');
      result.current.push('third');
    });

    act(() => {
      result.current.reset('reset-value');
    });

    expect(result.current.current).toBe('reset-value');
    expect(result.current.canUndo).toBe(false);
    expect(result.current.canRedo).toBe(false);
  });

  it('should not undo when at beginning of history', () => {
    const { result } = renderHook(() => useUndo('initial'));

    expect(result.current.canUndo).toBe(false);

    act(() => {
      result.current.undo();
    });

    expect(result.current.current).toBe('initial');
  });

  it('should not redo when at end of history', () => {
    const { result } = renderHook(() => useUndo('initial'));

    act(() => {
      result.current.push('second');
    });

    expect(result.current.canRedo).toBe(false);

    act(() => {
      result.current.redo();
    });

    expect(result.current.current).toBe('second');
  });
});

describe('useUndoObject', () => {
  it('should update specific fields while preserving history', () => {
    const { result } = renderHook(() =>
      useUndoObject({ name: 'John', age: 30 })
    );

    act(() => {
      result.current.update({ age: 31 });
    });

    expect(result.current.current).toEqual({ name: 'John', age: 31 });
    expect(result.current.canUndo).toBe(true);

    act(() => {
      result.current.undo();
    });

    expect(result.current.current).toEqual({ name: 'John', age: 30 });
  });
});
