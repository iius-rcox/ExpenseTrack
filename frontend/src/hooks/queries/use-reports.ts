import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiFetch, apiDownload } from '@/services/api'
import type {
  ExpenseReport,
  ReportListResponse,
  GenerateDraftRequest,
  UpdateLineRequest,
  ExpenseLine,
  AddLineRequest,
  AvailableTransactionsResponse,
} from '@/types/api'

export const reportKeys = {
  all: ['reports'] as const,
  lists: () => [...reportKeys.all, 'list'] as const,
  list: (filters: Record<string, unknown>) => [...reportKeys.lists(), filters] as const,
  details: () => [...reportKeys.all, 'detail'] as const,
  detail: (id: string) => [...reportKeys.details(), id] as const,
  preview: (period: string) => [...reportKeys.all, 'preview', period] as const,
  draftExists: (period: string) => [...reportKeys.all, 'draft-exists', period] as const,
  availableTransactions: (reportId: string, search?: string, page?: number, pageSize?: number) =>
    [...reportKeys.all, 'available-transactions', reportId, search, page, pageSize] as const,
}

interface ReportListParams {
  page?: number
  pageSize?: number
  status?: string
}

export function useReportList(params: ReportListParams = {}) {
  const { page = 1, pageSize = 20, status } = params

  return useQuery({
    queryKey: reportKeys.list({ page, pageSize, status }),
    queryFn: async () => {
      const searchParams = new URLSearchParams()
      searchParams.set('page', String(page))
      searchParams.set('pageSize', String(pageSize))
      if (status) searchParams.set('status', status)

      return apiFetch<ReportListResponse>(`/reports?${searchParams}`)
    },
  })
}

export function useReportDetail(reportId: string) {
  return useQuery({
    queryKey: reportKeys.detail(reportId),
    queryFn: () => apiFetch<ExpenseReport>(`/reports/${reportId}`),
    enabled: !!reportId,
  })
}

export function useReportPreview(period: string) {
  return useQuery({
    queryKey: reportKeys.preview(period),
    queryFn: () => apiFetch<ExpenseLine[]>(`/reports/preview?period=${period}`),
    enabled: !!period,
    staleTime: 60_000,
  })
}

/**
 * Check if a draft report exists for the given period.
 * Returns the draft ID if found.
 */
export function useCheckDraftExists(period: string) {
  return useQuery({
    queryKey: reportKeys.draftExists(period),
    queryFn: async () => {
      const response = await apiFetch<{ exists: boolean; reportId?: string }>(
        `/reports/draft/exists?period=${period}`
      )
      return response
    },
    enabled: !!period,
    staleTime: 0, // Always fresh check
  })
}

/**
 * Get or create draft report for period.
 * If draft exists, loads it. If not, generates new draft.
 */
export function useGetOrCreateDraft(period: string) {
  const { data: draftCheck, isLoading: checkingDraft } = useCheckDraftExists(period)
  const generateReport = useGenerateReport()

  const { data: existingDraft, isLoading: loadingDraft } = useReportDetail(draftCheck?.reportId || '')

  return {
    draft: existingDraft,
    isLoading: checkingDraft || loadingDraft || generateReport.isPending,
    needsGeneration: draftCheck?.exists === false,
    generateDraft: () => generateReport.mutate({ period }),
  }
}

export function useGenerateReport() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: GenerateDraftRequest) => {
      // Use extended timeout (180s) for report generation which involves
      // AI categorization and description normalization for each line.
      // Note: OpenAI rate limiting (HTTP 429) can significantly slow processing.
      return apiFetch<ExpenseReport>('/reports/generate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
        timeout: 180000, // 180 seconds (3 minutes)
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: reportKeys.lists() })
    },
  })
}

export function useUpdateReportLine() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      reportId,
      lineId,
      data,
    }: {
      reportId: string
      lineId: string
      data: UpdateLineRequest
    }) => {
      return apiFetch<ExpenseLine>(`/reports/${reportId}/lines/${lineId}`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      })
    },
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: reportKeys.detail(variables.reportId) })
    },
  })
}

export function useSubmitReport() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (reportId: string) => {
      return apiFetch<ExpenseReport>(`/reports/${reportId}/submit`, {
        method: 'POST',
      })
    },
    onSuccess: (_data, reportId) => {
      queryClient.invalidateQueries({ queryKey: reportKeys.detail(reportId) })
      queryClient.invalidateQueries({ queryKey: reportKeys.lists() })
    },
  })
}

export function useDeleteReport() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (reportId: string) => {
      return apiFetch(`/reports/${reportId}`, {
        method: 'DELETE',
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: reportKeys.lists() })
    },
  })
}

export function useExportReport() {
  return useMutation({
    mutationFn: async ({
      reportId,
      format,
    }: {
      reportId: string
      format: 'pdf' | 'excel'
    }) => {
      const filename = `expense-report-${reportId}.${format === 'pdf' ? 'pdf' : 'xlsx'}`

      // apiDownload handles auth, blob download, and file save
      await apiDownload(`/reports/${reportId}/export?format=${format}`, filename)

      return { filename }
    },
  })
}

/**
 * Get available transactions that can be added to a report.
 * Excludes transactions already on any active report.
 */
export function useAvailableTransactions(
  reportId: string,
  search?: string,
  page = 1,
  pageSize = 20,
  enabled = true
) {
  return useQuery({
    queryKey: reportKeys.availableTransactions(reportId, search, page, pageSize),
    queryFn: async () => {
      const params = new URLSearchParams()
      if (search) params.set('search', search)
      params.set('page', String(page))
      params.set('pageSize', String(pageSize))

      return apiFetch<AvailableTransactionsResponse>(
        `/reports/${reportId}/available-transactions?${params}`
      )
    },
    enabled: enabled && !!reportId,
    staleTime: 30_000, // Cache for 30 seconds
  })
}

/**
 * Add a transaction as a new expense line to a report.
 */
export function useAddReportLine() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      reportId,
      data,
    }: {
      reportId: string
      data: AddLineRequest
    }) => {
      return apiFetch<ExpenseLine>(`/reports/${reportId}/lines`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      })
    },
    onSuccess: (_data, variables) => {
      // Invalidate report detail and available transactions
      queryClient.invalidateQueries({ queryKey: reportKeys.detail(variables.reportId) })
      queryClient.invalidateQueries({
        queryKey: reportKeys.availableTransactions(variables.reportId),
      })
    },
  })
}

/**
 * Remove an expense line from a report.
 * Transaction becomes available for other reports again.
 */
export function useRemoveReportLine() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      reportId,
      lineId,
    }: {
      reportId: string
      lineId: string
    }) => {
      console.log('[useRemoveReportLine] Making DELETE request')
      console.log('[useRemoveReportLine] URL:', `/reports/${reportId}/lines/${lineId}`)
      try {
        const result = await apiFetch(`/reports/${reportId}/lines/${lineId}`, {
          method: 'DELETE',
        })
        console.log('[useRemoveReportLine] Success, result:', result)
        return result
      } catch (error) {
        console.error('[useRemoveReportLine] Error:', error)
        throw error
      }
    },
    onSuccess: (_data, variables) => {
      // Invalidate report detail and available transactions
      queryClient.invalidateQueries({ queryKey: reportKeys.detail(variables.reportId) })
      queryClient.invalidateQueries({
        queryKey: reportKeys.availableTransactions(variables.reportId),
      })
    },
  })
}
