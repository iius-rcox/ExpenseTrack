/**
 * ExpenseBadge Component (T040)
 *
 * Visual indicator shown on transactions that match learned expense patterns.
 * Displays confidence level and suggested category with quick actions.
 *
 * Design System Integration:
 * - Uses ConfidenceIndicator for visual confidence display
 * - Color coding: emerald (high), amber (medium)
 * - Low confidence predictions are NOT displayed (filtered at data layer)
 *
 * @see specs/023-expense-prediction/spec.md Section 5.1 for requirements
 * @see frontend/src/types/prediction.ts for PredictionBadgeProps
 */

import { memo, useCallback } from 'react';
import { motion } from 'framer-motion';
import { Sparkles, Check, X, ChevronRight, CircleCheck, CircleX } from 'lucide-react';
import { cn, safeDisplayString } from '@/lib/utils';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { ConfidenceIndicator } from '@/components/design-system/confidence-indicator';
import type {
  PredictionConfidence,
  PredictionSummary,
} from '@/types/prediction';

/**
 * Props for the ExpenseBadge component.
 */
export interface ExpenseBadgeProps {
  /** Prediction data to display */
  prediction: PredictionSummary;
  /** Handler for confirming the prediction */
  onConfirm?: (predictionId: string) => void;
  /** Handler for rejecting the prediction */
  onReject?: (predictionId: string) => void;
  /** Handler for viewing prediction details */
  onViewDetails?: (predictionId: string) => void;
  /** Whether actions are currently processing */
  isProcessing?: boolean;
  /** Whether to show in compact mode (badge only, no actions) */
  compact?: boolean;
  /** Additional CSS classes */
  className?: string;
}

/**
 * Color configuration for confidence levels
 */
const CONFIDENCE_COLORS: Record<
  Exclude<PredictionConfidence, 'Low'>,
  { bg: string; border: string; text: string; icon: string }
> = {
  High: {
    bg: 'bg-emerald-500/10 dark:bg-emerald-500/20',
    border: 'border-emerald-500/30 dark:border-emerald-500/40',
    text: 'text-emerald-700 dark:text-emerald-400',
    icon: 'text-emerald-600 dark:text-emerald-400',
  },
  Medium: {
    bg: 'bg-amber-500/10 dark:bg-amber-500/20',
    border: 'border-amber-500/30 dark:border-amber-500/40',
    text: 'text-amber-700 dark:text-amber-400',
    icon: 'text-amber-600 dark:text-amber-400',
  },
};

/**
 * Color configuration for personal expense predictions
 */
const PERSONAL_COLORS = {
  bg: 'bg-rose-500/10 dark:bg-rose-500/20',
  border: 'border-rose-500/30 dark:border-rose-500/40',
  text: 'text-rose-700 dark:text-rose-400',
  icon: 'text-rose-600 dark:text-rose-400',
};

/**
 * Labels for confidence levels
 */
const CONFIDENCE_LABELS: Record<Exclude<PredictionConfidence, 'Low'>, string> = {
  High: 'High confidence expense',
  Medium: 'Medium confidence expense',
};

/**
 * Convert confidence level to numeric score for ConfidenceIndicator
 */
function confidenceToScore(level: PredictionConfidence): number {
  switch (level) {
    case 'High':
      return 0.9;
    case 'Medium':
      return 0.75;
    case 'Low':
      return 0.5;
  }
}

/**
 * ExpenseBadge - Shows expense prediction on transaction rows
 *
 * Displays a badge indicating the transaction likely matches a known
 * expense pattern. Provides quick actions for confirm/reject.
 */
