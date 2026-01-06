/**
 * TanStack Query hooks for Missing Receipts (Feature 026)
 *
 * Provides React Query hooks for fetching and mutating missing receipt data
 * with optimistic updates, proper cache invalidation, and retry logic.
 */
import { useEffect } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getMissingReceipts,
  getMissingReceiptsWidget,
  updateReceiptUrl,
  dismissMissingReceipt,
  type MissingReceiptsParams,
} from '@/lib/api/missing-receipts'
import type { MissingReceiptsListResponse } from '@/types/api'

/**
 * Retry delay with exponential backoff.
 * 1s → 2s → 4s → 8s → 16s → 30s (capped)
 */
const retryDelay = (attemptIndex: number) =>
  Math.min(1000 * 2 ** attemptIndex, 30000)

/**
 * Query key factory for missing receipts.
 * Follows TanStack Query best practices for hierarchical keys.
 */
export const missingReceiptsKeys = {
  all: ['missing-receipts'] as const,
  lists: () => [...missingReceiptsKeys.all, 'list'] as const,
  list: (params: MissingReceiptsParams) => [...missingReceiptsKeys.lists(), params] as const,
  widget: () => [...missingReceiptsKeys.all, 'widget'] as const,
  detail: (id: string) => [...missingReceiptsKeys.all, 'detail', id] as const,
}

/**
 * Hook for fetching paginated list of missing receipts.
 * Supports sorting, pagination, and filtering dismissed items.
 * Includes retry logic with exponential backoff.
 */
export function useMissingReceipts(params: MissingReceiptsParams = {}) {
  return useQuery({
    queryKey: missingReceiptsKeys.list(params),
    queryFn: () => getMissingReceipts(params),
    staleTime: 30_000, // 30 seconds - missing receipts don't change frequently
    retry: 3,
    retryDelay,
  })
}

/**
 * Hook for prefetching adjacent pages for smoother pagination.
 * Call this in the list component to prefetch next/prev pages.
 */
export function usePrefetchAdjacentPages(params: MissingReceiptsParams, totalPages: number) {
  const queryClient = useQueryClient()
  const currentPage = params.page ?? 1

  useEffect(() => {
    // Prefetch next page
    if (currentPage < totalPages) {
      queryClient.prefetchQuery({
        queryKey: missingReceiptsKeys.list({ ...params, page: currentPage + 1 }),
        queryFn: () => getMissingReceipts({ ...params, page: currentPage + 1 }),
        staleTime: 30_000,
      })
    }

    // Prefetch previous page (if not already on first page)
    if (currentPage > 1) {
      queryClient.prefetchQuery({
        queryKey: missingReceiptsKeys.list({ ...params, page: currentPage - 1 }),
        queryFn: () => getMissingReceipts({ ...params, page: currentPage - 1 }),
        staleTime: 30_000,
      })
    }
  }, [queryClient, currentPage, totalPages, params])
}

/**
 * Hook for fetching widget summary data.
 * Returns total count and top 3 most recent missing receipts.
 * Includes retry logic with exponential backoff.
 */
export function useMissingReceiptsWidget() {
  return useQuery({
    queryKey: missingReceiptsKeys.widget(),
    queryFn: getMissingReceiptsWidget,
    staleTime: 60_000, // 1 minute - widget data is less critical to be real-time
    retry: 3,
    retryDelay,
  })
}

/**
 * Hook for updating a receipt URL.
 * Uses optimistic updates for instant UI feedback.
 */
export function useUpdateReceiptUrl() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ transactionId, receiptUrl }: { transactionId: string; receiptUrl: string | null }) =>
      updateReceiptUrl(transactionId, receiptUrl),

    onMutate: async ({ transactionId, receiptUrl }) => {
      // Cancel outgoing fetches to prevent overwrite
      await queryClient.cancelQueries({ queryKey: missingReceiptsKeys.lists() })
      await queryClient.cancelQueries({ queryKey: missingReceiptsKeys.widget() })

      // Snapshot previous values for rollback
      const previousLists = queryClient.getQueriesData({ queryKey: missingReceiptsKeys.lists() })
      const previousWidget = queryClient.getQueryData(missingReceiptsKeys.widget())

      // Optimistically update all list queries
      queryClient.setQueriesData<MissingReceiptsListResponse>(
        { queryKey: missingReceiptsKeys.lists() },
        (old) => {
          if (!old) return old
          return {
            ...old,
            items: old.items.map((item) =>
              item.transactionId === transactionId
                ? { ...item, receiptUrl }
                : item
            ),
          }
        }
      )

      return { previousLists, previousWidget }
    },

    onError: (_error, _variables, context) => {
      // Rollback on error
      if (context?.previousLists) {
        context.previousLists.forEach(([queryKey, data]) => {
          queryClient.setQueryData(queryKey, data)
        })
      }
      if (context?.previousWidget) {
        queryClient.setQueryData(missingReceiptsKeys.widget(), context.previousWidget)
      }
    },

    onSettled: () => {
      // Refetch to ensure consistency with server
      queryClient.invalidateQueries({ queryKey: missingReceiptsKeys.lists() })
      queryClient.invalidateQueries({ queryKey: missingReceiptsKeys.widget() })
    },
  })
}

