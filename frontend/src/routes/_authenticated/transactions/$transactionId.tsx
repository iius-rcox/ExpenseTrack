"use client"

import { createFileRoute, Link } from '@tanstack/react-router'
import { useTransactionDetail } from '@/hooks/queries/use-transactions'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { formatCurrency, formatDate, formatDateTime } from '@/lib/utils'
import {
  ArrowLeft,
  AlertCircle,
  Calendar,
  DollarSign,
  Building2,
  CreditCard,
  FileText,
  Receipt,
  ExternalLink,
  Check,
  X,
} from 'lucide-react'

export const Route = createFileRoute('/_authenticated/transactions/$transactionId')({
  component: TransactionDetailPage,
  parseParams: (params: Record<string, string>) => ({
    transactionId: params.transactionId,
  }),
})

function TransactionDetailPage() {
  const params = Route.useParams()
  const transactionId = params.transactionId
  const { data: transaction, isLoading, error } = useTransactionDetail(transactionId)

  if (isLoading) {
    return <TransactionDetailSkeleton />
  }

  if (error) {
    return (
      <div className="space-y-6">
        <Button variant="ghost" asChild>
          <Link to="/transactions">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to Transactions
          </Link>
        </Button>
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Error loading transaction</AlertTitle>
          <AlertDescription>
            {error.message || 'Failed to load transaction details. Please try again.'}
          </AlertDescription>
        </Alert>
      </div>
    )
  }

  if (!transaction) {
    return null
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" asChild>
            <Link to="/transactions">
              <ArrowLeft className="h-4 w-4" />
            </Link>
          </Button>
          <div>
            <h1 className="text-2xl font-bold">{transaction.description}</h1>
            <p className="text-sm text-muted-foreground">
              Transaction ID: {transaction.id.slice(0, 8)}...
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {transaction.hasMatchedReceipt ? (
            <Badge variant="default" className="bg-green-500">
              <Check className="mr-1 h-3 w-3" />
              Matched
            </Badge>
          ) : (
            <Badge variant="secondary">
              <X className="mr-1 h-3 w-3" />
              Unmatched
            </Badge>
          )}
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Transaction Details */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <CreditCard className="h-5 w-5" />
              Transaction Details
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid gap-4 grid-cols-2">
              <div className="space-y-1">
                <p className="text-sm text-muted-foreground flex items-center gap-1">
                  <Calendar className="h-4 w-4" /> Date
                </p>
                <p className="font-medium">
                  {formatDate(transaction.transactionDate)}
                </p>
              </div>
              <div className="space-y-1">
                <p className="text-sm text-muted-foreground flex items-center gap-1">
                  <DollarSign className="h-4 w-4" /> Amount
                </p>
                <p className="font-medium text-lg">
                  {formatCurrency(transaction.amount)}
                </p>
              </div>
              <div className="space-y-1 col-span-2">
                <p className="text-sm text-muted-foreground flex items-center gap-1">
                  <FileText className="h-4 w-4" /> Description
                </p>
                <p className="font-medium">{transaction.description}</p>
              </div>
              {transaction.category && (
                <div className="space-y-1">
                  <p className="text-sm text-muted-foreground">Category</p>
                  <Badge variant="outline">{transaction.category}</Badge>
                </div>
              )}
              {transaction.merchantName && (
                <div className="space-y-1">
                  <p className="text-sm text-muted-foreground flex items-center gap-1">
                    <Building2 className="h-4 w-4" /> Merchant
                  </p>
                  <p className="font-medium">{transaction.merchantName}</p>
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Statement Info - only show if transaction has a statement */}
        {transaction.statementId && (
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <FileText className="h-5 w-5" />
                Statement Information
              </CardTitle>
              <CardDescription>Source statement details</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Statement ID</span>
                <span className="truncate ml-4">{transaction.statementId.slice(0, 8)}...</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Import Date</span>
                <span>{formatDateTime(transaction.createdAt)}</span>
              </div>
              {transaction.rawDescription && transaction.rawDescription !== transaction.description && (
                <>
                  <Separator />
                  <div className="space-y-1">
                    <span className="text-muted-foreground">Original Description</span>
                    <p className="text-xs bg-muted p-2 rounded">{transaction.rawDescription}</p>
                  </div>
                </>
              )}
            </CardContent>
          </Card>
        )}

        {/* Matched Receipt */}
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Receipt className="h-5 w-5" />
              Linked Receipt
            </CardTitle>
            <CardDescription>
              {transaction.hasMatchedReceipt
                ? 'This transaction is matched to a receipt'
                : 'No receipt has been matched to this transaction'}
            </CardDescription>
          </CardHeader>
          <CardContent>
            {transaction.matchedReceipt ? (
              <div className="flex items-center justify-between p-4 bg-muted/50 rounded-lg">
                <div className="flex items-center gap-4">
                  <div className="h-16 w-16 bg-muted rounded flex items-center justify-center">
                    {transaction.matchedReceipt.thumbnailUrl ? (
                      <img
                        src={transaction.matchedReceipt.thumbnailUrl}
                        alt="Receipt"
                        className="h-full w-full object-cover rounded"
                      />
                    ) : (
                      <Receipt className="h-8 w-8 text-muted-foreground" />
                    )}
                  </div>
                  <div>
                    <p className="font-medium">
                      {transaction.matchedReceipt.vendor || 'Unknown Vendor'}
                    </p>
                    <p className="text-sm text-muted-foreground">
                      {transaction.matchedReceipt.date
                        ? formatDate(transaction.matchedReceipt.date)
                        : 'Date unknown'}{' '}
                      • {formatCurrency(transaction.matchedReceipt.amount || 0)}
                    </p>
                    <Badge variant="outline" className="mt-1">
                      Confidence: {Math.round((transaction.matchedReceipt.matchConfidence || 0) * 100)}%
                    </Badge>
                  </div>
                </div>
                <Button variant="outline" asChild>
                  <Link to="/receipts/$receiptId" params={{ receiptId: transaction.matchedReceipt.id }}>
                    <ExternalLink className="mr-2 h-4 w-4" />
                    View Receipt
                  </Link>
                </Button>
              </div>
            ) : (
              <div className="flex flex-col items-center justify-center py-8 text-center">
                <Receipt className="h-12 w-12 text-muted-foreground" />
                <p className="mt-4 text-sm text-muted-foreground">
                  No receipt matched yet. Upload a receipt or check the matching queue.
                </p>
                <div className="flex gap-2 mt-4">
                  <Button variant="outline" asChild>
                    <Link to="/receipts">
                      Upload Receipt
                    </Link>
                  </Button>
                  <Button variant="outline" asChild>
                    <Link to="/matching">
                      Review Matches
                    </Link>
                  </Button>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

function TransactionDetailSkeleton() {
  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Skeleton className="h-10 w-10" />
        <div className="space-y-2">
          <Skeleton className="h-8 w-64" />
          <Skeleton className="h-4 w-32" />
        </div>
      </div>
      <div className="grid gap-6 lg:grid-cols-2">
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
        <Card>
          <CardHeader>
            <Skeleton className="h-6 w-48" />
          </CardHeader>
          <CardContent className="space-y-3">
            {[1, 2, 3].map((i) => (
              <div key={i} className="flex justify-between">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-4 w-32" />
              </div>
            ))}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
