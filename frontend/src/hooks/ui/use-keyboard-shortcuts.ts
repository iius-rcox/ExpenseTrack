import { useEffect, useCallback, useRef } from 'react';

/**
 * Keyboard Shortcuts Hook for Match Review and Navigation
 *
 * Captures keyboard events at the document level for global shortcuts.
 * Automatically ignores events when the user is typing in form fields.
 *
 * @example
 * ```tsx
 * useKeyboardShortcuts({
 *   'a': () => approveMatch(),
 *   'r': () => rejectMatch(),
 *   'ArrowRight': () => nextMatch(),
 *   'ArrowLeft': () => previousMatch(),
 *   'Escape': () => closePanel(),
 * });
 * ```
 */

export type ShortcutHandler = () => void;
export type ShortcutMap = Record<string, ShortcutHandler>;

export interface UseKeyboardShortcutsOptions {
  /** Whether shortcuts are enabled (default: true) */
  enabled?: boolean;
  /** Whether to ignore shortcuts when in text inputs (default: true) */
  ignoreInputs?: boolean;
  /** Whether to prevent default browser behavior (default: true) */
  preventDefault?: boolean;
  /** Specific elements to exclude (in addition to inputs) */
  excludeSelectors?: string[];
}

/**
 * Check if an element should block keyboard shortcuts
 */
function isEditableElement(element: EventTarget | null): boolean {
  if (!element || !(element instanceof HTMLElement)) {
    return false;
  }

  // Standard form inputs
  if (
    element instanceof HTMLInputElement ||
    element instanceof HTMLTextAreaElement ||
    element instanceof HTMLSelectElement
  ) {
    return true;
  }

  // Content editable elements
  if (element.isContentEditable) {
    return true;
  }

  // Check for role="textbox" or similar
  const role = element.getAttribute('role');
  if (role === 'textbox' || role === 'searchbox') {
    return true;
  }

  return false;
}

export function useKeyboardShortcuts(
  shortcuts: ShortcutMap,
  options: UseKeyboardShortcutsOptions = {}
): void {
  const {
    enabled = true,
    ignoreInputs = true,
    preventDefault = true,
    excludeSelectors = [],
  } = options;

  // Use ref to avoid recreating the handler on every render
  const shortcutsRef = useRef(shortcuts);
  shortcutsRef.current = shortcuts;

  const handleKeyDown = useCallback(
    (event: KeyboardEvent) => {
      // Check if shortcuts are enabled
      if (!enabled) return;

      // Check if we should ignore this event (user is typing)
      if (ignoreInputs && isEditableElement(event.target)) {
        return;
      }

      // Check custom exclude selectors
      if (excludeSelectors.length > 0 && event.target instanceof Element) {
        for (const selector of excludeSelectors) {
          if (event.target.matches(selector)) {
            return;
          }
        }
      }

      // Build the key string (handles modifiers)
      const parts: string[] = [];
      if (event.ctrlKey) parts.push('Ctrl');
      if (event.altKey) parts.push('Alt');
      if (event.shiftKey) parts.push('Shift');
      if (event.metaKey) parts.push('Meta');

      // Normalize the key
      let key = event.key;
      // Handle common key aliases
      if (key === ' ') key = 'Space';
      if (key.length === 1) key = key.toLowerCase();

      parts.push(key);
      const shortcutKey = parts.join('+');

      // Also try just the key without modifiers (for simple shortcuts)
      const simpleKey = key;

      // Try to find a matching handler
      const handler =
        shortcutsRef.current[shortcutKey] || shortcutsRef.current[simpleKey];

      if (handler) {
        if (preventDefault) {
          event.preventDefault();
        }
        handler();
      }
    },
    [enabled, ignoreInputs, preventDefault, excludeSelectors]
  );

  useEffect(() => {
    if (!enabled) return;

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [enabled, handleKeyDown]);
}

/**
 * Hook variant that returns a status object for displaying shortcut hints
 */
export interface ShortcutInfo {
  key: string;
  description: string;
  action: ShortcutHandler;
}

export function useKeyboardShortcutsWithInfo(
  shortcuts: ShortcutInfo[],
  options: UseKeyboardShortcutsOptions = {}
): {
  /** List of registered shortcuts for display */
  shortcuts: Array<{ key: string; description: string }>;
} {
  // Convert to ShortcutMap
  const shortcutMap: ShortcutMap = {};
  for (const shortcut of shortcuts) {
    shortcutMap[shortcut.key] = shortcut.action;
  }

  useKeyboardShortcuts(shortcutMap, options);

  return {
    shortcuts: shortcuts.map(({ key, description }) => ({ key, description })),
  };
}
