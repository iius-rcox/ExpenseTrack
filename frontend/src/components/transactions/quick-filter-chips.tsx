/**
 * Quick Filter Chips Component
 *
 * Pre-defined filter shortcuts for common transaction filter combinations.
 * Provides one-click access to frequently used filters like "This Week",
 * "Unmatched", "High Value", etc.
 */

import { useCallback } from 'react'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import type { TransactionFilters } from '@/types/transaction'

interface QuickFilterChipsProps {
  /** Current filter state */
  filters: TransactionFilters
  /** Callback when a chip is clicked (applies its filter preset) */
  onApplyFilter: (filters: TransactionFilters) => void
  /** Default/empty filters state for resetting */
  defaultFilters: TransactionFilters
}

/**
 * A quick filter chip definition.
 */
interface QuickFilterChip {
  /** Unique identifier */
  id: string
  /** Display label */
  label: string
  /** Icon emoji (optional) */
  icon?: string
  /** Filter values to apply when chip is clicked */
  getFilters: (defaults: TransactionFilters) => TransactionFilters
  /** Check if this chip's filter is currently active */
  isActive: (filters: TransactionFilters) => boolean
}

/**
 * Helper to get start of current week (Sunday).
 */
function getStartOfWeek(): Date {
  const today = new Date()
  const dayOfWeek = today.getDay()
  const start = new Date(today)
  start.setDate(today.getDate() - dayOfWeek)
  start.setHours(0, 0, 0, 0)
  return start
}

/**
 * Helper to get start of current month.
 */
function getStartOfMonth(): Date {
  const today = new Date()
  return new Date(today.getFullYear(), today.getMonth(), 1)
}

/**
 * Helper to get start of last 30 days.
 */
function getLast30Days(): Date {
  const date = new Date()
  date.setDate(date.getDate() - 30)
  return date
}

/**
 * Pre-defined quick filter chips.
 */
const QUICK_FILTERS: QuickFilterChip[] = [
  {
    id: 'this-week',
    label: 'This Week',
    icon: '📅',
    getFilters: (defaults) => ({
      ...defaults,
      dateRange: {
        start: getStartOfWeek(),
        end: new Date(),
      },
    }),
    isActive: (filters) => {
      if (!filters.dateRange.start) return false
      const weekStart = getStartOfWeek()
      return filters.dateRange.start.toDateString() === weekStart.toDateString()
    },
  },
  {
    id: 'this-month',
    label: 'This Month',
    icon: '🗓️',
    getFilters: (defaults) => ({
      ...defaults,
      dateRange: {
        start: getStartOfMonth(),
        end: new Date(),
      },
    }),
    isActive: (filters) => {
      if (!filters.dateRange.start) return false
      const monthStart = getStartOfMonth()
      return filters.dateRange.start.toDateString() === monthStart.toDateString()
    },
  },
  {
    id: 'unmatched',
    label: 'Unmatched',
    icon: '🔍',
    // Only show BUSINESS transactions that are unmatched (personal transactions don't need matching)
    getFilters: (defaults) => ({
      ...defaults,
      matchStatus: ['unmatched'],
      reimbursability: ['business'],
    }),
    isActive: (filters) =>
      filters.matchStatus.length === 1 &&
      filters.matchStatus[0] === 'unmatched' &&
      filters.reimbursability.length === 1 &&
      filters.reimbursability[0] === 'business',
  },
  {
    id: 'needs-receipt',
    label: 'Needs Receipt',
    icon: '🧾',
    // Business transactions that need a receipt attached (unmatched or pending)
    getFilters: (defaults) => ({
      ...defaults,
      matchStatus: ['unmatched', 'pending'],
      reimbursability: ['business'],
    }),
    isActive: (filters) =>
      filters.matchStatus.length === 2 &&
      filters.matchStatus.includes('unmatched') &&
      filters.matchStatus.includes('pending') &&
      filters.reimbursability.length === 1 &&
      filters.reimbursability[0] === 'business',
  },
  {
    id: 'needs-review',
    label: 'Needs Review',
    icon: '💡',
    // AI predictions awaiting user confirmation (business transactions only)
    getFilters: (defaults) => ({
      ...defaults,
      hasPendingPrediction: true,
      reimbursability: ['business'],
    }),
    isActive: (filters) =>
      filters.hasPendingPrediction === true &&
      filters.reimbursability.length === 1 &&
      filters.reimbursability[0] === 'business',
  },
  {
    id: 'high-value',
    label: 'High Value ($100+)',
    icon: '💰',
    getFilters: (defaults) => ({
      ...defaults,
      amountRange: {
        min: 100,
        max: null,
      },
    }),
    isActive: (filters) =>
      filters.amountRange.min === 100 && filters.amountRange.max === null,
  },
  {
    id: 'last-30-days',
    label: 'Last 30 Days',
    icon: '📆',
    getFilters: (defaults) => ({
      ...defaults,
      dateRange: {
        start: getLast30Days(),
        end: new Date(),
      },
    }),
    isActive: (filters) => {
      if (!filters.dateRange.start) return false
      const thirtyDaysAgo = getLast30Days()
      // Allow 1 day variance for timing
      const diff = Math.abs(
        filters.dateRange.start.getTime() - thirtyDaysAgo.getTime()
      )
      return diff < 86400000 // Within 1 day
    },
  },
]

export function QuickFilterChips({
  filters,
  onApplyFilter,
  defaultFilters,
}: QuickFilterChipsProps) {
  // Future: could show "Clear All" chip when filters are active
  // const hasFilters = useMemo(() => hasActiveFilters(filters), [filters])

  // Handle chip click - toggle behavior while preserving other filters
  const handleChipClick = useCallback(
    (chip: QuickFilterChip) => {
      if (chip.isActive(filters)) {
        // If chip is active, clear only this chip's filter fields (preserve other filters)
        // Get the chip's filter to know which fields to clear
        const chipFilter = chip.getFilters(defaultFilters)
        const clearedFilter = { ...filters }

        // Clear only the fields that the chip sets
        if (chipFilter.matchStatus.length > 0) {
          clearedFilter.matchStatus = []
        }
        if (chipFilter.reimbursability.length > 0) {
          clearedFilter.reimbursability = []
        }
        if (chipFilter.hasPendingPrediction) {
          clearedFilter.hasPendingPrediction = false
        }
        if (chipFilter.dateRange.start || chipFilter.dateRange.end) {
          clearedFilter.dateRange = { start: null, end: null }
        }

        onApplyFilter(clearedFilter)
      } else {
        // Apply the chip's filter preset while preserving other filters
        onApplyFilter(chip.getFilters(filters))
      }
    },
    [filters, onApplyFilter, defaultFilters]
  )

  return (
    <div className="flex flex-wrap gap-2">
      {QUICK_FILTERS.map((chip) => {
        const active = chip.isActive(filters)
        return (
          <Badge
            key={chip.id}
            variant={active ? 'default' : 'outline'}
            className={cn(
              'cursor-pointer transition-colors select-none',
              active
                ? 'bg-primary hover:bg-primary/90'
                : 'hover:bg-muted hover:border-muted-foreground/30'
            )}
            onClick={() => handleChipClick(chip)}
          >
            {chip.icon && <span className="mr-1">{chip.icon}</span>}
            {chip.label}
          </Badge>
        )
      })}
    </div>
  )
}
