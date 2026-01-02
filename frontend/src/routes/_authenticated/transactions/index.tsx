"use client"

/**
 * Transactions Page (T058)
 *
 * Main transaction explorer with:
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
  useTransactionListWithFilters,
  useTransactionCategories,
  useTransactionTags,
  useUpdateTransaction,
  useBulkUpdateTransactions,
  useBulkDeleteTransactions,
  useExportTransactions,
} from '@/hooks/queries/use-transactions'
import { Link } from '@tanstack/react-router'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { toast } from 'sonner'
import { Upload } from 'lucide-react'
import { TransactionFilterPanel } from '@/components/transactions/transaction-filter-panel'
import { TransactionGrid } from '@/components/transactions/transaction-grid'
import { BulkActionsBar } from '@/components/transactions/bulk-actions-bar'
import {
  DEFAULT_TRANSACTION_FILTERS,
  DEFAULT_TRANSACTION_SELECTION,
} from '@/types/transaction'
import type {
  TransactionView,
  TransactionFilters,
  TransactionSortConfig,
  TransactionSelectionState,
} from '@/types/transaction'

// Search params schema for URL state
const transactionSearchSchema = z.object({
  page: z.coerce.number().optional().default(1),
  pageSize: z.coerce.number().optional().default(50),
  search: z.string().optional(),
  startDate: z.string().optional(),
  endDate: z.string().optional(),
  sortBy: z.enum(['date', 'amount', 'merchant', 'category']).optional().default('date'),
  sortOrder: z.enum(['asc', 'desc']).optional().default('desc'),
})

export const Route = createFileRoute('/_authenticated/transactions/')({
  validateSearch: transactionSearchSchema,
  component: TransactionsPage,
})

function TransactionsPage() {
  const search = Route.useSearch()
  const navigate = Route.useNavigate()

  // Filter state (local, not persisted in URL for simplicity)
  const [filters, setFilters] = useState<TransactionFilters>({
    ...DEFAULT_TRANSACTION_FILTERS,
    search: search.search || '',
    dateRange: {
      start: search.startDate ? new Date(search.startDate) : null,
      end: search.endDate ? new Date(search.endDate) : null,
    },
  })

  // Sort state
  const [sort, setSort] = useState<TransactionSortConfig>({
    field: search.sortBy || 'date',
    direction: search.sortOrder || 'desc',
  })

  // Selection state
  const [selection, setSelection] = useState<TransactionSelectionState>(
    DEFAULT_TRANSACTION_SELECTION
  )

  // Track which transactions are currently being saved
  const [savingIds, setSavingIds] = useState<Set<string>>(new Set())

  // Data fetching
  const {
    data: transactionData,
    isLoading,
    error,
  } = useTransactionListWithFilters({
    filters,
    sort,
    page: search.page,
    pageSize: search.pageSize,
  })

  // Reference data
  const { data: categories = [] } = useTransactionCategories()
  const { data: tags = [] } = useTransactionTags()

  // Mutations
  const updateTransaction = useUpdateTransaction()
  const bulkUpdate = useBulkUpdateTransactions()
  const bulkDelete = useBulkDeleteTransactions()
  const exportTransactions = useExportTransactions()

  // Handle filter changes
  const handleFilterChange = useCallback(
    (newFilters: TransactionFilters) => {
      setFilters(newFilters)
      // Sync search to URL
      navigate({
        search: {
          ...search,
          search: newFilters.search || undefined,
          startDate: newFilters.dateRange.start?.toISOString().split('T')[0] || undefined,
          endDate: newFilters.dateRange.end?.toISOString().split('T')[0] || undefined,
          page: 1, // Reset to first page on filter change
        },
      })
    },
    [search, navigate]
  )

  // Handle filter reset
  const handleFilterReset = useCallback(() => {
    setFilters(DEFAULT_TRANSACTION_FILTERS)
    navigate({
      search: {
        page: 1,
        pageSize: search.pageSize,
        sortBy: search.sortBy,
        sortOrder: search.sortOrder,
      },
    })
  }, [search.pageSize, search.sortBy, search.sortOrder, navigate])

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

  const handleClearSelection = useCallback(() => {
    setSelection(DEFAULT_TRANSACTION_SELECTION)
  }, [])

  // Processing state for bulk actions
  const isProcessing =
    bulkUpdate.isPending || bulkDelete.isPending || exportTransactions.isPending

  // Transform transaction list for grid
  const transactions = useMemo(
    () => transactionData?.transactions || [],
    [transactionData]
  )

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
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
        transactions={transactions}
        isLoading={isLoading}
        sort={sort}
        selection={selection}
        categories={categories}
        onSortChange={handleSortChange}
        onSelectionChange={setSelection}
        onTransactionEdit={handleTransactionEdit}
        onTransactionClick={handleTransactionClick}
        savingIds={savingIds}
        containerHeight={Math.min(600, Math.max(400, transactions.length * 56))}
      />

      {/* Pagination Info */}
      {transactionData && transactionData.totalCount > 0 && (
        <div className="flex items-center justify-between text-sm text-muted-foreground">
          <p>
            Showing {((search.page - 1) * search.pageSize) + 1} to{' '}
            {Math.min(search.page * search.pageSize, transactionData.totalCount)} of{' '}
            {transactionData.totalCount} transactions
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
                search.page >= Math.ceil(transactionData.totalCount / search.pageSize)
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
        isProcessing={isProcessing}
      />
    </div>
  )
}

