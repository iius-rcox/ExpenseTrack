import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { apiFetch, apiUpload } from '@/services/api'
import { ApiError } from '@/types/api'
import type {
  ReceiptSummary,
  ReceiptDetail,
  ReceiptListResponse,
  ReceiptStatusCounts,
  UploadResponse
} from '@/types/api'
import type { ExtractedFieldKey, ReceiptUpdateRequest } from '@/types/receipt'
import { dashboardKeys } from './use-dashboard'

export const receiptKeys = {
  all: ['receipts'] as const,
  lists: () => [...receiptKeys.all, 'list'] as const,
  list: (filters: Record<string, unknown>) => [...receiptKeys.lists(), filters] as const,
  details: () => [...receiptKeys.all, 'detail'] as const,
  detail: (id: string) => [...receiptKeys.details(), id] as const,
  statusCounts: () => [...receiptKeys.all, 'status-counts'] as const,
}

interface ReceiptListParams {
  status?: string
  page?: number
  pageSize?: number
}

export function useReceiptList(params: ReceiptListParams = {}) {
  const { status, page = 1, pageSize = 20 } = params

  return useQuery({
    queryKey: receiptKeys.list({ status, page, pageSize }),
    queryFn: async () => {
      const searchParams = new URLSearchParams()
      if (status) searchParams.set('status', status)
      searchParams.set('pageNumber', String(page))
      searchParams.set('pageSize', String(pageSize))

      return apiFetch<ReceiptListResponse>(`/receipts?${searchParams}`)
    },
  })
}

export function useReceiptDetail(id: string) {
  return useQuery({
    queryKey: receiptKeys.detail(id),
    queryFn: () => apiFetch<ReceiptDetail>(`/receipts/${id}`),
    enabled: !!id,
  })
}

export function useReceiptStatusCounts() {
  return useQuery({
    queryKey: receiptKeys.statusCounts(),
    queryFn: () => apiFetch<ReceiptStatusCounts>('/receipts/counts'),
    staleTime: 30_000,
  })
}

export function useUploadReceipts() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      files,
      onProgress
    }: {
      files: File[]
      onProgress?: (progress: number) => void
    }) => {
      const formData = new FormData()
      files.forEach(file => {
        formData.append('files', file)
      })

      return apiUpload<UploadResponse>('/receipts', formData, onProgress)
    },
    onSuccess: () => {
      // Invalidate receipt lists and status counts
      queryClient.invalidateQueries({ queryKey: receiptKeys.lists() })
      queryClient.invalidateQueries({ queryKey: receiptKeys.statusCounts() })
    },
  })
}

export function useDeleteReceipt() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      await apiFetch(`/receipts/${id}`, { method: 'DELETE' })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: receiptKeys.lists() })
      queryClient.invalidateQueries({ queryKey: receiptKeys.statusCounts() })
    },
  })
}

export function useRetryReceipt() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      return apiFetch<ReceiptSummary>(`/receipts/${id}/retry`, { method: 'POST' })
    },
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: receiptKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: receiptKeys.lists() })
      queryClient.invalidateQueries({ queryKey: receiptKeys.statusCounts() })
    },
  })
}

export function useProcessReceipt() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      return apiFetch<ReceiptSummary>(`/receipts/${id}/process`, { method: 'POST' })
    },
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: receiptKeys.detail(id) })
      queryClient.invalidateQueries({ queryKey: receiptKeys.lists() })
      queryClient.invalidateQueries({ queryKey: receiptKeys.statusCounts() })
    },
  })
}

/**
 * Field update response from the API.
 */
interface FieldUpdateResponse {
  success: boolean
  field: {
    key: string
    value: string | number | null
    confidence: number
    isEdited: boolean
    originalValue?: string | number | null
  }
}

/**
 * Mutation params for updating a receipt field.
 */
interface UpdateFieldParams {
  receiptId: string
  field: ExtractedFieldKey
  value: string | number | null
}

/**
 * Hook for updating receipt fields with optimistic updates.
 * Provides instant UI feedback while the API request is in flight.
 * Includes rollback on error for data consistency.
 */
