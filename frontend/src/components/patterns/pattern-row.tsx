/**
 * PatternRow Component
 *
 * Displays a single expense pattern in the pattern grid with expandable details.
 * Supports selection, inline toggle for suppression, and expansion for full details.
 *
 * @see specs/023-expense-prediction/plan.md for pattern management requirements
 */

import { memo, useState, useCallback } from 'react'
import { ChevronRight, ChevronDown, Trash2, ExternalLink, Calendar, Hash } from 'lucide-react'
import { motion, AnimatePresence } from 'framer-motion'
import { cn } from '@/lib/utils'
import { TableRow, TableCell } from '@/components/ui/table'
import { Checkbox } from '@/components/ui/checkbox'
import { Switch } from '@/components/ui/switch'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Progress } from '@/components/ui/progress'
import { ConfidenceIndicator } from '@/components/design-system/confidence-indicator'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import type { PatternSummary } from '@/types/prediction'

export interface PatternRowProps {
  /** Pattern data */
  pattern: PatternSummary
  /** Whether this row is selected */
  isSelected: boolean
  /** Whether this row is expanded */
  isExpanded: boolean
  /** Called when selection changes */
  onSelect: (id: string, shiftKey: boolean) => void
  /** Called when expand/collapse is toggled */
  onToggleExpand: (id: string) => void
  /** Called when suppression is toggled */
  onToggleSuppression: (id: string, isSuppressed: boolean) => void
  /** Called when delete is confirmed */
  onDelete: (id: string) => void
  /** Whether a mutation is in progress */
  isProcessing?: boolean
}

/**
 * Format currency amount
 */
function formatAmount(amount: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
  }).format(amount)
}

/**
 * Format relative date
 */
function formatRelativeDate(date: Date | string): string {
  const d = typeof date === 'string' ? new Date(date) : date
  const now = new Date()
  const diffMs = now.getTime() - d.getTime()
  const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24))

  if (diffDays === 0) return 'Today'
  if (diffDays === 1) return 'Yesterday'
  if (diffDays < 7) return `${diffDays} days ago`
  if (diffDays < 30) return `${Math.floor(diffDays / 7)} weeks ago`
  if (diffDays < 365) return `${Math.floor(diffDays / 30)} months ago`
  return `${Math.floor(diffDays / 365)} years ago`
}

/**
 * PatternRow displays a single pattern with expandable details
 */
