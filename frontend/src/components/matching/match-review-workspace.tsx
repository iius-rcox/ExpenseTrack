"use client"

/**
 * MatchReviewWorkspace Component (T070)
 *
 * Main workspace for reviewing match suggestions with keyboard navigation.
 * Supports A=approve, R=reject, arrow keys for navigation.
 */

import { useState, useCallback, useEffect } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { ConfidenceIndicator } from '@/components/design-system/confidence-indicator'
import { MatchReviewSkeleton } from '@/components/design-system/loading-skeleton'
import { EmptyState } from '@/components/design-system/empty-state'
import { MatchingFactors, buildMatchingFactors } from './matching-factors'
import { ManualMatchDialog } from './manual-match-dialog'
import { useKeyboardShortcuts } from '@/hooks/ui/use-keyboard-shortcuts'
import { cn } from '@/lib/utils'
import { fadeIn } from '@/lib/animations'
import { toast } from 'sonner'
import type { MatchProposal } from '@/types/api'
import {
  Check,
  X,
  ChevronLeft,
  ChevronRight,
  Keyboard,
  Receipt,
  CreditCard,
  ExternalLink,
  Loader2,
  CheckCheck,
  Layers,
} from 'lucide-react'

interface MatchReviewWorkspaceProps {
  proposals: MatchProposal[]
  isLoading?: boolean
  onConfirm: (matchId: string) => void
  onReject: (matchId: string) => void
  onBatchApprove?: (minConfidence: number) => void
  isProcessing?: boolean
  currentIndex?: number
  onIndexChange?: (index: number) => void
}

// Keyboard shortcuts are defined inline in the useKeyboardShortcuts call
// Keys: a=approve, r=reject, j/ArrowDown=next, k/ArrowUp=previous, m=manual

