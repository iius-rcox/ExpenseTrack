/**
 * Analytics Query Hooks (T078)
 *
 * TanStack Query hooks for analytics data fetching.
 * Provides hooks for spending trends, merchant analytics, and subscription detection.
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/services/api'
import type {
  MonthlyComparison,
  CacheStatisticsResponse,
  CacheStatisticsApiResponse,
  TierBreakdown,
} from '@/types/api'
import type {
  AnalyticsDateRange,
  DateRangePreset,
  TimeGranularity,
  SubscriptionDetectionResponse,
  SubscriptionFrequency,
  DetectionConfidence,
  MerchantAnalyticsResponse,
} from '@/types/analytics'

// ============================================================================
// Query Keys Factory
// ============================================================================

export const analyticsKeys = {
  all: ['analytics'] as const,
  monthlyComparison: (period: string) => [...analyticsKeys.all, 'monthly', period] as const,
  cacheStats: (period: string) => [...analyticsKeys.all, 'cache', period] as const,
  spending: (startDate: string, endDate: string) => [...analyticsKeys.all, 'spending', startDate, endDate] as const,
  subscriptions: () => [...analyticsKeys.all, 'subscriptions'] as const,
  subscriptionsByParams: (params: Record<string, unknown>) => [...analyticsKeys.subscriptions(), params] as const,
  merchants: () => [...analyticsKeys.all, 'merchants'] as const,
  merchantsByParams: (params: Record<string, unknown>) => [...analyticsKeys.merchants(), params] as const,
}

// ============================================================================
// Date Range Utilities
// ============================================================================

/**
 * Get default date range (last 30 days)
 */
export function getDefaultDateRange(): AnalyticsDateRange {
  const end = new Date()
  const start = new Date()
  start.setDate(start.getDate() - 30)

  return {
    startDate: start.toISOString().split('T')[0],
    endDate: end.toISOString().split('T')[0],
    preset: 'last30days',
    label: 'Last 30 Days',
  }
}

/**
 * Get date range from preset
 */
export function getDateRangeFromPreset(preset: DateRangePreset): AnalyticsDateRange {
  const now = new Date()
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate())
  let start: Date
  let end: Date = today
  let label: string

  switch (preset) {
    case 'last7days':
      start = new Date(today)
      start.setDate(start.getDate() - 7)
      label = 'Last 7 Days'
      break
    case 'last30days':
      start = new Date(today)
      start.setDate(start.getDate() - 30)
      label = 'Last 30 Days'
      break
    case 'last90days':
      start = new Date(today)
      start.setDate(start.getDate() - 90)
      label = 'Last 90 Days'
      break
    case 'thisMonth':
      start = new Date(now.getFullYear(), now.getMonth(), 1)
      label = 'This Month'
      break
    case 'lastMonth':
      start = new Date(now.getFullYear(), now.getMonth() - 1, 1)
      end = new Date(now.getFullYear(), now.getMonth(), 0)
      label = 'Last Month'
      break
    case 'thisQuarter': {
      const quarterStart = Math.floor(now.getMonth() / 3) * 3
      start = new Date(now.getFullYear(), quarterStart, 1)
      label = 'This Quarter'
      break
    }
    case 'lastQuarter': {
      const currentQuarterStart = Math.floor(now.getMonth() / 3) * 3
      start = new Date(now.getFullYear(), currentQuarterStart - 3, 1)
      end = new Date(now.getFullYear(), currentQuarterStart, 0)
      label = 'Last Quarter'
      break
    }
    case 'thisYear':
      start = new Date(now.getFullYear(), 0, 1)
      label = 'This Year'
      break
    case 'lastYear':
      start = new Date(now.getFullYear() - 1, 0, 1)
      end = new Date(now.getFullYear() - 1, 11, 31)
      label = 'Last Year'
      break
    case 'custom':
    default:
      return getDefaultDateRange()
  }

  return {
    startDate: start.toISOString().split('T')[0],
    endDate: end.toISOString().split('T')[0],
    preset,
    label,
  }
}

// ============================================================================
// Existing Hooks (preserved)
// ============================================================================

export function useMonthlyComparison(period: string) {
  return useQuery({
    queryKey: analyticsKeys.monthlyComparison(period),
    queryFn: () => apiFetch<MonthlyComparison>(`/analytics/comparison?currentPeriod=${period}`),
    enabled: !!period,
    staleTime: 5 * 60 * 1000, // 5 minutes
  })
}

/**
 * Transform backend cache statistics to UI-friendly format.
 * Backend returns nested Overall object; UI expects flat structure with tier breakdown array.
 */
