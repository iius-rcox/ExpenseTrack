/**
 * Transaction Query Hooks (T050)
 *
 * TanStack Query hooks for transaction data fetching and caching.
 * Implements search, filtering, sorting, and pagination.
 *
 * @see frontend-api-contracts.md Section 3 for API specifications
 * @see src/types/transaction.ts for type definitions
 */

import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { apiFetch, apiUpload } from '@/services/api'
import type {
  TransactionDetail,
  TransactionListResponse,
  StatementImport,
} from '@/types/api'
import type {
  TransactionView,
  TransactionFilters,
  TransactionSortConfig,
} from '@/types/transaction'

// =============================================================================
// Query Keys
// =============================================================================

/**
 * Query key factory for transactions.
 *
 * Hierarchical structure enables targeted cache invalidation:
 * - transactionKeys.all: Invalidate everything
 * - transactionKeys.lists(): Invalidate all list queries
 * - transactionKeys.list(filters): Invalidate specific filter combination
 */
export const transactionKeys = {
  all: ['transactions'] as const,
  lists: () => [...transactionKeys.all, 'list'] as const,
  list: (filters: Record<string, unknown>) => [...transactionKeys.lists(), filters] as const,
  details: () => [...transactionKeys.all, 'detail'] as const,
  detail: (id: string) => [...transactionKeys.details(), id] as const,
  imports: () => [...transactionKeys.all, 'imports'] as const,
  categories: () => [...transactionKeys.all, 'categories'] as const,
  tags: () => [...transactionKeys.all, 'tags'] as const,
}

// =============================================================================
// Transform Helpers
// =============================================================================

/**
 * Transform API TransactionDetail to frontend TransactionView.
 *
 * Handles:
 * - Date parsing from ISO strings
 * - Match status computation from hasMatchedReceipt
 * - Default values for optional fields
 */
export function transformToTransactionView(
  detail: TransactionDetail
): TransactionView {
  return {
    id: detail.id,
    date: new Date(detail.transactionDate),
    description: detail.description,
    merchant: detail.merchantName || detail.description,
    amount: detail.amount,
    category: detail.category || 'Uncategorized',
    categoryId: detail.category || '',
    tags: [],
    notes: '',
    matchStatus: detail.hasMatchedReceipt
      ? 'matched'
      : detail.matchedReceiptId
        ? 'pending'
        : 'unmatched',
    matchedReceiptId: detail.matchedReceiptId || undefined,
    matchConfidence: detail.matchedReceipt?.matchConfidence,
    source: 'import',
    statementId: detail.statementId,
    importFileName: detail.importFileName,
    isEditing: false,
  }
}

// =============================================================================
// Legacy Hook (Backward Compatibility)
// =============================================================================

interface TransactionListParams {
  page?: number
  pageSize?: number
  matched?: boolean
  startDate?: string
  endDate?: string
  search?: string
}

/**
 * @deprecated Use useTransactionListWithFilters for new components.
 * This hook is maintained for backward compatibility.
 */
export function useTransactionList(params: TransactionListParams = {}) {
  const { page = 1, pageSize = 20, matched, startDate, endDate, search } = params

  return useQuery({
    queryKey: transactionKeys.list({ page, pageSize, matched, startDate, endDate, search }),
    queryFn: async () => {
      const searchParams = new URLSearchParams()
      searchParams.set('page', String(page))
      searchParams.set('pageSize', String(pageSize))
      if (matched !== undefined) searchParams.set('matched', String(matched))
      if (startDate) searchParams.set('startDate', startDate)
      if (endDate) searchParams.set('endDate', endDate)
      if (search) searchParams.set('search', search)

      return apiFetch<TransactionListResponse>(`/transactions?${searchParams}`)
    },
    placeholderData: keepPreviousData,
    staleTime: 30_000,
  })
}

// =============================================================================
// Enhanced Hook with Full Filter Support
// =============================================================================

/**
 * Transaction list parameters with full filter support.
 */
export interface UseTransactionListWithFiltersParams {
  filters?: TransactionFilters
  sort?: TransactionSortConfig
  page?: number
  pageSize?: number
  enabled?: boolean
}

/**
 * Transaction list query result with transformed view models.
 */