export function useUpdateReceiptField() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ receiptId, field, value }: UpdateFieldParams) => {
      return apiFetch<FieldUpdateResponse>(`/receipts/${receiptId}/fields`, {
        method: 'PATCH',
        body: JSON.stringify({ field, value }),
      })
    },

    // Optimistic update: immediately show the change in the UI
    onMutate: async ({ receiptId, field, value }) => {
      // Cancel any outgoing refetches to prevent overwriting optimistic update
      await queryClient.cancelQueries({ queryKey: receiptKeys.detail(receiptId) })

      // Snapshot the previous value
      const previousDetail = queryClient.getQueryData<ReceiptDetail>(
        receiptKeys.detail(receiptId)
      )

      // Optimistically update the cache
      if (previousDetail) {
        const updatedDetail = { ...previousDetail }

        // Update the specific field based on the field key
        switch (field) {
          case 'merchant':
            updatedDetail.vendor = value as string | null
            break
          case 'amount':
            updatedDetail.amount = value as number | null
            break
          case 'date':
            updatedDetail.date = value as string | null
            break
          case 'taxAmount':
            updatedDetail.tax = value as number | null
            break
          // Add more field mappings as needed
        }

        // Update confidence scores to reflect edit
        if (!updatedDetail.confidenceScores) {
          updatedDetail.confidenceScores = {}
        }
        // Mark as edited (confidence doesn't change for edited fields)

        queryClient.setQueryData(receiptKeys.detail(receiptId), updatedDetail)
      }

      // Return context with previous value for rollback
      return { previousDetail }
    },

    // Rollback on error
    onError: (_error, { receiptId }, context) => {
      if (context?.previousDetail) {
        queryClient.setQueryData(
          receiptKeys.detail(receiptId),
          context.previousDetail
        )
      }
    },

    // Always refetch after success or error to ensure data consistency
    onSettled: (_, _error, { receiptId }) => {
      queryClient.invalidateQueries({ queryKey: receiptKeys.detail(receiptId) })
      // Also invalidate list in case summary fields changed
      queryClient.invalidateQueries({ queryKey: receiptKeys.lists() })
    },
  })
}

/**
 * Hook for batch updating multiple receipt fields.
 * Useful for bulk corrections or category assignments.
 */
interface BatchUpdateParams {
  receiptId: string
  fields: Array<{ field: ExtractedFieldKey; value: string | number | null }>
}

export function useBatchUpdateReceiptFields() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ receiptId, fields }: BatchUpdateParams) => {
      return apiFetch<{ success: boolean; updatedCount: number }>(
        `/receipts/${receiptId}/fields/batch`,
        {
          method: 'PATCH',
          body: JSON.stringify({ fields }),
        }
      )
    },

    onSuccess: (_, { receiptId }) => {
      queryClient.invalidateQueries({ queryKey: receiptKeys.detail(receiptId) })
      queryClient.invalidateQueries({ queryKey: receiptKeys.lists() })
    },
  })
}

/**
 * Hook for uploading receipts with enhanced tracking.
 * Extends base upload with dashboard invalidation.
 */
export function useUploadReceiptsWithTracking() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      files,
      onProgress,
    }: {
      files: File[]
      onProgress?: (progress: number, fileIndex: number) => void
    }) => {
      const formData = new FormData()
      files.forEach((file) => {
        formData.append('files', file)
      })

      return apiUpload<UploadResponse>('/receipts', formData, (progress) => {
        onProgress?.(progress, 0)
      })
    },

    onSuccess: () => {
      // Invalidate receipt queries
      queryClient.invalidateQueries({ queryKey: receiptKeys.lists() })
      queryClient.invalidateQueries({ queryKey: receiptKeys.statusCounts() })
      // Also invalidate dashboard to update pending counts
      queryClient.invalidateQueries({ queryKey: dashboardKeys.all })
    },
  })
}

/**
 * Hook for polling receipt processing status.
 * Useful for tracking processing progress after upload.
 */
