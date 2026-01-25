"use client"

/**
 * Transactions Page (T058)
 *
 * Main transaction explorer with:
 * - Tab toggle between Transactions and Patterns views
 * - Advanced filtering and search
 * - Virtualized grid for large datasets
 * - Inline editing with optimistic updates
 * - Bulk operations (categorize, tag, export, delete)
 * - Statement import dialog
 */

import { useState, useCallback, useMemo } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { z } from 'zod'
import {
  useTransactionCategories,
  useTransactionTags,
  useUpdateTransaction,
  useBulkUpdateTransactions,
  useBulkDeleteTransactions,
  useExportTransactions,
  useMarkTransactionReimbursable,
  useMarkTransactionNotReimbursable,
  useClearReimbursabilityOverride,
  useBulkMarkReimbursability,
} from '@/hooks/queries/use-transactions'
import {
  useMixedTransactionList,
  useCreateTransactionGroup,
  useUpdateTransactionGroup,
  useDeleteTransactionGroup,
  useRemoveTransactionFromGroup,
  useCanGroupTransactions,
} from '@/hooks/queries/use-transaction-groups'
import { computeSelectionComposition } from '@/components/transactions/bulk-actions-bar'
import { usePatternWorkspace, useGeneratePredictions } from '@/hooks/queries/use-predictions'
import { Link } from '@tanstack/react-router'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { toast } from 'sonner'
import { Upload, Sparkles, ListFilter } from 'lucide-react'
import { TransactionFilterPanel } from '@/components/transactions/transaction-filter-panel'
import { TransactionGrid } from '@/components/transactions/transaction-grid'
import { BulkActionsBar } from '@/components/transactions/bulk-actions-bar'
import { CreateGroupDialog } from '@/components/transactions/create-group-dialog'
import {
  PatternGrid,
  PatternStats,
  PatternFilterPanel,
  BulkPatternActions,
} from '@/components/patterns'
import {
  DEFAULT_TRANSACTION_FILTERS,
  DEFAULT_TRANSACTION_SELECTION,
} from '@/types/transaction'
import {
  DEFAULT_PATTERN_GRID_FILTERS,
  DEFAULT_PATTERN_SELECTION,
  DEFAULT_PATTERN_SORT,
} from '@/types/prediction'
import type {
  TransactionView,
  TransactionFilters,
  TransactionSortConfig,
  TransactionSelectionState,
  TransactionListItem,
} from '@/types/transaction'
import type {
  PatternGridFilters,
  PatternSortConfig,
  PatternSelectionState,
} from '@/types/prediction'

// Search params schema for URL state - supports all filter types
const transactionSearchSchema = z.object({
  // View and pagination
  view: z.enum(['transactions', 'patterns']).optional().default('transactions'),
  page: z.coerce.number().optional().default(1),
  pageSize: z.coerce.number().optional().default(50),
  // Text search
  search: z.string().optional(),
  // Date range
  startDate: z.string().optional(),
  endDate: z.string().optional(),
  // Amount range
  minAmount: z.coerce.number().optional(),
  maxAmount: z.coerce.number().optional(),
  // Multi-select filters (comma-separated in URL)
  matchStatus: z.string().optional(), // comma-separated: matched,pending,unmatched
  categories: z.string().optional(),  // comma-separated category IDs
  // Boolean filters
  hasPendingPrediction: z.coerce.boolean().optional(),
  // Sort
  sortBy: z.enum(['date', 'amount', 'merchant', 'category']).optional().default('date'),
  sortOrder: z.enum(['asc', 'desc']).optional().default('desc'),
})

export const Route = createFileRoute('/_authenticated/transactions/')({
  validateSearch: transactionSearchSchema,
  component: TransactionsPage,
})

