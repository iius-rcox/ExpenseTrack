/**
 * PatternStats Component
 *
 * Summary statistics header for the pattern management view.
 * Displays active count, suppressed count, and overall accuracy.
 *
 * @see specs/023-expense-prediction/plan.md for pattern management requirements
 */

import { Activity, EyeOff, TrendingUp, RefreshCw } from 'lucide-react'
import { StatCard } from '@/components/design-system/stat-card'
import { Button } from '@/components/ui/button'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { cn } from '@/lib/utils'

export interface PatternStatsProps {
  /** Number of active (non-suppressed) patterns */
  activeCount: number
  /** Number of suppressed patterns */
  suppressedCount: number
  /** Overall prediction accuracy rate (0-1) */
  overallAccuracyRate: number
  /** Whether data is loading */
  isLoading?: boolean
  /** Whether rebuild is in progress */
  isRebuilding?: boolean
  /** Callback to trigger pattern rebuild */
  onRebuild?: () => void
  /** Additional CSS classes */
  className?: string
}

/**
 * Skeleton loader for stats
 */
function StatsSkeleton() {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
      {[...Array(3)].map((_, i) => (
        <div
          key={i}
          className="bg-muted/30 rounded-lg p-6 animate-pulse space-y-3"
        >
          <div className="h-4 w-24 bg-muted rounded" />
          <div className="h-8 w-16 bg-muted rounded" />
        </div>
      ))}
    </div>
  )
}

/**
 * PatternStats displays summary metrics for patterns
 */
export function PatternStats({
  activeCount,
  suppressedCount,
  overallAccuracyRate,
  isLoading = false,
  isRebuilding = false,
  onRebuild,
  className,
}: PatternStatsProps) {
  const totalCount = activeCount + suppressedCount
  const accuracyPercent = Math.round(overallAccuracyRate * 100)

  if (isLoading) {
    return <StatsSkeleton />
  }

  return (
    <div className={cn('space-y-4', className)}>
      {/* Stats Row */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        {/* Active Patterns */}
        <StatCard
          label="Active Patterns"
          value={activeCount}
          icon={<Activity className="h-5 w-5" />}
        />

        {/* Suppressed Patterns */}
        <StatCard
          label="Suppressed Patterns"
          value={suppressedCount}
          icon={<EyeOff className="h-5 w-5" />}
          className={suppressedCount > 0 ? 'opacity-80' : undefined}
        />

        {/* Overall Accuracy */}
        <StatCard
          label="Overall Accuracy"
          value={`${accuracyPercent}%`}
          icon={<TrendingUp className="h-5 w-5" />}
          highlight={accuracyPercent >= 80}
        />
      </div>

      {/* Actions Row */}
      {onRebuild && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            {totalCount === 0
              ? 'No patterns learned yet. Submit expense reports to build your pattern library.'
              : `${totalCount} total pattern${totalCount !== 1 ? 's' : ''} learned from your expense reports.`}
          </p>

          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant="outline"
                size="sm"
                onClick={onRebuild}
                disabled={isRebuilding || totalCount === 0}
                className="gap-2"
              >
                <RefreshCw
                  className={cn('h-4 w-4', isRebuilding && 'animate-spin')}
                />
                {isRebuilding ? 'Rebuilding...' : 'Rebuild Patterns'}
              </Button>
            </TooltipTrigger>
            <TooltipContent>
              <p>
                Recalculate all patterns from your expense reports.
                <br />
                Use this if predictions seem inaccurate.
              </p>
            </TooltipContent>
          </Tooltip>
        </div>
      )}
    </div>
  )
}
