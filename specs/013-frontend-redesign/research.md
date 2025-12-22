# Research: Front-End Redesign with Refined Intelligence Design System

**Feature**: 013-frontend-redesign
**Date**: 2025-12-21
**Purpose**: Resolve technical decisions and document best practices for the frontend redesign

---

## 1. Animation Library Selection

### Decision: Framer Motion

### Rationale
Framer Motion provides the best balance of power, developer experience, and bundle size for React applications. It offers:
- Declarative API that integrates naturally with React components
- Built-in gesture support (drag, hover, tap) for micro-interactions
- AnimatePresence for enter/exit animations (critical for list updates)
- Layout animations for smooth reordering (transaction list, match queue)
- SSR-friendly with automatic hydration handling

### Alternatives Considered

| Library | Rejected Because |
|---------|------------------|
| React Spring | More complex API, larger learning curve for team |
| GSAP | Imperative API doesn't fit React paradigm well |
| CSS-only | Insufficient for complex orchestration (staggered reveals, layout animations) |
| Motion One | Less mature React integration |

### Implementation Notes
- Use `motion` components sparingly on high-impact moments (page load, list updates)
- Prefer CSS transitions for simple hover/focus states
- Create animation presets in `lib/animations.ts` for consistency

---

## 2. Polling vs WebSocket for Real-Time Updates

### Decision: Polling with 30-second interval + Immediate updates for user actions

### Rationale
Based on clarification session, the hybrid approach provides:
- Simpler infrastructure (no WebSocket server required)
- Adequate freshness for background processing events (receipts, matches)
- Immediate feedback for user-initiated actions via TanStack Query mutation callbacks
- Reduced server load compared to persistent connections

### Implementation Pattern

```typescript
// Background polling for dashboard metrics
const { data: metrics } = useQuery({
  queryKey: ['dashboard', 'metrics'],
  queryFn: fetchDashboardMetrics,
  refetchInterval: 30_000, // 30 seconds
  refetchIntervalInBackground: false, // Pause when tab inactive
});

// Immediate update after user action
const uploadMutation = useMutation({
  mutationFn: uploadReceipt,
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: ['receipts'] });
    queryClient.invalidateQueries({ queryKey: ['dashboard'] });
  },
});
```

### Alternatives Considered

| Approach | Rejected Because |
|----------|------------------|
| Full WebSocket | Infrastructure complexity, not justified for expense management use case |
| Server-Sent Events | Limited browser support for reconnection, similar complexity to WS |
| Manual refresh only | Poor UX, users miss important updates |

---

## 3. Auto-Save with Undo Implementation

### Decision: Optimistic updates with local undo stack

### Rationale
Based on clarification session, auto-save with undo provides:
- Zero friction for common editing workflows
- Protection against accidental changes via undo
- Responsive feel with optimistic updates

### Implementation Pattern

```typescript
// Undo stack hook
function useUndoStack<T>(initialValue: T, options: { maxHistory?: number }) {
  const [history, setHistory] = useState<T[]>([initialValue]);
  const [pointer, setPointer] = useState(0);

  const push = (value: T) => {
    const newHistory = history.slice(0, pointer + 1);
    newHistory.push(value);
    if (newHistory.length > (options.maxHistory ?? 10)) {
      newHistory.shift();
    }
    setHistory(newHistory);
    setPointer(newHistory.length - 1);
  };

  const undo = () => pointer > 0 && setPointer(p => p - 1);
  const redo = () => pointer < history.length - 1 && setPointer(p => p + 1);

  return { value: history[pointer], push, undo, redo, canUndo: pointer > 0, canRedo: pointer < history.length - 1 };
}

// Auto-save with debounce
const debouncedSave = useDebouncedCallback(
  (value) => mutation.mutate(value),
  500 // 500ms debounce
);
```

### Alternatives Considered

