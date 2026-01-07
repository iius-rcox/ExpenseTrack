'use client'

/**
 * SubscriptionDetector Component (T082)
 *
 * Displays detected recurring charges and subscriptions.
 * Shows frequency, costs, and allows user acknowledgment.
 */

import { useMemo, useState } from 'react'

/**
 * DEFENSIVE HELPER: Safely convert any value to a displayable string.
 * Guards against React Error #301 where empty objects {} might be in cached data.
 */
function safeDisplayString(value: unknown, fallback = ''): string {
  if (value === null || value === undefined) return fallback;
  if (typeof value === 'object' && !Array.isArray(value) && !(value instanceof Date)) {
    const keys = Object.keys(value as object);
    if (keys.length === 0) return fallback;
    return fallback;
  }
  return String(value);
}
import { motion, AnimatePresence } from 'framer-motion'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Switch } from '@/components/ui/switch'
import { fadeIn, staggerContainer, staggerChild } from '@/lib/animations'
import { cn } from '@/lib/utils'
import {
  Repeat,
  CalendarClock,
  DollarSign,
  AlertCircle,
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  RefreshCcw,
  Filter,
} from 'lucide-react'
import type {
  SubscriptionDetection,
  SubscriptionDetectionResponse,
  SubscriptionFrequency,
  DetectionConfidence,
  SubscriptionSummary,
} from '@/types/analytics'

// Format currency
function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value)
}

// Format date
function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

// Frequency label mapping
const FREQUENCY_LABELS: Record<SubscriptionFrequency, string> = {
  weekly: 'Weekly',
  biweekly: 'Bi-weekly',
  monthly: 'Monthly',
  quarterly: 'Quarterly',
  annual: 'Annual',
  unknown: 'Irregular',
}

// Confidence badge colors
const CONFIDENCE_STYLES: Record<DetectionConfidence, { bg: string; text: string }> = {
  high: { bg: 'bg-emerald-100', text: 'text-emerald-700' },
  medium: { bg: 'bg-amber-100', text: 'text-amber-700' },
  low: { bg: 'bg-rose-100', text: 'text-rose-700' },
}

interface SubscriptionDetectorProps {
  data?: SubscriptionDetectionResponse
  isLoading?: boolean
  groupByCategory?: boolean
  showSummary?: boolean
  frequencyFilter?: SubscriptionFrequency[]
  className?: string
  title?: string
  onAcknowledge?: (subscriptionId: string, acknowledged: boolean) => void
  onSubscriptionClick?: (subscription: SubscriptionDetection) => void
  onRefresh?: () => void
}

// Summary card component
function SubscriptionSummaryCard({ summary }: { summary: SubscriptionSummary }) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <div className="rounded-lg border bg-muted/30 p-4">
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Repeat className="h-4 w-4" />
          Subscriptions
        </div>
        <p className="mt-1 text-2xl font-bold">{summary.subscriptionCount}</p>
      </div>

      <div className="rounded-lg border bg-muted/30 p-4">
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <CalendarClock className="h-4 w-4" />
          Monthly Cost
        </div>
        <p className="mt-1 font-mono text-2xl font-bold">
          {formatCurrency(summary.estimatedMonthlyTotal)}
        </p>
      </div>

      <div className="rounded-lg border bg-muted/30 p-4">
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <DollarSign className="h-4 w-4" />
          Annual Estimate
        </div>
        <p className="mt-1 font-mono text-2xl font-bold">
          {formatCurrency(summary.estimatedAnnualTotal)}
        </p>
      </div>

      <div className="rounded-lg border bg-muted/30 p-4">
        <div className="text-sm text-muted-foreground">By Frequency</div>
        <div className="mt-2 flex flex-wrap gap-1">
          {summary.byFrequency
            .filter((f) => f.count > 0)
            .slice(0, 3)
            .map((f) => (
              <Badge key={f.frequency} variant="outline" className="text-xs">
                {FREQUENCY_LABELS[f.frequency]}: {f.count}
              </Badge>
            ))}
        </div>
      </div>
    </div>
  )
}

