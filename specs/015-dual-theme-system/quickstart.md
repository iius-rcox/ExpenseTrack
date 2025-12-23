# Quickstart: Dual Theme System

**Feature Branch**: `015-dual-theme-system`
**Created**: 2025-12-23
**Estimated Time**: 2-3 hours

## Prerequisites

- [ ] Feature branch created: `git checkout -b 015-dual-theme-system`
- [ ] Frontend dev server runs: `cd frontend && npm run dev`
- [ ] Familiar with [spec.md](./spec.md) requirements
- [ ] Reviewed [data-model.md](./data-model.md) token definitions

## Implementation Order

```
1. ThemeProvider Setup (15 min)
      ↓
2. Update globals.css (30 min)
      ↓
3. Replace design-tokens.ts (15 min)
      ↓
4. Create ThemeToggle component (20 min)
      ↓
5. Add glassmorphism utilities (20 min)
      ↓
6. Update components using old tokens (30 min)
      ↓
7. Visual testing (30 min)
      ↓
8. Playwright E2E tests (20 min)
```

## Step 1: ThemeProvider Setup

### 1.1 Create Theme Provider

```bash
# Create the provider file
touch frontend/src/providers/theme-provider.tsx
```

**File: `frontend/src/providers/theme-provider.tsx`**

```tsx
"use client"

import { ThemeProvider as NextThemesProvider } from "next-themes"
import type { ThemeProviderProps } from "next-themes"

export function ThemeProvider({ children, ...props }: ThemeProviderProps) {
  return (
    <NextThemesProvider
      attribute="class"
      defaultTheme="system"
      enableSystem
      storageKey="expenseflow-theme"
      disableTransitionOnChange={false}
      {...props}
    >
      {children}
    </NextThemesProvider>
  )
}
```

### 1.2 Wrap App with ThemeProvider

**File: `frontend/src/routes/__root.tsx`**

Add the ThemeProvider import and wrap the content:

```tsx
import { ThemeProvider } from "@/providers/theme-provider"

function RootComponent() {
  return (
    <ThemeProvider>
      <ErrorBoundary>
        <Outlet />
        <Toaster position="top-right" richColors closeButton />
        {import.meta.env.DEV && (
          <>
            <TanStackRouterDevtools position="bottom-right" />
            <ReactQueryDevtools initialIsOpen={false} buttonPosition="bottom-left" />
          </>
        )}
      </ErrorBoundary>
    </ThemeProvider>
  )
}
```

### 1.3 Add Flash Prevention Script

**File: `frontend/index.html`**

Add before closing `</head>`:

```html
<script>
  (function() {
    const theme = localStorage.getItem('expenseflow-theme');
    if (theme === 'dark' || (!theme && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
      document.documentElement.classList.add('dark');
    }
  })();
</script>
```

## Step 2: Update globals.css

Replace the entire file with the new theme tokens:

**File: `frontend/src/globals.css`**

