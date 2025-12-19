import { useQuery } from '@tanstack/react-query'
import { apiFetch } from '@/services/api'
import type { DashboardMetrics, RecentActivityItem } from '@/types/api'

export const dashboardKeys = {
  all: ['dashboard'] as const,
  metrics: () => [...dashboardKeys.all, 'metrics'] as const,
  activity: () => [...dashboardKeys.all, 'activity'] as const,
}

export function useDashboardMetrics() {
  return useQuery({
    queryKey: dashboardKeys.metrics(),
    queryFn: () => apiFetch<DashboardMetrics>('/dashboard/metrics'),
    staleTime: 30_000, // 30 seconds
  })
}

export function useRecentActivity() {
  return useQuery({
    queryKey: dashboardKeys.activity(),
    queryFn: () => apiFetch<RecentActivityItem[]>('/dashboard/activity'),
    staleTime: 30_000,
  })
}
