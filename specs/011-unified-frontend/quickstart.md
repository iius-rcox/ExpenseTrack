# Quickstart: Unified Frontend Experience

**Feature Branch**: `011-unified-frontend`
**Date**: 2025-12-18

This guide provides step-by-step instructions for setting up and running the unified frontend.

## Prerequisites

- Node.js 20.x or later
- pnpm 9.x (recommended) or npm 10.x
- Access to the ExpenseFlow backend API (local or staging)
- Azure AD tenant credentials for MSAL configuration

## 1. Initial Setup

### Install Dependencies

```powershell
cd frontend

# Install existing dependencies plus new ones
pnpm add @tanstack/react-router @tanstack/react-query @tanstack/zod-adapter zod date-fns lucide-react sonner tailwind-merge clsx class-variance-authority

# Install dev dependencies
pnpm add -D @tanstack/router-plugin @tanstack/router-devtools @tanstack/react-query-devtools tailwindcss postcss autoprefixer @types/node
```

### Initialize Tailwind CSS

```powershell
# Initialize Tailwind if not already done
npx tailwindcss init -p
```

### Initialize shadcn/ui

```powershell
# Run shadcn init
npx shadcn@latest init

# Follow prompts:
# - Style: New York
# - Base color: Zinc
# - CSS variables: Yes
# - Tailwind config location: tailwind.config.ts
# - Components location: src/components/ui
# - Utils location: src/lib/utils.ts
# - React Server Components: No
```

## 2. Add Required shadcn/ui Components

```powershell
# Core layout components
npx shadcn@latest add sidebar button card

# Navigation
npx shadcn@latest add breadcrumb dropdown-menu

# Data display
npx shadcn@latest add table badge skeleton avatar

# Forms
npx shadcn@latest add input select textarea checkbox label

# Feedback
npx shadcn@latest add dialog alert alert-dialog toast sonner

# Additional UI
npx shadcn@latest add tabs progress tooltip separator scroll-area
```

## 3. Configure Vite

Update `vite.config.ts`:

```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { TanStackRouterVite } from '@tanstack/router-plugin/vite'
import path from 'path'

export default defineConfig({
  plugins: [
    TanStackRouterVite(),
    react(),
  ],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
})
```

## 4. Configure TypeScript

Update `tsconfig.json`:

```json
{
  "compilerOptions": {
    "target": "ES2020",
    "useDefineForClassFields": true,
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "resolveJsonModule": true,
    "isolatedModules": true,
    "noEmit": true,
    "jsx": "react-jsx",
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true,
    "baseUrl": ".",
    "paths": {
      "@/*": ["./src/*"]
    }
  },
  "include": ["src"],
  "references": [{ "path": "./tsconfig.node.json" }]
}
```

## 5. Configure Tailwind

Update `tailwind.config.ts`:

```typescript
import type { Config } from 'tailwindcss'

const config: Config = {
  darkMode: ['class'],
  content: [
    './src/**/*.{ts,tsx}',
  ],
  theme: {
    container: {
      center: true,
      padding: '2rem',
      screens: {
        '2xl': '1400px',
      },
    },
    extend: {
      colors: {
        border: 'hsl(var(--border))',
        input: 'hsl(var(--input))',
        ring: 'hsl(var(--ring))',
        background: 'hsl(var(--background))',
        foreground: 'hsl(var(--foreground))',
        primary: {
          DEFAULT: 'hsl(var(--primary))',
          foreground: 'hsl(var(--primary-foreground))',
        },
        secondary: {
          DEFAULT: 'hsl(var(--secondary))',
          foreground: 'hsl(var(--secondary-foreground))',
        },
        destructive: {
          DEFAULT: 'hsl(var(--destructive))',
          foreground: 'hsl(var(--destructive-foreground))',
        },
        muted: {
          DEFAULT: 'hsl(var(--muted))',
          foreground: 'hsl(var(--muted-foreground))',
        },
        accent: {
          DEFAULT: 'hsl(var(--accent))',
          foreground: 'hsl(var(--accent-foreground))',
        },
        popover: {
          DEFAULT: 'hsl(var(--popover))',
          foreground: 'hsl(var(--popover-foreground))',
        },
        card: {
          DEFAULT: 'hsl(var(--card))',
          foreground: 'hsl(var(--card-foreground))',
        },
        sidebar: {
          DEFAULT: 'hsl(var(--sidebar-background))',
          foreground: 'hsl(var(--sidebar-foreground))',
          primary: 'hsl(var(--sidebar-primary))',
          'primary-foreground': 'hsl(var(--sidebar-primary-foreground))',
          accent: 'hsl(var(--sidebar-accent))',
          'accent-foreground': 'hsl(var(--sidebar-accent-foreground))',
          border: 'hsl(var(--sidebar-border))',
          ring: 'hsl(var(--sidebar-ring))',
        },
      },
      borderRadius: {
        lg: 'var(--radius)',
        md: 'calc(var(--radius) - 2px)',
        sm: 'calc(var(--radius) - 4px)',
      },
    },
  },
  plugins: [require('tailwindcss-animate')],
}

export default config
```

