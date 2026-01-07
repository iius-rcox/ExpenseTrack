/**
 * PatternGrid Component
 *
 * Pattern grid with:
 * - Sortable column headers
 * - Multi-selection support with shift-click range selection
 * - Expandable rows for pattern details
 * - Empty and loading states
 *
 * Note: Virtualization was removed because HTML tables don't support
 * the absolute positioning required for virtual scrolling.
 * For typical pattern counts (<100), direct rendering performs well.
 *
 * @see specs/023-expense-prediction/plan.md for pattern management requirements
 */

import { useCallback, useState, useEffect } from 'react'
import { motion } from 'framer-motion'
import {
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
  Sparkles,
} from 'lucide-react'
import {
  Table,
  TableBody,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Checkbox } from '@/components/ui/checkbox'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Switch } from '@/components/ui/switch'
import { PatternRow, PatternRowSkeleton } from './pattern-row'
import { cn } from '@/lib/utils'
import type {
  PatternSummary,
  PatternSortConfig,
  PatternSelectionState,
} from '@/types/prediction'
import { ConfidenceIndicator } from '@/components/design-system/confidence-indicator'

/**
 * DEFENSIVE HELPER: Safely convert any value to a displayable string.
 * Guards against React Error #301 where empty objects {} might be in cached data.
 */
function safeDisplayString(value: unknown, fallback = ''): string {
  if (value === null || value === undefined) return fallback;
  if (typeof value === 'object' && !Array.isArray(value) && !(value instanceof Date)) {
    const keys = Object.keys(value as object);
    if (keys.length === 0) return fallback;
    return fallback;
  }
  return String(value);
}

/**
 * Sort field type for patterns
 */
export type PatternSortField = PatternSortConfig['field']

/**
 * Props for the PatternGrid component
 */
interface PatternGridProps {
  /** List of patterns to display */
  patterns: PatternSummary[]
  /** Loading state */
  isLoading?: boolean
  /** Current sort configuration */
  sort: PatternSortConfig
  /** Current selection state */
  selection: PatternSelectionState
  /** Set of expanded pattern IDs */
  expandedIds: Set<string>
  /** Callback when sort changes */
  onSortChange: (sort: PatternSortConfig) => void
  /** Callback when selection changes */
  onSelectionChange: (selection: PatternSelectionState) => void
  /** Callback when expanded state changes */
  onExpandedChange: (expandedIds: Set<string>) => void
  /** Callback when suppression is toggled */
  onToggleSuppression: (id: string, isSuppressed: boolean) => void
  /** Callback when receipt match requirement is toggled */
  onToggleReceiptMatch: (id: string, requiresReceiptMatch: boolean) => void
  /** Callback when delete is confirmed */
  onDelete: (id: string) => void
  /** Whether any mutations are in progress */
  isProcessing?: boolean
  /** Height of the scrollable container */
  containerHeight?: number
  /** Whether there are active filters */
  hasFilters?: boolean
}

/**
 * Empty state component
 */
function EmptyState({ hasFilters }: { hasFilters: boolean }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      className="flex flex-col items-center justify-center py-16 text-center"
    >
      <Sparkles className="h-12 w-12 text-muted-foreground/50 mb-4" />
      <h3 className="text-lg font-medium text-foreground">
        {hasFilters ? 'No matching patterns' : 'No patterns yet'}
      </h3>
      <p className="text-sm text-muted-foreground mt-1 max-w-[300px]">
        {hasFilters
          ? 'Try adjusting your filters or search terms'
          : 'Patterns are learned from your expense reports. Submit reports to build your pattern library.'}
      </p>
    </motion.div>
  )
}

/**
 * Loading skeleton for the grid
 */
function LoadingSkeleton({ count = 5 }: { count?: number }) {
  return (
    <>
      {Array.from({ length: count }).map((_, i) => (
        <PatternRowSkeleton key={i} />
      ))}
    </>
  )
}

/**
 * Mobile loading skeleton
 */
