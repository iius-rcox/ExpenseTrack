import { useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import { apiFetch } from '@/services/api'
import { DEFAULT_PATTERN_SORT } from '@/types/prediction'
import type {
  PredictionSummary,
  PredictionDetail,
  PredictionListResponse,
  PredictionDashboard,
  PredictionAccuracyStats,
  PatternDetail,
  PatternListResponse,
  PredictionActionResponse,
  BulkPredictionActionResponse,
  ConfirmPredictionRequest,
  RejectPredictionRequest,
  BulkPredictionActionRequest,
  BulkPatternActionRequest,
  UpdatePatternSuppressionRequest,
  UpdatePatternReceiptMatchRequest,
  PredictionStatus,
  PredictionConfidence,
  PatternStatusFilter,
  PatternSortConfig,
} from '@/types/prediction'

// Query key factory for predictions
export const predictionKeys = {
  all: ['predictions'] as const,
  dashboard: () => [...predictionKeys.all, 'dashboard'] as const,
  stats: () => [...predictionKeys.all, 'stats'] as const,
  lists: () => [...predictionKeys.all, 'list'] as const,
  list: (filters: Record<string, unknown>) => [...predictionKeys.lists(), filters] as const,
  detail: (id: string) => [...predictionKeys.all, 'detail', id] as const,
  transaction: (transactionId: string) => [...predictionKeys.all, 'transaction', transactionId] as const,
  patterns: () => [...predictionKeys.all, 'patterns'] as const,
  patternList: (filters: Record<string, unknown>) => [...predictionKeys.patterns(), 'list', filters] as const,
  patternDetail: (id: string) => [...predictionKeys.patterns(), 'detail', id] as const,
}

interface PredictionListParams {
  page?: number
  pageSize?: number
  status?: PredictionStatus
  minConfidence?: PredictionConfidence
}

interface PatternListParams {
  page?: number
  pageSize?: number
  includeSuppressed?: boolean
  status?: PatternStatusFilter
  category?: string
  search?: string
  sortBy?: PatternSortConfig['field']
  sortOrder?: PatternSortConfig['direction']
}

/**
 * Hook to fetch the prediction dashboard summary.
 */
export function usePredictionDashboard() {
  return useQuery({
    queryKey: predictionKeys.dashboard(),
    queryFn: async () => {
      return apiFetch<PredictionDashboard>('/predictions/dashboard')
    },
    staleTime: 30_000, // 30 seconds
  })
}

/**
 * Hook to fetch prediction accuracy statistics.
 */
export function usePredictionStats() {
  return useQuery({
    queryKey: predictionKeys.stats(),
    queryFn: async () => {
      return apiFetch<PredictionAccuracyStats>('/predictions/stats')
    },
    staleTime: 60_000, // 1 minute
  })
}

/**
 * Hook to fetch paginated list of predictions.
 */
export function usePredictions(params: PredictionListParams = {}) {
  const { page = 1, pageSize = 20, status, minConfidence } = params

  return useQuery({
    queryKey: predictionKeys.list({ page, pageSize, status, minConfidence }),
    queryFn: async () => {
      const searchParams = new URLSearchParams()
      searchParams.set('page', String(page))
      searchParams.set('pageSize', String(pageSize))
      if (status) searchParams.set('status', status)
      if (minConfidence) searchParams.set('minConfidence', minConfidence)

      return apiFetch<PredictionListResponse>(`/predictions?${searchParams}`)
    },
  })
}

/**
 * Hook to fetch a single prediction by ID.
 */
export function usePrediction(id: string) {
  return useQuery({
    queryKey: predictionKeys.detail(id),
    queryFn: async () => {
      return apiFetch<PredictionDetail>(`/predictions/${id}`)
    },
    enabled: !!id,
  })
}

/**
 * Hook to fetch prediction for a specific transaction.
 * Returns null if no prediction exists.
 */
export function usePredictionForTransaction(transactionId: string) {
  return useQuery({
    queryKey: predictionKeys.transaction(transactionId),
    queryFn: async () => {
      const response = await fetch(`/api/predictions/transaction/${transactionId}`)
      if (response.status === 204) {
        return null
      }
      if (!response.ok) {
        throw new Error('Failed to fetch prediction')
      }
      return response.json() as Promise<PredictionSummary>
    },
    enabled: !!transactionId,
  })
}

/**
 * Hook to confirm a prediction with optimistic updates.
 * Shows success toast and invalidates prediction queries.
 */
export function useConfirmPrediction() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: ConfirmPredictionRequest) => {
      return apiFetch<PredictionActionResponse>('/predictions/confirm', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
      })
    },
    onMutate: async (request) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: predictionKeys.all })

      // Snapshot previous values for rollback
      const previousLists = queryClient.getQueriesData<PredictionListResponse>({
        queryKey: predictionKeys.lists(),
      })

      // Optimistically update prediction status in all list queries
      queryClient.setQueriesData<PredictionListResponse>(
        { queryKey: predictionKeys.lists() },
        (old) => {
          if (!old) return old
          return {
            ...old,
            predictions: old.predictions.map((p) =>
              p.id === request.predictionId
                ? { ...p, status: 'Confirmed' as PredictionStatus }
                : p
            ),
            pendingCount: Math.max(0, old.pendingCount - 1),
          }
        }
      )

      return { previousLists }
    },
    onError: (_error, _request, context) => {
      // Rollback on error
      if (context?.previousLists) {
        context.previousLists.forEach(([queryKey, data]) => {
          queryClient.setQueryData(queryKey, data)
        })
      }
      toast.error('Failed to confirm prediction')
    },
    onSuccess: (response) => {
      toast.success(response.message || 'Prediction confirmed')
      queryClient.invalidateQueries({ queryKey: predictionKeys.all })
    },
  })
}

