/**
 * ActionQueue Component (T029)
 *
 * Displays priority-sorted pending items requiring user attention.
 * Items are grouped by priority (high, medium, low) with visual distinction.
 *
 * Action types:
 * - review_match: Review AI-proposed receipt-transaction matches
 * - correct_extraction: Fix AI-extracted receipt fields
 * - approve_report: Approve pending expense reports
 * - categorize: Categorize uncategorized transactions
 * - missing_receipt: Add missing receipts
 */

import { memo, useMemo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Link } from '@tanstack/react-router';
import {
  AlertTriangle,
  CheckSquare,
  Edit3,
  FileCheck,
  Tag,
  Receipt,
  ChevronRight,
  Clock,
} from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { ConfidenceBadge } from '@/components/design-system/confidence-indicator';
import { ActionQueueSkeleton } from '@/components/design-system/loading-skeleton';
import { EmptyPendingMatches } from '@/components/design-system/empty-state';
import { cn, formatCurrency, formatRelativeTime } from '@/lib/utils';
import { listItemVariants, staggerContainer } from '@/lib/animations';
import type { ActionQueueItem, ActionType, ActionPriority } from '@/types/dashboard';

interface ActionQueueProps {
  /** Queue items to display */
  items?: ActionQueueItem[];
  /** Loading state */
  isLoading?: boolean;
  /** Callback when action is clicked */
  onActionClick?: (action: ActionQueueItem) => void;
  /** Maximum items to show before "View All" */
  maxItems?: number;
  /** Show header */
  showHeader?: boolean;
  /** Additional CSS classes */
  className?: string;
}

/**
 * Icon mapping for action types.
 */
const actionTypeIcons: Record<ActionType, React.ElementType> = {
  review_match: CheckSquare,
  correct_extraction: Edit3,
  approve_report: FileCheck,
  categorize: Tag,
  missing_receipt: Receipt,
};

/**
 * Priority styling configuration.
 */
const priorityConfig: Record<
  ActionPriority,
  {
    label: string;
    badgeVariant: 'destructive' | 'warning' | 'secondary';
    bgClass: string;
    borderClass: string;
  }
> = {
  high: {
    label: 'High Priority',
    badgeVariant: 'destructive',
    bgClass: 'bg-destructive/5',
    borderClass: 'border-l-destructive',
  },
  medium: {
    label: 'Medium',
    badgeVariant: 'warning',
    bgClass: 'bg-confidence-medium/5',
    borderClass: 'border-l-confidence-medium',
  },
  low: {
    label: 'Low',
    badgeVariant: 'secondary',
    bgClass: 'bg-muted/50',
    borderClass: 'border-l-muted-foreground',
  },
};

/**
 * Action type labels for display.
 */
const actionTypeLabels: Record<ActionType, string> = {
  review_match: 'Review Match',
  correct_extraction: 'Fix Extraction',
  approve_report: 'Approve Report',
  categorize: 'Categorize',
  missing_receipt: 'Add Receipt',
};

/**
 * Individual action item component.
 */