function TransactionsPage() {
  const search = Route.useSearch()

  // Ensure search params are valid strings, not empty objects (defensive against malformed URL state)
  const safeView = (typeof search.view === 'string' && (search.view === 'transactions' || search.view === 'patterns'))
    ? search.view
    : 'transactions';
  const safeSortBy = (typeof search.sortBy === 'string' && ['date', 'amount', 'merchant', 'category'].includes(search.sortBy))
    ? search.sortBy as 'date' | 'amount' | 'merchant' | 'category'
    : 'date';
  const safeSortOrder = (typeof search.sortOrder === 'string' && (search.sortOrder === 'asc' || search.sortOrder === 'desc'))
    ? search.sortOrder
    : 'desc';

  const navigate = Route.useNavigate()

  // Parse comma-separated URL params to arrays
  const parseCommaSeparated = (value: string | undefined): string[] =>
    value ? value.split(',').filter(Boolean) : []

  // Initialize filters from URL params (fully persisted)
  const [filters, setFilters] = useState<TransactionFilters>(() => ({
    ...DEFAULT_TRANSACTION_FILTERS,
    search: typeof search.search === 'string' ? search.search : '',
    dateRange: {
      start: search.startDate ? new Date(search.startDate) : null,
      end: search.endDate ? new Date(search.endDate) : null,
    },
    amountRange: {
      min: typeof search.minAmount === 'number' ? search.minAmount : null,
      max: typeof search.maxAmount === 'number' ? search.maxAmount : null,
    },
    matchStatus: parseCommaSeparated(search.matchStatus) as ('matched' | 'pending' | 'unmatched' | 'manual')[],
    categories: parseCommaSeparated(search.categories),
    hasPendingPrediction: search.hasPendingPrediction ?? false,
  }))

  // Sort state - use defensive safe values
  const [sort, setSort] = useState<TransactionSortConfig>({
    field: safeSortBy,
    direction: safeSortOrder,
  })

  // Selection state
  const [selection, setSelection] = useState<TransactionSelectionState>(
    DEFAULT_TRANSACTION_SELECTION
  )

  // Track which transactions are currently being saved
  const [savingIds, setSavingIds] = useState<Set<string>>(new Set())

  // Transaction grouping state (Feature 028)
  const [showCreateGroupDialog, setShowCreateGroupDialog] = useState(false)
  const [expandedGroupIds, setExpandedGroupIds] = useState<Set<string>>(new Set())

  // =========================================================================
  // Pattern State
  // =========================================================================

  // Pattern filter state
  const [patternFilters, setPatternFilters] = useState<PatternGridFilters>(
    DEFAULT_PATTERN_GRID_FILTERS
  )

  // Pattern sort state
  const [patternSort, setPatternSort] = useState<PatternSortConfig>(
    DEFAULT_PATTERN_SORT
  )

  // Pattern selection state
  const [patternSelection, setPatternSelection] = useState<PatternSelectionState>(
    DEFAULT_PATTERN_SELECTION
  )

  // Pattern expanded rows
  const [patternExpandedIds, setPatternExpandedIds] = useState<Set<string>>(new Set())

  // Handle view tab change
  const handleViewChange = useCallback(
    (newView: string) => {
      navigate({
        search: {
          ...search,
          view: newView as 'transactions' | 'patterns',
          page: 1, // Reset pagination when switching views
        },
      })
    },
    [search, navigate]
  )

  // Data fetching - use mixed list to include both transactions and groups
  // All filter params are now passed for comprehensive server-side filtering
  const {
    data: mixedListData,
    isLoading,
    error,
  } = useMixedTransactionList({
    page: search.page,
    pageSize: search.pageSize,
    // Date range
    startDate: filters.dateRange.start?.toISOString().split('T')[0],
    endDate: filters.dateRange.end?.toISOString().split('T')[0],
    // Text search
    search: filters.search || undefined,
    // Amount range
    minAmount: filters.amountRange.min ?? undefined,
    maxAmount: filters.amountRange.max ?? undefined,
    // Multi-select filters
    matchStatus: filters.matchStatus.length > 0 ? filters.matchStatus : undefined,
    reimbursability: filters.reimbursability.length > 0 ? filters.reimbursability : undefined,
    categories: filters.categories.length > 0 ? filters.categories : undefined,
    // Boolean filters
    hasPendingPrediction: filters.hasPendingPrediction || undefined,
    // Sort
    sortBy: sort.field,
    sortOrder: sort.direction,
  })

  // Reference data
  const { data: categories = [] } = useTransactionCategories()
  const { data: tags = [] } = useTransactionTags()

  // Pattern data (only fetch when on patterns tab)
  const patternWorkspace = usePatternWorkspace({
    includeSuppressed: patternFilters.includeSuppressed,
    search: patternFilters.search,
    status: patternFilters.status,
    category: patternFilters.category ?? undefined, // Convert null to undefined
    sortBy: patternSort.field,
    sortOrder: patternSort.direction,
    page: search.view === 'patterns' ? search.page : 1,
    pageSize: search.pageSize,
    enabled: search.view === 'patterns',
  })

  // Prediction generation mutation
  const generatePredictions = useGeneratePredictions()

  // Extract unique categories from patterns for filter dropdown
  // Filters out empty objects {} which are truthy but can't be rendered
  const patternCategories = useMemo(() => {
    const cats = new Set<string>()
    patternWorkspace.patterns.forEach((p) => {
      // Guard against empty objects {} - they are truthy but not valid strings
      if (p.category && typeof p.category === 'string' && p.category.length > 0) {
        cats.add(p.category)
      }
    })
    return Array.from(cats).sort()
  }, [patternWorkspace.patterns])

  // Mutations
  const updateTransaction = useUpdateTransaction()
  const bulkUpdate = useBulkUpdateTransactions()
  const bulkDelete = useBulkDeleteTransactions()
  const exportTransactions = useExportTransactions()

  // Reimbursability mutations
  const markReimbursable = useMarkTransactionReimbursable()
  const markNotReimbursable = useMarkTransactionNotReimbursable()
  const clearReimbursabilityOverride = useClearReimbursabilityOverride()
  const bulkMarkReimbursability = useBulkMarkReimbursability()

  // Transaction grouping mutations (Feature 028)
  const createGroup = useCreateTransactionGroup()
  const updateGroup = useUpdateTransactionGroup()
  const deleteGroup = useDeleteTransactionGroup()
  const removeFromGroup = useRemoveTransactionFromGroup()
  const selectedTransactionIds = useMemo(
    () => Array.from(selection.selectedIds),
    [selection.selectedIds]
  )
  const { canGroup } = useCanGroupTransactions(selectedTransactionIds)

  // Handle filter changes - sync all filters to URL for persistence
  const handleFilterChange = useCallback(
    (newFilters: TransactionFilters) => {
      setFilters(newFilters)
      // Sync all filter params to URL (enables bookmarking and sharing)
      navigate({
        search: {
          ...search,
          // Text search
          search: newFilters.search || undefined,
          // Date range
          startDate: newFilters.dateRange.start?.toISOString().split('T')[0] || undefined,
          endDate: newFilters.dateRange.end?.toISOString().split('T')[0] || undefined,
          // Amount range
          minAmount: newFilters.amountRange.min ?? undefined,
          maxAmount: newFilters.amountRange.max ?? undefined,
          // Multi-select filters (comma-separated)
          matchStatus: newFilters.matchStatus.length > 0 ? newFilters.matchStatus.join(',') : undefined,
          categories: newFilters.categories.length > 0 ? newFilters.categories.join(',') : undefined,
          // Boolean filters
          hasPendingPrediction: newFilters.hasPendingPrediction || undefined,
          // Reset pagination on filter change
          page: 1,
        },
      })
    },
    [search, navigate]
  )

  // Handle filter reset - clears all filter params from URL
  const handleFilterReset = useCallback(() => {
    setFilters(DEFAULT_TRANSACTION_FILTERS)
    navigate({
      search: {
        view: search.view,
        page: 1,
        pageSize: search.pageSize,
        sortBy: search.sortBy,
        sortOrder: search.sortOrder,
        // Explicitly clear all filter params
        search: undefined,
        startDate: undefined,
        endDate: undefined,
        minAmount: undefined,
        maxAmount: undefined,
        matchStatus: undefined,
        categories: undefined,
        hasPendingPrediction: undefined,
      },
    })
  }, [search.view, search.pageSize, search.sortBy, search.sortOrder, navigate])

  // Handle sort changes
  const handleSortChange = useCallback(
    (newSort: TransactionSortConfig) => {
      setSort(newSort)
      navigate({
        search: {
          ...search,
          sortBy: newSort.field,
          sortOrder: newSort.direction,
        },
      })
    },
    [search, navigate]
  )

  // Handle inline edit
  const handleTransactionEdit = useCallback(
    (id: string, updates: Partial<TransactionView>) => {
      setSavingIds((prev) => new Set(prev).add(id))

      updateTransaction.mutate(
        {
          id,
          updates: {
            category: updates.categoryId,
            notes: updates.notes,
            tags: updates.tags,
          },
        },
        {
          onSettled: () => {
            setSavingIds((prev) => {
              const next = new Set(prev)
              next.delete(id)
              return next
            })
          },
          onError: (error) => {
            toast.error(`Failed to update: ${error.message}`)
          },
        }
      )
    },
    [updateTransaction]
  )

  // Handle transaction click (navigate to detail)
  const handleTransactionClick = useCallback(
    (transaction: TransactionView) => {
      navigate({
        to: '/transactions/$transactionId',
        params: { transactionId: transaction.id },
      })
    },
    [navigate]
  )

  // Reimbursability handlers
  const handleMarkReimbursable = useCallback(
    (transactionId: string) => {
      markReimbursable.mutate(transactionId, {
        onSuccess: () => {
          toast.success('Transaction marked as reimbursable')
        },
        onError: (error) => {
          toast.error(`Failed: ${error.message}`)
        },
      })
    },
    [markReimbursable]
  )

  const handleMarkNotReimbursable = useCallback(
    (transactionId: string) => {
      markNotReimbursable.mutate(transactionId, {
        onSuccess: () => {
          toast.success('Transaction marked as not reimbursable')
        },
        onError: (error) => {
          toast.error(`Failed: ${error.message}`)
        },
      })
    },
    [markNotReimbursable]
  )

  const handleClearReimbursabilityOverride = useCallback(
    (transactionId: string) => {
      clearReimbursabilityOverride.mutate(transactionId, {
        onSuccess: () => {
          toast.success('Manual override cleared')
        },
        onError: (error) => {
          toast.error(`Failed: ${error.message}`)
        },
      })
    },
    [clearReimbursabilityOverride]
  )

  // Bulk actions
  const handleBulkCategorize = useCallback(
    (categoryId: string) => {
      const ids = Array.from(selection.selectedIds)
      bulkUpdate.mutate(
        { ids, updates: { category: categoryId } },
        {
          onSuccess: () => {
            toast.success(`Updated ${ids.length} transactions`)
            setSelection(DEFAULT_TRANSACTION_SELECTION)
          },
          onError: (error) => {
            toast.error(`Failed to update: ${error.message}`)
          },
        }
      )
    },
    [selection.selectedIds, bulkUpdate]
  )

  const handleBulkTag = useCallback(
    (newTags: string[]) => {
      const ids = Array.from(selection.selectedIds)
      bulkUpdate.mutate(
        { ids, updates: { tags: newTags } },
        {
          onSuccess: () => {
            toast.success(`Added tags to ${ids.length} transactions`)
            setSelection(DEFAULT_TRANSACTION_SELECTION)
          },
          onError: (error) => {
            toast.error(`Failed to add tags: ${error.message}`)
          },
        }
      )
    },
    [selection.selectedIds, bulkUpdate]
  )

  const handleBulkExport = useCallback(() => {
    const ids = Array.from(selection.selectedIds)
    exportTransactions.mutate(
      { format: 'csv', ids },
      {
        onSuccess: () => {
          toast.success('Export started')
        },
        onError: (error) => {
          toast.error(`Export failed: ${error.message}`)
        },
      }
    )
  }, [selection.selectedIds, exportTransactions])

  const handleBulkDelete = useCallback(() => {
    const ids = Array.from(selection.selectedIds)
    bulkDelete.mutate(ids, {
      onSuccess: () => {
        toast.success(`Deleted ${ids.length} transactions`)
        setSelection(DEFAULT_TRANSACTION_SELECTION)
      },
      onError: (error) => {
        toast.error(`Delete failed: ${error.message}`)
      },
    })
  }, [selection.selectedIds, bulkDelete])

  const handleBulkMarkReimbursable = useCallback(() => {
    const ids = Array.from(selection.selectedIds)
    bulkMarkReimbursability.mutate(
      { transactionIds: ids, isReimbursable: true },
      {
        onSuccess: (data) => {
          toast.success(data.message)
          setSelection(DEFAULT_TRANSACTION_SELECTION)
        },
        onError: (error) => {
          toast.error(`Failed to mark as reimbursable: ${error.message}`)
        },
      }
    )
  }, [selection.selectedIds, bulkMarkReimbursability])

  const handleBulkMarkNotReimbursable = useCallback(() => {
    const ids = Array.from(selection.selectedIds)
    bulkMarkReimbursability.mutate(
      { transactionIds: ids, isReimbursable: false },
      {
        onSuccess: (data) => {
          toast.success(data.message)
          setSelection(DEFAULT_TRANSACTION_SELECTION)
        },
        onError: (error) => {
          toast.error(`Failed to mark as not reimbursable: ${error.message}`)
        },
      }
    )
  }, [selection.selectedIds, bulkMarkReimbursability])

  const handleClearSelection = useCallback(() => {
    setSelection(DEFAULT_TRANSACTION_SELECTION)
  }, [])

  // Transaction grouping handlers (Feature 028)
  const handleOpenGroupDialog = useCallback(() => {
    setShowCreateGroupDialog(true)
  }, [])

  const handleCloseGroupDialog = useCallback(() => {
    setShowCreateGroupDialog(false)
  }, [])

  const handleCreateGroup = useCallback(
    (name: string, dateOverride?: Date) => {
      const transactionIds = Array.from(selection.selectedIds)
      createGroup.mutate(
        {
          transactionIds,
          name,
          displayDateOverride: dateOverride?.toISOString().split('T')[0],
        },
        {
          onSuccess: (group) => {
            toast.success(`Created group "${group.name}"`, {
              description: `${group.transactionCount} transactions grouped`,
            })
            setShowCreateGroupDialog(false)
            setSelection(DEFAULT_TRANSACTION_SELECTION)
          },
          onError: (error) => {
            toast.error(`Failed to create group: ${error.message}`)
          },
        }
      )
    },
    [selection.selectedIds, createGroup]
  )

  // Group expansion toggle handler
  const handleGroupToggle = useCallback((groupId: string) => {
    setExpandedGroupIds((prev) => {
      const next = new Set(prev)
      if (next.has(groupId)) {
        next.delete(groupId)
      } else {
        next.add(groupId)
      }
      return next
    })
  }, [])

  // Group name edit handler
  const handleGroupEditName = useCallback(
    (groupId: string, name: string) => {
      updateGroup.mutate(
        { id: groupId, updates: { name } },
        {
          onError: (error) => {
            toast.error(`Failed to update group: ${error.message}`)
          },
        }
      )
    },
    [updateGroup]
  )

  // Group date edit handler
  const handleGroupEditDate = useCallback(
    (groupId: string, date: Date) => {
      updateGroup.mutate(
        { id: groupId, updates: { displayDate: date.toISOString().split('T')[0] } },
        {
          onError: (error) => {
            toast.error(`Failed to update group date: ${error.message}`)
          },
        }
      )
    },
    [updateGroup]
  )

  // Remove transaction from group handler
  const handleGroupRemoveTransaction = useCallback(
    (groupId: string, transactionId: string) => {
      removeFromGroup.mutate(
        { groupId, transactionId },
        {
          onError: (error) => {
            toast.error(`Failed to remove transaction: ${error.message}`)
          },
        }
      )
    },
    [removeFromGroup]
  )

  // Delete (ungroup) entire group handler
  const handleGroupDelete = useCallback(
    (groupId: string) => {
      deleteGroup.mutate(groupId, {
        onSuccess: () => {
          toast.success('Group disbanded successfully')
          // Clear any expanded state for this group
          setExpandedGroupIds((prev) => {
            const next = new Set(prev)
            next.delete(groupId)
            return next
          })
        },
        onError: (error) => {
          toast.error(`Failed to ungroup: ${error.message}`)
        },
      })
    },
    [deleteGroup]
  )

  // Processing state for bulk actions
  const isProcessing =
    bulkUpdate.isPending ||
    bulkDelete.isPending ||
    exportTransactions.isPending ||
    bulkMarkReimbursability.isPending ||
    createGroup.isPending

  // Processing state for group operations
  const isGroupProcessing =
    updateGroup.isPending ||
    deleteGroup.isPending ||
    removeFromGroup.isPending

  // =========================================================================
  // Pattern Handlers
  // =========================================================================

  const handlePatternFilterChange = useCallback(
    (newFilters: PatternGridFilters) => {
      setPatternFilters(newFilters)
    },
    []
  )

  const handlePatternSortChange = useCallback(
    (newSort: PatternSortConfig) => {
      setPatternSort(newSort)
    },
    []
  )

  const handlePatternClearSelection = useCallback(() => {
    setPatternSelection(DEFAULT_PATTERN_SELECTION)
  }, [])

  const handlePatternBulkSuppress = useCallback(() => {
    const ids = Array.from(patternSelection.selectedIds)
    patternWorkspace.bulkSuppress(ids)
    setPatternSelection(DEFAULT_PATTERN_SELECTION)
  }, [patternSelection.selectedIds, patternWorkspace])

  const handlePatternBulkEnable = useCallback(() => {
    const ids = Array.from(patternSelection.selectedIds)
    patternWorkspace.bulkEnable(ids)
    setPatternSelection(DEFAULT_PATTERN_SELECTION)
  }, [patternSelection.selectedIds, patternWorkspace])

  const handlePatternBulkDelete = useCallback(() => {
    const ids = Array.from(patternSelection.selectedIds)
    patternWorkspace.bulkDelete(ids)
    setPatternSelection(DEFAULT_PATTERN_SELECTION)
  }, [patternSelection.selectedIds, patternWorkspace])

  const handlePatternRebuild = useCallback(async () => {
    try {
      await patternWorkspace.rebuild()
      toast.success('Pattern rebuild started')
    } catch {
      toast.error('Failed to rebuild patterns')
    }
  }, [patternWorkspace])

  const handleGeneratePredictions = useCallback(async () => {
    try {
      const count = await generatePredictions.mutateAsync()
      if (count === 0) {
        toast.info('No new transactions to process', {
          description: 'All transactions already have predictions.',
        })
      } else {
        toast.success(`Generated ${count} prediction${count !== 1 ? 's' : ''}`, {
          description: 'View the Transactions tab and use the Reimbursable filter to see flagged expenses.',
        })
      }
    } catch {
      toast.error('Failed to generate predictions', {
        description: 'Please try again or contact support if the issue persists.',
      })
    }
  }, [generatePredictions])

  // Mixed list items for grid (transactions and groups)
  const items: TransactionListItem[] = useMemo(
    () => mixedListData?.items || [],
    [mixedListData]
  )

  // Extract just transactions for legacy compatibility and group dialog
  const transactions: TransactionView[] = useMemo(
    () => items.filter((item): item is TransactionView & { type: 'transaction' } =>
      item.type === 'transaction'
    ),
    [items]
  )

  // Get selected transactions for the group dialog (Feature 028)
  // Only transactions can be selected for grouping, not existing groups
  const selectedTransactions = useMemo(
    () => transactions.filter((t) => selection.selectedIds.has(t.id)),
    [transactions, selection.selectedIds]
  )

  // Compute selection composition for smart bulk action disabling
  const selectionComposition = useMemo(
    () => computeSelectionComposition(items, selection.selectedIds),
    [items, selection.selectedIds]
  )

  return (
    <div className="space-y-6 w-full max-w-full overflow-x-hidden">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Transactions</h1>
          <p className="text-muted-foreground">
            View, filter, and manage your imported transactions
          </p>
        </div>
        <Button asChild>
          <Link to="/statements">
            <Upload className="mr-2 h-4 w-4" />
            Import Statement
          </Link>
        </Button>
      </div>

      {/* View Toggle Tabs */}
      <Tabs value={safeView} onValueChange={handleViewChange} className="space-y-6">
        <TabsList>
          <TabsTrigger value="transactions" className="gap-2">
            <ListFilter className="h-4 w-4" />
            Transactions
          </TabsTrigger>
          <TabsTrigger value="patterns" className="gap-2">
            <Sparkles className="h-4 w-4" />
            Patterns
          </TabsTrigger>
        </TabsList>

        {/* Transactions View */}
        <TabsContent value="transactions" className="space-y-6 mt-0">
          {/* Filter Panel */}
          <TransactionFilterPanel
            filters={filters}
            categories={categories}
            tags={tags}
            onChange={handleFilterChange}
            onReset={handleFilterReset}
          />

          {/* Error State */}
          {error && (
            <Card className="border-destructive">
              <CardContent className="pt-6">
                <p className="text-destructive">
                  Failed to load transactions. Please try again.
                </p>
              </CardContent>
            </Card>
          )}

          {/* Transaction Grid */}
          <TransactionGrid
            items={items}
            isLoading={isLoading}
            sort={sort}
            selection={selection}
            categories={categories}
            onSortChange={handleSortChange}
            onSelectionChange={setSelection}
            onTransactionEdit={handleTransactionEdit}
            onTransactionClick={handleTransactionClick}
            savingIds={savingIds}
            containerHeight={Math.min(600, Math.max(400, items.length * 56))}
            onMarkReimbursable={handleMarkReimbursable}
            onMarkNotReimbursable={handleMarkNotReimbursable}
            onClearReimbursabilityOverride={handleClearReimbursabilityOverride}
            isPredictionProcessing={
              markReimbursable.isPending ||
              markNotReimbursable.isPending ||
              clearReimbursabilityOverride.isPending
            }
            // Group-specific props (Feature 028)
            expandedGroupIds={expandedGroupIds}
            onGroupToggle={handleGroupToggle}
            onGroupEditName={handleGroupEditName}
            onGroupEditDate={handleGroupEditDate}
            onGroupRemoveTransaction={handleGroupRemoveTransaction}
            onGroupDelete={handleGroupDelete}
            isGroupProcessing={isGroupProcessing}
          />

          {/* Pagination Info */}
          {mixedListData && mixedListData.totalCount > 0 && (
            <div className="flex items-center justify-between text-sm text-muted-foreground">
              <p>
                Showing {((search.page - 1) * search.pageSize) + 1} to{' '}
                {Math.min(search.page * search.pageSize, mixedListData.totalCount)} of{' '}
                {mixedListData.totalCount} items
              </p>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={search.page <= 1}
                  onClick={() =>
                    navigate({
                      search: { ...search, page: search.page - 1 },
                    })
                  }
                >
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={
                    search.page >= Math.ceil(mixedListData.totalCount / search.pageSize)
                  }
                  onClick={() =>
                    navigate({
                      search: { ...search, page: search.page + 1 },
                    })
                  }
                >
                  Next
                </Button>
              </div>
            </div>
          )}

          {/* Bulk Actions Bar */}
          <BulkActionsBar
            selectedCount={selection.selectedIds.size}
            categories={categories}
            availableTags={tags}
            onCategorize={handleBulkCategorize}
            onTag={handleBulkTag}
            onExport={handleBulkExport}
            onDelete={handleBulkDelete}
            onClearSelection={handleClearSelection}
            onMarkReimbursable={handleBulkMarkReimbursable}
            onMarkNotReimbursable={handleBulkMarkNotReimbursable}
            isProcessing={isProcessing}
            onGroup={handleOpenGroupDialog}
            canGroup={canGroup && (selectionComposition?.hasOnlyTransactions ?? false)}
            selectionComposition={selectionComposition}
          />

          {/* Create Group Dialog (Feature 028) */}
          <CreateGroupDialog
            open={showCreateGroupDialog}
            onOpenChange={handleCloseGroupDialog}
            transactions={selectedTransactions}
            onCreateGroup={handleCreateGroup}
            isCreating={createGroup.isPending}
          />
        </TabsContent>

        {/* Patterns View */}
        <TabsContent value="patterns" className="space-y-6 mt-0">
          {/* Pattern Stats */}
          <PatternStats
            activeCount={patternWorkspace.patterns.filter(p => !p.isSuppressed).length}
            suppressedCount={patternWorkspace.patterns.filter(p => p.isSuppressed).length}
            overallAccuracyRate={
              patternWorkspace.patterns.length > 0
                ? patternWorkspace.patterns.reduce((sum, p) => sum + p.accuracyRate, 0) /
                  patternWorkspace.patterns.length
                : 0
            }
            isLoading={patternWorkspace.isLoading}
            isRebuilding={patternWorkspace.isRebuilding}
            onRebuild={handlePatternRebuild}
            isGenerating={generatePredictions.isPending}
            onGenerate={handleGeneratePredictions}
          />

          {/* Pattern Filters */}
          <PatternFilterPanel
            filters={patternFilters}
            categories={patternCategories}
            onFiltersChange={handlePatternFilterChange}
          />

          {/* Pattern Error State */}
          {patternWorkspace.error && (
            <Card className="border-destructive">
              <CardContent className="pt-6">
                <p className="text-destructive">
                  Failed to load patterns. Please try again.
                </p>
              </CardContent>
            </Card>
          )}

          {/* Pattern Grid */}
          <PatternGrid
            patterns={patternWorkspace.patterns}
            isLoading={patternWorkspace.isLoading}
            sort={patternSort}
            selection={patternSelection}
            expandedIds={patternExpandedIds}
            onSortChange={handlePatternSortChange}
            onSelectionChange={setPatternSelection}
            onExpandedChange={setPatternExpandedIds}
            onToggleSuppression={patternWorkspace.toggleSuppression}
            onToggleReceiptMatch={patternWorkspace.toggleReceiptMatch}
            onDelete={patternWorkspace.deletePattern}
            isProcessing={patternWorkspace.isProcessing}
            containerHeight={Math.min(600, Math.max(400, patternWorkspace.patterns.length * 56))}
          />

          {/* Pattern Pagination */}
          {patternWorkspace.totalCount > 0 && (
            <div className="flex items-center justify-between text-sm text-muted-foreground">
              <p>
                Showing {((search.page - 1) * search.pageSize) + 1} to{' '}
                {Math.min(search.page * search.pageSize, patternWorkspace.totalCount)} of{' '}
                {patternWorkspace.totalCount} patterns
              </p>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={search.page <= 1}
                  onClick={() =>
                    navigate({
                      search: { ...search, page: search.page - 1 },
                    })
                  }
                >
                  Previous
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={
                    search.page >= Math.ceil(patternWorkspace.totalCount / search.pageSize)
                  }
                  onClick={() =>
                    navigate({
                      search: { ...search, page: search.page + 1 },
                    })
                  }
                >
                  Next
                </Button>
              </div>
            </div>
          )}

          {/* Bulk Pattern Actions */}
          <BulkPatternActions
            selectedCount={patternSelection.selectedIds.size}
            onSuppress={handlePatternBulkSuppress}
            onEnable={handlePatternBulkEnable}
            onDelete={handlePatternBulkDelete}
            onClearSelection={handlePatternClearSelection}
            isProcessing={patternWorkspace.isProcessing}
          />
        </TabsContent>
      </Tabs>
    </div>
  )
}

