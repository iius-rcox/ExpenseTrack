/**
 * Transaction Group Query Hooks (Feature 028)
 *
 * TanStack Query hooks for transaction group CRUD operations.
 * Handles:
 * - Creating groups from selected transactions
 * - Fetching group details with child transactions
 * - Updating group name/date
 * - Ungrouping (deleting groups)
 * - Adding/removing transactions from groups
 *
 * @see transaction.ts for TransactionGroupView types
 */

import {
  useQuery,
  useMutation,
  useQueryClient,
  keepPreviousData,
} from '@tanstack/react-query';
import { toast } from 'sonner';
import { apiFetch } from '@/services/api';
import { transactionKeys } from './use-transactions';
import type {
  TransactionGroupView,
  TransactionViewWithType,
  TransactionListItem,
  GroupMatchStatus,
  CreateGroupRequest,
  UpdateGroupRequest,
  AddToGroupRequest,
} from '@/types/transaction';

// =============================================================================
// Query Keys
// =============================================================================

/**
 * Query key factory for transaction groups.
 *
 * Hierarchical structure enables targeted cache invalidation:
 * - transactionGroupKeys.all: Invalidate everything
 * - transactionGroupKeys.lists(): Invalidate all list queries
 * - transactionGroupKeys.details(): Invalidate all detail queries
 * - transactionGroupKeys.detail(id): Invalidate specific group
 */
export const transactionGroupKeys = {
  all: ['transactionGroups'] as const,
  lists: () => [...transactionGroupKeys.all, 'list'] as const,
  list: (params: Record<string, unknown>) =>
    [...transactionGroupKeys.lists(), params] as const,
  details: () => [...transactionGroupKeys.all, 'detail'] as const,
  detail: (id: string) => [...transactionGroupKeys.details(), id] as const,
  mixed: () => [...transactionGroupKeys.all, 'mixed'] as const,
  mixedList: (params: Record<string, unknown>) =>
    [...transactionGroupKeys.mixed(), params] as const,
};

// =============================================================================
// API Response Types
// =============================================================================

/**
 * API response for transaction group summary (list item).
 */
interface TransactionGroupSummaryDto {
  id: string;
  name: string;
  displayDate: string;
  isDateOverridden: boolean;
  combinedAmount: number;
  transactionCount: number;
  matchStatus: number; // 0=Unmatched, 1=Proposed, 2=Matched
  matchedReceiptId?: string;
  createdAt: string;
  isReimbursable?: boolean | null;
}

/**
 * API response for transaction group detail (with transactions).
 */
interface TransactionGroupDetailDto extends TransactionGroupSummaryDto {
  transactions: {
    id: string;
    transactionDate: string;
    amount: number;
    description: string;
  }[];
}

/**
 * API response for group list.
 */
