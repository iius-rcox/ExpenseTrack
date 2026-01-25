/**
 * Playwright API Route Handlers for E2E Tests
 *
 * These functions create route handlers that intercept API requests
 * at the browser level, providing realistic mock responses.
 *
 * Usage in tests:
 *   import { setupApiMocks } from './fixtures/api-mocks'
 *   test.beforeEach(async ({ page }) => {
 *     await setupApiMocks(page)
 *   })
 */

import { Page, Route } from '@playwright/test'

// =============================================================================
// Mock Data
// =============================================================================

export const mockDashboardSummary = {
  totalTransactions: 156,
  totalSpending: 4523.67,
  pendingReceipts: 12,
  matchedReceipts: 144,
  unmatchedTransactions: 23,
  recentActivity: [
    {
      type: 'transaction',
      description: 'Purchase at Whole Foods',
      timestamp: new Date().toISOString(),
      amount: 87.45,
    },
    {
      type: 'receipt',
      description: 'Receipt uploaded via email',
      timestamp: new Date(Date.now() - 3600000).toISOString(),
      amount: 45.67,
    },
  ],
}

export const mockTransactions = {
  items: [
    {
      id: 'txn-001',
      date: new Date().toISOString(),
      description: 'WHOLE FOODS MARKET #10847',
      amount: 87.45,
      vendorName: 'Whole Foods',
      category: 'Groceries',
      matchStatus: 'matched',
      receiptId: 'rcpt-001',
    },
    {
      id: 'txn-002',
      date: new Date(Date.now() - 86400000).toISOString(),
      description: 'UBER *TRIP',
      amount: 24.50,
      vendorName: 'Uber',
      category: 'Transportation',
      matchStatus: 'unmatched',
      receiptId: null,
    },
    {
      id: 'txn-003',
      date: new Date(Date.now() - 172800000).toISOString(),
      description: 'AMAZON.COM*123ABC',
      amount: 156.78,
      vendorName: 'Amazon',
      category: 'Shopping',
      matchStatus: 'pending',
      receiptId: null,
    },
  ],
  totalCount: 156,
  pageSize: 50,
  pageNumber: 1,
}

export const mockReceipts = {
  items: [
    {
      id: 'rcpt-001',
      originalFilename: 'whole-foods-receipt.jpg',
      uploadedAt: new Date().toISOString(),
      status: 'complete',
      thumbnailUrl: '/api/thumbnails/rcpt-001',
      extractedFields: {
        vendor: 'Whole Foods Market',
        amount: 87.45,
        date: new Date().toISOString().split('T')[0],
        category: 'Groceries',
      },
      confidence: 0.94,
    },
    {
      id: 'rcpt-002',
      originalFilename: 'starbucks-receipt.pdf',
      uploadedAt: new Date(Date.now() - 3600000).toISOString(),
      status: 'processing',
      thumbnailUrl: null,
      extractedFields: null,
      confidence: null,
    },
  ],
  totalCount: 144,
  pageSize: 50,
  pageNumber: 1,
}

export const mockAnalytics = {
  spendingByCategory: [
    { category: 'Groceries', amount: 523.45, percentageOfTotal: 28.5, transactionCount: 12 },
    { category: 'Transportation', amount: 287.32, percentageOfTotal: 15.6, transactionCount: 8 },
    { category: 'Dining', amount: 412.67, percentageOfTotal: 22.4, transactionCount: 15 },
    { category: 'Entertainment', amount: 189.55, percentageOfTotal: 10.3, transactionCount: 6 },
    { category: 'Utilities', amount: 234.89, percentageOfTotal: 12.8, transactionCount: 4 },
  ],
  spendingTrend: Array.from({ length: 14 }, (_, i) => ({
    date: new Date(Date.now() - (13 - i) * 86400000).toISOString(),
    amount: 150 + Math.random() * 100,
    transactionCount: 3 + Math.floor(Math.random() * 5),
  })),
  comparison: {
    currentPeriod: '2026-01',
    previousPeriod: '2025-12',
    summary: {
      currentTotal: 2547.89,
      previousTotal: 2312.45,
      change: 235.44,
      changePercent: 10.18,
    },
  },
}

