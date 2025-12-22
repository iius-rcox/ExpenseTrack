"use client"

/**
 * BatchReviewPanel Component (T071)
 *
 * Panel for batch approval of matches based on confidence threshold.
 * Allows users to approve all matches above a certain confidence level.
 */

import { useState, useMemo } from 'react'
import { motion } from 'framer-motion'
import { cn } from '@/lib/utils'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
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
import { ConfidenceIndicator } from '@/components/design-system/confidence-indicator'
import { staggerChild, staggerContainer } from '@/lib/animations'
import type { MatchProposal } from '@/types/api'
import {
  CheckCheck,
  XCircle,
  AlertTriangle,
  TrendingUp,
  Loader2,
} from 'lucide-react'

interface BatchReviewPanelProps {
  proposals: MatchProposal[]
  threshold: number
  onThresholdChange: (value: number) => void
  onApproveAll: (minConfidence: number) => void
  onRejectAll?: (ids: string[]) => void
  isProcessing?: boolean
}

/**
 * Threshold presets for quick selection
 */
const THRESHOLD_PRESETS = [
  { label: 'Strict', value: 0.95, description: 'Only near-perfect matches' },
  { label: 'Recommended', value: 0.9, description: 'High confidence matches' },
  { label: 'Moderate', value: 0.8, description: 'Good confidence matches' },
  { label: 'Relaxed', value: 0.7, description: 'Includes lower confidence' },
] as const

/**
 * Get color class based on threshold
 */
function getThresholdColor(threshold: number): string {
  if (threshold >= 0.9) return 'text-emerald-600 dark:text-emerald-400'
  if (threshold >= 0.8) return 'text-amber-600 dark:text-amber-400'
  return 'text-rose-600 dark:text-rose-400'
}

