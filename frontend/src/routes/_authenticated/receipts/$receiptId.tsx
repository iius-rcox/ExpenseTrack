"use client"

import { useCallback, useMemo } from 'react'
import { createFileRoute, Link } from '@tanstack/react-router'
import { useReceiptDetail, useDeleteReceipt, useRetryReceipt, useProcessReceipt, useUpdateReceipt } from '@/hooks/queries/use-receipts'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { ScrollArea } from '@/components/ui/scroll-area'
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
import { formatCurrency, formatDateTime, getStatusVariant } from '@/lib/utils'
import { toast } from 'sonner'
import {
  ArrowLeft,
  FileImage,
  RefreshCw,
  Trash2,
  AlertCircle,
  ExternalLink,
  Loader2,
  Save,
  RotateCcw,
  X,
} from 'lucide-react'
import { DocumentViewer } from '@/components/ui/document-viewer'
import { ExtractedField } from '@/components/receipts/extracted-field'
import { toReceiptPreview, type ExtractedFieldKey, type CorrectionMetadata, type ReceiptUpdateRequest } from '@/types/receipt'
import { useUndo } from '@/hooks/ui/use-undo'
import { motion, AnimatePresence } from 'framer-motion'

export const Route = createFileRoute('/_authenticated/receipts/$receiptId')({
  component: ReceiptDetailPage,
  parseParams: (params: Record<string, string>) => ({
    receiptId: params.receiptId,
  }),
})

interface FieldEditState {
  field: ExtractedFieldKey
  originalValue: string | number | null
  currentValue: string | number | null
}

