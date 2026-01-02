/**
 * PredictionFeedback Component (T063)
 *
 * Standalone confirm/reject feedback buttons for expense predictions.
 * Can be used in transaction lists, prediction workspaces, or anywhere
 * feedback actions are needed.
 *
 * Features:
 * - Thumb up/down buttons for confirm/reject
 * - Loading and disabled states
 * - Optional inline mode for compact displays
 * - Accessibility support with tooltips
 *
 * @see specs/023-expense-prediction/spec.md Section 5.2 for feedback requirements
 */

import { memo, useCallback } from 'react';
import { ThumbsUp, ThumbsDown, Loader2 } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';

export interface PredictionFeedbackProps {
  /** Prediction ID to act on */
  predictionId: string;
  /** Handler for confirming the prediction */
  onConfirm?: (predictionId: string) => void;
  /** Handler for rejecting the prediction */
  onReject?: (predictionId: string) => void;
  /** Whether the confirm action is in progress */
  isConfirming?: boolean;
  /** Whether the reject action is in progress */
  isRejecting?: boolean;
  /** Whether all actions are disabled */
  disabled?: boolean;
  /** Size variant */
  size?: 'sm' | 'md' | 'lg';
  /** Whether to show labels alongside icons */
  showLabels?: boolean;
  /** Additional CSS classes */
  className?: string;
}

/**
 * Size configurations for buttons
 */
const SIZE_CLASSES = {
  sm: {
    button: 'h-6 w-6',
    icon: 'h-3 w-3',
    gap: 'gap-0.5',
  },
  md: {
    button: 'h-8 w-8',
    icon: 'h-4 w-4',
    gap: 'gap-1',
  },
  lg: {
    button: 'h-10 w-10',
    icon: 'h-5 w-5',
    gap: 'gap-1.5',
  },
};

/**
 * PredictionFeedback - Confirm/Reject thumb buttons for predictions
 *
 * Provides user feedback mechanism for expense predictions.
 * Confirming a prediction strengthens the pattern's confidence,
 * while rejecting weakens it (and may trigger auto-suppression).
 */
export const PredictionFeedback = memo(function PredictionFeedback({
  predictionId,
  onConfirm,
  onReject,
  isConfirming = false,
  isRejecting = false,
  disabled = false,
  size = 'md',
  showLabels = false,
  className,
}: PredictionFeedbackProps) {
  const sizeConfig = SIZE_CLASSES[size];
  const isProcessing = isConfirming || isRejecting;
  const isDisabled = disabled || isProcessing;

  const handleConfirm = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      onConfirm?.(predictionId);
    },
    [onConfirm, predictionId]
  );

  const handleReject = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      onReject?.(predictionId);
    },
    [onReject, predictionId]
  );

  return (
    <div
      className={cn(
        'inline-flex items-center',
        sizeConfig.gap,
        className
      )}
      onClick={(e) => e.stopPropagation()}
    >
      {/* Confirm Button */}
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              size="icon"
              variant="ghost"
              className={cn(
                sizeConfig.button,
                'hover:bg-green-500/20 hover:text-green-600',
                'dark:hover:bg-green-500/30 dark:hover:text-green-400',
                'focus-visible:ring-green-500/30',
                isConfirming && 'text-green-600 dark:text-green-400'
              )}
              onClick={handleConfirm}
              disabled={isDisabled}
              aria-label="Confirm as expense"
            >
              {isConfirming ? (
                <Loader2 className={cn(sizeConfig.icon, 'animate-spin')} />
              ) : (
                <ThumbsUp className={sizeConfig.icon} />
              )}
            </Button>
          </TooltipTrigger>
          <TooltipContent side="top">
            <p>This is an expense - confirm to improve future predictions</p>
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>

      {showLabels && (
        <span className="text-xs text-muted-foreground">
          {isConfirming ? 'Confirming...' : ''}
        </span>
      )}

      {/* Reject Button */}
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              size="icon"
              variant="ghost"
              className={cn(
                sizeConfig.button,
                'hover:bg-red-500/20 hover:text-red-600',
                'dark:hover:bg-red-500/30 dark:hover:text-red-400',
                'focus-visible:ring-red-500/30',
                isRejecting && 'text-red-600 dark:text-red-400'
              )}
              onClick={handleReject}
              disabled={isDisabled}
              aria-label="Not an expense"
            >
              {isRejecting ? (
                <Loader2 className={cn(sizeConfig.icon, 'animate-spin')} />
              ) : (
                <ThumbsDown className={sizeConfig.icon} />
              )}
            </Button>
          </TooltipTrigger>
          <TooltipContent side="top">
            <p>Not an expense - reject to improve future predictions</p>
          </TooltipContent>
        </Tooltip>
      </TooltipProvider>

      {showLabels && (
        <span className="text-xs text-muted-foreground">
          {isRejecting ? 'Rejecting...' : ''}
        </span>
      )}
    </div>
  );
});

/**
 * Skeleton loader for PredictionFeedback
 */
export function PredictionFeedbackSkeleton({
  size = 'md',
}: {
  size?: 'sm' | 'md' | 'lg';
}) {
  const sizeConfig = SIZE_CLASSES[size];

  return (
    <div className={cn('inline-flex items-center', sizeConfig.gap)}>
      <div
        className={cn(
          sizeConfig.button,
          'rounded-md bg-muted animate-pulse'
        )}
      />
      <div
        className={cn(
          sizeConfig.button,
          'rounded-md bg-muted animate-pulse'
        )}
      />
    </div>
  );
}

export default PredictionFeedback;
