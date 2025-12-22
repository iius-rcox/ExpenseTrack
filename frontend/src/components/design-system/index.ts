// Design System Components
export {
  ConfidenceIndicator,
  ConfidenceInline,
  ConfidenceBadge,
} from './confidence-indicator';
export type { ConfidenceIndicatorProps } from './confidence-indicator';

export { StatCard, StatCardCompact } from './stat-card';
export type { StatCardProps } from './stat-card';

export {
  EmptyState,
  EmptyReceipts,
  EmptyTransactions,
  EmptySearchResults,
  EmptyReports,
  EmptyDashboard,
  EmptyPendingMatches,
} from './empty-state';
export type { EmptyStateProps } from './empty-state';

export {
  SkeletonCard,
  DashboardMetricsSkeleton,
  ExpenseStreamSkeleton,
  ActionQueueSkeleton,
  CategoryBreakdownSkeleton,
  ReceiptCardSkeleton,
  ReceiptGridSkeleton,
  ReceiptIntelligenceSkeleton,
  TransactionRowSkeleton,
  TransactionTableSkeleton,
  MatchReviewSkeleton,
  SpendingChartSkeleton,
  TopMerchantsListSkeleton,
} from './loading-skeleton';

// Import the glow CSS (consumers need to import this separately or add to global CSS)
import './confidence-glow.css';
