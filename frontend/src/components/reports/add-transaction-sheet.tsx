/**
 * AddTransactionSheet - Slide-out panel for adding available transactions to a report
 *
 * Features:
 * - Searchable list with debounced input
 * - Pagination for large transaction sets
 * - Out-of-period warning indicators
 * - Receipt status indicators
 * - Loading and empty states
 */

import { useState, useEffect } from 'react'
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import {
  AlertTriangle,
  CheckCircle2,
  XCircle,
  Plus,
  Search,
  Loader2,
  Receipt,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react'
import { useAvailableTransactions } from '@/hooks/queries/use-reports'
import { cn } from '@/lib/utils'

interface AddTransactionSheetProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  reportId: string
  reportPeriod: string
  onAdd: (transactionId: string) => void
  isAdding: boolean
  addingTransactionId?: string | null
}

export function AddTransactionSheet({
  open,
  onOpenChange,
  reportId,
  reportPeriod,
  onAdd,
  isAdding,
  addingTransactionId,
}: AddTransactionSheetProps) {
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [page, setPage] = useState(1)
  const pageSize = 20

  // Debounce search input
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(search)
      setPage(1) // Reset to first page on search
    }, 300)
    return () => clearTimeout(timer)
  }, [search])

  // Reset state when sheet closes
  useEffect(() => {
    if (!open) {
      setSearch('')
      setDebouncedSearch('')
      setPage(1)
    }
  }, [open])

  const {
    data,
    isLoading,
    isFetching,
  } = useAvailableTransactions(reportId, debouncedSearch || undefined, page, pageSize, open)

  const transactions = data?.transactions ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.ceil(totalCount / pageSize) || 1

  // Format date for display
  const formatDate = (dateString: string) => {
    const date = new Date(dateString)
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
  }

  // Format currency
  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount)
  }

  // Parse period for display (e.g., "2026-01" -> "January 2026")
  const formatPeriod = (period: string) => {
    const [year, month] = period.split('-')
    const date = new Date(parseInt(year), parseInt(month) - 1)
    return date.toLocaleDateString('en-US', { month: 'long', year: 'numeric' })
  }

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full sm:max-w-lg flex flex-col">
        <SheetHeader>
          <SheetTitle>Add Transaction</SheetTitle>
          <SheetDescription>
            Select a transaction to add to the {formatPeriod(reportPeriod)} report.
            Transactions already on reports are not shown.
          </SheetDescription>
        </SheetHeader>

        {/* Search Input */}
        <div className="relative mt-4">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search transactions..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9"
            data-testid="search-transactions"
          />
          {isFetching && !isLoading && (
            <Loader2 className="absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 animate-spin text-muted-foreground" />
          )}
        </div>

        {/* Transaction List */}
        <ScrollArea className="flex-1 mt-4 -mx-6 px-6">
          {isLoading ? (
            // Loading skeletons
            <div className="space-y-3">
              {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="flex items-center gap-3 p-3 rounded-lg border">
                  <Skeleton className="h-10 w-10 rounded-full" />
                  <div className="flex-1 space-y-2">
                    <Skeleton className="h-4 w-3/4" />
                    <Skeleton className="h-3 w-1/2" />
                  </div>
                  <Skeleton className="h-8 w-8" />
                </div>
              ))}
            </div>
          ) : transactions.length === 0 ? (
            // Empty state
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <Receipt className="h-12 w-12 text-muted-foreground/50 mb-4" />
              <p className="text-muted-foreground">
                {debouncedSearch
                  ? `No transactions found matching "${debouncedSearch}"`
                  : 'No available transactions'}
              </p>
              <p className="text-sm text-muted-foreground mt-1">
                All transactions may already be on reports.
              </p>
            </div>
          ) : (
            // Transaction list
            <div className="space-y-2">
              {transactions.map((txn) => {
                const isCurrentlyAdding = isAdding && addingTransactionId === txn.id

                return (
                  <div
                    key={txn.id}
                    className={cn(
                      'flex items-start gap-3 p-3 rounded-lg border transition-colors',
                      'hover:bg-muted/50',
                      txn.isOutsidePeriod && 'border-amber-200 bg-amber-50/50 dark:border-amber-900 dark:bg-amber-950/20'
                    )}
                    data-testid={`available-transaction-${txn.id}`}
                  >
                    {/* Receipt Status Icon */}
                    <div className="flex-shrink-0 mt-0.5">
                      {txn.hasMatchedReceipt ? (
                        <CheckCircle2 className="h-5 w-5 text-green-600" />
                      ) : (
                        <XCircle className="h-5 w-5 text-muted-foreground" />
                      )}
                    </div>

                    {/* Transaction Details */}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-start justify-between gap-2">
                        <div className="min-w-0 flex-1">
                          <p className="font-medium truncate">{txn.description}</p>
                          <p className="text-sm text-muted-foreground">
                            {formatDate(txn.transactionDate)}
                            {txn.vendor && ` • ${txn.vendor}`}
                          </p>
                        </div>
                        <span className="font-semibold whitespace-nowrap">
                          {formatCurrency(txn.amount)}
                        </span>
                      </div>

                      {/* Badges */}
                      <div className="flex items-center gap-2 mt-2">
                        {txn.hasMatchedReceipt && (
                          <Badge variant="secondary" className="text-xs">
                            Has Receipt
                          </Badge>
                        )}
                        {txn.isOutsidePeriod && (
                          <Badge
                            variant="outline"
                            className="text-xs border-amber-500 text-amber-700 dark:text-amber-400"
                            data-testid="period-warning"
                          >
                            <AlertTriangle className="h-3 w-3 mr-1" />
                            Outside Period
                          </Badge>
                        )}
                      </div>
                    </div>

                    {/* Add Button */}
                    <Button
                      size="icon"
                      variant="ghost"
                      onClick={() => onAdd(txn.id)}
                      disabled={isAdding}
                      className="flex-shrink-0 h-8 w-8"
                      data-testid="add-to-report-btn"
                    >
                      {isCurrentlyAdding ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <Plus className="h-4 w-4" />
                      )}
                      <span className="sr-only">Add to report</span>
                    </Button>
                  </div>
                )
              })}
            </div>
          )}
        </ScrollArea>

        {/* Pagination */}
        {!isLoading && transactions.length > 0 && (
          <div className="flex items-center justify-between pt-4 border-t mt-4">
            <p className="text-sm text-muted-foreground">
              Showing {transactions.length} of {totalCount} transactions
            </p>
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="icon"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page === 1 || isFetching}
                className="h-8 w-8"
              >
                <ChevronLeft className="h-4 w-4" />
                <span className="sr-only">Previous page</span>
              </Button>
              <span className="text-sm text-muted-foreground min-w-[4rem] text-center">
                {page} / {totalPages}
              </span>
              <Button
                variant="outline"
                size="icon"
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page === totalPages || isFetching}
                className="h-8 w-8"
              >
                <ChevronRight className="h-4 w-4" />
                <span className="sr-only">Next page</span>
              </Button>
            </div>
          </div>
        )}
      </SheetContent>
    </Sheet>
  )
}
