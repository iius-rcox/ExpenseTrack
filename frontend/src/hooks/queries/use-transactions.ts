import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiFetch, apiUpload } from '@/services/api'
import type {
  TransactionDetail,
  TransactionListResponse,
  StatementImport,
} from '@/types/api'

export const transactionKeys = {
  all: ['transactions'] as const,
  lists: () => [...transactionKeys.all, 'list'] as const,
  list: (filters: Record<string, unknown>) => [...transactionKeys.lists(), filters] as const,
  details: () => [...transactionKeys.all, 'detail'] as const,
  detail: (id: string) => [...transactionKeys.details(), id] as const,
  imports: () => [...transactionKeys.all, 'imports'] as const,
}

interface TransactionListParams {
  page?: number
  pageSize?: number
  matched?: boolean
  startDate?: string
  endDate?: string
  search?: string
}

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

      return apiFetch<TransactionListResponse>(`/api/transactions?${searchParams}`)
    },
  })
}

export function useTransactionDetail(id: string) {
  return useQuery({
    queryKey: transactionKeys.detail(id),
    queryFn: () => apiFetch<TransactionDetail>(`/api/transactions/${id}`),
    enabled: !!id,
  })
}

export function useStatementImports() {
  return useQuery({
    queryKey: transactionKeys.imports(),
    queryFn: () => apiFetch<StatementImport[]>('/api/statements/imports'),
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

      return apiUpload<ImportStatementResult>('/api/statements/import', formData, onProgress)
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: transactionKeys.lists() })
      queryClient.invalidateQueries({ queryKey: transactionKeys.imports() })
    },
  })
}