interface TransactionGroupListResponse {
  groups: TransactionGroupSummaryDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/**
 * API response for mixed transaction/group list.
 * Note: Prediction structure matches backend PredictionSummaryDto.
 */
interface TransactionMixedListResponse {
  transactions: {
    id: string;
    transactionDate: string;
    description: string;
    amount: number;
    hasMatchedReceipt: boolean;
    prediction?: {
      id: string;
      transactionId: string;
      patternId: string | null;
      vendorName: string;
      confidenceScore: number;
      confidenceLevel: 'Low' | 'Medium' | 'High';
      status: 'Pending' | 'Confirmed' | 'Rejected' | 'Ignored';
      suggestedCategory: string | null;
      suggestedGLCode: string | null;
      isManualOverride: boolean;
    } | null;
  }[];
  groups: TransactionGroupDetailDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// =============================================================================
// Defensive Type Coercion Helpers
// =============================================================================

/**
 * Check if value is an empty object {} (which would cause React Error #301).
 * Empty objects are truthy in JS, so `{} || 'default'` returns `{}`.
 */
function isEmptyObject(val: unknown): boolean {
  return (
    val !== null &&
    typeof val === 'object' &&
    !Array.isArray(val) &&
    !(val instanceof Date) &&
    Object.keys(val as object).length === 0
  );
}

/**
 * Safely coerce a value to string, protecting against empty objects.
 */
function safeString(val: unknown, _context?: string): string {
  if (val === null || val === undefined) return '';
  if (isEmptyObject(val)) return '';
  return String(val);
}

/**
 * Safely coerce a value to number, protecting against empty objects.
 * This prevents React Error #301 when numeric fields become empty objects.
 */
function safeNumber(val: unknown, fallback: number = 0, _context?: string): number {
  if (typeof val === 'number' && !isNaN(val)) return val;
  if (typeof val === 'string') {
    const parsed = Number(val);
    if (!isNaN(parsed)) return parsed;
  }
  if (isEmptyObject(val) || val === null || val === undefined) return fallback;
  return fallback;
}

// =============================================================================
// Transform Helpers
// =============================================================================

/**
 * Map numeric match status to string enum.
 */
function mapMatchStatus(status: number): GroupMatchStatus {
  switch (status) {
    case 1:
      return 'proposed';
    case 2:
      return 'matched';
    default:
      return 'unmatched';
  }
}

/**
 * Transform API TransactionGroupSummaryDto to frontend TransactionGroupView.
 * Uses defensive coercion to prevent React Error #301 from empty objects.
 */
function transformToGroupView(
  dto: TransactionGroupSummaryDto
): TransactionGroupView {
  return {
    id: safeString(dto.id, 'group.id'),
    type: 'group',
    name: safeString(dto.name, 'group.name'),
    displayDate: new Date(dto.displayDate),
    isDateOverridden: Boolean(dto.isDateOverridden),
    combinedAmount: safeNumber(dto.combinedAmount, 0, 'group.combinedAmount'),
    transactionCount: safeNumber(dto.transactionCount, 0, 'group.transactionCount'),
    matchStatus: mapMatchStatus(dto.matchStatus),
    matchedReceiptId: dto.matchedReceiptId ? safeString(dto.matchedReceiptId, 'group.matchedReceiptId') : undefined,
    createdAt: new Date(dto.createdAt),
    isReimbursable: dto.isReimbursable ?? undefined,
  };
}

/**
 * Transform API TransactionGroupDetailDto to frontend TransactionGroupView with transactions.
 * Uses defensive coercion to prevent React Error #301 from empty objects.
 */
function transformToGroupDetailView(
  dto: TransactionGroupDetailDto
): TransactionGroupView {
  return {
    ...transformToGroupView(dto),
    transactions: dto.transactions.map((tx) => ({
      id: safeString(tx.id, 'group.transaction.id'),
      date: new Date(tx.transactionDate),
      amount: safeNumber(tx.amount, 0, 'group.transaction.amount'),
      description: safeString(tx.description, 'group.transaction.description'),
    })),
  };
}

// =============================================================================
// Query Hooks
// =============================================================================

/**
 * Hook for fetching all transaction groups for the current user.
 */
export function useTransactionGroups(params: { page?: number; pageSize?: number } = {}) {
  const { page = 1, pageSize = 50 } = params;

  return useQuery({
    queryKey: transactionGroupKeys.list({ page, pageSize }),
    queryFn: async () => {
      const searchParams = new URLSearchParams();
      searchParams.set('page', String(page));
      searchParams.set('pageSize', String(pageSize));

      const response = await apiFetch<TransactionGroupListResponse>(
        `/transaction-groups?${searchParams}`
      );

      return {
        groups: response.groups.map(transformToGroupView),
        totalCount: response.totalCount,
        page: response.page,
        pageSize: response.pageSize,
      };
    },
    placeholderData: keepPreviousData,
    staleTime: 30_000,
  });
}

/**
 * Hook for fetching a single transaction group with its transactions.
 */
export function useTransactionGroup(id: string, options: { enabled?: boolean } = {}) {
  const { enabled = true } = options;

  return useQuery({
    queryKey: transactionGroupKeys.detail(id),
    queryFn: async () => {
      const response = await apiFetch<TransactionGroupDetailDto>(
        `/transaction-groups/${id}`
      );
      return transformToGroupDetailView(response);
    },
    enabled: enabled && !!id,
  });
}

/**
 * Parameters for the mixed transaction/group list query.
 * Supports all transaction filter types for comprehensive filtering.
 */
export interface MixedListParams {
  page?: number;
  pageSize?: number;
  // Date range
  startDate?: string;
  endDate?: string;
  // Text search
  search?: string;
  // Amount range
  minAmount?: number;
  maxAmount?: number;
  // Multi-select filters
  matchStatus?: string[];  // 'matched' | 'pending' | 'unmatched'
  reimbursability?: string[];  // 'business' | 'personal' | 'uncategorized'
  categories?: string[];
  // Boolean filters
  hasPendingPrediction?: boolean;
  // Sort
  sortBy?: 'date' | 'amount' | 'merchant' | 'category';
  sortOrder?: 'asc' | 'desc';
}

/**
 * Hook for fetching a mixed list of transactions and groups.
 *
 * Returns both ungrouped transactions and transaction groups in a single list,
 * suitable for rendering in the TransactionGrid component.
 *
 * @example
 * ```tsx
 * const { data, isLoading } = useMixedTransactionList({
 *   page: 1,
 *   pageSize: 50,
 *   sortBy: 'date',
 *   sortOrder: 'desc',
 * });
 * // data.items contains TransactionListItem[] (union of transactions and groups)
 * ```
 */
export function useMixedTransactionList(params: MixedListParams = {}) {
  const {
    page = 1,
    pageSize = 50,
    startDate,
    endDate,
    search,
    minAmount,
    maxAmount,
    matchStatus,
    reimbursability,
    categories,
    hasPendingPrediction,
    sortBy = 'date',
    sortOrder = 'desc',
  } = params;

  return useQuery({
    queryKey: transactionGroupKeys.mixedList({
      page,
      pageSize,
      startDate,
      endDate,
      search,
      minAmount,
      maxAmount,
      matchStatus,
      reimbursability,
      categories,
      hasPendingPrediction,
      sortBy,
      sortOrder,
    }),
    queryFn: async () => {
      const searchParams = new URLSearchParams();
      searchParams.set('page', String(page));
      searchParams.set('pageSize', String(pageSize));
      searchParams.set('sortBy', sortBy);
      searchParams.set('sortOrder', sortOrder);

      // Date range
      if (startDate) searchParams.set('startDate', startDate);
      if (endDate) searchParams.set('endDate', endDate);
      // Text search
      if (search) searchParams.set('search', search);
      // Amount range
      if (minAmount !== undefined) searchParams.set('minAmount', String(minAmount));
      if (maxAmount !== undefined) searchParams.set('maxAmount', String(maxAmount));
      // Multi-select filters (array params)
      matchStatus?.forEach((status) => searchParams.append('matchStatus', status));
      reimbursability?.forEach((r) => searchParams.append('reimbursability', r));
      categories?.forEach((cat) => searchParams.append('categories', cat));
      // Boolean filters
      if (hasPendingPrediction) searchParams.set('hasPendingPrediction', 'true');

      const response = await apiFetch<TransactionMixedListResponse>(
        `/transaction-groups/mixed?${searchParams}`
      );

      // Transform and merge transactions and groups into a single list
      const items: TransactionListItem[] = [];

      // Transform transactions to TransactionViewWithType
      for (const tx of response.transactions) {
        // Convert empty object prediction to null (defensive against malformed API responses)
        const prediction = isEmptyObject(tx.prediction) ? null : tx.prediction;

        // Apply defensive coercion to all fields
        const txDescription = safeString(tx.description, 'tx.description');
        const txAmount = safeNumber(tx.amount, 0, 'tx.amount');

        items.push({
          type: 'transaction',
          id: safeString(tx.id, 'tx.id'),
          date: new Date(tx.transactionDate),
          description: txDescription,
          merchant: prediction?.vendorName
            ? safeString(prediction.vendorName, 'prediction.vendorName')
            : txDescription.split(' ')[0],
          amount: txAmount,
          category: prediction?.suggestedCategory
            ? safeString(prediction.suggestedCategory, 'prediction.suggestedCategory')
            : '',
          categoryId: '', // Not available in mixed list response
          notes: '',
          tags: [],
          source: 'import',
          matchStatus: tx.hasMatchedReceipt ? 'matched' : 'unmatched',
          prediction: prediction
            ? {
                // Map directly from backend PredictionSummaryDto
                // Ensure all values are primitives to avoid React Error #301
                // Use defensive helpers that handle empty objects
                id: safeString(prediction.id, 'prediction.id'),
                transactionId: safeString(prediction.transactionId, 'prediction.transactionId'),
                patternId: prediction.patternId
                  ? safeString(prediction.patternId, 'prediction.patternId')
                  : null,
                vendorName: safeString(prediction.vendorName ?? '', 'prediction.vendorName'),
                confidenceScore: safeNumber(prediction.confidenceScore, 0, 'prediction.confidenceScore'),
                // Use backend-provided values directly (already strings via JsonStringEnumConverter)
                // Handle edge case where these might be empty objects
                confidenceLevel: (isEmptyObject(prediction.confidenceLevel)
                  ? 'Low'
                  : prediction.confidenceLevel) as 'Low' | 'Medium' | 'High',
                status: (isEmptyObject(prediction.status)
                  ? 'Pending'
                  : prediction.status) as 'Pending' | 'Confirmed' | 'Rejected' | 'Ignored',
                suggestedCategory: prediction.suggestedCategory && !isEmptyObject(prediction.suggestedCategory)
                  ? safeString(prediction.suggestedCategory, 'prediction.suggestedCategory')
                  : null,
                suggestedGLCode: prediction.suggestedGLCode && !isEmptyObject(prediction.suggestedGLCode)
                  ? safeString(prediction.suggestedGLCode, 'prediction.suggestedGLCode')
                  : null,
                isManualOverride: isEmptyObject(prediction.isManualOverride)
                  ? false
                  : Boolean(prediction.isManualOverride),
              }
            : undefined,
        } as TransactionViewWithType);
      }

      // Transform groups to TransactionGroupView
      for (const group of response.groups) {
        items.push(transformToGroupDetailView(group));
      }

      // Sort the combined list (backend already sorted, but we merge two arrays)
      items.sort((a, b) => {
        const dateA = a.type === 'group' ? a.displayDate : a.date;
        const dateB = b.type === 'group' ? b.displayDate : b.date;

        if (sortBy === 'date') {
          const comparison = dateA.getTime() - dateB.getTime();
          return sortOrder === 'desc' ? -comparison : comparison;
        } else {
          const amountA = a.type === 'group' ? a.combinedAmount : a.amount;
          const amountB = b.type === 'group' ? b.combinedAmount : b.amount;
          const comparison = amountA - amountB;
          return sortOrder === 'desc' ? -comparison : comparison;
        }
      });

      return {
        items,
        totalCount: response.totalCount,
        page: response.page,
        pageSize: response.pageSize,
      };
    },
    placeholderData: keepPreviousData,
    staleTime: 30_000,
  });
}

// =============================================================================
// Mutation Hooks
// =============================================================================

/**
 * Hook for creating a new transaction group.
 *
 * @example
 * ```tsx
 * const createGroup = useCreateTransactionGroup();
 * createGroup.mutate({
 *   transactionIds: ['tx-1', 'tx-2', 'tx-3'],
 *   name: 'Twilio (3 charges)', // optional, auto-generated if not provided
 *   displayDateOverride: '2024-12-31', // optional
 * });
 * ```
 */
export function useCreateTransactionGroup() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (request: CreateGroupRequest) => {
      const response = await apiFetch<TransactionGroupDetailDto>(
        '/transaction-groups',
        {
          method: 'POST',
          body: JSON.stringify(request),
        }
      );
      return transformToGroupDetailView(response);
    },

