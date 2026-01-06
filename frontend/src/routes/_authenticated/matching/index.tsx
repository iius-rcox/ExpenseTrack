"use client"

/**
 * Matching Page (T073)
 *
 * Match review interface with two modes:
 * - Review Mode: Keyboard-driven split-pane workspace for efficient review
 * - List Mode: Traditional list view for browsing all matches
 *
 * Supports batch operations with confidence threshold filtering.
 */

import { useState } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { z } from 'zod'
import {
  useMatchProposals,
  useMatchingStats,
  useTriggerAutoMatch,
  useConfirmMatch,
  useRejectMatch,
  useBatchApprove,
} from '@/hooks/queries/use-matching'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { MatchReviewWorkspace } from '@/components/matching/match-review-workspace'
import { BatchReviewPanel } from '@/components/matching/batch-review-panel'
import { MatchProposalCard } from '@/components/matching/match-proposal-card'
import { MatchStatsSummary } from '@/components/matching/match-stats-summary'
import { ManualMatchDialog } from '@/components/matching/manual-match-dialog'
import { MissingReceiptsWidget } from '@/components/missing-receipts/missing-receipts-widget'
import { toast } from 'sonner'
import {
  GitCompareArrows,
  RefreshCcw,
  Clock,
  CheckCircle2,
  XCircle,
  ChevronLeft,
  ChevronRight,
  Loader2,
  Zap,
  LinkIcon,
  ListFilter,
  ScanSearch,
} from 'lucide-react'

const matchingSearchSchema = z.object({
  page: z.coerce.number().optional().default(1),
  pageSize: z.coerce.number().optional().default(20),
  status: z.enum(['Proposed', 'Confirmed', 'Rejected', '']).optional(),
  mode: z.enum(['review', 'list']).optional().default('review'),
})

export const Route = createFileRoute('/_authenticated/matching/')({
  validateSearch: matchingSearchSchema,
  component: MatchingPage,
})

