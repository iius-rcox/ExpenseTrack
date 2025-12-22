# Quickstart: Front-End Redesign with Refined Intelligence Design System

**Feature**: 013-frontend-redesign
**Date**: 2025-12-21
**Purpose**: Step-by-step guide to get started with the frontend redesign implementation

---

## Prerequisites

### Development Environment

- **Node.js**: v20.x or later (LTS recommended)
- **npm**: v10.x or later
- **Editor**: VS Code with recommended extensions
- **Browser**: Chrome/Firefox with React DevTools

### Required VS Code Extensions

```json
{
  "recommendations": [
    "bradlc.vscode-tailwindcss",
    "dbaeumer.vscode-eslint",
    "esbenp.prettier-vscode",
    "styled-components.vscode-styled-components",
    "ms-playwright.playwright"
  ]
}
```

---

## 1. Project Setup

### 1.1 Clone and Install

```bash
# Navigate to frontend directory
cd frontend

# Install dependencies (including new ones)
npm install

# Install new dependencies for redesign
npm install framer-motion @tanstack/react-virtual use-debounce
```

### 1.2 Verify Setup

```bash
# Start development server
npm run dev

# Run type check
npm run type-check

# Run linter
npm run lint
```

The app should be available at `http://localhost:5173`

---

## 2. Design System Setup

### 2.1 Add Design Tokens

Create the design tokens file:

```bash
touch src/lib/design-tokens.ts
```

Add the Refined Intelligence palette (see `data-model.md` for full types):

```typescript
// src/lib/design-tokens.ts
export const tokens = {
  colors: {
    slate: {
      950: '#020617',
      900: '#0f172a',
      800: '#1e293b',
      700: '#334155',
      // ... full scale
    },
    accent: {
      copper: '#b87333',
      emerald: '#10b981',
      amber: '#f59e0b',
      rose: '#f43f5e',
    },
  },
  // ... typography, animation tokens
} as const;
```

### 2.2 Update Tailwind Config

Extend `tailwind.config.ts` with custom theme:

```typescript
// tailwind.config.ts
export default {
  theme: {
    extend: {
      colors: {
        accent: {
          copper: '#b87333',
          'copper-light': '#d4a574',
          'copper-dark': '#8b5a2b',
        },
        confidence: {
          high: '#10b981',
          medium: '#f59e0b',
          low: '#f43f5e',
        },
      },
      fontFamily: {
        serif: ['Playfair Display', 'Georgia', 'serif'],
        sans: ['Plus Jakarta Sans', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'monospace'],
      },
      animation: {
        'confidence-pulse': 'confidence-pulse 2s ease-in-out infinite',
      },
      keyframes: {
        'confidence-pulse': {
          '0%, 100%': { opacity: '0.4' },
          '50%': { opacity: '0.7' },
        },
      },
    },
  },
};
```

### 2.3 Add Custom Fonts

Update `index.html` to include fonts:

```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Playfair+Display:wght@400;500;600;700&family=Plus+Jakarta+Sans:wght@300;400;500;600;700&family=JetBrains+Mono:wght@400;500&display=swap" rel="stylesheet">
```

---

## 3. Component Structure Setup

### 3.1 Create Component Directories

```bash
# Create new component directories
mkdir -p src/components/dashboard
mkdir -p src/components/transactions
mkdir -p src/components/analytics
mkdir -p src/components/design-system
mkdir -p src/hooks/ui
```

### 3.2 Create Base Components

Create the confidence indicator (signature component):

```bash
touch src/components/design-system/confidence-indicator.tsx
```