export function useReceiptProcessingStatus(
  receiptId: string,
  options: { enabled?: boolean; refetchInterval?: number } = {}
) {
  const { enabled = true, refetchInterval = 2000 } = options

  return useQuery({
    queryKey: [...receiptKeys.detail(receiptId), 'status'],
    queryFn: () => apiFetch<{ status: string; progress?: number }>(`/receipts/${receiptId}/status`),
    enabled: enabled && !!receiptId,
    refetchInterval: (query) => {
      // Stop polling when complete or error
      const status = query.state.data?.status
      if (status === 'Ready' || status === 'Error' || status === 'Matched') {
        return false
      }
      return refetchInterval
    },
  })
}

/**
 * Update receipt params combining ID with request body.
 * Feature 024: Extraction Editor Training
 */
interface UpdateReceiptParams {
  receiptId: string
  request: ReceiptUpdateRequest
}

/**
 * Hook for updating receipt data with optimistic concurrency and training feedback.
 * Handles 409 Conflict errors with user-friendly toast notifications.
 * Feature 024: Extraction Editor Training
 *
 * @example
 * const { mutate: updateReceipt, isPending } = useUpdateReceipt()
 *
 * updateReceipt({
 *   receiptId: '123',
 *   request: {
 *     vendor: 'Acme Corp',
 *     amount: 42.50,
 *     rowVersion: receipt.rowVersion,
 *     corrections: [{ fieldName: 'vendor', originalValue: 'ACM CORP' }]
 *   }
 * })
 */
export function useUpdateReceipt() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ receiptId, request }: UpdateReceiptParams) => {
      return apiFetch<ReceiptDetail>(`/receipts/${receiptId}`, {
        method: 'PUT',
        body: JSON.stringify(request),
      })
    },

    // Optimistic update: immediately show the change in the UI
    onMutate: async ({ receiptId, request }) => {
      // Cancel any outgoing refetches to prevent overwriting optimistic update
      await queryClient.cancelQueries({ queryKey: receiptKeys.detail(receiptId) })

      // Snapshot the previous value for rollback
      const previousDetail = queryClient.getQueryData<ReceiptDetail>(
        receiptKeys.detail(receiptId)
      )

      // Optimistically update the cache with primary receipt fields
      // Note: lineItems are not updated optimistically as they require type transformation
      if (previousDetail) {
        const updatedDetail: ReceiptDetail = {
          ...previousDetail,
          vendor: request.vendor ?? previousDetail.vendor,
          amount: request.amount ?? previousDetail.amount,
          date: request.date ?? previousDetail.date,
          tax: request.tax ?? previousDetail.tax,
          currency: request.currency ?? previousDetail.currency,
        }

        queryClient.setQueryData(receiptKeys.detail(receiptId), updatedDetail)
      }

      return { previousDetail }
    },

    // Handle errors, especially 409 Conflict
    onError: (error, { receiptId }, context) => {
      // Rollback on error
      if (context?.previousDetail) {
        queryClient.setQueryData(
          receiptKeys.detail(receiptId),
          context.previousDetail
        )
      }

      // Show appropriate toast based on error type
      if (error instanceof ApiError && error.isConflict) {
        toast.error('Concurrency conflict', {
          description: 'This receipt was modified by another user. Please refresh and try again.',
          action: {
            label: 'Refresh',
            onClick: () => {
              queryClient.invalidateQueries({ queryKey: receiptKeys.detail(receiptId) })
            }
          }
        })
      } else {
        toast.error('Failed to update receipt', {
          description: error instanceof Error ? error.message : 'An unknown error occurred'
        })
      }
    },

    // On success, show toast and update the cache with server response
    onSuccess: (updatedReceipt, { receiptId }) => {
      // Replace optimistic data with actual server response
      queryClient.setQueryData(receiptKeys.detail(receiptId), updatedReceipt)

      toast.success('Receipt updated', {
        description: 'Your changes have been saved.'
      })
    },

    // Always refetch list after mutation to ensure consistency
    onSettled: () => {
      // Invalidate list in case summary fields changed
      queryClient.invalidateQueries({ queryKey: receiptKeys.lists() })
    },
  })
}
