/**
 * MSW Request Handlers for Frontend Integration Tests
 *
 * These handlers intercept API requests at the network level, providing
 * realistic mock responses that match the backend OpenAPI contract.
 *
 * Usage:
 * - Import handlers into test setup for default behavior
 * - Override specific handlers in tests using server.use()
 */

import { http, HttpResponse, delay } from 'msw'

// =============================================================================
// Analytics Endpoint Handlers
// =============================================================================

export const analyticsHandlers = [
  // Monthly Comparison - GET /api/analytics/comparison
  http.get('/api/analytics/comparison', async () => {
    await delay(50) // Simulate network latency
    return HttpResponse.json({
      currentPeriod: '2025-12',
      previousPeriod: '2025-11',
      summary: {
        currentTotal: 2547.89,
        previousTotal: 2312.45,
        change: 235.44,
        changePercent: 10.18,
      },
      newVendors: [
        {
          vendorName: 'Spotify',
          amount: 14.99,
          transactionCount: 1,
        },
      ],
      missingRecurring: [],
      significantChanges: [
        {
          vendorName: 'Amazon',
          currentAmount: 456.78,
          previousAmount: 234.56,
          change: 222.22,
          changePercent: 94.7,
        },
      ],
    })
  }),

  // Spending Trend - GET /api/analytics/spending-trend
  http.get('/api/analytics/spending-trend', async ({ request }) => {
    await delay(50)
    const url = new URL(request.url)
    const granularity = url.searchParams.get('granularity') || 'daily'

    // Generate sample trend data based on granularity
    const dataPoints =
      granularity === 'monthly' ? 6 : granularity === 'weekly' ? 8 : 14

    const trendData = Array.from({ length: dataPoints }, (_, i) => {
      const date = new Date()
      date.setDate(date.getDate() - (dataPoints - i - 1))
      return {
        date: date.toISOString(),
        amount: 150 + Math.random() * 100,
        transactionCount: 3 + Math.floor(Math.random() * 5),
      }
    })

    return HttpResponse.json(trendData)
  }),

  // Category Breakdown - GET /api/analytics/spending-by-category
  http.get('/api/analytics/spending-by-category', async () => {
    await delay(50)
    return HttpResponse.json([
      {
        category: 'Groceries',
        amount: 523.45,
        percentageOfTotal: 28.5,
        transactionCount: 12,
      },
      {
        category: 'Transportation',
        amount: 287.32,
        percentageOfTotal: 15.6,
        transactionCount: 8,
      },
      {
        category: 'Dining',
        amount: 412.67,
        percentageOfTotal: 22.4,
        transactionCount: 15,
      },
      {
        category: 'Entertainment',
        amount: 189.55,
        percentageOfTotal: 10.3,
        transactionCount: 6,
      },
      {
        category: 'Utilities',
        amount: 234.89,
        percentageOfTotal: 12.8,
        transactionCount: 4,
      },
      {
        category: 'Other',
        amount: 189.01,
        percentageOfTotal: 10.4,
        transactionCount: 9,
      },
    ])
  }),

  // Merchant Analytics - GET /api/analytics/merchants
  http.get('/api/analytics/merchants', async ({ request }) => {
    await delay(50)
    const url = new URL(request.url)
    const limit = parseInt(url.searchParams.get('limit') || '10')

    const merchants = [
      {
        merchantName: 'Whole Foods',
        totalSpent: 423.67,
        transactionCount: 8,
        lastTransaction: new Date().toISOString(),
        averageTransaction: 52.96,
      },
      {
        merchantName: 'Amazon',
        totalSpent: 387.45,
        transactionCount: 12,
        lastTransaction: new Date().toISOString(),
        averageTransaction: 32.29,
      },
      {
        merchantName: 'Uber',
        totalSpent: 234.89,
        transactionCount: 15,
        lastTransaction: new Date().toISOString(),
        averageTransaction: 15.66,
      },
      {
        merchantName: 'Netflix',
        totalSpent: 45.97,
        transactionCount: 3,
        lastTransaction: new Date().toISOString(),
        averageTransaction: 15.32,
      },
      {
        merchantName: 'Starbucks',
        totalSpent: 156.78,
        transactionCount: 24,
        lastTransaction: new Date().toISOString(),
        averageTransaction: 6.53,
      },
    ]

    return HttpResponse.json({
      merchants: merchants.slice(0, limit),
      totalMerchants: 47,
    })
  }),

  // Subscription Detection - GET /api/analytics/subscriptions
  http.get('/api/analytics/subscriptions', async () => {
    await delay(50)
    return HttpResponse.json({
      subscriptions: [
        {
          merchantName: 'Netflix',
          estimatedAmount: 15.99,
          frequency: 'monthly',
          confidence: 0.95,
          lastOccurrence: new Date().toISOString(),
          nextExpected: new Date(
            Date.now() + 30 * 24 * 60 * 60 * 1000
          ).toISOString(),
        },
        {
          merchantName: 'Spotify',
          estimatedAmount: 14.99,
          frequency: 'monthly',
          confidence: 0.92,
          lastOccurrence: new Date().toISOString(),
          nextExpected: new Date(
            Date.now() + 30 * 24 * 60 * 60 * 1000
          ).toISOString(),
        },
        {
          merchantName: 'Adobe Creative Cloud',
          estimatedAmount: 54.99,
          frequency: 'monthly',
          confidence: 0.88,
          lastOccurrence: new Date().toISOString(),
          nextExpected: new Date(
            Date.now() + 30 * 24 * 60 * 60 * 1000
          ).toISOString(),
        },
      ],
      totalMonthlyEstimate: 85.97,
    })
  }),
]

