# CSS Variable Contract: Dual Theme System

**Feature Branch**: `015-dual-theme-system`
**Created**: 2025-12-23
**Type**: CSS Custom Property Specification

## Purpose

This contract defines the CSS custom properties (variables) that components can depend on. All theme-aware components MUST use these variables instead of hard-coded colors.

## Variable Naming Convention

```
--{category}[-{modifier}]: {value};
```

Categories:
- Core semantic: `background`, `foreground`, `card`, `primary`, etc.
- Component-specific: `sidebar-*`, `chart-*`
- Custom: `glass-*`, `shadow-*`, `gradient-*`

## Core Semantic Variables

### Required Variables (shadcn compatibility)

Every theme MUST define these variables:

| Variable | Description | Light Default | Dark Default |
|----------|-------------|---------------|--------------|
| `--background` | Page background | 60 11% 97% | 210 25% 8% |
| `--foreground` | Primary text | 0 0% 10% | 0 0% 88% |
| `--card` | Card/container background | 0 0% 100% | 224 30% 17% |
| `--card-foreground` | Card text | 0 0% 10% | 0 0% 88% |
| `--popover` | Dropdown/popover background | 0 0% 100% | 224 30% 17% |
| `--popover-foreground` | Popover text | 0 0% 10% | 0 0% 88% |
| `--primary` | Primary action color | 158 36% 28% | 187 100% 42% |
| `--primary-foreground` | Text on primary | 0 0% 100% | 210 25% 8% |
| `--secondary` | Secondary surfaces | 60 8% 96% | 225 27% 14% |
| `--secondary-foreground` | Secondary text | 0 0% 10% | 0 0% 88% |
| `--muted` | Muted surfaces | 60 8% 96% | 225 27% 14% |
| `--muted-foreground` | Muted text | 0 0% 40% | 0 0% 50% |
| `--accent` | Accent/hover color | 154 32% 43% | 210 79% 51% |
| `--accent-foreground` | Accent text | 0 0% 100% | 0 0% 100% |
| `--destructive` | Error/danger color | 0 72% 51% | 0 62% 30% |
| `--destructive-foreground` | Destructive text | 0 0% 100% | 0 0% 100% |
| `--border` | Border color | 0 0% 94% | 0 0% 100% / 10% |
| `--input` | Input border | 0 0% 94% | 0 0% 100% / 10% |
| `--ring` | Focus ring | 158 36% 28% | 187 100% 42% |
| `--radius` | Border radius base | 0.5rem | 0.5rem |

### Value Format

**IMPORTANT**: Values MUST be in HSL format without the `hsl()` wrapper:

```css
/* ✅ CORRECT */
--primary: 158 36% 28%;

/* ❌ WRONG */
--primary: hsl(158, 36%, 28%);
--primary: #2d5f4f;
```

Components consume these with:
```css
background-color: hsl(var(--primary));
```

## Chart Variables

| Variable | Description | Light | Dark |
|----------|-------------|-------|------|
| `--chart-1` | Primary chart color | 158 36% 28% | 187 100% 42% |
| `--chart-2` | Secondary chart color | 154 32% 43% | 210 79% 51% |
| `--chart-3` | Tertiary chart color | 158 36% 23% | 282 68% 38% |
| `--chart-4` | Quaternary chart color | 43 74% 66% | 187 60% 55% |
| `--chart-5` | Quinary chart color | 27 87% 67% | 210 60% 60% |

## Sidebar Variables

| Variable | Description | Light | Dark |
|----------|-------------|-------|------|
| `--sidebar-background` | Sidebar bg | 60 11% 97% | 210 25% 8% |
| `--sidebar-foreground` | Sidebar text | 0 0% 40% | 0 0% 69% |
| `--sidebar-primary` | Active item color | 158 36% 28% | 187 100% 42% |
| `--sidebar-primary-foreground` | Active text | 0 0% 100% | 0 0% 100% |
| `--sidebar-accent` | Hover background | 158 36% 28% / 8% | 187 100% 42% / 12% |
| `--sidebar-accent-foreground` | Hover text | 158 36% 28% | 187 100% 42% |
| `--sidebar-border` | Sidebar borders | 0 0% 94% | 0 0% 100% / 10% |
| `--sidebar-ring` | Sidebar focus | 158 36% 28% | 187 100% 42% |

## Custom Theme Variables

### Glassmorphism (Dark Mode Only)

