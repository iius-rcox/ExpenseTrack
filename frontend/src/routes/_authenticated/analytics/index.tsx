'use client'

/**
 * Analytics Dashboard Route (T084)
 *
 * Enhanced analytics page using the "Refined Intelligence" design system.
 * Integrates spending trends, category breakdown, merchant analytics,
 * and subscription detection components.
 *
 * Features:
 * - Flexible date range selection with presets
 * - Spending trend visualization with multiple chart types
 * - Category breakdown with pie/bar/list views
 * - Top merchant analysis with trend indicators
 * - Subscription detection and management
 * - Cache performance monitoring
 */

import { useState, useMemo } from 'react'
import { createFileRoute, Link } from '@tanstack/react-router'
import { motion } from 'framer-motion'
import {
  useMonthlyComparison,
  useCacheStatistics,
  useSpendingTrend,
  useSpendingByCategory,
  useMerchantAnalytics,
  useSubscriptionDetection,
  getDefaultDateRange,
} from '@/hooks/queries/use-analytics'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { fadeIn, staggerContainer, staggerChild } from '@/lib/animations'
import { cn } from '@/lib/utils'
import {
  TrendingUp,
  TrendingDown,
  DollarSign,
  BarChart3,
  PieChart,
  Store,
  Repeat,
  Database,
  RefreshCcw,
  AlertCircle,
  Brain,
} from 'lucide-react'
import type { AnalyticsDateRange, TimeGranularity } from '@/types/analytics'

// Import analytics components
import {
  SpendingTrendChart,
  CategoryBreakdown,
  MerchantAnalytics,
  SubscriptionDetector,
  DateRangePicker,
  QuickPresets,
  ComparisonDateRange,
} from '@/components/analytics'

// Import error boundary for graceful degradation
import { ErrorBoundary, CompactErrorFallback } from '@/components/error-boundary'

export const Route = createFileRoute('/_authenticated/analytics/')({
  component: AnalyticsDashboard,
})

// Format currency
function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(value)
}

// Format percentage
function formatPercentage(value: number): string {
  return `${(value * 100).toFixed(1)}%`
}

// Summary metric card
function MetricCard({
  title,
  value,
  icon: Icon,
  trend,
  trendLabel,
  isLoading,
}: {
  title: string
  value: string | number
  icon: React.ElementType
  trend?: number
  trendLabel?: string
  isLoading?: boolean
}) {
  const isPositive = trend !== undefined && trend > 0
  const isNegative = trend !== undefined && trend < 0

  return (
    <motion.div variants={staggerChild}>
      <Card>
        <CardContent className="pt-6">
          <div className="flex items-center gap-2 text-muted-foreground text-sm">
            <Icon className="h-4 w-4" />
            {title}
          </div>
          {isLoading ? (
            <Skeleton className="h-8 w-24 mt-2" />
          ) : (
            <div className="mt-2 flex items-end justify-between">
              <p className="text-2xl font-bold font-mono">
                {typeof value === 'number' ? formatCurrency(value) : value}
              </p>
              {trend !== undefined && (
                <div
                  className={cn(
                    'flex items-center gap-1 text-sm',
                    isPositive ? 'text-rose-500' : isNegative ? 'text-emerald-500' : 'text-slate-500'
                  )}
                >
                  {isPositive ? (
                    <TrendingUp className="h-4 w-4" />
                  ) : isNegative ? (
                    <TrendingDown className="h-4 w-4" />
                  ) : null}
                  <span className="font-mono">
                    {isPositive ? '+' : ''}
                    {trend.toFixed(1)}%
                  </span>
                  {trendLabel && (
                    <span className="text-muted-foreground text-xs">{trendLabel}</span>
                  )}
                </div>
              )}
            </div>
          )}
        </CardContent>
      </Card>
    </motion.div>
  )
}