/**
 * Hook to reject a prediction with optimistic updates.
 * Shows success toast and notifies user if pattern was auto-suppressed.
 */
export function useRejectPrediction() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: RejectPredictionRequest) => {
      return apiFetch<PredictionActionResponse>('/predictions/reject', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
      })
    },
    onMutate: async (request) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: predictionKeys.all })

      // Snapshot previous values for rollback
      const previousLists = queryClient.getQueriesData<PredictionListResponse>({
        queryKey: predictionKeys.lists(),
      })

      // Optimistically update prediction status in all list queries
      queryClient.setQueriesData<PredictionListResponse>(
        { queryKey: predictionKeys.lists() },
        (old) => {
          if (!old) return old
          return {
            ...old,
            predictions: old.predictions.map((p) =>
              p.id === request.predictionId
                ? { ...p, status: 'Rejected' as PredictionStatus }
                : p
            ),
            pendingCount: Math.max(0, old.pendingCount - 1),
          }
        }
      )

      return { previousLists }
    },
    onError: (_error, _request, context) => {
      // Rollback on error
      if (context?.previousLists) {
        context.previousLists.forEach(([queryKey, data]) => {
          queryClient.setQueryData(queryKey, data)
        })
      }
      toast.error('Failed to reject prediction')
    },
    onSuccess: (response) => {
      // Show special notification if pattern was auto-suppressed
      if (response.patternSuppressed) {
        toast.warning(response.message, {
          description: 'You can re-enable this pattern from the Pattern Dashboard.',
          duration: 5000,
        })
        // Also invalidate patterns to reflect suppression
        queryClient.invalidateQueries({ queryKey: predictionKeys.patterns() })
      } else {
        toast.success(response.message || 'Prediction rejected')
      }
      queryClient.invalidateQueries({ queryKey: predictionKeys.all })
    },
  })
}

/**
 * Hook for bulk prediction actions.
 */
export function useBulkPredictionAction() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: BulkPredictionActionRequest) => {
      return apiFetch<BulkPredictionActionResponse>('/predictions/bulk', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: predictionKeys.all })
    },
  })
}

/**
 * Hook to fetch paginated list of patterns.
 *
 * @example
 * ```tsx
 * const { data, isLoading } = usePatterns({
 *   status: 'active',
 *   search: 'uber',
 *   sortBy: 'accuracyRate',
 *   sortOrder: 'desc',
 * });
 * ```
 */