    onSuccess: (data) => {
      // Invalidate transaction lists (grouped transactions will be hidden)
      queryClient.invalidateQueries({ queryKey: transactionKeys.lists() });
      // Invalidate transaction details (grouped transactions have new groupId)
      queryClient.invalidateQueries({ queryKey: transactionKeys.details() });
      // Invalidate group lists
      queryClient.invalidateQueries({ queryKey: transactionGroupKeys.lists() });
      // Invalidate mixed lists
      queryClient.invalidateQueries({ queryKey: transactionGroupKeys.mixed() });

      toast.success('Group created', {
        description: `Created "${data.name}" with ${data.transactionCount} transactions.`,
      });
    },

    onError: (error) => {
      toast.error('Failed to create group', {
        description: error instanceof Error ? error.message : 'An unexpected error occurred',
      });
    },
  });
}

/**
 * Hook for updating a transaction group (name, date).
 *
 * @example
 * ```tsx
 * const updateGroup = useUpdateTransactionGroup();
 * updateGroup.mutate({
 *   id: 'group-123',
 *   updates: { name: 'Updated Name', displayDate: '2024-12-25' },
 * });
 * ```
 */
export function useUpdateTransactionGroup() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      id,
      updates,
    }: {
      id: string;
      updates: UpdateGroupRequest;
    }) => {
      const response = await apiFetch<TransactionGroupDetailDto>(
        `/transaction-groups/${id}`,
        {
          method: 'PATCH',
          body: JSON.stringify(updates),
        }
      );
      return transformToGroupDetailView(response);
    },

    // Optimistic update
    onMutate: async ({ id, updates }) => {
      await queryClient.cancelQueries({
        queryKey: transactionGroupKeys.detail(id),
      });

      const previousGroup = queryClient.getQueryData<TransactionGroupView>(
        transactionGroupKeys.detail(id)
      );

      // Optimistically update the group
      if (previousGroup) {
        queryClient.setQueryData<TransactionGroupView>(
          transactionGroupKeys.detail(id),
          {
            ...previousGroup,
            name: updates.name ?? previousGroup.name,
            displayDate: updates.displayDate
              ? new Date(updates.displayDate)
              : previousGroup.displayDate,
            isDateOverridden: updates.displayDate
              ? true
              : previousGroup.isDateOverridden,
          }
        );
      }

      return { previousGroup };
    },

    onError: (error, { id }, context) => {
      // Rollback optimistic update
      if (context?.previousGroup) {
        queryClient.setQueryData(
          transactionGroupKeys.detail(id),
          context.previousGroup
        );
      }

      toast.error('Failed to update group', {
        description: error instanceof Error ? error.message : 'An unexpected error occurred',
      });
    },

    onSuccess: (data) => {
      toast.success('Group updated', {
        description: `"${data.name}" has been updated.`,
      });
    },

    onSettled: (_, _error, { id }) => {
      queryClient.invalidateQueries({
        queryKey: transactionGroupKeys.detail(id),
      });
      queryClient.invalidateQueries({
        queryKey: transactionGroupKeys.lists(),
      });
      queryClient.invalidateQueries({
        queryKey: transactionGroupKeys.mixed(),
      });
    },
  });
}

