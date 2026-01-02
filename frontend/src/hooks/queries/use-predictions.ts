import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { apiFetch } from '@/services/api'
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
  UpdatePatternSuppressionRequest,
  PredictionStatus,
  PredictionConfidence,
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
 */
export function usePatterns(params: PatternListParams = {}) {
  const { page = 1, pageSize = 20, includeSuppressed = false } = params

  return useQuery({
    queryKey: predictionKeys.patternList({ page, pageSize, includeSuppressed }),
    queryFn: async () => {
      const searchParams = new URLSearchParams()
      searchParams.set('page', String(page))
      searchParams.set('pageSize', String(pageSize))
      searchParams.set('includeSuppressed', String(includeSuppressed))

      return apiFetch<PatternListResponse>(`/predictions/patterns?${searchParams}`)
    },
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
 * Hook to update pattern suppression.
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
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: predictionKeys.patterns() })
    },
  })
}

/**
 * Hook to delete a pattern.
 */
export function useDeletePattern() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      return apiFetch(`/predictions/patterns/${id}`, {
        method: 'DELETE',
      })
    },
    onSuccess: () => {
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
