'use client'

/**
 * MissingReceiptCard Component (T014, T020, T022, T026, T029, T031)
 *
 * Displays a single missing receipt item with:
 * - Transaction date, vendor, amount, days since transaction
 * - Reimbursability source indicator (User Override vs AI Prediction)
 * - Action buttons for URL management, upload, and dismiss/restore
 *
 * Part of Feature 026: Missing Receipts UI
 */

import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Checkbox } from '@/components/ui/checkbox'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { formatCurrency, formatDate, cn, truncate } from '@/lib/utils'
import {
  Calendar,
  DollarSign,
  Clock,
  Link2,
  ExternalLink,
  Upload,
  X,
  RotateCcw,
  UserCheck,
  Bot,
  MoreVertical,
  ArrowRight,
} from 'lucide-react'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Link } from '@tanstack/react-router'
import type { MissingReceiptSummary, ReimbursabilitySource } from '@/types/api'

/**
 * Get urgency level based on days since transaction
 */
function getUrgencyLevel(days: number): 'low' | 'medium' | 'high' {
  if (days <= 7) return 'low'
  if (days <= 30) return 'medium'
  return 'high'
}

interface MissingReceiptCardProps {
  /** Missing receipt data */
  item: MissingReceiptSummary
  /** Callback when "Add URL" / "Edit URL" is clicked */
  onAddUrl?: () => void
  /** Callback when "Upload Receipt" is clicked */
  onUpload?: () => void
  /** Callback when "Dismiss" is clicked */
  onDismiss?: () => void
  /** Callback when "Restore" is clicked (for dismissed items) */
  onRestore?: () => void
  /** Whether actions are currently processing */
  isProcessing?: boolean
  /** Whether the item is selected for bulk actions */
  isSelected?: boolean
  /** Callback when selection changes */
  onSelectionChange?: (selected: boolean) => void
  /** Whether to show the selection checkbox */
  showSelection?: boolean
  /** Additional classes */
  className?: string
}