/**
 * Hook for deleting (ungrouping) a transaction group.
 *
 * This removes the group and returns all transactions to ungrouped state.
 * Any receipt match on the group is also removed.
 *
 * @example
 * ```tsx
 * const deleteGroup = useDeleteTransactionGroup();
 * deleteGroup.mutate('group-123');
 * ```
 */
export function useDeleteTransactionGroup() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: string) => {
      await apiFetch<void>(`/transaction-groups/${id}`, {
        method: 'DELETE',
      });
      return { id };
    },

    onSuccess: () => {
      // Invalidate transaction lists (previously grouped transactions will reappear)
      queryClient.invalidateQueries({ queryKey: transactionKeys.lists() });
      // Invalidate transaction details (ungrouped transactions have groupId cleared)
      queryClient.invalidateQueries({ queryKey: transactionKeys.details() });
      // Invalidate group lists
      queryClient.invalidateQueries({ queryKey: transactionGroupKeys.lists() });
      // Invalidate mixed lists
      queryClient.invalidateQueries({ queryKey: transactionGroupKeys.mixed() });

      toast.success('Group removed', {
        description: 'Transactions have been ungrouped.',
      });
    },

    onError: (error) => {
      toast.error('Failed to remove group', {
        description: error instanceof Error ? error.message : 'An unexpected error occurred',
      });
    },
  });
}

