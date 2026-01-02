/**
 * Auto-Suggested Badge Component (T052)
 *
 * Displays an "Auto-suggested" indicator for expense lines that were
 * pre-selected by the expense prediction system based on historical patterns.
 *
 * @see specs/023-expense-prediction/spec.md - User Story 2
 */

import { Sparkles } from 'lucide-react'
import { cn } from '@/lib/utils'

interface AutoSuggestedBadgeProps {
  /** Compact mode shows just the icon with tooltip */
  compact?: boolean
  /** Additional CSS classes */
  className?: string
}

/**
 * Badge indicating that an expense line was auto-suggested by AI prediction.
 *
 * Usage:
 * - In expense line tables: shows "Auto-suggested" with sparkle icon
 * - Helps users identify which lines were pre-selected vs manually added
 * - Compact mode for space-constrained contexts
 */
export function AutoSuggestedBadge({
  compact = false,
  className,
}: AutoSuggestedBadgeProps) {
  if (compact) {
    return (
      <span
        className={cn(
          'inline-flex items-center justify-center',
          'w-6 h-6 rounded-full',
          'bg-violet-100 dark:bg-violet-900/30',
          'text-violet-600 dark:text-violet-400',
          className
        )}
        title="Auto-suggested expense"
      >
        <Sparkles className="h-3.5 w-3.5" />
      </span>
    )
  }

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-xs font-medium',
        'bg-violet-100 dark:bg-violet-900/30',
        'text-violet-700 dark:text-violet-300',
        'border border-violet-200 dark:border-violet-800',
        className
      )}
    >
      <Sparkles className="h-3 w-3" />
      <span>Auto-suggested</span>
    </span>
  )
}

/**
 * Skeleton loader for AutoSuggestedBadge
 */
export function AutoSuggestedBadgeSkeleton({ compact = false }: { compact?: boolean }) {
  if (compact) {
    return (
      <span className="inline-block w-6 h-6 rounded-full bg-muted animate-pulse" />
    )
  }

  return (
    <span className="inline-block w-24 h-5 rounded-full bg-muted animate-pulse" />
  )
}

/**
 * Summary section showing auto-suggested vs manual expense counts
 */
interface AutoSuggestedSummaryProps {
  /** Number of auto-suggested expenses */
  autoSuggestedCount: number
  /** Total number of expenses */
  totalCount: number
  /** Additional CSS classes */
  className?: string
}

export function AutoSuggestedSummary({
  autoSuggestedCount,
  totalCount,
  className,
}: AutoSuggestedSummaryProps) {
  const manualCount = totalCount - autoSuggestedCount
  const autoPercentage =
    totalCount > 0 ? Math.round((autoSuggestedCount / totalCount) * 100) : 0

  if (autoSuggestedCount === 0) {
    return null // Don't show if no auto-suggested items
  }

  return (
    <div
      className={cn(
        'flex items-center gap-3 p-3 rounded-lg',
        'bg-violet-50 dark:bg-violet-950/30',
        'border border-violet-100 dark:border-violet-900',
        className
      )}
    >
      <Sparkles className="h-5 w-5 text-violet-600 dark:text-violet-400" />
      <div className="flex-1 text-sm">
        <p className="font-medium text-violet-800 dark:text-violet-200">
          {autoSuggestedCount} expense{autoSuggestedCount !== 1 ? 's' : ''}{' '}
          auto-suggested ({autoPercentage}%)
        </p>
        <p className="text-violet-600 dark:text-violet-400">
          {manualCount} manually added
        </p>
      </div>
    </div>
  )
}
