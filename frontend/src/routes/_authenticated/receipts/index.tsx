"use client"

import { useState, useCallback, useMemo } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { z } from 'zod'
import { useReceiptList, useReceiptStatusCounts } from '@/hooks/queries/use-receipts'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { ReceiptCard, ReceiptCardSkeleton } from '@/components/receipts/receipt-card'
import { ReceiptUploadDropzone } from '@/components/receipts/receipt-upload-dropzone'
import { ReceiptFilterPanel, type ReceiptFilters } from '@/components/receipts/receipt-filter-panel'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { Receipt, Upload, ChevronLeft, ChevronRight } from 'lucide-react'

const receiptSearchSchema = z.object({
  status: z.string().optional(),
  matchStatus: z.enum(['unmatched', 'proposed', 'matched']).optional(),
  vendor: z.string().optional(),
  dateFrom: z.string().optional(),
  dateTo: z.string().optional(),
  sortBy: z.enum(['date', 'amount', 'vendor', 'created']).optional().default('created'),
  sortOrder: z.enum(['asc', 'desc']).optional().default('desc'),
  page: z.coerce.number().optional().default(1),
  pageSize: z.coerce.number().optional().default(20),
})

export const Route = createFileRoute('/_authenticated/receipts/')({
  validateSearch: receiptSearchSchema,
  component: ReceiptsPage,
})

function ReceiptsPage() {
  const search = Route.useSearch()
  const navigate = Route.useNavigate()
  const [uploadDialogOpen, setUploadDialogOpen] = useState(false)

  const { status, matchStatus, vendor, dateFrom, dateTo, sortBy, sortOrder, page, pageSize } = search

  const { data: receipts, isLoading, error } = useReceiptList({
    status,
    matchStatus,
    vendor,
    dateFrom,
    dateTo,
    sortBy,
    sortOrder,
    page,
    pageSize,
  })

  const { data: statusCounts } = useReceiptStatusCounts()

  // Memoize current filters object for the panel
  const currentFilters = useMemo<ReceiptFilters>(() => ({
    status,
    matchStatus,
    vendor,
    dateFrom,
    dateTo,
    sortBy: sortBy || 'created',
    sortOrder: sortOrder || 'desc',
  }), [status, matchStatus, vendor, dateFrom, dateTo, sortBy, sortOrder])

  const handleFiltersChange = useCallback((newFilters: ReceiptFilters) => {
    navigate({
      search: {
        status: newFilters.status,
        matchStatus: newFilters.matchStatus,
        vendor: newFilters.vendor,
        dateFrom: newFilters.dateFrom,
        dateTo: newFilters.dateTo,
        sortBy: newFilters.sortBy,
        sortOrder: newFilters.sortOrder,
        page: 1, // Reset to first page on filter change
        pageSize,
      },
    })
  }, [navigate, pageSize])

  const handlePageChange = useCallback((newPage: number) => {
    navigate({
      search: {
        ...search,
        page: newPage,
      },
    })
  }, [navigate, search])

  const totalPages = receipts ? Math.ceil(receipts.totalCount / pageSize) : 0

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Receipts</h1>
          <p className="text-muted-foreground">
            Upload and manage your receipts
          </p>
        </div>
        <Dialog open={uploadDialogOpen} onOpenChange={setUploadDialogOpen}>
          <DialogTrigger asChild>
            <Button>
              <Upload className="mr-2 h-4 w-4" />
              Upload Receipts
            </Button>
          </DialogTrigger>
          <DialogContent className="sm:max-w-lg">
            <DialogHeader>
              <DialogTitle>Upload Receipts</DialogTitle>
              <DialogDescription>
                Drag and drop or browse for receipt images or PDFs
              </DialogDescription>
            </DialogHeader>
            <ReceiptUploadDropzone
              onUploadComplete={() => setUploadDialogOpen(false)}
            />
          </DialogContent>
        </Dialog>
      </div>

      {/* Filter Panel */}
      <ReceiptFilterPanel
        filters={currentFilters}
        onFiltersChange={handleFiltersChange}
        statusCounts={statusCounts?.counts}
        totalCount={receipts?.totalCount}
      />

      {/* Error State */}
      {error && (
        <Card className="border-destructive">
          <CardContent className="pt-6">
            <p className="text-destructive">Failed to load receipts. Please try again.</p>
          </CardContent>
        </Card>
      )}

      {/* Receipt Grid */}
      <div className="grid gap-4 grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6">
        {isLoading
          ? Array.from({ length: 12 }).map((_, i) => <ReceiptCardSkeleton key={i} />)
          : receipts?.items.map((receipt) => (
              <ReceiptCard key={receipt.id} receipt={receipt} />
            ))}
      </div>

      {/* Empty State */}
      {!isLoading && receipts?.items.length === 0 && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Receipt className="h-12 w-12 text-muted-foreground" />
            <h3 className="mt-4 text-lg font-semibold">No receipts found</h3>
            <p className="text-sm text-muted-foreground mt-1 text-center max-w-sm">
              {(status || matchStatus || vendor)
                ? 'No receipts match your current filters. Try adjusting your search criteria.'
                : 'Upload your first receipt to get started'}
            </p>
            {(status || matchStatus || vendor) ? (
              <Button
                variant="outline"
                className="mt-4"
                onClick={() => handleFiltersChange({
                  sortBy: 'created',
                  sortOrder: 'desc',
                })}
              >
                Clear Filters
              </Button>
            ) : (
              <Button
                className="mt-4"
                onClick={() => setUploadDialogOpen(true)}
              >
                <Upload className="mr-2 h-4 w-4" />
                Upload Receipts
              </Button>
            )}
          </CardContent>
        </Card>
      )}

      {/* Pagination */}
      {receipts && receipts.totalCount > pageSize && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            Showing {((page - 1) * pageSize) + 1} to {Math.min(page * pageSize, receipts.totalCount)} of {receipts.totalCount} receipts
          </p>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="icon"
              onClick={() => handlePageChange(page - 1)}
              disabled={page <= 1}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <span className="text-sm">
              Page {page} of {totalPages}
            </span>
            <Button
              variant="outline"
              size="icon"
              onClick={() => handlePageChange(page + 1)}
              disabled={page >= totalPages}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
