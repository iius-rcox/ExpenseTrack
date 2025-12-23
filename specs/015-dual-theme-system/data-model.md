# Data Model: Dual Theme System

**Feature Branch**: `015-dual-theme-system`
**Created**: 2025-12-23
**Type**: Design Tokens (CSS Variables + TypeScript Types)

## Overview

This document defines the design token structure for the dual-theme system. Unlike database entities, these are CSS custom properties and TypeScript interfaces that govern the visual language of the application.

## Theme Token Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Theme Token Layers                        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────────┐    ┌─────────────────┐                 │
│  │   Light Mode    │    │    Dark Mode    │                 │
│  │ (Luxury Minimal)│    │  (Dark Cyber)   │                 │
│  └────────┬────────┘    └────────┬────────┘                 │
│           │                      │                          │
│           ▼                      ▼                          │
│  ┌──────────────────────────────────────────┐              │
│  │           Semantic Tokens                 │              │
│  │  (--background, --primary, --accent)     │              │
│  └────────────────────┬─────────────────────┘              │
│                       │                                     │
│                       ▼                                     │
│  ┌──────────────────────────────────────────┐              │
│  │         Component Tokens                  │              │
│  │  (--card, --sidebar-*, --chart-*)        │              │
│  └────────────────────┬─────────────────────┘              │
│                       │                                     │
│                       ▼                                     │
│  ┌──────────────────────────────────────────┐              │
│  │          shadcn Components               │              │
│  │       (Button, Card, Input, etc.)        │              │
│  └──────────────────────────────────────────┘              │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

## CSS Variable Tokens

### Core Semantic Tokens

These map directly to shadcn/ui expectations.

#### Light Mode (:root)

```css
:root {
  /* Backgrounds */
  --background: 60 11% 97%;           /* #fafaf8 - off-white */
  --foreground: 0 0% 10%;             /* #1a1a1a - near-black */

  /* Cards & Popovers */
  --card: 0 0% 100%;                  /* #ffffff - pure white */
  --card-foreground: 0 0% 10%;        /* #1a1a1a */
  --popover: 0 0% 100%;               /* #ffffff */
  --popover-foreground: 0 0% 10%;     /* #1a1a1a */

  /* Primary Action Color (Emerald) */
  --primary: 158 36% 28%;             /* #2d5f4f - deep emerald */
  --primary-foreground: 0 0% 100%;    /* #ffffff */

  /* Secondary & Muted */
  --secondary: 60 8% 96%;             /* #f5f5f3 - light off-white */
  --secondary-foreground: 0 0% 10%;   /* #1a1a1a */
  --muted: 60 8% 96%;                 /* #f5f5f3 */
  --muted-foreground: 0 0% 40%;       /* #666666 - medium gray */

  /* Accent (Lighter Emerald for Hover) */
  --accent: 154 32% 43%;              /* #4a8f75 */
  --accent-foreground: 0 0% 100%;     /* #ffffff */

  /* Destructive */
  --destructive: 0 72% 51%;           /* #d32f2f - red */
  --destructive-foreground: 0 0% 100%;

  /* Borders & Inputs */
  --border: 0 0% 94%;                 /* #f0f0f0 - subtle */
  --input: 0 0% 94%;                  /* #f0f0f0 */
  --ring: 158 36% 28%;                /* #2d5f4f - emerald focus ring */

  /* Radius */
  --radius: 0.5rem;
}
```

#### Dark Mode (.dark)

```css
.dark {
  /* Backgrounds */
  --background: 210 25% 8%;           /* #0f1419 - dark navy-black */
  --foreground: 0 0% 88%;             /* #e0e0e0 - light gray */

  /* Cards & Popovers (Glassmorphism base) */
  --card: 224 30% 17%;                /* #1e2438 */
  --card-foreground: 0 0% 88%;        /* #e0e0e0 */
  --popover: 224 30% 17%;             /* #1e2438 */
  --popover-foreground: 0 0% 88%;     /* #e0e0e0 */

  /* Primary Action Color (Cyan) */
  --primary: 187 100% 42%;            /* #00bcd4 - bright cyan */
  --primary-foreground: 210 25% 8%;   /* #0f1419 - dark for contrast */

  /* Secondary & Muted */
  --secondary: 225 27% 14%;           /* #1a1f2e - dark navy */
  --secondary-foreground: 0 0% 88%;   /* #e0e0e0 */
  --muted: 225 27% 14%;               /* #1a1f2e */
  --muted-foreground: 0 0% 50%;       /* #808080 - dark gray */

  /* Accent (Electric Blue for Gradients) */
  --accent: 210 79% 51%;              /* #1e88e5 */
  --accent-foreground: 0 0% 100%;     /* #ffffff */

  /* Destructive */
  --destructive: 0 62% 30%;           /* Darker red for dark mode */
  --destructive-foreground: 0 0% 100%;

  /* Borders & Inputs */
  --border: 0 0% 100% / 10%;          /* rgba(255,255,255,0.1) */
  --input: 0 0% 100% / 10%;
  --ring: 187 100% 42%;               /* #00bcd4 - cyan focus ring */
}
```