/**
 * Hook for adding transactions to an existing group.
 *
 * @example
 * ```tsx
 * const addToGroup = useAddTransactionsToGroup();
 * addToGroup.mutate({
 *   groupId: 'group-123',
 *   transactionIds: ['tx-4', 'tx-5'],
 * });
 * ```
 */
export function useAddTransactionsToGroup() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      groupId,
      transactionIds,
    }: {
      groupId: string;
      transactionIds: string[];
    }) => {
      const request: AddToGroupRequest = { transactionIds };
      const response = await apiFetch<TransactionGroupDetailDto>(
        `/transaction-groups/${groupId}/transactions`,
        {
          method: 'POST',
          body: JSON.stringify(request),
        }
      );
      return transformToGroupDetailView(response);
    },

    onSuccess: (data, { groupId, transactionIds }) => {
      // Invalidate transaction lists (added transactions will be hidden)
      queryClient.invalidateQueries({ queryKey: transactionKeys.lists() });
      // Invalidate transaction details (added transactions have new groupId)
      queryClient.invalidateQueries({ queryKey: transactionKeys.details() });
      // Invalidate the specific group detail
      queryClient.invalidateQueries({
        queryKey: transactionGroupKeys.detail(groupId),
      });
      // Invalidate group lists (count changed)
      queryClient.invalidateQueries({ queryKey: transactionGroupKeys.lists() });
      // Invalidate mixed lists
      queryClient.invalidateQueries({ queryKey: transactionGroupKeys.mixed() });

      const count = transactionIds.length;
      toast.success('Transactions added', {
        description: `Added ${count} transaction${count === 1 ? '' : 's'} to "${data.name}".`,
      });
    },

    onError: (error) => {
      toast.error('Failed to add transactions', {
        description: error instanceof Error ? error.message : 'An unexpected error occurred',
      });
    },
  });
}