```typescript
// src/components/design-system/confidence-indicator.tsx
import { cn } from '@/lib/utils';

export type ConfidenceLevel = 'high' | 'medium' | 'low';

interface ConfidenceIndicatorProps {
  score: number;
  showLabel?: boolean;
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

function getConfidenceLevel(score: number): ConfidenceLevel {
  if (score >= 0.9) return 'high';
  if (score >= 0.7) return 'medium';
  return 'low';
}

export function ConfidenceIndicator({
  score,
  showLabel = false,
  size = 'md',
  className,
}: ConfidenceIndicatorProps) {
  const level = getConfidenceLevel(score);
  const dots = Math.round(score * 5);

  return (
    <div className={cn('flex items-center gap-1.5', className)}>
      <div className="flex gap-0.5">
        {[...Array(5)].map((_, i) => (
          <div
            key={i}
            className={cn(
              'rounded-full transition-colors',
              size === 'sm' && 'w-1 h-1',
              size === 'md' && 'w-1.5 h-1.5',
              size === 'lg' && 'w-2 h-2',
              i < dots ? `bg-confidence-${level}` : 'bg-slate-700'
            )}
          />
        ))}
      </div>
      {showLabel && (
        <span className="text-xs text-slate-500">
          {Math.round(score * 100)}%
        </span>
      )}
    </div>
  );
}
```

---

## 4. Animation Setup

### 4.1 Create Animation Presets

```bash
touch src/lib/animations.ts
```

```typescript
// src/lib/animations.ts
import { type Variants } from 'framer-motion';

export const fadeIn: Variants = {
  hidden: { opacity: 0 },
  visible: { opacity: 1, transition: { duration: 0.3 } },
};

export const slideUp: Variants = {
  hidden: { opacity: 0, y: 8 },
  visible: { opacity: 1, y: 0, transition: { duration: 0.3 } },
};

export const staggerChildren: Variants = {
  visible: {
    transition: {
      staggerChildren: 0.05,
    },
  },
};

export const confidenceGlow = {
  initial: { opacity: 0 },
  animate: {
    opacity: [0.4, 0.7, 0.4],
    transition: { duration: 2, repeat: Infinity, ease: 'easeInOut' },
  },
};
```

---

## 5. Hook Setup

### 5.1 Create Undo Hook

```bash
touch src/hooks/ui/use-undo.ts
```

```typescript
// src/hooks/ui/use-undo.ts
import { useState, useCallback } from 'react';

interface UseUndoOptions {
  maxHistory?: number;
}

export function useUndo<T>(initialValue: T, options: UseUndoOptions = {}) {
  const { maxHistory = 10 } = options;
  const [history, setHistory] = useState<T[]>([initialValue]);
  const [pointer, setPointer] = useState(0);

  const current = history[pointer];
  const canUndo = pointer > 0;
  const canRedo = pointer < history.length - 1;

  const push = useCallback((value: T) => {
    setHistory((prev) => {
      const newHistory = prev.slice(0, pointer + 1);
      newHistory.push(value);
      if (newHistory.length > maxHistory) {
        newHistory.shift();
        return newHistory;
      }
      return newHistory;
    });
    setPointer((p) => Math.min(p + 1, maxHistory - 1));
  }, [pointer, maxHistory]);

  const undo = useCallback(() => {
    if (canUndo) setPointer((p) => p - 1);
  }, [canUndo]);

  const redo = useCallback(() => {
    if (canRedo) setPointer((p) => p + 1);
  }, [canRedo]);

  const reset = useCallback((value: T) => {
    setHistory([value]);
    setPointer(0);
  }, []);

  return { current, push, undo, redo, reset, canUndo, canRedo };
}
```

### 5.2 Create Keyboard Shortcuts Hook

```bash
touch src/hooks/ui/use-keyboard-shortcuts.ts
```

```typescript
// src/hooks/ui/use-keyboard-shortcuts.ts
import { useEffect } from 'react';

type ShortcutMap = Record<string, () => void>;

export function useKeyboardShortcuts(shortcuts: ShortcutMap, enabled = true) {
  useEffect(() => {
    if (!enabled) return;

    const handler = (e: KeyboardEvent) => {
      // Ignore when typing in inputs
      if (
        e.target instanceof HTMLInputElement ||
        e.target instanceof HTMLTextAreaElement
      ) {
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
```