## 6. Create Core Files

### Create Route Files Structure

```powershell
# Create routes directory structure
mkdir -p src/routes/_authenticated/receipts
mkdir -p src/routes/_authenticated/transactions
mkdir -p src/routes/_authenticated/matching
mkdir -p src/routes/_authenticated/reports
mkdir -p src/routes/_authenticated/statements

# Create component directories
mkdir -p src/components/layout
mkdir -p src/components/receipts
mkdir -p src/components/transactions
mkdir -p src/components/matching
mkdir -p src/components/reports
mkdir -p src/components/analytics

# Create other directories
mkdir -p src/services
mkdir -p src/hooks
mkdir -p src/lib
mkdir -p src/types/api
```

### Create Router Configuration

Create `src/router.ts`:

```typescript
import { createRouter } from '@tanstack/react-router'
import { routeTree } from './routeTree.gen'
import { queryClient } from './lib/query-client'

export const router = createRouter({
  routeTree,
  context: {
    queryClient,
    msalInstance: undefined!,
    account: null,
    isAuthenticated: false,
  },
  defaultPreload: 'intent',
  defaultPreloadStaleTime: 0,
})

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}
```

### Create Query Client

Create `src/lib/query-client.ts`:

```typescript
import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      gcTime: 5 * 60 * 1000,
      retry: 1,
      refetchOnWindowFocus: true,
    },
  },
})
```

## 7. Environment Configuration

Create `.env.local`:

```bash
# API Configuration
VITE_API_BASE_URL=http://localhost:5000/api

# MSAL Configuration (already in authConfig.ts)
VITE_MSAL_CLIENT_ID=00435dee-8aff-429b-bab6-762973c091c4
VITE_MSAL_TENANT_ID=953922e6-5370-4a01-a3d5-773a30df726b
```

## 8. Running the Application

### Development Mode

```powershell
# Start the frontend dev server
cd frontend
pnpm dev

# In another terminal, start the backend (if local)
cd ../backend
dotnet run --project src/ExpenseFlow.Api
```

The frontend will be available at `http://localhost:3000`

### Build for Production

```powershell
cd frontend

# Type check
pnpm type-check

# Lint
pnpm lint

# Build
pnpm build

# Preview production build
pnpm preview
```

## 9. Verification Checklist

After setup, verify:

- [ ] `pnpm dev` starts without errors
- [ ] Routes are generated in `src/routeTree.gen.ts`
- [ ] Login redirect works with MSAL
- [ ] API proxy forwards requests to backend
- [ ] shadcn/ui components render correctly
- [ ] Sidebar collapses on mobile viewport
- [ ] TanStack Query DevTools appear in development

## 10. Development Workflow

### Adding New Routes

1. Create route file in `src/routes/` following TanStack Router conventions
2. Route tree auto-generates on save
3. Add query options in `src/services/`
4. Add React Query hooks in `src/hooks/`

### Adding shadcn/ui Components

```powershell
# Use the CLI to add components
npx shadcn@latest add [component-name]

# Or use the MCP server during implementation
# Components will be retrieved via shadcn MCP
```

### Testing

```powershell
# Run unit tests
pnpm test

# Run tests in watch mode
pnpm test:watch

# Run E2E tests (requires Playwright)
pnpm test:e2e
```

## Troubleshooting

### Route Generation Issues

If routes aren't generating:
```powershell
# Clear cache and restart
rm -rf node_modules/.vite
pnpm dev
```

### MSAL Token Errors

If authentication fails:
1. Clear browser localStorage
2. Check tenant/client ID configuration
3. Verify redirect URIs in Azure AD app registration

### Tailwind Classes Not Applied

If styles aren't working:
1. Verify `content` paths in `tailwind.config.ts`
2. Check that `globals.css` is imported in `main.tsx`
3. Restart dev server

## Next Steps

After initial setup:

1. Implement root layout with sidebar navigation
2. Create authenticated layout with auth guard
3. Build dashboard with summary cards
4. Implement receipt list and upload components
5. Add transaction list with filtering
6. Build match review interface
7. Create report generation flow
8. Add analytics visualizations
9. Implement settings page