export function usePatterns(params: PatternListParams = {}) {
  const {
    page = 1,
    pageSize = 20,
    includeSuppressed = false,
    status,
    category,
    search,
    sortBy = DEFAULT_PATTERN_SORT.field,
    sortOrder = DEFAULT_PATTERN_SORT.direction,
  } = params

  const queryParams = { page, pageSize, includeSuppressed, status, category, search, sortBy, sortOrder }

  return useQuery({
    queryKey: predictionKeys.patternList(queryParams),
    queryFn: async () => {
      const searchParams = new URLSearchParams()
      searchParams.set('page', String(page))
      searchParams.set('pageSize', String(pageSize))
      searchParams.set('sortBy', sortBy)
      searchParams.set('sortOrder', sortOrder)

      // Status filter determines includeSuppressed behavior
      if (status === 'all') {
        searchParams.set('includeSuppressed', 'true')
      } else if (status === 'suppressed') {
        searchParams.set('includeSuppressed', 'true')
        searchParams.set('suppressedOnly', 'true')
      } else {
        searchParams.set('includeSuppressed', String(includeSuppressed))
      }

      if (category) searchParams.set('category', category)
      if (search) searchParams.set('search', search)

      return apiFetch<PatternListResponse>(`/predictions/patterns?${searchParams}`)
    },
    placeholderData: keepPreviousData,
    staleTime: 30_000,
  })
}

/**
 * Hook to fetch a single pattern by ID.
 */
export function usePattern(id: string) {
  return useQuery({
    queryKey: predictionKeys.patternDetail(id),
    queryFn: async () => {
      return apiFetch<PatternDetail>(`/predictions/patterns/${id}`)
    },
    enabled: !!id,
  })
}

/**
 * Hook to update pattern suppression with optimistic updates.
 * Shows toast notification on success/error.
 */
export function useUpdatePatternSuppression() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ id, isSuppressed }: { id: string; isSuppressed: boolean }) => {
      return apiFetch(`/predictions/patterns/${id}/suppression`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ patternId: id, isSuppressed } as UpdatePatternSuppressionRequest),
      })
    },
    onMutate: async ({ id, isSuppressed }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: predictionKeys.patterns() })

      // Snapshot previous values for rollback
      const previousLists = queryClient.getQueriesData<PatternListResponse>({
        queryKey: predictionKeys.patterns(),
      })

      // Optimistically update pattern in all list queries
      queryClient.setQueriesData<PatternListResponse>(
        { queryKey: predictionKeys.patterns() },
        (old) => {
          if (!old) return old
          return {
            ...old,
            patterns: old.patterns.map((p) =>
              p.id === id ? { ...p, isSuppressed } : p
            ),
            activeCount: isSuppressed
              ? Math.max(0, old.activeCount - 1)
              : old.activeCount + 1,
            suppressedCount: isSuppressed
              ? old.suppressedCount + 1
              : Math.max(0, old.suppressedCount - 1),
          }
        }
      )

      return { previousLists }
    },
    onError: (_error, { isSuppressed }, context) => {
      // Rollback on error
      if (context?.previousLists) {
        context.previousLists.forEach(([queryKey, data]) => {
          queryClient.setQueryData(queryKey, data)
        })
      }
      toast.error(`Failed to ${isSuppressed ? 'suppress' : 'enable'} pattern`)
    },
    onSuccess: (_, { isSuppressed }) => {
      toast.success(isSuppressed ? 'Pattern suppressed' : 'Pattern enabled')
      queryClient.invalidateQueries({ queryKey: predictionKeys.patterns() })
    },
  })
}

/**
 * Hook to update pattern receipt match requirement with optimistic updates.
 * When enabled, predictions are only generated for transactions with confirmed receipt matches.
 * Shows toast notification on success/error.
 */
