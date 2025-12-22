/**
 * MetricsRow Component (T027)
 *
 * Displays the key dashboard metrics in a responsive row of stat cards.
 * Uses the StatCard component from the design system with trend indicators.
 *
 * Metrics shown:
 * - Monthly spending total with change percentage
 * - Pending review count
 * - Matching percentage
 * - (Optional) Categorized percentage
 */

import { motion } from 'framer-motion';
import { Link } from '@tanstack/react-router';
import {
  DollarSign,
  Clock,
  CheckCircle,
  AlertCircle,
  TrendingUp,
  TrendingDown,
} from 'lucide-react';
// TrendingUp, TrendingDown used in MetricsSummaryBar
import { StatCard, StatCardCompact } from '@/components/design-system/stat-card';
import { DashboardMetricsSkeleton } from '@/components/design-system/loading-skeleton';
import { cn, formatCurrency } from '@/lib/utils';
import { staggerContainer, listItemVariants } from '@/lib/animations';
import type { DashboardMetrics } from '@/types/api';

interface MetricsRowProps {
  /** Dashboard metrics data */
  metrics?: DashboardMetrics;
  /** Loading state */
  isLoading?: boolean;
  /** Error state */
  error?: Error | null;
  /** Use compact variant for smaller screens */
  compact?: boolean;
  /** Additional CSS classes */
  className?: string;
}

/**
 * Calculate trend direction from percentage change.
 */
function getTrend(change: number): 'up' | 'down' | 'neutral' {
  if (change > 1) return 'up';
  if (change < -1) return 'down';
  return 'neutral';
}

/**
 * Calculate match rate as a percentage.
 * Returns 100 if no transactions, otherwise matched/(matched+unmatched) * 100
 */
function calculateMatchRate(matched: number, unmatched: number): number {
  const total = matched + unmatched;
  if (total === 0) return 100; // No transactions = perfect match rate
  return Math.round((matched / total) * 100);
}

export function MetricsRow({
  metrics,
  isLoading,
  error,
  compact = false,
  className,
}: MetricsRowProps) {
  // Show loading skeleton
  if (isLoading) {
    return <DashboardMetricsSkeleton />;
  }

  // Show error state
  if (error) {
    return (
      <div
        className={cn(
          'grid grid-cols-2 gap-4 md:grid-cols-4',
          className
        )}
      >
        {[1, 2, 3, 4].map((i) => (
          <div
            key={i}
            className="flex items-center justify-center rounded-xl border border-destructive/20 bg-destructive/5 p-6"
          >
            <AlertCircle className="h-5 w-5 text-destructive" />
            <span className="ml-2 text-sm text-destructive">Failed to load</span>
          </div>
        ))}
      </div>
    );
  }

  // No data state
  if (!metrics) {
    return null;
  }

  const monthlySpending = metrics.monthlySpending;
  const spendingTrend = getTrend(monthlySpending.percentChange);

  // Use compact variant for smaller screens
  if (compact) {
    return (
      <div className={cn('grid grid-cols-2 gap-4 md:grid-cols-4', className)}>
        <StatCardCompact label="Spending" value={formatCurrency(monthlySpending.currentMonth)} />
        <StatCardCompact label="Pending" value={metrics.pendingReceiptsCount + metrics.pendingMatchesCount} />
        <StatCardCompact label="Match Rate" value={`${calculateMatchRate(metrics.matchedTransactionsCount, metrics.unmatchedTransactionsCount)}%`} />
        <StatCardCompact label="Drafts" value={metrics.draftReportsCount} />
      </div>
    );
  }

  return (
    <motion.div
      className={cn(
        'grid grid-cols-2 gap-4 md:grid-cols-4',
        className
      )}
      variants={staggerContainer}
      initial="hidden"
      animate="visible"
    >
      {/* Monthly Spending */}
      <motion.div variants={listItemVariants}>
        <Link to="/transactions">
          <StatCard
            label="Monthly Spending"
            value={monthlySpending.currentMonth}
            trend={monthlySpending.percentChange}
            trendDirection={spendingTrend}
            icon={<DollarSign className="h-4 w-4" />}
            isCurrency
          />
        </Link>
      </motion.div>

      {/* Pending Review */}
      <motion.div variants={listItemVariants}>
        <Link to="/matching">
          <StatCard
            label="Pending Review"
            value={metrics.pendingReceiptsCount + metrics.pendingMatchesCount}
            icon={<Clock className="h-4 w-4" />}
            highlight={metrics.pendingReceiptsCount + metrics.pendingMatchesCount > 10}
          />
        </Link>
      </motion.div>

      {/* Matching Rate */}
      <motion.div variants={listItemVariants}>
        <Link to="/matching">
          <StatCard
            label="Match Rate"
            value={calculateMatchRate(metrics.matchedTransactionsCount, metrics.unmatchedTransactionsCount)}
            icon={<CheckCircle className="h-4 w-4" />}
            isPercentage
            highlight={metrics.unmatchedTransactionsCount === 0}
          />
        </Link>
      </motion.div>

      {/* Draft Reports */}
      <motion.div variants={listItemVariants}>
        <Link to="/reports">
          <StatCard
            label="Draft Reports"
            value={metrics.draftReportsCount}
            icon={
              metrics.draftReportsCount > 0 ? (
                <AlertCircle className="h-4 w-4" />
              ) : (
                <CheckCircle className="h-4 w-4" />
              )
            }
            highlight={metrics.draftReportsCount > 0}
          />
        </Link>
      </motion.div>
    </motion.div>
  );
}

