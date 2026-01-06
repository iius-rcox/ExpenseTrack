'use client'

/**
 * Missing Receipts Page (T016, T030, T033, T034)
 *
 * Full list view of transactions marked as reimbursable but lacking matched receipts.
 * Features:
 * - Paginated list with 25 items per page
 * - Sorting by date, amount, or vendor
 * - Toggle to show/hide dismissed items
 * - Actions: Add URL, Upload, Dismiss/Restore
 * - Bulk selection and actions
 * - Undo toast for dismiss actions
 * - Framer Motion animations
 * - ARIA live regions for accessibility
 *
 * Part of Feature 026: Missing Receipts UI
 */

import { useState, useCallback, useRef, useEffect } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { z } from 'zod'
import { motion, AnimatePresence } from 'framer-motion'
import { useMissingReceiptsWorkspace } from '@/hooks/queries/use-missing-receipts'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Checkbox } from '@/components/ui/checkbox'
import { Switch } from '@/components/ui/switch'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  MissingReceiptCard,
  MissingReceiptCardSkeleton,
} from '@/components/missing-receipts/missing-receipt-card'
import { EmptyMissingReceipts } from '@/components/missing-receipts/missing-receipts-empty'
import { ReceiptUrlDialog } from '@/components/missing-receipts/receipt-url-dialog'
import { DismissConfirmDialog } from '@/components/missing-receipts/dismiss-confirm-dialog'
import { MissingReceiptsErrorBoundary } from '@/components/missing-receipts/missing-receipts-error-boundary'
import { toast } from 'sonner'
import {
  Receipt,
  ChevronLeft,
  ChevronRight,
  RefreshCcw,
  ArrowUpDown,
  Eye,
  EyeOff,
  X,
  CheckSquare,
} from 'lucide-react'

// URL search params schema
const missingReceiptsSearchSchema = z.object({
  page: z.coerce.number().optional().default(1),
  pageSize: z.coerce.number().optional().default(25),
  sortBy: z.enum(['date', 'amount', 'vendor']).optional().default('date'),
  sortOrder: z.enum(['asc', 'desc']).optional().default('desc'),
  includeDismissed: z.coerce.boolean().optional().default(false),
})

export const Route = createFileRoute('/_authenticated/missing-receipts/')({
  validateSearch: missingReceiptsSearchSchema,
  component: MissingReceiptsPage,
})

// Animation variants for list items
const itemVariants = {
  initial: { opacity: 0, y: 20 },
  animate: { opacity: 1, y: 0 },
  exit: { opacity: 0, x: -20, transition: { duration: 0.2 } },
}

function MissingReceiptsPage() {
  return (
    <MissingReceiptsErrorBoundary>
      <MissingReceiptsContent />
    </MissingReceiptsErrorBoundary>
  )
}

