import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/services/api'
import type {
  MatchDetail,
  MatchProposal,
  ProposalListResponse,
  MatchingStats,
  AutoMatchResponse,
  ConfirmMatchRequest,
  MatchReceiptSummary,
  MatchTransactionSummary,
} from '@/types/api'
import type { ManualMatchRequest, MatchCandidate } from '@/types/match'

/**
 * Normalizes confidence scores from backend (0-100) to frontend (0-1) scale.
 * The backend returns scores as percentages (e.g., 95 for 95%) while the
 * frontend design tokens and display logic expect decimal values (e.g., 0.95).
 */
function normalizeProposal<T extends MatchProposal>(proposal: T): T {
  return {
    ...proposal,
    // Convert 0-100 to 0-1 scale
    confidenceScore: proposal.confidenceScore / 100,
    amountScore: proposal.amountScore / 100,
    dateScore: proposal.dateScore / 100,
    vendorScore: proposal.vendorScore / 100,
  }
}

function normalizeProposalList(response: ProposalListResponse): ProposalListResponse {
  return {
    ...response,
    items: response.items.map(normalizeProposal),
  }
}

function normalizeStats(stats: MatchingStats): MatchingStats {
  return {
    ...stats,
    averageConfidence: stats.averageConfidence / 100,
  }
}

// Paginated response types matching backend DTOs
interface UnmatchedReceiptsResponse {
  items: MatchReceiptSummary[]
  totalCount: number
  page: number
  pageSize: number
}

interface UnmatchedTransactionsResponse {
  items: MatchTransactionSummary[]
  totalCount: number
  page: number
  pageSize: number
}

export const matchingKeys = {
  all: ['matching'] as const,
  proposals: () => [...matchingKeys.all, 'proposals'] as const,
  proposalList: (filters: Record<string, unknown>) => [...matchingKeys.proposals(), filters] as const,
  proposal: (id: string) => [...matchingKeys.proposals(), id] as const,
  stats: () => [...matchingKeys.all, 'stats'] as const,
  unmatchedReceipts: () => [...matchingKeys.all, 'unmatched-receipts'] as const,
  unmatchedTransactions: () => [...matchingKeys.all, 'unmatched-transactions'] as const,
  candidates: (receiptId: string) => [...matchingKeys.all, 'candidates', receiptId] as const,
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

      const response = await apiFetch<ProposalListResponse>(`/matching/proposals?${searchParams}`)
      return normalizeProposalList(response)
    },
  })
}

export function useMatchProposal(matchId: string) {
  return useQuery({
    queryKey: matchingKeys.proposal(matchId),
    queryFn: async () => {
      const response = await apiFetch<MatchDetail>(`/matching/proposals/${matchId}`)
      return normalizeProposal(response)
    },
    enabled: !!matchId,
  })
}

export function useMatchingStats() {
  return useQuery({
    queryKey: matchingKeys.stats(),
    queryFn: async () => {
      const response = await apiFetch<MatchingStats>('/matching/stats')
      return normalizeStats(response)
    },
    staleTime: 30_000,
  })
}

export function useUnmatchedReceipts() {
  return useQuery({
    queryKey: matchingKeys.unmatchedReceipts(),
    queryFn: async () => {
      const response = await apiFetch<UnmatchedReceiptsResponse>('/matching/receipts/unmatched')
      return response.items
    },
    staleTime: 60_000,
  })
}

export function useUnmatchedTransactions() {
  return useQuery({
    queryKey: matchingKeys.unmatchedTransactions(),
    queryFn: async () => {
      const response = await apiFetch<UnmatchedTransactionsResponse>('/matching/transactions/unmatched')
      return response.items
    },
    staleTime: 60_000,
  })
}

/**
 * Normalizes candidate scores from backend (0-100) to frontend (0-1) scale.
 */
function normalizeCandidate(candidate: MatchCandidate): MatchCandidate {
  return {
    ...candidate,
    confidenceScore: candidate.confidenceScore / 100,
    amountScore: candidate.amountScore / 100,
    dateScore: candidate.dateScore / 100,
    vendorScore: candidate.vendorScore / 100,
  }
}

/**
 * Fetches ranked match candidates for a specific receipt.
 * Returns both ungrouped transactions and transaction groups as potential matches.
 */
export function useMatchCandidates(receiptId: string, limit: number = 10) {
  return useQuery({
    queryKey: matchingKeys.candidates(receiptId),
    queryFn: async () => {
      const response = await apiFetch<MatchCandidate[]>(
        `/matching/candidates/${receiptId}?limit=${limit}`
      )
      return response.map(normalizeCandidate)
    },
    enabled: !!receiptId,
    staleTime: 30_000,
  })
}

export function useConfirmMatch() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ matchId, data }: { matchId: string; data?: ConfirmMatchRequest }) => {
      return apiFetch<MatchDetail>(`/matching/${matchId}/confirm`, {
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
      return apiFetch<MatchDetail>(`/matching/${matchId}/reject`, {
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
      const response = await apiFetch<AutoMatchResponse>('/matching/auto', {
        method: 'POST',
      })
      // Normalize the proposals in the response
      return {
        ...response,
        proposals: response.proposals.map(normalizeProposal),
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: matchingKeys.all })
    },
  })
}

/**
 * T067: Batch approve matches above a confidence threshold
 */
export function useBatchApprove() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({ ids, minConfidence }: { ids?: string[]; minConfidence?: number }) => {
      // Convert frontend 0-1 threshold to backend 0-100 scale
      const backendConfidence = minConfidence !== undefined ? minConfidence * 100 : undefined
      return apiFetch<{ approved: number; skipped: number }>('/matching/batch-approve', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ids, minConfidence: backendConfidence }),
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: matchingKeys.all })
    },
  })
}

/**
 * Batch reject multiple matches
 */
export function useBatchReject() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (ids: string[]) => {
      return apiFetch<{ rejected: number }>('/matching/batch-reject', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ ids }),
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: matchingKeys.all })
    },
  })
}

/**
 * Combined hook for match review workspace
 * Provides all state and actions needed for the review UI
 */
export function useMatchReviewWorkspace(params: ProposalListParams = {}) {
  const proposalsQuery = useMatchProposals(params)
  const confirmMatch = useConfirmMatch()
  const rejectMatch = useRejectMatch()
  const manualMatch = useManualMatch()
  const batchApprove = useBatchApprove()
  const batchReject = useBatchReject()

  const isProcessing =
    confirmMatch.isPending ||
    rejectMatch.isPending ||
    manualMatch.isPending ||
    batchApprove.isPending ||
    batchReject.isPending

  return {
    // Data
    proposals: proposalsQuery.data?.items ?? [],
    totalCount: proposalsQuery.data?.totalCount ?? 0,
    page: proposalsQuery.data?.page ?? 1,
    pageSize: proposalsQuery.data?.pageSize ?? 20,
    isLoading: proposalsQuery.isLoading,
    isError: proposalsQuery.isError,
    error: proposalsQuery.error,

    // Actions
    confirm: (matchId: string) => confirmMatch.mutate({ matchId }),
    reject: rejectMatch.mutate,
    createManualMatch: manualMatch.mutate,
    batchApprove: batchApprove.mutate,
    batchReject: batchReject.mutate,

    // Status
    isProcessing,
    isConfirming: confirmMatch.isPending,
    isRejecting: rejectMatch.isPending,

    // Refetch
    refetch: proposalsQuery.refetch,
  }
}