```css
@import "tailwindcss";

@theme {
  /* Colors - Luxury Minimalist (Light) / Dark Cyber (Dark) */
  --color-background: hsl(var(--background));
  --color-foreground: hsl(var(--foreground));
  --color-card: hsl(var(--card));
  --color-card-foreground: hsl(var(--card-foreground));
  --color-popover: hsl(var(--popover));
  --color-popover-foreground: hsl(var(--popover-foreground));
  --color-primary: hsl(var(--primary));
  --color-primary-foreground: hsl(var(--primary-foreground));
  --color-secondary: hsl(var(--secondary));
  --color-secondary-foreground: hsl(var(--secondary-foreground));
  --color-muted: hsl(var(--muted));
  --color-muted-foreground: hsl(var(--muted-foreground));
  --color-accent: hsl(var(--accent));
  --color-accent-foreground: hsl(var(--accent-foreground));
  --color-destructive: hsl(var(--destructive));
  --color-destructive-foreground: hsl(var(--destructive-foreground));
  --color-border: hsl(var(--border));
  --color-input: hsl(var(--input));
  --color-ring: hsl(var(--ring));

  /* Chart colors */
  --color-chart-1: hsl(var(--chart-1));
  --color-chart-2: hsl(var(--chart-2));
  --color-chart-3: hsl(var(--chart-3));
  --color-chart-4: hsl(var(--chart-4));
  --color-chart-5: hsl(var(--chart-5));

  /* Sidebar */
  --color-sidebar-background: hsl(var(--sidebar-background));
  --color-sidebar-foreground: hsl(var(--sidebar-foreground));
  --color-sidebar-primary: hsl(var(--sidebar-primary));
  --color-sidebar-primary-foreground: hsl(var(--sidebar-primary-foreground));
  --color-sidebar-accent: hsl(var(--sidebar-accent));
  --color-sidebar-accent-foreground: hsl(var(--sidebar-accent-foreground));
  --color-sidebar-border: hsl(var(--sidebar-border));
  --color-sidebar-ring: hsl(var(--sidebar-ring));

  /* Radius */
  --radius-lg: 0.5rem;
  --radius-md: calc(var(--radius-lg) - 2px);
  --radius-sm: calc(var(--radius-lg) - 4px);
}

/* Light Mode: Luxury Minimalist */
:root {
  /* Backgrounds */
  --background: 60 11% 97%;
  --foreground: 0 0% 10%;

  /* Cards & Popovers */
  --card: 0 0% 100%;
  --card-foreground: 0 0% 10%;
  --popover: 0 0% 100%;
  --popover-foreground: 0 0% 10%;

  /* Primary (Emerald) */
  --primary: 158 36% 28%;
  --primary-foreground: 0 0% 100%;

  /* Secondary & Muted */
  --secondary: 60 8% 96%;
  --secondary-foreground: 0 0% 10%;
  --muted: 60 8% 96%;
  --muted-foreground: 0 0% 40%;

  /* Accent (Lighter Emerald) */
  --accent: 154 32% 43%;
  --accent-foreground: 0 0% 100%;

  /* Destructive */
  --destructive: 0 72% 51%;
  --destructive-foreground: 0 0% 100%;

  /* Borders & Inputs */
  --border: 0 0% 94%;
  --input: 0 0% 94%;
  --ring: 158 36% 28%;
  --radius: 0.5rem;

  /* Chart Colors (Emerald-based) */
  --chart-1: 158 36% 28%;
  --chart-2: 154 32% 43%;
  --chart-3: 158 36% 23%;
  --chart-4: 43 74% 66%;
  --chart-5: 27 87% 67%;

  /* Sidebar */
  --sidebar-background: 60 11% 97%;
  --sidebar-foreground: 0 0% 40%;
  --sidebar-primary: 158 36% 28%;
  --sidebar-primary-foreground: 0 0% 100%;
  --sidebar-accent: 158 36% 28% / 8%;
  --sidebar-accent-foreground: 158 36% 28%;
  --sidebar-border: 0 0% 94%;
  --sidebar-ring: 158 36% 28%;

  /* Shadows (Emerald-tinted) */
  --shadow-sm: 0 2px 8px rgba(0, 0, 0, 0.04);
  --shadow-md: 0 8px 24px rgba(45, 95, 79, 0.1);
  --shadow-lg: 0 12px 36px rgba(45, 95, 79, 0.15);
  --shadow-hover: 0 8px 24px rgba(45, 95, 79, 0.1);

  /* Confidence Colors (Semantic) */
  --confidence-high: #10b981;
  --confidence-medium: #f59e0b;
  --confidence-low: #f43f5e;

  /* Animation */
  --duration-instant: 0ms;
  --duration-fast: 150ms;
  --duration-normal: 300ms;
  --duration-slow: 500ms;
  --easing-default: cubic-bezier(0.4, 0, 0.2, 1);
  --easing-spring: cubic-bezier(0.34, 1.56, 0.64, 1);
  --easing-bounce: cubic-bezier(0.68, -0.55, 0.265, 1.55);
}

/* Dark Mode: Dark Cyber */
.dark {
  /* Backgrounds */
  --background: 210 25% 8%;
  --foreground: 0 0% 88%;

  /* Cards & Popovers */
  --card: 224 30% 17%;
  --card-foreground: 0 0% 88%;
  --popover: 224 30% 17%;
  --popover-foreground: 0 0% 88%;

  /* Primary (Cyan) */
  --primary: 187 100% 42%;
  --primary-foreground: 210 25% 8%;

  /* Secondary & Muted */
  --secondary: 225 27% 14%;
  --secondary-foreground: 0 0% 88%;
  --muted: 225 27% 14%;
  --muted-foreground: 0 0% 50%;

  /* Accent (Electric Blue) */
  --accent: 210 79% 51%;
  --accent-foreground: 0 0% 100%;

  /* Destructive */
  --destructive: 0 62% 30%;
  --destructive-foreground: 0 0% 100%;

  /* Borders & Inputs */
  --border: 0 0% 100% / 10%;
  --input: 0 0% 100% / 10%;
  --ring: 187 100% 42%;

  /* Chart Colors (Cyan-based) */
  --chart-1: 187 100% 42%;
  --chart-2: 210 79% 51%;
  --chart-3: 282 68% 38%;
  --chart-4: 187 60% 55%;
  --chart-5: 210 60% 60%;

  /* Sidebar */
  --sidebar-background: 210 25% 8%;
  --sidebar-foreground: 0 0% 69%;
  --sidebar-primary: 187 100% 42%;
  --sidebar-primary-foreground: 0 0% 100%;
  --sidebar-accent: 187 100% 42% / 12%;
  --sidebar-accent-foreground: 187 100% 42%;
  --sidebar-border: 0 0% 100% / 10%;
  --sidebar-ring: 187 100% 42%;

  /* Shadows (Cyan-tinted) */
  --shadow-sm: 0 8px 32px rgba(0, 0, 0, 0.3);
  --shadow-md: 0 12px 48px rgba(0, 188, 212, 0.2);
  --shadow-lg: 0 16px 64px rgba(0, 0, 0, 0.5);
  --shadow-hover: 0 12px 48px rgba(0, 188, 212, 0.2);

  /* Glassmorphism */
  --glass-bg: 0 0% 100% / 8%;
  --glass-border: 0 0% 100% / 10%;
  --glass-blur: 10px;
}

/* Base Styles */
* {
  border-color: hsl(var(--border));
}

body {
  background-color: hsl(var(--background));
  color: hsl(var(--foreground));
  transition: background-color var(--duration-normal) var(--easing-default),
              color var(--duration-normal) var(--easing-default);
}

/* Glassmorphism Utility (Dark Mode) */
.glass {
  background: hsl(var(--card));
}

.dark .glass {
  background: hsl(var(--glass-bg));
  backdrop-filter: blur(var(--glass-blur));
  -webkit-backdrop-filter: blur(var(--glass-blur));
  border-color: hsl(var(--glass-border));
}

/* Fallback for browsers without backdrop-filter */
@supports not (backdrop-filter: blur(10px)) {
  .dark .glass {
    background: hsl(224 30% 17% / 95%);
  }
}

/* Gradient Text Utility (Dark Mode) */
.gradient-text {
  color: hsl(var(--primary));
}

.dark .gradient-text {
  background: linear-gradient(135deg, #00bcd4 0%, #1e88e5 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

/* Stat Card Shine Animation */
.stat-card-shine {
  position: relative;
  overflow: hidden;
}

.stat-card-shine::before {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(0, 188, 212, 0.1), transparent);
  transition: left 0.6s ease;
  pointer-events: none;
}

.dark .stat-card-shine:hover::before {
  left: 100%;
}
```