/**
 * Metric summary bar for mobile view.
 * Shows key metrics in a compact horizontal scrollable bar.
 */
export function MetricsSummaryBar({
  metrics,
  isLoading,
}: Pick<MetricsRowProps, 'metrics' | 'isLoading'>) {
  if (isLoading || !metrics) {
    return (
      <div className="flex gap-3 overflow-x-auto pb-2">
        {[1, 2, 3].map((i) => (
          <div
            key={i}
            className="flex h-12 min-w-[120px] animate-pulse items-center gap-2 rounded-lg bg-muted px-3"
          />
        ))}
      </div>
    );
  }

  const monthlySpending = metrics.monthlySpending;
  const spendingTrend = getTrend(monthlySpending.percentChange);

  return (
    <div className="flex gap-3 overflow-x-auto pb-2">
      {/* Spending */}
      <div className="flex min-w-[140px] items-center gap-2 rounded-lg bg-muted/50 px-3 py-2">
        <DollarSign className="h-4 w-4 text-accent-copper" />
        <div>
          <div className="text-xs text-muted-foreground">Spending</div>
          <div className="flex items-center gap-1 font-semibold">
            {formatCurrency(monthlySpending.currentMonth)}
            {spendingTrend === 'up' && (
              <TrendingUp className="h-3 w-3 text-destructive" />
            )}
            {spendingTrend === 'down' && (
              <TrendingDown className="h-3 w-3 text-confidence-high" />
            )}
          </div>
        </div>
      </div>

      {/* Pending */}
      <div className="flex min-w-[100px] items-center gap-2 rounded-lg bg-muted/50 px-3 py-2">
        <Clock className="h-4 w-4 text-confidence-medium" />
        <div>
          <div className="text-xs text-muted-foreground">Pending</div>
          <div className="font-semibold">
            {metrics.pendingReceiptsCount + metrics.pendingMatchesCount}
          </div>
        </div>
      </div>

      {/* Matched */}
      <div className="flex min-w-[100px] items-center gap-2 rounded-lg bg-muted/50 px-3 py-2">
        <CheckCircle className="h-4 w-4 text-confidence-high" />
        <div>
          <div className="text-xs text-muted-foreground">Matched</div>
          <div className="font-semibold">
            {metrics.unmatchedTransactionsCount === 0 ? '100%' : 'Pending'}
          </div>
        </div>
      </div>
    </div>
  );
}

export default MetricsRow;