export const mockSettings = {
  userId: 'test-user-id',
  email: 'testuser@expenseflow.example.com',
  name: 'Test User',
  preferences: {
    defaultCategory: 'Other',
    autoMatch: true,
    emailNotifications: true,
    theme: 'system',
  },
}

// =============================================================================
// Route Handler Setup
// =============================================================================

/**
 * Sets up all API route mocks for a Playwright page
 */
export async function setupApiMocks(page: Page): Promise<void> {
  // Dashboard
  await page.route('**/api/dashboard/summary', async (route: Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockDashboardSummary),
    })
  })

  // Transactions
  await page.route('**/api/transactions*', async (route: Route) => {
    if (route.request().method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockTransactions),
      })
    } else {
      await route.continue()
    }
  })

  // Receipts list
  await page.route('**/api/receipts', async (route: Route) => {
    if (route.request().method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockReceipts),
      })
    } else {
      await route.continue()
    }
  })

  // Receipt upload
  await page.route('**/api/receipts/upload', async (route: Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        totalUploaded: 1,
        receipts: [{ id: 'rcpt-new', status: 'processing' }],
        failed: [],
      }),
    })
  })

  // Individual receipt
  await page.route('**/api/receipts/*', async (route: Route) => {
    const url = route.request().url()
    if (route.request().method() === 'GET' && !url.includes('upload')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockReceipts.items[0]),
      })
    } else {
      await route.continue()
    }
  })

  // Thumbnails - return a placeholder image
  await page.route('**/api/thumbnails/*', async (route: Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'image/png',
      body: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==', 'base64'),
    })
  })

  // Analytics endpoints
  await page.route('**/api/analytics/spending-by-category', async (route: Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockAnalytics.spendingByCategory),
    })
  })

  await page.route('**/api/analytics/spending-trend', async (route: Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockAnalytics.spendingTrend),
    })
  })

  await page.route('**/api/analytics/comparison', async (route: Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockAnalytics.comparison),
    })
  })

  await page.route('**/api/analytics/merchants', async (route: Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ merchants: [], totalMerchants: 0 }),
    })
  })

  await page.route('**/api/analytics/subscriptions', async (route: Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ subscriptions: [], totalMonthlyEstimate: 0 }),
    })
  })

  // Settings
  await page.route('**/api/settings*', async (route: Route) => {
    if (route.request().method() === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockSettings),
      })
    } else {
      await route.continue()
    }
  })

  // Matching endpoints
  await page.route('**/api/matching*', async (route: Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ candidates: [], totalCount: 0 }),
    })
  })

  // Reports
  await page.route('**/api/reports*', async (route: Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ reports: [], totalCount: 0 }),
    })
  })

  // Statements
  await page.route('**/api/statements*', async (route: Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ statements: [], totalCount: 0 }),
    })
  })

  // Reference data
  await page.route('**/api/reference-data/*', async (route: Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([]),
    })
  })
}

/**
 * Creates a 401 Unauthorized response for testing auth failures
 */
export async function setupUnauthorizedMock(page: Page, pattern: string): Promise<void> {
  await page.route(pattern, async (route: Route) => {
    await route.fulfill({
      status: 401,
      contentType: 'application/json',
      body: JSON.stringify({
        type: 'https://tools.ietf.org/html/rfc7235#section-3.1',
        title: 'Unauthorized',
        status: 401,
        detail: 'Bearer token is missing or expired',
      }),
    })
  })
}

/**
 * Creates a 500 Server Error response for testing error states
 */
export async function setupServerErrorMock(page: Page, pattern: string): Promise<void> {
  await page.route(pattern, async (route: Route) => {
    await route.fulfill({
      status: 500,
      contentType: 'application/json',
      body: JSON.stringify({
        type: 'https://tools.ietf.org/html/rfc7807',
        title: 'Internal Server Error',
        status: 500,
        detail: 'An unexpected error occurred',
      }),
    })
  })
}
