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
  useMatchCandidates,
  useManualMatch,
} from '@/hooks/queries/use-matching'
import { formatCurrency, formatDate } from '@/lib/utils'
import { toast } from 'sonner'
import type { MatchReceiptSummary } from '@/types/api'
import type { MatchCandidate } from '@/types/match'
import {
  LinkIcon,
  Search,
  Receipt,
  CreditCard,
  Calendar,
  DollarSign,
  Check,
  Loader2,
  Layers,
  TrendingUp,
} from 'lucide-react'
import { Badge } from '@/components/ui/badge'

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
  const [selectedReceipt, setSelectedReceipt] = useState<MatchReceiptSummary | null>(null)
  const [selectedCandidate, setSelectedCandidate] = useState<MatchCandidate | null>(null)
  const [receiptSearch, setReceiptSearch] = useState('')
  const [candidateSearch, setCandidateSearch] = useState('')

  const { data: receipts, isLoading: loadingReceipts } = useUnmatchedReceipts()
  // Fetch candidates when a receipt is selected
  const { data: candidates, isLoading: loadingCandidates } = useMatchCandidates(
    selectedReceipt?.id ?? '',
    20 // Fetch top 20 candidates
  )
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
        r.vendorExtracted?.toLowerCase().includes(search) ||
        r.originalFilename.toLowerCase().includes(search)
    )
  }, [receipts, receiptSearch])

  const filteredCandidates = useMemo(() => {
    if (!candidates) return []
    if (!candidateSearch) return candidates
    const search = candidateSearch.toLowerCase()
    return candidates.filter((c) => c.displayName.toLowerCase().includes(search))
  }, [candidates, candidateSearch])

  const handleMatch = () => {
    if (!selectedReceipt || !selectedCandidate) {
      toast.error('Please select both a receipt and a match target')
      return
    }

    // Build request with either transactionId or transactionGroupId based on candidate type
    const request = {
      receiptId: selectedReceipt.id,
      ...(selectedCandidate.candidateType === 'group'
        ? { transactionGroupId: selectedCandidate.id }
        : { transactionId: selectedCandidate.id }),
    }

    manualMatch(request, {
      onSuccess: () => {
        const targetType = selectedCandidate.candidateType === 'group' ? 'group' : 'transaction'
        toast.success(`Receipt matched to ${targetType} successfully`)
        setOpen(false)
        setSelectedReceipt(null)
        setSelectedCandidate(null)
      },
      onError: (error) => {
        toast.error(`Failed to create match: ${error.message}`)
      },
    })
  }

  const handleOpenChange = (newOpen: boolean) => {
    setOpen(newOpen)
    if (!newOpen) {
      setSelectedReceipt(null)
      setSelectedCandidate(null)
      setReceiptSearch('')
      setCandidateSearch('')
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
            Select a receipt, then choose from ranked match candidates (transactions or groups)
          </DialogDescription>
        </DialogHeader>

        <Tabs defaultValue="receipts" className="w-full">
          <TabsList className="grid w-full grid-cols-2">
            <TabsTrigger value="receipts">
              <Receipt className="mr-2 h-4 w-4" />
              1. Select Receipt
              {selectedReceipt && <Check className="ml-2 h-4 w-4 text-green-500" />}
            </TabsTrigger>
            <TabsTrigger value="candidates" disabled={!selectedReceipt}>
              <CreditCard className="mr-2 h-4 w-4" />
              2. Select Match
              {selectedCandidate && <Check className="ml-2 h-4 w-4 text-green-500" />}
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
                            {receipt.vendorExtracted || receipt.originalFilename}
                          </p>
                          <div className="flex items-center gap-3 text-sm text-muted-foreground">
                            <span className="flex items-center gap-1">
                              <Calendar className="h-3 w-3" />
                              {receipt.dateExtracted ? formatDate(receipt.dateExtracted) : 'No date'}
                            </span>
                            <span className="flex items-center gap-1">
                              <DollarSign className="h-3 w-3" />
                              {receipt.amountExtracted != null
                                ? formatCurrency(receipt.amountExtracted)
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

          <TabsContent value="candidates" className="space-y-4">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search candidates..."
                value={candidateSearch}
                onChange={(e) => setCandidateSearch(e.target.value)}
                className="pl-9"
              />
            </div>
            <ScrollArea className="h-[300px]">
              <div className="space-y-2 pr-4">
                {!selectedReceipt ? (
                  <p className="text-sm text-muted-foreground text-center py-8">
                    Please select a receipt first
                  </p>
                ) : loadingCandidates ? (
                  <div className="flex items-center justify-center py-8 gap-2">
                    <Loader2 className="h-4 w-4 animate-spin" />
                    <span className="text-sm text-muted-foreground">Finding best matches...</span>
                  </div>
                ) : filteredCandidates.length === 0 ? (
                  <p className="text-sm text-muted-foreground text-center py-8">
                    No match candidates found
                  </p>
                ) : (
                  filteredCandidates.map((candidate) => (
                    <Card
                      key={candidate.id}
                      className={`cursor-pointer transition-all ${
                        selectedCandidate?.id === candidate.id
                          ? 'ring-2 ring-primary'
                          : 'hover:bg-muted/50'
                      }`}
                      onClick={() => setSelectedCandidate(candidate)}
                    >
                      <CardContent className="flex items-center justify-between p-3">
                        <div className="flex items-center gap-3 flex-1 min-w-0">
                          {/* Icon based on type */}
                          <div className="flex-shrink-0">
                            {candidate.candidateType === 'group' ? (
                              <div className="h-8 w-8 rounded bg-purple-100 dark:bg-purple-900/30 flex items-center justify-center">
                                <Layers className="h-4 w-4 text-purple-600 dark:text-purple-400" />
                              </div>
                            ) : (
                              <div className="h-8 w-8 rounded bg-blue-100 dark:bg-blue-900/30 flex items-center justify-center">
                                <CreditCard className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                              </div>
                            )}
                          </div>
                          <div className="flex-1 min-w-0">
                            <div className="flex items-center gap-2">
                              <p className="font-medium truncate">{candidate.displayName}</p>
                              {candidate.candidateType === 'group' && (
                                <Badge variant="secondary" className="text-xs shrink-0">
                                  {candidate.transactionCount} txns
                                </Badge>
                              )}
                            </div>
                            <div className="flex items-center gap-3 text-sm text-muted-foreground">
                              <span className="flex items-center gap-1">
                                <Calendar className="h-3 w-3" />
                                {formatDate(candidate.date)}
                              </span>
                              <span className="flex items-center gap-1">
                                <TrendingUp className="h-3 w-3" />
                                <span className={`font-medium ${
                                  candidate.confidenceScore >= 0.9 ? 'text-green-600 dark:text-green-400' :
                                  candidate.confidenceScore >= 0.7 ? 'text-yellow-600 dark:text-yellow-400' :
                                  'text-red-600 dark:text-red-400'
                                }`}>
                                  {Math.round(candidate.confidenceScore * 100)}%
                                </span>
                              </span>
                            </div>
                          </div>
                        </div>
                        <div className="flex items-center gap-3">
                          <span className="font-medium">{formatCurrency(candidate.amount)}</span>
                          {selectedCandidate?.id === candidate.id && (
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
        {(selectedReceipt || selectedCandidate) && (
          <div className="border-t pt-4">
            <Label className="text-sm text-muted-foreground">Selected Match</Label>
            <div className="flex items-center gap-4 mt-2">
              <div className="flex-1 p-3 bg-muted/50 rounded-lg">
                <p className="text-xs text-muted-foreground mb-1">Receipt</p>
                <p className="font-medium truncate">
                  {selectedReceipt
                    ? selectedReceipt.vendorExtracted || selectedReceipt.originalFilename
                    : 'Not selected'}
                </p>
              </div>
              <LinkIcon className="h-5 w-5 text-muted-foreground" />
              <div className="flex-1 p-3 bg-muted/50 rounded-lg">
                <p className="text-xs text-muted-foreground mb-1 flex items-center gap-1">
                  {selectedCandidate?.candidateType === 'group' ? (
                    <>
                      <Layers className="h-3 w-3" />
                      Group
                    </>
                  ) : (
                    <>
                      <CreditCard className="h-3 w-3" />
                      Transaction
                    </>
                  )}
                </p>
                <p className="font-medium truncate">
                  {selectedCandidate ? selectedCandidate.displayName : 'Not selected'}
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
            disabled={!selectedReceipt || !selectedCandidate || isMatching}
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