/**
 * Hook for dismissing or restoring a missing receipt.
 * Uses optimistic updates - items disappear immediately from list when dismissed.
 */
export function useDismissMissingReceipt() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ transactionId, dismiss }: { transactionId: string; dismiss: boolean | null }) =>
      dismissMissingReceipt(transactionId, dismiss),

    onMutate: async ({ transactionId, dismiss }) => {
      await queryClient.cancelQueries({ queryKey: missingReceiptsKeys.lists() })
      await queryClient.cancelQueries({ queryKey: missingReceiptsKeys.widget() })

      const previousLists = queryClient.getQueriesData({ queryKey: missingReceiptsKeys.lists() })
      const previousWidget = queryClient.getQueryData(missingReceiptsKeys.widget())

      // Optimistically update list queries
      queryClient.setQueriesData<MissingReceiptsListResponse>(
        { queryKey: missingReceiptsKeys.lists() },
        (old) => {
          if (!old) return old

          // Check if this is a list that shows dismissed items
          // For now, we update the isDismissed flag; UI will filter based on includeDismissed param
          return {
            ...old,
            items: old.items.map((item) =>
              item.transactionId === transactionId
                ? { ...item, isDismissed: dismiss === true }
                : item
            ),
            // Adjust total count for queries that don't include dismissed
            totalCount: dismiss === true ? old.totalCount - 1 : old.totalCount,
          }
        }
      )

      return { previousLists, previousWidget }
    },

    onError: (_error, _variables, context) => {
      if (context?.previousLists) {
        context.previousLists.forEach(([queryKey, data]) => {
          queryClient.setQueryData(queryKey, data)
        })
      }
      if (context?.previousWidget) {
        queryClient.setQueryData(missingReceiptsKeys.widget(), context.previousWidget)
      }
    },

    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: missingReceiptsKeys.lists() })
      queryClient.invalidateQueries({ queryKey: missingReceiptsKeys.widget() })
    },
  })
}

/**
 * Combined hook for the missing receipts workspace.
 * Provides all state and actions needed for the full list view.
 */
export function useMissingReceiptsWorkspace(params: MissingReceiptsParams = {}) {
  const listQuery = useMissingReceipts(params)
  const updateUrl = useUpdateReceiptUrl()
  const dismiss = useDismissMissingReceipt()
  const queryClient = useQueryClient()

  const isProcessing = updateUrl.isPending || dismiss.isPending
  const totalPages = listQuery.data?.totalPages ?? 0

  // Prefetch adjacent pages for smoother pagination
  usePrefetchAdjacentPages(params, totalPages)

  /**
   * Dismiss multiple receipts at once.
   * Returns a promise that resolves when all are dismissed.
   */
  const dismissMultiple = async (transactionIds: string[]) => {
    const promises = transactionIds.map((transactionId) =>
      dismiss.mutateAsync({ transactionId, dismiss: true })
    )
    await Promise.all(promises)
    // Invalidate queries after batch operation
    queryClient.invalidateQueries({ queryKey: missingReceiptsKeys.lists() })
    queryClient.invalidateQueries({ queryKey: missingReceiptsKeys.widget() })
  }

  return {
    // Data
    items: listQuery.data?.items ?? [],
    totalCount: listQuery.data?.totalCount ?? 0,
    page: listQuery.data?.page ?? 1,
    pageSize: listQuery.data?.pageSize ?? 25,
    totalPages,
    isLoading: listQuery.isLoading,
    isError: listQuery.isError,
    error: listQuery.error,

    // Actions
    updateReceiptUrl: (transactionId: string, receiptUrl: string | null) =>
      updateUrl.mutate({ transactionId, receiptUrl }),
    dismissReceipt: (transactionId: string) =>
      dismiss.mutate({ transactionId, dismiss: true }),
    restoreReceipt: (transactionId: string) =>
      dismiss.mutate({ transactionId, dismiss: false }),
    dismissMultiple,

    // Status
    isProcessing,
    isUpdatingUrl: updateUrl.isPending,
    isDismissing: dismiss.isPending,

    // Refetch
    refetch: listQuery.refetch,
  }
}
