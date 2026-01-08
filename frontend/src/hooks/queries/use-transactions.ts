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
  filterSuggestions: () => [...transactionKeys.all, 'filter-suggestions'] as const,
}

// =============================================================================
// Transform Helpers
// =============================================================================

/**
 * DEFENSIVE HELPER: Safely extract a string value, converting empty objects to empty strings.
 *
 * This guards against React Error #301 where empty objects {} from cached API responses
 * or unexpected backend behavior could be passed as React children.
 *
 * IMPORTANT: {} is truthy in JavaScript, so `value || ''` DOES NOT catch it!
 * This helper explicitly checks for empty objects.
 */
function safeString(value: unknown, fallback = ''): string {
  if (value === null || value === undefined) {
    return fallback;
  }
  if (typeof value === 'string') {
    return value || fallback;
  }
  // Empty object {} - the main culprit of React Error #301
  if (typeof value === 'object' && !Array.isArray(value)) {
    console.warn('[use-transactions] Empty object detected in API response, using fallback');
    return fallback;
  }
  return String(value);
}

/**
 * Transform API TransactionDetail to frontend TransactionView.
 *
 * Handles:
 * - Date parsing from ISO strings
 * - Match status computation from hasMatchedReceipt
 * - Default values for optional fields
 * - Empty object protection (React Error #301)
 */
export function transformToTransactionView(
  detail: TransactionDetail
): TransactionView {
  // Safely extract category string - guards against {} from cached/malformed API responses
  const categoryStr = safeString(detail.category);

  return {
    id: detail.id,
    date: new Date(detail.transactionDate),
    description: safeString(detail.description),
    merchant: safeString(detail.merchantName) || safeString(detail.description),
    amount: detail.amount,
    category: categoryStr || 'Uncategorized',
    categoryId: categoryStr,
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
  hasPendingPrediction: false,
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
  filters.tags.forEach((tag) => searchParams.append('tags', tag))

  // Convert matchStatus array to backend's 'matched' boolean parameter
  // Backend expects: matched=true (has receipt) or matched=false (no receipt)
  // Frontend sends: matchStatus=['matched'] or ['unmatched'] or ['matched', 'unmatched']
  if (filters.matchStatus.length === 1) {
    // Single selection - convert to boolean
    if (filters.matchStatus[0] === 'matched') {
      searchParams.set('matched', 'true')
    } else if (filters.matchStatus[0] === 'unmatched') {
      searchParams.set('matched', 'false')
    }
    // 'pending' status not currently supported by backend
  }
  // If both selected or none selected, don't filter (show all)

  // Predictions filter (Feature 023)
  if (filters.hasPendingPrediction) {
    searchParams.set('hasPendingPrediction', 'true')
  }

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
        prediction: summary.prediction ?? null,
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

/**
 * A smart filter suggestion from the API.
 */
export interface FilterSuggestion {
  /** Suggestion type (merchant, match_status, amount_range, date_range) */
  type: string
  /** Human-readable label */
  label: string
  /** Description explaining the suggestion */
  description: string
  /** The filter value to apply */
  filterValue: unknown
  /** Number of matching transactions */
  transactionCount: number
  /** Relevance score (0-100) */
  relevanceScore: number
}

/**
 * Response from the filter suggestions API.
 */
export interface FilterSuggestionsResponse {
  suggestions: FilterSuggestion[]
  totalTransactions: number
  earliestDate?: string
  latestDate?: string
}

/**
 * Hook for fetching smart filter suggestions.
 * Analyzes the user's transaction data and suggests relevant filters
 * based on patterns like top merchants, date ranges, and match status.
 */
export function useFilterSuggestions() {
  return useQuery({
    queryKey: transactionKeys.filterSuggestions(),
    queryFn: async () => {
      try {
        const response = await apiFetch<FilterSuggestionsResponse>(
          '/transactions/filter-suggestions'
        )
        return response
      } catch {
        // Return empty suggestions if endpoint doesn't exist
        return {
          suggestions: [],
          totalTransactions: 0,
        } as FilterSuggestionsResponse
      }
    },
    // Suggestions don't change frequently - cache for 10 minutes
    staleTime: 10 * 60 * 1000,
    // Only fetch when user interacts with filters, not on initial load
    enabled: true,
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
  if (filters.hasPendingPrediction) count++

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
    hasPendingPrediction: false,
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

// =============================================================================
// Reimbursability Mutation Hooks
// =============================================================================

import type {
  PredictionActionResponse,
  BulkTransactionReimbursabilityRequest,
  BulkTransactionReimbursabilityResponse,
} from '@/types/prediction'

// Also import predictionKeys for cache invalidation
import { predictionKeys } from './use-predictions'

/**
 * Hook for marking a transaction as reimbursable (business expense).
 *
 * Creates or updates a manual override prediction with Confirmed status.
 * This marks the transaction as a valid business expense.
 *
 * @example
 * ```tsx
 * const markReimbursable = useMarkTransactionReimbursable();
 * markReimbursable.mutate('tx-123');
 * ```
 */
export function useMarkTransactionReimbursable() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (transactionId: string) => {
      return apiFetch<PredictionActionResponse>(
        `/transactions/${transactionId}/reimbursable`,
        { method: 'POST' }
      )
    },

    onSuccess: () => {
      // Invalidate both transaction and prediction caches
      queryClient.invalidateQueries({ queryKey: transactionKeys.lists() })
      queryClient.invalidateQueries({ queryKey: predictionKeys.all })
    },
  })
}

/**
 * Hook for marking a transaction as not reimbursable (personal expense).
 *
 * Creates or updates a manual override prediction with Rejected status.
 * Use this for personal purchases that shouldn't be included in expense reports.
 *
 * @example
 * ```tsx
 * const markNotReimbursable = useMarkTransactionNotReimbursable();
 * markNotReimbursable.mutate('tx-123');
 * ```
 */
export function useMarkTransactionNotReimbursable() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (transactionId: string) => {
      return apiFetch<PredictionActionResponse>(
        `/transactions/${transactionId}/not-reimbursable`,
        { method: 'POST' }
      )
    },

    onSuccess: () => {
      // Invalidate both transaction and prediction caches
      queryClient.invalidateQueries({ queryKey: transactionKeys.lists() })
      queryClient.invalidateQueries({ queryKey: predictionKeys.all })
    },
  })
}

