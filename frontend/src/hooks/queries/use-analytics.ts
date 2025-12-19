import { useQuery } from '@tanstack/react-query'
import { apiFetch } from '@/services/api'
import type {
  MonthlyComparison,
  CacheStatisticsResponse,
} from '@/types/api'

export const analyticsKeys = {
  all: ['analytics'] as const,
  monthlyComparison: (period: string) => [...analyticsKeys.all, 'monthly', period] as const,
  cacheStats: (period: string) => [...analyticsKeys.all, 'cache', period] as const,
  spending: (startDate: string, endDate: string) => [...analyticsKeys.all, 'spending', startDate, endDate] as const,
}

export function useMonthlyComparison(period: string) {
  return useQuery({
    queryKey: analyticsKeys.monthlyComparison(period),
    queryFn: () => apiFetch<MonthlyComparison>(`/api/analytics/monthly-comparison?period=${period}`),
    enabled: !!period,
    staleTime: 5 * 60 * 1000, // 5 minutes
  })
}

export function useCacheStatistics(period: string) {
  return useQuery({
    queryKey: analyticsKeys.cacheStats(period),
    queryFn: () => apiFetch<CacheStatisticsResponse>(`/api/analytics/cache-stats?period=${period}`),
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
      `/api/analytics/spending-by-category?startDate=${startDate}&endDate=${endDate}`
    ),
    enabled: !!startDate && !!endDate,
    staleTime: 5 * 60 * 1000,
  })
}

export function useSpendingByVendor(startDate: string, endDate: string) {
  return useQuery({
    queryKey: [...analyticsKeys.spending(startDate, endDate), 'vendor'],
    queryFn: () => apiFetch<SpendingByVendorItem[]>(
      `/api/analytics/spending-by-vendor?startDate=${startDate}&endDate=${endDate}`
    ),
    enabled: !!startDate && !!endDate,
    staleTime: 5 * 60 * 1000,
  })
}

export function useSpendingTrend(startDate: string, endDate: string, granularity: 'day' | 'week' | 'month' = 'day') {
  return useQuery({
    queryKey: [...analyticsKeys.spending(startDate, endDate), 'trend', granularity],
    queryFn: () => apiFetch<SpendingTrendItem[]>(
      `/api/analytics/spending-trend?startDate=${startDate}&endDate=${endDate}&granularity=${granularity}`
    ),
    enabled: !!startDate && !!endDate,
    staleTime: 5 * 60 * 1000,
  })
}
