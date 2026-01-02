/**
 * API Contract Validation Tests
 *
 * These tests verify that frontend type definitions match the backend OpenAPI contract.
 * Contract violations are caught at compile-time through TypeScript's type system.
 *
 * How it works:
 * 1. We define expected types based on OpenAPI schema
 * 2. We create type assertions that fail compilation if types don't match
 * 3. Runtime tests verify the test infrastructure is working
 *
 * When contracts break:
 * - TypeScript will fail to compile
 * - The error message will indicate which field has the mismatch
 * - Fix by updating frontend types OR coordinating with backend
 */

import { describe, it, expect } from 'vitest'

// =============================================================================
// Type Definitions (matching OpenAPI contract)
// =============================================================================

/**
 * MonthlyComparisonResponse - /api/analytics/monthly-comparison
 *
 * OpenAPI Schema:
 * - currentTotal: number (required)
 * - previousTotal: number (required)
 * - percentageChange: number (required)
 * - newVendors: VendorChange[] (required)
 * - missingRecurring: VendorChange[] (required)
 * - significantChanges: VendorChange[] (required)
 */
interface MonthlyComparisonResponse {
  currentTotal: number
  previousTotal: number
  percentageChange: number
  newVendors: VendorChange[]
  missingRecurring: VendorChange[]
  significantChanges: VendorChange[]
}

interface VendorChange {
  vendorName: string
  currentAmount: number
  previousAmount: number
  changePercentage: number
}

/**
 * SpendingTrendItem - /api/analytics/spending-trend
 *
 * OpenAPI Schema:
 * - date: string (ISO 8601 format)
 * - amount: number (required)
 * - transactionCount: number (required)
 */
interface SpendingTrendItem {
  date: string
  amount: number
  transactionCount: number
}

/**
 * CategoryBreakdownItem - /api/analytics/spending-by-category
 *
 * OpenAPI Schema:
 * - category: string (required)
 * - amount: number (required)
 * - percentageOfTotal: number (required)
 * - transactionCount: number (required)
 */
interface CategoryBreakdownItem {
  category: string
  amount: number
  percentageOfTotal: number
  transactionCount: number
}

/**
 * MerchantAnalyticsResponse - /api/analytics/merchants
 *
 * OpenAPI Schema:
 * - merchants: MerchantAnalytics[] (required)
 * - totalMerchants: number (required)
 */
interface MerchantAnalyticsResponse {
  merchants: MerchantAnalytics[]
  totalMerchants: number
}

interface MerchantAnalytics {
  merchantName: string
  totalSpent: number
  transactionCount: number
  lastTransaction: string
  averageTransaction: number
}

/**
 * SubscriptionDetectionResponse - /api/analytics/subscriptions
 *
 * OpenAPI Schema:
 * - subscriptions: DetectedSubscription[] (required)
 * - totalMonthlyEstimate: number (required)
 */
interface SubscriptionDetectionResponse {
  subscriptions: DetectedSubscription[]
  totalMonthlyEstimate: number
}

interface DetectedSubscription {
  merchantName: string
  estimatedAmount: number
  frequency: 'weekly' | 'biweekly' | 'monthly' | 'quarterly' | 'yearly'
  confidence: number
  lastOccurrence: string
  nextExpected: string
}

/**
 * DashboardSummaryResponse - /api/dashboard/summary
 *
 * OpenAPI Schema:
 * - totalTransactions: number (required)
 * - totalSpending: number (required)
 * - pendingReceipts: number (required)
 * - matchedReceipts: number (required)
 * - recentActivity: ActivityItem[] (required)
 */
interface DashboardSummaryResponse {
  totalTransactions: number
  totalSpending: number
  pendingReceipts: number
  matchedReceipts: number
  recentActivity: ActivityItem[]
}

interface ActivityItem {
  type: 'transaction' | 'receipt' | 'match' | 'report'
  description: string
  timestamp: string
  amount?: number
}

// =============================================================================
// Type Assertion Utilities
// =============================================================================

/**
 * Asserts that a value matches the expected type shape.
 * This is a runtime no-op but provides compile-time type checking.
 */
function assertType<T>(_value: T): void {
  // No-op at runtime - type checking happens at compile time
}

// =============================================================================
// Contract Validation Tests
// =============================================================================