### Chart Color Tokens

```css
:root {
  /* Light mode - Emerald-based palette */
  --chart-1: 158 36% 28%;             /* Emerald primary */
  --chart-2: 154 32% 43%;             /* Emerald light */
  --chart-3: 158 36% 23%;             /* Emerald dark */
  --chart-4: 43 74% 66%;              /* Warm gold */
  --chart-5: 27 87% 67%;              /* Warm orange */
}

.dark {
  /* Dark mode - Cyan/Blue palette */
  --chart-1: 187 100% 42%;            /* Cyan primary */
  --chart-2: 210 79% 51%;             /* Electric blue */
  --chart-3: 282 68% 38%;             /* Purple accent */
  --chart-4: 187 60% 55%;             /* Cyan light */
  --chart-5: 210 60% 60%;             /* Blue light */
}
```

### Sidebar Tokens

```css
:root {
  /* Light mode sidebar */
  --sidebar-background: 60 11% 97%;   /* Same as main background */
  --sidebar-foreground: 0 0% 40%;     /* #666666 */
  --sidebar-primary: 158 36% 28%;     /* Emerald */
  --sidebar-primary-foreground: 0 0% 100%;
  --sidebar-accent: 158 36% 28% / 8%; /* Emerald tint */
  --sidebar-accent-foreground: 158 36% 28%;
  --sidebar-border: 0 0% 94%;         /* #f0f0f0 */
  --sidebar-ring: 158 36% 28%;        /* Emerald */
}

.dark {
  /* Dark mode sidebar */
  --sidebar-background: 210 25% 8%;   /* #0f1419 */
  --sidebar-foreground: 0 0% 69%;     /* #b0b0b0 */
  --sidebar-primary: 187 100% 42%;    /* Cyan */
  --sidebar-primary-foreground: 0 0% 100%;
  --sidebar-accent: 187 100% 42% / 12%; /* Cyan tint */
  --sidebar-accent-foreground: 187 100% 42%;
  --sidebar-border: 0 0% 100% / 10%;
  --sidebar-ring: 187 100% 42%;       /* Cyan */
}
```

## Custom Theme Tokens

### Glassmorphism Tokens (Dark Mode Only)

```css
.dark {
  --glass-bg: 0 0% 100% / 8%;         /* rgba(255,255,255,0.08) */
  --glass-border: 0 0% 100% / 10%;    /* rgba(255,255,255,0.1) */
  --glass-blur: 10px;
}
```

### Gradient Tokens

```css
:root {
  --gradient-primary: linear-gradient(135deg, hsl(158 36% 28%), hsl(154 32% 43%));
}

.dark {
  --gradient-primary: linear-gradient(135deg, hsl(187 100% 42%), hsl(210 79% 51%));
  --gradient-text: linear-gradient(135deg, #00bcd4 0%, #1e88e5 100%);
}
```

### Shadow Tokens

```css
:root {
  --shadow-sm: 0 2px 8px rgba(0, 0, 0, 0.04);
  --shadow-md: 0 8px 24px rgba(45, 95, 79, 0.1);
  --shadow-lg: 0 12px 36px rgba(45, 95, 79, 0.15);
  --shadow-hover: 0 8px 24px rgba(45, 95, 79, 0.1);
}

.dark {
  --shadow-sm: 0 8px 32px rgba(0, 0, 0, 0.3);
  --shadow-md: 0 12px 48px rgba(0, 188, 212, 0.2);
  --shadow-lg: 0 16px 64px rgba(0, 0, 0, 0.5);
  --shadow-hover: 0 12px 48px rgba(0, 188, 212, 0.2);
}
```

