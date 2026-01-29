/**
 * ReimbursabilityActions Component
 *
 * Shows transaction reimbursability status and provides actions for manual overrides.
 * Integrates with the prediction system to display confirmed/rejected status
 * and allows users to manually mark transactions as reimbursable or not.
 *
 * Features:
 * - Visual status indicator (reimbursable, not reimbursable, pending, none)
 * - Dropdown menu for manual override actions
 * - Clear override option for manually marked transactions
 *
 * @see specs/023-expense-prediction/spec.md for prediction system
 */

import { memo, useCallback } from 'react';
import {
  Check,
  X,
  HelpCircle,
  CircleCheck,
  CircleX,
  Undo2,
  Sparkles,
  AlertTriangle,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Badge } from '@/components/ui/badge';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import type { PredictionSummary } from '@/types/prediction';

/**
 * Reimbursability status derived from prediction
 */
export type ReimbursabilityStatus =
  | 'reimbursable'      // Confirmed prediction = business expense
  | 'not-reimbursable'  // Rejected prediction = personal expense
  | 'pending'           // Pending prediction awaiting action
  | 'none';             // No prediction exists

/**
 * Props for ReimbursabilityActions component
 */
export interface ReimbursabilityActionsProps {
  /** Transaction ID */
  transactionId: string;
  /** Prediction summary if one exists */
  prediction?: PredictionSummary | null;
  /** Handler for marking as reimbursable */
  onMarkReimbursable?: (transactionId: string) => void;
  /** Handler for marking as not reimbursable */
  onMarkNotReimbursable?: (transactionId: string) => void;
  /** Handler for clearing manual override */
  onClearOverride?: (transactionId: string) => void;
  /** Whether an action is processing */
  isProcessing?: boolean;
  /** Additional CSS classes */
  className?: string;
}

/**
 * Status configuration for visual display
 */
const STATUS_CONFIG: Record<
  ReimbursabilityStatus,
  { icon: React.ElementType; color: string; label: string; tooltip: string }
> = {
  reimbursable: {
    icon: CircleCheck,
    color: 'text-green-600 bg-green-100 dark:bg-green-900/30 border-green-300 dark:border-green-700',
    label: 'Reimbursable',
    tooltip: 'This transaction is marked as a business expense',
  },
  'not-reimbursable': {
    icon: CircleX,
    color: 'text-red-600 bg-red-100 dark:bg-red-900/30 border-red-300 dark:border-red-700',
    label: 'Not Reimbursable',
    tooltip: 'This transaction is marked as a personal expense',
  },
  pending: {
    icon: Sparkles,
    color: 'text-amber-600 bg-amber-100 dark:bg-amber-900/30 border-amber-300 dark:border-amber-700',
    label: 'Pending',
    tooltip: 'This transaction has a pending expense prediction',
  },
  none: {
    icon: HelpCircle,
    color: 'text-muted-foreground bg-muted border-border',
    label: 'Unknown',
    tooltip: 'No reimbursability status set',
  },
};

/**
 * Derive reimbursability status from prediction
 */
function getReimbursabilityStatus(prediction?: PredictionSummary | null): ReimbursabilityStatus {
  if (!prediction) return 'none';

  switch (prediction.status) {
    case 'Confirmed':
      return 'reimbursable';
    case 'Rejected':
      return 'not-reimbursable';
    case 'Pending':
      return 'pending';
    case 'Ignored':
      return 'none';
    default:
      return 'none';
  }
}

/**
 * ReimbursabilityActions - Shows status badge with action dropdown
 */
