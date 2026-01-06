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
import { Sparkles, Check, X, ChevronRight } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { ConfidenceIndicator } from '@/components/design-system/confidence-indicator';
import type {
  PredictionConfidence,
  PredictionSummary,
} from '@/types/prediction';

/**
 * DEFENSIVE HELPER: Safely convert any value to a displayable string.
 * Guards against React Error #301 where empty objects {} might be in cached data.
 * Empty objects are truthy in JS, so `value && <span>{value}</span>` will fail!
 */
function safeDisplayString(value: unknown, fallback = ''): string {
  if (value === null || value === undefined) return fallback;
  if (typeof value === 'object' && !Array.isArray(value) && !(value instanceof Date)) {
    const keys = Object.keys(value as object);
    if (keys.length === 0) {
      console.warn('[ExpenseBadge] Empty object detected, using fallback');
      return fallback;
    }
    return fallback;
  }
  return String(value);
}

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
  // Handle confirm action
  const handleConfirm = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      onConfirm?.(prediction.id);
    },
    [onConfirm, prediction.id]
  );

  // Handle reject action
  const handleReject = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      onReject?.(prediction.id);
    },
    [onReject, prediction.id]
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

  const colors = CONFIDENCE_COLORS[prediction.confidenceLevel];
  const label = CONFIDENCE_LABELS[prediction.confidenceLevel];
  const confidenceScore = confidenceToScore(prediction.confidenceLevel);

  // Compact mode - just the badge
  if (compact) {
    return (
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <Badge
              variant="outline"
              className={cn(
                'gap-1.5 cursor-default transition-colors',
                colors.bg,
                colors.border,
                colors.text,
                className
              )}
            >
              <Sparkles className={cn('h-3 w-3', colors.icon)} />
              <span className="text-xs font-medium">Expense</span>
            </Badge>
          </TooltipTrigger>
          <TooltipContent side="top" className="max-w-xs">
            <div className="space-y-1">
              <p className="font-medium">{label}</p>
              {safeDisplayString(prediction.suggestedCategory) && (
                <p className="text-xs text-muted-foreground">
                  Suggested: {safeDisplayString(prediction.suggestedCategory)}
                </p>
              )}
              <div className="flex items-center gap-2 pt-1">
                <ConfidenceIndicator
                  score={prediction.confidenceScore}
                  showLabel
                  size="sm"
                />
              </div>
            </div>
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>
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
          Expense
        </span>
      </div>

      {/* Confidence indicator */}
      <ConfidenceIndicator score={confidenceScore} size="sm" />

      {/* Category suggestion */}
      {safeDisplayString(prediction.suggestedCategory) && (
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
                {safeDisplayString(prediction.suggestedCategory)}
              </span>
            </TooltipTrigger>
            <TooltipContent>
              Suggested category: {safeDisplayString(prediction.suggestedCategory)}
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
  className,
}: {
  confidenceLevel: Exclude<PredictionConfidence, 'Low'>;
  className?: string;
}) {
  const colors = CONFIDENCE_COLORS[confidenceLevel];

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 text-xs font-medium',
        colors.text,
        className
      )}
    >
      <Sparkles className={cn('h-2.5 w-2.5', colors.icon)} />
      <span>Expense</span>
    </span>
  );
}

export default ExpenseBadge;