## Step 3: Replace design-tokens.ts

**File: `frontend/src/lib/design-tokens.ts`**

```typescript
/**
 * Design Tokens for the ExpenseFlow Dual Theme System
 *
 * Light Mode: Luxury Minimalist (Emerald #2d5f4f)
 * Dark Mode: Dark Cyber (Cyan #00bcd4)
 *
 * Note: Color values are now defined in CSS variables (globals.css).
 * This file provides TypeScript utilities for confidence levels and animations.
 */

export type Theme = 'light' | 'dark' | 'system';

export interface AnimationTokens {
  duration: {
    instant: number;
    fast: number;
    normal: number;
    slow: number;
  };
  easing: {
    default: string;
    spring: string;
    bounce: string;
  };
}

export interface ConfidenceColors {
  high: string;
  medium: string;
  low: string;
}

// Confidence level thresholds
export const CONFIDENCE_THRESHOLDS = {
  HIGH: 0.9,
  MEDIUM: 0.7,
} as const;

export type ConfidenceLevel = 'high' | 'medium' | 'low';

export function getConfidenceLevel(score: number): ConfidenceLevel {
  if (score >= CONFIDENCE_THRESHOLDS.HIGH) return 'high';
  if (score >= CONFIDENCE_THRESHOLDS.MEDIUM) return 'medium';
  return 'low';
}

// Animation tokens (shared between themes)
export const animation: AnimationTokens = {
  duration: {
    instant: 0,
    fast: 150,
    normal: 300,
    slow: 500,
  },
  easing: {
    default: 'cubic-bezier(0.4, 0, 0.2, 1)',
    spring: 'cubic-bezier(0.34, 1.56, 0.64, 1)',
    bounce: 'cubic-bezier(0.68, -0.55, 0.265, 1.55)',
  },
};

// Confidence colors (semantic, theme-independent)
export const confidenceColors: ConfidenceColors = {
  high: '#10b981',
  medium: '#f59e0b',
  low: '#f43f5e',
};

export function getConfidenceColor(score: number): string {
  const level = getConfidenceLevel(score);
  return confidenceColors[level];
}
```

## Step 4: Create ThemeToggle Component

**File: `frontend/src/components/theme-toggle.tsx`**

