'use client'

/**
 * MerchantAnalytics Component (T081)
 *
 * Displays top merchants and spending patterns by vendor.
 * Shows trends, transaction counts, and comparison to previous periods.
 */

import { useMemo, useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { fadeIn, staggerContainer, staggerChild } from '@/lib/animations'
import { cn } from '@/lib/utils'
import {
  TrendingUp,
  TrendingDown,
  Minus,
  Store,
  Search,
  ArrowUpDown,
  Sparkles,
} from 'lucide-react'
import type { TopMerchant, MerchantAnalyticsResponse } from '@/types/analytics'

// Format currency
function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(value)
}

type SortOption = 'amount' | 'count' | 'change' | 'average'

interface MerchantAnalyticsProps {
  data?: MerchantAnalyticsResponse
  isLoading?: boolean
  topCount?: number
  showTrends?: boolean
  showNewMerchants?: boolean
  className?: string
  title?: string
  onMerchantSelect?: (merchant: TopMerchant) => void
}

// Trend indicator component
function TrendIndicator({
  changePercent,
  size = 'sm',
}: {
  changePercent?: number
  size?: 'sm' | 'md'
}) {
  if (changePercent === undefined) return null

  const isUp = changePercent > 0
  const isNeutral = Math.abs(changePercent) < 1

  const Icon = isNeutral ? Minus : isUp ? TrendingUp : TrendingDown
  const colorClass = isNeutral
    ? 'text-slate-500'
    : isUp
    ? 'text-rose-500'
    : 'text-emerald-500'

  const sizeClass = size === 'sm' ? 'h-3 w-3' : 'h-4 w-4'
  const textClass = size === 'sm' ? 'text-xs' : 'text-sm'

  return (
    <div className={cn('flex items-center gap-0.5', colorClass)}>
      <Icon className={sizeClass} />
      <span className={cn('font-mono', textClass)}>
        {Math.abs(changePercent).toFixed(1)}%
      </span>
    </div>
  )
}

// Individual merchant card
function MerchantCard({
  merchant,
  rank,
  showTrend,
  onClick,
}: {
  merchant: TopMerchant
  rank: number
  showTrend: boolean
  onClick?: () => void
}) {
  const TrendIcon =
    merchant.trend === 'increasing'
      ? TrendingUp
      : merchant.trend === 'decreasing'
      ? TrendingDown
      : Minus

  return (
    <motion.div
      variants={staggerChild}
      className={cn(
        'flex items-center gap-3 rounded-lg border p-4 transition-colors',
        onClick && 'cursor-pointer hover:bg-muted/50'
      )}
      onClick={onClick}
    >
      {/* Rank badge */}
      <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-muted text-sm font-bold">
        {rank}
      </div>

      {/* Merchant info */}
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <Store className="h-4 w-4 text-muted-foreground shrink-0" />
          <span className="font-medium truncate">
            {merchant.displayName || merchant.merchantName}
          </span>
          {merchant.trend && showTrend && (
            <TrendIcon
              className={cn(
                'h-3 w-3 shrink-0',
                merchant.trend === 'increasing'
                  ? 'text-rose-500'
                  : merchant.trend === 'decreasing'
                  ? 'text-emerald-500'
                  : 'text-slate-400'
              )}
            />
          )}
        </div>
        <div className="mt-1 flex items-center gap-3 text-sm text-muted-foreground">
          <span>{merchant.transactionCount} transactions</span>
          <span>avg {formatCurrency(merchant.averageAmount)}</span>
          {merchant.primaryCategory && (
            <Badge variant="outline" className="text-xs">
              {merchant.primaryCategory}
            </Badge>
          )}
        </div>
      </div>

      {/* Amount and change */}
      <div className="text-right shrink-0">
        <p className="font-mono text-lg font-semibold">
          {formatCurrency(merchant.totalAmount)}
        </p>
        <div className="flex items-center justify-end gap-2">
          <span className="text-xs text-muted-foreground">
            {merchant.percentageOfTotal.toFixed(1)}%
          </span>
          {showTrend && <TrendIndicator changePercent={merchant.changePercent} />}
        </div>
      </div>
    </motion.div>
  )
}

// New merchant badge list
function NewMerchantsList({ merchants }: { merchants: TopMerchant[] }) {
  if (!merchants.length) return null

  return (
    <div className="space-y-2">
      <div className="flex items-center gap-2 text-sm font-medium text-muted-foreground">
        <Sparkles className="h-4 w-4" />
        New Merchants
      </div>
      <div className="flex flex-wrap gap-2">
        {merchants.slice(0, 5).map((merchant) => (
          <Badge key={merchant.merchantName} variant="secondary" className="gap-1">
            <Store className="h-3 w-3" />
            {merchant.displayName || merchant.merchantName}
            <span className="text-xs opacity-75">
              {formatCurrency(merchant.totalAmount)}
            </span>
          </Badge>
        ))}
        {merchants.length > 5 && (
          <Badge variant="outline">+{merchants.length - 5} more</Badge>
        )}
      </div>
    </div>
  )
}