| Approach | Rejected Because |
|----------|------------------|
| Explicit save button | Friction, user may lose work on navigation |
| Prompt on navigation | Interrupts flow, dated UX pattern |
| Server-side undo | Complexity, latency for simple undo operations |

---

## 4. File Upload Validation Strategy

### Decision: Client-side validation with graceful backend fallback

### Rationale
Based on clarification (JPEG, PNG, HEIC, PDF up to 20MB):
- Immediate feedback for obvious violations (wrong type, too large)
- Backend remains authoritative for security
- Progressive upload with cancel capability

### Implementation Pattern

```typescript
const ALLOWED_TYPES = ['image/jpeg', 'image/png', 'image/heic', 'application/pdf'];
const MAX_SIZE_BYTES = 20 * 1024 * 1024; // 20MB

function validateFile(file: File): ValidationResult {
  if (!ALLOWED_TYPES.includes(file.type)) {
    return { valid: false, error: 'File type not supported. Please upload JPEG, PNG, HEIC, or PDF.' };
  }
  if (file.size > MAX_SIZE_BYTES) {
    return { valid: false, error: `File too large. Maximum size is 20MB.` };
  }
  return { valid: true };
}
```

### HEIC Handling
HEIC files from iOS devices may report as `image/heic` or empty string. Implementation should:
- Check file extension as fallback
- Accept both `image/heic` and `image/heif` MIME types

---

## 5. Design Token Architecture

### Decision: CSS Custom Properties with TypeScript type safety

### Rationale
CSS Custom Properties provide:
- Runtime theme switching capability (future dark mode)
- Browser DevTools visibility for debugging
- Zero JavaScript overhead for styling
- Native CSS cascade behavior

TypeScript overlay ensures:
- Type-safe token usage in components
- Autocomplete in IDE
- Build-time validation

### Implementation Pattern

```typescript
// lib/design-tokens.ts
export const tokens = {
  colors: {
    // Refined Intelligence palette
    slate: {
      900: 'var(--color-slate-900)',
      800: 'var(--color-slate-800)',
      // ...
    },
    accent: {
      copper: 'var(--color-accent-copper)',
      emerald: 'var(--color-accent-emerald)',
    },
    confidence: {
      high: 'var(--color-confidence-high)',    // emerald
      medium: 'var(--color-confidence-medium)', // amber
      low: 'var(--color-confidence-low)',       // rose
    },
  },
  typography: {
    serif: 'var(--font-serif)',      // Display headings
    sans: 'var(--font-sans)',        // Body text
    mono: 'var(--font-mono)',        // Numbers, dates
  },
  spacing: {
    // 4px base unit
    1: 'var(--spacing-1)',  // 4px
    2: 'var(--spacing-2)',  // 8px
    // ...
  },
  animation: {
    duration: {
      fast: 'var(--duration-fast)',     // 150ms
      normal: 'var(--duration-normal)', // 300ms
      slow: 'var(--duration-slow)',     // 500ms
    },
    easing: {
      default: 'var(--easing-default)',
      spring: 'var(--easing-spring)',
    },
  },
} as const;
```

### CSS Variables (in global.css or Tailwind config)

```css
:root {
  /* Refined Intelligence Palette */
  --color-slate-950: #020617;
  --color-slate-900: #0f172a;
  --color-slate-800: #1e293b;
  --color-accent-copper: #b87333;
  --color-accent-emerald: #10b981;

  /* Confidence indicators */
  --color-confidence-high: #10b981;
  --color-confidence-medium: #f59e0b;
  --color-confidence-low: #f43f5e;

  /* Typography */
  --font-serif: 'Playfair Display', Georgia, serif;
  --font-sans: 'Plus Jakarta Sans', system-ui, sans-serif;
  --font-mono: 'JetBrains Mono', monospace;
}
```

---

## 6. Virtualized Scrolling for Large Datasets

### Decision: TanStack Virtual for transaction lists >100 items

