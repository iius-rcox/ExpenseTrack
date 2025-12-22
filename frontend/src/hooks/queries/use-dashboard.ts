import { useQuery } from '@tanstack/react-query';
import { apiFetch } from '@/services/api';
import type { DashboardMetrics, RecentActivityItem } from '@/types/api';
import type {
  ExpenseStreamItem,
  ActionQueueItem,
  CategoryBreakdownData,
} from '@/types/dashboard';

/**
 * Dashboard Query Keys
 *
 * Hierarchical key structure for cache invalidation:
 * - dashboardKeys.all invalidates everything
 * - dashboardKeys.metrics() invalidates only metrics
 * - dashboardKeys.activity() invalidates only activity stream
 */
export const dashboardKeys = {
  all: ['dashboard'] as const,
  metrics: () => [...dashboardKeys.all, 'metrics'] as const,
  activity: (limit?: number) =>
    limit
      ? ([...dashboardKeys.all, 'activity', { limit }] as const)
      : ([...dashboardKeys.all, 'activity'] as const),
  actions: () => [...dashboardKeys.all, 'actions'] as const,
  categories: () => [...dashboardKeys.all, 'categories'] as const,
};

/** Default polling interval for real-time updates (30 seconds per spec) */
const POLLING_INTERVAL = 30_000;

/**
 * Dashboard Metrics Hook (T023)
 *
 * Fetches key metrics: monthly spending, pending counts, matching percentage.
 * Polls every 30 seconds for real-time updates without page refresh.
 *
 * @param enabled - Disable polling when dashboard not visible (default: true)
 */
export function useDashboardMetrics(enabled = true) {
  return useQuery({
    queryKey: dashboardKeys.metrics(),
    queryFn: () => apiFetch<DashboardMetrics>('/dashboard/metrics'),
    staleTime: POLLING_INTERVAL,
    refetchInterval: enabled ? POLLING_INTERVAL : false,
    refetchIntervalInBackground: false, // Pause when tab not visible
  });
}

/**
 * Expense Stream Hook (T024)
 *
 * Fetches recent expense activity for the real-time feed.
 * Maps backend RecentActivityItem to our ExpenseStreamItem with confidence.
 *
 * @param limit - Number of items to fetch (default: 10)
 * @param enabled - Disable polling when not visible (default: true)
 */
export function useExpenseStream(limit = 10, enabled = true) {
  return useQuery({
    queryKey: dashboardKeys.activity(limit),
    queryFn: async () => {
      const data = await apiFetch<{ items: ExpenseStreamItem[] }>(
        `/dashboard/activity?limit=${limit}`
      );
      return data.items;
    },
    staleTime: POLLING_INTERVAL,
    refetchInterval: enabled ? POLLING_INTERVAL : false,
    refetchIntervalInBackground: false,
  });
}

/**
 * Recent Activity Hook (legacy compatibility)
 *
 * Preserved for backward compatibility with existing dashboard.
 *
 * @deprecated Use useExpenseStream for new components.
 * This hook returns loosely-typed RecentActivityItem[], while useExpenseStream
 * returns strongly-typed ExpenseStreamItem[] with confidence scores and status.
 * @see useExpenseStream
 */
export function useRecentActivity() {
  return useQuery({
    queryKey: dashboardKeys.activity(),
    queryFn: () => apiFetch<RecentActivityItem[]>('/dashboard/activity'),
    staleTime: POLLING_INTERVAL,
  });
}

/**
 * Action Queue Hook (T025)
 *
 * Fetches priority-sorted pending items requiring user attention.
 * Items include: match reviews, receipt corrections, report approvals.
 *
 * @param enabled - Disable polling when not visible (default: true)
 */
export function useActionQueue(enabled = true) {
  return useQuery({
    queryKey: dashboardKeys.actions(),
    queryFn: async () => {
      const data = await apiFetch<{ items: ActionQueueItem[] }>(
        '/dashboard/actions'
      );
      return data.items;
    },
    staleTime: POLLING_INTERVAL,
    refetchInterval: enabled ? POLLING_INTERVAL : false,
    refetchIntervalInBackground: false,
  });
}

/**
 * Category Breakdown Hook
 *
 * Fetches spending breakdown by category for the dashboard chart.
 * Less frequently updated than activity stream (1 minute stale time).
 */
export function useCategoryBreakdown() {
  return useQuery({
    queryKey: dashboardKeys.categories(),
    queryFn: async () => {
      const data = await apiFetch<{ categories: CategoryBreakdownData[] }>(
        '/analytics/categories'
      );
      return data.categories;
    },
    staleTime: 60_000, // Categories change less frequently
    refetchInterval: 60_000,
    refetchIntervalInBackground: false,
  });
}