export const ExpenseBadge = memo(function ExpenseBadge({
  prediction,
  onConfirm,
  onReject,
  onViewDetails,
  isProcessing = false,
  compact = false,
  className,
}: ExpenseBadgeProps) {
  // Define all hooks unconditionally first (React rules of hooks)
  // Handle confirm action - use transactionId for mutation compatibility
  const handleConfirm = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      onConfirm?.(prediction.transactionId);
    },
    [onConfirm, prediction.transactionId]
  );

  // Handle reject action - use transactionId for mutation compatibility
  const handleReject = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      onReject?.(prediction.transactionId);
    },
    [onReject, prediction.transactionId]
  );

  // Handle view details
  const handleViewDetails = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      onViewDetails?.(prediction.id);
    },
    [onViewDetails, prediction.id]
  );

  // Don't render for low confidence (should be filtered at data layer, but defensive)
  if (prediction.confidenceLevel === 'Low') {
    return null;
  }

  // Use personal colors if this is a personal expense prediction
  const isPersonal = prediction.isPersonalPrediction === true;
  const colors = isPersonal ? PERSONAL_COLORS : CONFIDENCE_COLORS[prediction.confidenceLevel];
  const label = isPersonal ? 'Likely Personal expense' : CONFIDENCE_LABELS[prediction.confidenceLevel];
  const badgeText = isPersonal ? 'Likely Personal' : 'Pending Review';
  const confidenceScore = confidenceToScore(prediction.confidenceLevel);

  // Compact mode - interactive badge with dropdown
  if (compact) {
    return (
      <div className={cn('flex items-center', className)} onClick={(e) => e.stopPropagation()}>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Badge
              variant="outline"
              className={cn(
                'gap-1.5 cursor-pointer transition-colors hover:opacity-80',
                colors.bg,
                colors.border,
                colors.text,
                isProcessing && 'opacity-50 cursor-not-allowed'
              )}
              title={`${label} - Click to change`}
            >
              <Sparkles className={cn('h-3 w-3', colors.icon)} />
              <span className="text-xs font-medium">{badgeText}</span>
            </Badge>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start" className="w-56">
            {/* Header with confidence info */}
            <div className="px-2 py-1.5 border-b">
              <p className="text-sm font-medium">{label}</p>
              <div className="flex items-center gap-2 mt-1">
                <ConfidenceIndicator
                  score={prediction.confidenceScore}
                  showLabel
                  size="sm"
                />
              </div>
              {safeDisplayString(prediction.suggestedCategory, '', 'ExpenseBadge.suggestedCategory.compact.check') && (
                <p className="text-xs text-muted-foreground mt-1">
                  Suggested: {safeDisplayString(prediction.suggestedCategory, '', 'ExpenseBadge.suggestedCategory.compact.display')}
                </p>
              )}
            </div>
            <DropdownMenuSeparator className="my-0" />
            {/* Actions - use transactionId for mutation compatibility */}
            <DropdownMenuItem
              onSelect={() => onConfirm?.(prediction.transactionId)}
              disabled={isProcessing}
            >
              <CircleCheck className="mr-2 h-4 w-4 text-green-600" />
              Mark as Business
            </DropdownMenuItem>
            <DropdownMenuItem
              onSelect={() => onReject?.(prediction.transactionId)}
              disabled={isProcessing}
            >
              <CircleX className="mr-2 h-4 w-4 text-red-600" />
              Mark as Personal
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    );
  }

  // Full mode with actions
  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.95 }}
      animate={{ opacity: 1, scale: 1 }}
      exit={{ opacity: 0, scale: 0.95 }}
      transition={{ duration: 0.15 }}
      className={cn(
        'inline-flex items-center gap-2 rounded-lg border px-2.5 py-1.5',
        colors.bg,
        colors.border,
        isProcessing && 'opacity-60 pointer-events-none',
        className
      )}
      onClick={(e) => e.stopPropagation()}
    >
      {/* Icon and label */}
      <div className="flex items-center gap-1.5">
        <Sparkles className={cn('h-3.5 w-3.5', colors.icon)} />
        <span className={cn('text-xs font-medium', colors.text)}>
          {badgeText}
        </span>
      </div>

      {/* Confidence indicator */}
      <ConfidenceIndicator score={confidenceScore} size="sm" />

      {/* Category suggestion */}
      {safeDisplayString(prediction.suggestedCategory, '', 'ExpenseBadge.suggestedCategory.full.check') && (
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger asChild>
              <span
                className={cn(
                  'text-xs max-w-[100px] truncate',
                  colors.text,
                  'opacity-80'
                )}
              >
                {safeDisplayString(prediction.suggestedCategory, '', 'ExpenseBadge.suggestedCategory.full.display')}
              </span>
            </TooltipTrigger>
            <TooltipContent>
              Suggested category: {safeDisplayString(prediction.suggestedCategory, '', 'ExpenseBadge.suggestedCategory.full.tooltip')}
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      )}

      {/* Divider */}
      <div className="h-4 w-px bg-current opacity-20" />

      {/* Quick actions */}
      <div className="flex items-center gap-0.5">
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                size="icon"
                variant="ghost"
                className={cn(
                  'h-5 w-5',
                  'hover:bg-green-500/20 hover:text-green-600',
                  'dark:hover:bg-green-500/30 dark:hover:text-green-400'
                )}
                onClick={handleConfirm}
                disabled={isProcessing}
              >
                <Check className="h-3 w-3" />
              </Button>
            </TooltipTrigger>
            <TooltipContent>Confirm as expense</TooltipContent>
          </Tooltip>
        </TooltipProvider>

        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                size="icon"
                variant="ghost"
                className={cn(
                  'h-5 w-5',
                  'hover:bg-red-500/20 hover:text-red-600',
                  'dark:hover:bg-red-500/30 dark:hover:text-red-400'
                )}
                onClick={handleReject}
                disabled={isProcessing}
              >
                <X className="h-3 w-3" />
              </Button>
            </TooltipTrigger>
            <TooltipContent>Not an expense</TooltipContent>
          </Tooltip>
        </TooltipProvider>

        {onViewDetails && (
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  size="icon"
                  variant="ghost"
                  className="h-5 w-5 text-muted-foreground hover:text-foreground"
                  onClick={handleViewDetails}
                  disabled={isProcessing}
                >
                  <ChevronRight className="h-3 w-3" />
                </Button>
              </TooltipTrigger>
              <TooltipContent>View details</TooltipContent>
            </Tooltip>
          </TooltipProvider>
        )}
      </div>
    </motion.div>
  );
});