function MissingReceiptsContent() {
  const search = Route.useSearch()
  const navigate = Route.useNavigate()

  // Dialog states
  const [urlDialogOpen, setUrlDialogOpen] = useState(false)
  const [dismissDialogOpen, setDismissDialogOpen] = useState(false)
  const [selectedTransaction, setSelectedTransaction] = useState<{
    id: string
    description: string
    receiptUrl: string | null
  } | null>(null)

  // Bulk selection state
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [selectionMode, setSelectionMode] = useState(false)

  // Ref for focus management
  const listRef = useRef<HTMLDivElement>(null)

  const {
    items,
    totalCount,
    page,
    pageSize,
    totalPages,
    isLoading,
    isError,
    error,
    updateReceiptUrl,
    dismissReceipt,
    restoreReceipt,
    dismissMultiple,
    isProcessing,
    isUpdatingUrl,
    refetch,
  } = useMissingReceiptsWorkspace({
    page: search.page,
    pageSize: search.pageSize,
    sortBy: search.sortBy,
    sortOrder: search.sortOrder,
    includeDismissed: search.includeDismissed,
  })

  // Clear selection when items change (e.g., after dismiss)
  useEffect(() => {
    setSelectedIds(new Set())
  }, [items.length])

  const handleSortChange = (value: string) => {
    const [sortBy, sortOrder] = value.split('-') as ['date' | 'amount' | 'vendor', 'asc' | 'desc']
    navigate({
      search: {
        ...search,
        sortBy,
        sortOrder,
        page: 1,
      },
    })
  }

  const handleToggleDismissed = (checked: boolean) => {
    setSelectionMode(false)
    setSelectedIds(new Set())
    navigate({
      search: {
        ...search,
        includeDismissed: checked,
        page: 1,
      },
    })
  }

  const handlePageChange = (newPage: number) => {
    setSelectedIds(new Set())
    navigate({
      search: {
        ...search,
        page: newPage,
      },
    })
  }

  const handleAddUrl = (item: typeof items[0]) => {
    setSelectedTransaction({
      id: item.transactionId,
      description: item.description,
      receiptUrl: item.receiptUrl,
    })
    setUrlDialogOpen(true)
  }

  const handleSaveUrl = (url: string | null) => {
    if (selectedTransaction) {
      updateReceiptUrl(selectedTransaction.id, url)
      setUrlDialogOpen(false)
      setSelectedTransaction(null)
      toast.success(url ? 'URL saved' : 'URL cleared')
    }
  }

  const handleUpload = (_transactionId: string) => {
    toast.info('Quick upload coming in next update')
  }

  const handleDismiss = (item: typeof items[0]) => {
    setSelectedTransaction({
      id: item.transactionId,
      description: item.description,
      receiptUrl: item.receiptUrl,
    })
    setDismissDialogOpen(true)
  }

  // Dismiss with undo toast
  const handleConfirmDismiss = useCallback(() => {
    if (selectedTransaction) {
      const { id, description } = selectedTransaction
      dismissReceipt(id)
      setDismissDialogOpen(false)
      setSelectedTransaction(null)

      // Show undo toast
      toast.success('Receipt dismissed', {
        description: description.slice(0, 40) + (description.length > 40 ? '...' : ''),
        action: {
          label: 'Undo',
          onClick: () => {
            restoreReceipt(id)
            toast.success('Receipt restored')
          },
        },
        duration: 5000,
      })

      // Focus management - focus the next card
      setTimeout(() => {
        const nextCard = listRef.current?.querySelector('[data-missing-receipt-card]') as HTMLElement
        nextCard?.focus()
      }, 300)
    }
  }, [selectedTransaction, dismissReceipt, restoreReceipt])

  const handleRestore = (transactionId: string) => {
    restoreReceipt(transactionId)
    toast.success('Receipt restored')
  }

  // Bulk selection handlers
  const handleSelectionChange = (transactionId: string, selected: boolean) => {
    setSelectedIds((prev) => {
      const next = new Set(prev)
      if (selected) {
        next.add(transactionId)
      } else {
        next.delete(transactionId)
      }
      return next
    })
  }

  const handleSelectAll = () => {
    if (selectedIds.size === items.length) {
      setSelectedIds(new Set())
    } else {
      setSelectedIds(new Set(items.map((item) => item.transactionId)))
    }
  }

  const handleBulkDismiss = async () => {
    const count = selectedIds.size
    const ids = Array.from(selectedIds)

    try {
      await dismissMultiple(ids)
      setSelectedIds(new Set())
      setSelectionMode(false)

      toast.success(`${count} receipt${count > 1 ? 's' : ''} dismissed`, {
        action: {
          label: 'Undo All',
          onClick: async () => {
            for (const id of ids) {
              restoreReceipt(id)
            }
            toast.success(`${count} receipt${count > 1 ? 's' : ''} restored`)
          },
        },
        duration: 5000,
      })
    } catch {
      toast.error('Failed to dismiss some receipts')
    }
  }

  const handleCancelSelection = () => {
    setSelectedIds(new Set())
    setSelectionMode(false)
  }

  const sortValue = `${search.sortBy}-${search.sortOrder}`
  const allSelected = items.length > 0 && selectedIds.size === items.length
  const someSelected = selectedIds.size > 0 && selectedIds.size < items.length

  return (
    <div className="space-y-6">
      {/* ARIA Live region for screen readers */}
      <div role="status" aria-live="polite" className="sr-only">
        {isLoading
          ? 'Loading missing receipts...'
          : `${totalCount} missing receipt${totalCount !== 1 ? 's' : ''} found`}
      </div>

      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight flex items-center gap-3">
            <Receipt className="h-8 w-8" />
            Missing Receipts
          </h1>
          <p className="text-muted-foreground mt-1">
            Transactions flagged as reimbursable but lacking matched receipts
          </p>
        </div>
        <Button
          variant="ghost"
          size="icon"
          onClick={() => refetch()}
          disabled={isLoading}
          aria-label="Refresh list"
        >
          <RefreshCcw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
        </Button>
      </div>

      {/* Bulk Selection Bar */}
      <AnimatePresence>
        {selectionMode && selectedIds.size > 0 && (
          <motion.div
            initial={{ opacity: 0, y: -10 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -10 }}
          >
            <Card className="border-primary">
              <CardContent className="p-3">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <CheckSquare className="h-5 w-5 text-primary" />
                    <span className="font-medium">
                      {selectedIds.size} item{selectedIds.size > 1 ? 's' : ''} selected
                    </span>
                  </div>
                  <div className="flex items-center gap-2">
                    <Button
                      variant="destructive"
                      size="sm"
                      onClick={handleBulkDismiss}
                      disabled={isProcessing}
                    >
                      <X className="mr-2 h-4 w-4" />
                      Dismiss Selected
                    </Button>
                    <Button variant="ghost" size="sm" onClick={handleCancelSelection}>
                      Cancel
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Filters and controls */}
      <Card>
        <CardContent className="p-4">
          <div className="flex flex-wrap items-center justify-between gap-4">
            {/* Left: Selection + Sort controls */}
            <div className="flex items-center gap-4">
              {/* Bulk selection toggle */}
              {items.length > 0 && !search.includeDismissed && (
                <div className="flex items-center gap-2">
                  {selectionMode ? (
                    <Checkbox
                      id="select-all"
                      checked={allSelected}
                      ref={(el) => {
                        if (el) {
                          ;(el as unknown as HTMLInputElement).indeterminate = someSelected
                        }
                      }}
                      onCheckedChange={handleSelectAll}
                      aria-label={allSelected ? 'Deselect all' : 'Select all'}
                    />
                  ) : (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setSelectionMode(true)}
                    >
                      Select
                    </Button>
                  )}
                </div>
              )}

              {/* Sort dropdown */}
              <div className="flex items-center gap-2">
                <ArrowUpDown className="h-4 w-4 text-muted-foreground" />
                <Select value={sortValue} onValueChange={handleSortChange}>
                  <SelectTrigger className="w-40">
                    <SelectValue placeholder="Sort by..." />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="date-desc">Newest first</SelectItem>
                    <SelectItem value="date-asc">Oldest first</SelectItem>
                    <SelectItem value="amount-desc">Highest amount</SelectItem>
                    <SelectItem value="amount-asc">Lowest amount</SelectItem>
                    <SelectItem value="vendor-asc">Vendor A-Z</SelectItem>
                    <SelectItem value="vendor-desc">Vendor Z-A</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              {/* Count badge */}
              {!isLoading && (
                <Badge variant="outline" className="font-mono">
                  {totalCount} item{totalCount !== 1 ? 's' : ''}
                </Badge>
              )}
            </div>

            {/* Right: Show dismissed toggle */}
            <div className="flex items-center gap-2">
              {search.includeDismissed ? (
                <Eye className="h-4 w-4 text-muted-foreground" />
              ) : (
                <EyeOff className="h-4 w-4 text-muted-foreground" />
              )}
              <Switch
                id="show-dismissed"
                checked={search.includeDismissed}
                onCheckedChange={handleToggleDismissed}
              />
              <Label
                htmlFor="show-dismissed"
                className="text-sm text-muted-foreground cursor-pointer"
              >
                Show dismissed
              </Label>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* List content */}
      {isError ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Receipt className="h-12 w-12 text-destructive" />
            <h3 className="mt-4 text-lg font-semibold">Failed to load</h3>
            <p className="text-sm text-muted-foreground mt-1">
              {error?.message || 'An error occurred while loading missing receipts'}
            </p>
            <Button className="mt-4" onClick={() => refetch()}>
              Try Again
            </Button>
          </CardContent>
        </Card>
      ) : isLoading ? (
        <div className="space-y-3" role="status" aria-label="Loading">
          {Array.from({ length: 5 }).map((_, i) => (
            <MissingReceiptCardSkeleton key={i} />
          ))}
        </div>
      ) : items.length === 0 ? (
        <Card>
          <CardContent className="py-6">
            {search.includeDismissed ? (
              <EmptyMissingReceipts
                showingDismissed={true}
                onUpload={() => handleToggleDismissed(false)}
              />
            ) : (
              <EmptyMissingReceipts
                showingDismissed={false}
                onUpload={() => toast.info('Upload coming in next update')}
                onImport={() => navigate({ to: '/statements' })}
              />
            )}
          </CardContent>
        </Card>
      ) : (
        <div ref={listRef} className="space-y-3" role="list">
          <AnimatePresence initial={false}>
            {items.map((item, index) => (
              <motion.div
                key={item.transactionId}
                variants={itemVariants}
                initial="initial"
                animate="animate"
                exit="exit"
                transition={{ delay: index * 0.03, duration: 0.2 }}
                layout
              >
                <MissingReceiptCard
                  item={item}
                  onAddUrl={() => handleAddUrl(item)}
                  onUpload={() => handleUpload(item.transactionId)}
                  onDismiss={() => handleDismiss(item)}
                  onRestore={() => handleRestore(item.transactionId)}
                  isProcessing={isProcessing}
                  showSelection={selectionMode && !item.isDismissed}
                  isSelected={selectedIds.has(item.transactionId)}
                  onSelectionChange={(selected) =>
                    handleSelectionChange(item.transactionId, selected)
                  }
                />
              </motion.div>
            ))}
          </AnimatePresence>
        </div>
      )}

      {/* Receipt URL Dialog */}
      <ReceiptUrlDialog
        open={urlDialogOpen}
        onOpenChange={(open) => {
          setUrlDialogOpen(open)
          if (!open) setSelectedTransaction(null)
        }}
        currentUrl={selectedTransaction?.receiptUrl ?? null}
        transactionDescription={selectedTransaction?.description ?? ''}
        onSave={handleSaveUrl}
        isSaving={isUpdatingUrl}
      />

      {/* Dismiss Confirm Dialog */}
      <DismissConfirmDialog
        open={dismissDialogOpen}
        onOpenChange={(open) => {
          setDismissDialogOpen(open)
          if (!open) setSelectedTransaction(null)
        }}
        transactionDescription={selectedTransaction?.description ?? ''}
        onConfirm={handleConfirmDismiss}
      />

      {/* Pagination */}
      {!isLoading && totalPages > 1 && (
        <nav aria-label="Pagination" className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            Showing {(page - 1) * pageSize + 1} to {Math.min(page * pageSize, totalCount)} of{' '}
            {totalCount}
          </p>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="icon"
              onClick={() => handlePageChange(page - 1)}
              disabled={page <= 1 || isLoading}
              aria-label="Previous page"
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <span className="text-sm font-medium px-2">
              Page {page} of {totalPages}
            </span>
            <Button
              variant="outline"
              size="icon"
              onClick={() => handlePageChange(page + 1)}
              disabled={page >= totalPages || isLoading}
              aria-label="Next page"
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </nav>
      )}
    </div>
  )
}
