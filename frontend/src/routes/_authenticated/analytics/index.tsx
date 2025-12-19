"use client"

import { useState } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import {
  useMonthlyComparison,
  useCacheStatistics,
  useSpendingByCategory,
  useSpendingByVendor,
} from '@/hooks/queries/use-analytics'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Progress } from '@/components/ui/progress'
import { formatCurrency, formatPercentage, getCurrentPeriod, getPreviousPeriod, formatPeriod } from '@/lib/utils'
import {
  TrendingUp,
  TrendingDown,
  DollarSign,
  ChevronLeft,
  ChevronRight,
  Building2,
  Tag,
  Zap,
  Database,
  RefreshCcw,
} from 'lucide-react'

export const Route = createFileRoute('/_authenticated/analytics/')({
  component: AnalyticsPage,
})

function AnalyticsPage() {
  const [selectedPeriod, setSelectedPeriod] = useState(getCurrentPeriod())

  // Calculate date range for the selected period
  const [year, month] = selectedPeriod.split('-').map(Number)
  const startDate = `${selectedPeriod}-01`
  const endDate = new Date(year, month, 0).toISOString().split('T')[0]

  const { data: monthlyComparison, isLoading: loadingComparison, refetch: refetchComparison } = useMonthlyComparison(selectedPeriod)
  const { data: cacheStats, isLoading: loadingCache } = useCacheStatistics(selectedPeriod)
  const { data: spendingByCategory, isLoading: loadingCategory } = useSpendingByCategory(startDate, endDate)
  const { data: spendingByVendor, isLoading: loadingVendor } = useSpendingByVendor(startDate, endDate)

  const handlePreviousPeriod = () => {
    setSelectedPeriod(getPreviousPeriod(selectedPeriod))
  }

  const handleNextPeriod = () => {
    const date = new Date(year, month, 1)
    setSelectedPeriod(`${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`)
  }

  const isLoading = loadingComparison || loadingCache || loadingCategory || loadingVendor

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Analytics</h1>
          <p className="text-muted-foreground">
            Spending insights and trends
          </p>
        </div>
        <div className="flex items-center gap-4">
          {/* Period Selector */}
          <div className="flex items-center gap-2">
            <Button variant="outline" size="icon" onClick={handlePreviousPeriod}>
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <div className="min-w-[140px] text-center">
              <p className="font-medium">{formatPeriod(selectedPeriod)}</p>
            </div>
            <Button
              variant="outline"
              size="icon"
              onClick={handleNextPeriod}
              disabled={selectedPeriod >= getCurrentPeriod()}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
          <Button variant="outline" size="icon" onClick={() => refetchComparison()}>
            <RefreshCcw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
          </Button>
        </div>
      </div>

      {/* Monthly Comparison Summary */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-2 text-muted-foreground text-sm">
              <DollarSign className="h-4 w-4" />
              Current Period
            </div>
            {loadingComparison ? (
              <Skeleton className="h-8 w-24 mt-2" />
            ) : (
              <p className="text-2xl font-bold mt-2">
                {formatCurrency(monthlyComparison?.currentTotal ?? 0)}
              </p>
            )}
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-2 text-muted-foreground text-sm">
              <DollarSign className="h-4 w-4" />
              Previous Period
            </div>
            {loadingComparison ? (
              <Skeleton className="h-8 w-24 mt-2" />
            ) : (
              <p className="text-2xl font-bold mt-2">
                {formatCurrency(monthlyComparison?.previousTotal ?? 0)}
              </p>
            )}
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-2 text-muted-foreground text-sm">
              {monthlyComparison && monthlyComparison.percentageChange >= 0 ? (
                <TrendingUp className="h-4 w-4" />
              ) : (
                <TrendingDown className="h-4 w-4" />
              )}
              Change
            </div>
            {loadingComparison ? (
              <Skeleton className="h-8 w-24 mt-2" />
            ) : (
              <p className={`text-2xl font-bold mt-2 ${
                (monthlyComparison?.percentageChange ?? 0) >= 0 ? 'text-red-500' : 'text-green-500'
              }`}>
                {(monthlyComparison?.percentageChange ?? 0) >= 0 ? '+' : ''}
                {formatPercentage((monthlyComparison?.percentageChange ?? 0) / 100, 1)}
              </p>
            )}
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-2 text-muted-foreground text-sm">
              <Zap className="h-4 w-4" />
              Cache Hit Rate
            </div>
            {loadingCache ? (
              <Skeleton className="h-8 w-24 mt-2" />
            ) : (
              <p className="text-2xl font-bold mt-2">
                {formatPercentage(cacheStats?.hitRate ?? 0)}
              </p>
            )}
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Spending by Category */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Tag className="h-5 w-5" />
              Spending by Category
            </CardTitle>
            <CardDescription>Breakdown of expenses by category</CardDescription>
          </CardHeader>
          <CardContent>
            {loadingCategory ? (
              <div className="space-y-4">
                {Array.from({ length: 5 }).map((_, i) => (
                  <div key={i} className="space-y-2">
                    <div className="flex justify-between">
                      <Skeleton className="h-4 w-24" />
                      <Skeleton className="h-4 w-16" />
                    </div>
                    <Skeleton className="h-2 w-full" />
                  </div>
                ))}
              </div>
            ) : spendingByCategory && spendingByCategory.length > 0 ? (
              <div className="space-y-4">
                {spendingByCategory.slice(0, 8).map((item) => (
                  <div key={item.category} className="space-y-2">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <span className="font-medium">{item.category || 'Uncategorized'}</span>
                        <Badge variant="outline" className="text-xs">
                          {item.transactionCount} txns
                        </Badge>
                      </div>
                      <span className="font-medium">{formatCurrency(item.amount)}</span>
                    </div>
                    <Progress value={item.percentageOfTotal} className="h-2" />
                    <p className="text-xs text-muted-foreground text-right">
                      {formatPercentage(item.percentageOfTotal / 100, 1)} of total
                    </p>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-sm text-muted-foreground text-center py-8">
                No category data available for this period
              </p>
            )}
          </CardContent>
        </Card>

        {/* Top Vendors */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Building2 className="h-5 w-5" />
              Top Vendors
            </CardTitle>
            <CardDescription>Highest spending by vendor</CardDescription>
          </CardHeader>
          <CardContent>
            {loadingVendor ? (
              <div className="space-y-4">
                {Array.from({ length: 5 }).map((_, i) => (
                  <div key={i} className="space-y-2">
                    <div className="flex justify-between">
                      <Skeleton className="h-4 w-32" />
                      <Skeleton className="h-4 w-16" />
                    </div>
                    <Skeleton className="h-2 w-full" />
                  </div>
                ))}
              </div>
            ) : spendingByVendor && spendingByVendor.length > 0 ? (
              <div className="space-y-4">
                {spendingByVendor.slice(0, 8).map((item) => (
                  <div key={item.vendorName} className="space-y-2">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        <span className="font-medium truncate max-w-[180px]">
                          {item.vendorName || 'Unknown'}
                        </span>
                        <Badge variant="outline" className="text-xs">
                          {item.transactionCount} txns
                        </Badge>
                      </div>
                      <span className="font-medium">{formatCurrency(item.amount)}</span>
                    </div>
                    <Progress value={item.percentageOfTotal} className="h-2" />
                    <p className="text-xs text-muted-foreground text-right">
                      {formatPercentage(item.percentageOfTotal / 100, 1)} of total
                    </p>
                  </div>
                ))}
              </div>
            ) : (
              <p className="text-sm text-muted-foreground text-center py-8">
                No vendor data available for this period
              </p>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Monthly Changes */}
      {monthlyComparison && (
        <div className="grid gap-6 lg:grid-cols-3">
          {/* New Vendors */}
          {monthlyComparison.newVendors.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">New Vendors This Month</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  {monthlyComparison.newVendors.map((vendor) => (
                    <div key={vendor.vendorName} className="flex justify-between text-sm">
                      <span className="truncate">{vendor.vendorName}</span>
                      <span className="font-medium">{formatCurrency(vendor.amount)}</span>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Missing Recurring */}
          {monthlyComparison.missingRecurring.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base text-yellow-600">Missing Recurring</CardTitle>
                <CardDescription>Expected vendors not seen this month</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  {monthlyComparison.missingRecurring.map((vendor) => (
                    <div key={vendor.vendorName} className="flex justify-between text-sm">
                      <span className="truncate">{vendor.vendorName}</span>
                      <span className="text-muted-foreground">
                        Usually {formatCurrency(vendor.amount)}
                      </span>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Significant Changes */}
          {monthlyComparison.significantChanges.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Significant Changes</CardTitle>
                <CardDescription>Vendors with notable spending changes</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  {monthlyComparison.significantChanges.slice(0, 5).map((change) => (
                    <div key={change.vendorName} className="flex justify-between text-sm">
                      <span className="truncate">{change.vendorName}</span>
                      <span className={change.percentageChange >= 0 ? 'text-red-500' : 'text-green-500'}>
                        {change.percentageChange >= 0 ? '+' : ''}
                        {formatPercentage(change.percentageChange / 100)}
                      </span>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      )}

      {/* Cache Statistics */}
      {cacheStats && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Database className="h-5 w-5" />
              AI Cache Performance
            </CardTitle>
            <CardDescription>
              Cache efficiency and cost savings for {formatPeriod(selectedPeriod)}
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 md:grid-cols-4">
              <div className="space-y-1">
                <p className="text-sm text-muted-foreground">Total Operations</p>
                <p className="text-xl font-bold">{cacheStats.totalOperations}</p>
              </div>
              <div className="space-y-1">
                <p className="text-sm text-muted-foreground">Hit Rate</p>
                <p className="text-xl font-bold">{formatPercentage(cacheStats.hitRate)}</p>
              </div>
              <div className="space-y-1">
                <p className="text-sm text-muted-foreground">Est. Cost Saved</p>
                <p className="text-xl font-bold text-green-500">
                  {formatCurrency(cacheStats.estimatedCostSaved)}
                </p>
              </div>
              <div className="space-y-1">
                <p className="text-sm text-muted-foreground">Tier Breakdown</p>
                <div className="flex gap-1">
                  {cacheStats.tierBreakdown.map((tier) => (
                    <Badge key={tier.tier} variant="outline" className="text-xs">
                      T{tier.tier}: {tier.count}
                    </Badge>
                  ))}
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
