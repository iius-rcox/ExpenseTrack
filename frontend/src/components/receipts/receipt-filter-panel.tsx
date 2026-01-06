'use client'

import { useState, useCallback, useEffect } from 'react'
import { Search, X, SlidersHorizontal, ArrowUpDown } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { cn } from '@/lib/utils'

export interface ReceiptFilters {
  status?: string
  matchStatus?: 'unmatched' | 'proposed' | 'matched'
  vendor?: string
  dateFrom?: string
  dateTo?: string
  sortBy: 'date' | 'amount' | 'vendor' | 'created'
  sortOrder: 'asc' | 'desc'
}

interface ReceiptFilterPanelProps {
  filters: ReceiptFilters
  onFiltersChange: (filters: ReceiptFilters) => void
  statusCounts?: Record<string, number>
  totalCount?: number
  className?: string
}

const STATUS_OPTIONS = [
  { value: 'all', label: 'All Status' },
  { value: 'Uploaded', label: 'Pending' },
  { value: 'Processing', label: 'Processing' },
  { value: 'Ready', label: 'Processed' },
  { value: 'Error', label: 'Error' },
]

const MATCH_STATUS_OPTIONS = [
  { value: 'all', label: 'All Match' },
  { value: 'unmatched', label: 'Unmatched' },
  { value: 'proposed', label: 'Proposed' },
  { value: 'matched', label: 'Matched' },
]

const SORT_OPTIONS = [
  { value: 'created-desc', label: 'Newest First', sortBy: 'created', sortOrder: 'desc' },
  { value: 'created-asc', label: 'Oldest First', sortBy: 'created', sortOrder: 'asc' },
  { value: 'date-desc', label: 'Receipt Date (Newest)', sortBy: 'date', sortOrder: 'desc' },
  { value: 'date-asc', label: 'Receipt Date (Oldest)', sortBy: 'date', sortOrder: 'asc' },
  { value: 'amount-desc', label: 'Amount (High to Low)', sortBy: 'amount', sortOrder: 'desc' },
  { value: 'amount-asc', label: 'Amount (Low to High)', sortBy: 'amount', sortOrder: 'asc' },
  { value: 'vendor-asc', label: 'Vendor (A-Z)', sortBy: 'vendor', sortOrder: 'asc' },
  { value: 'vendor-desc', label: 'Vendor (Z-A)', sortBy: 'vendor', sortOrder: 'desc' },
] as const