function transformCacheStatistics(api: CacheStatisticsApiResponse): CacheStatisticsResponse {
  const { overall } = api

  // Calculate overall hit rate (Tier 1 + Tier 2 hits as a percentage of total)
  // Tier 1 = cache, Tier 2 = embedding, Tier 3 = AI (most expensive)
  const hitRate = overall.totalOperations > 0
    ? ((overall.tier1Hits + overall.tier2Hits) / overall.totalOperations) * 100
    : 0

  // Build tier breakdown array
  const tierBreakdown: TierBreakdown[] = [
    {
      tier: 1,
      tierName: 'Cache',
      count: overall.tier1Hits,
      percentage: overall.tier1HitRate,
    },
    {
      tier: 2,
      tierName: 'Embedding',
      count: overall.tier2Hits,
      percentage: overall.tier2HitRate,
    },
    {
      tier: 3,
      tierName: 'AI',
      count: overall.tier3Hits,
      percentage: overall.tier3HitRate,
    },
  ]

  // Estimate cost saved: If all operations used Tier 3 (AI), cost would be ~$0.01/op
  // Actual cost is estimatedMonthlyCost. Saved = (total * $0.01) - actual
  const costPerAICall = 0.01
  const maxCost = overall.totalOperations * costPerAICall
  const estimatedCostSaved = Math.max(0, maxCost - overall.estimatedMonthlyCost)

  return {
    period: api.period,
    totalOperations: overall.totalOperations,
    hitRate,
    estimatedCostSaved,
    tierBreakdown,
    avgResponseTimeMs: overall.avgResponseTimeMs,
    belowTarget: overall.belowTarget,
  }
}

export function useCacheStatistics(period: string) {
  return useQuery({
    queryKey: analyticsKeys.cacheStats(period),
    queryFn: async () => {
      const response = await apiFetch<CacheStatisticsApiResponse>(`/analytics/cache-stats?period=${period}`)
      return transformCacheStatistics(response)
    },
    enabled: !!period,
    staleTime: 5 * 60 * 1000,
  })
}

interface SpendingByCategoryItem {
  category: string
  amount: number
  transactionCount: number
  percentageOfTotal: number
}

interface SpendingByVendorItem {
  vendorName: string
  amount: number
  transactionCount: number
  percentageOfTotal: number
}

interface SpendingTrendItem {
  date: string
  amount: number
  transactionCount: number
}

export function useSpendingByCategory(startDate: string, endDate: string) {
  return useQuery({
    queryKey: [...analyticsKeys.spending(startDate, endDate), 'category'],
    queryFn: () => apiFetch<SpendingByCategoryItem[]>(
      `/analytics/spending-by-category?startDate=${startDate}&endDate=${endDate}`
    ),
    enabled: !!startDate && !!endDate,
    staleTime: 5 * 60 * 1000,
  })
}

export function useSpendingByVendor(startDate: string, endDate: string) {
  return useQuery({
    queryKey: [...analyticsKeys.spending(startDate, endDate), 'vendor'],
    queryFn: () => apiFetch<SpendingByVendorItem[]>(
      `/analytics/spending-by-vendor?startDate=${startDate}&endDate=${endDate}`
    ),
    enabled: !!startDate && !!endDate,
    staleTime: 5 * 60 * 1000,
  })
}

export function useSpendingTrend(startDate: string, endDate: string, granularity: 'day' | 'week' | 'month' = 'day') {
  return useQuery({
    queryKey: [...analyticsKeys.spending(startDate, endDate), 'trend', granularity],
    queryFn: () => apiFetch<SpendingTrendItem[]>(
      `/analytics/spending-trend?startDate=${startDate}&endDate=${endDate}&granularity=${granularity}`
    ),
    enabled: !!startDate && !!endDate,
    staleTime: 5 * 60 * 1000,
  })
}

// ============================================================================
// Subscription Detection Hooks (T078)
// ============================================================================

interface SubscriptionQueryParams {
  minConfidence?: DetectionConfidence
  frequency?: SubscriptionFrequency[]
  includeAcknowledged?: boolean
}

/**
 * Hook for fetching detected subscriptions
 */
export function useSubscriptionDetection(options: SubscriptionQueryParams = {}) {
  const { minConfidence, frequency, includeAcknowledged = true } = options

  const params = { minConfidence, frequency, includeAcknowledged }

  return useQuery({
    queryKey: analyticsKeys.subscriptionsByParams(params),
    queryFn: async () => {
      const searchParams = new URLSearchParams()
      if (minConfidence) searchParams.set('minConfidence', minConfidence)
      if (frequency?.length) {
        frequency.forEach((f) => searchParams.append('frequency', f))
      }
      if (!includeAcknowledged) searchParams.set('includeAcknowledged', 'false')

      const queryString = searchParams.toString()
      return apiFetch<SubscriptionDetectionResponse>(
        `/analytics/subscriptions${queryString ? `?${queryString}` : ''}`
      )
    },
    staleTime: 10 * 60 * 1000, // 10 minutes - subscription data changes rarely
  })
}

/**
 * Hook for acknowledging/unacknowledging subscriptions
 */