export function useUpdatePatternReceiptMatch() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      id,
      requiresReceiptMatch,
    }: {
      id: string
      requiresReceiptMatch: boolean
    }) => {
      return apiFetch(`/predictions/patterns/${id}/receipt-match`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          patternId: id,
          requiresReceiptMatch,
        } as UpdatePatternReceiptMatchRequest),
      })
    },
    onMutate: async ({ id, requiresReceiptMatch }) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: predictionKeys.patterns() })

      // Snapshot previous values for rollback
      const previousLists = queryClient.getQueriesData<PatternListResponse>({
        queryKey: predictionKeys.patterns(),
      })

      // Optimistically update pattern in all list queries
      queryClient.setQueriesData<PatternListResponse>(
        { queryKey: predictionKeys.patterns() },
        (old) => {
          if (!old) return old
          return {
            ...old,
            patterns: old.patterns.map((p) =>
              p.id === id ? { ...p, requiresReceiptMatch } : p
            ),
          }
        }
      )

      return { previousLists }
    },
    onError: (_error, { requiresReceiptMatch }, context) => {
      // Rollback on error
      if (context?.previousLists) {
        context.previousLists.forEach(([queryKey, data]) => {
          queryClient.setQueryData(queryKey, data)
        })
      }
      toast.error(
        `Failed to ${requiresReceiptMatch ? 'enable' : 'disable'} receipt match requirement`
      )
    },
    onSuccess: (_, { requiresReceiptMatch }) => {
      toast.success(
        requiresReceiptMatch
          ? 'Receipt match now required for predictions'
          : 'Receipt match no longer required'
      )
      queryClient.invalidateQueries({ queryKey: predictionKeys.patterns() })
    },
  })
}

/**
 * Hook to delete a pattern.
 * Shows toast notification on success/error.
 */
export function useDeletePattern() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      return apiFetch(`/predictions/patterns/${id}`, {
        method: 'DELETE',
      })
    },
    onMutate: async (id) => {
      await queryClient.cancelQueries({ queryKey: predictionKeys.patterns() })

      const previousLists = queryClient.getQueriesData<PatternListResponse>({
        queryKey: predictionKeys.patterns(),
      })

      // Optimistically remove pattern from lists
      queryClient.setQueriesData<PatternListResponse>(
        { queryKey: predictionKeys.patterns() },
        (old) => {
          if (!old) return old
          const pattern = old.patterns.find((p) => p.id === id)
          return {
            ...old,
            patterns: old.patterns.filter((p) => p.id !== id),
            totalCount: Math.max(0, old.totalCount - 1),
            activeCount: pattern && !pattern.isSuppressed
              ? Math.max(0, old.activeCount - 1)
              : old.activeCount,
            suppressedCount: pattern?.isSuppressed
              ? Math.max(0, old.suppressedCount - 1)
              : old.suppressedCount,
          }
        }
      )

      return { previousLists }
    },
    onError: (_error, _id, context) => {
      if (context?.previousLists) {
        context.previousLists.forEach(([queryKey, data]) => {
          queryClient.setQueryData(queryKey, data)
        })
      }
      toast.error('Failed to delete pattern')
    },
    onSuccess: () => {
      toast.success('Pattern deleted')
      queryClient.invalidateQueries({ queryKey: predictionKeys.patterns() })
    },
  })
}

/**
 * Response for bulk pattern action.
 */
interface BulkPatternActionResponse {
  successCount: number
  failedCount: number
  failedIds: string[]
  message: string
}

/**
 * Hook for bulk pattern actions (suppress, enable, delete).
 * Provides optimistic updates and rollback on error.
 */