### Animation Tokens

```css
:root {
  --duration-instant: 0ms;
  --duration-fast: 150ms;
  --duration-normal: 300ms;
  --duration-slow: 500ms;

  --easing-default: cubic-bezier(0.4, 0, 0.2, 1);
  --easing-spring: cubic-bezier(0.34, 1.56, 0.64, 1);
  --easing-bounce: cubic-bezier(0.68, -0.55, 0.265, 1.55);
}
```

## TypeScript Interface

Replace `frontend/src/lib/design-tokens.ts` with:

```typescript
/**
 * Design Tokens for the ExpenseFlow Dual Theme System
 *
 * Light Mode: Luxury Minimalist (Emerald #2d5f4f)
 * Dark Mode: Dark Cyber (Cyan #00bcd4)
 */

export type Theme = 'light' | 'dark' | 'system';

export interface SemanticColors {
  background: string;
  foreground: string;
  card: string;
  cardForeground: string;
  primary: string;
  primaryForeground: string;
  secondary: string;
  secondaryForeground: string;
  muted: string;
  mutedForeground: string;
  accent: string;
  accentForeground: string;
  destructive: string;
  destructiveForeground: string;
  border: string;
  input: string;
  ring: string;
}

export interface ChartColors {
  chart1: string;
  chart2: string;
  chart3: string;
  chart4: string;
  chart5: string;
}

export interface ConfidenceColors {
  high: string;   // Success/confirmed
  medium: string; // Warning/needs review
  low: string;    // Error/uncertain
}

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

// Confidence level utilities (retained from original)
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

// Confidence colors (shared between themes)
export const confidenceColors: ConfidenceColors = {
  high: '#10b981',   // Emerald - retained for semantic meaning
  medium: '#f59e0b', // Amber
  low: '#f43f5e',    // Rose
};

export function getConfidenceColor(score: number): string {
  const level = getConfidenceLevel(score);
  return confidenceColors[level];
}
```

## Color Reference Table

### Light Mode (Luxury Minimalist)

| Purpose | Variable | Hex | HSL |
|---------|----------|-----|-----|
| Page background | `--background` | #fafaf8 | 60 11% 97% |
| Card background | `--card` | #ffffff | 0 0% 100% |
| Primary accent | `--primary` | #2d5f4f | 158 36% 28% |
| Hover accent | `--accent` | #4a8f75 | 154 32% 43% |
| Primary text | `--foreground` | #1a1a1a | 0 0% 10% |
| Muted text | `--muted-foreground` | #666666 | 0 0% 40% |
| Subtle border | `--border` | #f0f0f0 | 0 0% 94% |

### Dark Mode (Dark Cyber)

| Purpose | Variable | Hex | HSL |
|---------|----------|-----|-----|
| Page background | `--background` | #0f1419 | 210 25% 8% |
| Card background | `--card` | #1e2438 | 224 30% 17% |
| Primary accent | `--primary` | #00bcd4 | 187 100% 42% |
| Secondary accent | `--accent` | #1e88e5 | 210 79% 51% |
| Primary text | `--foreground` | #e0e0e0 | 0 0% 88% |
| Muted text | `--muted-foreground` | #808080 | 0 0% 50% |
| Glass border | `--border` | rgba(255,255,255,0.1) | 0 0% 100% / 10% |

## Migration Notes

### Removed Tokens

The following tokens from "Refined Intelligence" are removed:

```css
/* REMOVED - Slate scale */
--color-slate-950 through --color-slate-50

/* REMOVED - Copper accents */
--color-accent-copper
--color-accent-copper-light
--color-accent-copper-dark

/* REMOVED - Serif font */
--font-serif: 'Playfair Display', Georgia, serif
```

### Retained Tokens

```css
/* RETAINED - Confidence colors (semantic) */
--color-confidence-high: #10b981
--color-confidence-medium: #f59e0b
--color-confidence-low: #f43f5e

/* RETAINED - Animation tokens */
--duration-*, --easing-*
```

## Validation Rules

1. **Contrast Ratio**: All text/background combinations must meet WCAG 2.1 AA (4.5:1 body, 3:1 large)
2. **HSL Format**: shadcn variables must use space-separated HSL values without `hsl()` wrapper
3. **Fallbacks**: Glassmorphism must have `@supports` fallback
4. **Transitions**: Theme switch must complete within 500ms
