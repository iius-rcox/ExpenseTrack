"use client"

import { createFileRoute, Link } from '@tanstack/react-router'
import { useDashboardMetrics, useRecentActivity } from '@/hooks/queries/use-dashboard'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { formatCurrency, formatRelativeTime } from '@/lib/utils'
import {
  Receipt,
  CreditCard,
  GitMerge,
  FileText,
  AlertCircle,
  ArrowRight,
  TrendingUp,
  TrendingDown,
  Clock,
} from 'lucide-react'

export const Route = createFileRoute('/_authenticated/dashboard')({
  component: DashboardPage,
})

function DashboardPage() {
  const { data: metrics, isLoading: metricsLoading, error: metricsError } = useDashboardMetrics()
  const { data: activity, isLoading: activityLoading } = useRecentActivity()

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
          <p className="text-muted-foreground">
            Overview of your expense management activity
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button asChild variant="outline">
            <Link to="/receipts">
              <Receipt className="mr-2 h-4 w-4" />
              Upload Receipt
            </Link>
          </Button>
        </div>
      </div>

      {metricsError && (
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Error loading dashboard</AlertTitle>
          <AlertDescription>
            Failed to load dashboard metrics. Please try refreshing the page.
          </AlertDescription>
        </Alert>
      )}

      {/* Metrics Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <MetricCard
          title="Pending Receipts"
          value={metrics?.pendingReceiptsCount}
          description="Awaiting processing"
          icon={Receipt}
          loading={metricsLoading}
          href="/receipts?status=Pending"
        />
        <MetricCard
          title="Unmatched Transactions"
          value={metrics?.unmatchedTransactionsCount}
          description="Need matching"
          icon={CreditCard}
          loading={metricsLoading}
          href="/transactions?matched=false"
        />
        <MetricCard
          title="Pending Matches"
          value={metrics?.pendingMatchesCount}
          description="Awaiting review"
          icon={GitMerge}
          loading={metricsLoading}
          href="/matching"
        />
        <MetricCard
          title="Draft Reports"
          value={metrics?.draftReportsCount}
          description="Ready to submit"
          icon={FileText}
          loading={metricsLoading}
          href="/reports?status=Draft"
        />
      </div>

      {/* Monthly Spending */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle>Monthly Spending</CardTitle>
            <CardDescription>
              Your expense trends over the current period
            </CardDescription>
          </CardHeader>
          <CardContent>
            {metricsLoading ? (
              <div className="space-y-3">
                <Skeleton className="h-8 w-48" />
                <Skeleton className="h-4 w-32" />
              </div>
            ) : (
              <div className="space-y-4">
                <div className="flex items-baseline gap-2">
                  <span className="text-4xl font-bold">
                    {formatCurrency(metrics?.monthlySpending.currentMonth ?? 0)}
                  </span>
                  <span className="text-sm text-muted-foreground">this month</span>
                </div>
                <div className="flex items-center gap-2">
                  {(metrics?.monthlySpending.percentChange ?? 0) >= 0 ? (
                    <TrendingUp className="h-4 w-4 text-red-500" />
                  ) : (
                    <TrendingDown className="h-4 w-4 text-green-500" />
                  )}
                  <span className={`text-sm ${(metrics?.monthlySpending.percentChange ?? 0) >= 0 ? 'text-red-500' : 'text-green-500'}`}>
                    {Math.abs(metrics?.monthlySpending.percentChange ?? 0).toFixed(1)}%
                  </span>
                  <span className="text-sm text-muted-foreground">
                    vs last month ({formatCurrency(metrics?.monthlySpending.previousMonth ?? 0)})
                  </span>
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Quick Actions</CardTitle>
            <CardDescription>Common tasks</CardDescription>
          </CardHeader>
          <CardContent className="space-y-2">
            <Button asChild variant="outline" className="w-full justify-start">
              <Link to="/receipts">
                <Receipt className="mr-2 h-4 w-4" />
                Upload Receipts
                <ArrowRight className="ml-auto h-4 w-4" />
              </Link>
            </Button>
            <Button asChild variant="outline" className="w-full justify-start">
              <Link to="/matching">
                <GitMerge className="mr-2 h-4 w-4" />
                Review Matches
                <ArrowRight className="ml-auto h-4 w-4" />
              </Link>
            </Button>
            <Button asChild variant="outline" className="w-full justify-start">
              <Link to="/reports">
                <FileText className="mr-2 h-4 w-4" />
                Create Report
                <ArrowRight className="ml-auto h-4 w-4" />
              </Link>
            </Button>
          </CardContent>
        </Card>
      </div>

      {/* Recent Activity */}
      <Card>
        <CardHeader>
          <CardTitle>Recent Activity</CardTitle>
          <CardDescription>Latest actions in your account</CardDescription>
        </CardHeader>
        <CardContent>
          {activityLoading ? (
            <div className="space-y-4">
              {[1, 2, 3, 4, 5].map((i) => (
                <div key={i} className="flex items-center gap-4">
                  <Skeleton className="h-10 w-10 rounded-full" />
                  <div className="space-y-2">
                    <Skeleton className="h-4 w-[250px]" />
                    <Skeleton className="h-4 w-[200px]" />
                  </div>
                </div>
              ))}
            </div>
          ) : activity && activity.length > 0 ? (
            <div className="space-y-4">
              {activity.map((item, index) => (
                <ActivityItem key={index} item={item} />
              ))}
            </div>
          ) : (
            <div className="flex flex-col items-center justify-center py-8 text-center">
              <Clock className="h-12 w-12 text-muted-foreground" />
              <h3 className="mt-4 text-lg font-semibold">No recent activity</h3>
              <p className="text-sm text-muted-foreground">
                Your recent actions will appear here
              </p>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

interface MetricCardProps {
  title: string
  value?: number
  description: string
  icon: React.ElementType
  loading: boolean
  href?: string
}

function MetricCard({ title, value, description, icon: Icon, loading, href }: MetricCardProps) {
  const content = (
    <Card className={href ? 'cursor-pointer transition-colors hover:bg-accent/50' : ''}>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
        <Icon className="h-4 w-4 text-muted-foreground" />
      </CardHeader>
      <CardContent>
        {loading ? (
          <>
            <Skeleton className="h-8 w-16" />
            <Skeleton className="mt-1 h-4 w-24" />
          </>
        ) : (
          <>
            <div className="text-2xl font-bold">{value ?? 0}</div>
            <p className="text-xs text-muted-foreground">{description}</p>
          </>
        )}
      </CardContent>
    </Card>
  )

  if (href) {
    return <Link to={href}>{content}</Link>
  }

  return content
}

interface ActivityItemProps {
  item: {
    type: string
    title: string
    description: string
    timestamp: string
  }
}

function ActivityItem({ item }: ActivityItemProps) {
  const getActivityIcon = (type: string) => {
    switch (type) {
      case 'receipt':
        return Receipt
      case 'transaction':
        return CreditCard
      case 'match':
        return GitMerge
      case 'report':
        return FileText
      default:
        return Clock
    }
  }

  const Icon = getActivityIcon(item.type)

  return (
    <div className="flex items-start gap-4">
      <div className="rounded-full bg-primary/10 p-2">
        <Icon className="h-4 w-4 text-primary" />
      </div>
      <div className="flex-1 space-y-1">
        <p className="text-sm font-medium leading-none">{item.title}</p>
        <p className="text-sm text-muted-foreground">{item.description}</p>
      </div>
      <span className="text-xs text-muted-foreground">
        {formatRelativeTime(item.timestamp)}
      </span>
    </div>
  )
}
