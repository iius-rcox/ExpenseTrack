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

// Mock data for expense reports and available transactions
export const mockExpenseReport = {
  id: 'report-001',
  period: '2026-01',
  status: 'Draft',
  title: 'January 2026 Expense Report',
  lineCount: 3,
  totalAmount: 268.73,
  missingReceiptCount: 1,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  submittedAt: null,
  lines: [
    {
      id: 'line-001',
      transactionId: 'txn-001',
      receiptId: 'rcpt-001',
      transactionDate: '2026-01-15',
      amount: 87.45,
      description: 'WHOLE FOODS MARKET #10847',
      normalizedDescription: 'Whole Foods grocery purchase',
      vendor: 'Whole Foods',
      glCode: '6200',
      department: 'IT',
      project: null,
      category: 'Groceries',
      hasReceipt: true,
      missingReceiptJustification: null,
      notes: null,
      splitAllocations: [],
    },
    {
      id: 'line-002',
      transactionId: 'txn-002',
      receiptId: null,
      transactionDate: '2026-01-14',
      amount: 24.50,
      description: 'UBER *TRIP',
      normalizedDescription: 'Uber ride',
      vendor: 'Uber',
      glCode: '6300',
      department: 'IT',
      project: null,
      category: 'Transportation',
      hasReceipt: false,
      missingReceiptJustification: null,
      notes: null,
      splitAllocations: [],
    },
    {
      id: 'line-003',
      transactionId: 'txn-003',
      receiptId: 'rcpt-003',
      transactionDate: '2026-01-13',
      amount: 156.78,
      description: 'AMAZON.COM*123ABC',
      normalizedDescription: 'Amazon office supplies',
      vendor: 'Amazon',
      glCode: '6100',
      department: 'IT',
      project: null,
      category: 'Office Supplies',
      hasReceipt: true,
      missingReceiptJustification: null,
      notes: null,
      splitAllocations: [],
    },
  ],
}

export const mockAvailableTransactions = {
  transactions: [
    {
      id: 'txn-available-001',
      transactionDate: '2026-01-20',
      description: 'STARBUCKS COFFEE',
      originalDescription: 'STARBUCKS #12345',
      amount: 12.50,
      hasMatchedReceipt: false,
      receiptId: null,
      vendor: null,
      isOutsidePeriod: false,
    },
    {
      id: 'txn-available-002',
      transactionDate: '2026-01-18',
      description: 'OFFICE DEPOT',
      originalDescription: 'OFFICE DEPOT #98765',
      amount: 89.99,
      hasMatchedReceipt: true,
      receiptId: 'rcpt-003',
      vendor: 'Office Depot',
      isOutsidePeriod: false,
    },
    {
      id: 'txn-outside-period',
      transactionDate: '2025-12-15',
      description: 'HOTEL BOOKING',
      originalDescription: 'MARRIOTT HOTELS',
      amount: 245.00,
      hasMatchedReceipt: true,
      receiptId: 'rcpt-004',
      vendor: 'Marriott',
      isOutsidePeriod: true, // Outside report period
    },
  ],
  totalCount: 3,
  reportPeriod: '2026-01',
}

// =============================================================================
// Route Handler Setup
// =============================================================================

/**
 * Sets up all API route mocks for a Playwright page
 */
export async function setupApiMocks(page: Page): Promise<void> {
  // Dashboard - catch all dashboard endpoints
  await page.route('**/api/dashboard/*', async (route: Route) => {
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

  // Analytics - categories endpoint (catch-all for category-related queries)
  await page.route('**/api/analytics/categories*', async (route: Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockAnalytics.spendingByCategory),
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

  // Reports - catch all /api/reports/* endpoints with a single handler
  // This avoids pattern matching issues with multiple overlapping routes
  await page.route(/\/api\/reports/, async (route: Route) => {
    const url = route.request().url()
    const method = route.request().method()

    // Debug: console.log('>>> MOCK: reports handler - URL:', url, 'Method:', method)

    // Handle draft/exists check
    if (url.includes('/draft/exists')) {
      // Debug: console.log('>>> MOCK: draft/exists matched')
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          exists: true,
          reportId: mockExpenseReport.id,
        }),
      })
      return
    }

    // Handle preview
    if (url.includes('/preview')) {
      // Debug: console.log('>>> MOCK: preview matched')
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockExpenseReport.lines),
      })
      return
    }

    // Handle generate (POST)
    if (url.includes('/generate') && method === 'POST') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockExpenseReport),
      })
      return
    }

    // Handle available-transactions endpoint
    if (url.includes('/available-transactions')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockAvailableTransactions),
      })
      return
    }

    // Handle add line (POST to /lines)
    if (url.includes('/lines') && method === 'POST') {
      const body = JSON.parse(route.request().postData() || '{}')
      const newLine = {
        id: `line-new-${Date.now()}`,
        transactionId: body.transactionId,
        transactionDate: new Date().toISOString().split('T')[0],
        amount: 12.50,
        description: 'New transaction',
        normalizedDescription: 'New transaction added',
        vendor: 'Test Vendor',
        glCode: body.glCode || '6000',
        department: body.departmentCode || 'IT',
        hasReceipt: false,
      }
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify(newLine),
      })
      return
    }

    // Handle remove line (DELETE to /lines/:id)
    if (url.includes('/lines/') && method === 'DELETE') {
      await route.fulfill({
        status: 204,
      })
      return
    }

    // Handle update line (PATCH to /lines/:id)
    if (url.includes('/lines/') && method === 'PATCH') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockExpenseReport.lines[0]),
      })
      return
    }

    // Handle reports list (GET /api/reports)
    if (method === 'GET' && url.match(/\/api\/reports(\?|$)/)) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          reports: [mockExpenseReport],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        }),
      })
      return
    }

    // Handle individual report GET (e.g., /api/reports/{id})
    if (method === 'GET') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockExpenseReport),
      })
      return
    }

    // For anything else, continue to the network
    await route.continue()
  })

  // Statements
  await page.route('**/api/statements*', async (route: Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ statements: [], totalCount: 0 }),
    })
  })

  // Reference data - GL accounts (required for editor to load data)
  // Actual endpoint is /reference/gl-accounts (not /reference-data/)
  await page.route('**/api/reference/gl-accounts*', async (route: Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { code: '6100', name: 'Office Supplies' },
        { code: '6200', name: 'Groceries & Food' },
        { code: '6300', name: 'Transportation' },
        { code: '6000', name: 'General Expense' },
      ]),
    })
  })

  // Reference data - departments
  await page.route('**/api/reference/departments*', async (route: Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { code: 'IT', name: 'Information Technology' },
        { code: 'HR', name: 'Human Resources' },
        { code: 'FIN', name: 'Finance' },
      ]),
    })
  })

  // Reference data - other endpoints (legacy reference-data pattern)
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