export function useAcknowledgeSubscription() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ subscriptionId, acknowledged }: { subscriptionId: string; acknowledged: boolean }) => {
      return apiFetch<void>(`/analytics/subscriptions/${subscriptionId}/acknowledge`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ acknowledged }),
      })
    },
    onMutate: async ({ subscriptionId, acknowledged }) => {
      await queryClient.cancelQueries({ queryKey: analyticsKeys.subscriptions() })

      const previousData = queryClient.getQueriesData({ queryKey: analyticsKeys.subscriptions() })

      queryClient.setQueriesData(
        { queryKey: analyticsKeys.subscriptions() },
        (old: SubscriptionDetectionResponse | undefined) => {
          if (!old) return old
          return {
            ...old,
            subscriptions: old.subscriptions.map((sub) =>
              sub.id === subscriptionId ? { ...sub, isAcknowledged: acknowledged } : sub
            ),
          }
        }
      )

      return { previousData }
    },
    onError: (_error, _variables, context) => {
      if (context?.previousData) {
        context.previousData.forEach(([queryKey, data]) => {
          queryClient.setQueryData(queryKey, data)
        })
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: analyticsKeys.subscriptions() })
    },
  })
}

/**
 * Hook for triggering subscription detection analysis
 */
export function useTriggerSubscriptionAnalysis() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async () => {
      return apiFetch<{ detected: number; analyzed: number }>('/analytics/subscriptions/analyze', {
        method: 'POST',
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: analyticsKeys.subscriptions() })
    },
  })
}

// ============================================================================
// Enhanced Merchant Analytics Hooks (T078)
// ============================================================================

interface MerchantAnalyticsParams {
  startDate: string
  endDate: string
  topCount?: number
  includeComparison?: boolean
}

/**
 * Hook for fetching detailed merchant analytics
 */
export function useMerchantAnalytics(params: MerchantAnalyticsParams) {
  const { startDate, endDate, topCount = 10, includeComparison = true } = params

  return useQuery({
    queryKey: analyticsKeys.merchantsByParams({ startDate, endDate, topCount, includeComparison }),
    queryFn: async () => {
      const searchParams = new URLSearchParams()
      searchParams.set('startDate', startDate)
      searchParams.set('endDate', endDate)
      searchParams.set('topCount', String(topCount))
      if (includeComparison) searchParams.set('includeComparison', 'true')

      return apiFetch<MerchantAnalyticsResponse>(`/analytics/merchants?${searchParams}`)
    },
    enabled: !!startDate && !!endDate,
    staleTime: 5 * 60 * 1000,
  })
}

// ============================================================================
// Combined Dashboard Hook (T078)
// ============================================================================

interface AnalyticsDashboardParams {
  dateRange: AnalyticsDateRange
  granularity?: TimeGranularity
}

/**
 * Combined hook for the analytics dashboard
 * Fetches all required data in parallel
 */
export function useAnalyticsDashboard(params: AnalyticsDashboardParams) {
  const { dateRange, granularity = 'day' } = params
  const { startDate, endDate } = dateRange

  // Map broader granularity to what useSpendingTrend supports
  const trendGranularity: 'day' | 'week' | 'month' =
    granularity === 'quarter' || granularity === 'year' ? 'month' : granularity

  const trendsQuery = useSpendingTrend(startDate, endDate, trendGranularity)
  const categoriesQuery = useSpendingByCategory(startDate, endDate)
  const vendorsQuery = useSpendingByVendor(startDate, endDate)
  const subscriptionsQuery = useSubscriptionDetection({ minConfidence: 'medium' })

  const isLoading =
    trendsQuery.isLoading ||
    categoriesQuery.isLoading ||
    vendorsQuery.isLoading ||
    subscriptionsQuery.isLoading

  const hasError =
    trendsQuery.isError ||
    categoriesQuery.isError ||
    vendorsQuery.isError ||
    subscriptionsQuery.isError

  return {
    // Data
    trends: trendsQuery.data,
    categories: categoriesQuery.data,
    vendors: vendorsQuery.data,
    subscriptions: subscriptionsQuery.data,

    // Status
    isLoading,
    hasError,
    errors: {
      trends: trendsQuery.error,
      categories: categoriesQuery.error,
      vendors: vendorsQuery.error,
      subscriptions: subscriptionsQuery.error,
    },

    // Individual loading states for progressive UI
    isTrendsLoading: trendsQuery.isLoading,
    isCategoriesLoading: categoriesQuery.isLoading,
    isVendorsLoading: vendorsQuery.isLoading,
    isSubscriptionsLoading: subscriptionsQuery.isLoading,

    // Refetch functions
    refetchAll: async () => {
      await Promise.all([
        trendsQuery.refetch(),
        categoriesQuery.refetch(),
        vendorsQuery.refetch(),
        subscriptionsQuery.refetch(),
      ])
    },
  }
}