export const PatternRow = memo(function PatternRow({
  pattern,
  isSelected,
  isExpanded,
  onSelect,
  onToggleExpand,
  onToggleSuppression,
  onDelete,
  isProcessing = false,
}: PatternRowProps) {
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false)

  const handleCheckboxClick = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation()
      onSelect(pattern.id, e.shiftKey)
    },
    [pattern.id, onSelect]
  )

  const handleRowClick = useCallback(
    (e: React.MouseEvent) => {
      // Don't toggle expand if clicking on interactive elements
      const target = e.target as HTMLElement
      if (
        target.closest('button') ||
        target.closest('[role="checkbox"]') ||
        target.closest('[role="switch"]') ||
        target.closest('[role="dialog"]')
      ) {
        return
      }
      onToggleExpand(pattern.id)
    },
    [pattern.id, onToggleExpand]
  )

  const handleSwitchChange = useCallback(
    (checked: boolean) => {
      // When switch is ON, pattern is active (not suppressed)
      // When switch is OFF, pattern is suppressed
      onToggleSuppression(pattern.id, !checked)
    },
    [pattern.id, onToggleSuppression]
  )

  const handleDelete = useCallback(() => {
    onDelete(pattern.id)
    setIsDeleteDialogOpen(false)
  }, [pattern.id, onDelete])

  // Calculate confirm/reject stats for expanded view
  const confirmRate = pattern.accuracyRate

  return (
    <>
      <TableRow
        className={cn(
          'cursor-pointer transition-colors group',
          isSelected && 'bg-primary/5',
          pattern.isSuppressed && 'opacity-60'
        )}
        onClick={handleRowClick}
        data-state={isSelected ? 'selected' : undefined}
      >
        {/* Checkbox */}
        <TableCell className="w-[40px] px-2">
          <div onClick={handleCheckboxClick}>
            <Checkbox
              checked={isSelected}
              aria-label={`Select ${pattern.displayName}`}
            />
          </div>
        </TableCell>

        {/* Expand Chevron + Vendor Name */}
        <TableCell className="font-medium">
          <div className="flex items-center gap-2">
            <Button
              variant="ghost"
              size="icon"
              className="h-6 w-6 p-0"
              onClick={(e) => {
                e.stopPropagation()
                onToggleExpand(pattern.id)
              }}
              aria-label={isExpanded ? 'Collapse details' : 'Expand details'}
            >
              {isExpanded ? (
                <ChevronDown className="h-4 w-4" />
              ) : (
                <ChevronRight className="h-4 w-4" />
              )}
            </Button>
            <span className="truncate max-w-[200px]">{pattern.displayName}</span>
          </div>
        </TableCell>

        {/* Category */}
        <TableCell>
          {pattern.category ? (
            <Badge variant="secondary" className="text-xs">
              {pattern.category}
            </Badge>
          ) : (
            <span className="text-muted-foreground text-sm">—</span>
          )}
        </TableCell>

        {/* Average Amount */}
        <TableCell className="font-mono text-right">
          {formatAmount(pattern.averageAmount)}
        </TableCell>

        {/* Accuracy */}
        <TableCell>
          <div className="flex items-center gap-2">
            <ConfidenceIndicator score={pattern.accuracyRate} size="sm" />
            <span className="text-xs text-muted-foreground font-mono">
              {Math.round(pattern.accuracyRate * 100)}%
            </span>
          </div>
        </TableCell>

        {/* Status Toggle */}
        <TableCell>
          <Tooltip>
            <TooltipTrigger asChild>
              <div className="flex items-center gap-2">
                <Switch
                  checked={!pattern.isSuppressed}
                  onCheckedChange={handleSwitchChange}
                  disabled={isProcessing}
                  aria-label={
                    pattern.isSuppressed
                      ? 'Enable this pattern'
                      : 'Suppress this pattern'
                  }
                />
                <span className="text-xs text-muted-foreground">
                  {pattern.isSuppressed ? 'Suppressed' : 'Active'}
                </span>
              </div>
            </TooltipTrigger>
            <TooltipContent>
              {pattern.isSuppressed
                ? 'Enable to generate predictions'
                : 'Suppress to stop predictions'}
            </TooltipContent>
          </Tooltip>
        </TableCell>
      </TableRow>

      {/* Expanded Details Row */}
      <AnimatePresence>
        {isExpanded && (
          <TableRow className="bg-muted/30 hover:bg-muted/30">
            <TableCell colSpan={6} className="p-0">
              <motion.div
                initial={{ height: 0, opacity: 0 }}
                animate={{ height: 'auto', opacity: 1 }}
                exit={{ height: 0, opacity: 0 }}
                transition={{ duration: 0.15, ease: 'easeInOut' }}
                className="overflow-hidden"
              >
                <div className="px-10 py-2.5 flex items-center gap-6">
                  {/* Compact Stats - inline */}
                  <div className="flex items-center gap-4 text-sm">
                    <span className="text-muted-foreground">
                      <span className="font-mono">{formatAmount(pattern.averageAmount * 0.7)}</span>
                      <span className="mx-1">–</span>
                      <span className="font-mono">{formatAmount(pattern.averageAmount * 1.3)}</span>
                    </span>
                    <span className="text-muted-foreground">•</span>
                    <span className="flex items-center gap-1">
                      <Hash className="h-3 w-3 text-muted-foreground" />
                      <span>{pattern.occurrenceCount}×</span>
                    </span>
                    <span className="text-muted-foreground">•</span>
                    <span className="flex items-center gap-1">
                      <Calendar className="h-3 w-3 text-muted-foreground" />
                      <span>{formatRelativeDate(pattern.lastSeenAt)}</span>
                    </span>
                  </div>

                  {/* Compact Feedback */}
                  <div className="flex items-center gap-2">
                    <Progress
                      value={confirmRate * 100}
                      className="w-24 h-1.5"
                    />
                    <span className="text-xs text-muted-foreground">
                      {Math.round(confirmRate * 100)}%
                    </span>
                  </div>

                  {/* Spacer */}
                  <div className="flex-1" />

                  {/* Compact Actions */}
                  <div className="flex items-center gap-1.5">
                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-1"
                      onClick={(e) => {
                        e.stopPropagation()
                        // TODO: Navigate to transactions filtered by this vendor
                        console.log('View transactions for:', pattern.displayName)
                      }}
                    >
                      <ExternalLink className="h-3 w-3" />
                      View Transactions
                    </Button>

                    <AlertDialog
                      open={isDeleteDialogOpen}
                      onOpenChange={setIsDeleteDialogOpen}
                    >
                      <AlertDialogTrigger asChild>
                        <Button
                          variant="outline"
                          size="sm"
                          className="gap-1 text-destructive hover:text-destructive hover:bg-destructive/10"
                          onClick={(e) => e.stopPropagation()}
                        >
                          <Trash2 className="h-3 w-3" />
                          Delete
                        </Button>
                      </AlertDialogTrigger>
                      <AlertDialogContent onClick={(e) => e.stopPropagation()}>
                        <AlertDialogHeader>
                          <AlertDialogTitle>Delete Pattern</AlertDialogTitle>
                          <AlertDialogDescription>
                            Are you sure you want to delete the pattern for{' '}
                            <strong>{pattern.displayName}</strong>? This will
                            remove all learned behavior and predictions for this
                            vendor. This action cannot be undone.
                          </AlertDialogDescription>
                        </AlertDialogHeader>
                        <AlertDialogFooter>
                          <AlertDialogCancel>Cancel</AlertDialogCancel>
                          <AlertDialogAction
                            onClick={handleDelete}
                            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                          >
                            Delete Pattern
                          </AlertDialogAction>
                        </AlertDialogFooter>
                      </AlertDialogContent>
                    </AlertDialog>
                  </div>
                </div>
              </motion.div>
            </TableCell>
          </TableRow>
        )}
      </AnimatePresence>
    </>
  )
})