export function BatchReviewPanel({
  proposals,
  threshold,
  onThresholdChange,
  onApproveAll,
  onRejectAll,
  isProcessing = false,
}: BatchReviewPanelProps) {
  const [confirmAction, setConfirmAction] = useState<'approve' | 'reject' | null>(null)

  // Filter pending proposals
  const pendingProposals = proposals.filter((p) => p.status === 'Proposed')

  // Calculate counts based on threshold
  const counts = useMemo(() => {
    const aboveThreshold = pendingProposals.filter(
      (p) => p.confidenceScore >= threshold
    )
    const belowThreshold = pendingProposals.filter(
      (p) => p.confidenceScore < threshold
    )

    // Calculate average confidence for eligible matches
    const avgConfidence =
      aboveThreshold.length > 0
        ? aboveThreshold.reduce((sum, p) => sum + p.confidenceScore, 0) /
          aboveThreshold.length
        : 0

    return {
      total: pendingProposals.length,
      eligible: aboveThreshold.length,
      ineligible: belowThreshold.length,
      avgConfidence,
      eligibleIds: aboveThreshold.map((p) => p.matchId),
      ineligibleIds: belowThreshold.map((p) => p.matchId),
    }
  }, [pendingProposals, threshold])

  const handleApprove = () => {
    onApproveAll(threshold)
    setConfirmAction(null)
  }

  const handleReject = () => {
    if (onRejectAll) {
      onRejectAll(counts.ineligibleIds)
    }
    setConfirmAction(null)
  }

  if (pendingProposals.length === 0) {
    return null
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="text-lg">Batch Review</CardTitle>
            <CardDescription>
              Approve or reject multiple matches at once
            </CardDescription>
          </div>
          <Badge variant="secondary" className="text-sm font-mono">
            {counts.total} pending
          </Badge>
        </div>
      </CardHeader>

      <CardContent className="space-y-6">
        {/* Threshold Slider */}
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <label className="text-sm font-medium">Confidence Threshold</label>
            <span className={cn('text-lg font-bold font-mono', getThresholdColor(threshold))}>
              {Math.round(threshold * 100)}%
            </span>
          </div>

          <input
            type="range"
            value={threshold * 100}
            onChange={(e) => onThresholdChange(Number(e.target.value) / 100)}
            min={50}
            max={99}
            step={1}
            disabled={isProcessing}
            className="w-full h-2 bg-muted rounded-lg appearance-none cursor-pointer accent-primary"
          />

          {/* Threshold Presets */}
          <div className="flex flex-wrap gap-2">
            {THRESHOLD_PRESETS.map((preset) => (
              <Button
                key={preset.value}
                variant={threshold === preset.value ? 'default' : 'outline'}
                size="sm"
                onClick={() => onThresholdChange(preset.value)}
                disabled={isProcessing}
              >
                {preset.label}
              </Button>
            ))}
          </div>
        </div>

        <Separator />

        {/* Results Preview */}
        <motion.div
          variants={staggerContainer}
          initial="hidden"
          animate="visible"
          className="grid grid-cols-2 gap-4"
        >
          {/* Eligible for approval */}
          <motion.div
            variants={staggerChild}
            className="p-4 rounded-lg bg-emerald-50 dark:bg-emerald-950/30 border border-emerald-200 dark:border-emerald-900"
          >
            <div className="flex items-center gap-2 mb-2">
              <CheckCheck className="h-5 w-5 text-emerald-600" />
              <span className="font-medium text-emerald-700 dark:text-emerald-300">
                Will Approve
              </span>
            </div>
            <p className="text-3xl font-bold text-emerald-600 font-mono">
              {counts.eligible}
            </p>
            <p className="text-sm text-emerald-600/70">
              matches ≥ {Math.round(threshold * 100)}%
            </p>
            {counts.eligible > 0 && (
              <div className="mt-2 flex items-center gap-2 text-xs text-emerald-600/70">
                <TrendingUp className="h-3 w-3" />
                Avg: {Math.round(counts.avgConfidence * 100)}%
              </div>
            )}
          </motion.div>

          {/* Below threshold */}
          <motion.div
            variants={staggerChild}
            className="p-4 rounded-lg bg-muted/50 border border-muted"
          >
            <div className="flex items-center gap-2 mb-2">
              <AlertTriangle className="h-5 w-5 text-muted-foreground" />
              <span className="font-medium text-muted-foreground">
                Need Review
              </span>
            </div>
            <p className="text-3xl font-bold text-muted-foreground font-mono">
              {counts.ineligible}
            </p>
            <p className="text-sm text-muted-foreground/70">
              matches &lt; {Math.round(threshold * 100)}%
            </p>
          </motion.div>
        </motion.div>

        {/* Match Preview (compact list of eligible matches) */}
        {counts.eligible > 0 && counts.eligible <= 10 && (
          <>
            <Separator />
            <div className="space-y-2">
              <h4 className="text-sm font-medium text-muted-foreground">
                Matches to approve:
              </h4>
              <div className="max-h-48 overflow-y-auto space-y-2">
                {pendingProposals
                  .filter((p) => p.confidenceScore >= threshold)
                  .map((proposal) => (
                    <div
                      key={proposal.matchId}
                      className="flex items-center justify-between p-2 rounded bg-muted/30 text-sm"
                    >
                      <div className="flex items-center gap-2 min-w-0">
                        <ConfidenceIndicator
                          score={proposal.confidenceScore}
                          size="sm"
                        />
                        <span className="truncate">
                          {proposal.receipt?.vendorExtracted || 'Unknown'} → {proposal.transaction?.description}
                        </span>
                      </div>
                      <Badge variant="outline" className="font-mono text-xs">
                        ${proposal.transaction?.amount?.toFixed(2)}
                      </Badge>
                    </div>
                  ))}
              </div>
            </div>
          </>
        )}

        <Separator />

        {/* Action Buttons */}
        <div className="flex items-center justify-between gap-4">
          {/* Reject all below threshold */}
          {onRejectAll && counts.ineligible > 0 && (
            <AlertDialog
              open={confirmAction === 'reject'}
              onOpenChange={(open) => !open && setConfirmAction(null)}
            >
              <AlertDialogTrigger asChild>
                <Button
                  variant="outline"
                  onClick={() => setConfirmAction('reject')}
                  disabled={isProcessing}
                  className="text-rose-600 hover:text-rose-700"
                >
                  <XCircle className="h-4 w-4 mr-2" />
                  Reject {counts.ineligible} Low Confidence
                </Button>
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>Reject Low Confidence Matches?</AlertDialogTitle>
                  <AlertDialogDescription>
                    This will reject {counts.ineligible} matches with confidence below{' '}
                    {Math.round(threshold * 100)}%. This action cannot be undone.
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel>Cancel</AlertDialogCancel>
                  <AlertDialogAction
                    onClick={handleReject}
                    className="bg-rose-600 hover:bg-rose-700"
                  >
                    Reject All
                  </AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>
          )}

          {/* Approve all above threshold */}
          <AlertDialog
            open={confirmAction === 'approve'}
            onOpenChange={(open) => !open && setConfirmAction(null)}
          >
            <AlertDialogTrigger asChild>
              <Button
                onClick={() => setConfirmAction('approve')}
                disabled={isProcessing || counts.eligible === 0}
                className="bg-emerald-600 hover:bg-emerald-700 ml-auto"
              >
                {isProcessing ? (
                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                ) : (
                  <CheckCheck className="h-4 w-4 mr-2" />
                )}
                Approve {counts.eligible} Matches
              </Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>Approve {counts.eligible} Matches?</AlertDialogTitle>
                <AlertDialogDescription>
                  This will approve all matches with confidence of{' '}
                  {Math.round(threshold * 100)}% or higher. Each receipt will be
                  linked to its matched transaction.
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>Cancel</AlertDialogCancel>
                <AlertDialogAction
                  onClick={handleApprove}
                  className="bg-emerald-600 hover:bg-emerald-700"
                >
                  Approve All
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </div>
      </CardContent>
    </Card>
  )
}