### Rationale
The spec identifies 10,000+ transactions as an edge case. TanStack Virtual provides:
- Seamless integration with existing TanStack ecosystem
- Small bundle size (~3KB)
- Support for dynamic row heights (variable transaction descriptions)
- Maintained by same team as Router and Query

### Implementation Threshold
- < 100 items: Standard React rendering
- ≥ 100 items: Virtualized windowing
- Detection via `data.length` check before render

### Alternatives Considered

| Library | Rejected Because |
|---------|------------------|
| react-window | Less flexible for dynamic heights |
| react-virtualized | Larger bundle, more features than needed |
| Intersection Observer | More complex to implement correctly |

---

## 7. Keyboard Navigation Pattern

### Decision: Focus management with keyboard shortcut hints

### Rationale
Match review requires fast keyboard navigation (A = approve, R = reject). Implementation should:
- Capture key events at document level for global shortcuts
- Display shortcut hints in UI (e.g., "Press A to approve")
- Respect focus context (don't capture when in input fields)

### Implementation Pattern

```typescript
// hooks/ui/useKeyboardShortcuts.ts
function useKeyboardShortcuts(shortcuts: Record<string, () => void>, enabled = true) {
  useEffect(() => {
    if (!enabled) return;

    const handler = (e: KeyboardEvent) => {
      // Ignore when typing in inputs
      if (e.target instanceof HTMLInputElement || e.target instanceof HTMLTextAreaElement) {
        return;
      }

      const key = e.key.toLowerCase();
      if (shortcuts[key]) {
        e.preventDefault();
        shortcuts[key]();
      }
    };

    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, [shortcuts, enabled]);
}

// Usage in MatchReview
useKeyboardShortcuts({
  'a': () => approveMutation.mutate(currentMatch.id),
  'r': () => rejectMutation.mutate(currentMatch.id),
  'ArrowRight': () => nextMatch(),
  'ArrowLeft': () => previousMatch(),
});
```

---

## 8. Confidence Glow Animation

### Decision: CSS-only gradient animation with Tailwind

### Rationale
The "confidence glow" is a signature visual element. CSS-only implementation:
- Zero JavaScript overhead
- GPU-accelerated (opacity, transform)
- Works with Tailwind's utility classes

### Implementation Pattern

```css
@keyframes confidence-pulse {
  0%, 100% { opacity: 0.4; }
  50% { opacity: 0.7; }
}

.confidence-glow-high {
  background: linear-gradient(135deg,
    rgba(16, 185, 129, 0.2) 0%,
    rgba(16, 185, 129, 0.1) 50%,
    transparent 100%
  );
  animation: confidence-pulse 2s ease-in-out infinite;
}

.confidence-glow-medium {
  background: linear-gradient(135deg,
    rgba(245, 158, 11, 0.2) 0%,
    rgba(245, 158, 11, 0.1) 50%,
    transparent 100%
  );
  animation: confidence-pulse 2s ease-in-out infinite;
}

.confidence-glow-low {
  background: linear-gradient(135deg,
    rgba(244, 63, 94, 0.2) 0%,
    rgba(244, 63, 94, 0.1) 50%,
    transparent 100%
  );
  animation: confidence-pulse 2s ease-in-out infinite;
}
```

---

## Summary of Decisions

| Topic | Decision | Rationale |
|-------|----------|-----------|
| Animation Library | Framer Motion | Best React integration for complex animations |
| Real-time Updates | 30s polling + immediate user actions | Simpler than WebSocket, adequate freshness |
| Edit Behavior | Auto-save with undo stack | Zero friction, protection via undo |
| File Validation | Client + backend, 20MB limit | Immediate feedback, backend authority |
| Design Tokens | CSS Custom Properties + TS types | Runtime theming, type safety |
| Large Lists | TanStack Virtual (>100 items) | Consistent ecosystem, small bundle |
| Keyboard Nav | Document-level shortcuts | Fast match review workflow |
| Confidence Glow | CSS-only gradients | Zero JS overhead, GPU-accelerated |
