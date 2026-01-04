'use client'

/**
 * ExtractionCorrectionsList Component (T051, T052, T053)
 *
 * Admin component for viewing extraction corrections with filtering.
 * Feature 024: Extraction Editor Training
 *
 * Features:
 * - DataTable with sortable columns
 * - Field name dropdown filter
 * - Date range picker for filtering
 * - User attribution column
 * - Link to receipt detail
 * - Pagination controls
 */

import { useMemo, useCallback } from 'react'
import { Link } from '@tanstack/react-router'
import { format, parseISO } from 'date-fns'
import {
  useExtractionCorrections,
  CORRECTION_FIELD_NAMES,
  type ExtractionCorrectionQueryParams,
  type ExtractionCorrection,
} from '@/hooks/queries/use-extraction-corrections'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'
import {
  ChevronLeft,
  ChevronRight,
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
  FileText,
  Calendar,
  User,
  Filter,
  X,
} from 'lucide-react'

interface ExtractionCorrectionsListProps {
  queryParams: ExtractionCorrectionQueryParams
  onFilterChange: (params: ExtractionCorrectionQueryParams) => void
  onPageChange: (page: number) => void
}

export function ExtractionCorrectionsList({
  queryParams,
  onFilterChange,
  onPageChange,
}: ExtractionCorrectionsListProps) {
  const { data, isLoading, error } = useExtractionCorrections(queryParams)

  // Handle field filter change
  const handleFieldChange = useCallback(
    (value: string) => {
      onFilterChange({
        ...queryParams,
        fieldName: value === 'all' ? undefined : value,
        page: 1, // Reset to first page
      })
    },
    [queryParams, onFilterChange]
  )

  // Handle date filter changes
  const handleStartDateChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      onFilterChange({
        ...queryParams,
        startDate: e.target.value || undefined,
        page: 1,
      })
    },
    [queryParams, onFilterChange]
  )

  const handleEndDateChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      onFilterChange({
        ...queryParams,
        endDate: e.target.value || undefined,
        page: 1,
      })
    },
    [queryParams, onFilterChange]
  )

  // Handle sort change
  const handleSortChange = useCallback(
    (field: string) => {
      const newDirection =
        queryParams.sortBy === field && queryParams.sortDirection === 'desc'
          ? 'asc'
          : 'desc'
      onFilterChange({
        ...queryParams,
        sortBy: field,
        sortDirection: newDirection,
      })
    },
    [queryParams, onFilterChange]
  )

  // Clear all filters
  const handleClearFilters = useCallback(() => {
    onFilterChange({
      page: 1,
      pageSize: queryParams.pageSize,
      sortBy: 'createdAt',
      sortDirection: 'desc',
    })
  }, [queryParams.pageSize, onFilterChange])

  // Check if any filters are active
  const hasActiveFilters = useMemo(() => {
    return !!(
      queryParams.fieldName ||
      queryParams.startDate ||
      queryParams.endDate ||
      queryParams.userId
    )
  }, [queryParams])

  // Get sort icon for column
  const getSortIcon = (field: string) => {
    if (queryParams.sortBy !== field) {
      return <ArrowUpDown className="h-4 w-4 ml-1 opacity-50" />
    }
    return queryParams.sortDirection === 'desc' ? (
      <ArrowDown className="h-4 w-4 ml-1" />
    ) : (
      <ArrowUp className="h-4 w-4 ml-1" />
    )
  }

  // Format correction for display
  const formatValue = (value: string | null): string => {
    if (value === null || value === '') return '(empty)'
    return value
  }

  // Get field badge color
  const getFieldBadgeVariant = (
    fieldName: string
  ): 'default' | 'secondary' | 'outline' => {
    switch (fieldName) {
      case 'vendor':
        return 'default'
      case 'amount':
      case 'tax':
        return 'secondary'
      default:
        return 'outline'
    }
  }

  if (error) {
    return (
      <Card className="border-destructive">
        <CardContent className="pt-6">
          <p className="text-destructive">
            Failed to load corrections. Please try again.
          </p>
        </CardContent>
      </Card>
    )
  }

  return (
    <div className="space-y-4">
      {/* Filters */}
      <Card>
        <CardHeader className="pb-3">
          <div className="flex items-center justify-between">
            <CardTitle className="text-base flex items-center gap-2">
              <Filter className="h-4 w-4" />
              Filters
            </CardTitle>
            {hasActiveFilters && (
              <Button
                variant="ghost"
                size="sm"
                onClick={handleClearFilters}
                className="h-8"
              >
                <X className="h-4 w-4 mr-1" />
                Clear
              </Button>
            )}
          </div>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {/* Field Name Filter */}
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2">
                <FileText className="h-4 w-4 text-muted-foreground" />
                Field Name
              </label>
              <Select
                value={queryParams.fieldName || 'all'}
                onValueChange={handleFieldChange}
              >
                <SelectTrigger>
                  <SelectValue placeholder="All fields" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All fields</SelectItem>
                  {CORRECTION_FIELD_NAMES.map((field) => (
                    <SelectItem key={field.value} value={field.value}>
                      {field.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Start Date Filter */}
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2">
                <Calendar className="h-4 w-4 text-muted-foreground" />
                Start Date
              </label>
              <Input
                type="date"
                value={queryParams.startDate || ''}
                onChange={handleStartDateChange}
              />
            </div>

            {/* End Date Filter */}
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2">
                <Calendar className="h-4 w-4 text-muted-foreground" />
                End Date
              </label>
              <Input
                type="date"
                value={queryParams.endDate || ''}
                onChange={handleEndDateChange}
              />
            </div>

            {/* User Filter (text input for now) */}
            <div className="space-y-2">
              <label className="text-sm font-medium flex items-center gap-2">
                <User className="h-4 w-4 text-muted-foreground" />
                User ID
              </label>
              <Input
                type="text"
                placeholder="Filter by user ID"
                value={queryParams.userId || ''}
                onChange={(e) =>
                  onFilterChange({
                    ...queryParams,
                    userId: e.target.value || undefined,
                    page: 1,
                  })
                }
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Table */}
      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead
                  className="cursor-pointer select-none"
                  onClick={() => handleSortChange('createdAt')}
                >
                  <div className="flex items-center">
                    Date
                    {getSortIcon('createdAt')}
                  </div>
                </TableHead>
                <TableHead
                  className="cursor-pointer select-none"
                  onClick={() => handleSortChange('fieldName')}
                >
                  <div className="flex items-center">
                    Field
                    {getSortIcon('fieldName')}
                  </div>
                </TableHead>
                <TableHead>Original Value</TableHead>
                <TableHead>Corrected Value</TableHead>
                <TableHead
                  className="cursor-pointer select-none"
                  onClick={() => handleSortChange('userName')}
                >
                  <div className="flex items-center">
                    User
                    {getSortIcon('userName')}
                  </div>
                </TableHead>
                <TableHead className="text-right">Receipt</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                // Loading skeleton
                Array.from({ length: 5 }).map((_, i) => (
                  <TableRow key={i}>
                    <TableCell>
                      <Skeleton className="h-4 w-24" />
                    </TableCell>
                    <TableCell>
                      <Skeleton className="h-5 w-16" />
                    </TableCell>
                    <TableCell>
                      <Skeleton className="h-4 w-32" />
                    </TableCell>
                    <TableCell>
                      <Skeleton className="h-4 w-32" />
                    </TableCell>
                    <TableCell>
                      <Skeleton className="h-4 w-24" />
                    </TableCell>
                    <TableCell className="text-right">
                      <Skeleton className="h-8 w-16 ml-auto" />
                    </TableCell>
                  </TableRow>
                ))
              ) : data?.items.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} className="text-center py-8">
                    <p className="text-muted-foreground">No corrections found</p>
                    {hasActiveFilters && (
                      <Button
                        variant="link"
                        size="sm"
                        onClick={handleClearFilters}
                        className="mt-2"
                      >
                        Clear filters
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ) : (
                data?.items.map((correction: ExtractionCorrection) => (
                  <TableRow key={correction.id}>
                    <TableCell className="font-mono text-sm">
                      {format(parseISO(correction.createdAt), 'MMM d, yyyy h:mm a')}
                    </TableCell>
                    <TableCell>
                      <Badge variant={getFieldBadgeVariant(correction.fieldName)}>
                        {correction.fieldName}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <span
                        className={cn(
                          'text-sm',
                          correction.originalValue === '' && 'text-muted-foreground italic'
                        )}
                      >
                        {formatValue(correction.originalValue)}
                      </span>
                    </TableCell>
                    <TableCell>
                      <span
                        className={cn(
                          'text-sm font-medium',
                          correction.correctedValue === null && 'text-muted-foreground italic'
                        )}
                      >
                        {formatValue(correction.correctedValue)}
                      </span>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <User className="h-3 w-3 text-muted-foreground" />
                        <span className="text-sm">{correction.userName}</span>
                      </div>
                    </TableCell>
                    <TableCell className="text-right">
                      <Button variant="ghost" size="sm" asChild>
                        <Link
                          to="/receipts/$receiptId"
                          params={{ receiptId: correction.receiptId }}
                        >
                          View
                        </Link>
                      </Button>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            Showing {((data.page - 1) * data.pageSize) + 1} to{' '}
            {Math.min(data.page * data.pageSize, data.totalCount)} of{' '}
            {data.totalCount} corrections
          </p>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={!data.hasPreviousPage}
              onClick={() => onPageChange(data.page - 1)}
            >
              <ChevronLeft className="h-4 w-4 mr-1" />
              Previous
            </Button>
            <span className="text-sm text-muted-foreground px-2">
              Page {data.page} of {data.totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              disabled={!data.hasNextPage}
              onClick={() => onPageChange(data.page + 1)}
            >
              Next
              <ChevronRight className="h-4 w-4 ml-1" />
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}

/**
 * Skeleton loader for ExtractionCorrectionsList
 */
export function ExtractionCorrectionsListSkeleton() {
  return (
    <div className="space-y-4">
      {/* Filter skeleton */}
      <Card>
        <CardHeader className="pb-3">
          <Skeleton className="h-5 w-20" />
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="h-10 w-full" />
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Table skeleton */}
      <Card>
        <CardContent className="p-0">
          <div className="p-4 space-y-4">
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} className="h-12 w-full" />
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