| Variable | Description | Value |
|----------|-------------|-------|
| `--glass-bg` | Glass background | 0 0% 100% / 8% |
| `--glass-border` | Glass border | 0 0% 100% / 10% |
| `--glass-blur` | Blur amount | 10px |

**Usage**:
```css
.dark .glass-card {
  background: hsl(var(--glass-bg));
  backdrop-filter: blur(var(--glass-blur));
  -webkit-backdrop-filter: blur(var(--glass-blur));
  border: 1px solid hsl(var(--glass-border));
}
```

### Shadow Variables

| Variable | Light Mode | Dark Mode |
|----------|------------|-----------|
| `--shadow-sm` | 0 2px 8px rgba(0,0,0,0.04) | 0 8px 32px rgba(0,0,0,0.3) |
| `--shadow-md` | 0 8px 24px rgba(45,95,79,0.1) | 0 12px 48px rgba(0,188,212,0.2) |
| `--shadow-lg` | 0 12px 36px rgba(45,95,79,0.15) | 0 16px 64px rgba(0,0,0,0.5) |
| `--shadow-hover` | 0 8px 24px rgba(45,95,79,0.1) | 0 12px 48px rgba(0,188,212,0.2) |

### Gradient Variables

| Variable | Light Mode | Dark Mode |
|----------|------------|-----------|
| `--gradient-primary` | emerald→emerald-light | cyan→blue |
| `--gradient-text` | (solid color) | linear-gradient(135deg, #00bcd4, #1e88e5) |

### Animation Variables

| Variable | Value | Description |
|----------|-------|-------------|
| `--duration-instant` | 0ms | No animation |
| `--duration-fast` | 150ms | Quick feedback |
| `--duration-normal` | 300ms | Standard transitions |
| `--duration-slow` | 500ms | Theme switch |
| `--easing-default` | cubic-bezier(0.4, 0, 0.2, 1) | Standard easing |
| `--easing-spring` | cubic-bezier(0.34, 1.56, 0.64, 1) | Bouncy feel |
| `--easing-bounce` | cubic-bezier(0.68, -0.55, 0.265, 1.55) | Playful |

## Confidence Colors (Theme-Independent)

These colors have semantic meaning and remain consistent across themes:

| Variable | Value | Usage |
|----------|-------|-------|
| `--confidence-high` | #10b981 | AI confidence ≥90% |
| `--confidence-medium` | #f59e0b | AI confidence 70-89% |
| `--confidence-low` | #f43f5e | AI confidence <70% |

## Tailwind CSS 4.x @theme Block

The `@theme` block provides Tailwind-specific aliases:

```css
@theme {
  --color-background: hsl(var(--background));
  --color-foreground: hsl(var(--foreground));
  --color-card: hsl(var(--card));
  --color-primary: hsl(var(--primary));
  /* ... etc */
}
```

This enables using `bg-background`, `text-foreground`, etc. in Tailwind classes.

## Component Usage Examples

### Button
```tsx
<button className="bg-primary text-primary-foreground hover:bg-accent">
  Click me
</button>
```

### Card
```tsx
<div className="bg-card text-card-foreground border border-border rounded-lg shadow-sm">
  {/* content */}
</div>
```

### Glassmorphic Card (Dark Mode)
```tsx
<div className="bg-card dark:bg-[hsl(var(--glass-bg))] dark:backdrop-blur-[10px] border border-border">
  {/* content */}
</div>
```

### Gradient Text (Dark Mode)
```tsx
<h1 className="text-primary dark:bg-gradient-to-r dark:from-[#00bcd4] dark:to-[#1e88e5] dark:bg-clip-text dark:text-transparent">
  Dashboard
</h1>
```

## Validation Checklist

Before merging theme changes, verify:

- [ ] All 17 core semantic variables defined in both `:root` and `.dark`
- [ ] All 5 chart variables defined in both modes
- [ ] All 8 sidebar variables defined in both modes
- [ ] HSL values use space-separated format (not comma-separated)
- [ ] No hard-coded colors in component styles
- [ ] Focus states visible in both themes
- [ ] Contrast ratios meet WCAG AA (4.5:1 body, 3:1 large)
- [ ] Glassmorphism has `@supports` fallback

## Breaking Changes from "Refined Intelligence"

The following CSS variables are **removed** and must not be used:

```css
/* REMOVED - Do not use */
--color-slate-*
--color-accent-copper*
--font-serif
```

Components using these must be updated to use the new semantic variables.