// Cache stats card
function CacheStatsCard({
  period,
  isLoading,
}: {
  period: string
  isLoading?: boolean
}) {
  const { data: cacheStats, isLoading: loadingCache } = useCacheStatistics(period)

  if (loadingCache || isLoading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Database className="h-5 w-5" />
            AI Cache Performance
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid gap-4 md:grid-cols-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="h-12 w-full" />
            ))}
          </div>
        </CardContent>
      </Card>
    )
  }

  if (!cacheStats) return null

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Database className="h-5 w-5" />
          AI Cache Performance
        </CardTitle>
        <CardDescription>
          Cache efficiency and cost savings
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="grid gap-4 md:grid-cols-4">
          <div className="space-y-1">
            <p className="text-sm text-muted-foreground">Total Operations</p>
            <p className="text-xl font-bold font-mono">{cacheStats.totalOperations}</p>
          </div>
          <div className="space-y-1">
            <p className="text-sm text-muted-foreground">Hit Rate</p>
            <p className="text-xl font-bold font-mono">{formatPercentage(cacheStats.hitRate)}</p>
          </div>
          <div className="space-y-1">
            <p className="text-sm text-muted-foreground">Est. Cost Saved</p>
            <p className="text-xl font-bold font-mono text-emerald-500">
              {formatCurrency(cacheStats.estimatedCostSaved)}
            </p>
          </div>
          <div className="space-y-1">
            <p className="text-sm text-muted-foreground">Tier Breakdown</p>
            <div className="flex gap-1 flex-wrap">
              {cacheStats.tierBreakdown.map((tier) => (
                <Badge key={tier.tier} variant="outline" className="text-xs font-mono">
                  T{tier.tier}: {tier.count}
                </Badge>
              ))}
            </div>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

