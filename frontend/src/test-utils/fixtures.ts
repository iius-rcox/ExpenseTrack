/**
 * Test Fixture Factories
 *
 * Factory functions for creating test data that matches API contracts.
 * Use these to generate consistent, type-safe mock data for tests.
 */

// =============================================================================
// Type Definitions (matching API contracts)
// =============================================================================

export interface VendorChange {
  vendorName: string
  currentAmount: number
  previousAmount: number
  changePercentage: number
}

export interface MonthlyComparisonResponse {
  currentTotal: number
  previousTotal: number
  percentageChange: number
  newVendors: VendorChange[]
  missingRecurring: VendorChange[]
  significantChanges: VendorChange[]
}

export interface SpendingTrendItem {
  date: string
  amount: number
  transactionCount: number
}

export interface CategoryBreakdownItem {
  category: string
  amount: number
  percentageOfTotal: number
  transactionCount: number
}

export interface MerchantItem {
  merchantName: string
  totalSpent: number
  transactionCount: number
  lastTransaction: string
  averageTransaction: number
}

export interface MerchantAnalyticsResponse {
  merchants: MerchantItem[]
  totalMerchants: number
}

export interface SubscriptionItem {
  merchantName: string
  estimatedAmount: number
  frequency: 'weekly' | 'biweekly' | 'monthly' | 'quarterly' | 'yearly'
  confidence: number
  lastOccurrence: string
  nextExpected: string
}

export interface SubscriptionDetectionResponse {
  subscriptions: SubscriptionItem[]
  totalMonthlyEstimate: number
}

export interface ActivityItem {
  type: 'transaction' | 'receipt' | 'report' | 'match'
  description: string
  timestamp: string
  amount?: number
}

export interface DashboardSummaryResponse {
  totalTransactions: number
  totalSpending: number
  pendingReceipts: number
  matchedReceipts: number
  recentActivity: ActivityItem[]
}

export interface ApiErrorResponse {
  type: string
  title: string
  status: number
  detail?: string
  instance?: string
}

// =============================================================================
// Analytics Fixture Factories
// =============================================================================

/**
 * Create a monthly comparison response
 */
export function createMonthlyComparison(
  overrides: Partial<MonthlyComparisonResponse> = {}
): MonthlyComparisonResponse {
  return {
    currentTotal: 2547.89,
    previousTotal: 2312.45,
    percentageChange: 10.18,
    newVendors: [],
    missingRecurring: [],
    significantChanges: [],
    ...overrides,
  }
}

/**
 * Create spending trend data for a specified number of days
 */
export function createSpendingTrend(days: number = 14): SpendingTrendItem[] {
  return Array.from({ length: days }, (_, i) => {
    const date = new Date()
    date.setDate(date.getDate() - (days - i - 1))
    return {
      date: date.toISOString(),
      amount: 100 + Math.random() * 150,
      transactionCount: 2 + Math.floor(Math.random() * 6),
    }
  })
}

/**
 * Create category breakdown data
 */
export function createCategoryBreakdown(
  count: number = 6
): CategoryBreakdownItem[] {
  const categories = [
    'Groceries',
    'Transportation',
    'Dining',
    'Entertainment',
    'Utilities',
    'Shopping',
    'Healthcare',
    'Travel',
    'Subscriptions',
    'Other',
  ]

  return categories.slice(0, count).map((category, index) => {
    const amount = 100 + Math.random() * 500
    return {
      category,
      amount,
      percentageOfTotal: (100 / count) - (index * 2) + Math.random() * 4,
      transactionCount: 3 + Math.floor(Math.random() * 10),
    }
  })
}

/**
 * Create merchant analytics response
 */
export function createMerchantAnalytics(
  count: number = 5
): MerchantAnalyticsResponse {
  const merchantNames = [
    'Whole Foods',
    'Amazon',
    'Uber',
    'Netflix',
    'Starbucks',
    'Target',
    'Costco',
    'Apple',
    'Google',
    'Spotify',
  ]

  const merchants: MerchantItem[] = merchantNames.slice(0, count).map((name) => ({
    merchantName: name,
    totalSpent: 50 + Math.random() * 400,
    transactionCount: 2 + Math.floor(Math.random() * 15),
    lastTransaction: new Date().toISOString(),
    averageTransaction: 10 + Math.random() * 50,
  }))

  return {
    merchants,
    totalMerchants: 47,
  }
}

/**
 * Create subscription detection response
 */