function MobileLoadingSkeleton({ count = 5 }: { count?: number }) {
  return (
    <div className="space-y-3">
      {Array.from({ length: count }).map((_, i) => (
        <Card key={i} className="animate-pulse">
          <CardContent className="p-4">
            <div className="flex justify-between items-start mb-2">
              <div className="h-5 w-32 bg-muted rounded" />
              <div className="h-5 w-20 bg-muted rounded" />
            </div>
            <div className="h-4 w-24 bg-muted rounded mb-2" />
            <div className="h-4 w-16 bg-muted rounded" />
          </CardContent>
        </Card>
      ))}
    </div>
  )
}

/**
 * Mobile pattern card component
 */
interface MobilePatternCardProps {
  pattern: PatternSummary
  isSelected: boolean
  onSelect: () => void
  onToggleSuppression: (isSuppressed: boolean) => void
  onToggleReceiptMatch: (requiresReceiptMatch: boolean) => void
  isProcessing?: boolean
}

function MobilePatternCard({
  pattern,
  isSelected,
  onSelect,
  onToggleSuppression,
  onToggleReceiptMatch,
  isProcessing = false,
}: MobilePatternCardProps) {
  const formattedAmount = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(pattern.averageAmount)

  return (
    <Card
      className={cn(
        'transition-colors',
        isSelected && 'ring-2 ring-primary',
        pattern.isSuppressed && 'opacity-60'
      )}
    >
      <CardContent className="p-4">
        <div className="flex items-start gap-3">
          <Checkbox
            checked={isSelected}
            onCheckedChange={onSelect}
            aria-label={`Select ${pattern.displayName}`}
          />
          <div className="flex-1 min-w-0">
            <div className="flex items-start justify-between gap-2">
              <div className="min-w-0 flex-1">
                <h4 className="font-medium truncate">{pattern.displayName}</h4>
                {safeDisplayString(pattern.category) && (
                  <Badge variant="secondary" className="text-xs mt-1">
                    {safeDisplayString(pattern.category)}
                  </Badge>
                )}
              </div>
              <div className="text-right shrink-0">
                <div className="font-mono font-semibold">{formattedAmount}</div>
                <div className="text-xs text-muted-foreground mt-0.5">
                  avg amount
                </div>
              </div>
            </div>

            <div className="flex items-center justify-between mt-3">
              <div className="flex items-center gap-2">
                <ConfidenceIndicator score={pattern.accuracyRate} size="sm" />
                <span className="text-xs text-muted-foreground">
                  {Math.round(pattern.accuracyRate * 100)}% accuracy
                </span>
              </div>
              <div className="flex items-center gap-2">
                <Switch
                  checked={!pattern.isSuppressed}
                  onCheckedChange={(checked) => onToggleSuppression(!checked)}
                  disabled={isProcessing}
                  className="scale-90"
                />
                <span className="text-xs text-muted-foreground">
                  {pattern.isSuppressed ? 'Off' : 'On'}
                </span>
              </div>
            </div>

            {/* Receipt Match Requirement */}
            <div className="flex items-center justify-between mt-2 pt-2 border-t border-border/50">
              <span className="text-xs text-muted-foreground">Receipt required</span>
              <div className="flex items-center gap-2">
                <Switch
                  checked={pattern.requiresReceiptMatch}
                  onCheckedChange={onToggleReceiptMatch}
                  disabled={isProcessing}
                  className="scale-90"
                />
                <span className="text-xs text-muted-foreground">
                  {pattern.requiresReceiptMatch ? 'Yes' : 'No'}
                </span>
              </div>
            </div>

            <div className="text-xs text-muted-foreground mt-2">
              {pattern.occurrenceCount} occurrence
              {pattern.occurrenceCount !== 1 ? 's' : ''}
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

/**
 * Virtualized pattern grid component
 */
export function PatternGrid({
  patterns,
  isLoading = false,
  sort,
  selection,
  expandedIds,
  onSortChange,
  onSelectionChange,
  onExpandedChange,
  onToggleSuppression,
  onToggleReceiptMatch,
  onDelete,
  isProcessing = false,
  containerHeight = 600,
  hasFilters = false,
}: PatternGridProps) {
  const [isMobile, setIsMobile] = useState(false)

  // Detect mobile viewport
  useEffect(() => {
    const checkMobile = () => setIsMobile(window.innerWidth < 768)
    checkMobile()
    window.addEventListener('resize', checkMobile)
    return () => window.removeEventListener('resize', checkMobile)
  }, [])

  // Handle sort column click
  const handleSortClick = useCallback(
    (field: PatternSortField) => {
      if (sort.field === field) {
        onSortChange({
          field,
          direction: sort.direction === 'asc' ? 'desc' : 'asc',
        })
      } else {
        onSortChange({
          field,
          direction: 'desc',
        })
      }
    },
    [sort, onSortChange]
  )

  // Handle select all toggle
  const handleSelectAll = useCallback(() => {
    if (selection.isSelectAll) {
      onSelectionChange({
        selectedIds: new Set(),
        lastSelectedId: null,
        isSelectAll: false,
      })
    } else {
      onSelectionChange({
        selectedIds: new Set(patterns.map((p) => p.id)),
        lastSelectedId: null,
        isSelectAll: true,
      })
    }
  }, [selection.isSelectAll, patterns, onSelectionChange])

  // Handle row selection
  const handleRowSelect = useCallback(
    (patternId: string, shiftKey: boolean) => {
      const newSelection = new Set(selection.selectedIds)

      if (shiftKey && selection.lastSelectedId) {
        const lastIndex = patterns.findIndex(
          (p) => p.id === selection.lastSelectedId
        )
        const currentIndex = patterns.findIndex((p) => p.id === patternId)

        if (lastIndex !== -1 && currentIndex !== -1) {
          const start = Math.min(lastIndex, currentIndex)
          const end = Math.max(lastIndex, currentIndex)

          for (let i = start; i <= end; i++) {
            newSelection.add(patterns[i].id)
          }
        }
      } else {
        if (newSelection.has(patternId)) {
          newSelection.delete(patternId)
        } else {
          newSelection.add(patternId)
        }
      }

      onSelectionChange({
        selectedIds: newSelection,
        lastSelectedId: patternId,
        isSelectAll: newSelection.size === patterns.length,
      })
    },
    [selection, patterns, onSelectionChange]
  )

  // Handle row expansion toggle
  const handleToggleExpand = useCallback(
    (patternId: string) => {
      const newExpanded = new Set(expandedIds)
      if (newExpanded.has(patternId)) {
        newExpanded.delete(patternId)
      } else {
        newExpanded.add(patternId)
      }
      onExpandedChange(newExpanded)
    },
    [expandedIds, onExpandedChange]
  )

  // Render sort indicator
  const renderSortIndicator = (field: PatternSortField) => {
    if (sort.field !== field) {
      return <ArrowUpDown className="h-3.5 w-3.5 ml-1 opacity-50" />
    }
    return sort.direction === 'asc' ? (
      <ArrowUp className="h-3.5 w-3.5 ml-1" />
    ) : (
      <ArrowDown className="h-3.5 w-3.5 ml-1" />
    )
  }

  // Indeterminate state for select-all checkbox
  const isIndeterminate =
    selection.selectedIds.size > 0 &&
    selection.selectedIds.size < patterns.length

  // Check for empty state
  if (!isLoading && patterns.length === 0) {
    return <EmptyState hasFilters={hasFilters} />
  }

  // Mobile View
  if (isMobile) {
    return (
      <div className="space-y-3">
        {/* Mobile Header with Sort */}
        <div className="flex items-center justify-between px-1">
          <div className="flex items-center gap-2">
            <Checkbox
              checked={selection.isSelectAll}
              ref={(el) => {
                if (el) {
                  ;(el as HTMLInputElement).indeterminate = isIndeterminate
                }
              }}
              onCheckedChange={handleSelectAll}
              aria-label="Select all patterns"
            />
            <span className="text-sm text-muted-foreground">
              {selection.selectedIds.size > 0
                ? `${selection.selectedIds.size} selected`
                : `${patterns.length} patterns`}
            </span>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={() =>
              handleSortClick(
                sort.field === 'accuracyRate' ? 'occurrenceCount' : 'accuracyRate'
              )
            }
          >
            Sort: {sort.field === 'accuracyRate' ? 'Accuracy' : 'Frequency'}
            {renderSortIndicator(sort.field)}
          </Button>
        </div>

        {isLoading ? (
          <MobileLoadingSkeleton count={5} />
        ) : (
          <div className="space-y-3">
            {patterns.map((pattern) => (
              <MobilePatternCard
                key={pattern.id}
                pattern={pattern}
                isSelected={selection.selectedIds.has(pattern.id)}
                onSelect={() => handleRowSelect(pattern.id, false)}
                onToggleSuppression={(isSuppressed) =>
                  onToggleSuppression(pattern.id, isSuppressed)
                }
                onToggleReceiptMatch={(requiresReceiptMatch) =>
                  onToggleReceiptMatch(pattern.id, requiresReceiptMatch)
                }
                isProcessing={isProcessing}
              />
            ))}
          </div>
        )}
      </div>
    )
  }

  // Desktop View - render all patterns directly (no virtualization needed for typical counts)
  return (
    <div className="space-y-0">
      <div
        className="overflow-auto rounded-md border"
        style={{ maxHeight: containerHeight }}
      >
        <Table>
          <TableHeader className="sticky top-0 bg-background z-10">
            <TableRow>
              {/* Select All Checkbox */}
              <TableHead className="w-[40px] px-2">
                <Checkbox
                  checked={selection.isSelectAll}
                  ref={(el) => {
                    if (el) {
                      const input = el as unknown as HTMLInputElement
                      input.indeterminate = isIndeterminate
                    }
                  }}
                  onCheckedChange={handleSelectAll}
                  aria-label="Select all patterns"
                />
              </TableHead>

              {/* Vendor */}
              <TableHead>
                <Button
                  variant="ghost"
                  size="sm"
                  className="-ml-3 h-8 data-[state=open]:bg-accent"
                  onClick={() => handleSortClick('displayName')}
                >
                  Vendor
                  {renderSortIndicator('displayName')}
                </Button>
              </TableHead>

              {/* Category */}
              <TableHead className="w-[120px]">Category</TableHead>

              {/* Average Amount */}
              <TableHead className="w-[100px] text-right">
                <Button
                  variant="ghost"
                  size="sm"
                  className="-mr-3 h-8 data-[state=open]:bg-accent"
                  onClick={() => handleSortClick('averageAmount')}
                >
                  Avg Amount
                  {renderSortIndicator('averageAmount')}
                </Button>
              </TableHead>

              {/* Accuracy */}
              <TableHead className="w-[120px]">
                <Button
                  variant="ghost"
                  size="sm"
                  className="-ml-3 h-8 data-[state=open]:bg-accent"
                  onClick={() => handleSortClick('accuracyRate')}
                >
                  Accuracy
                  {renderSortIndicator('accuracyRate')}
                </Button>
              </TableHead>

              {/* Status */}
              <TableHead className="w-[140px]">Status</TableHead>
            </TableRow>
          </TableHeader>

          <TableBody>
            {isLoading ? (
              <LoadingSkeleton count={8} />
            ) : (
              patterns.map((pattern) => (
                <PatternRow
                  key={pattern.id}
                  pattern={pattern}
                  isSelected={selection.selectedIds.has(pattern.id)}
                  isExpanded={expandedIds.has(pattern.id)}
                  onSelect={handleRowSelect}
                  onToggleExpand={handleToggleExpand}
                  onToggleSuppression={onToggleSuppression}
                  onToggleReceiptMatch={onToggleReceiptMatch}
                  onDelete={onDelete}
                  isProcessing={isProcessing}
                />
              ))
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  )
}
