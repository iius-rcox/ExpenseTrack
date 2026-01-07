/**
 * PatternFilterPanel Component
 *
 * Filter controls for the pattern grid.
 * Provides search, category, and status filtering.
 *
 * @see specs/023-expense-prediction/plan.md for pattern management requirements
 */

import { useState, useCallback, useEffect } from 'react'
import { Search, X, Filter } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { cn } from '@/lib/utils'
import type { PatternGridFilters, PatternStatusFilter } from '@/types/prediction'

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
      console.warn('[PatternFilterPanel] Empty object detected, using fallback');
      return fallback;
    }
    return fallback;
  }
  return String(value);
}

export interface PatternFilterPanelProps {
  /** Current filter state */
  filters: PatternGridFilters
  /** Available categories for filtering */
  categories?: string[]
  /** Callback when filters change */
  onFiltersChange: (filters: PatternGridFilters) => void
  /** Additional CSS classes */
  className?: string
}

/**
 * Status filter options
 */
const STATUS_OPTIONS: { value: PatternStatusFilter; label: string }[] = [
  { value: 'all', label: 'All Patterns' },
  { value: 'active', label: 'Active Only' },
  { value: 'suppressed', label: 'Suppressed Only' },
]

/**
 * Calculate active filter count
 */
function getActiveFilterCount(filters: PatternGridFilters): number {
  let count = 0
  if (filters.search) count++
  if (filters.status !== 'all') count++
  if (filters.category) count++
  return count
}

/**
 * PatternFilterPanel provides filtering controls
 */
export function PatternFilterPanel({
  filters,
  categories = [],
  onFiltersChange,
  className,
}: PatternFilterPanelProps) {
  const [searchValue, setSearchValue] = useState(filters.search)
  const activeFilterCount = getActiveFilterCount(filters)

  // Debounce search input
  useEffect(() => {
    const timer = setTimeout(() => {
      if (searchValue !== filters.search) {
        onFiltersChange({ ...filters, search: searchValue })
      }
    }, 300)

    return () => clearTimeout(timer)
  }, [searchValue, filters, onFiltersChange])

  // Sync search value with external changes
  useEffect(() => {
    setSearchValue(filters.search)
  }, [filters.search])

  const handleStatusChange = useCallback(
    (value: string) => {
      onFiltersChange({
        ...filters,
        status: value as PatternStatusFilter,
        // Update includeSuppressed based on status
        includeSuppressed: value !== 'active',
      })
    },
    [filters, onFiltersChange]
  )

  const handleCategoryChange = useCallback(
    (value: string) => {
      onFiltersChange({
        ...filters,
        category: value === 'all' ? null : value,
      })
    },
    [filters, onFiltersChange]
  )

  const handleClearSearch = useCallback(() => {
    setSearchValue('')
    onFiltersChange({ ...filters, search: '' })
  }, [filters, onFiltersChange])

  const handleClearAllFilters = useCallback(() => {
    setSearchValue('')
    onFiltersChange({
      includeSuppressed: true,
      search: '',
      status: 'all',
      category: null,
    })
  }, [onFiltersChange])

  return (
    <div className={cn('flex flex-col sm:flex-row gap-3', className)}>
      {/* Search Input */}
      <div className="relative flex-1 max-w-sm">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          value={searchValue}
          onChange={(e) => setSearchValue(e.target.value)}
          placeholder="Search patterns..."
          className="pl-9 pr-9"
        />
        {searchValue && (
          <Button
            variant="ghost"
            size="icon"
            className="absolute right-1 top-1/2 -translate-y-1/2 h-7 w-7"
            onClick={handleClearSearch}
          >
            <X className="h-4 w-4" />
            <span className="sr-only">Clear search</span>
          </Button>
        )}
      </div>

      {/* Status Filter */}
      <Select value={filters.status} onValueChange={handleStatusChange}>
        <SelectTrigger className="w-[160px]">
          <SelectValue placeholder="Status" />
        </SelectTrigger>
        <SelectContent>
          {STATUS_OPTIONS.map((option) => (
            <SelectItem key={option.value} value={option.value}>
              {option.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      {/* Category Filter (if categories available) */}
      {categories.length > 0 && (
        <Select
          value={filters.category || 'all'}
          onValueChange={handleCategoryChange}
        >
          <SelectTrigger className="w-[160px]">
            <SelectValue placeholder="Category" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Categories</SelectItem>
            {categories.map((category) => (
              <SelectItem key={safeDisplayString(category, `cat-${Math.random()}`)} value={safeDisplayString(category) || `unknown-${Math.random()}`}>
                {safeDisplayString(category, 'Unknown')}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      )}

      {/* Active Filters Badge & Clear */}
      {activeFilterCount > 0 && (
        <div className="flex items-center gap-2">
          <Badge variant="secondary" className="gap-1">
            <Filter className="h-3 w-3" />
            {activeFilterCount} filter{activeFilterCount !== 1 ? 's' : ''}
          </Badge>
          <Button
            variant="ghost"
            size="sm"
            onClick={handleClearAllFilters}
            className="h-8 px-2 text-xs"
          >
            Clear all
          </Button>
        </div>
      )}
    </div>
  )
}

/**
 * Compact filter panel for mobile
 * Displays filters in a more compact layout without popover
 */
export function PatternFilterPanelCompact({
  filters,
  categories = [],
  onFiltersChange,
  className,
}: PatternFilterPanelProps) {
  const activeFilterCount = getActiveFilterCount(filters)

  return (
    <div className={cn('flex flex-col gap-3', className)}>
      {/* Search Row */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          value={filters.search}
          onChange={(e) => onFiltersChange({ ...filters, search: e.target.value })}
          placeholder="Search patterns..."
          className="pl-9 h-9"
        />
      </div>

      {/* Filters Row */}
      <div className="flex items-center gap-2">
        {/* Status */}
        <Select
          value={filters.status}
          onValueChange={(value) => {
            onFiltersChange({
              ...filters,
              status: value as PatternStatusFilter,
              includeSuppressed: value !== 'active',
            })
          }}
        >
          <SelectTrigger className="h-9 flex-1">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {STATUS_OPTIONS.map((option) => (
              <SelectItem key={option.value} value={option.value}>
                {option.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {/* Category */}
        {categories.length > 0 && (
          <Select
            value={filters.category || 'all'}
            onValueChange={(value) => {
              onFiltersChange({
                ...filters,
                category: value === 'all' ? null : value,
              })
            }}
          >
            <SelectTrigger className="h-9 flex-1">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Categories</SelectItem>
              {categories.map((category) => (
                <SelectItem key={safeDisplayString(category, `cat-${Math.random()}`)} value={safeDisplayString(category) || `unknown-${Math.random()}`}>
                  {safeDisplayString(category, 'Unknown')}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}

        {/* Clear Button */}
        {activeFilterCount > 0 && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              onFiltersChange({
                includeSuppressed: true,
                search: '',
                status: 'all',
                category: null,
              })
            }}
            className="h-9 px-2"
          >
            <X className="h-4 w-4" />
          </Button>
        )}
      </div>
    </div>
  )
}
