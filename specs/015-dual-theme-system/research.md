# Research: Dual Theme System

**Feature Branch**: `015-dual-theme-system`
**Created**: 2025-12-23
**Status**: Complete

## Research Summary

This document captures technical research findings to inform the implementation plan for the Dual Theme System feature.

## Current State Analysis

### Theme Infrastructure

| Component | Status | Notes |
|-----------|--------|-------|
| `next-themes` | Installed (v0.4.6) | In package.json but **not yet wired up** |
| ThemeProvider | Not implemented | Needs to wrap app in `__root.tsx` |
| `.dark` class | Exists in globals.css | Shadcn zinc theme values present |
| System preference detection | Not implemented | `next-themes` handles this automatically |

### Existing Design Token Systems

**1. Tailwind CSS 4.x `@theme` block** (globals.css:3-46)
- Uses `--color-*` naming convention
- Defines shadcn zinc theme variables
- Includes chart colors and sidebar tokens

**2. CSS Variables `:root`** (globals.css:49-123)
- Duplicate of `@theme` variables for shadcn component compatibility
- Includes "Refined Intelligence" custom properties (slate/copper)
- These will be **replaced entirely** per clarification decision

**3. TypeScript Design Tokens** (lib/design-tokens.ts)
- Exports `ColorTokens`, `TypographyTokens`, `AnimationTokens` interfaces
- Contains slate palette and copper accents
- Will be **replaced entirely** with new theme tokens

### shadcn/ui Integration

shadcn components use CSS variables with HSL values in the format:
```css
--primary: 240 5.9% 10%;  /* H S% L% without hsl() wrapper */
```

Components reference these via:
```css
background-color: hsl(var(--primary));
```

**Important**: New theme values must follow this HSL format for shadcn compatibility.

### Files Using Dark Mode Classes

8 components currently use `dark:` Tailwind variants:
- `transaction-row.tsx`
- `extracted-field.tsx`
- `batch-upload-queue.tsx`
- `matching-factors.tsx`
- `match-review-workspace.tsx`
- `batch-review-panel.tsx`
- `subscription-detector.tsx`
- `alert.tsx` (shadcn)

These will automatically inherit new theme colors when CSS variables are updated.

## Theme Specifications

### Light Mode: Luxury Minimalist

| Token | Value | HSL Conversion |
|-------|-------|----------------|
| bg-primary | #fafaf8 | 60 11% 97% |
| bg-secondary | #f5f5f3 | 60 8% 96% |
| bg-card | #ffffff | 0 0% 100% |
| accent-primary | #2d5f4f | 158 36% 28% |
| accent-secondary | #4a8f75 | 154 32% 43% |
| text-primary | #1a1a1a | 0 0% 10% |
| text-secondary | #666666 | 0 0% 40% |
| text-tertiary | #999999 | 0 0% 60% |
| border | #f0f0f0 | 0 0% 94% |
| border-emphasis | #e5e5e5 | 0 0% 90% |

### Dark Mode: Dark Cyber

| Token | Value | HSL Conversion |
|-------|-------|----------------|
| bg-primary | #0f1419 | 210 25% 8% |
| bg-secondary | #1a1f2e | 225 27% 14% |
| bg-card | #1e2438 | 224 30% 17% |
| accent-primary | #00bcd4 | 187 100% 42% |
| accent-secondary | #1e88e5 | 210 79% 51% |
| text-primary | #e0e0e0 | 0 0% 88% |
| text-secondary | #b0b0b0 | 0 0% 69% |
| text-tertiary | #808080 | 0 0% 50% |
| border | rgba(255,255,255,0.1) | 0 0% 100% / 10% |
| glassmorphism-bg | rgba(255,255,255,0.08) | 0 0% 100% / 8% |

## Implementation Approach

### ThemeProvider Integration

`next-themes` will be added to the app structure:

```
main.tsx
  └── QueryClientProvider
        └── MsalProvider
              └── RouterProvider
                    └── __root.tsx
                          └── ThemeProvider ← NEW
                                └── Outlet
```

The ThemeProvider should:
1. Use `attribute="class"` for Tailwind dark mode
2. Use `defaultTheme="system"` for OS preference detection
3. Use `enableSystem={true}` for automatic switching
4. Use `storageKey="expenseflow-theme"` for persistence

### CSS Variable Mapping Strategy

Map themeing.md colors to shadcn variable names:

| shadcn Variable | Light Mode | Dark Mode |
|-----------------|------------|-----------|
| `--background` | #fafaf8 → 60 11% 97% | #0f1419 → 210 25% 8% |
| `--foreground` | #1a1a1a → 0 0% 10% | #e0e0e0 → 0 0% 88% |
| `--card` | #ffffff → 0 0% 100% | #1e2438 → 224 30% 17% |
| `--primary` | #2d5f4f → 158 36% 28% | #00bcd4 → 187 100% 42% |
| `--secondary` | #f5f5f3 → 60 8% 96% | #1a1f2e → 225 27% 14% |
| `--accent` | #4a8f75 → 154 32% 43% | #1e88e5 → 210 79% 51% |

### Glassmorphism Implementation

Dark mode cards require:
```css
.dark .glassmorphic-card {
  background: rgba(255, 255, 255, 0.08);
  backdrop-filter: blur(10px);
  -webkit-backdrop-filter: blur(10px);
  border: 1px solid rgba(255, 255, 255, 0.1);
}

/* Graceful fallback for unsupported browsers */
@supports not (backdrop-filter: blur(10px)) {
  .dark .glassmorphic-card {
    background: rgba(30, 36, 56, 0.95);
  }
}
```

### Gradient Text Implementation

Dark mode stat values and H1 headings:
```css
.dark .gradient-text {
  background: linear-gradient(135deg, #00bcd4 0%, #1e88e5 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}
```

### Shine Animation for Cards

```css
.stat-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(0, 188, 212, 0.1), transparent);
  transition: left 0.6s ease;
}

.stat-card:hover::before {
  left: 100%;
}
```

## Browser Compatibility

| Feature | Chrome | Firefox | Safari | Edge |
|---------|--------|---------|--------|------|
| CSS Variables | 49+ | 31+ | 9.1+ | 15+ |
| backdrop-filter | 76+ | 103+ | 14+ | 79+ |
| background-clip: text | 120+ (prefixed) | 49+ | 14+ | 15+ |

### Fallback Strategy

1. **backdrop-filter**: Use `@supports` query to provide solid background fallback
2. **gradient text**: Solid color fallback for very old browsers (not needed for target browsers)

## Removed Systems

The following will be completely removed:

1. **Slate color scale** (globals.css:86-96)
2. **Copper accent colors** (globals.css:98-101)
3. **"Refined Intelligence" TypeScript tokens** (lib/design-tokens.ts)

Components currently using `colors.slate.*` or `colors.accent.copper*` must be updated to use the new theme variables.

## Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| Component styling breaks | Medium | Comprehensive visual testing |
| Chart colors incompatible | Low | Update Recharts theme config |
| Third-party components | Low | Most use CSS variables already |
| Performance impact | Low | CSS-only changes, minimal JS |

## Open Questions (Resolved)

1. ~~Browser fallback for glassmorphism~~ → **Resolved**: Graceful fallback with `@supports`
2. ~~Token migration strategy~~ → **Resolved**: Clean replacement, no backward compatibility

## Next Steps

1. Generate data-model.md with complete token definitions
2. Create CSS variable contracts in contracts/
3. Write quickstart.md implementation guide