function ReceiptDetailPage() {
  const params = Route.useParams()
  const receiptId = params.receiptId
  const navigate = Route.useNavigate()
  const { data: receipt, isLoading, error } = useReceiptDetail(receiptId)
  const { mutate: deleteReceipt, isPending: isDeleting } = useDeleteReceipt()
  const { mutate: retryReceipt, isPending: isRetrying } = useRetryReceipt()
  const { mutate: processReceipt, isPending: isProcessing } = useProcessReceipt()
  const { mutate: updateReceipt, isPending: isSaving } = useUpdateReceipt()

  // Track field edits with undo support (Feature 024)
  const {
    current: editedFields,
    push: setEditedFields,
    undo,
    canUndo,
    reset: clearEdits,
  } = useUndo<Map<ExtractedFieldKey, FieldEditState>>(new Map())

  // Convert receipt to preview format for ExtractedField components
  const receiptPreview = useMemo(() => {
    if (!receipt) return null
    return toReceiptPreview(receipt)
  }, [receipt])

  // Check if receipt is processing (cannot edit)
  const isReceiptProcessing = receipt?.status === 'Processing'

  // Get field with any pending edits applied
  const getFieldWithEdits = useCallback(
    (field: { key: ExtractedFieldKey; value: string | number | null; confidence: number; isEdited: boolean }) => {
      const edit = editedFields.get(field.key)
      if (edit) {
        return { ...field, value: edit.currentValue, isEdited: true }
      }
      return field
    },
    [editedFields]
  )

  // Handle field update
  const handleFieldUpdate = useCallback(
    (fieldKey: ExtractedFieldKey, newValue: string | number | null) => {
      const field = receiptPreview?.extractedFields.find((f) => f.key === fieldKey)
      const originalValue = field?.value ?? null

      // Don't track no-op edits
      if (originalValue === newValue) {
        const newEdits = new Map(editedFields)
        newEdits.delete(fieldKey)
        setEditedFields(newEdits)
        return
      }

      const newEdits = new Map(editedFields)
      newEdits.set(fieldKey, {
        field: fieldKey,
        originalValue,
        currentValue: newValue,
      })
      setEditedFields(newEdits)
    },
    [editedFields, setEditedFields, receiptPreview]
  )

  // Handle field undo
  const handleFieldUndo = useCallback(
    (fieldKey: ExtractedFieldKey) => {
      const newEdits = new Map(editedFields)
      newEdits.delete(fieldKey)
      setEditedFields(newEdits)
    },
    [editedFields, setEditedFields]
  )

  // Check if there are unsaved changes
  const hasChanges = editedFields.size > 0

  // Map UI field keys to API field names for training feedback
  const mapFieldToApiName = (field: ExtractedFieldKey): CorrectionMetadata['fieldName'] | null => {
    switch (field) {
      case 'merchant':
        return 'vendor'
      case 'amount':
        return 'amount'
      case 'date':
        return 'date'
      case 'taxAmount':
        return 'tax'
      case 'currency':
        return 'currency'
      default:
        // Fields like 'category', 'tip', 'subtotal', 'paymentMethod' are not tracked for corrections
        return null
    }
  }

  // Save all changes
  const handleSaveAll = () => {
    if (!receipt || !receiptPreview || editedFields.size === 0) return

    // Build corrections metadata for training feedback (only for trackable fields)
    const corrections: CorrectionMetadata[] = Array.from(editedFields.values())
      .map((edit) => {
        const apiFieldName = mapFieldToApiName(edit.field)
        if (!apiFieldName) return null
        return {
          fieldName: apiFieldName,
          originalValue: String(edit.originalValue ?? ''),
        }
      })
      .filter((c): c is CorrectionMetadata => c !== null)

    // Build update request
    const request: ReceiptUpdateRequest = {
      rowVersion: receiptPreview.rowVersion,
      corrections: corrections.length > 0 ? corrections : undefined,
    }

    // Map field edits to receipt properties
    editedFields.forEach((edit) => {
      switch (edit.field) {
        case 'merchant':
          request.vendor = edit.currentValue as string | null
          break
        case 'amount':
          request.amount = edit.currentValue as number | null
          break
        case 'date':
          request.date = edit.currentValue as string | null
          break
        case 'taxAmount':
          request.tax = edit.currentValue as number | null
          break
        case 'currency':
          request.currency = edit.currentValue as string | null
          break
      }
    })

    updateReceipt(
      { receiptId, request },
      {
        onSuccess: () => {
          clearEdits(new Map())
        },
        // Note: useUpdateReceipt already handles error toasts
      }
    )
  }

  // Discard all changes
  const handleDiscard = () => {
    clearEdits(new Map())
    toast.info('Changes discarded')
  }

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

  const handleProcess = () => {
    processReceipt(receiptId, {
      onSuccess: () => {
        toast.success('Receipt processing started')
      },
      onError: (error) => {
        toast.error(`Failed to start processing: ${error.message}`)
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
        <div className="flex items-center justify-between">
          <Button variant="ghost" asChild>
            <Link to="/receipts">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back to Receipts
            </Link>
          </Button>
          <AlertDialog>
            <AlertDialogTrigger asChild>
              <Button variant="destructive" disabled={isDeleting}>
                {isDeleting ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <Trash2 className="mr-2 h-4 w-4" />
                )}
                Delete Broken Receipt
              </Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>Delete Broken Receipt?</AlertDialogTitle>
                <AlertDialogDescription>
                  This receipt failed to load and may have corrupted data.
                  Deleting it will allow you to upload a fresh copy.
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
          {receipt.status === 'Uploaded' && (
            <Button
              variant="default"
              onClick={handleProcess}
              disabled={isProcessing}
            >
              {isProcessing ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <RefreshCw className="mr-2 h-4 w-4" />
              )}
              Process Receipt
            </Button>
          )}
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
        {/* Receipt Document Viewer */}
        <Card className="flex flex-col">
          <CardHeader className="pb-2">
            <CardTitle className="flex items-center gap-2">
              <FileImage className="h-5 w-5" />
              Receipt Document
            </CardTitle>
          </CardHeader>
          <CardContent className="flex-1 p-2">
            <div className="h-[500px] bg-muted rounded-lg overflow-hidden">
              <DocumentViewer
                src={receipt.blobUrl}
                contentType={receipt.contentType}
                filename={receipt.originalFilename}
                alt={receipt.originalFilename}
                showControls={true}
              />
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

          {/* Extracted Data with Inline Editing (Feature 024) */}
          <Card className="flex flex-col">
            <CardHeader className="pb-2">
              <div className="flex items-center justify-between">
                <div className="space-y-1">
                  <CardTitle>Extracted Information</CardTitle>
                  <CardDescription>
                    {receiptPreview && receiptPreview.extractedFields.length > 0 ? (
                      <>
                        Click the pencil icon to edit fields •{' '}
                        <span className="text-muted-foreground">
                          {Math.round(
                            (receiptPreview.extractedFields.reduce((sum, f) => sum + f.confidence, 0) /
                              receiptPreview.extractedFields.length) *
                              100
                          )}
                          % avg confidence
                        </span>
                      </>
                    ) : (
                      'Data extracted from the receipt'
                    )}
                  </CardDescription>
                </div>
                {isReceiptProcessing && (
                  <Badge variant="outline" className="text-blue-600 border-blue-600/30">
                    <Loader2 className="h-3 w-3 mr-1 animate-spin" />
                    Processing
                  </Badge>
                )}
              </div>
            </CardHeader>
            <Separator />
            <ScrollArea className="flex-1 max-h-[400px]">
              <CardContent className="p-4 space-y-3">
                {receiptPreview?.extractedFields.map((field) => (
                  <ExtractedField
                    key={field.key}
                    field={getFieldWithEdits(field)}
                    onUpdate={(key, value) => handleFieldUpdate(key, value)}
                    onUndo={() => handleFieldUndo(field.key)}
                    canUndo={editedFields.has(field.key)}
                    isSaving={isSaving}
                    readOnly={isReceiptProcessing}
                    showConfidence
                    size="md"
                  />
                ))}

                {(!receiptPreview || receiptPreview.extractedFields.length === 0) && (
                  <div className="text-center py-8 text-muted-foreground">
                    <AlertCircle className="h-8 w-8 mx-auto mb-2 opacity-50" />
                    <p className="text-sm">No fields extracted yet</p>
                  </div>
                )}

                {receipt.lineItems.length > 0 && (
                  <>
                    <Separator className="my-4" />
                    <div>
                      <h4 className="font-medium mb-2 text-sm text-muted-foreground">Line Items</h4>
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
            </ScrollArea>

            {/* Save/Discard buttons appear when there are changes */}
            <AnimatePresence>
              {hasChanges && (
                <motion.div
                  initial={{ opacity: 0, y: 20 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={{ opacity: 0, y: 20 }}
                  className="border-t p-4 bg-muted/30"
                >
                  <div className="flex items-center gap-2 flex-wrap">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={undo}
                      disabled={!canUndo || isSaving}
                    >
                      <RotateCcw className="h-4 w-4 mr-1" />
                      Undo
                    </Button>
                    <div className="flex-1" />
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={handleDiscard}
                      disabled={isSaving}
                    >
                      <X className="h-4 w-4 mr-1" />
                      Discard
                    </Button>
                    <Button
                      size="sm"
                      onClick={handleSaveAll}
                      disabled={isSaving}
                    >
                      {isSaving ? (
                        <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                      ) : (
                        <Save className="h-4 w-4 mr-1" />
                      )}
                      Save All ({editedFields.size})
                    </Button>
                  </div>
                </motion.div>
              )}
            </AnimatePresence>
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