export function ReceiptFilterPanel({
  filters,
  onFiltersChange,
  statusCounts,
  totalCount,
  className,
}: ReceiptFilterPanelProps) {
  const [searchInput, setSearchInput] = useState(filters.vendor || '')

  // Debounce vendor search
  useEffect(() => {
    const timer = setTimeout(() => {
      if (searchInput !== (filters.vendor || '')) {
        onFiltersChange({ ...filters, vendor: searchInput || undefined })
      }
    }, 300)
    return () => clearTimeout(timer)
  }, [searchInput, filters, onFiltersChange])

  // Sync search input with external filter changes
  useEffect(() => {
    if (filters.vendor !== searchInput) {
      setSearchInput(filters.vendor || '')
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filters.vendor])

  const handleStatusChange = useCallback((value: string) => {
    onFiltersChange({
      ...filters,
      status: value === 'all' ? undefined : value,
    })
  }, [filters, onFiltersChange])

  const handleMatchStatusChange = useCallback((value: string) => {
    onFiltersChange({
      ...filters,
      matchStatus: value === 'all' ? undefined : value as 'unmatched' | 'proposed' | 'matched',
    })
  }, [filters, onFiltersChange])

  const handleSortChange = useCallback((value: string) => {
    const option = SORT_OPTIONS.find(o => `${o.sortBy}-${o.sortOrder}` === value)
    if (option) {
      onFiltersChange({
        ...filters,
        sortBy: option.sortBy as ReceiptFilters['sortBy'],
        sortOrder: option.sortOrder as ReceiptFilters['sortOrder'],
      })
    }
  }, [filters, onFiltersChange])

  const handleClearSearch = useCallback(() => {
    setSearchInput('')
    onFiltersChange({ ...filters, vendor: undefined })
  }, [filters, onFiltersChange])

  const handleClearAll = useCallback(() => {
    setSearchInput('')
    onFiltersChange({
      status: undefined,
      matchStatus: undefined,
      vendor: undefined,
      dateFrom: undefined,
      dateTo: undefined,
      sortBy: 'created',
      sortOrder: 'desc',
    })
  }, [onFiltersChange])

  // Count active filters (excluding sort)
  const activeFilterCount = [
    filters.status,
    filters.matchStatus,
    filters.vendor,
    filters.dateFrom,
    filters.dateTo,
  ].filter(Boolean).length

  const currentSortValue = `${filters.sortBy}-${filters.sortOrder}`
  const currentSortLabel = SORT_OPTIONS.find(o => `${o.sortBy}-${o.sortOrder}` === currentSortValue)?.label || 'Sort'

  return (
    <div className={cn('space-y-3', className)}>
      {/* Main filter row */}
      <div className="flex flex-wrap items-center gap-2">
        {/* Search input */}
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
          <Input
            placeholder="Search vendor..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            className="pl-9 pr-8"
          />
          {searchInput && (
            <Button
              variant="ghost"
              size="icon"
              className="absolute right-1 top-1/2 -translate-y-1/2 h-6 w-6"
              onClick={handleClearSearch}
            >
              <X className="h-3 w-3" />
            </Button>
          )}
        </div>

        {/* Status filter */}
        <Select value={filters.status || 'all'} onValueChange={handleStatusChange}>
          <SelectTrigger className="w-[130px]">
            <SelectValue placeholder="Status" />
          </SelectTrigger>
          <SelectContent>
            {STATUS_OPTIONS.map(({ value, label }) => (
              <SelectItem key={value} value={value}>
                <span className="flex items-center gap-2">
                  {label}
                  {statusCounts && value !== 'all' && (
                    <Badge variant="secondary" className="ml-1 text-xs">
                      {statusCounts[value] ?? 0}
                    </Badge>
                  )}
                </span>
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {/* Match status filter */}
        <Select value={filters.matchStatus || 'all'} onValueChange={handleMatchStatusChange}>
          <SelectTrigger className="w-[130px]">
            <SelectValue placeholder="Match" />
          </SelectTrigger>
          <SelectContent>
            {MATCH_STATUS_OPTIONS.map(({ value, label }) => (
              <SelectItem key={value} value={value}>
                {label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        {/* Sort dropdown */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" size="sm" className="gap-2">
              <ArrowUpDown className="h-4 w-4" />
              <span className="hidden sm:inline">{currentSortLabel}</span>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-48">
            {SORT_OPTIONS.map((option, index) => (
              <div key={`${option.sortBy}-${option.sortOrder}`}>
                {index > 0 && index % 2 === 0 && <DropdownMenuSeparator />}
                <DropdownMenuItem
                  onClick={() => handleSortChange(`${option.sortBy}-${option.sortOrder}`)}
                  className={cn(
                    currentSortValue === `${option.sortBy}-${option.sortOrder}` && 'bg-accent'
                  )}
                >
                  {option.label}
                </DropdownMenuItem>
              </div>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>

        {/* Clear filters button */}
        {activeFilterCount > 0 && (
          <Button variant="ghost" size="sm" onClick={handleClearAll} className="gap-1">
            <X className="h-3 w-3" />
            Clear ({activeFilterCount})
          </Button>
        )}
      </div>

      {/* Active filters display */}
      {activeFilterCount > 0 && (
        <div className="flex flex-wrap items-center gap-2">
          <span className="text-sm text-muted-foreground">
            <SlidersHorizontal className="inline h-3 w-3 mr-1" />
            Filters:
          </span>
          {filters.status && (
            <Badge variant="secondary" className="gap-1">
              Status: {STATUS_OPTIONS.find(o => o.value === filters.status)?.label}
              <button
                onClick={() => onFiltersChange({ ...filters, status: undefined })}
                className="ml-1 hover:text-foreground"
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          )}
          {filters.matchStatus && (
            <Badge variant="secondary" className="gap-1">
              Match: {MATCH_STATUS_OPTIONS.find(o => o.value === filters.matchStatus)?.label}
              <button
                onClick={() => onFiltersChange({ ...filters, matchStatus: undefined })}
                className="ml-1 hover:text-foreground"
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          )}
          {filters.vendor && (
            <Badge variant="secondary" className="gap-1">
              Vendor: "{filters.vendor}"
              <button
                onClick={handleClearSearch}
                className="ml-1 hover:text-foreground"
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          )}
          {totalCount !== undefined && (
            <span className="text-sm text-muted-foreground ml-auto">
              {totalCount} {totalCount === 1 ? 'receipt' : 'receipts'}
            </span>
          )}
        </div>
      )}
    </div>
  )
}