```tsx
"use client"

import { Moon, Sun, Monitor } from "lucide-react"
import { useTheme } from "next-themes"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"

export function ThemeToggle() {
  const { setTheme, theme } = useTheme()

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" aria-label="Toggle theme">
          <Sun className="h-5 w-5 rotate-0 scale-100 transition-all dark:-rotate-90 dark:scale-0" />
          <Moon className="absolute h-5 w-5 rotate-90 scale-0 transition-all dark:rotate-0 dark:scale-100" />
          <span className="sr-only">Toggle theme</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={() => setTheme("light")}>
          <Sun className="mr-2 h-4 w-4" />
          <span>Light</span>
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => setTheme("dark")}>
          <Moon className="mr-2 h-4 w-4" />
          <span>Dark</span>
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => setTheme("system")}>
          <Monitor className="mr-2 h-4 w-4" />
          <span>System</span>
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
```

## Step 5: Add ThemeToggle to Navigation

Locate the navigation/header component and add the ThemeToggle:

```tsx
import { ThemeToggle } from "@/components/theme-toggle"

// In the header/nav component:
<div className="flex items-center gap-2">
  <ThemeToggle />
  {/* other nav items */}
</div>
```

## Step 6: Update Components Using Old Tokens

Search for and update any components using the old design tokens:

```bash
# Find files importing old tokens
grep -r "colors.slate" frontend/src/
grep -r "colors.accent.copper" frontend/src/
grep -r "from.*design-tokens" frontend/src/
```

Replace with CSS variable usage:
- `colors.slate.800` → `text-foreground` or `hsl(var(--foreground))`
- `colors.accent.copper` → `text-primary` or `hsl(var(--primary))`

## Step 7: Visual Testing

### Manual Verification Checklist

**Light Mode (Luxury Minimalist)**:
- [ ] Background is off-white (#fafaf8)
- [ ] Cards are pure white (#ffffff)
- [ ] Primary accent is deep emerald (#2d5f4f)
- [ ] Hover states show lighter emerald
- [ ] Shadows have subtle emerald tint

**Dark Mode (Dark Cyber)**:
- [ ] Background is dark navy (#0f1419)
- [ ] Cards have glassmorphism effect
- [ ] Primary accent is bright cyan (#00bcd4)
- [ ] Stat values show gradient text
- [ ] Card hover shows shine animation

**Theme Switching**:
- [ ] Toggle is visible in navigation
- [ ] Transition is smooth (300ms)
- [ ] No flash of wrong colors on load
- [ ] Preference persists after refresh
- [ ] System preference detected on first visit

## Step 8: Playwright E2E Tests

**File: `frontend/tests/e2e/theme-toggle.spec.ts`**

```typescript
import { test, expect } from '@playwright/test'

test.describe('Theme Toggle', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/')
  })

  test('should toggle from light to dark mode', async ({ page }) => {
    // Start in light mode
    await page.evaluate(() => localStorage.setItem('expenseflow-theme', 'light'))
    await page.reload()

    // Click theme toggle
    await page.getByRole('button', { name: /toggle theme/i }).click()
    await page.getByRole('menuitem', { name: /dark/i }).click()

    // Verify dark class is applied
    await expect(page.locator('html')).toHaveClass(/dark/)
  })

  test('should persist theme preference', async ({ page }) => {
    // Set dark mode
    await page.getByRole('button', { name: /toggle theme/i }).click()
    await page.getByRole('menuitem', { name: /dark/i }).click()

    // Reload page
    await page.reload()

    // Verify still in dark mode
    await expect(page.locator('html')).toHaveClass(/dark/)
  })

  test('should complete transition within 500ms', async ({ page }) => {
    const startTime = Date.now()

    await page.getByRole('button', { name: /toggle theme/i }).click()
    await page.getByRole('menuitem', { name: /dark/i }).click()

    // Wait for transition to complete
    await page.waitForFunction(() => {
      return document.documentElement.classList.contains('dark')
    })

    const endTime = Date.now()
    expect(endTime - startTime).toBeLessThan(500)
  })
})
```

## Validation

Run these commands to validate the implementation:

```bash
# Type check
cd frontend && npm run typecheck

# Lint
npm run lint

# Unit tests
npm run test

# E2E tests
npm run test:e2e

# Visual check
npm run dev
# Then manually verify both themes
```

## Troubleshooting

### Theme flash on load
Ensure the flash prevention script is in `index.html` before `</head>`.

### Glassmorphism not working
Check browser supports `backdrop-filter`. The fallback should apply automatically.

### Components not updating
Ensure components use CSS variables (`text-primary`, `bg-card`) not hard-coded colors.

### next-themes hydration error
Wrap theme-dependent content in a mounted check or use the `suppressHydrationWarning` prop.
