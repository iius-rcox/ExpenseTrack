# Theme Provider Contract

**Feature Branch**: `015-dual-theme-system`
**Created**: 2025-12-23
**Type**: React Context/Provider Specification

## Purpose

This contract defines the interface for the theme switching infrastructure using `next-themes`.

## Provider Configuration

### Required Setup

```tsx
// frontend/src/providers/theme-provider.tsx
import { ThemeProvider as NextThemesProvider } from "next-themes";
import type { ThemeProviderProps } from "next-themes";

export function ThemeProvider({ children, ...props }: ThemeProviderProps) {
  return (
    <NextThemesProvider
      attribute="class"
      defaultTheme="system"
      enableSystem={true}
      storageKey="expenseflow-theme"
      disableTransitionOnChange={false}
      {...props}
    >
      {children}
    </NextThemesProvider>
  );
}
```

### Configuration Options

| Option | Value | Description |
|--------|-------|-------------|
| `attribute` | `"class"` | Adds `.dark` class to `<html>` |
| `defaultTheme` | `"system"` | Respects OS preference on first visit |
| `enableSystem` | `true` | Enables automatic OS preference detection |
| `storageKey` | `"expenseflow-theme"` | localStorage key for persistence |
| `disableTransitionOnChange` | `false` | Enables smooth transitions |

## Hook Interface

### useTheme Hook

```tsx
import { useTheme } from "next-themes";

const { theme, setTheme, resolvedTheme, systemTheme } = useTheme();
```

| Property | Type | Description |
|----------|------|-------------|
| `theme` | `'light' \| 'dark' \| 'system' \| undefined` | Current theme setting |
| `setTheme` | `(theme: string) => void` | Function to change theme |
| `resolvedTheme` | `'light' \| 'dark' \| undefined` | Actual theme (resolves 'system') |
| `systemTheme` | `'light' \| 'dark' \| undefined` | OS preference |

## Theme Toggle Component Contract

### Interface

```tsx
interface ThemeToggleProps {
  className?: string;
}

export function ThemeToggle({ className }: ThemeToggleProps): JSX.Element;
```

### Behavior Requirements

1. **Visual State**: Display current theme state (sun/moon icon)
2. **Toggle Action**: Cycle between light → dark → system → light
3. **Keyboard Accessible**: Respond to Enter/Space
4. **Focus Visible**: Show focus ring matching `--ring` color
5. **ARIA**: Include `aria-label` describing current state

### Recommended Implementation

```tsx
import { Moon, Sun, Monitor } from "lucide-react";
import { useTheme } from "next-themes";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

export function ThemeToggle() {
  const { setTheme, theme } = useTheme();

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
  );
}
```

## Integration Points

### App Structure

```
main.tsx
  └── QueryClientProvider
        └── MsalProvider
              └── RouterProvider
                    └── __root.tsx
                          └── ThemeProvider ← INJECT HERE
                                └── Outlet
                                      └── (all routes)
```

### Required Changes

1. **Create**: `frontend/src/providers/theme-provider.tsx`
2. **Modify**: `frontend/src/routes/__root.tsx` - wrap `Outlet` with `ThemeProvider`
3. **Create**: `frontend/src/components/theme-toggle.tsx`
4. **Modify**: `frontend/src/components/layout/app-header.tsx` - add `ThemeToggle`

## Hydration Handling

### Flash Prevention

To prevent theme flash on load, add to `index.html`:

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

### Mounted State

Components that depend on theme should handle hydration:

```tsx
import { useTheme } from "next-themes";
import { useEffect, useState } from "react";

export function ThemeAwareComponent() {
  const [mounted, setMounted] = useState(false);
  const { resolvedTheme } = useTheme();

  useEffect(() => {
    setMounted(true);
  }, []);

  if (!mounted) {
    return null; // or skeleton
  }

  return <div>{resolvedTheme === 'dark' ? 'Dark' : 'Light'}</div>;
}
```

## Storage Contract

### localStorage Schema

```json
{
  "expenseflow-theme": "light" | "dark" | "system"
}
```

### Persistence Rules

1. **Default**: No entry = use system preference
2. **Manual Selection**: Store explicit choice
3. **Clear Storage**: Revert to system preference
4. **Cross-Tab**: Changes sync via storage events

## Testing Contract

### Required Test Cases

| ID | Test Case | Expected Result |
|----|-----------|-----------------|
| TC-001 | Click toggle in light mode | Page transitions to dark mode |
| TC-002 | Click toggle in dark mode | Page transitions to light mode |
| TC-003 | Set to dark, close/reopen browser | Loads in dark mode |
| TC-004 | Clear storage, set OS to dark | Loads in dark mode |
| TC-005 | Manual selection, change OS preference | Manual preference preserved |
| TC-006 | Theme transition | Completes within 500ms |

## Success Criteria Mapping

| Requirement | Contract Element |
|-------------|------------------|
| FR-001: Toggle in nav bar | ThemeToggle component |
| FR-002: Persist preference | storageKey + localStorage |
| FR-003: Apply immediately | setTheme + class attribute |
| FR-004: Smooth 300ms transition | CSS transitions + disableTransitionOnChange=false |
| FR-009: Respect OS preference | defaultTheme="system" + enableSystem=true |
| SC-001: Transition <500ms | CSS transition duration |
| SC-002: Persist across sessions | localStorage contract |