function MatchingPage() {
  const search = Route.useSearch()
  const navigate = Route.useNavigate()
  const [activeTab, setActiveTab] = useState<string>(search.status || 'Proposed')
  const [batchThreshold, setBatchThreshold] = useState(0.9)
  const [reviewIndex, setReviewIndex] = useState(0)

  const { data: stats, isLoading: loadingStats } = useMatchingStats()
  const { data: proposals, isLoading: loadingProposals, refetch } = useMatchProposals({
    page: search.page,
    pageSize: search.pageSize,
    status: activeTab || undefined,
  })
  const { mutate: triggerAutoMatch, isPending: isAutoMatching } = useTriggerAutoMatch()
  const confirmMatch = useConfirmMatch()
  const rejectMatch = useRejectMatch()
  const batchApprove = useBatchApprove()

  const isProcessing = confirmMatch.isPending || rejectMatch.isPending || batchApprove.isPending

  const handleTabChange = (status: string) => {
    setActiveTab(status)
    setReviewIndex(0)
    navigate({
      search: {
        ...search,
        status: status as 'Proposed' | 'Confirmed' | 'Rejected' | '' | undefined,
        page: 1,
      },
    })
  }

  const handleModeChange = (mode: 'review' | 'list') => {
    navigate({
      search: {
        ...search,
        mode,
      },
    })
  }

  const handlePageChange = (newPage: number) => {
    navigate({
      search: {
        ...search,
        page: newPage,
      },
    })
  }

  const handleAutoMatch = () => {
    triggerAutoMatch(undefined, {
      onSuccess: (result) => {
        toast.success(
          `Auto-match complete: ${result.proposedCount} new proposals from ${result.processedCount} processed items`
        )
        refetch()
      },
      onError: (error) => {
        toast.error(`Auto-match failed: ${error.message}`)
      },
    })
  }

  const handleConfirm = (matchId: string) => {
    confirmMatch.mutate(
      { matchId },
      {
        onSuccess: () => refetch(),
        onError: (error) => toast.error(`Failed to confirm: ${error.message}`),
      }
    )
  }

  const handleReject = (matchId: string) => {
    rejectMatch.mutate(matchId, {
      onSuccess: () => refetch(),
      onError: (error) => toast.error(`Failed to reject: ${error.message}`),
    })
  }

  const handleBatchApprove = (minConfidence: number) => {
    batchApprove.mutate(
      { minConfidence },
      {
        onSuccess: (result) => {
          toast.success(`Approved ${result.approved} matches`)
          refetch()
        },
        onError: (error) => toast.error(`Batch approve failed: ${error.message}`),
      }
    )
  }

  const totalPages = proposals
    ? Math.ceil(proposals.totalCount / search.pageSize)
    : 0

  const pendingProposals = proposals?.items.filter((p) => p.status === 'Proposed') ?? []
  const isReviewMode = search.mode === 'review' && activeTab === 'Proposed'

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Match Review</h1>
          <p className="text-muted-foreground">
            Review and confirm AI-suggested matches between receipts and transactions
          </p>
        </div>
        <div className="flex gap-2">
          <ManualMatchDialog
            trigger={
              <Button variant="outline">
                <LinkIcon className="mr-2 h-4 w-4" />
                Manual Match
              </Button>
            }
          />
          <Button onClick={handleAutoMatch} disabled={isAutoMatching}>
            {isAutoMatching ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Zap className="mr-2 h-4 w-4" />
            )}
            Run Auto-Match
          </Button>
        </div>
      </div>

      {/* Stats Summary with Missing Receipts Widget */}
      <div className="grid gap-6 lg:grid-cols-[1fr,300px]">
        <MatchStatsSummary stats={stats} isLoading={loadingStats} />
        <MissingReceiptsWidget
          onQuickUpload={(_transactionId) => {
            // Quick upload integration will be added in T024 (User Story 3)
            toast.info('Quick upload coming in next update')
          }}
        />
      </div>

      {/* View Mode Toggle - Only show for Proposed tab */}
      {activeTab === 'Proposed' && (
        <div className="flex items-center gap-2">
          <Button
            variant={search.mode === 'review' ? 'default' : 'outline'}
            size="sm"
            onClick={() => handleModeChange('review')}
          >
            <ScanSearch className="mr-2 h-4 w-4" />
            Review Mode
          </Button>
          <Button
            variant={search.mode === 'list' ? 'default' : 'outline'}
            size="sm"
            onClick={() => handleModeChange('list')}
          >
            <ListFilter className="mr-2 h-4 w-4" />
            List Mode
          </Button>
        </div>
      )}

      {/* Proposals Section */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <Tabs value={activeTab} onValueChange={handleTabChange}>
            <TabsList>
              <TabsTrigger value="Proposed">
                <Clock className="mr-2 h-4 w-4" />
                Pending
                {stats && (
                  <Badge variant="secondary" className="ml-2">
                    {stats.proposedCount}
                  </Badge>
                )}
              </TabsTrigger>
              <TabsTrigger value="Confirmed">
                <CheckCircle2 className="mr-2 h-4 w-4" />
                Confirmed
              </TabsTrigger>
              <TabsTrigger value="Rejected">
                <XCircle className="mr-2 h-4 w-4" />
                Rejected
              </TabsTrigger>
            </TabsList>
          </Tabs>
          <Button
            variant="ghost"
            size="icon"
            onClick={() => refetch()}
            disabled={loadingProposals}
          >
            <RefreshCcw className={`h-4 w-4 ${loadingProposals ? 'animate-spin' : ''}`} />
          </Button>
        </div>

        {/* Review Mode Content */}
        {isReviewMode && !loadingProposals && pendingProposals.length > 0 && (
          <div className="grid lg:grid-cols-[1fr,320px] gap-6">
            <MatchReviewWorkspace
              proposals={proposals?.items ?? []}
              isLoading={loadingProposals}
              onConfirm={handleConfirm}
              onReject={handleReject}
              onBatchApprove={handleBatchApprove}
              isProcessing={isProcessing}
              currentIndex={reviewIndex}
              onIndexChange={setReviewIndex}
            />
            <BatchReviewPanel
              proposals={proposals?.items ?? []}
              threshold={batchThreshold}
              onThresholdChange={setBatchThreshold}
              onApproveAll={handleBatchApprove}
              isProcessing={isProcessing}
            />
          </div>
        )}

        {/* List Mode / Non-Proposed Tabs Content */}
        {(!isReviewMode || activeTab !== 'Proposed') && (
          <>
            {loadingProposals ? (
              <div className="space-y-4">
                {Array.from({ length: 3 }).map((_, i) => (
                  <Card key={i}>
                    <CardContent className="p-6">
                      <div className="space-y-4">
                        <div className="flex items-center justify-between">
                          <Skeleton className="h-6 w-20" />
                          <Skeleton className="h-8 w-24" />
                        </div>
                        <div className="grid grid-cols-2 gap-4">
                          <div className="space-y-2">
                            <Skeleton className="h-4 w-16" />
                            <Skeleton className="h-16 w-full" />
                          </div>
                          <div className="space-y-2">
                            <Skeleton className="h-4 w-20" />
                            <Skeleton className="h-16 w-full" />
                          </div>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            ) : proposals?.items.length === 0 ? (
              <Card>
                <CardContent className="flex flex-col items-center justify-center py-12">
                  <GitCompareArrows className="h-12 w-12 text-muted-foreground" />
                  <h3 className="mt-4 text-lg font-semibold">No matches found</h3>
                  <p className="text-sm text-muted-foreground mt-1">
                    {activeTab === 'Proposed'
                      ? 'No pending matches to review. Try running auto-match.'
                      : `No ${activeTab.toLowerCase()} matches yet.`}
                  </p>
                  {activeTab === 'Proposed' && (
                    <Button className="mt-4" onClick={handleAutoMatch} disabled={isAutoMatching}>
                      {isAutoMatching ? (
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      ) : (
                        <Zap className="mr-2 h-4 w-4" />
                      )}
                      Run Auto-Match
                    </Button>
                  )}
                </CardContent>
              </Card>
            ) : (
              <div className="space-y-4">
                {proposals?.items.map((proposal) => (
                  <MatchProposalCard
                    key={proposal.matchId}
                    proposal={proposal}
                    onConfirmed={() => refetch()}
                    onRejected={() => refetch()}
                  />
                ))}
              </div>
            )}

            {/* Pagination */}
            {proposals && proposals.totalCount > search.pageSize && (
              <div className="flex items-center justify-between">
                <p className="text-sm text-muted-foreground">
                  Showing {((search.page - 1) * search.pageSize) + 1} to{' '}
                  {Math.min(search.page * search.pageSize, proposals.totalCount)} of{' '}
                  {proposals.totalCount} proposals
                </p>
                <div className="flex items-center gap-2">
                  <Button
                    variant="outline"
                    size="icon"
                    onClick={() => handlePageChange(search.page - 1)}
                    disabled={search.page <= 1}
                  >
                    <ChevronLeft className="h-4 w-4" />
                  </Button>
                  <span className="text-sm">
                    Page {search.page} of {totalPages}
                  </span>
                  <Button
                    variant="outline"
                    size="icon"
                    onClick={() => handlePageChange(search.page + 1)}
                    disabled={search.page >= totalPages}
                  >
                    <ChevronRight className="h-4 w-4" />
                  </Button>
                </div>
              </div>
            )}
          </>
        )}

        {/* Empty state for Review Mode */}
        {isReviewMode && !loadingProposals && pendingProposals.length === 0 && (
          <Card>
            <CardContent className="flex flex-col items-center justify-center py-12">
              <GitCompareArrows className="h-12 w-12 text-muted-foreground" />
              <h3 className="mt-4 text-lg font-semibold">All caught up!</h3>
              <p className="text-sm text-muted-foreground mt-1">
                No pending matches to review. Try running auto-match.
              </p>
              <Button className="mt-4" onClick={handleAutoMatch} disabled={isAutoMatching}>
                {isAutoMatching ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <Zap className="mr-2 h-4 w-4" />
                )}
                Run Auto-Match
              </Button>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  )
}