export interface TransactionListResult {
  transactions: TransactionView[]
  totalCount: number
  page: number
  pageSize: number
  unmatchedCount: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

const DEFAULT_FILTERS: TransactionFilters = {
  search: '',
  dateRange: { start: null, end: null },
  categories: [],
  amountRange: { min: null, max: null },
  matchStatus: [],
  tags: [],
}

const DEFAULT_SORT: TransactionSortConfig = {
  field: 'date',
  direction: 'desc',
}

/**
 * Build URL search string from filter/sort params.
 */
function buildSearchString(
  filters: TransactionFilters,
  sort: TransactionSortConfig,
  page: number,
  pageSize: number
): string {
  const searchParams = new URLSearchParams()

  searchParams.set('page', String(page))
  searchParams.set('pageSize', String(pageSize))
  searchParams.set('sortBy', sort.field)
  searchParams.set('sortOrder', sort.direction)

  if (filters.search) {
    searchParams.set('search', filters.search)
  }
  if (filters.dateRange.start) {
    searchParams.set('startDate', filters.dateRange.start.toISOString().split('T')[0])
  }
  if (filters.dateRange.end) {
    searchParams.set('endDate', filters.dateRange.end.toISOString().split('T')[0])
  }
  if (filters.amountRange.min !== null) {
    searchParams.set('minAmount', String(filters.amountRange.min))
  }
  if (filters.amountRange.max !== null) {
    searchParams.set('maxAmount', String(filters.amountRange.max))
  }

  // Array params
  filters.categories.forEach((cat) => searchParams.append('categories', cat))
  filters.matchStatus.forEach((status) => searchParams.append('matchStatus', status))
  filters.tags.forEach((tag) => searchParams.append('tags', tag))

  return searchParams.toString()
}

/**
 * Hook for fetching paginated, filtered, sorted transaction list.
 *
 * Features:
 * - Full-text search across description, merchant, notes
 * - Multi-dimensional filtering (date, category, amount, status)
 * - Column sorting with direction toggle
 * - Pagination with keepPreviousData for smooth UX
 *
 * @example
 * ```tsx
 * const { data, isLoading } = useTransactionListWithFilters({
 *   filters: { search: 'coffee', categories: ['food'] },
 *   sort: { field: 'date', direction: 'desc' },
 *   page: 1,
 *   pageSize: 50,
 * });
 * ```
 */
export function useTransactionListWithFilters({
  filters = DEFAULT_FILTERS,
  sort = DEFAULT_SORT,
  page = 1,
  pageSize = 50,
  enabled = true,
}: UseTransactionListWithFiltersParams = {}) {
  const queryParams = { filters, sort, page, pageSize }

  return useQuery({
    queryKey: transactionKeys.list(queryParams),
    queryFn: async (): Promise<TransactionListResult> => {
      const searchString = buildSearchString(filters, sort, page, pageSize)
      const response = await apiFetch<TransactionListResponse>(
        `/transactions?${searchString}`
      )

      // Transform API response to view models
      const transactions: TransactionView[] = response.transactions.map((summary) => ({
        id: summary.id,
        date: new Date(summary.transactionDate),
        description: summary.description,
        merchant: summary.description,
        amount: summary.amount,
        category: 'Uncategorized',
        categoryId: '',
        tags: [],
        notes: '',
        matchStatus: summary.hasMatchedReceipt ? 'matched' : 'unmatched',
        matchedReceiptId: undefined,
        matchConfidence: undefined,
        source: 'import' as const,
        isEditing: false,
      }))

      const totalPages = Math.ceil(response.totalCount / response.pageSize)

      return {
        transactions,
        totalCount: response.totalCount,
        page: response.page,
        pageSize: response.pageSize,
        unmatchedCount: response.unmatchedCount,
        totalPages,
        hasNextPage: response.page < totalPages,
        hasPreviousPage: response.page > 1,
      }
    },
    enabled,
    placeholderData: keepPreviousData,
    staleTime: 30_000,
  })
}

export function useTransactionDetail(id: string) {
  return useQuery({
    queryKey: transactionKeys.detail(id),
    queryFn: () => apiFetch<TransactionDetail>(`/transactions/${id}`),
    enabled: !!id,
  })
}

export function useStatementImports() {
  return useQuery({
    queryKey: transactionKeys.imports(),
    queryFn: () => apiFetch<StatementImport[]>('/statements/imports'),
    staleTime: 60_000,
  })
}

interface ImportStatementResult {
  importId: string
  transactionCount: number
  duplicateCount: number
}

export function useImportStatement() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      file,
      onProgress
    }: {
      file: File
      onProgress?: (progress: number) => void
    }) => {
      const formData = new FormData()
      formData.append('file', file)

      return apiUpload<ImportStatementResult>('/statements/import', formData, onProgress)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: transactionKeys.lists() })
      queryClient.invalidateQueries({ queryKey: transactionKeys.imports() })
    },
  })
}

// =============================================================================
// Enhanced Detail Hook with View Model
// =============================================================================

/**
 * Hook for fetching a single transaction with full details as a view model.
 *
 * @param id - Transaction ID
 * @param options - Query options
 */
