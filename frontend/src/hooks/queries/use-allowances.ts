/**
 * TanStack Query hooks for Recurring Expense Allowances
 *
 * These hooks manage the API interactions for creating, reading,
 * updating, and deleting recurring expense allowances.
 */

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/services/api'
import type {
  Allowance,
  AllowanceListResponse,
  CreateAllowanceRequest,
  UpdateAllowanceRequest,
} from '@/types/allowance'

/**
 * Query keys for allowance-related queries
 */
export const allowanceKeys = {
  all: ['allowances'] as const,
  list: () => [...allowanceKeys.all, 'list'] as const,
  listActive: () => [...allowanceKeys.list(), { activeOnly: true }] as const,
  listAll: () => [...allowanceKeys.list(), { activeOnly: false }] as const,
  detail: (id: string) => [...allowanceKeys.all, 'detail', id] as const,
}

/**
 * Fetch all allowances for the current user
 *
 * @param activeOnly - If true, only return active allowances (default: false)
 */
export function useAllowances(activeOnly: boolean = false) {
  return useQuery({
    queryKey: activeOnly ? allowanceKeys.listActive() : allowanceKeys.listAll(),
    queryFn: () =>
      apiFetch<AllowanceListResponse>(`/allowances?activeOnly=${activeOnly}`),
    staleTime: 5 * 60 * 1000, // 5 minutes
  })
}

/**
 * Create a new recurring expense allowance
 */
export function useCreateAllowance() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: CreateAllowanceRequest) => {
      return apiFetch<Allowance>('/allowances', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      })
    },
    onSuccess: () => {
      // Invalidate all allowance list queries to refetch
      queryClient.invalidateQueries({ queryKey: allowanceKeys.list() })
    },
  })
}

/**
 * Update an existing allowance
 */
export function useUpdateAllowance() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      id,
      data,
    }: {
      id: string
      data: UpdateAllowanceRequest
    }) => {
      return apiFetch<Allowance>(`/allowances/${id}`, {
        method: 'PATCH',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      })
    },
    onSuccess: (data) => {
      // Update the specific allowance in cache
      queryClient.setQueryData(allowanceKeys.detail(data.id), data)
      // Invalidate list queries to refetch
      queryClient.invalidateQueries({ queryKey: allowanceKeys.list() })
    },
  })
}

/**
 * Delete (soft-delete) an allowance
 */
export function useDeleteAllowance() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (id: string) => {
      return apiFetch(`/allowances/${id}`, {
        method: 'DELETE',
      })
    },
    onSuccess: () => {
      // Invalidate list queries to refetch
      queryClient.invalidateQueries({ queryKey: allowanceKeys.list() })
    },
  })
}