/**
 * Hook for clearing a manual reimbursability override.
 *
 * Removes the manual prediction, allowing the system to auto-predict
 * based on learned patterns on the next prediction cycle.
 *
 * @example
 * ```tsx
 * const clearOverride = useClearReimbursabilityOverride();
 * clearOverride.mutate('tx-123');
 * ```
 */
export function useClearReimbursabilityOverride() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (transactionId: string) => {
      return apiFetch<PredictionActionResponse>(
        `/transactions/${transactionId}/reimbursability-override`,
        { method: 'DELETE' }
      )
    },

    onSuccess: () => {
      // Invalidate both transaction and prediction caches
      queryClient.invalidateQueries({ queryKey: transactionKeys.lists() })
      queryClient.invalidateQueries({ queryKey: predictionKeys.all })
    },
  })
}

/**
 * Hook for bulk marking multiple transactions as reimbursable or not reimbursable.
 *
 * Creates or updates manual override predictions for all specified transactions.
 * Useful for quickly categorizing multiple transactions from the transaction list.
 *
 * @example
 * ```tsx
 * const bulkMark = useBulkMarkReimbursability();
 *
 * // Mark selected as not reimbursable (personal)
 * bulkMark.mutate({
 *   transactionIds: ['tx-1', 'tx-2', 'tx-3'],
 *   isReimbursable: false
 * });
 *
 * // Mark selected as reimbursable (business)
 * bulkMark.mutate({
 *   transactionIds: selectedIds,
 *   isReimbursable: true
 * });
 * ```
 */
export function useBulkMarkReimbursability() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: BulkTransactionReimbursabilityRequest) => {
      return apiFetch<BulkTransactionReimbursabilityResponse>(
        '/transactions/bulk/reimbursability',
        {
          method: 'POST',
          body: JSON.stringify(request),
        }
      )
    },

    onSuccess: (_data, variables) => {
      // Invalidate both transaction and prediction caches
      queryClient.invalidateQueries({ queryKey: transactionKeys.lists() })
      queryClient.invalidateQueries({ queryKey: predictionKeys.all })

      // Optionally invalidate specific transaction details
      variables.transactionIds.forEach((id) => {
        queryClient.invalidateQueries({ queryKey: transactionKeys.detail(id) })
      })
    },
  })
}