export const ReimbursabilityActions = memo(function ReimbursabilityActions({
  transactionId,
  prediction,
  onMarkReimbursable,
  onMarkNotReimbursable,
  onClearOverride,
  isProcessing = false,
  className,
}: ReimbursabilityActionsProps) {
  const status = getReimbursabilityStatus(prediction);
  const config = STATUS_CONFIG[status];
  const StatusIcon = config.icon;
  const isManualOverride = prediction?.isManualOverride ?? false;

  // Handlers
  const handleMarkReimbursable = useCallback(
    (e: Event) => {
      e.stopPropagation();
      onMarkReimbursable?.(transactionId);
    },
    [onMarkReimbursable, transactionId]
  );

  const handleMarkNotReimbursable = useCallback(
    (e: Event) => {
      e.stopPropagation();
      onMarkNotReimbursable?.(transactionId);
    },
    [onMarkNotReimbursable, transactionId]
  );

  const handleClearOverride = useCallback(
    (e: Event) => {
      e.stopPropagation();
      onClearOverride?.(transactionId);
    },
    [onClearOverride, transactionId]
  );

  // For "none" status, show clickable badge that opens dropdown
  // Note: Removed Tooltip wrapper - nested asChild causes event handler conflicts
  if (status === 'none') {
    return (
      <div className={cn('flex items-center', className)} onClick={(e) => e.stopPropagation()}>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Badge
              variant="outline"
              className={cn(
                'gap-1 cursor-pointer text-xs hover:bg-muted/80 transition-colors',
                config.color,
                isProcessing && 'opacity-50 cursor-not-allowed'
              )}
              title={`${config.tooltip} - Click to set status`}
            >
              <StatusIcon className="h-3 w-3" />
              <span>{config.label}</span>
            </Badge>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-48">
            <DropdownMenuItem onSelect={handleMarkReimbursable}>
              <Check className="mr-2 h-4 w-4 text-green-600" />
              Mark as Reimbursable
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={handleMarkNotReimbursable}>
              <X className="mr-2 h-4 w-4 text-red-600" />
              Mark as Not Reimbursable
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    );
  }

  // For pending status, don't show the actions here (ExpenseBadge handles it)
  if (status === 'pending') {
    return null;
  }

  // For confirmed/rejected status, show clickable badge with dropdown
  // Note: Removed Tooltip wrapper - nested asChild causes event handler conflicts
  return (
    <div className={cn('flex items-center', className)} onClick={(e) => e.stopPropagation()}>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Badge
            variant="outline"
            className={cn(
              'gap-1 cursor-pointer text-xs hover:opacity-80 transition-opacity',
              config.color,
              isManualOverride && 'ring-1 ring-offset-1 ring-blue-400',
              isProcessing && 'opacity-50 cursor-not-allowed'
            )}
            title={`${config.tooltip}${isManualOverride ? ' (Manually set)' : ''} - Click to change`}
          >
            <StatusIcon className="h-3 w-3" />
            <span>{status === 'reimbursable' ? 'Business' : 'Personal'}</span>
          </Badge>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-52">
          {status === 'not-reimbursable' && (
            <DropdownMenuItem onSelect={handleMarkReimbursable}>
              <Check className="mr-2 h-4 w-4 text-green-600" />
              Change to Reimbursable
            </DropdownMenuItem>
          )}
          {status === 'reimbursable' && (
            <DropdownMenuItem onSelect={handleMarkNotReimbursable}>
              <X className="mr-2 h-4 w-4 text-red-600" />
              Change to Not Reimbursable
            </DropdownMenuItem>
          )}
          {isManualOverride && (
            <>
              <DropdownMenuSeparator />
              <DropdownMenuItem onSelect={handleClearOverride}>
                <Undo2 className="mr-2 h-4 w-4 text-muted-foreground" />
                Clear Manual Override
              </DropdownMenuItem>
            </>
          )}
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  );
});

/**
 * Compact variant showing just the status icon
 */
export const ReimbursabilityStatusBadge = memo(function ReimbursabilityStatusBadge({
  prediction,
  className,
}: {
  prediction?: PredictionSummary | null;
  className?: string;
}) {
  const status = getReimbursabilityStatus(prediction);

  // Don't show anything for none or pending
  if (status === 'none' || status === 'pending') {
    return null;
  }

  const config = STATUS_CONFIG[status];
  const StatusIcon = config.icon;

  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <Badge
            variant="outline"
            className={cn('gap-1 cursor-default', config.color, className)}
          >
            <StatusIcon className="h-3 w-3" />
          </Badge>
        </TooltipTrigger>
        <TooltipContent>{config.tooltip}</TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
});

/**
 * Missing Receipt Badge - Warning for Business expenses without matched receipts
 *
 * Shows a prominent warning when a transaction/group is:
 * - Marked as Business (reimbursable)
 * - AND does not have a matched receipt
 *
 * This helps users identify incomplete expense documentation.
 */
export const MissingReceiptBadge = memo(function MissingReceiptBadge({
  isReimbursable,
  isMatched,
  className,
}: {
  /** Whether the item is marked as Business (reimbursable) */
  isReimbursable: boolean | undefined;
  /** Whether the item has a matched receipt */
  isMatched: boolean;
  /** Additional CSS classes */
  className?: string;
}) {
  // Only show for Business expenses without matched receipts
  if (isReimbursable !== true || isMatched) {
    return null;
  }

  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <Badge
            variant="outline"
            className={cn(
              'gap-1 cursor-default',
              'text-orange-600 bg-orange-100 dark:bg-orange-900/30 border-orange-300 dark:border-orange-700',
              className
            )}
          >
            <AlertTriangle className="h-3 w-3" />
            <span className="text-xs">Missing Receipt</span>
          </Badge>
        </TooltipTrigger>
        <TooltipContent>
          <p>This business expense needs a receipt for reimbursement</p>
        </TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
});

export default ReimbursabilityActions;