export function MerchantAnalytics({
  data,
  isLoading = false,
  topCount = 10,
  showTrends = true,
  showNewMerchants = true,
  className,
  title = 'Top Merchants',
  onMerchantSelect,
}: MerchantAnalyticsProps) {
  const [searchQuery, setSearchQuery] = useState('')
  const [sortBy, setSortBy] = useState<SortOption>('amount')

  // Filter and sort merchants
  const merchants = useMemo(() => {
    if (!data?.topMerchants) return []

    let filtered = data.topMerchants

    // Apply search filter
    if (searchQuery) {
      const query = searchQuery.toLowerCase()
      filtered = filtered.filter(
        (m) =>
          m.merchantName.toLowerCase().includes(query) ||
          m.displayName?.toLowerCase().includes(query) ||
          m.primaryCategory?.toLowerCase().includes(query)
      )
    }

    // Apply sort
    const sorted = [...filtered].sort((a, b) => {
      switch (sortBy) {
        case 'count':
          return b.transactionCount - a.transactionCount
        case 'change':
          return (b.changePercent || 0) - (a.changePercent || 0)
        case 'average':
          return b.averageAmount - a.averageAmount
        case 'amount':
        default:
          return b.totalAmount - a.totalAmount
      }
    })

    return sorted.slice(0, topCount)
  }, [data?.topMerchants, searchQuery, sortBy, topCount])

  // Summary stats
  const summary = useMemo(() => {
    if (!data?.topMerchants?.length) return null

    const total = data.topMerchants.reduce((sum, m) => sum + m.totalAmount, 0)
    const avgPerMerchant = total / data.topMerchants.length
    const topThreeTotal = data.topMerchants
      .slice(0, 3)
      .reduce((sum, m) => sum + m.totalAmount, 0)
    const topThreePercent = (topThreeTotal / total) * 100

    return { total, avgPerMerchant, topThreePercent }
  }, [data?.topMerchants])

  if (isLoading) {
    return (
      <Card className={className}>
        <CardHeader>
          <Skeleton className="h-6 w-40" />
        </CardHeader>
        <CardContent className="space-y-3">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-20 w-full" />
          ))}
        </CardContent>
      </Card>
    )
  }

  if (!data?.topMerchants?.length) {
    return (
      <Card className={className}>
        <CardHeader>
          <CardTitle className="text-lg">{title}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col items-center justify-center py-8 text-center">
            <Store className="h-12 w-12 text-muted-foreground/50" />
            <p className="mt-3 text-muted-foreground">No merchant data available</p>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <motion.div variants={fadeIn} initial="hidden" animate="visible">
      <Card className={className}>
        <CardHeader className="pb-3">
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="text-lg">{title}</CardTitle>
              {summary && (
                <p className="text-sm text-muted-foreground mt-1">
                  Top 3 merchants account for{' '}
                  <span className="font-mono font-medium">
                    {summary.topThreePercent.toFixed(1)}%
                  </span>{' '}
                  of spending
                </p>
              )}
            </div>
            <Badge variant="outline">{data.totalMerchantCount} merchants</Badge>
          </div>
        </CardHeader>

        <CardContent className="space-y-4">
          {/* New merchants section */}
          {showNewMerchants && data.newMerchants?.length > 0 && (
            <NewMerchantsList merchants={data.newMerchants} />
          )}

          {/* Search and sort controls */}
          <div className="flex gap-2">
            <div className="relative flex-1">
              <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search merchants..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-9"
              />
            </div>
            <Button
              variant="outline"
              size="icon"
              className="shrink-0"
              onClick={() => {
                const options: SortOption[] = ['amount', 'count', 'change', 'average']
                const currentIndex = options.indexOf(sortBy)
                setSortBy(options[(currentIndex + 1) % options.length])
              }}
              title={`Sort by ${sortBy}`}
            >
              <ArrowUpDown className="h-4 w-4" />
            </Button>
          </div>

          {/* Sort indicator */}
          <div className="text-xs text-muted-foreground">
            Sorted by{' '}
            <span className="font-medium">
              {sortBy === 'amount'
                ? 'total amount'
                : sortBy === 'count'
                ? 'transaction count'
                : sortBy === 'change'
                ? 'change %'
                : 'average transaction'}
            </span>
          </div>

          {/* Merchant list */}
          <AnimatePresence mode="wait">
            <motion.div
              key={sortBy + searchQuery}
              variants={staggerContainer}
              initial="hidden"
              animate="visible"
              className="space-y-2"
            >
              {merchants.length === 0 ? (
                <div className="py-8 text-center text-muted-foreground">
                  No merchants match your search
                </div>
              ) : (
                merchants.map((merchant, index) => (
                  <MerchantCard
                    key={merchant.merchantName}
                    merchant={merchant}
                    rank={index + 1}
                    showTrend={showTrends}
                    onClick={onMerchantSelect ? () => onMerchantSelect(merchant) : undefined}
                  />
                ))
              )}
            </motion.div>
          </AnimatePresence>

          {/* Significant changes section */}
          {showTrends && data.significantChanges?.length > 0 && (
            <div className="border-t pt-4">
              <h4 className="mb-3 text-sm font-medium text-muted-foreground">
                Significant Changes
              </h4>
              <div className="grid gap-2 sm:grid-cols-2">
                {data.significantChanges.slice(0, 4).map((merchant) => (
                  <div
                    key={merchant.merchantName}
                    className="flex items-center justify-between rounded-lg border p-3"
                  >
                    <span className="text-sm font-medium truncate flex-1 mr-2">
                      {merchant.displayName || merchant.merchantName}
                    </span>
                    <TrendIndicator changePercent={merchant.changePercent} size="md" />
                  </div>
                ))}
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </motion.div>
  )
}

export default MerchantAnalytics