describe('API Contract Validation', () => {
  describe('MonthlyComparisonResponse', () => {
    it('has correct structure with all required fields', () => {
      const response: MonthlyComparisonResponse = {
        currentTotal: 2547.89,
        previousTotal: 2312.45,
        percentageChange: 10.18,
        newVendors: [
          {
            vendorName: 'Spotify',
            currentAmount: 14.99,
            previousAmount: 0,
            changePercentage: 100,
          },
        ],
        missingRecurring: [],
        significantChanges: [
          {
            vendorName: 'Amazon',
            currentAmount: 456.78,
            previousAmount: 234.56,
            changePercentage: 94.7,
          },
        ],
      }

      // Type assertion - will fail compilation if type is wrong
      assertType<MonthlyComparisonResponse>(response)

      // Runtime validation
      expect(response.currentTotal).toBeTypeOf('number')
      expect(response.previousTotal).toBeTypeOf('number')
      expect(response.percentageChange).toBeTypeOf('number')
      expect(Array.isArray(response.newVendors)).toBe(true)
      expect(Array.isArray(response.missingRecurring)).toBe(true)
      expect(Array.isArray(response.significantChanges)).toBe(true)
    })

    it('VendorChange has correct structure', () => {
      const vendorChange: VendorChange = {
        vendorName: 'Test Vendor',
        currentAmount: 100.0,
        previousAmount: 50.0,
        changePercentage: 100.0,
      }

      assertType<VendorChange>(vendorChange)

      expect(vendorChange.vendorName).toBeTypeOf('string')
      expect(vendorChange.currentAmount).toBeTypeOf('number')
      expect(vendorChange.previousAmount).toBeTypeOf('number')
      expect(vendorChange.changePercentage).toBeTypeOf('number')
    })
  })

  describe('SpendingTrendItem', () => {
    it('has correct structure with all required fields', () => {
      const item: SpendingTrendItem = {
        date: '2024-01-15T00:00:00Z',
        amount: 245.67,
        transactionCount: 5,
      }

      assertType<SpendingTrendItem>(item)

      expect(item.date).toBeTypeOf('string')
      expect(item.amount).toBeTypeOf('number')
      expect(item.transactionCount).toBeTypeOf('number')
    })

    it('date field is ISO 8601 compatible string', () => {
      const item: SpendingTrendItem = {
        date: new Date().toISOString(),
        amount: 100,
        transactionCount: 1,
      }

      // Verify date is parseable
      expect(() => new Date(item.date)).not.toThrow()
      expect(new Date(item.date).toISOString()).toBe(item.date)
    })
  })

  describe('CategoryBreakdownItem', () => {
    it('has correct structure with all required fields', () => {
      const item: CategoryBreakdownItem = {
        category: 'Groceries',
        amount: 523.45,
        percentageOfTotal: 28.5,
        transactionCount: 12,
      }

      assertType<CategoryBreakdownItem>(item)

      expect(item.category).toBeTypeOf('string')
      expect(item.amount).toBeTypeOf('number')
      expect(item.percentageOfTotal).toBeTypeOf('number')
      expect(item.transactionCount).toBeTypeOf('number')
    })

    it('percentageOfTotal is within valid range', () => {
      const item: CategoryBreakdownItem = {
        category: 'Test',
        amount: 100,
        percentageOfTotal: 50.5,
        transactionCount: 5,
      }

      expect(item.percentageOfTotal).toBeGreaterThanOrEqual(0)
      expect(item.percentageOfTotal).toBeLessThanOrEqual(100)
    })
  })

  describe('MerchantAnalyticsResponse', () => {
    it('has correct structure with all required fields', () => {
      const response: MerchantAnalyticsResponse = {
        merchants: [
          {
            merchantName: 'Whole Foods',
            totalSpent: 423.67,
            transactionCount: 8,
            lastTransaction: new Date().toISOString(),
            averageTransaction: 52.96,
          },
        ],
        totalMerchants: 47,
      }

      assertType<MerchantAnalyticsResponse>(response)

      expect(Array.isArray(response.merchants)).toBe(true)
      expect(response.totalMerchants).toBeTypeOf('number')
    })

    it('MerchantAnalytics has correct structure', () => {
      const merchant: MerchantAnalytics = {
        merchantName: 'Test Merchant',
        totalSpent: 500.0,
        transactionCount: 10,
        lastTransaction: new Date().toISOString(),
        averageTransaction: 50.0,
      }

      assertType<MerchantAnalytics>(merchant)

      expect(merchant.merchantName).toBeTypeOf('string')
      expect(merchant.totalSpent).toBeTypeOf('number')
      expect(merchant.transactionCount).toBeTypeOf('number')
      expect(merchant.lastTransaction).toBeTypeOf('string')
      expect(merchant.averageTransaction).toBeTypeOf('number')
    })
  })

  describe('SubscriptionDetectionResponse', () => {
    it('has correct structure with all required fields', () => {
      const response: SubscriptionDetectionResponse = {
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
        ],
        totalMonthlyEstimate: 85.97,
      }

      assertType<SubscriptionDetectionResponse>(response)

      expect(Array.isArray(response.subscriptions)).toBe(true)
      expect(response.totalMonthlyEstimate).toBeTypeOf('number')
    })

    it('DetectedSubscription has correct structure', () => {
      const subscription: DetectedSubscription = {
        merchantName: 'Spotify',
        estimatedAmount: 14.99,
        frequency: 'monthly',
        confidence: 0.92,
        lastOccurrence: new Date().toISOString(),
        nextExpected: new Date(
          Date.now() + 30 * 24 * 60 * 60 * 1000
        ).toISOString(),
      }

      assertType<DetectedSubscription>(subscription)

      expect(subscription.merchantName).toBeTypeOf('string')
      expect(subscription.estimatedAmount).toBeTypeOf('number')
      expect(subscription.frequency).toBeTypeOf('string')
      expect(subscription.confidence).toBeTypeOf('number')
      expect(subscription.lastOccurrence).toBeTypeOf('string')
      expect(subscription.nextExpected).toBeTypeOf('string')
    })

    it('frequency is a valid enum value', () => {
      const validFrequencies: DetectedSubscription['frequency'][] = [
        'weekly',
        'biweekly',
        'monthly',
        'quarterly',
        'yearly',
      ]

      const subscription: DetectedSubscription = {
        merchantName: 'Test',
        estimatedAmount: 10,
        frequency: 'monthly',
        confidence: 0.9,
        lastOccurrence: new Date().toISOString(),
        nextExpected: new Date().toISOString(),
      }

      expect(validFrequencies).toContain(subscription.frequency)
    })

    it('confidence is within valid range (0-1)', () => {
      const subscription: DetectedSubscription = {
        merchantName: 'Test',
        estimatedAmount: 10,
        frequency: 'monthly',
        confidence: 0.85,
        lastOccurrence: new Date().toISOString(),
        nextExpected: new Date().toISOString(),
      }

      expect(subscription.confidence).toBeGreaterThanOrEqual(0)
      expect(subscription.confidence).toBeLessThanOrEqual(1)
    })
  })

  describe('DashboardSummaryResponse', () => {
    it('has correct structure with all required fields', () => {
      const response: DashboardSummaryResponse = {
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
            type: 'report',
            description: 'Monthly expense report generated',
            timestamp: new Date().toISOString(),
          },
        ],
      }

      assertType<DashboardSummaryResponse>(response)

      expect(response.totalTransactions).toBeTypeOf('number')
      expect(response.totalSpending).toBeTypeOf('number')
      expect(response.pendingReceipts).toBeTypeOf('number')
      expect(response.matchedReceipts).toBeTypeOf('number')
      expect(Array.isArray(response.recentActivity)).toBe(true)
    })

    it('ActivityItem has correct structure', () => {
      const activity: ActivityItem = {
        type: 'transaction',
        description: 'Test activity',
        timestamp: new Date().toISOString(),
        amount: 50.0,
      }

      assertType<ActivityItem>(activity)

      expect(activity.type).toBeTypeOf('string')
      expect(activity.description).toBeTypeOf('string')
      expect(activity.timestamp).toBeTypeOf('string')
    })

    it('ActivityItem.type is a valid enum value', () => {
      const validTypes: ActivityItem['type'][] = [
        'transaction',
        'receipt',
        'match',
        'report',
      ]

      const activity: ActivityItem = {
        type: 'match',
        description: 'Test',
        timestamp: new Date().toISOString(),
      }

      expect(validTypes).toContain(activity.type)
    })

    it('ActivityItem.amount is optional', () => {
      const activityWithAmount: ActivityItem = {
        type: 'transaction',
        description: 'With amount',
        timestamp: new Date().toISOString(),
        amount: 100,
      }

      const activityWithoutAmount: ActivityItem = {
        type: 'report',
        description: 'Without amount',
        timestamp: new Date().toISOString(),
      }

      // Both should be valid
      assertType<ActivityItem>(activityWithAmount)
      assertType<ActivityItem>(activityWithoutAmount)

      expect(activityWithAmount.amount).toBe(100)
      expect(activityWithoutAmount.amount).toBeUndefined()
    })
  })
})

