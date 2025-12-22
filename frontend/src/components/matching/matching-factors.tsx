"use client"

/**
 * MatchingFactors Component (T069)
 *
 * Displays the individual factors that contributed to a match's confidence score.
 * Shows weight, values from both sides, and match quality for each factor.
 */

import { motion } from 'framer-motion'
import { cn } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { ConfidenceIndicator } from '@/components/design-system/confidence-indicator'
import { staggerChild, staggerContainer } from '@/lib/animations'
import {
  DollarSign,
  Calendar,
  Store,
  Tag,
  Check,
  Info,
} from 'lucide-react'

interface MatchingFactor {
  type: 'amount' | 'date' | 'merchant' | 'category'
  weight: number
  receiptValue: string
  transactionValue: string
  isExactMatch: boolean
}

interface MatchingFactorsProps {
  factors: MatchingFactor[]
  confidence: number
  highlightedFactor?: MatchingFactor['type']
  onHover?: (factor: MatchingFactor['type'] | null) => void
  compact?: boolean
}

const FACTOR_ICONS = {
  amount: DollarSign,
  date: Calendar,
  merchant: Store,
  category: Tag,
} as const

const FACTOR_LABELS = {
  amount: 'Amount',
  date: 'Date',
  merchant: 'Merchant',
  category: 'Category',
} as const

/**
 * Get color class based on factor match quality
 */
function getMatchColor(isExactMatch: boolean, weight: number): string {
  if (isExactMatch) {
    return 'text-emerald-600 dark:text-emerald-400'
  }
  if (weight >= 0.3) {
    return 'text-amber-600 dark:text-amber-400'
  }
  return 'text-rose-600 dark:text-rose-400'
}

/**
 * Get background class for highlighted factor
 */
function getHighlightClass(isHighlighted: boolean): string {
  return isHighlighted
    ? 'bg-accent/10 ring-2 ring-accent/20'
    : 'bg-muted/50 hover:bg-muted/80'
}