// =============================================================================
// Dashboard Endpoint Handlers
// =============================================================================

export const dashboardHandlers = [
  // Dashboard Summary - GET /api/dashboard/summary
  http.get('/api/dashboard/summary', async () => {
    await delay(50)
    return HttpResponse.json({
      totalTransactions: 156,
      totalSpending: 4523.67,
      pendingReceipts: 12,
      matchedReceipts: 144,
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
        {
          type: 'match',
          description: 'Receipt matched with Amazon transaction',
          timestamp: new Date(Date.now() - 7200000).toISOString(),
          amount: 123.45,
        },
        {
          type: 'report',
          description: 'Monthly expense report generated',
          timestamp: new Date(Date.now() - 86400000).toISOString(),
        },
      ],
    })
  }),
]

// =============================================================================
// Error Response Handlers (for testing error states)
// =============================================================================

/**
 * Creates a handler that returns a 401 Unauthorized response
 */
export const createUnauthorizedHandler = (path: string) =>
  http.get(path, () => {
    return HttpResponse.json(
      {
        type: 'https://tools.ietf.org/html/rfc7235#section-3.1',
        title: 'Unauthorized',
        status: 401,
        detail: 'Bearer token is missing or expired',
        instance: path,
      },
      { status: 401 }
    )
  })

/**
 * Creates a handler that returns a 500 Server Error response
 */
export const createServerErrorHandler = (path: string) =>
  http.get(path, () => {
    return HttpResponse.json(
      {
        type: 'https://tools.ietf.org/html/rfc7807',
        title: 'Internal Server Error',
        status: 500,
        detail: 'An unexpected error occurred while processing your request',
        instance: path,
      },
      { status: 500 }
    )
  })

/**
 * Creates a handler that returns an empty array response
 */
export const createEmptyResponseHandler = (path: string) =>
  http.get(path, () => {
    return HttpResponse.json([])
  })

/**
 * Creates a handler that simulates network timeout
 */
export const createTimeoutHandler = (path: string) =>
  http.get(path, async () => {
    await delay('infinite')
  })

/**
 * Creates a handler that returns malformed/unexpected JSON structure
 */
export const createMalformedResponseHandler = (path: string) =>
  http.get(path, () => {
    return HttpResponse.json({
      unexpectedField: 'this structure does not match the contract',
      randomData: [1, 2, 3],
      nested: { garbage: true },
    })
  })

/**
 * Creates a handler that simulates a network error (connection refused, etc.)
 */
export const createNetworkErrorHandler = (path: string) =>
  http.get(path, () => {
    return HttpResponse.error()
  })

/**
 * Creates a handler that returns a 403 Forbidden response
 */
export const createForbiddenHandler = (path: string) =>
  http.get(path, () => {
    return HttpResponse.json(
      {
        type: 'https://tools.ietf.org/html/rfc7231#section-6.5.3',
        title: 'Forbidden',
        status: 403,
        detail: 'You do not have permission to access this resource',
        instance: path,
      },
      { status: 403 }
    )
  })

/**
 * Creates a handler that returns a 404 Not Found response
 */
export const createNotFoundHandler = (path: string) =>
  http.get(path, () => {
    return HttpResponse.json(
      {
        type: 'https://tools.ietf.org/html/rfc7231#section-6.5.4',
        title: 'Not Found',
        status: 404,
        detail: 'The requested resource was not found',
        instance: path,
      },
      { status: 404 }
    )
  })

/**
 * Creates a handler that returns a 429 Rate Limit response
 */
export const createRateLimitHandler = (path: string) =>
  http.get(path, () => {
    return HttpResponse.json(
      {
        type: 'https://tools.ietf.org/html/rfc6585#section-4',
        title: 'Too Many Requests',
        status: 429,
        detail: 'Rate limit exceeded. Please wait before retrying.',
        instance: path,
      },
      { status: 429, headers: { 'Retry-After': '60' } }
    )
  })

// =============================================================================
// Error State Handler Variants
// =============================================================================

export const errorHandlers = {
  analytics: {
    monthlyComparison: {
      unauthorized: createUnauthorizedHandler(
        '/api/analytics/comparison'
      ),
      serverError: createServerErrorHandler('/api/analytics/comparison'),
    },
    spendingTrend: {
      unauthorized: createUnauthorizedHandler('/api/analytics/spending-trend'),
      serverError: createServerErrorHandler('/api/analytics/spending-trend'),
      empty: createEmptyResponseHandler('/api/analytics/spending-trend'),
    },
    categoryBreakdown: {
      unauthorized: createUnauthorizedHandler(
        '/api/analytics/spending-by-category'
      ),
      serverError: createServerErrorHandler(
        '/api/analytics/spending-by-category'
      ),
      empty: createEmptyResponseHandler('/api/analytics/spending-by-category'),
    },
    merchants: {
      unauthorized: createUnauthorizedHandler('/api/analytics/merchants'),
      serverError: createServerErrorHandler('/api/analytics/merchants'),
    },
    subscriptions: {
      unauthorized: createUnauthorizedHandler('/api/analytics/subscriptions'),
      serverError: createServerErrorHandler('/api/analytics/subscriptions'),
    },
  },
  dashboard: {
    summary: {
      unauthorized: createUnauthorizedHandler('/api/dashboard/summary'),
      serverError: createServerErrorHandler('/api/dashboard/summary'),
    },
  },
}

// =============================================================================
// Combined Handlers Export
// =============================================================================

/**
 * Default handlers for successful API responses
 * Use these in test setup for baseline behavior
 */
export const handlers = [...analyticsHandlers, ...dashboardHandlers]

/**
 * All available handlers including error variants
 */
export default handlers
