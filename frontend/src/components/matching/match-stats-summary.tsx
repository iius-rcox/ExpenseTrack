"use client"

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Progress } from '@/components/ui/progress'
import { Skeleton } from '@/components/ui/skeleton'
import type { MatchingStats } from '@/types/api'
import { CheckCircle2, Clock, Receipt, CreditCard, TrendingUp } from 'lucide-react'

interface MatchStatsSummaryProps {
  stats: MatchingStats | undefined
  isLoading?: boolean
}

export function MatchStatsSummary({ stats, isLoading }: MatchStatsSummaryProps) {
  if (isLoading) {
    return <MatchStatsSummarySkeleton />
  }

  if (!stats) {
    return null
  }

  const items = [
    {
      label: 'Matched',
      value: stats.matchedCount,
      icon: CheckCircle2,
      color: 'text-green-500',
      description: 'Confirmed matches',
    },
    {
      label: 'Pending',
      value: stats.proposedCount,
      icon: Clock,
      color: 'text-yellow-500',
      description: 'Awaiting review',
    },
    {
      label: 'Unmatched Receipts',
      value: stats.unmatchedReceiptsCount,
      icon: Receipt,
      color: 'text-muted-foreground',
      description: 'Need matching',
    },
    {
      label: 'Unmatched Transactions',
      value: stats.unmatchedTransactionsCount,
      icon: CreditCard,
      color: 'text-muted-foreground',
      description: 'Need matching',
    },
  ]

  return (
    <div className="space-y-6">
      {/* Stats Grid */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {items.map((item) => (
          <Card key={item.label}>
            <CardContent className="pt-6">
              <div className="flex items-center gap-2">
                <item.icon className={`h-4 w-4 ${item.color}`} />
                <span className="text-sm text-muted-foreground">{item.label}</span>
              </div>
              <p className="text-2xl font-bold mt-2">{item.value}</p>
              <p className="text-xs text-muted-foreground mt-1">{item.description}</p>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Performance Metrics */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base flex items-center gap-2">
            <TrendingUp className="h-4 w-4" />
            Matching Performance
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <div className="flex justify-between text-sm">
              <span className="text-muted-foreground">Auto-Match Rate</span>
              <span className="font-medium">{Math.round(stats.autoMatchRate * 100)}%</span>
            </div>
            <Progress value={stats.autoMatchRate * 100} className="h-2" />
          </div>
          <div className="space-y-2">
            <div className="flex justify-between text-sm">
              <span className="text-muted-foreground">Average Confidence</span>
              <span className="font-medium">{Math.round(stats.averageConfidence * 100)}%</span>
            </div>
            <Progress value={stats.averageConfidence * 100} className="h-2" />
          </div>
        </CardContent>
      </Card>
    </div>
  )
}

function MatchStatsSummarySkeleton() {
  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <Card key={i}>
            <CardContent className="pt-6">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-8 w-16 mt-2" />
              <Skeleton className="h-3 w-20 mt-1" />
            </CardContent>
          </Card>
        ))}
      </div>
      <Card>
        <CardHeader>
          <Skeleton className="h-5 w-40" />
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <div className="flex justify-between">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 w-12" />
            </div>
            <Skeleton className="h-2 w-full" />
          </div>
          <div className="space-y-2">
            <div className="flex justify-between">
              <Skeleton className="h-4 w-32" />
              <Skeleton className="h-4 w-12" />
            </div>
            <Skeleton className="h-2 w-full" />
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