---

## 6. Testing Setup

### 6.1 Install Test Dependencies

```bash
npm install -D vitest @testing-library/react @testing-library/jest-dom jsdom @playwright/test
```

### 6.2 Configure Vitest

```typescript
// vitest.config.ts
import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: ['./tests/setup.ts'],
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
});
```

### 6.3 Create Test Setup

```bash
mkdir -p tests
touch tests/setup.ts
```

```typescript
// tests/setup.ts
import '@testing-library/jest-dom';
```

---

## 7. Development Workflow

### 7.1 Start Development

```bash
# Terminal 1: Start dev server
npm run dev

# Terminal 2: Run type checking in watch mode
npm run type-check -- --watch

# Terminal 3: Run tests in watch mode
npm run test -- --watch
```

### 7.2 Component Development Order

Implement components in this order (aligns with user story priorities):

1. **Design System Foundation** (Week 1)
   - `design-system/confidence-indicator.tsx`
   - `design-system/stat-card.tsx`
   - `design-system/empty-state.tsx`
   - `design-system/loading-skeleton.tsx`

2. **Dashboard (P1)** (Week 2)
   - `dashboard/dashboard-layout.tsx`
   - `dashboard/metrics-row.tsx`
   - `dashboard/expense-stream.tsx`
   - `dashboard/action-queue.tsx`
   - `dashboard/category-breakdown.tsx`

3. **Receipt Intelligence (P1)** (Week 3)
   - `receipts/receipt-intelligence-panel.tsx`
   - `receipts/extracted-field.tsx`
   - `receipts/batch-upload-queue.tsx`

4. **Transaction Explorer (P2)** (Week 4)
   - `transactions/transaction-grid.tsx`
   - `transactions/transaction-row.tsx`
   - `transactions/filter-panel.tsx`
   - `transactions/bulk-actions-bar.tsx`

5. **Match Review (P2)** (Week 5)
   - `matching/match-review-workspace.tsx`
   - `matching/comparison-view.tsx`
   - `matching/batch-review-panel.tsx`

6. **Analytics (P3)** (Week 6)
   - `analytics/spending-trend-chart.tsx`
   - `analytics/category-treemap.tsx`
   - `analytics/subscription-list.tsx`

---

## 8. Verification Checklist

Before proceeding to implementation:

- [ ] `npm run dev` starts without errors
- [ ] `npm run type-check` passes
- [ ] `npm run lint` passes
- [ ] Design tokens file created
- [ ] Tailwind config extended with custom theme
- [ ] Custom fonts loading correctly
- [ ] Component directories created
- [ ] Animation presets defined
- [ ] Core hooks implemented
- [ ] Test infrastructure configured

---

## Quick Reference

### Key Files

| File | Purpose |
|------|---------|
| `src/lib/design-tokens.ts` | Color, typography, spacing values |
| `src/lib/animations.ts` | Framer Motion presets |
| `src/hooks/ui/use-undo.ts` | Undo stack for auto-save |
| `src/hooks/ui/use-keyboard-shortcuts.ts` | Global keyboard shortcuts |
| `tailwind.config.ts` | Theme extensions |

### Key Commands

| Command | Purpose |
|---------|---------|
| `npm run dev` | Start development server |
| `npm run build` | Production build |
| `npm run type-check` | TypeScript validation |
| `npm run lint` | ESLint check |
| `npm run test` | Run unit tests |
| `npx playwright test` | Run E2E tests |

### API Base URL

- **Development**: `http://localhost:5000/api`
- **Staging**: `https://staging.expense.ii-us.com/api`
- **Production**: `https://expense.ii-us.com/api`

---

## Next Steps

After completing setup, proceed to `/speckit.tasks` to generate the task breakdown for implementation.