function AnalyticsDashboard() {
  // Date range state
  const [dateRange, setDateRange] = useState<AnalyticsDateRange>(getDefaultDateRange())
  const [comparisonRange, setComparisonRange] = useState<AnalyticsDateRange | undefined>()
  const [granularity] = useState<TimeGranularity>('day')
  const [activeTab, setActiveTab] = useState('overview')

  // Calculate period for legacy hooks
  const period = useMemo(() => {
    const date = new Date(dateRange.startDate)
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`
  }, [dateRange.startDate])

  // Map granularity for useSpendingTrend (only supports 'day' | 'week' | 'month')
  const trendGranularity: 'day' | 'week' | 'month' =
    granularity === 'quarter' || granularity === 'year' ? 'month' : granularity

  // Data queries
  const { data: monthlyComparison, isLoading: loadingComparison, refetch } = useMonthlyComparison(period)
  const { data: trendData, isLoading: loadingTrend } = useSpendingTrend(
    dateRange.startDate,
    dateRange.endDate,
    trendGranularity
  )
  const { data: categoryData, isLoading: loadingCategory } = useSpendingByCategory(
    dateRange.startDate,
    dateRange.endDate
  )
  const { data: merchantData, isLoading: loadingMerchant } = useMerchantAnalytics({
    startDate: dateRange.startDate,
    endDate: dateRange.endDate,
    topCount: 10,
    includeComparison: !!comparisonRange,
  })
  const { data: subscriptionData, isLoading: loadingSubscriptions } = useSubscriptionDetection({
    minConfidence: 'medium',
  })

  // Transform trend data for chart
  const chartTrendData = useMemo(() => {
    if (!trendData) return []
    return trendData.map((item) => ({
      period: item.date,
      amount: item.amount,
      transactionCount: item.transactionCount,
    }))
  }, [trendData])

  // Transform category data for breakdown
  const chartCategoryData = useMemo(() => {
    if (!categoryData) return []
    return categoryData.map((item) => ({
      category: item.category || 'Uncategorized',
      amount: item.amount,
      percentage: item.percentageOfTotal,
      transactionCount: item.transactionCount,
    }))
  }, [categoryData])

  const isLoading = loadingComparison || loadingTrend || loadingCategory || loadingMerchant

  return (
    <motion.div
      variants={fadeIn}
      initial="hidden"
      animate="visible"
      className="space-y-6"
    >
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Analytics</h1>
          <p className="text-muted-foreground">
            Spending insights and trends
          </p>
        </div>
        <div className="flex items-center gap-2 flex-wrap">
          <DateRangePicker
            value={dateRange}
            onChange={setDateRange}
          />
          <ComparisonDateRange
            primaryRange={dateRange}
            comparisonRange={comparisonRange}
            onComparisonChange={setComparisonRange}
          />
          <Button variant="outline" size="icon" onClick={() => refetch()}>
            <RefreshCcw className={cn('h-4 w-4', isLoading && 'animate-spin')} />
          </Button>
          <Button variant="outline" asChild>
            <Link to="/predictions/patterns" className="gap-2">
              <Brain className="h-4 w-4" />
              <span className="hidden sm:inline">Expense Patterns</span>
            </Link>
          </Button>
        </div>
      </div>

      {/* Quick presets */}
      <QuickPresets value={dateRange} onChange={setDateRange} />

      {/* Summary Metrics */}
      <motion.div
        variants={staggerContainer}
        initial="hidden"
        animate="visible"
        className="grid gap-4 md:grid-cols-4"
      >
        <MetricCard
          title="Current Period"
          value={monthlyComparison?.summary?.currentTotal ?? 0}
          icon={DollarSign}
          isLoading={loadingComparison}
        />
        <MetricCard
          title="Previous Period"
          value={monthlyComparison?.summary?.previousTotal ?? 0}
          icon={DollarSign}
          isLoading={loadingComparison}
        />
        <MetricCard
          title="Change"
          value={`${(monthlyComparison?.summary?.changePercent ?? 0) >= 0 ? '+' : ''}${(monthlyComparison?.summary?.changePercent ?? 0).toFixed(1)}%`}
          icon={(monthlyComparison?.summary?.changePercent ?? 0) >= 0 ? TrendingUp : TrendingDown}
          trend={monthlyComparison?.summary?.changePercent}
          trendLabel="vs prev"
          isLoading={loadingComparison}
        />
        <MetricCard
          title="Subscriptions"
          value={`${subscriptionData?.subscriptions?.length ?? 0} detected`}
          icon={Repeat}
          isLoading={loadingSubscriptions}
        />
      </motion.div>

      {/* Main Content Tabs */}
      <Tabs value={activeTab} onValueChange={setActiveTab}>
        <TabsList>
          <TabsTrigger value="overview" className="gap-2">
            <BarChart3 className="h-4 w-4" />
            Overview
          </TabsTrigger>
          <TabsTrigger value="categories" className="gap-2">
            <PieChart className="h-4 w-4" />
            Categories
          </TabsTrigger>
          <TabsTrigger value="merchants" className="gap-2">
            <Store className="h-4 w-4" />
            Merchants
          </TabsTrigger>
          <TabsTrigger value="subscriptions" className="gap-2">
            <Repeat className="h-4 w-4" />
            Subscriptions
          </TabsTrigger>
        </TabsList>

        {/* Overview Tab */}
        <TabsContent value="overview" className="space-y-6">
          {/* Spending Trend */}
          <ErrorBoundary
            boundaryId="spending-trend"
            fallback={(_error: Error, resetError: () => void) => (
              <CompactErrorFallback
                resetError={resetError}
                title="Failed to load spending trends"
              />
            )}
          >
            <SpendingTrendChart
              data={chartTrendData}
              isLoading={loadingTrend}
              chartType="area"
              granularity={trendGranularity}
              showComparison={!!comparisonRange}
              title="Spending Over Time"
            />
          </ErrorBoundary>

          {/* Category and Merchant Grid */}
          <div className="grid gap-6 lg:grid-cols-2">
            <ErrorBoundary
              boundaryId="category-breakdown"
              fallback={(_error: Error, resetError: () => void) => (
                <CompactErrorFallback
                  resetError={resetError}
                  title="Failed to load categories"
                />
              )}
            >
              <CategoryBreakdown
                data={chartCategoryData}
                isLoading={loadingCategory}
                chartType="donut"
                maxCategories={6}
                title="Category Distribution"
              />
            </ErrorBoundary>
            <ErrorBoundary
              boundaryId="merchant-analytics"
              fallback={(_error: Error, resetError: () => void) => (
                <CompactErrorFallback
                  resetError={resetError}
                  title="Failed to load merchants"
                />
              )}
            >
              <MerchantAnalytics
                data={merchantData}
                isLoading={loadingMerchant}
                topCount={5}
                showTrends
                showNewMerchants={false}
                title="Top Merchants"
              />
            </ErrorBoundary>
          </div>

          {/* Cache Stats */}
          <ErrorBoundary
            boundaryId="cache-stats"
            fallback={(_error: Error, resetError: () => void) => (
              <CompactErrorFallback
                resetError={resetError}
                title="Failed to load cache statistics"
              />
            )}
          >
            <CacheStatsCard period={period} isLoading={loadingComparison} />
          </ErrorBoundary>

          {/* Monthly Changes */}
          {monthlyComparison && (
            <div className="grid gap-6 lg:grid-cols-3">
              {/* New Vendors */}
              {monthlyComparison.newVendors?.length > 0 && (
                <Card>
                  <CardHeader>
                    <CardTitle className="text-base flex items-center gap-2">
                      <AlertCircle className="h-4 w-4 text-blue-500" />
                      New Vendors
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2">
                      {monthlyComparison.newVendors.slice(0, 5).map((vendor) => (
                        <div key={vendor.vendorName} className="flex justify-between text-sm">
                          <span className="truncate">{vendor.vendorName}</span>
                          <span className="font-mono">{formatCurrency(vendor.amount)}</span>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Missing Recurring */}
              {monthlyComparison.missingRecurring?.length > 0 && (
                <Card>
                  <CardHeader>
                    <CardTitle className="text-base text-amber-600">Missing Recurring</CardTitle>
                    <CardDescription>Expected vendors not seen</CardDescription>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2">
                      {monthlyComparison.missingRecurring.slice(0, 5).map((vendor) => (
                        <div key={vendor.vendorName} className="flex justify-between text-sm">
                          <span className="truncate">{vendor.vendorName}</span>
                          <span className="text-muted-foreground font-mono">
                            Usually {formatCurrency(vendor.amount)}
                          </span>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Significant Changes */}
              {monthlyComparison.significantChanges?.length > 0 && (
                <Card>
                  <CardHeader>
                    <CardTitle className="text-base">Significant Changes</CardTitle>
                    <CardDescription>Notable spending changes</CardDescription>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2">
                      {monthlyComparison.significantChanges.slice(0, 5).map((change) => (
                        <div key={change.vendorName} className="flex justify-between text-sm">
                          <span className="truncate">{change.vendorName}</span>
                          <span
                            className={cn(
                              'font-mono',
                              change.changePercent >= 0 ? 'text-rose-500' : 'text-emerald-500'
                            )}
                          >
                            {change.changePercent >= 0 ? '+' : ''}
                            {change.changePercent.toFixed(1)}%
                          </span>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              )}
            </div>
          )}
        </TabsContent>

        {/* Categories Tab */}
        <TabsContent value="categories">
          <ErrorBoundary
            boundaryId="category-breakdown-full"
            fallback={(_error: Error, resetError: () => void) => (
              <CompactErrorFallback
                resetError={resetError}
                title="Failed to load category breakdown"
              />
            )}
          >
            <CategoryBreakdown
              data={chartCategoryData}
              isLoading={loadingCategory}
              chartType="donut"
              showComparison={!!comparisonRange}
              maxCategories={12}
              height={400}
              title="Spending by Category"
            />
          </ErrorBoundary>
        </TabsContent>

        {/* Merchants Tab */}
        <TabsContent value="merchants">
          <ErrorBoundary
            boundaryId="merchant-analytics-full"
            fallback={(_error: Error, resetError: () => void) => (
              <CompactErrorFallback
                resetError={resetError}
                title="Failed to load merchant analytics"
              />
            )}
          >
            <MerchantAnalytics
              data={merchantData}
              isLoading={loadingMerchant}
              topCount={20}
              showTrends
              showNewMerchants
              title="Merchant Analysis"
            />
          </ErrorBoundary>
        </TabsContent>

        {/* Subscriptions Tab */}
        <TabsContent value="subscriptions">
          <ErrorBoundary
            boundaryId="subscription-detector"
            fallback={(_error: Error, resetError: () => void) => (
              <CompactErrorFallback
                resetError={resetError}
                title="Failed to load subscription detection"
              />
            )}
          >
            <SubscriptionDetector
              data={subscriptionData}
              isLoading={loadingSubscriptions}
              showSummary
              groupByCategory
              title="Detected Subscriptions"
            />
          </ErrorBoundary>
        </TabsContent>
      </Tabs>
    </motion.div>
  )
}

export default AnalyticsDashboard
