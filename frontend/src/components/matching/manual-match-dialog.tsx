"use client"

import { useState, useMemo, useEffect } from 'react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Card, CardContent } from '@/components/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import {
  useUnmatchedReceipts,
  useUnmatchedTransactions,
  useManualMatch,
} from '@/hooks/queries/use-matching'
import { formatCurrency, formatDate } from '@/lib/utils'
import { toast } from 'sonner'
import type { ReceiptSummary, TransactionSummary } from '@/types/api'
import {
  LinkIcon,
  Search,
  Receipt,
  CreditCard,
  Calendar,
  DollarSign,
  Check,
  Loader2,
} from 'lucide-react'

interface ManualMatchDialogProps {
  trigger?: React.ReactNode
  /** Control open state externally */
  open?: boolean
  /** Callback when open state changes */
  onOpenChange?: (open: boolean) => void
  /** Pre-select a specific receipt by ID */
  receiptId?: string
}

export function ManualMatchDialog({
  trigger,
  open: controlledOpen,
  onOpenChange,
  receiptId,
}: ManualMatchDialogProps) {
  const [internalOpen, setInternalOpen] = useState(false)

  // Support both controlled and uncontrolled modes
  const open = controlledOpen ?? internalOpen
  const setOpen = onOpenChange ?? setInternalOpen
  const [selectedReceipt, setSelectedReceipt] = useState<ReceiptSummary | null>(null)
  const [selectedTransaction, setSelectedTransaction] = useState<TransactionSummary | null>(null)
  const [receiptSearch, setReceiptSearch] = useState('')
  const [transactionSearch, setTransactionSearch] = useState('')

  const { data: receipts, isLoading: loadingReceipts } = useUnmatchedReceipts()
  const { data: transactions, isLoading: loadingTransactions } = useUnmatchedTransactions()
  const { mutate: manualMatch, isPending: isMatching } = useManualMatch()

  // Pre-select receipt when receiptId is provided and dialog opens
  useEffect(() => {
    if (open && receiptId && receipts && !selectedReceipt) {
      const receipt = receipts.find((r) => r.id === receiptId)
      if (receipt) {
        setSelectedReceipt(receipt)
      }
    }
  }, [open, receiptId, receipts, selectedReceipt])

  const filteredReceipts = useMemo(() => {
    if (!receipts) return []
    if (!receiptSearch) return receipts
    const search = receiptSearch.toLowerCase()
    return receipts.filter(
      (r) =>
        r.vendor?.toLowerCase().includes(search) ||
        r.originalFilename.toLowerCase().includes(search)
    )
  }, [receipts, receiptSearch])

  const filteredTransactions = useMemo(() => {
    if (!transactions) return []
    if (!transactionSearch) return transactions
    const search = transactionSearch.toLowerCase()
    return transactions.filter((t) => t.description.toLowerCase().includes(search))
  }, [transactions, transactionSearch])

  const handleMatch = () => {
    if (!selectedReceipt || !selectedTransaction) {
      toast.error('Please select both a receipt and a transaction')
      return
    }

    manualMatch(
      {
        receiptId: selectedReceipt.id,
        transactionId: selectedTransaction.id,
      },
      {
        onSuccess: () => {
          toast.success('Manual match created successfully')
          setOpen(false)
          setSelectedReceipt(null)
          setSelectedTransaction(null)
        },
        onError: (error) => {
          toast.error(`Failed to create match: ${error.message}`)
        },
      }
    )
  }

  const handleOpenChange = (newOpen: boolean) => {
    setOpen(newOpen)
    if (!newOpen) {
      setSelectedReceipt(null)
      setSelectedTransaction(null)
      setReceiptSearch('')
      setTransactionSearch('')
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogTrigger asChild>
        {trigger || (
          <Button variant="outline">
            <LinkIcon className="mr-2 h-4 w-4" />
            Manual Match
          </Button>
        )}
      </DialogTrigger>
      <DialogContent className="sm:max-w-4xl">
        <DialogHeader>
          <DialogTitle>Create Manual Match</DialogTitle>
          <DialogDescription>
            Select a receipt and a transaction to match them together
          </DialogDescription>
        </DialogHeader>

        <Tabs defaultValue="receipts" className="w-full">
          <TabsList className="grid w-full grid-cols-2">
            <TabsTrigger value="receipts">
              <Receipt className="mr-2 h-4 w-4" />
              1. Select Receipt
              {selectedReceipt && <Check className="ml-2 h-4 w-4 text-green-500" />}
            </TabsTrigger>
            <TabsTrigger value="transactions">
              <CreditCard className="mr-2 h-4 w-4" />
              2. Select Transaction
              {selectedTransaction && <Check className="ml-2 h-4 w-4 text-green-500" />}
            </TabsTrigger>
          </TabsList>

          <TabsContent value="receipts" className="space-y-4">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search receipts..."
                value={receiptSearch}
                onChange={(e) => setReceiptSearch(e.target.value)}
                className="pl-9"
              />
            </div>
            <ScrollArea className="h-[300px]">
              <div className="space-y-2 pr-4">
                {loadingReceipts ? (
                  <p className="text-sm text-muted-foreground text-center py-8">
                    Loading receipts...
                  </p>
                ) : filteredReceipts.length === 0 ? (
                  <p className="text-sm text-muted-foreground text-center py-8">
                    No unmatched receipts found
                  </p>
                ) : (
                  filteredReceipts.map((receipt) => (
                    <Card
                      key={receipt.id}
                      className={`cursor-pointer transition-all ${
                        selectedReceipt?.id === receipt.id
                          ? 'ring-2 ring-primary'
                          : 'hover:bg-muted/50'
                      }`}
                      onClick={() => setSelectedReceipt(receipt)}
                    >
                      <CardContent className="flex items-center gap-4 p-3">
                        <div className="h-12 w-12 rounded bg-muted flex-shrink-0 flex items-center justify-center overflow-hidden">
                          {receipt.thumbnailUrl ? (
                            <img
                              src={receipt.thumbnailUrl}
                              alt="Receipt"
                              className="h-full w-full object-cover"
                            />
                          ) : (
                            <Receipt className="h-6 w-6 text-muted-foreground" />
                          )}
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className="font-medium truncate">
                            {receipt.vendor || receipt.originalFilename}
                          </p>
                          <div className="flex items-center gap-3 text-sm text-muted-foreground">
                            <span className="flex items-center gap-1">
                              <Calendar className="h-3 w-3" />
                              {receipt.date ? formatDate(receipt.date) : 'No date'}
                            </span>
                            <span className="flex items-center gap-1">
                              <DollarSign className="h-3 w-3" />
                              {receipt.amount != null
                                ? formatCurrency(receipt.amount)
                                : 'No amount'}
                            </span>
                          </div>
                        </div>
                        {selectedReceipt?.id === receipt.id && (
                          <Check className="h-5 w-5 text-primary" />
                        )}
                      </CardContent>
                    </Card>
                  ))
                )}
              </div>
            </ScrollArea>
          </TabsContent>

          <TabsContent value="transactions" className="space-y-4">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search transactions..."
                value={transactionSearch}
                onChange={(e) => setTransactionSearch(e.target.value)}
                className="pl-9"
              />
            </div>
            <ScrollArea className="h-[300px]">
              <div className="space-y-2 pr-4">
                {loadingTransactions ? (
                  <p className="text-sm text-muted-foreground text-center py-8">
                    Loading transactions...
                  </p>
                ) : filteredTransactions.length === 0 ? (
                  <p className="text-sm text-muted-foreground text-center py-8">
                    No unmatched transactions found
                  </p>
                ) : (
                  filteredTransactions.map((txn) => (
                    <Card
                      key={txn.id}
                      className={`cursor-pointer transition-all ${
                        selectedTransaction?.id === txn.id
                          ? 'ring-2 ring-primary'
                          : 'hover:bg-muted/50'
                      }`}
                      onClick={() => setSelectedTransaction(txn)}
                    >
                      <CardContent className="flex items-center justify-between p-3">
                        <div className="flex-1 min-w-0">
                          <p className="font-medium truncate">{txn.description}</p>
                          <div className="flex items-center gap-3 text-sm text-muted-foreground">
                            <span className="flex items-center gap-1">
                              <Calendar className="h-3 w-3" />
                              {formatDate(txn.transactionDate)}
                            </span>
                          </div>
                        </div>
                        <div className="flex items-center gap-3">
                          <span className="font-medium">{formatCurrency(txn.amount)}</span>
                          {selectedTransaction?.id === txn.id && (
                            <Check className="h-5 w-5 text-primary" />
                          )}
                        </div>
                      </CardContent>
                    </Card>
                  ))
                )}
              </div>
            </ScrollArea>
          </TabsContent>
        </Tabs>

        {/* Selection Summary */}
        {(selectedReceipt || selectedTransaction) && (
          <div className="border-t pt-4">
            <Label className="text-sm text-muted-foreground">Selected Match</Label>
            <div className="flex items-center gap-4 mt-2">
              <div className="flex-1 p-3 bg-muted/50 rounded-lg">
                <p className="text-xs text-muted-foreground mb-1">Receipt</p>
                <p className="font-medium truncate">
                  {selectedReceipt
                    ? selectedReceipt.vendor || selectedReceipt.originalFilename
                    : 'Not selected'}
                </p>
              </div>
              <LinkIcon className="h-5 w-5 text-muted-foreground" />
              <div className="flex-1 p-3 bg-muted/50 rounded-lg">
                <p className="text-xs text-muted-foreground mb-1">Transaction</p>
                <p className="font-medium truncate">
                  {selectedTransaction ? selectedTransaction.description : 'Not selected'}
                </p>
              </div>
            </div>
          </div>
        )}

        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={() => setOpen(false)}>
            Cancel
          </Button>
          <Button
            onClick={handleMatch}
            disabled={!selectedReceipt || !selectedTransaction || isMatching}
          >
            {isMatching ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Matching...
              </>
            ) : (
              <>
                <LinkIcon className="mr-2 h-4 w-4" />
                Create Match
              </>
            )}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