export function MissingReceiptCard({
  item,
  onAddUrl,
  onUpload,
  onDismiss,
  onRestore,
  isProcessing = false,
  isSelected = false,
  onSelectionChange,
  showSelection = false,
  className,
}: MissingReceiptCardProps) {
  const hasUrl = !!item.receiptUrl
  const urgency = getUrgencyLevel(item.daysSinceTransaction)

  return (
    <Card
      data-missing-receipt-card
      data-transaction-id={item.transactionId}
      className={cn(
        'transition-all duration-200 hover:shadow-md border-l-4',
        // Urgency border colors
        urgency === 'low' && 'border-l-transparent',
        urgency === 'medium' && 'border-l-amber-500',
        urgency === 'high' && 'border-l-red-500',
        // Dismissed state
        item.isDismissed && 'opacity-60 border-l-muted',
        // Selected state
        isSelected && 'ring-2 ring-primary ring-offset-2',
        className
      )}
      role="article"
      aria-label={`Missing receipt for ${item.description}, ${item.amount} dollars, ${item.daysSinceTransaction} days ago`}
    >
      <CardContent className="p-4">
        <div className="flex items-start justify-between gap-4">
          {/* Selection checkbox */}
          {showSelection && (
            <div className="pt-0.5">
              <Checkbox
                id={`select-${item.transactionId}`}
                checked={isSelected}
                onCheckedChange={(checked) => onSelectionChange?.(checked === true)}
                aria-label={`Select ${item.description}`}
              />
            </div>
          )}

          {/* Main content */}
          <div className="flex-1 min-w-0">
            {/* Vendor/Description */}
            <div className="flex items-center gap-2 mb-2">
              <h3
                className="text-base font-medium truncate"
                title={item.description}
              >
                {item.description}
              </h3>
              <SourceBadge source={item.source} />
              {item.isDismissed && (
                <Badge variant="secondary" className="shrink-0">
                  Dismissed
                </Badge>
              )}
            </div>

            {/* Metadata row */}
            <div className="flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
              <span className="flex items-center gap-1.5">
                <Calendar className="h-3.5 w-3.5" />
                {formatDate(item.transactionDate)}
              </span>
              <span className="flex items-center gap-1.5 font-medium text-foreground">
                <DollarSign className="h-3.5 w-3.5" />
                {formatCurrency(item.amount)}
              </span>
              <span className="flex items-center gap-1.5">
                <Clock className="h-3.5 w-3.5" />
                <DaysAgo days={item.daysSinceTransaction} />
              </span>
            </div>

            {/* URL display (T026) */}
            {hasUrl && (
              <div className="mt-2">
                <ReceiptUrlLink url={item.receiptUrl!} />
              </div>
            )}
          </div>

          {/* Actions */}
          <div className="flex items-center gap-1 shrink-0">
            {/* Quick actions for non-dismissed items */}
            {!item.isDismissed && (
              <>
                {/* Add/Edit URL button (T020) */}
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={onAddUrl}
                      disabled={isProcessing}
                      className="h-8 w-8"
                    >
                      <Link2 className="h-4 w-4" />
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent>
                    {hasUrl ? 'Edit URL' : 'Add URL'}
                  </TooltipContent>
                </Tooltip>

                {/* Upload button (T022) */}
                {onUpload && (
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={onUpload}
                        disabled={isProcessing}
                        className="h-8 w-8"
                      >
                        <Upload className="h-4 w-4" />
                      </Button>
                    </TooltipTrigger>
                    <TooltipContent>Upload Receipt</TooltipContent>
                  </Tooltip>
                )}
              </>
            )}

            {/* More actions dropdown */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="ghost"
                  size="icon"
                  disabled={isProcessing}
                  className="h-8 w-8"
                >
                  <MoreVertical className="h-4 w-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                {!item.isDismissed ? (
                  <>
                    <DropdownMenuItem asChild>
                      <Link
                        to="/transactions/$transactionId"
                        params={{ transactionId: item.transactionId }}
                      >
                        <ArrowRight className="mr-2 h-4 w-4" />
                        View Transaction
                      </Link>
                    </DropdownMenuItem>
                    {onAddUrl && (
                      <DropdownMenuItem onClick={onAddUrl}>
                        <Link2 className="mr-2 h-4 w-4" />
                        {hasUrl ? 'Edit URL' : 'Add URL'}
                      </DropdownMenuItem>
                    )}
                    {onUpload && (
                      <DropdownMenuItem onClick={onUpload}>
                        <Upload className="mr-2 h-4 w-4" />
                        Upload Receipt
                      </DropdownMenuItem>
                    )}
                    {onDismiss && (
                      <DropdownMenuItem onClick={onDismiss}>
                        <X className="mr-2 h-4 w-4" />
                        Dismiss
                      </DropdownMenuItem>
                    )}
                  </>
                ) : (
                  <>
                    <DropdownMenuItem asChild>
                      <Link
                        to="/transactions/$transactionId"
                        params={{ transactionId: item.transactionId }}
                      >
                        <ArrowRight className="mr-2 h-4 w-4" />
                        View Transaction
                      </Link>
                    </DropdownMenuItem>
                    {onRestore && (
                      <DropdownMenuItem onClick={onRestore}>
                        <RotateCcw className="mr-2 h-4 w-4" />
                        Restore
                      </DropdownMenuItem>
                    )}
                  </>
                )}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

/**
 * Badge indicating whether reimbursability was set by user or AI
 */
function SourceBadge({ source }: { source: ReimbursabilitySource }) {
  if (source === 'UserOverride') {
    return (
      <Tooltip>
        <TooltipTrigger asChild>
          <Badge variant="outline" className="gap-1 shrink-0">
            <UserCheck className="h-3 w-3" />
            User
          </Badge>
        </TooltipTrigger>
        <TooltipContent>Marked reimbursable by you</TooltipContent>
      </Tooltip>
    )
  }

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <Badge variant="secondary" className="gap-1 shrink-0">
          <Bot className="h-3 w-3" />
          AI
        </Badge>
      </TooltipTrigger>
      <TooltipContent>AI predicted as reimbursable</TooltipContent>
    </Tooltip>
  )
}

/**
 * Formatted "X days ago" display with color coding
 */
function DaysAgo({ days }: { days: number }) {
  const getColorClass = () => {
    if (days <= 7) return 'text-muted-foreground'
    if (days <= 30) return 'text-amber-600 dark:text-amber-500'
    return 'text-red-600 dark:text-red-500'
  }

  const formatDays = () => {
    if (days === 0) return 'Today'
    if (days === 1) return '1 day ago'
    return `${days} days ago`
  }

  return <span className={getColorClass()}>{formatDays()}</span>
}

/**
 * Receipt URL link with truncation and external link icon (T026, T027)
 */
function ReceiptUrlLink({ url }: { url: string }) {
  const displayUrl = truncate(url, 40)

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <a
          href={url}
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex items-center gap-1.5 text-sm text-primary hover:underline"
          onClick={(e) => e.stopPropagation()}
        >
          <ExternalLink className="h-3.5 w-3.5" />
          <span className="truncate">{displayUrl}</span>
        </a>
      </TooltipTrigger>
      <TooltipContent className="max-w-sm break-all">{url}</TooltipContent>
    </Tooltip>
  )
}

/**
 * Skeleton loader for the card
 */
export function MissingReceiptCardSkeleton() {
  return (
    <Card>
      <CardContent className="p-4">
        <div className="flex items-start justify-between gap-4">
          <div className="flex-1 space-y-3">
            <div className="flex items-center gap-2">
              <Skeleton className="h-5 w-48" />
              <Skeleton className="h-5 w-16" />
            </div>
            <div className="flex items-center gap-4">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-4 w-16" />
              <Skeleton className="h-4 w-20" />
            </div>
          </div>
          <div className="flex gap-1">
            <Skeleton className="h-8 w-8 rounded-md" />
            <Skeleton className="h-8 w-8 rounded-md" />
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