// Individual subscription card
function SubscriptionCard({
  subscription,
  onAcknowledge,
  onClick,
  isExpanded,
  onToggleExpand,
}: {
  subscription: SubscriptionDetection
  onAcknowledge?: (id: string, acknowledged: boolean) => void
  onClick?: () => void
  isExpanded: boolean
  onToggleExpand: () => void
}) {
  const confidenceStyle = CONFIDENCE_STYLES[subscription.confidence]

  return (
    <motion.div
      variants={staggerChild}
      className={cn(
        'rounded-lg border transition-colors',
        subscription.isAcknowledged && 'bg-muted/30 opacity-75',
        onClick && 'cursor-pointer'
      )}
    >
      {/* Main content */}
      <div className="p-4" onClick={onClick}>
        <div className="flex items-start justify-between gap-3">
          {/* Left side - merchant and details */}
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <Repeat className="h-4 w-4 text-muted-foreground shrink-0" />
              <span className="font-medium truncate">{subscription.merchantName}</span>
              {subscription.isAcknowledged && (
                <CheckCircle2 className="h-4 w-4 text-emerald-500 shrink-0" />
              )}
            </div>

            <div className="mt-1 flex items-center gap-2 text-sm text-muted-foreground">
              <Badge variant="outline" className="text-xs">
                {FREQUENCY_LABELS[subscription.frequency]}
              </Badge>
              <Badge className={cn('text-xs', confidenceStyle.bg, confidenceStyle.text)}>
                {subscription.confidence} confidence
              </Badge>
              {safeDisplayString(subscription.category) && (
                <Badge variant="secondary" className="text-xs">
                  {safeDisplayString(subscription.category)}
                </Badge>
              )}
            </div>
          </div>

          {/* Right side - amount and acknowledge */}
          <div className="flex items-center gap-3 shrink-0">
            <div className="text-right">
              <p className="font-mono text-lg font-semibold">
                {formatCurrency(subscription.amount)}
              </p>
              <p className="text-xs text-muted-foreground">
                per {subscription.frequency === 'annual' ? 'year' : subscription.frequency}
              </p>
            </div>

            {onAcknowledge && (
              <div
                className="flex items-center gap-2"
                onClick={(e) => e.stopPropagation()}
              >
                <Switch
                  checked={subscription.isAcknowledged ?? false}
                  onCheckedChange={(checked) => onAcknowledge(subscription.id, checked)}
                />
              </div>
            )}

            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8"
              onClick={(e) => {
                e.stopPropagation()
                onToggleExpand()
              }}
            >
              {isExpanded ? (
                <ChevronUp className="h-4 w-4" />
              ) : (
                <ChevronDown className="h-4 w-4" />
              )}
            </Button>
          </div>
        </div>
      </div>

      {/* Expanded details */}
      <AnimatePresence>
        {isExpanded && (
          <motion.div
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.2 }}
            className="overflow-hidden"
          >
            <div className="border-t bg-muted/20 p-4">
              <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
                <div>
                  <p className="text-xs text-muted-foreground">First Seen</p>
                  <p className="font-medium">{formatDate(subscription.firstSeen)}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Last Seen</p>
                  <p className="font-medium">{formatDate(subscription.lastSeen)}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Occurrences</p>
                  <p className="font-medium">{subscription.occurrenceCount}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Total Spent</p>
                  <p className="font-mono font-medium">
                    {formatCurrency(subscription.totalSpent)}
                  </p>
                </div>
              </div>

              {subscription.nextExpected && (
                <div className="mt-3 flex items-center gap-2 rounded-md bg-muted/50 p-2 text-sm">
                  <AlertCircle className="h-4 w-4 text-amber-500" />
                  <span>
                    Next expected charge: <strong>{formatDate(subscription.nextExpected)}</strong>
                  </span>
                </div>
              )}

              {subscription.notes && (
                <p className="mt-3 text-sm text-muted-foreground">
                  Note: {subscription.notes}
                </p>
              )}
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </motion.div>
  )
}

