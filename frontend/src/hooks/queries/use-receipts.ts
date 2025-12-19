import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiFetch, apiUpload } from '@/services/api'
import type {
  ReceiptSummary,
  ReceiptDetail,
  ReceiptListResponse,
  ReceiptStatusCounts,
  UploadResponse
} from '@/types/api'

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
