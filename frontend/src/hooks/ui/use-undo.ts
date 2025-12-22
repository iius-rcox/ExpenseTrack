import { useState, useCallback, useMemo } from 'react';

/**
 * Undo Stack Hook for Auto-Save with Undo Capability
 *
 * Provides a local undo/redo stack for form editing and inline changes.
 * Designed for the auto-save pattern where changes are saved immediately
 * but users can undo if needed.
 *
 * @example
 * ```tsx
 * const { current, push, undo, redo, canUndo, canRedo } = useUndo(initialValue);
 *
 * const handleChange = (newValue) => {
 *   push(newValue);
 *   saveToServer(newValue); // Auto-save
 * };
 * ```
 */

export interface UseUndoOptions {
  /** Maximum number of history entries to keep (default: 10) */
  maxHistory?: number;
}

export interface UseUndoReturn<T> {
  /** The current value in the undo stack */
  current: T;
  /** Push a new value onto the stack (clears redo history) */
  push: (value: T) => void;
  /** Undo to the previous value */
  undo: () => void;
  /** Redo to the next value (if available) */
  redo: () => void;
  /** Reset the stack with a new initial value */
  reset: (value: T) => void;
  /** Whether undo is available */
  canUndo: boolean;
  /** Whether redo is available */
  canRedo: boolean;
  /** Number of undo steps available */
  undoCount: number;
  /** Number of redo steps available */
  redoCount: number;
}

export function useUndo<T>(
  initialValue: T,
  options: UseUndoOptions = {}
): UseUndoReturn<T> {
  const { maxHistory = 10 } = options;

  // Store both history and pointer in a single state to avoid stale closure issues
  const [state, setState] = useState<{ history: T[]; pointer: number }>({
    history: [initialValue],
    pointer: 0,
  });

  const current = state.history[state.pointer];
  const canUndo = state.pointer > 0;
  const canRedo = state.pointer < state.history.length - 1;
  const undoCount = state.pointer;
  const redoCount = state.history.length - 1 - state.pointer;

  const push = useCallback(
    (value: T) => {
      setState((prev) => {
        // Remove all "future" history (redo states) when pushing a new value
        const newHistory = prev.history.slice(0, prev.pointer + 1);
        newHistory.push(value);

        // Enforce max history limit
        if (newHistory.length > maxHistory) {
          newHistory.shift();
          return {
            history: newHistory,
            pointer: newHistory.length - 1,
          };
        }

        return {
          history: newHistory,
          pointer: newHistory.length - 1,
        };
      });
    },
    [maxHistory]
  );

  const undo = useCallback(() => {
    setState((prev) => {
      if (prev.pointer > 0) {
        return { ...prev, pointer: prev.pointer - 1 };
      }
      return prev;
    });
  }, []);

  const redo = useCallback(() => {
    setState((prev) => {
      if (prev.pointer < prev.history.length - 1) {
        return { ...prev, pointer: prev.pointer + 1 };
      }
      return prev;
    });
  }, []);

  const reset = useCallback((value: T) => {
    setState({ history: [value], pointer: 0 });
  }, []);

  return useMemo(
    () => ({
      current,
      push,
      undo,
      redo,
      reset,
      canUndo,
      canRedo,
      undoCount,
      redoCount,
    }),
    [current, push, undo, redo, reset, canUndo, canRedo, undoCount, redoCount]
  );
}

/**
 * Specialized version for object states with partial updates
 */
export function useUndoObject<T extends Record<string, unknown>>(
  initialValue: T,
  options: UseUndoOptions = {}
): UseUndoReturn<T> & {
  /** Update specific fields while preserving undo history */
  update: (partial: Partial<T>) => void;
} {
  const undoState = useUndo(initialValue, options);

  const update = useCallback(
    (partial: Partial<T>) => {
      undoState.push({ ...undoState.current, ...partial });
    },
    [undoState]
  );

  return useMemo(
    () => ({
      ...undoState,
      update,
    }),
    [undoState, update]
  );
}