export function useBulkPatternAction() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: BulkPatternActionRequest) => {
      return apiFetch<BulkPatternActionResponse>('/predictions/patterns/bulk', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
      })
    },
    onMutate: async ({ patternIds, action }) => {
      await queryClient.cancelQueries({ queryKey: predictionKeys.patterns() })

      const previousLists = queryClient.getQueriesData<PatternListResponse>({
        queryKey: predictionKeys.patterns(),
      })

      const idSet = new Set(patternIds)

      // Optimistically update patterns based on action
      queryClient.setQueriesData<PatternListResponse>(
        { queryKey: predictionKeys.patterns() },
        (old) => {
          if (!old) return old

          if (action === 'delete') {
            const deletedPatterns = old.patterns.filter((p) => idSet.has(p.id))
            const activeDeleted = deletedPatterns.filter((p) => !p.isSuppressed).length
            const suppressedDeleted = deletedPatterns.filter((p) => p.isSuppressed).length

            return {
              ...old,
              patterns: old.patterns.filter((p) => !idSet.has(p.id)),
              totalCount: Math.max(0, old.totalCount - patternIds.length),
              activeCount: Math.max(0, old.activeCount - activeDeleted),
              suppressedCount: Math.max(0, old.suppressedCount - suppressedDeleted),
            }
          }

          const isSuppressed = action === 'suppress'
          let activeChange = 0
          let suppressedChange = 0

          const updatedPatterns = old.patterns.map((p) => {
            if (!idSet.has(p.id)) return p
            // Only count changes for patterns that are actually changing
            if (p.isSuppressed !== isSuppressed) {
              if (isSuppressed) {
                activeChange--
                suppressedChange++
              } else {
                activeChange++
                suppressedChange--
              }
            }
            return { ...p, isSuppressed }
          })

          return {
            ...old,
            patterns: updatedPatterns,
            activeCount: Math.max(0, old.activeCount + activeChange),
            suppressedCount: Math.max(0, old.suppressedCount + suppressedChange),
          }
        }
      )

      return { previousLists }
    },
    onError: (_error, { action }, context) => {
      if (context?.previousLists) {
        context.previousLists.forEach(([queryKey, data]) => {
          queryClient.setQueryData(queryKey, data)
        })
      }
      const actionText = action === 'delete' ? 'delete' : action === 'suppress' ? 'suppress' : 'enable'
      toast.error(`Failed to ${actionText} patterns`)
    },
    onSuccess: (response, { action }) => {
      const actionText = action === 'delete' ? 'deleted' : action === 'suppress' ? 'suppressed' : 'enabled'
      if (response.failedCount > 0) {
        toast.warning(
          `${response.successCount} patterns ${actionText}, ${response.failedCount} failed`
        )
      } else {
        toast.success(`${response.successCount} patterns ${actionText}`)
      }
      queryClient.invalidateQueries({ queryKey: predictionKeys.patterns() })
    },
  })
}

/**
 * Hook to trigger pattern learning from a report.
 */
export function useLearnFromReport() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (reportId: string) => {
      return apiFetch<number>(`/predictions/learn/${reportId}`, {
        method: 'POST',
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: predictionKeys.patterns() })
    },
  })
}

/**
 * Hook to rebuild all patterns.
 */
export function useRebuildPatterns() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async () => {
      return apiFetch<number>('/predictions/rebuild', {
        method: 'POST',
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: predictionKeys.all })
    },
  })
}

/**
 * Hook to generate predictions for unprocessed transactions.
 */
export function useGeneratePredictions() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async () => {
      return apiFetch<number>('/predictions/generate', {
        method: 'POST',
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: predictionKeys.all })
    },
  })
}

/**
 * Combined hook for the predictions workspace.
 * Provides all state and actions needed for the predictions UI.
 */
export function usePredictionWorkspace(params: PredictionListParams = {}) {
  const dashboardQuery = usePredictionDashboard()
  const predictionsQuery = usePredictions(params)
  const confirmPrediction = useConfirmPrediction()
  const rejectPrediction = useRejectPrediction()
  const bulkAction = useBulkPredictionAction()

  const isProcessing =
    confirmPrediction.isPending ||
    rejectPrediction.isPending ||
    bulkAction.isPending

  return {
    // Dashboard data
    dashboard: dashboardQuery.data,
    isDashboardLoading: dashboardQuery.isLoading,

    // Predictions data
    predictions: predictionsQuery.data?.predictions ?? [],
    totalCount: predictionsQuery.data?.totalCount ?? 0,
    page: predictionsQuery.data?.page ?? 1,
    pageSize: predictionsQuery.data?.pageSize ?? 20,
    pendingCount: predictionsQuery.data?.pendingCount ?? 0,
    highConfidenceCount: predictionsQuery.data?.highConfidenceCount ?? 0,
    isLoading: predictionsQuery.isLoading,
    isError: predictionsQuery.isError,
    error: predictionsQuery.error,

    // Actions
    confirm: (predictionId: string, glCodeOverride?: string, departmentOverride?: string) =>
      confirmPrediction.mutate({ predictionId, glCodeOverride, departmentOverride }),
    reject: (predictionId: string) => rejectPrediction.mutate({ predictionId }),
    bulkConfirm: (predictionIds: string[]) =>
      bulkAction.mutate({ predictionIds, action: 'Confirmed' }),
    bulkReject: (predictionIds: string[]) =>
      bulkAction.mutate({ predictionIds, action: 'Rejected' }),

    // Status
    isProcessing,
    isConfirming: confirmPrediction.isPending,
    isRejecting: rejectPrediction.isPending,

    // Refetch
    refetch: predictionsQuery.refetch,
    refetchDashboard: dashboardQuery.refetch,
  }
}