export function useTransactionDetailView(
  id: string,
  options: { enabled?: boolean } = {}
) {
  const { enabled = true } = options

  return useQuery({
    queryKey: [...transactionKeys.detail(id), 'view'],
    queryFn: async () => {
      const detail = await apiFetch<TransactionDetail>(`/transactions/${id}`)
      return transformToTransactionView(detail)
    },
    enabled: enabled && !!id,
  })
}

// =============================================================================
// Reference Data Hooks
// =============================================================================

/**
 * Hook for fetching available categories for filter dropdown.
 */
export function useTransactionCategories() {
  return useQuery({
    queryKey: transactionKeys.categories(),
    queryFn: async () => {
      try {
        const response = await apiFetch<{ categories: { id: string; name: string }[] }>(
          '/transactions/categories'
        )
        return response.categories
      } catch {
        // Fallback categories if endpoint doesn't exist yet
        return [
          { id: 'food', name: 'Food & Dining' },
          { id: 'transport', name: 'Transportation' },
          { id: 'utilities', name: 'Utilities' },
          { id: 'entertainment', name: 'Entertainment' },
          { id: 'shopping', name: 'Shopping' },
          { id: 'travel', name: 'Travel' },
          { id: 'health', name: 'Health & Medical' },
          { id: 'business', name: 'Business' },
          { id: 'other', name: 'Other' },
        ]
      }
    },
    staleTime: 5 * 60 * 1000,
  })
}

/**
 * Hook for fetching available tags for filter dropdown.
 */
export function useTransactionTags() {
  return useQuery({
    queryKey: transactionKeys.tags(),
    queryFn: async () => {
      try {
        const response = await apiFetch<{ tags: string[] }>('/transactions/tags')
        return response.tags
      } catch {
        return []
      }
    },
    staleTime: 5 * 60 * 1000,
  })
}

// =============================================================================
// Filter Utilities
// =============================================================================

/**
 * Calculate active filter count for UI badge.
 */
export function getActiveFilterCount(filters: TransactionFilters): number {
  let count = 0

  if (filters.search) count++
  if (filters.dateRange.start || filters.dateRange.end) count++
  if (filters.categories.length > 0) count++
  if (filters.amountRange.min !== null || filters.amountRange.max !== null) count++
  if (filters.matchStatus.length > 0) count++
  if (filters.tags.length > 0) count++

  return count
}

/**
 * Check if any filters are active.
 */
export function hasActiveFilters(filters: TransactionFilters): boolean {
  return getActiveFilterCount(filters) > 0
}

/**
 * Reset filters to default state.
 */
export function getDefaultFilters(): TransactionFilters {
  return {
    search: '',
    dateRange: { start: null, end: null },
    categories: [],
    amountRange: { min: null, max: null },
    matchStatus: [],
    tags: [],
  }
}

/**
 * Reset sort to default state.
 */
export function getDefaultSort(): TransactionSortConfig {
  return {
    field: 'date',
    direction: 'desc',
  }
}

// =============================================================================
// Mutation Hooks (T051-T053)
// =============================================================================

/**
 * Transaction update request type.
 */
export interface TransactionUpdateRequest {
  category?: string
  notes?: string
  tags?: string[]
}

/**
 * Hook for updating a single transaction (T051).
 *
 * Features:
 * - Optimistic update for instant UI feedback
 * - Automatic rollback on error
 * - Cache invalidation on success
 *
 * @example
 * ```tsx
 * const updateTransaction = useUpdateTransaction();
 * updateTransaction.mutate({
 *   id: 'tx-123',
 *   updates: { category: 'food', notes: 'Lunch meeting' }
 * });
 * ```
 */
export function useUpdateTransaction() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      id,
      updates,
    }: {
      id: string
      updates: TransactionUpdateRequest
    }) => {
      return apiFetch<TransactionDetail>(`/transactions/${id}`, {
        method: 'PATCH',
        body: JSON.stringify(updates),
      })
    },

    // Optimistic update
    onMutate: async ({ id, updates }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: transactionKeys.lists() })
      await queryClient.cancelQueries({ queryKey: transactionKeys.detail(id) })

      // Snapshot previous values for rollback
      const previousLists = queryClient.getQueriesData({
        queryKey: transactionKeys.lists(),
      })
      const previousDetail = queryClient.getQueryData(transactionKeys.detail(id))

      // Optimistically update list caches
      queryClient.setQueriesData(
        { queryKey: transactionKeys.lists() },
        (old: TransactionListResult | undefined) => {
          if (!old) return old
          return {
            ...old,
            transactions: old.transactions.map((t) =>
              t.id === id
                ? {
                    ...t,
                    category: updates.category ?? t.category,
                    notes: updates.notes ?? t.notes,
                    tags: updates.tags ?? t.tags,
                  }
                : t
            ),
          }
        }
      )

      return { previousLists, previousDetail }
    },

    // Rollback on error
    onError: (_error, _variables, context) => {
      if (context?.previousLists) {
        context.previousLists.forEach(([queryKey, data]) => {
          queryClient.setQueryData(queryKey, data)
        })
      }
      if (context?.previousDetail) {
        queryClient.setQueryData(
          transactionKeys.detail(_variables.id),
          context.previousDetail
        )
      }
    },

    // Refetch on settle
    onSettled: (_, _error, { id }) => {
      queryClient.invalidateQueries({ queryKey: transactionKeys.lists() })
      queryClient.invalidateQueries({ queryKey: transactionKeys.detail(id) })
    },
  })
}

