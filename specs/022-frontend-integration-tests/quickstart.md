# Quickstart: Frontend Integration Tests

**Feature**: 022-frontend-integration-tests
**Date**: 2026-01-02

## Overview

This guide helps developers set up and run the new frontend integration testing infrastructure.

## Prerequisites

- Node.js 22+ (already installed for ExpenseFlow frontend)
- pnpm (existing package manager)
- Docker (for running backend in integration scenarios)

## Quick Setup

### 1. Install New Dependencies

```bash
cd frontend
pnpm add -D msw@^2.7 openapi-typescript@^7
```

### 2. Verify Test Commands

After implementation, these npm scripts will be available:

```bash
# Run unit tests (existing)
pnpm test

# Run integration tests (new)
pnpm test:integration

# Run contract validation (new)
pnpm test:contracts

# Run all frontend tests
pnpm test:all

# Generate types from OpenAPI spec
pnpm generate:api-types
```

## Running Tests

### Unit Tests (Vitest)

Fast, isolated component tests with mocked dependencies:

```bash
# Run all unit tests
pnpm test

# Run in watch mode
pnpm test -- --watch

# Run specific test file
pnpm test -- src/components/analytics/
```

### Integration Tests (Vitest + MSW)

Page-level tests with realistic API mocking:

```bash
# Run integration tests
pnpm test:integration

# Run with coverage
pnpm test:integration -- --coverage
```

### Contract Tests

Validate frontend types match backend OpenAPI:

```bash
# Run contract validation
pnpm test:contracts

# Regenerate API types from latest spec
pnpm generate:api-types
```

### E2E Tests (Playwright)

Full browser-based authentication flow tests:

```bash
# Run E2E tests
pnpm e2e

# Run specific auth tests
pnpm e2e -- tests/e2e/auth-flow.spec.ts

# Run in headed mode for debugging
pnpm e2e -- --headed
```

## Test File Locations

```
frontend/
├── src/
│   ├── test-utils/             # Shared test utilities
│   │   ├── msw-handlers.ts     # MSW API mock handlers
│   │   ├── auth-mock.ts        # MSAL mock utilities
│   │   └── render-with-providers.tsx
│   └── components/
│       └── error-boundary/     # Error boundary components
├── tests/
│   ├── setup.ts                # Global test setup (MSW server)
│   ├── unit/                   # Unit tests (*.test.tsx)
│   ├── integration/            # Integration tests (*.test.tsx)
│   │   ├── analytics.test.tsx
│   │   ├── dashboard.test.tsx
│   │   └── transactions.test.tsx
│   ├── contracts/              # Contract validation
│   │   └── api-contracts.test.ts
│   └── e2e/                    # Playwright E2E tests
│       └── auth-flow.spec.ts
└── vitest.config.ts            # Updated with integration project
```

## Writing Tests

### Integration Test Example

```typescript
// tests/integration/analytics.test.tsx
import { describe, it, expect } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { renderWithProviders } from '@/test-utils/render-with-providers'
import { server } from '@/test-utils/msw-server'
import { http, HttpResponse } from 'msw'
import AnalyticsPage from '@/routes/_authenticated/analytics'

describe('Analytics Page Integration', () => {
  it('renders all sections when APIs succeed', async () => {
    // MSW handlers already mock successful responses
    renderWithProviders(<AnalyticsPage />)

    await waitFor(() => {
      expect(screen.getByTestId('spending-trend-chart')).toBeInTheDocument()
      expect(screen.getByTestId('category-breakdown')).toBeInTheDocument()
      expect(screen.getByTestId('monthly-comparison')).toBeInTheDocument()
    })
  })

  it('shows error state when API fails', async () => {
    // Override handler for this test
    server.use(
      http.get('/api/analytics/spending-trend', () => {
        return HttpResponse.json(
          { type: 'error', title: 'Server Error', status: 500 },
          { status: 500 }
        )
      })
    )

    renderWithProviders(<AnalyticsPage />)

    await waitFor(() => {
      expect(screen.getByText(/something went wrong/i)).toBeInTheDocument()
    })
  })
})
```