export function createSubscriptions(
  count: number = 3
): SubscriptionDetectionResponse {
  const subscriptionTemplates = [
    { name: 'Netflix', amount: 15.99, frequency: 'monthly' as const },
    { name: 'Spotify', amount: 14.99, frequency: 'monthly' as const },
    { name: 'Adobe Creative Cloud', amount: 54.99, frequency: 'monthly' as const },
    { name: 'Amazon Prime', amount: 139.0, frequency: 'yearly' as const },
    { name: 'Gym Membership', amount: 49.99, frequency: 'monthly' as const },
    { name: 'iCloud Storage', amount: 2.99, frequency: 'monthly' as const },
  ]

  const subscriptions: SubscriptionItem[] = subscriptionTemplates
    .slice(0, count)
    .map((template) => ({
      merchantName: template.name,
      estimatedAmount: template.amount,
      frequency: template.frequency,
      confidence: 0.85 + Math.random() * 0.15,
      lastOccurrence: new Date().toISOString(),
      nextExpected: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString(),
    }))

  const monthlyTotal = subscriptions.reduce((sum, sub) => {
    const multiplier =
      sub.frequency === 'yearly'
        ? 1 / 12
        : sub.frequency === 'quarterly'
          ? 1 / 3
          : sub.frequency === 'biweekly'
            ? 2
            : sub.frequency === 'weekly'
              ? 4
              : 1
    return sum + sub.estimatedAmount * multiplier
  }, 0)

  return {
    subscriptions,
    totalMonthlyEstimate: Math.round(monthlyTotal * 100) / 100,
  }
}

// =============================================================================
// Dashboard Fixture Factories
// =============================================================================

/**
 * Create dashboard summary response
 */
export function createDashboardSummary(
  overrides: Partial<DashboardSummaryResponse> = {}
): DashboardSummaryResponse {
  return {
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
        description: 'Receipt uploaded',
        timestamp: new Date(Date.now() - 3600000).toISOString(),
        amount: 45.67,
      },
      {
        type: 'match',
        description: 'Receipt matched',
        timestamp: new Date(Date.now() - 7200000).toISOString(),
        amount: 123.45,
      },
    ],
    ...overrides,
  }
}

// =============================================================================
// Error Fixture Factories
// =============================================================================

/**
 * Create an API error response (RFC 7807 ProblemDetails)
 */
export function createApiError(
  status: number,
  title: string,
  overrides: Partial<ApiErrorResponse> = {}
): ApiErrorResponse {
  const typeMap: Record<number, string> = {
    400: 'https://tools.ietf.org/html/rfc7231#section-6.5.1',
    401: 'https://tools.ietf.org/html/rfc7235#section-3.1',
    403: 'https://tools.ietf.org/html/rfc7231#section-6.5.3',
    404: 'https://tools.ietf.org/html/rfc7231#section-6.5.4',
    500: 'https://tools.ietf.org/html/rfc7807',
  }

  return {
    type: typeMap[status] || 'https://tools.ietf.org/html/rfc7807',
    title,
    status,
    ...overrides,
  }
}

// =============================================================================
// Fixture Variants for Edge Case Testing
// =============================================================================

export const fixtureVariants = {
  monthlyComparison: {
    /** Standard successful response */
    valid: createMonthlyComparison(),

    /** Empty arrays for optional fields */
    empty: createMonthlyComparison({
      newVendors: [],
      missingRecurring: [],
      significantChanges: [],
    }),

    /** Only required fields */
    minimal: {
      currentTotal: 0,
      previousTotal: 0,
      percentageChange: 0,
      newVendors: [],
      missingRecurring: [],
      significantChanges: [],
    } as MonthlyComparisonResponse,

    /** Zero spending (edge case) */
    zeroSpending: createMonthlyComparison({
      currentTotal: 0,
      previousTotal: 0,
      percentageChange: 0,
    }),

    /** Negative change */
    negativeChange: createMonthlyComparison({
      currentTotal: 2000,
      previousTotal: 2500,
      percentageChange: -20,
    }),
  },

  spendingTrend: {
    valid: createSpendingTrend(14),
    empty: [] as SpendingTrendItem[],
    singleDay: createSpendingTrend(1),
    longPeriod: createSpendingTrend(90),
  },

  categoryBreakdown: {
    valid: createCategoryBreakdown(6),
    empty: [] as CategoryBreakdownItem[],
    singleCategory: createCategoryBreakdown(1),
  },

  merchantAnalytics: {
    valid: createMerchantAnalytics(5),
    empty: { merchants: [], totalMerchants: 0 } as MerchantAnalyticsResponse,
    singleMerchant: createMerchantAnalytics(1),
  },

  subscriptions: {
    valid: createSubscriptions(3),
    empty: {
      subscriptions: [],
      totalMonthlyEstimate: 0,
    } as SubscriptionDetectionResponse,
    singleSubscription: createSubscriptions(1),
  },

  /** Alias for subscriptionDetection (contract test compatibility) */
  subscriptionDetection: {
    valid: createSubscriptions(3),
    empty: {
      subscriptions: [],
      totalMonthlyEstimate: 0,
    } as SubscriptionDetectionResponse,
    singleSubscription: createSubscriptions(1),
  },

  dashboardSummary: {
    valid: createDashboardSummary(),
    empty: createDashboardSummary({
      totalTransactions: 0,
      totalSpending: 0,
      pendingReceipts: 0,
      matchedReceipts: 0,
      recentActivity: [],
    }),
  },

  errors: {
    unauthorized: createApiError(401, 'Unauthorized', {
      detail: 'Bearer token is missing or expired',
    }),
    forbidden: createApiError(403, 'Forbidden', {
      detail: 'You do not have permission to access this resource',
    }),
    notFound: createApiError(404, 'Not Found', {
      detail: 'The requested resource was not found',
    }),
    serverError: createApiError(500, 'Internal Server Error', {
      detail: 'An unexpected error occurred',
    }),
  },
}
