/**
 * ExpenseStream Component (T028)
 *
 * Real-time activity feed showing recent expense events.
 * Updates via 30-second polling without full page refresh.
 *
 * Each item displays:
 * - Event type icon
 * - Title and amount
 * - Timestamp (relative)
 * - Confidence indicator (when applicable)
 * - Status indicator
 */

import { memo, useMemo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Link } from '@tanstack/react-router';
import {
  Receipt,
  ArrowRightLeft,
  CheckCircle2,
  Tag,
  FileText,
  Clock,
  AlertCircle,
  Loader2,
} from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { ConfidenceInline } from '@/components/design-system/confidence-indicator';
import { ExpenseStreamSkeleton } from '@/components/design-system/loading-skeleton';
import { cn, formatCurrency, formatRelativeTime } from '@/lib/utils';
import { listItemVariants, staggerContainer } from '@/lib/animations';
import type {
  ExpenseStreamItem,
  ExpenseStreamEventType,
  ExpenseStreamStatus,
} from '@/types/dashboard';

interface ExpenseStreamProps {
  /** Activity items to display */
  items?: ExpenseStreamItem[];
  /** Loading state */
  isLoading?: boolean;
  /** Maximum items to show (default: 10) */
  maxItems?: number;
  /** Callback when item is clicked */
  onItemClick?: (item: ExpenseStreamItem) => void;
  /** Show "View All" link */
  showViewAll?: boolean;
  /** Additional CSS classes */
  className?: string;
}

/**
 * Icon mapping for event types.
 */
const eventTypeIcons: Record<ExpenseStreamEventType, React.ElementType> = {
  receipt: Receipt,
  transaction: ArrowRightLeft,
  match: CheckCircle2,
  category: Tag,
  report: FileText,
};

/**
 * Color mapping for event types.
 */
const eventTypeColors: Record<ExpenseStreamEventType, string> = {
  receipt: 'text-blue-500 bg-blue-500/10',
  transaction: 'text-purple-500 bg-purple-500/10',
  match: 'text-confidence-high bg-confidence-high/10',
  category: 'text-amber-500 bg-amber-500/10',
  report: 'text-slate-500 bg-slate-500/10',
};

/**
 * Status badge styling.
 */
const statusStyles: Record<
  ExpenseStreamStatus,
  { variant: 'default' | 'secondary' | 'destructive' | 'outline'; icon?: React.ElementType }
> = {
  pending: { variant: 'outline', icon: Clock },
  processing: { variant: 'secondary', icon: Loader2 },
  complete: { variant: 'default', icon: CheckCircle2 },
  error: { variant: 'destructive', icon: AlertCircle },
  needs_review: { variant: 'outline', icon: AlertCircle },
};

/**
 * Individual stream item component.
 */
const StreamItem = memo(function StreamItem({
  item,
  onClick,
}: {
  item: ExpenseStreamItem;
  onClick?: (item: ExpenseStreamItem) => void;
}) {
  const Icon = eventTypeIcons[item.type] || Receipt;
  const iconColorClass = eventTypeColors[item.type] || eventTypeColors.receipt;
  const statusConfig = statusStyles[item.status] || statusStyles.pending;
  const StatusIcon = statusConfig.icon;

  const content = (
    <motion.div
      variants={listItemVariants}
      className={cn(
        'group flex items-start gap-3 rounded-lg p-3 transition-colors',
        'hover:bg-muted/50 cursor-pointer',
        item.status === 'needs_review' && 'bg-confidence-medium/5'
      )}
      onClick={() => onClick?.(item)}
    >
      {/* Type Icon */}
      <div
        className={cn(
          'flex h-9 w-9 shrink-0 items-center justify-center rounded-lg',
          iconColorClass
        )}
      >
        <Icon className="h-4 w-4" />
      </div>

      {/* Content */}
      <div className="min-w-0 flex-1">
        <div className="flex items-start justify-between gap-2">
          <div className="min-w-0 flex-1">
            {/* Title */}
            <p className="truncate font-medium text-sm">{item.title}</p>
            {/* Timestamp */}
            <p className="text-xs text-muted-foreground">
              {formatRelativeTime(item.timestamp)}
            </p>
          </div>

          {/* Amount */}
          {item.amount !== undefined && (
            <div className="shrink-0 text-right">
              <span className="font-semibold text-sm tabular-nums">
                {formatCurrency(item.amount)}
              </span>
            </div>
          )}
        </div>

        {/* Footer: Confidence + Status */}
        <div className="mt-2 flex items-center gap-2">
          {/* Confidence (if applicable) */}
          {item.confidence !== undefined && item.confidence > 0 && (
            <ConfidenceInline score={item.confidence} />
          )}

          {/* Status Badge */}
          {item.status !== 'complete' && (
            <Badge variant={statusConfig.variant} className="text-xs">
              {StatusIcon && (
                <StatusIcon
                  className={cn(
                    'mr-1 h-3 w-3',
                    item.status === 'processing' && 'animate-spin'
                  )}
                />
              )}
              {item.status.replace('_', ' ')}
            </Badge>
          )}
        </div>
      </div>
    </motion.div>
  );

  // Wrap with Link if actionUrl is provided
  if (item.actionUrl) {
    return <Link to={item.actionUrl}>{content}</Link>;
  }

  return content;
});