/**
 * Skeleton loader for ExpenseBadge
 */
export function ExpenseBadgeSkeleton({ compact = false }: { compact?: boolean }) {
  if (compact) {
    return (
      <div className="inline-flex items-center gap-1.5 rounded-md border border-muted bg-muted/50 px-2 py-0.5 animate-pulse">
        <div className="h-3 w-3 rounded bg-muted" />
        <div className="h-3 w-12 rounded bg-muted" />
      </div>
    );
  }

  return (
    <div className="inline-flex items-center gap-2 rounded-lg border border-muted bg-muted/50 px-2.5 py-1.5 animate-pulse">
      <div className="h-3.5 w-3.5 rounded bg-muted" />
      <div className="h-3 w-14 rounded bg-muted" />
      <div className="flex gap-0.5">
        {[...Array(5)].map((_, i) => (
          <div key={i} className="h-1.5 w-1.5 rounded-full bg-muted" />
        ))}
      </div>
      <div className="h-4 w-px bg-muted" />
      <div className="h-5 w-5 rounded bg-muted" />
      <div className="h-5 w-5 rounded bg-muted" />
    </div>
  );
}

/**
 * Inline variant for very compact displays (e.g., within text)
 */
export function ExpenseBadgeInline({
  confidenceLevel,
  isPersonalPrediction,
  className,
}: {
  confidenceLevel: Exclude<PredictionConfidence, 'Low'>;
  isPersonalPrediction?: boolean;
  className?: string;
}) {
  const isPersonal = isPersonalPrediction === true;
  const colors = isPersonal ? PERSONAL_COLORS : CONFIDENCE_COLORS[confidenceLevel];
  const badgeText = isPersonal ? 'Likely Personal' : 'Pending Review';

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 text-xs font-medium',
        colors.text,
        className
      )}
    >
      <Sparkles className={cn('h-2.5 w-2.5', colors.icon)} />
      <span>{badgeText}</span>
    </span>
  );
}

export default ExpenseBadge;