const ActionItem = memo(function ActionItem({
  item,
  onClick,
}: {
  item: ActionQueueItem;
  onClick?: (item: ActionQueueItem) => void;
}) {
  const Icon = actionTypeIcons[item.type] || CheckSquare;
  const config = priorityConfig[item.priority] || priorityConfig.low;

  const content = (
    <motion.div
      variants={listItemVariants}
      className={cn(
        'group relative flex items-start gap-3 border-l-4 p-4 transition-all',
        'hover:bg-muted/30 cursor-pointer',
        config.bgClass,
        config.borderClass
      )}
      onClick={() => onClick?.(item)}
    >
      {/* Icon */}
      <div
        className={cn(
          'flex h-8 w-8 shrink-0 items-center justify-center rounded-md',
          'bg-background shadow-sm ring-1 ring-border'
        )}
      >
        <Icon className="h-4 w-4" />
      </div>

      {/* Content */}
      <div className="min-w-0 flex-1">
        {/* Header: Priority badge + Type label */}
        <div className="flex items-center gap-2 mb-1">
          {item.priority === 'high' && (
            <Badge variant="destructive" className="text-[10px] px-1.5 py-0">
              <AlertTriangle className="mr-0.5 h-2.5 w-2.5" />
              Urgent
            </Badge>
          )}
          <span className="text-xs text-muted-foreground">
            {actionTypeLabels[item.type]}
          </span>
        </div>

        {/* Title */}
        <p className="font-medium text-sm leading-snug">{item.title}</p>

        {/* Description */}
        {item.description && (
          <p className="mt-0.5 text-xs text-muted-foreground line-clamp-2">
            {item.description}
          </p>
        )}

        {/* Footer: Confidence + Amount + Time */}
        <div className="mt-2 flex items-center gap-3 text-xs text-muted-foreground">
          {/* Confidence (for match reviews) */}
          {item.confidence !== undefined && item.confidence > 0 && (
            <ConfidenceBadge score={item.confidence} />
          )}

          {/* Amount */}
          {item.amount !== undefined && (
            <span className="font-medium tabular-nums">
              {formatCurrency(item.amount)}
            </span>
          )}

          {/* Created time */}
          <span className="flex items-center gap-1">
            <Clock className="h-3 w-3" />
            {formatRelativeTime(item.createdAt)}
          </span>
        </div>
      </div>

      {/* Chevron */}
      <ChevronRight
        className={cn(
          'h-4 w-4 shrink-0 text-muted-foreground/50',
          'transition-transform group-hover:translate-x-0.5'
        )}
      />
    </motion.div>
  );

  // Wrap with Link if actionUrl is provided
  if (item.actionUrl) {
    return <Link to={item.actionUrl}>{content}</Link>;
  }

  return content;
});

/**
 * ActionQueue displays pending items requiring user attention.
 */
export function ActionQueue({
  items,
  isLoading,
  onActionClick,
  maxItems = 5,
  showHeader = true,
  className,
}: ActionQueueProps) {
  // Sort by priority (high first) then by date (newest first)
  const sortedItems = useMemo(() => {
    if (!items) return [];

    const priorityOrder: Record<ActionPriority, number> = {
      high: 0,
      medium: 1,
      low: 2,
    };

    return [...items]
      .sort((a, b) => {
        const priorityDiff = priorityOrder[a.priority] - priorityOrder[b.priority];
        if (priorityDiff !== 0) return priorityDiff;
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      })
      .slice(0, maxItems);
  }, [items, maxItems]);

  const hasMore = (items?.length ?? 0) > maxItems;
  const highPriorityCount = items?.filter((i) => i.priority === 'high').length ?? 0;

  if (isLoading) {
    return <ActionQueueSkeleton />;
  }

  return (
    <Card className={cn('overflow-hidden', className)}>
      {showHeader && (
        <CardHeader className="flex flex-row items-center justify-between pb-2">
          <div className="flex items-center gap-2">
            <CardTitle className="text-base font-semibold">
              Action Queue
            </CardTitle>
            {highPriorityCount > 0 && (
              <Badge variant="destructive" className="text-xs">
                {highPriorityCount} urgent
              </Badge>
            )}
          </div>
          {hasMore && (
            <Link
              to="/matching"
              className="text-sm text-primary hover:underline"
            >
              View all ({items?.length})
            </Link>
          )}
        </CardHeader>
      )}
      <CardContent className="p-0">
        {sortedItems.length === 0 ? (
          <EmptyPendingMatches />
        ) : (
          <motion.div
            className="divide-y"
            variants={staggerContainer}
            initial="hidden"
            animate="visible"
          >
            <AnimatePresence mode="popLayout">
              {sortedItems.map((item) => (
                <ActionItem key={item.id} item={item} onClick={onActionClick} />
              ))}
            </AnimatePresence>
          </motion.div>
        )}
      </CardContent>
    </Card>
  );
}

/**
 * Compact action count badge for navigation.
 */
export function ActionQueueBadge({
  count,
  highPriorityCount,
}: {
  count: number;
  highPriorityCount: number;
}) {
  if (count === 0) return null;

  return (
    <span
      className={cn(
        'ml-auto flex h-5 min-w-[20px] items-center justify-center rounded-full px-1.5 text-xs font-medium',
        highPriorityCount > 0
          ? 'bg-destructive text-destructive-foreground'
          : 'bg-muted text-muted-foreground'
      )}
    >
      {count > 99 ? '99+' : count}
    </span>
  );
}

export default ActionQueue;
