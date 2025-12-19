import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/services/api'
import type {
  MatchDetail,
  ProposalListResponse,
  MatchingStats,
  AutoMatchResponse,
  ConfirmMatchRequest,
  ManualMatchRequest,
  TransactionSummary,
  ReceiptSummary,
} from '@/types/api'

export const matchingKeys = {
  all: ['matching'] as const,
  proposals: () => [...matchingKeys.all, 'proposals'] as const,
  proposalList: (filters: Record<string, unknown>) => [...matchingKeys.proposals(), filters] as const,
  proposal: (id: string) => [...matchingKeys.proposals(), id] as const,
  stats: () => [...matchingKeys.all, 'stats'] as const,
  unmatchedReceipts: () => [...matchingKeys.all, 'unmatched-receipts'] as const,
  unmatchedTransactions: () => [...matchingKeys.all, 'unmatched-transactions'] as const,
}

interface ProposalListParams {
  page?: number
  pageSize?: number
  status?: string
}

export function useMatchProposals(params: ProposalListParams = {}) {
  const { page = 1, pageSize = 20, status } = params

  return useQuery({
    queryKey: matchingKeys.proposalList({ page, pageSize, status }),
    queryFn: async () => {
      const searchParams = new URLSearchParams()
      searchParams.set('page', String(page))
      searchParams.set('pageSize', String(pageSize))
      if (status) searchParams.set('status', status)

      return apiFetch<ProposalListResponse>(`/matching/proposals?${searchParams}`)
    },
  })
}

export function useMatchProposal(matchId: string) {
  return useQuery({
    queryKey: matchingKeys.proposal(matchId),
    queryFn: () => apiFetch<MatchDetail>(`/matching/proposals/${matchId}`),
    enabled: !!matchId,
  })
}

export function useMatchingStats() {
  return useQuery({
    queryKey: matchingKeys.stats(),
    queryFn: () => apiFetch<MatchingStats>('/matching/stats'),
    staleTime: 30_000,
  })
}

export function useUnmatchedReceipts() {
  return useQuery({
    queryKey: matchingKeys.unmatchedReceipts(),
    queryFn: () => apiFetch<ReceiptSummary[]>('/matching/unmatched-receipts'),
    staleTime: 60_000,
  })
}

export function useUnmatchedTransactions() {
  return useQuery({
    queryKey: matchingKeys.unmatchedTransactions(),
    queryFn: () => apiFetch<TransactionSummary[]>('/matching/unmatched-transactions'),
    staleTime: 60_000,
  })
}

export function useConfirmMatch() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ matchId, data }: { matchId: string; data?: ConfirmMatchRequest }) => {
      return apiFetch<MatchDetail>(`/matching/proposals/${matchId}/confirm`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: data ? JSON.stringify(data) : undefined,
      })
    },
    onMutate: async ({ matchId }) => {
      // Cancel outgoing fetches
      await queryClient.cancelQueries({ queryKey: matchingKeys.proposals() })

      // Snapshot previous value
      const previousProposals = queryClient.getQueriesData({ queryKey: matchingKeys.proposals() })

      // Optimistically update
      queryClient.setQueriesData(
        { queryKey: matchingKeys.proposals() },
        (old: ProposalListResponse | undefined) => {
          if (!old) return old
          return {
            ...old,
            items: old.items.map((item) =>
              item.matchId === matchId ? { ...item, status: 'Confirmed' } : item
            ),
          }
        }
      )

      return { previousProposals }
    },
    onError: (_error, _variables, context) => {
      // Rollback on error
      if (context?.previousProposals) {
        context.previousProposals.forEach(([queryKey, data]) => {
          queryClient.setQueryData(queryKey, data)
        })
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: matchingKeys.proposals() })
      queryClient.invalidateQueries({ queryKey: matchingKeys.stats() })
    },
  })
}

export function useRejectMatch() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (matchId: string) => {
      return apiFetch<MatchDetail>(`/matching/proposals/${matchId}/reject`, {
        method: 'POST',
      })
    },
    onMutate: async (matchId) => {
      await queryClient.cancelQueries({ queryKey: matchingKeys.proposals() })

      const previousProposals = queryClient.getQueriesData({ queryKey: matchingKeys.proposals() })

      queryClient.setQueriesData(
        { queryKey: matchingKeys.proposals() },
        (old: ProposalListResponse | undefined) => {
          if (!old) return old
          return {
            ...old,
            items: old.items.map((item) =>
              item.matchId === matchId ? { ...item, status: 'Rejected' } : item
            ),
          }
        }
      )

      return { previousProposals }
    },
    onError: (_error, _variables, context) => {
      if (context?.previousProposals) {
        context.previousProposals.forEach(([queryKey, data]) => {
          queryClient.setQueryData(queryKey, data)
        })
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: matchingKeys.proposals() })
      queryClient.invalidateQueries({ queryKey: matchingKeys.stats() })
    },
  })
}

export function useManualMatch() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: ManualMatchRequest) => {
      return apiFetch<MatchDetail>('/matching/manual', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: matchingKeys.all })
    },
  })
}

export function useTriggerAutoMatch() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async () => {
      return apiFetch<AutoMatchResponse>('/matching/auto-match', {
        method: 'POST',
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: matchingKeys.all })
    },
  })
}
