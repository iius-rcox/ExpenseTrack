"use client"

import { createFileRoute, Link } from '@tanstack/react-router'
import { useReceiptDetail, useDeleteReceipt, useRetryReceipt } from '@/hooks/queries/use-receipts'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'
import { formatCurrency, formatDate, formatDateTime, getStatusVariant } from '@/lib/utils'
import { toast } from 'sonner'
import {
  ArrowLeft,
  FileImage,
  RefreshCw,
  Trash2,
  AlertCircle,
  ExternalLink,
  Calendar,
  DollarSign,
  Store,
  FileText,
  Loader2,
} from 'lucide-react'

export const Route = createFileRoute('/_authenticated/receipts/$receiptId')({
  component: ReceiptDetailPage,
  parseParams: (params: Record<string, string>) => ({
    receiptId: params.receiptId,
  }),
})

function ReceiptDetailPage() {
  const params = Route.useParams()
  const receiptId = params.receiptId
  const navigate = Route.useNavigate()
  const { data: receipt, isLoading, error } = useReceiptDetail(receiptId)
  const { mutate: deleteReceipt, isPending: isDeleting } = useDeleteReceipt()
  const { mutate: retryReceipt, isPending: isRetrying } = useRetryReceipt()

  const handleDelete = () => {
    deleteReceipt(receiptId, {
      onSuccess: () => {
        toast.success('Receipt deleted successfully')
        navigate({ to: '/receipts' })
      },
      onError: (error) => {
        toast.error(`Failed to delete receipt: ${error.message}`)
      },
    })
  }

  const handleRetry = () => {
    retryReceipt(receiptId, {
      onSuccess: () => {
        toast.success('Receipt reprocessing started')
      },
      onError: (error) => {
        toast.error(`Failed to retry processing: ${error.message}`)
      },
    })
  }

  if (isLoading) {
    return <ReceiptDetailSkeleton />
  }

  if (error) {
    return (
      <div className="space-y-6">
        <Button variant="ghost" asChild>
          <Link to="/receipts">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to Receipts
          </Link>
        </Button>
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Error loading receipt</AlertTitle>
          <AlertDescription>
            {error.message || 'Failed to load receipt details. Please try again.'}
          </AlertDescription>
        </Alert>
      </div>
    )
  }

  if (!receipt) {
    return null
  }

  const statusVariant = getStatusVariant(receipt.status)

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" asChild>
            <Link to="/receipts">
              <ArrowLeft className="h-4 w-4" />
            </Link>
          </Button>
          <div>
            <h1 className="text-2xl font-bold">{receipt.vendor || receipt.originalFilename}</h1>
            <p className="text-sm text-muted-foreground">
              Receipt ID: {receipt.id.slice(0, 8)}...
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {receipt.status === 'Error' && (
            <Button
              variant="outline"
              onClick={handleRetry}
              disabled={isRetrying}
            >
              {isRetrying ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <RefreshCw className="mr-2 h-4 w-4" />
              )}
              Retry Processing
            </Button>
          )}
          <AlertDialog>
            <AlertDialogTrigger asChild>
              <Button variant="destructive" disabled={isDeleting}>
                {isDeleting ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <Trash2 className="mr-2 h-4 w-4" />
                )}
                Delete
              </Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>Delete Receipt?</AlertDialogTitle>
                <AlertDialogDescription>
                  This action cannot be undone. This will permanently delete the receipt
                  and any associated matches.
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>Cancel</AlertDialogCancel>
                <AlertDialogAction onClick={handleDelete}>
                  Delete Receipt
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Receipt Image */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <FileImage className="h-5 w-5" />
              Receipt Image
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="relative aspect-[3/4] bg-muted rounded-lg overflow-hidden">
              {receipt.blobUrl ? (
                <img
                  src={receipt.blobUrl}
                  alt={receipt.originalFilename}
                  className="object-contain w-full h-full"
                />
              ) : (
                <div className="flex items-center justify-center w-full h-full text-muted-foreground">
                  <FileText className="h-16 w-16" />
                </div>
              )}
            </div>
            {receipt.blobUrl && (
              <Button variant="outline" className="w-full mt-4" asChild>
                <a href={receipt.blobUrl} target="_blank" rel="noopener noreferrer">
                  <ExternalLink className="mr-2 h-4 w-4" />
                  Open Full Size
                </a>
              </Button>
            )}
          </CardContent>
        </Card>

        {/* Receipt Details */}
        <div className="space-y-6">
          {/* Status */}
          <Card>
            <CardHeader>
              <CardTitle>Status</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="flex items-center justify-between">
                <Badge variant={statusVariant} className="text-sm px-3 py-1">
                  {receipt.status}
                </Badge>
                {receipt.errorMessage && (
                  <span className="text-sm text-destructive">{receipt.errorMessage}</span>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Extracted Data */}
          <Card>
            <CardHeader>
              <CardTitle>Extracted Information</CardTitle>
              <CardDescription>Data extracted from the receipt</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-4 grid-cols-2">
                <div className="space-y-1">
                  <p className="text-sm text-muted-foreground flex items-center gap-1">
                    <Store className="h-4 w-4" /> Vendor
                  </p>
                  <p className="font-medium">{receipt.vendor || 'Not detected'}</p>
                </div>
                <div className="space-y-1">
                  <p className="text-sm text-muted-foreground flex items-center gap-1">
                    <Calendar className="h-4 w-4" /> Date
                  </p>
                  <p className="font-medium">
                    {receipt.date ? formatDate(receipt.date) : 'Not detected'}
                  </p>
                </div>
                <div className="space-y-1">
                  <p className="text-sm text-muted-foreground flex items-center gap-1">
                    <DollarSign className="h-4 w-4" /> Total
                  </p>
                  <p className="font-medium text-lg">
                    {receipt.amount != null
                      ? formatCurrency(receipt.amount, receipt.currency)
                      : 'Not detected'}
                  </p>
                </div>
                <div className="space-y-1">
                  <p className="text-sm text-muted-foreground flex items-center gap-1">
                    <DollarSign className="h-4 w-4" /> Tax
                  </p>
                  <p className="font-medium">
                    {receipt.tax != null
                      ? formatCurrency(receipt.tax, receipt.currency)
                      : 'Not detected'}
                  </p>
                </div>
              </div>

              {receipt.lineItems.length > 0 && (
                <>
                  <Separator />
                  <div>
                    <h4 className="font-medium mb-2">Line Items</h4>
                    <div className="space-y-2">
                      {receipt.lineItems.map((item, index) => (
                        <div
                          key={index}
                          className="flex items-center justify-between text-sm p-2 rounded bg-muted/50"
                        >
                          <span className="flex-1 truncate">{item.description}</span>
                          <span className="font-medium">
                            {item.totalPrice != null
                              ? formatCurrency(item.totalPrice, receipt.currency)
                              : '--'}
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                </>
              )}
            </CardContent>
          </Card>

          {/* Metadata */}
          <Card>
            <CardHeader>
              <CardTitle>File Information</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Original Filename</span>
                <span className="truncate ml-4">{receipt.originalFilename}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">File Size</span>
                <span>{(receipt.fileSize / 1024).toFixed(1)} KB</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Content Type</span>
                <span>{receipt.contentType}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Page Count</span>
                <span>{receipt.pageCount}</span>
              </div>
              <Separator />
              <div className="flex justify-between">
                <span className="text-muted-foreground">Uploaded</span>
                <span>{formatDateTime(receipt.createdAt)}</span>
              </div>
              {receipt.processedAt && (
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Processed</span>
                  <span>{formatDateTime(receipt.processedAt)}</span>
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}

function ReceiptDetailSkeleton() {
  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Skeleton className="h-10 w-10" />
        <div className="space-y-2">
          <Skeleton className="h-8 w-48" />
          <Skeleton className="h-4 w-32" />
        </div>
      </div>
      <div className="grid gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <Skeleton className="h-6 w-32" />
          </CardHeader>
          <CardContent>
            <Skeleton className="aspect-[3/4] w-full" />
          </CardContent>
        </Card>
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <Skeleton className="h-6 w-24" />
            </CardHeader>
            <CardContent>
              <Skeleton className="h-8 w-20" />
            </CardContent>
          </Card>
          <Card>
            <CardHeader>
              <Skeleton className="h-6 w-40" />
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-4 grid-cols-2">
                {[1, 2, 3, 4].map((i) => (
                  <div key={i} className="space-y-2">
                    <Skeleton className="h-4 w-16" />
                    <Skeleton className="h-5 w-24" />
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