### MSW Handler Example

```typescript
// src/test-utils/msw-handlers.ts
import { http, HttpResponse } from 'msw'

export const analyticsHandlers = [
  http.get('/api/analytics/monthly-comparison', () => {
    return HttpResponse.json({
      currentTotal: 2500.00,
      previousTotal: 2300.00,
      percentageChange: 8.7,
      newVendors: [],
      missingRecurring: [],
      significantChanges: []
    })
  }),

  http.get('/api/analytics/spending-trend', () => {
    return HttpResponse.json([
      { date: '2026-01-01T00:00:00Z', amount: 150.00, transactionCount: 5 },
      { date: '2026-01-02T00:00:00Z', amount: 200.00, transactionCount: 8 }
    ])
  }),

  // ... more handlers
]

export const handlers = [
  ...analyticsHandlers,
  // ... other handler groups
]
```

### Auth Mock Example

```typescript
// src/test-utils/auth-mock.ts
import { vi } from 'vitest'

export const mockAccount = {
  homeAccountId: 'test-home-id',
  username: 'test@example.com',
  name: 'Test User',
  localAccountId: 'test-local-id',
  tenantId: 'test-tenant'
}

export const mockAuthenticatedState = {
  accounts: [mockAccount],
  inProgress: 'none' as const,
  instance: {
    getActiveAccount: () => mockAccount,
    acquireTokenSilent: vi.fn().mockResolvedValue({
      accessToken: 'mock-access-token',
      expiresOn: new Date(Date.now() + 3600000)
    })
  }
}

export const mockUnauthenticatedState = {
  accounts: [],
  inProgress: 'none' as const,
  instance: {
    getActiveAccount: () => null,
    loginRedirect: vi.fn()
  }
}
```

## Contract Validation

### How It Works

1. Backend generates OpenAPI spec at `/swagger/v1/swagger.json`
2. `openapi-typescript` generates TypeScript types from spec
3. Contract tests compare generated types with frontend types
4. Mismatches fail CI (blocking merge)

### Manual Validation

```bash
# Fetch latest OpenAPI spec from backend
curl http://localhost:5000/swagger/v1/swagger.json > api-spec.json

# Generate TypeScript types
pnpm openapi-typescript api-spec.json -o src/types/generated-api.d.ts

# Run contract tests
pnpm test:contracts
```

## CI Integration

Tests run automatically in GitHub Actions:

| Workflow | Tests Run | Trigger |
|----------|-----------|---------|
| `ci-quick.yml` | Unit only | Push to any branch |
| `ci-full.yml` | Unit + Integration + Contract | PR to main |
| `ci-full.yml` | E2E (auth flow) | PR to main |

### Expected CI Times

| Test Suite | Duration |
|------------|----------|
| Unit tests | ~30s |
| Integration tests | ~60s |
| Contract tests | ~10s |
| E2E auth tests | ~90s |
| **Total new tests** | **<3 min** |

## Troubleshooting

### MSW Not Intercepting Requests

```typescript
// Ensure server is started in setup.ts
import { server } from './msw-server'
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }))
afterEach(() => server.resetHandlers())
afterAll(() => server.close())
```

### MSAL Mock Issues

```typescript
// Mock the entire @azure/msal-react module
vi.mock('@azure/msal-react', () => ({
  useMsal: () => mockAuthenticatedState,
  useIsAuthenticated: () => true,
  MsalProvider: ({ children }) => children
}))
```

### Contract Test Failures

1. Check if backend OpenAPI spec has changed
2. Regenerate types: `pnpm generate:api-types`
3. Update frontend types to match
4. If intentional mismatch, coordinate with backend team

### Playwright Auth State

```typescript
// Save auth state after login (one-time setup)
await page.context().storageState({ path: 'playwright/.auth/user.json' })

// Reuse in tests
test.use({ storageState: 'playwright/.auth/user.json' })
```

## Next Steps

After setup is complete:

1. Run `pnpm test:all` to verify everything works
2. Check CI workflow passes in GitHub Actions
3. Add new integration tests as features are developed
4. Keep MSW handlers in sync with API changes