// =============================================================================
// Pattern Workspace Hook
// =============================================================================

/**
 * Parameters for the pattern workspace hook.
 */
export interface UsePatternWorkspaceParams extends PatternListParams {
  /** Enable/disable the query */
  enabled?: boolean
}

/**
 * Combined hook for the patterns workspace.
 * Provides all state and actions needed for the pattern management UI.
 *
 * @example
 * ```tsx
 * const {
 *   patterns,
 *   activeCount,
 *   suppressedCount,
 *   isLoading,
 *   toggleSuppression,
 *   deletePattern,
 *   bulkAction,
 * } = usePatternWorkspace({
 *   status: 'active',
 *   search: 'uber',
 *   sortBy: 'accuracyRate',
 * });
 * ```
 */
export function usePatternWorkspace(params: UsePatternWorkspaceParams = {}) {
  const { enabled = true, ...queryParams } = params

  const patternsQuery = usePatterns({ ...queryParams })
  const updateSuppression = useUpdatePatternSuppression()
  const updateReceiptMatch = useUpdatePatternReceiptMatch()
  const deletePatternMutation = useDeletePattern()
  const bulkPatternAction = useBulkPatternAction()
  const rebuildPatterns = useRebuildPatterns()

  const isProcessing =
    updateSuppression.isPending ||
    updateReceiptMatch.isPending ||
    deletePatternMutation.isPending ||
    bulkPatternAction.isPending ||
    rebuildPatterns.isPending

  return {
    // Pattern data
    patterns: patternsQuery.data?.patterns ?? [],
    totalCount: patternsQuery.data?.totalCount ?? 0,
    page: patternsQuery.data?.page ?? 1,
    pageSize: patternsQuery.data?.pageSize ?? 20,
    activeCount: patternsQuery.data?.activeCount ?? 0,
    suppressedCount: patternsQuery.data?.suppressedCount ?? 0,

    // Loading states
    isLoading: patternsQuery.isLoading,
    isError: patternsQuery.isError,
    error: patternsQuery.error,

    // Individual actions
    toggleSuppression: (id: string, isSuppressed: boolean) =>
      updateSuppression.mutate({ id, isSuppressed }),
    toggleReceiptMatch: (id: string, requiresReceiptMatch: boolean) =>
      updateReceiptMatch.mutate({ id, requiresReceiptMatch }),
    deletePattern: (id: string) => deletePatternMutation.mutate(id),
    rebuild: () => rebuildPatterns.mutateAsync(),

    // Bulk actions
    bulkSuppress: (patternIds: string[]) =>
      bulkPatternAction.mutate({ patternIds, action: 'suppress' }),
    bulkEnable: (patternIds: string[]) =>
      bulkPatternAction.mutate({ patternIds, action: 'enable' }),
    bulkDelete: (patternIds: string[]) =>
      bulkPatternAction.mutate({ patternIds, action: 'delete' }),

    // Processing states
    isProcessing,
    isTogglingSupppression: updateSuppression.isPending,
    isTogglingReceiptMatch: updateReceiptMatch.isPending,
    isDeleting: deletePatternMutation.isPending,
    isBulkProcessing: bulkPatternAction.isPending,
    isRebuilding: rebuildPatterns.isPending,

    // Refetch
    refetch: patternsQuery.refetch,
  }
}