export function SubscriptionDetector({
  data,
  isLoading = false,
  groupByCategory = false,
  showSummary = true,
  frequencyFilter = [],
  className,
  title = 'Detected Subscriptions',
  onAcknowledge,
  onSubscriptionClick,
  onRefresh,
}: SubscriptionDetectorProps) {
  const [expandedId, setExpandedId] = useState<string | null>(null)
  const [showAcknowledged, setShowAcknowledged] = useState(true)
  const [selectedFrequency, setSelectedFrequency] = useState<SubscriptionFrequency | null>(null)

  // Filter subscriptions
  const subscriptions = useMemo(() => {
    if (!data?.subscriptions) return []

    let filtered = data.subscriptions

    // Apply frequency filter
    if (frequencyFilter.length > 0) {
      filtered = filtered.filter((s) => frequencyFilter.includes(s.frequency))
    }

    // Apply selected frequency filter
    if (selectedFrequency) {
      filtered = filtered.filter((s) => s.frequency === selectedFrequency)
    }

    // Apply acknowledged filter
    if (!showAcknowledged) {
      filtered = filtered.filter((s) => !s.isAcknowledged)
    }

    // Sort by amount descending
    return [...filtered].sort((a, b) => b.amount - a.amount)
  }, [data?.subscriptions, frequencyFilter, selectedFrequency, showAcknowledged])

  // Group by category if enabled
  const groupedSubscriptions = useMemo(() => {
    if (!groupByCategory) return null

    const groups: Record<string, SubscriptionDetection[]> = {}
    subscriptions.forEach((sub) => {
      const category = sub.category || 'Uncategorized'
      if (!groups[category]) groups[category] = []
      groups[category].push(sub)
    })

    return Object.entries(groups).sort(
      ([, a], [, b]) =>
        b.reduce((sum, s) => sum + s.amount, 0) - a.reduce((sum, s) => sum + s.amount, 0)
    )
  }, [subscriptions, groupByCategory])

  // Get unique frequencies for filter
  const availableFrequencies = useMemo(() => {
    if (!data?.subscriptions) return []
    const freqs = new Set(data.subscriptions.map((s) => s.frequency))
    return Array.from(freqs)
  }, [data?.subscriptions])

  if (isLoading) {
    return (
      <Card className={className}>
        <CardHeader>
          <Skeleton className="h-6 w-48" />
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="h-24 w-full" />
            ))}
          </div>
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-20 w-full" />
          ))}
        </CardContent>
      </Card>
    )
  }

  if (!data?.subscriptions?.length) {
    return (
      <Card className={className}>
        <CardHeader>
          <CardTitle className="text-lg">{title}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col items-center justify-center py-8 text-center">
            <Repeat className="h-12 w-12 text-muted-foreground/50" />
            <p className="mt-3 text-muted-foreground">
              No recurring subscriptions detected
            </p>
            <p className="text-sm text-muted-foreground">
              We&apos;ll analyze your transactions to find recurring charges
            </p>
            {onRefresh && (
              <Button variant="outline" className="mt-4" onClick={onRefresh}>
                <RefreshCcw className="mr-2 h-4 w-4" />
                Analyze Now
              </Button>
            )}
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
              {data.analyzedAt && (
                <p className="text-xs text-muted-foreground mt-1">
                  Last analyzed: {formatDate(data.analyzedAt)}
                </p>
              )}
            </div>
            <div className="flex items-center gap-2">
              {onRefresh && (
                <Button variant="ghost" size="icon" onClick={onRefresh}>
                  <RefreshCcw className="h-4 w-4" />
                </Button>
              )}
            </div>
          </div>
        </CardHeader>

        <CardContent className="space-y-4">
          {/* Summary section */}
          {showSummary && data.summary && (
            <SubscriptionSummaryCard summary={data.summary} />
          )}

          {/* Filters */}
          <div className="flex flex-wrap items-center gap-2">
            <Filter className="h-4 w-4 text-muted-foreground" />

            {/* Frequency filter buttons */}
            <div className="flex flex-wrap gap-1">
              <Button
                variant={selectedFrequency === null ? 'secondary' : 'ghost'}
                size="sm"
                className="h-7"
                onClick={() => setSelectedFrequency(null)}
              >
                All
              </Button>
              {availableFrequencies.map((freq) => (
                <Button
                  key={freq}
                  variant={selectedFrequency === freq ? 'secondary' : 'ghost'}
                  size="sm"
                  className="h-7"
                  onClick={() => setSelectedFrequency(freq)}
                >
                  {FREQUENCY_LABELS[freq]}
                </Button>
              ))}
            </div>

            <div className="ml-auto flex items-center gap-2 text-sm">
              <span className="text-muted-foreground">Show acknowledged</span>
              <Switch
                checked={showAcknowledged}
                onCheckedChange={setShowAcknowledged}
              />
            </div>
          </div>

          {/* New subscriptions alert */}
          {data.newSubscriptions?.length > 0 && (
            <div className="rounded-lg border border-amber-200 bg-amber-50 p-3 dark:border-amber-900 dark:bg-amber-950">
              <div className="flex items-center gap-2 text-amber-800 dark:text-amber-200">
                <AlertCircle className="h-4 w-4" />
                <span className="font-medium">
                  {data.newSubscriptions.length} new subscription
                  {data.newSubscriptions.length > 1 ? 's' : ''} detected
                </span>
              </div>
            </div>
          )}

          {/* Subscription list */}
          {groupedSubscriptions ? (
            // Grouped view
            <div className="space-y-6">
              {groupedSubscriptions.map(([category, subs]) => (
                <div key={category}>
                  <h4 className="mb-3 text-sm font-medium text-muted-foreground">
                    {category} ({subs.length})
                  </h4>
                  <motion.div
                    variants={staggerContainer}
                    initial="hidden"
                    animate="visible"
                    className="space-y-2"
                  >
                    {subs.map((subscription) => (
                      <SubscriptionCard
                        key={subscription.id}
                        subscription={subscription}
                        onAcknowledge={onAcknowledge}
                        onClick={
                          onSubscriptionClick
                            ? () => onSubscriptionClick(subscription)
                            : undefined
                        }
                        isExpanded={expandedId === subscription.id}
                        onToggleExpand={() =>
                          setExpandedId(expandedId === subscription.id ? null : subscription.id)
                        }
                      />
                    ))}
                  </motion.div>
                </div>
              ))}
            </div>
          ) : (
            // Flat list view
            <motion.div
              variants={staggerContainer}
              initial="hidden"
              animate="visible"
              className="space-y-2"
            >
              {subscriptions.length === 0 ? (
                <div className="py-8 text-center text-muted-foreground">
                  No subscriptions match your filters
                </div>
              ) : (
                subscriptions.map((subscription) => (
                  <SubscriptionCard
                    key={subscription.id}
                    subscription={subscription}
                    onAcknowledge={onAcknowledge}
                    onClick={
                      onSubscriptionClick
                        ? () => onSubscriptionClick(subscription)
                        : undefined
                    }
                    isExpanded={expandedId === subscription.id}
                    onToggleExpand={() =>
                      setExpandedId(expandedId === subscription.id ? null : subscription.id)
                    }
                  />
                ))
              )}
            </motion.div>
          )}

          {/* Possibly ended subscriptions */}
          {data.possiblyEnded?.length > 0 && (
            <div className="border-t pt-4">
              <h4 className="mb-3 text-sm font-medium text-muted-foreground">
                Possibly Ended Subscriptions
              </h4>
              <div className="space-y-2">
                {data.possiblyEnded.map((subscription) => (
                  <div
                    key={subscription.id}
                    className="flex items-center justify-between rounded-lg border border-dashed p-3 text-muted-foreground"
                  >
                    <div className="flex items-center gap-2">
                      <Repeat className="h-4 w-4" />
                      <span>{subscription.merchantName}</span>
                    </div>
                    <span className="text-sm">
                      Last seen: {formatDate(subscription.lastSeen)}
                    </span>
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

export default SubscriptionDetector
