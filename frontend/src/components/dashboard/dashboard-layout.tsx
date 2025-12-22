/**
 * DashboardLayout Component (T031)
 *
 * Orchestrates all dashboard components in a responsive layout.
 * Implements the "Refined Intelligence" command center design.
 *
 * Layout structure:
 * - Top: MetricsRow (key stats)
 * - Middle: Two-column grid (ExpenseStream + ActionQueue)
 * - Bottom: CategoryBreakdown
 *
 * Responsive behavior:
 * - Desktop: Multi-column layout
 * - Tablet: Stacked two-column sections
 * - Mobile: Single column with summary bar
 */

import { motion } from 'framer-motion';
import { RefreshCcw, Settings } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import {
  useDashboardMetrics,
  useExpenseStream,
  useActionQueue,
  useCategoryBreakdown,
} from '@/hooks/queries/use-dashboard';
import { MetricsRow, MetricsSummaryBar } from './metrics-row';
import { ExpenseStream } from './expense-stream';
import { ActionQueue, ActionQueueBadge } from './action-queue';
import { CategoryBreakdown } from './category-breakdown';
import { MobileNav, MobileNavSpacer } from '@/components/mobile';
import { cn } from '@/lib/utils';
import { staggerContainer, listItemVariants } from '@/lib/animations';

interface DashboardLayoutProps {
  /** Additional CSS classes */
  className?: string;
}

/**
 * Main dashboard layout component.
 */
export function DashboardLayout({ className }: DashboardLayoutProps) {
  // Fetch dashboard data with polling
  const {
    data: metrics,
    isLoading: metricsLoading,
    error: metricsError,
    refetch: refetchMetrics,
    isRefetching: metricsRefetching,
  } = useDashboardMetrics();

  const {
    data: streamItems,
    isLoading: streamLoading,
    refetch: refetchStream,
    isRefetching: streamRefetching,
  } = useExpenseStream(10);

  const {
    data: actionItems,
    isLoading: actionsLoading,
    refetch: refetchActions,
    isRefetching: actionsRefetching,
  } = useActionQueue();

  const {
    data: categories,
    isLoading: categoriesLoading,
  } = useCategoryBreakdown();

  // Aggregate refreshing state for button disabled
  const isRefreshing =
    metricsRefetching || streamRefetching || actionsRefetching;

  // Refresh all data
  const handleRefreshAll = () => {
    refetchMetrics();
    refetchStream();
    refetchActions();
  };

  return (
    <div className={cn('space-y-6', className)}>
      {/* Header */}
      <motion.div
        className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between"
        variants={listItemVariants}
        initial="hidden"
        animate="visible"
      >
        <div>
          <h1 className="text-2xl font-bold tracking-tight md:text-3xl">
            Dashboard
          </h1>
          <p className="text-muted-foreground">
            Your expense command center
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={handleRefreshAll}
            disabled={isRefreshing}
            className="min-h-[44px] min-w-[44px] md:min-h-0 md:min-w-0"
          >
            <RefreshCcw
              className={cn('mr-2 h-4 w-4', isRefreshing && 'animate-spin')}
            />
            Refresh
          </Button>
          <Button variant="ghost" size="icon" className="h-11 w-11 md:h-9 md:w-9">
            <Settings className="h-4 w-4" />
          </Button>
        </div>
      </motion.div>

      {/* Mobile Summary Bar (visible on mobile only, <768px) */}
      <div className="block md:hidden">
        <MetricsSummaryBar
          metrics={metrics}
          isLoading={metricsLoading}
        />
      </div>

      {/* Metrics Row (hidden on mobile, shown on tablet+) */}
      <motion.div
        className="hidden md:block"
        variants={listItemVariants}
        initial="hidden"
        animate="visible"
      >
        <MetricsRow
          metrics={metrics}
          isLoading={metricsLoading}
          error={metricsError}
        />
      </motion.div>

      <Separator />

      {/* Main Content Grid */}
      <motion.div
        className="grid gap-6 lg:grid-cols-3"
        variants={staggerContainer}
        initial="hidden"
        animate="visible"
      >
        {/* Left Column: Activity Stream */}
        <motion.div variants={listItemVariants} className="lg:col-span-2">
          <ExpenseStream
            items={streamItems}
            isLoading={streamLoading}
            maxItems={8}
            showViewAll
          />
        </motion.div>

        {/* Right Column: Action Queue */}
        <motion.div variants={listItemVariants}>
          <ActionQueue
            items={actionItems}
            isLoading={actionsLoading}
            maxItems={5}
          />
        </motion.div>
      </motion.div>

      {/* Category Breakdown */}
      <motion.div
        variants={listItemVariants}
        initial="hidden"
        animate="visible"
      >
        <CategoryBreakdown
          categories={categories}
          isLoading={categoriesLoading}
          variant="list"
        />
      </motion.div>

      {/* Mobile Nav Spacer - prevents content from being hidden behind fixed nav */}
      <MobileNavSpacer />

      {/* Mobile Bottom Navigation */}
      <MobileNav
        pendingCounts={{
          receipts: metrics?.pendingReceiptsCount,
          matching: metrics?.pendingMatchesCount,
        }}
      />
    </div>
  );
}

/**
 * Compact dashboard view for widget/embed use.
 */
export function DashboardCompact({ className }: { className?: string }) {
  const { data: metrics, isLoading: metricsLoading } = useDashboardMetrics();
  const { data: actionItems, isLoading: actionsLoading } = useActionQueue();

  const pendingCount =
    (metrics?.pendingReceiptsCount ?? 0) + (metrics?.pendingMatchesCount ?? 0);
  const urgentCount =
    actionItems?.filter((i) => i.priority === 'high').length ?? 0;

  if (metricsLoading || actionsLoading) {
    return (
      <div className={cn('space-y-3', className)}>
        {[1, 2, 3].map((i) => (
          <div
            key={i}
            className="h-10 animate-pulse rounded-lg bg-muted"
          />
        ))}
      </div>
    );
  }

  return (
    <div className={cn('space-y-3', className)}>
      {/* Quick Stats */}
      <div className="grid grid-cols-2 gap-3">
        <div className="rounded-lg bg-muted/50 p-3">
          <div className="text-xs text-muted-foreground">Pending</div>
          <div className="text-lg font-semibold">{pendingCount}</div>
        </div>
        <div className="rounded-lg bg-muted/50 p-3">
          <div className="text-xs text-muted-foreground">Urgent</div>
          <div className="flex items-center gap-2">
            <span className="text-lg font-semibold">{urgentCount}</span>
            {urgentCount > 0 && (
              <span className="h-2 w-2 animate-pulse rounded-full bg-destructive" />
            )}
          </div>
        </div>
      </div>

      {/* Top Actions */}
      <div className="space-y-2">
        <div className="text-xs font-medium text-muted-foreground">
          Top Actions
        </div>
        {actionItems?.slice(0, 3).map((action) => (
          <div
            key={action.id}
            className="flex items-center gap-2 rounded px-2 py-1.5 text-sm hover:bg-muted/50"
          >
            <span className="flex-1 truncate">{action.title}</span>
            <ActionQueueBadge
              count={1}
              highPriorityCount={action.priority === 'high' ? 1 : 0}
            />
          </div>
        )) ?? <div className="text-sm text-muted-foreground">No actions</div>}
      </div>
    </div>
  );
}

export default DashboardLayout;