/**
 * Hook for removing a transaction from a group.
 *
 * Note: Groups must have at least 2 transactions. Use deleteGroup to fully ungroup.
 *
 * @example
 * ```tsx
 * const removeFromGroup = useRemoveTransactionFromGroup();
 * removeFromGroup.mutate({
 *   groupId: 'group-123',
 *   transactionId: 'tx-4',
 * });
 * ```
 */
export function useRemoveTransactionFromGroup() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      groupId,
      transactionId,
    }: {
      groupId: string;
      transactionId: string;
    }) => {
      const response = await apiFetch<TransactionGroupDetailDto>(
        `/transaction-groups/${groupId}/transactions/${transactionId}`,
        {
          method: 'DELETE',
        }
      );
      return transformToGroupDetailView(response);
    },

    // Optimistic update - remove transaction from local cache
    onMutate: async ({ groupId, transactionId }) => {
      await queryClient.cancelQueries({
        queryKey: transactionGroupKeys.detail(groupId),
      });

      const previousGroup = queryClient.getQueryData<TransactionGroupView>(
        transactionGroupKeys.detail(groupId)
      );

      // Pre-validate: groups must have at least 2 transactions
      if (previousGroup && previousGroup.transactionCount <= 2) {
        throw new Error('Groups must have at least 2 transactions. Delete the group instead.');
      }

      if (previousGroup?.transactions) {
        const removedTx = previousGroup.transactions.find(
          (tx) => tx.id === transactionId
        );
        if (removedTx) {
          queryClient.setQueryData<TransactionGroupView>(
            transactionGroupKeys.detail(groupId),
            {
              ...previousGroup,
              transactions: previousGroup.transactions.filter(
                (tx) => tx.id !== transactionId
              ),
              transactionCount: previousGroup.transactionCount - 1,
              combinedAmount: previousGroup.combinedAmount - removedTx.amount,
            }
          );
        }
      }

      return { previousGroup };
    },

    onError: (error, { groupId }, context) => {
      // Rollback optimistic update
      if (context?.previousGroup) {
        queryClient.setQueryData(
          transactionGroupKeys.detail(groupId),
          context.previousGroup
        );
      }

      toast.error('Failed to remove transaction', {
        description: error instanceof Error ? error.message : 'An unexpected error occurred',
      });
    },

    onSuccess: (data) => {
      toast.success('Transaction removed', {
        description: `Transaction removed from "${data.name}".`,
      });
    },

    onSettled: (_, _error, { groupId }) => {
      // Invalidate transaction lists (removed transaction will reappear)
      queryClient.invalidateQueries({ queryKey: transactionKeys.lists() });
      // Invalidate transaction details (removed transaction has groupId cleared)
      queryClient.invalidateQueries({ queryKey: transactionKeys.details() });
      // Invalidate the specific group detail
      queryClient.invalidateQueries({
        queryKey: transactionGroupKeys.detail(groupId),
      });
      // Invalidate group lists (count changed)
      queryClient.invalidateQueries({ queryKey: transactionGroupKeys.lists() });
      // Invalidate mixed lists
      queryClient.invalidateQueries({ queryKey: transactionGroupKeys.mixed() });
    },
  });
}