/**
 * Skeleton loader for PatternRow
 */
export function PatternRowSkeleton() {
  return (
    <TableRow className="animate-pulse">
      <TableCell className="w-[40px] px-2">
        <div className="h-4 w-4 bg-muted rounded" />
      </TableCell>
      <TableCell>
        <div className="flex items-center gap-2">
          <div className="h-4 w-4 bg-muted rounded" />
          <div className="h-4 w-32 bg-muted rounded" />
        </div>
      </TableCell>
      <TableCell>
        <div className="h-5 w-16 bg-muted rounded-full" />
      </TableCell>
      <TableCell>
        <div className="h-4 w-16 bg-muted rounded ml-auto" />
      </TableCell>
      <TableCell>
        <div className="flex items-center gap-2">
          <div className="flex gap-0.5">
            {[...Array(5)].map((_, i) => (
              <div key={i} className="h-1.5 w-1.5 bg-muted rounded-full" />
            ))}
          </div>
          <div className="h-4 w-8 bg-muted rounded" />
        </div>
      </TableCell>
      <TableCell>
        <div className="flex items-center gap-2">
          <div className="h-5 w-9 bg-muted rounded-full" />
          <div className="h-4 w-16 bg-muted rounded" />
        </div>
      </TableCell>
    </TableRow>
  )
}