/**
 * ExpenseStream displays a real-time feed of expense activity.
 */
export function ExpenseStream({
  items,
  isLoading,
  maxItems = 10,
  onItemClick,
  showViewAll = true,
  className,
}: ExpenseStreamProps) {
  // Limit displayed items
  const displayItems = useMemo(
    () => items?.slice(0, maxItems) ?? [],
    [items, maxItems]
  );

  const hasMore = (items?.length ?? 0) > maxItems;

  if (isLoading) {
    return <ExpenseStreamSkeleton />;
  }

  return (
    <Card className={cn('overflow-hidden', className)}>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-base font-semibold">Recent Activity</CardTitle>
        {showViewAll && hasMore && (
          <Link
            to="/transactions"
            className="text-sm text-accent-copper hover:underline"
          >
            View all
          </Link>
        )}
      </CardHeader>
      <CardContent className="p-0">
        {displayItems.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-12 text-center">
            <Receipt className="h-10 w-10 text-muted-foreground/50" />
            <p className="mt-2 text-sm text-muted-foreground">
              No recent activity
            </p>
            <p className="text-xs text-muted-foreground/70">
              New expenses will appear here
            </p>
          </div>
        ) : (
          <motion.div
            className="divide-y"
            variants={staggerContainer}
            initial="hidden"
            animate="visible"
          >
            <AnimatePresence mode="popLayout">
              {displayItems.map((item) => (
                <StreamItem key={item.id} item={item} onClick={onItemClick} />
              ))}
            </AnimatePresence>
          </motion.div>
        )}
      </CardContent>
    </Card>
  );
}

/**
 * Compact variant for sidebar or mobile view.
 */
export function ExpenseStreamCompact({
  items,
  isLoading,
  maxItems = 5,
}: Pick<ExpenseStreamProps, 'items' | 'isLoading' | 'maxItems'>) {
  const displayItems = useMemo(
    () => items?.slice(0, maxItems) ?? [],
    [items, maxItems]
  );

  if (isLoading) {
    return (
      <div className="space-y-2">
        {[1, 2, 3].map((i) => (
          <div
            key={i}
            className="flex h-10 animate-pulse items-center gap-2 rounded bg-muted"
          />
        ))}
      </div>
    );
  }

  if (displayItems.length === 0) {
    return (
      <div className="py-4 text-center text-sm text-muted-foreground">
        No recent activity
      </div>
    );
  }

  return (
    <div className="space-y-1">
      {displayItems.map((item) => (
        <div
          key={item.id}
          className="flex items-center gap-2 rounded px-2 py-1.5 text-sm hover:bg-muted/50"
        >
          <span className="truncate flex-1">{item.title}</span>
          {item.amount !== undefined && (
            <span className="shrink-0 tabular-nums text-muted-foreground">
              {formatCurrency(item.amount)}
            </span>
          )}
        </div>
      ))}
    </div>
  );
}

export default ExpenseStream;