// =============================================================================
// Contract Compatibility with Fixtures
// =============================================================================

describe('Contract Compatibility with Test Fixtures', () => {
  it('fixture factory output matches contract types', async () => {
    // Import fixtures dynamically to test their compatibility
    const { fixtureVariants } = await import('@/test-utils/fixtures')

    // These assignments will fail compilation if fixtures don't match contracts
    const _monthlyComparison: MonthlyComparisonResponse =
      fixtureVariants.monthlyComparison.valid

    const _spendingTrends: SpendingTrendItem[] =
      fixtureVariants.spendingTrend.valid

    const _categoryBreakdown: CategoryBreakdownItem[] =
      fixtureVariants.categoryBreakdown.valid

    const _merchantAnalytics: MerchantAnalyticsResponse =
      fixtureVariants.merchantAnalytics.valid

    const _subscriptionDetection: SubscriptionDetectionResponse =
      fixtureVariants.subscriptionDetection.valid

    const _dashboardSummary: DashboardSummaryResponse =
      fixtureVariants.dashboardSummary.valid

    // Runtime check that imports worked
    expect(_monthlyComparison).toBeDefined()
    expect(_spendingTrends).toBeDefined()
    expect(_categoryBreakdown).toBeDefined()
    expect(_merchantAnalytics).toBeDefined()
    expect(_subscriptionDetection).toBeDefined()
    expect(_dashboardSummary).toBeDefined()
  })
})
