'use client'

/**
 * MissingReceiptsWidget Component (T017, T023)
 *
 * Dashboard widget showing missing receipts count and top 3 recent items.
 * Provides quick actions and a link to the full list.
 *
 * Part of Feature 026: Missing Receipts UI
 */

import { Link } from '@tanstack/react-router'
import { useMissingReceiptsWidget } from '@/hooks/queries/use-missing-receipts'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { formatCurrency, cn } from '@/lib/utils'
import {
  Receipt,
  ChevronRight,
  Upload,
  Clock,
  AlertCircle,
} from 'lucide-react'
import { EmptyMissingReceiptsWidget } from './missing-receipts-empty'
import type { MissingReceiptSummary } from '@/types/api'

interface MissingReceiptsWidgetProps {
  /** Callback when quick upload is clicked on an item */
  onQuickUpload?: (transactionId: string) => void
  /** Additional classes */
  className?: string
}

export function MissingReceiptsWidget({
  onQuickUpload,
  className,
}: MissingReceiptsWidgetProps) {
  const { data, isLoading, isError } = useMissingReceiptsWidget()

  return (
    <Card className={cn('flex flex-col', className)}>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-base font-medium flex items-center gap-2">
          <Receipt className="h-4 w-4 text-muted-foreground" />
          Missing Receipts
        </CardTitle>
        {data && data.totalCount > 0 && (
          <Badge
            variant={data.totalCount > 5 ? 'destructive' : 'secondary'}
            className="font-mono"
          >
            {data.totalCount}
          </Badge>
        )}
      </CardHeader>
      <CardContent className="flex-1">
        {isLoading ? (
          <WidgetSkeleton />
        ) : isError ? (
          <WidgetError />
        ) : !data || data.totalCount === 0 ? (
          <EmptyMissingReceiptsWidget />
        ) : (
          <div className="space-y-3">
            {/* Recent items */}
            <div className="space-y-2">
              {data.recentItems.map((item) => (
                <WidgetItem
                  key={item.transactionId}
                  item={item}
                  onUpload={
                    onQuickUpload
                      ? () => onQuickUpload(item.transactionId)
                      : undefined
                  }
                />
              ))}
            </div>

            {/* View all link */}
            {data.totalCount > 3 && (
              <div className="pt-2 border-t">
                <Link
                  to="/missing-receipts"
                  className="flex items-center justify-center gap-1 text-sm text-primary hover:underline"
                >
                  View all {data.totalCount} missing receipts
                  <ChevronRight className="h-4 w-4" />
                </Link>
              </div>
            )}

            {data.totalCount <= 3 && (
              <div className="pt-2 border-t">
                <Link
                  to="/missing-receipts"
                  className="flex items-center justify-center gap-1 text-sm text-primary hover:underline"
                >
                  View details
                  <ChevronRight className="h-4 w-4" />
                </Link>
              </div>
            )}
          </div>
        )}
      </CardContent>
    </Card>
  )
}

/**
 * Single item in the widget list
 */
function WidgetItem({
  item,
  onUpload,
}: {
  item: MissingReceiptSummary
  onUpload?: () => void
}) {
  return (
    <div className="flex items-center justify-between gap-2 p-2 rounded-md hover:bg-muted/50 transition-colors">
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium truncate" title={item.description}>
          {item.description}
        </p>
        <div className="flex items-center gap-2 text-xs text-muted-foreground">
          <span>{formatCurrency(item.amount)}</span>
          <span className="text-muted-foreground/50">&middot;</span>
          <span className="flex items-center gap-1">
            <Clock className="h-3 w-3" />
            <DaysAgoCompact days={item.daysSinceTransaction} />
          </span>
        </div>
      </div>

      {/* Quick upload button (T023) */}
      {onUpload && (
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7 shrink-0"
              onClick={(e) => {
                e.preventDefault()
                onUpload()
              }}
            >
              <Upload className="h-3.5 w-3.5" />
            </Button>
          </TooltipTrigger>
          <TooltipContent>Upload receipt</TooltipContent>
        </Tooltip>
      )}
    </div>
  )
}

/**
 * Compact days ago display for widget
 */
function DaysAgoCompact({ days }: { days: number }) {
  if (days === 0) return <span>Today</span>
  if (days === 1) return <span>1d ago</span>
  return <span>{days}d ago</span>
}

/**
 * Loading skeleton for widget
 */
function WidgetSkeleton() {
  return (
    <div className="space-y-3">
      {[1, 2, 3].map((i) => (
        <div key={i} className="flex items-center justify-between gap-2 p-2">
          <div className="flex-1 space-y-1.5">
            <Skeleton className="h-4 w-32" />
            <Skeleton className="h-3 w-24" />
          </div>
          <Skeleton className="h-7 w-7 rounded-md" />
        </div>
      ))}
    </div>
  )
}

/**
 * Error state for widget
 */
function WidgetError() {
  return (
    <div className="flex flex-col items-center justify-center py-6 text-center">
      <AlertCircle className="h-8 w-8 text-muted-foreground" />
      <p className="mt-2 text-sm text-muted-foreground">
        Failed to load missing receipts
      </p>
    </div>
  )
}