export function MatchReviewWorkspace({
  proposals,
  isLoading = false,
  onConfirm,
  onReject,
  onBatchApprove,
  isProcessing = false,
  currentIndex: controlledIndex,
  onIndexChange,
}: MatchReviewWorkspaceProps) {
  // Internal state for uncontrolled mode
  const [internalIndex, setInternalIndex] = useState(0)
  const [manualMatchOpen, setManualMatchOpen] = useState(false)
  const [showShortcuts, setShowShortcuts] = useState(false)
  const [isMobile, setIsMobile] = useState(false)
  const [mobileTab, setMobileTab] = useState<'receipt' | 'transaction'>('receipt')

  // Detect mobile viewport
  useEffect(() => {
    const checkMobile = () => setIsMobile(window.innerWidth < 768)
    checkMobile()
    window.addEventListener('resize', checkMobile)
    return () => window.removeEventListener('resize', checkMobile)
  }, [])

  // Support both controlled and uncontrolled modes
  const currentIndex = controlledIndex ?? internalIndex
  const setCurrentIndex = onIndexChange ?? setInternalIndex

  // Filter to pending proposals only
  const pendingProposals = proposals.filter((p) => p.status === 'Proposed')
  const currentProposal = pendingProposals[currentIndex]

  // Navigation handlers
  const goToNext = useCallback(() => {
    if (currentIndex < pendingProposals.length - 1) {
      setCurrentIndex(currentIndex + 1)
    } else if (pendingProposals.length > 0) {
      // Wrap to beginning
      setCurrentIndex(0)
    }
  }, [currentIndex, pendingProposals.length, setCurrentIndex])

  const goToPrevious = useCallback(() => {
    if (currentIndex > 0) {
      setCurrentIndex(currentIndex - 1)
    } else if (pendingProposals.length > 0) {
      // Wrap to end
      setCurrentIndex(pendingProposals.length - 1)
    }
  }, [currentIndex, pendingProposals.length, setCurrentIndex])

  // Action handlers
  const handleApprove = useCallback(() => {
    if (!currentProposal || isProcessing) return
    onConfirm(currentProposal.matchId)
    toast.success('Match approved')
    // Auto-advance to next
    if (currentIndex < pendingProposals.length - 1) {
      // Don't advance yet - wait for refetch
    }
  }, [currentProposal, isProcessing, onConfirm, currentIndex, pendingProposals.length])

  const handleReject = useCallback(() => {
    if (!currentProposal || isProcessing) return
    onReject(currentProposal.matchId)
    toast.info('Match rejected')
  }, [currentProposal, isProcessing, onReject])

  // Keyboard shortcuts - bind each key to its handler
  useKeyboardShortcuts(
    {
      a: handleApprove,
      r: handleReject,
      ArrowDown: goToNext,
      j: goToNext,
      ArrowUp: goToPrevious,
      k: goToPrevious,
      m: () => setManualMatchOpen(true),
    },
    { enabled: !isProcessing && !manualMatchOpen }
  )

  // Loading state
  if (isLoading) {
    return (
      <div className="space-y-4">
        <MatchReviewSkeleton />
      </div>
    )
  }

  // Empty state
  if (pendingProposals.length === 0) {
    return (
      <EmptyState
        icon={<CheckCheck className="h-12 w-12 text-emerald-500" />}
        title="All caught up!"
        description="No pending matches to review. Check back later or import more receipts."
      />
    )
  }

  return (
    <div className="space-y-6">
      {/* Header with position and shortcuts toggle */}
      <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
        <div className="flex items-center justify-between md:justify-start gap-4">
          <Badge variant="outline" className="text-base font-mono">
            {currentIndex + 1} / {pendingProposals.length}
          </Badge>
          <span className="text-sm text-muted-foreground">
            pending matches
          </span>
          {/* Mobile navigation arrows */}
          {isMobile && (
            <div className="flex items-center gap-1">
              <Button
                variant="ghost"
                size="icon"
                onClick={goToPrevious}
                disabled={pendingProposals.length <= 1}
                className="h-11 w-11"
              >
                <ChevronLeft className="h-5 w-5" />
              </Button>
              <Button
                variant="ghost"
                size="icon"
                onClick={goToNext}
                disabled={pendingProposals.length <= 1}
                className="h-11 w-11"
              >
                <ChevronRight className="h-5 w-5" />
              </Button>
            </div>
          )}
        </div>
        <div className="hidden md:flex items-center gap-2">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setShowShortcuts(!showShortcuts)}
          >
            <Keyboard className="h-4 w-4 mr-1" />
            Shortcuts
          </Button>
          {onBatchApprove && (
            <Button
              variant="outline"
              size="sm"
              onClick={() => onBatchApprove(0.9)}
              disabled={isProcessing}
            >
              <CheckCheck className="h-4 w-4 mr-1" />
              Batch Approve (90%+)
            </Button>
          )}
        </div>
      </div>

      {/* Keyboard shortcuts help */}
      <AnimatePresence>
        {showShortcuts && (
          <motion.div
            initial={{ opacity: 0, height: 0 }}
            animate={{ opacity: 1, height: 'auto' }}
            exit={{ opacity: 0, height: 0 }}
            className="overflow-hidden"
          >
            <Card className="bg-muted/50">
              <CardContent className="p-4">
                <div className="grid grid-cols-2 md:grid-cols-5 gap-4 text-sm">
                  <div className="flex items-center gap-2">
                    <kbd className="px-2 py-1 bg-background rounded border text-xs font-mono">
                      A
                    </kbd>
                    <span>Approve</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <kbd className="px-2 py-1 bg-background rounded border text-xs font-mono">
                      R
                    </kbd>
                    <span>Reject</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <kbd className="px-2 py-1 bg-background rounded border text-xs font-mono">
                      ↓/J
                    </kbd>
                    <span>Next</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <kbd className="px-2 py-1 bg-background rounded border text-xs font-mono">
                      ↑/K
                    </kbd>
                    <span>Previous</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <kbd className="px-2 py-1 bg-background rounded border text-xs font-mono">
                      M
                    </kbd>
                    <span>Manual Match</span>
                  </div>
                </div>
              </CardContent>
            </Card>
          </motion.div>
        )}
      </AnimatePresence>

      {/* Main comparison view */}
      <AnimatePresence mode="wait">
        <motion.div
          key={currentProposal?.matchId}
          variants={fadeIn}
          initial="initial"
          animate="animate"
          exit="exit"
        >
          <Card className="overflow-hidden">
            <CardHeader className="bg-muted/30 pb-4">
              <div className="flex items-center justify-between">
                <CardTitle className="text-lg font-medium">
                  Match Review
                </CardTitle>
                <div className="flex items-center gap-3">
                  <ConfidenceIndicator
                    score={currentProposal?.confidenceScore ?? 0}
                    size="md"
                    showLabel
                  />
                  <span className="text-2xl font-bold font-mono">
                    {Math.round((currentProposal?.confidenceScore ?? 0) * 100)}%
                  </span>
                </div>
              </div>
            </CardHeader>

            <CardContent className="p-0">
              {/* Mobile: Tabbed view */}
              {isMobile ? (
                <Tabs value={mobileTab} onValueChange={(v) => setMobileTab(v as 'receipt' | 'transaction')} className="w-full">
                  <TabsList className="grid w-full grid-cols-2 rounded-none border-b">
                    <TabsTrigger value="receipt" className="h-12 data-[state=active]:bg-muted/50">
                      <Receipt className="h-4 w-4 mr-2" />
                      Receipt
                    </TabsTrigger>
                    <TabsTrigger value="transaction" className="h-12 data-[state=active]:bg-muted/50">
                      {currentProposal?.candidateType === 'group' ? (
                        <>
                          <Layers className="h-4 w-4 mr-2" />
                          Group
                        </>
                      ) : (
                        <>
                          <CreditCard className="h-4 w-4 mr-2" />
                          Transaction
                        </>
                      )}
                    </TabsTrigger>
                  </TabsList>
                  <TabsContent value="receipt" className="p-4 mt-0">
                    {currentProposal?.receipt ? (
                      <div className="space-y-4">
                        <div className="aspect-[3/4] max-h-48 rounded-lg overflow-hidden bg-muted flex items-center justify-center">
                          {currentProposal.receipt.thumbnailUrl ? (
                            <img
                              src={currentProposal.receipt.thumbnailUrl}
                              alt="Receipt"
                              className="w-full h-full object-contain"
                            />
                          ) : (
                            <Receipt className="h-12 w-12 text-muted-foreground/50" />
                          )}
                        </div>
                        <div className="space-y-2 text-sm">
                          <div className="flex justify-between">
                            <span className="text-muted-foreground">Merchant</span>
                            <span className="font-medium">{currentProposal.receipt.vendorExtracted || 'Unknown'}</span>
                          </div>
                          <div className="flex justify-between">
                            <span className="text-muted-foreground">Amount</span>
                            <span className="font-medium font-mono">${currentProposal.receipt.amountExtracted?.toFixed(2) || 'N/A'}</span>
                          </div>
                          <div className="flex justify-between">
                            <span className="text-muted-foreground">Date</span>
                            <span className="font-medium">{currentProposal.receipt.dateExtracted || 'Unknown'}</span>
                          </div>
                        </div>
                      </div>
                    ) : (
                      <p className="text-muted-foreground text-sm">Receipt data not available</p>
                    )}
                  </TabsContent>
                  <TabsContent value="transaction" className="p-4 mt-0">
                    {currentProposal?.candidateType === 'group' && currentProposal.transactionGroup ? (
                      <div className="space-y-4">
                        <div className="p-4 bg-muted/50 rounded-lg">
                          <div className="flex items-center gap-2 mb-2">
                            <p className="font-medium text-base">{currentProposal.transactionGroup.name}</p>
                            <span className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded-full">
                              {currentProposal.transactionGroup.transactionCount} txns
                            </span>
                          </div>
                          <p className="text-2xl font-bold font-mono">${currentProposal.transactionGroup.combinedAmount.toFixed(2)}</p>
                        </div>
                        <div className="space-y-2 text-sm">
                          <div className="flex justify-between">
                            <span className="text-muted-foreground">Display Date</span>
                            <span className="font-medium">{new Date(currentProposal.transactionGroup.displayDate).toLocaleDateString()}</span>
                          </div>
                        </div>
                      </div>
                    ) : currentProposal?.transaction ? (
                      <div className="space-y-4">
                        <div className="p-4 bg-muted/50 rounded-lg">
                          <p className="font-medium text-base mb-2">{currentProposal.transaction.description}</p>
                          <p className="text-2xl font-bold font-mono">${currentProposal.transaction.amount.toFixed(2)}</p>
                        </div>
                        <div className="space-y-2 text-sm">
                          <div className="flex justify-between">
                            <span className="text-muted-foreground">Date</span>
                            <span className="font-medium">{new Date(currentProposal.transaction.transactionDate).toLocaleDateString()}</span>
                          </div>
                          {currentProposal.transaction.postDate && (
                            <div className="flex justify-between">
                              <span className="text-muted-foreground">Post Date</span>
                              <span className="font-medium">{new Date(currentProposal.transaction.postDate).toLocaleDateString()}</span>
                            </div>
                          )}
                          <div className="flex justify-between">
                            <span className="text-muted-foreground">Original</span>
                            <span className="font-medium text-right max-w-[180px] truncate">{currentProposal.transaction.originalDescription}</span>
                          </div>
                        </div>
                      </div>
                    ) : (
                      <p className="text-muted-foreground text-sm">Transaction data not available</p>
                    )}
                  </TabsContent>
                </Tabs>
              ) : (
                /* Desktop: Split pane comparison */
                <div className="grid md:grid-cols-2">
                  {/* Receipt Side */}
                  <div className="p-6 border-b md:border-b-0 md:border-r">
                    <div className="flex items-center gap-2 mb-4">
                      <Receipt className="h-5 w-5 text-muted-foreground" />
                      <h3 className="font-medium">Receipt</h3>
                    </div>
                    {currentProposal?.receipt ? (
                      <div className="space-y-4">
                        {/* Receipt Image */}
                        <div className="aspect-[3/4] max-h-64 rounded-lg overflow-hidden bg-muted flex items-center justify-center">
                          {currentProposal.receipt.thumbnailUrl ? (
                            <img
                              src={currentProposal.receipt.thumbnailUrl}
                              alt="Receipt"
                              className="w-full h-full object-contain"
                            />
                          ) : (
                            <Receipt className="h-12 w-12 text-muted-foreground/50" />
                          )}
                        </div>
                        {/* Receipt Details */}
                        <div className="space-y-2 text-sm">
                          <div className="flex justify-between">
                            <span className="text-muted-foreground">Merchant</span>
                            <span className="font-medium">
                              {currentProposal.receipt.vendorExtracted || 'Unknown'}
                            </span>
                          </div>
                          <div className="flex justify-between">
                            <span className="text-muted-foreground">Amount</span>
                            <span className="font-medium font-mono">
                              ${currentProposal.receipt.amountExtracted?.toFixed(2) || 'N/A'}
                            </span>
                          </div>
                          <div className="flex justify-between">
                            <span className="text-muted-foreground">Date</span>
                            <span className="font-medium">
                              {currentProposal.receipt.dateExtracted || 'Unknown'}
                            </span>
                          </div>
                        </div>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="w-full"
                          asChild
                        >
                          <a
                            href={`/receipts/${currentProposal.receipt.id}`}
                            target="_blank"
                            rel="noopener noreferrer"
                          >
                            <ExternalLink className="h-4 w-4 mr-1" />
                            View Full Receipt
                          </a>
                        </Button>
                      </div>
                    ) : (
                      <p className="text-muted-foreground text-sm">
                        Receipt data not available
                      </p>
                    )}
                  </div>

                  {/* Transaction/Group Side */}
                  <div className="p-6">
                    <div className="flex items-center gap-2 mb-4">
                      {currentProposal?.candidateType === 'group' ? (
                        <>
                          <Layers className="h-5 w-5 text-muted-foreground" />
                          <h3 className="font-medium">Transaction Group</h3>
                        </>
                      ) : (
                        <>
                          <CreditCard className="h-5 w-5 text-muted-foreground" />
                          <h3 className="font-medium">Transaction</h3>
                        </>
                      )}
                    </div>
                    {currentProposal?.candidateType === 'group' && currentProposal.transactionGroup ? (
                      <div className="space-y-4">
                        {/* Transaction Group Details */}
                        <div className="p-4 bg-muted/50 rounded-lg">
                          <div className="flex items-center gap-2 mb-2">
                            <p className="font-medium text-base">
                              {currentProposal.transactionGroup.name}
                            </p>
                            <span className="text-xs bg-primary/10 text-primary px-2 py-0.5 rounded-full">
                              {currentProposal.transactionGroup.transactionCount} transactions
                            </span>
                          </div>
                          <p className="text-2xl font-bold font-mono">
                            ${currentProposal.transactionGroup.combinedAmount.toFixed(2)}
                          </p>
                        </div>
                        <div className="space-y-2 text-sm">
                          <div className="flex justify-between">
                            <span className="text-muted-foreground">Display Date</span>
                            <span className="font-medium">
                              {new Date(currentProposal.transactionGroup.displayDate).toLocaleDateString()}
                            </span>
                          </div>
                          <div className="flex justify-between">
                            <span className="text-muted-foreground">Group ID</span>
                            <span className="font-medium font-mono text-xs">
                              {currentProposal.transactionGroup.id.slice(0, 8)}...
                            </span>
                          </div>
                        </div>
                      </div>
                    ) : currentProposal?.transaction ? (
                      <div className="space-y-4">
                        {/* Transaction Details */}
                        <div className="p-4 bg-muted/50 rounded-lg">
                          <p className="font-medium text-base mb-2">
                            {currentProposal.transaction.description}
                          </p>
                          <p className="text-2xl font-bold font-mono">
                            ${currentProposal.transaction.amount.toFixed(2)}
                          </p>
                        </div>
                        <div className="space-y-2 text-sm">
                          <div className="flex justify-between">
                            <span className="text-muted-foreground">Date</span>
                            <span className="font-medium">
                              {new Date(currentProposal.transaction.transactionDate).toLocaleDateString()}
                            </span>
                          </div>
                          {currentProposal.transaction.postDate && (
                            <div className="flex justify-between">
                              <span className="text-muted-foreground">Post Date</span>
                              <span className="font-medium">
                                {new Date(currentProposal.transaction.postDate).toLocaleDateString()}
                              </span>
                            </div>
                          )}
                          <div className="flex justify-between">
                            <span className="text-muted-foreground">Original</span>
                            <span className="font-medium text-right max-w-[200px] truncate">
                              {currentProposal.transaction.originalDescription}
                            </span>
                          </div>
                        </div>
                      </div>
                    ) : (
                      <p className="text-muted-foreground text-sm">
                        Transaction data not available
                      </p>
                    )}
                  </div>
                </div>
              )}

              <Separator />

              {/* Matching Factors */}
              <div className="p-6">
                <h3 className="font-medium mb-4">Match Confidence Breakdown</h3>
                {currentProposal && (
                  <MatchingFactors
                    factors={buildMatchingFactors(
                      currentProposal.amountScore,
                      currentProposal.dateScore,
                      currentProposal.vendorScore,
                      {
                        amount: currentProposal.receipt?.amountExtracted ?? undefined,
                        date: currentProposal.receipt?.dateExtracted ?? undefined,
                        vendor: currentProposal.receipt?.vendorExtracted ?? undefined,
                      },
                      currentProposal.candidateType === 'group' && currentProposal.transactionGroup
                        ? {
                            amount: currentProposal.transactionGroup.combinedAmount,
                            date: currentProposal.transactionGroup.displayDate,
                            description: currentProposal.transactionGroup.name,
                          }
                        : {
                            amount: currentProposal.transaction?.amount ?? 0,
                            date: currentProposal.transaction?.transactionDate ?? '',
                            description: currentProposal.transaction?.description ?? '',
                          }
                    )}
                    confidence={currentProposal.confidenceScore}
                    compact={false}
                  />
                )}
              </div>
            </CardContent>
          </Card>
        </motion.div>
      </AnimatePresence>

      {/* Action buttons */}
      <div className={cn(
        "flex items-center",
        isMobile ? "justify-center gap-4" : "justify-between"
      )}>
        {/* Desktop Navigation */}
        {!isMobile && (
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="icon"
              onClick={goToPrevious}
              disabled={pendingProposals.length <= 1}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="icon"
              onClick={goToNext}
              disabled={pendingProposals.length <= 1}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        )}

        {/* Actions */}
        <div className={cn(
          "flex items-center",
          isMobile ? "gap-4 w-full" : "gap-3"
        )}>
          <Button
            variant="outline"
            onClick={handleReject}
            disabled={isProcessing || !currentProposal}
            className={cn(
              "text-rose-600 hover:text-rose-700 hover:bg-rose-50 dark:hover:bg-rose-950/30",
              isMobile ? "flex-1 h-14 text-base" : ""
            )}
            size={isMobile ? "lg" : "lg"}
          >
            {isProcessing ? (
              <Loader2 className="h-5 w-5 mr-2 animate-spin" />
            ) : (
              <X className="h-5 w-5 mr-2" />
            )}
            Reject
            {!isMobile && <kbd className="ml-2 px-1.5 py-0.5 text-xs bg-muted rounded">R</kbd>}
          </Button>
          <Button
            onClick={handleApprove}
            disabled={isProcessing || !currentProposal}
            className={cn(
              "bg-emerald-600 hover:bg-emerald-700",
              isMobile ? "flex-1 h-14 text-base" : ""
            )}
            size={isMobile ? "lg" : "lg"}
          >
            {isProcessing ? (
              <Loader2 className="h-5 w-5 mr-2 animate-spin" />
            ) : (
              <Check className="h-5 w-5 mr-2" />
            )}
            Approve
            {!isMobile && <kbd className="ml-2 px-1.5 py-0.5 text-xs bg-emerald-700 rounded">A</kbd>}
          </Button>
        </div>
      </div>

      {/* Manual Match Dialog */}
      <ManualMatchDialog
        open={manualMatchOpen}
        onOpenChange={setManualMatchOpen}
        receiptId={currentProposal?.receipt?.id}
      />
    </div>
  )
}