// =============================================================================
// Utility Hooks
// =============================================================================

/**
 * Hook to check if selected transactions can be grouped.
 *
 * Returns validation result for the Group button.
 */
export function useCanGroupTransactions(transactionIds: string[]) {
  // Basic validation: need at least 2 transactions
  const isCountValid = transactionIds.length >= 2;

  // TODO: Could add API validation to check:
  // - None are already grouped
  // - None already have matched receipts
  // For now, rely on server-side validation and error handling

  return {
    canGroup: isCountValid,
    reason: !isCountValid
      ? 'Select at least 2 transactions to group'
      : undefined,
  };
}

/**
 * API response for marking group reimbursability.
 */
interface GroupReimbursabilityResponse {
  success: boolean;
  group: TransactionGroupDetailDto | null;
  transactionsUpdated: number;
  message?: string;
}

/**
 * Hook for marking a transaction group as Business or Personal.
 * Updates all transactions in the group with the classification via the prediction system.
 *
 * @example
 * ```tsx
 * const markGroup = useMarkGroupReimbursability();
 * markGroup.mutate({ groupId: 'group-123', isReimbursable: true }); // Mark as Business
 * markGroup.mutate({ groupId: 'group-123', isReimbursable: false }); // Mark as Personal
 * ```
 */
export function useMarkGroupReimbursability() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      groupId,
      isReimbursable,
    }: {
      groupId: string;
      isReimbursable: boolean;
    }) => {
      const response = await apiFetch<GroupReimbursabilityResponse>(
        `/transaction-groups/${groupId}/reimbursability`,
        {
          method: 'POST',
          body: JSON.stringify({ isReimbursable }),
        }
      );

      return {
        ...response,
        group: response.group ? transformToGroupDetailView(response.group) : null,
      };
    },

    onSuccess: (data, { isReimbursable }) => {
      // Invalidate group queries
      queryClient.invalidateQueries({ queryKey: transactionGroupKeys.lists() });
      queryClient.invalidateQueries({ queryKey: transactionGroupKeys.mixed() });
      queryClient.invalidateQueries({ queryKey: transactionGroupKeys.details() });

      // Also invalidate transaction queries (predictions changed)
      queryClient.invalidateQueries({ queryKey: transactionKeys.lists() });
      queryClient.invalidateQueries({ queryKey: transactionKeys.details() });

      const label = isReimbursable ? 'Business' : 'Personal';
      toast.success(`Group marked as ${label}`, {
        description: `${data.transactionsUpdated} transaction(s) classified.`,
      });
    },

    onError: (error) => {
      toast.error('Failed to classify group', {
        description: error instanceof Error ? error.message : 'An unexpected error occurred',
      });
    },
  });
}
