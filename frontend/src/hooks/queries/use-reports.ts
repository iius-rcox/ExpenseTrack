import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/services/api'
import type {
  ExpenseReport,
  ReportListResponse,
  GenerateDraftRequest,
  UpdateLineRequest,
  ExpenseLine,
} from '@/types/api'

export const reportKeys = {
  all: ['reports'] as const,
  lists: () => [...reportKeys.all, 'list'] as const,
  list: (filters: Record<string, unknown>) => [...reportKeys.lists(), filters] as const,
  details: () => [...reportKeys.all, 'detail'] as const,
  detail: (id: string) => [...reportKeys.details(), id] as const,
  preview: (period: string) => [...reportKeys.all, 'preview', period] as const,
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

export function useGenerateReport() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: GenerateDraftRequest) => {
      return apiFetch<ExpenseReport>('/reports/generate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
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
      const response = await fetch(`/reports/${reportId}/export?format=${format}`, {
        method: 'GET',
        headers: {
          Accept: format === 'pdf' ? 'application/pdf' : 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        },
      })

      if (!response.ok) {
        throw new Error(`Export failed: ${response.statusText}`)
      }

      const blob = await response.blob()
      const filename = `expense-report-${reportId}.${format === 'pdf' ? 'pdf' : 'xlsx'}`

      // Trigger download
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = filename
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
      URL.revokeObjectURL(url)

      return { filename }
    },
  })
}
