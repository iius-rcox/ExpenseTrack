"use client"

import { useState } from 'react'
import { Card, CardContent, CardHeader } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { useConfirmMatch, useRejectMatch } from '@/hooks/queries/use-matching'
import { formatCurrency, formatDate } from '@/lib/utils'
import { toast } from 'sonner'
import type { MatchProposal } from '@/types/api'
import {
  Check,
  X,
  Receipt,
  CreditCard,
  Calendar,
  DollarSign,
  Loader2,
  Info,
} from 'lucide-react'

interface MatchProposalCardProps {
  proposal: MatchProposal
  onConfirmed?: () => void
  onRejected?: () => void
}

export function MatchProposalCard({ proposal, onConfirmed, onRejected }: MatchProposalCardProps) {
  const [isExpanded, setIsExpanded] = useState(false)
  const { mutate: confirmMatch, isPending: isConfirming } = useConfirmMatch()
  const { mutate: rejectMatch, isPending: isRejecting } = useRejectMatch()

  const isPending = isConfirming || isRejecting

  const handleConfirm = () => {
    confirmMatch(
      { matchId: proposal.matchId },
      {
        onSuccess: () => {
          toast.success('Match confirmed successfully')
          onConfirmed?.()
        },
        onError: (error) => {
          toast.error(`Failed to confirm match: ${error.message}`)
        },
      }
    )
  }

  const handleReject = () => {
    rejectMatch(proposal.matchId, {
      onSuccess: () => {
        toast.success('Match rejected')
        onRejected?.()
      },
      onError: (error) => {
        toast.error(`Failed to reject match: ${error.message}`)
      },
    })
  }

  const getConfidenceColor = (score: number) => {
    if (score >= 0.9) return 'text-green-500'
    if (score >= 0.7) return 'text-yellow-500'
    return 'text-red-500'
  }

  return (
    <Card className="transition-all hover:shadow-md">
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Badge
              variant={proposal.status === 'Proposed' ? 'outline' : 'default'}
              className={proposal.status === 'Confirmed' ? 'bg-green-500' : ''}
            >
              {proposal.status}
            </Badge>
            <TooltipProvider>
              <Tooltip>
                <TooltipTrigger asChild>
                  <div className="flex items-center gap-1 text-sm">
                    <span className={getConfidenceColor(proposal.confidenceScore)}>
                      {Math.round(proposal.confidenceScore * 100)}%
                    </span>
                    <span className="text-muted-foreground">confidence</span>
                    <Info className="h-3 w-3 text-muted-foreground" />
                  </div>
                </TooltipTrigger>
                <TooltipContent>
                  <div className="space-y-1 text-xs">
                    <p>Amount: {Math.round(proposal.amountScore * 100)}%</p>
                    <p>Date: {Math.round(proposal.dateScore * 100)}%</p>
                    <p>Vendor: {Math.round(proposal.vendorScore * 100)}%</p>
                  </div>
                </TooltipContent>
              </Tooltip>
            </TooltipProvider>
          </div>
          <div className="flex gap-2">
            {proposal.status === 'Proposed' && (
              <>
                <Button
                  size="sm"
                  variant="outline"
                  onClick={handleReject}
                  disabled={isPending}
                  className="h-8"
                >
                  {isRejecting ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    <X className="h-4 w-4" />
                  )}
                  <span className="sr-only">Reject</span>
                </Button>
                <Button
                  size="sm"
                  onClick={handleConfirm}
                  disabled={isPending}
                  className="h-8"
                >
                  {isConfirming ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    <Check className="h-4 w-4" />
                  )}
                  <span className="ml-1">Confirm</span>
                </Button>
              </>
            )}
          </div>
        </div>
      </CardHeader>
      <CardContent className="pt-0">
        <div className="grid grid-cols-2 gap-4">
          {/* Receipt Side */}
          <div className="space-y-3">
            <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
              <Receipt className="h-4 w-4" />
              Receipt
            </div>
            {proposal.receipt ? (
              <div className="space-y-2">
                <div className="flex items-start gap-3">
                  <div className="h-16 w-16 rounded bg-muted flex-shrink-0 flex items-center justify-center overflow-hidden">
                    {proposal.receipt.thumbnailUrl ? (
                      <img
                        src={proposal.receipt.thumbnailUrl}
                        alt="Receipt"
                        className="h-full w-full object-cover"
                      />
                    ) : (
                      <Receipt className="h-6 w-6 text-muted-foreground" />
                    )}
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="font-medium truncate">
                      {proposal.receipt.vendorExtracted || 'Unknown vendor'}
                    </p>
                    <div className="flex items-center gap-1 text-sm text-muted-foreground">
                      <Calendar className="h-3 w-3" />
                      {proposal.receipt.dateExtracted
                        ? formatDate(proposal.receipt.dateExtracted)
                        : 'No date'}
                    </div>
                    <div className="flex items-center gap-1 text-sm font-medium">
                      <DollarSign className="h-3 w-3" />
                      {proposal.receipt.amountExtracted != null
                        ? formatCurrency(proposal.receipt.amountExtracted)
                        : 'No amount'}
                    </div>
                  </div>
                </div>
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">Receipt data not available</p>
            )}
          </div>

          <Separator orientation="vertical" className="mx-auto hidden lg:block" />

          {/* Transaction Side */}
          <div className="space-y-3">
            <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
              <CreditCard className="h-4 w-4" />
              Transaction
            </div>
            {proposal.transaction ? (
              <div className="space-y-2">
                <p className="font-medium line-clamp-2">{proposal.transaction.description}</p>
                <div className="flex items-center gap-1 text-sm text-muted-foreground">
                  <Calendar className="h-3 w-3" />
                  {formatDate(proposal.transaction.transactionDate)}
                </div>
                <div className="flex items-center gap-1 text-sm font-medium">
                  <DollarSign className="h-3 w-3" />
                  {formatCurrency(proposal.transaction.amount)}
                </div>
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">Transaction data not available</p>
            )}
          </div>
        </div>

        {/* Expanded Details */}
        {isExpanded && (
          <>
            <Separator className="my-4" />
            <div className="space-y-2 text-sm">
              <p className="text-muted-foreground">
                <span className="font-medium">Match Reason:</span> {proposal.matchReason}
              </p>
              <p className="text-muted-foreground">
                <span className="font-medium">Created:</span> {formatDate(proposal.createdAt)}
              </p>
            </div>
          </>
        )}

        <Button
          variant="ghost"
          size="sm"
          className="mt-3 w-full"
          onClick={() => setIsExpanded(!isExpanded)}
        >
          {isExpanded ? 'Show Less' : 'Show More'}
        </Button>
      </CardContent>
    </Card>
  )
}
