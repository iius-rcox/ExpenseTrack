/**
 * Dashboard Types (T026)
 *
 * Type definitions for the dashboard components and API responses.
 * These types are designed to work with the "Refined Intelligence" design system,
 * particularly the confidence indicator components.
 */

import type { ConfidenceLevel } from '@/lib/design-tokens';

// ============================================================================
// Dashboard Metrics Types
// ============================================================================

/**
 * Extended dashboard metrics with additional fields for the redesigned dashboard.
 * Supplements the base DashboardMetrics from api.ts with trend data.
 */
export interface DashboardMetricsExtended {
  /** Total spending for current month */
  monthlyTotal: number;
  /** Percentage change from previous month (-100 to +∞) */
  monthlyChange: number;
  /** Number of items pending review */
  pendingReviewCount: number;
  /** Percentage of transactions matched to receipts */
  matchingPercentage: number;
  /** Percentage of transactions with AI categories */
  categorizedPercentage: number;
  /** Number of activity items in the stream */
  recentActivityCount: number;
}

/**
 * Individual metric for stat cards with optional trend.
 */
export interface MetricData {
  /** Display label for the metric */
  label: string;
  /** Numeric value */
  value: number;
  /** Previous period value for trend calculation */
  previousValue?: number;
  /** Trend direction based on change from previous */
  trend?: 'up' | 'down' | 'neutral';
  /** Percentage change from previous */
  changePercent?: number;
  /** Optional format hint */
  format?: 'currency' | 'percentage' | 'number';
  /** Link destination when metric is clicked */
  href?: string;
}

// ============================================================================
// Expense Stream Types (Activity Feed)
// ============================================================================

/** Possible types of expense stream events */
export type ExpenseStreamEventType =
  | 'receipt'
  | 'transaction'
  | 'match'
  | 'category'
  | 'report';

/** Status of an expense stream item */
export type ExpenseStreamStatus =
  | 'pending'
  | 'processing'
  | 'complete'
  | 'error'
  | 'needs_review';

/**
 * Individual item in the expense stream (activity feed).
 * Represents recent activity across receipts, transactions, and matches.
 */
export interface ExpenseStreamItem {
  /** Unique identifier for the activity item */
  id: string;
  /** Type of activity (receipt, transaction, match, etc.) */
  type: ExpenseStreamEventType;
  /** Human-readable title */
  title: string;
  /** Monetary amount (if applicable) */
  amount?: number;
  /** ISO 8601 timestamp of the event */
  timestamp: string;
  /** Current status of the item */
  status: ExpenseStreamStatus;
  /** AI confidence score (0-1) for relevant items */
  confidence?: number;
  /** Optional thumbnail URL for receipts */
  thumbnailUrl?: string;
  /** Link to view details */
  actionUrl?: string;
}

/**
 * Response structure from /dashboard/activity endpoint
 */
export interface ExpenseStreamResponse {
  items: ExpenseStreamItem[];
  hasMore?: boolean;
}

// ============================================================================
// Action Queue Types (Pending Items)
// ============================================================================

/** Priority levels for action queue items */
export type ActionPriority = 'high' | 'medium' | 'low';

/** Types of actions in the queue */
export type ActionType =
  | 'review_match'
  | 'correct_extraction'
  | 'approve_report'
  | 'categorize'
  | 'missing_receipt';

/**
 * Individual item in the action queue.
 * Represents a pending task requiring user attention.
 */
export interface ActionQueueItem {
  /** Unique identifier */
  id: string;
  /** Type of action required */
  type: ActionType;
  /** Priority level (affects sorting and visual treatment) */
  priority: ActionPriority;
  /** Brief title describing the action */
  title: string;
  /** More detailed description */
  description: string;
  /** ISO 8601 timestamp when action was created */
  createdAt: string;
  /** Link to perform the action */
  actionUrl: string;
  /** AI confidence if relevant (for review_match actions) */
  confidence?: number;
  /** Amount involved if applicable */
  amount?: number;
}

/**
 * Response structure from /dashboard/actions endpoint
 */
export interface ActionQueueResponse {
  items: ActionQueueItem[];
  total?: number;
}

// ============================================================================
// Category Breakdown Types
// ============================================================================

/**
 * Category spending data for charts and breakdowns.
 */
export interface CategoryBreakdownData {
  /** Category name */
  category: string;
  /** Category identifier */
  categoryId?: string;
  /** Total amount spent in this category */
  amount: number;
  /** Percentage of total spending */
  percentage: number;
  /** Number of transactions in this category */
  transactionCount: number;
  /** Color for visualization */
  color: string;
  /** Optional icon identifier */
  icon?: string;
}

/**
 * Spending trend data point for time series charts.
 */
export interface SpendingTrendPoint {
  /** Period identifier (e.g., "2025-12-01") */
  period: string;
  /** Total amount for the period */
  amount: number;
  /** Number of transactions */
  transactionCount: number;
  /** Per-category breakdown */
  categoryBreakdown?: CategoryBreakdownData[];
}

// ============================================================================
// Dashboard Component Props Types
// ============================================================================

/**
 * Props for the MetricsRow component.
 */
export interface MetricsRowProps {
  /** Loading state */
  isLoading?: boolean;
  /** Error state */
  error?: Error | null;
  /** Metrics data */
  metrics?: {
    monthlyTotal: number;
    monthlyChange: number;
    pendingReviewCount: number;
    matchingPercentage: number;
  };
}

/**
 * Props for the ExpenseStream component.
 */
export interface ExpenseStreamProps {
  /** Activity items to display */
  items?: ExpenseStreamItem[];
  /** Loading state */
  isLoading?: boolean;
  /** Maximum items to show */
  maxItems?: number;
  /** Callback when item is clicked */
  onItemClick?: (item: ExpenseStreamItem) => void;
}

/**
 * Props for the ActionQueue component.
 */
export interface ActionQueueProps {
  /** Queue items to display */
  items?: ActionQueueItem[];
  /** Loading state */
  isLoading?: boolean;
  /** Callback when action is clicked */
  onActionClick?: (action: ActionQueueItem) => void;
  /** Maximum items to show before "View All" */
  maxItems?: number;
}

/**
 * Props for the CategoryBreakdown component.
 */
export interface CategoryBreakdownProps {
  /** Category data for visualization */
  categories?: CategoryBreakdownData[];
  /** Loading state */
  isLoading?: boolean;
  /** Chart type */
  variant?: 'pie' | 'bar' | 'list';
  /** Click handler for category selection */
  onCategoryClick?: (category: CategoryBreakdownData) => void;
  /** Additional CSS classes */
  className?: string;
}

// ============================================================================
// Utility Types
// ============================================================================

/**
 * Helper type to add confidence information to any type.
 */
export interface WithConfidence {
  confidence: number;
  confidenceLevel: ConfidenceLevel;
}

/**
 * Props common to all dashboard widgets.
 */
export interface DashboardWidgetBaseProps {
  /** Additional CSS classes */
  className?: string;
  /** Loading skeleton instead of content */
  isLoading?: boolean;
  /** Error state with message */
  error?: Error | null;
}
