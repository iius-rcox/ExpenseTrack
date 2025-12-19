"use client"

import { useState } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { z } from 'zod'
import {
  useMatchProposals,
  useMatchingStats,
  useTriggerAutoMatch,
} from '@/hooks/queries/use-matching'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { MatchProposalCard } from '@/components/matching/match-proposal-card'
import { MatchStatsSummary } from '@/components/matching/match-stats-summary'
import { ManualMatchDialog } from '@/components/matching/manual-match-dialog'
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
} from 'lucide-react'

const matchingSearchSchema = z.object({
  page: z.coerce.number().optional().default(1),
  pageSize: z.coerce.number().optional().default(20),
  status: z.enum(['Proposed', 'Confirmed', 'Rejected', '']).optional(),
})

export const Route = createFileRoute('/_authenticated/matching/')({
  validateSearch: matchingSearchSchema,
  component: MatchingPage,
})

function MatchingPage() {
  const search = Route.useSearch()
  const navigate = Route.useNavigate()
  const [activeTab, setActiveTab] = useState<string>(search.status || 'Proposed')

  const { data: stats, isLoading: loadingStats } = useMatchingStats()
  const { data: proposals, isLoading: loadingProposals, refetch } = useMatchProposals({
    page: search.page,
    pageSize: search.pageSize,
    status: activeTab || undefined,
  })
  const { mutate: triggerAutoMatch, isPending: isAutoMatching } = useTriggerAutoMatch()

  const handleTabChange = (status: string) => {
    setActiveTab(status)
    navigate({
      search: {
        ...search,
        status: status as 'Proposed' | 'Confirmed' | 'Rejected' | '' | undefined,
        page: 1,
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

  const totalPages = proposals
    ? Math.ceil(proposals.totalCount / search.pageSize)
    : 0

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

      {/* Stats Summary */}
      <MatchStatsSummary stats={stats} isLoading={loadingStats} />

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

        {/* Proposals List */}
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
      </div>
    </div>
  )
}