/**
 * Hook for bulk updating multiple transactions (T052).
 *
 * @example
 * ```tsx
 * const bulkUpdate = useBulkUpdateTransactions();
 * bulkUpdate.mutate({
 *   ids: ['tx-1', 'tx-2', 'tx-3'],
 *   updates: { category: 'travel' }
 * });
 * ```
 */
export function useBulkUpdateTransactions() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      ids,
      updates,
    }: {
      ids: string[]
      updates: TransactionUpdateRequest
    }) => {
      return apiFetch<{ updatedCount: number; ids: string[] }>(
        '/transactions/bulk',
        {
          method: 'PATCH',
          body: JSON.stringify({ ids, updates }),
        }
      )
    },

    // Optimistic update for bulk
    onMutate: async ({ ids, updates }) => {
      await queryClient.cancelQueries({ queryKey: transactionKeys.lists() })

      const previousLists = queryClient.getQueriesData({
        queryKey: transactionKeys.lists(),
      })

      const idSet = new Set(ids)

      queryClient.setQueriesData(
        { queryKey: transactionKeys.lists() },
        (old: TransactionListResult | undefined) => {
          if (!old) return old
          return {
            ...old,
            transactions: old.transactions.map((t) =>
              idSet.has(t.id)
                ? {
                    ...t,
                    category: updates.category ?? t.category,
                    notes: updates.notes ?? t.notes,
                    tags: updates.tags ?? t.tags,
                  }
                : t
            ),
          }
        }
      )

      return { previousLists }
    },

    onError: (_error, _variables, context) => {
      if (context?.previousLists) {
        context.previousLists.forEach(([queryKey, data]) => {
          queryClient.setQueryData(queryKey, data)
        })
      }
    },

    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: transactionKeys.lists() })
    },
  })
}

/**
 * Hook for deleting multiple transactions.
 */
export function useBulkDeleteTransactions() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (ids: string[]) => {
      return apiFetch<{ deletedCount: number }>('/transactions/bulk', {
        method: 'DELETE',
        body: JSON.stringify({ ids }),
      })
    },

    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: transactionKeys.lists() })
    },
  })
}

/**
 * Export format options.
 */
export type TransactionExportFormat = 'csv' | 'xlsx'

/**
 * Hook for exporting transactions (T053).
 *
 * @example
 * ```tsx
 * const exportTransactions = useExportTransactions();
 * exportTransactions.mutate({
 *   format: 'csv',
 *   ids: ['tx-1', 'tx-2'], // or omit for all matching filters
 *   filters: currentFilters,
 * });
 * ```
 */
export function useExportTransactions() {
  return useMutation({
    mutationFn: async ({
      format,
      ids,
      filters,
    }: {
      format: TransactionExportFormat
      ids?: string[]
      filters?: TransactionFilters
    }) => {
      const searchParams = new URLSearchParams()
      searchParams.set('format', format)

      // If specific IDs provided, use those
      if (ids && ids.length > 0) {
        ids.forEach((id) => searchParams.append('ids', id))
      } else if (filters) {
        // Otherwise use current filters
        if (filters.search) searchParams.set('search', filters.search)
        if (filters.dateRange.start) {
          searchParams.set(
            'startDate',
            filters.dateRange.start.toISOString().split('T')[0]
          )
        }
        if (filters.dateRange.end) {
          searchParams.set(
            'endDate',
            filters.dateRange.end.toISOString().split('T')[0]
          )
        }
        filters.categories.forEach((cat) =>
          searchParams.append('categories', cat)
        )
        filters.matchStatus.forEach((status) =>
          searchParams.append('matchStatus', status)
        )
      }

      // Trigger file download
      const { apiDownload } = await import('@/services/api')
      const filename = `transactions-export-${new Date().toISOString().split('T')[0]}.${format}`
      await apiDownload(`/transactions/export?${searchParams}`, filename)

      return { filename }
    },
  })
}