export function MatchingFactors({
  factors,
  confidence,
  highlightedFactor,
  onHover,
  compact = false,
}: MatchingFactorsProps) {
  // Sort factors by weight (highest first)
  const sortedFactors = [...factors].sort((a, b) => b.weight - a.weight)

  if (compact) {
    return (
      <div className="flex items-center gap-2">
        <ConfidenceIndicator score={confidence} size="sm" />
        <div className="flex items-center gap-1">
          {sortedFactors.map((factor) => {
            const Icon = FACTOR_ICONS[factor.type]
            return (
              <TooltipProvider key={factor.type}>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <div
                      className={cn(
                        'flex items-center justify-center w-6 h-6 rounded',
                        factor.isExactMatch
                          ? 'bg-emerald-100 text-emerald-600 dark:bg-emerald-900/30 dark:text-emerald-400'
                          : 'bg-amber-100 text-amber-600 dark:bg-amber-900/30 dark:text-amber-400'
                      )}
                    >
                      <Icon className="h-3.5 w-3.5" />
                    </div>
                  </TooltipTrigger>
                  <TooltipContent side="bottom">
                    <p className="font-medium">{FACTOR_LABELS[factor.type]}</p>
                    <p className="text-xs text-muted-foreground">
                      {factor.receiptValue} ↔ {factor.transactionValue}
                    </p>
                  </TooltipContent>
                </Tooltip>
              </TooltipProvider>
            )
          })}
        </div>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      {/* Overall Confidence */}
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium text-muted-foreground">
          Match Confidence
        </span>
        <div className="flex items-center gap-2">
          <ConfidenceIndicator score={confidence} size="md" showLabel />
          <span className="text-lg font-semibold font-mono">
            {Math.round(confidence * 100)}%
          </span>
        </div>
      </div>

      {/* Factor Breakdown */}
      <motion.div
        variants={staggerContainer}
        initial="hidden"
        animate="visible"
        className="space-y-2"
      >
        {sortedFactors.map((factor) => {
          const Icon = FACTOR_ICONS[factor.type]
          const isHighlighted = highlightedFactor === factor.type

          return (
            <motion.div
              key={factor.type}
              variants={staggerChild}
              className={cn(
                'rounded-lg p-3 transition-all cursor-pointer',
                getHighlightClass(isHighlighted)
              )}
              onMouseEnter={() => onHover?.(factor.type)}
              onMouseLeave={() => onHover?.(null)}
            >
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center gap-2">
                  <div
                    className={cn(
                      'flex items-center justify-center w-8 h-8 rounded-full',
                      factor.isExactMatch
                        ? 'bg-emerald-100 dark:bg-emerald-900/30'
                        : 'bg-amber-100 dark:bg-amber-900/30'
                    )}
                  >
                    <Icon
                      className={cn(
                        'h-4 w-4',
                        getMatchColor(factor.isExactMatch, factor.weight)
                      )}
                    />
                  </div>
                  <div>
                    <span className="font-medium">
                      {FACTOR_LABELS[factor.type]}
                    </span>
                    <Badge
                      variant="secondary"
                      className="ml-2 text-xs font-mono"
                    >
                      {Math.round(factor.weight * 100)}% weight
                    </Badge>
                  </div>
                </div>
                <div
                  className={cn(
                    'flex items-center gap-1',
                    getMatchColor(factor.isExactMatch, factor.weight)
                  )}
                >
                  {factor.isExactMatch ? (
                    <>
                      <Check className="h-4 w-4" />
                      <span className="text-sm font-medium">Exact</span>
                    </>
                  ) : (
                    <>
                      <Info className="h-4 w-4" />
                      <span className="text-sm font-medium">Partial</span>
                    </>
                  )}
                </div>
              </div>

              {/* Value Comparison */}
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <span className="text-muted-foreground text-xs">Receipt</span>
                  <p className="font-medium truncate">{factor.receiptValue}</p>
                </div>
                <div>
                  <span className="text-muted-foreground text-xs">
                    Transaction
                  </span>
                  <p className="font-medium truncate">
                    {factor.transactionValue}
                  </p>
                </div>
              </div>
            </motion.div>
          )
        })}
      </motion.div>

      {/* Legend */}
      <div className="flex items-center justify-center gap-4 pt-2 border-t text-xs text-muted-foreground">
        <div className="flex items-center gap-1">
          <div className="w-3 h-3 rounded-full bg-emerald-500" />
          <span>Exact Match</span>
        </div>
        <div className="flex items-center gap-1">
          <div className="w-3 h-3 rounded-full bg-amber-500" />
          <span>Partial Match</span>
        </div>
        <div className="flex items-center gap-1">
          <div className="w-3 h-3 rounded-full bg-rose-500" />
          <span>Weak Match</span>
        </div>
      </div>
    </div>
  )
}

/**
 * Utility to convert API factor scores to MatchingFactor array
 */
export function buildMatchingFactors(
  amountScore: number,
  dateScore: number,
  vendorScore: number,
  receiptData: { amount?: number; date?: string; vendor?: string },
  transactionData: { amount: number; date: string; description: string }
): MatchingFactor[] {
  return [
    {
      type: 'amount',
      weight: 0.4, // Amount is weighted heavily
      receiptValue: receiptData.amount
        ? `$${receiptData.amount.toFixed(2)}`
        : 'Unknown',
      transactionValue: `$${transactionData.amount.toFixed(2)}`,
      isExactMatch: amountScore >= 0.95,
    },
    {
      type: 'date',
      weight: 0.3,
      receiptValue: receiptData.date || 'Unknown',
      transactionValue: transactionData.date,
      isExactMatch: dateScore >= 0.95,
    },
    {
      type: 'merchant',
      weight: 0.3,
      receiptValue: receiptData.vendor || 'Unknown',
      transactionValue: transactionData.description,
      isExactMatch: vendorScore >= 0.9,
    },
  ]
}
