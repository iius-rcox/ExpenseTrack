'use client'

/**
 * CategoryBreakdown Component (T080)
 *
 * Displays spending breakdown by category using pie/donut charts
 * and bar charts with comparison capabilities.
 */

import { useMemo, useState } from 'react'
import {
  Cell,
  Legend,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  Bar,
  BarChart,
  XAxis,
  YAxis,
  CartesianGrid,
} from 'recharts'
import { motion, AnimatePresence } from 'framer-motion'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Button } from '@/components/ui/button'
import { fadeIn, staggerContainer, staggerChild } from '@/lib/animations'
import { cn } from '@/lib/utils'
import {
  PieChart as PieChartIcon,
  BarChart2,
  List,
  TrendingUp,
  TrendingDown,
} from 'lucide-react'

// Chart color palette - theme-aware colors that work in both light and dark mode
// These map to the chart-1 through chart-5 CSS variables defined in globals.css
const CATEGORY_COLORS = [
  '#2d5f4f', // emerald/primary (light) -> cyan (dark via CSS)
  '#10b981', // green/secondary
  '#f59e0b', // amber
  '#f43f5e', // rose
  '#4a8f75', // lighter emerald
  '#64748b', // slate-500
  '#94a3b8', // slate-400
  '#1e4d40', // darker emerald
]

// Theme-aware muted color for "Other" category
const MUTED_COLOR = '#94a3b8'

// Theme-aware stroke color for selected items
const STROKE_COLOR = '#0f172a'

// Format currency
function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(value)
}

interface CategoryData {
  category: string
  categoryId?: string
  amount: number
  percentage: number
  transactionCount: number
  previousAmount?: number
  changePercent?: number
  color?: string
}

interface CategoryBreakdownProps {
  data?: CategoryData[]
  isLoading?: boolean
  chartType?: 'pie' | 'donut' | 'bar' | 'list'
  showComparison?: boolean
  maxCategories?: number
  height?: number
  className?: string
  title?: string
  onCategorySelect?: (category: CategoryData) => void
  selectedCategory?: string
}

// Custom tooltip for pie/donut charts
function PieTooltip({
  active,
  payload,
}: {
  active?: boolean
  payload?: Array<{
    value: number
    name: string
    payload: { percentage: number; transactionCount: number }
  }>
}) {
  if (!active || !payload?.length) return null

  const data = payload[0]

  return (
    <div className="rounded-lg border bg-background p-3 shadow-lg">
      <p className="mb-1 font-medium">{data.name}</p>
      <div className="space-y-1 text-sm">
        <div className="flex justify-between gap-4">
          <span className="text-muted-foreground">Amount</span>
          <span className="font-mono font-semibold">{formatCurrency(data.value)}</span>
        </div>
        <div className="flex justify-between gap-4">
          <span className="text-muted-foreground">Share</span>
          <span className="font-mono">{data.payload.percentage.toFixed(1)}%</span>
        </div>
        <div className="flex justify-between gap-4">
          <span className="text-muted-foreground">Transactions</span>
          <span className="font-mono">{data.payload.transactionCount}</span>
        </div>
      </div>
    </div>
  )
}

// List view component
function CategoryList({
  data,
  showComparison,
  selectedCategory,
  onCategorySelect,
}: {
  data: CategoryData[]
  showComparison: boolean
  selectedCategory?: string
  onCategorySelect?: (category: CategoryData) => void
}) {
  return (
    <motion.div
      variants={staggerContainer}
      initial="hidden"
      animate="visible"
      className="space-y-2"
    >
      {data.map((category, index) => {
        const isSelected = selectedCategory === category.category
        const hasChange = category.changePercent !== undefined

        return (
          <motion.div
            key={category.category}
            variants={staggerChild}
            className={cn(
              'flex items-center gap-3 rounded-lg border p-3 transition-colors',
              onCategorySelect && 'cursor-pointer hover:bg-muted/50',
              isSelected && 'border-primary bg-muted/50'
            )}
            onClick={() => onCategorySelect?.(category)}
          >
            {/* Color indicator */}
            <div
              className="h-3 w-3 rounded-full shrink-0"
              style={{
                backgroundColor:
                  category.color || CATEGORY_COLORS[index % CATEGORY_COLORS.length],
              }}
            />

            {/* Category name and percentage bar */}
            <div className="flex-1 min-w-0">
              <div className="flex items-center justify-between mb-1">
                <span className="font-medium truncate">{category.category}</span>
                <span className="font-mono text-sm">{category.percentage.toFixed(1)}%</span>
              </div>
              <div className="h-1.5 bg-muted rounded-full overflow-hidden">
                <div
                  className="h-full rounded-full transition-all duration-500"
                  style={{
                    width: `${category.percentage}%`,
                    backgroundColor:
                      category.color || CATEGORY_COLORS[index % CATEGORY_COLORS.length],
                  }}
                />
              </div>
            </div>

            {/* Amount and change */}
            <div className="text-right shrink-0">
              <p className="font-mono font-semibold">{formatCurrency(category.amount)}</p>
              {showComparison && hasChange && (
                <div
                  className={cn(
                    'flex items-center justify-end gap-1 text-xs',
                    category.changePercent! > 0 ? 'text-rose-500' : 'text-emerald-500'
                  )}
                >
                  {category.changePercent! > 0 ? (
                    <TrendingUp className="h-3 w-3" />
                  ) : (
                    <TrendingDown className="h-3 w-3" />
                  )}
                  <span>{Math.abs(category.changePercent!).toFixed(1)}%</span>
                </div>
              )}
            </div>
          </motion.div>
        )
      })}
    </motion.div>
  )
}

export function CategoryBreakdown({
  data = [],
  isLoading = false,
  chartType = 'donut',
  showComparison = false,
  maxCategories = 8,
  height = 300,
  className,
  title = 'Spending by Category',
  onCategorySelect,
  selectedCategory,
}: CategoryBreakdownProps) {
  const [viewType, setViewType] = useState(chartType)

  // Process data - limit categories and add "Other" if needed
  const processedData = useMemo(() => {
    if (!data.length) return []

    // Sort by amount descending
    const sorted = [...data].sort((a, b) => b.amount - a.amount)

    if (sorted.length <= maxCategories) {
      return sorted.map((cat, i) => ({
        ...cat,
        color: cat.color || CATEGORY_COLORS[i % CATEGORY_COLORS.length],
      }))
    }

    // Group remaining into "Other"
    const top = sorted.slice(0, maxCategories - 1)
    const rest = sorted.slice(maxCategories - 1)

    const otherAmount = rest.reduce((sum, cat) => sum + cat.amount, 0)
    const otherCount = rest.reduce((sum, cat) => sum + cat.transactionCount, 0)
    const total = sorted.reduce((sum, cat) => sum + cat.amount, 0)

    const result = [
      ...top.map((cat, i) => ({
        ...cat,
        color: cat.color || CATEGORY_COLORS[i % CATEGORY_COLORS.length],
      })),
      {
        category: 'Other',
        amount: otherAmount,
        percentage: (otherAmount / total) * 100,
        transactionCount: otherCount,
        color: MUTED_COLOR,
      },
    ]

    return result
  }, [data, maxCategories])

  // Calculate total
  const total = useMemo(
    () => processedData.reduce((sum, cat) => sum + cat.amount, 0),
    [processedData]
  )

  if (isLoading) {
    return (
      <Card className={className}>
        <CardHeader>
          <Skeleton className="h-6 w-48" />
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-center" style={{ height }}>
            <Skeleton className="h-48 w-48 rounded-full" />
          </div>
        </CardContent>
      </Card>
    )
  }

  if (!processedData.length) {
    return (
      <Card className={className}>
        <CardHeader>
          <CardTitle className="text-lg">{title}</CardTitle>
        </CardHeader>
        <CardContent>
          <div
            className="flex items-center justify-center text-muted-foreground"
            style={{ height }}
          >
            No category data available
          </div>
        </CardContent>
      </Card>
    )
  }

  const renderChart = () => {
    switch (viewType) {
      case 'pie':
      case 'donut':
        return (
          <ResponsiveContainer width="100%" height={height}>
            <PieChart>
              <Pie
                data={processedData}
                dataKey="amount"
                nameKey="category"
                cx="50%"
                cy="50%"
                innerRadius={viewType === 'donut' ? '50%' : 0}
                outerRadius="80%"
                paddingAngle={2}
                onClick={(_, index) => onCategorySelect?.(processedData[index])}
                style={{ cursor: onCategorySelect ? 'pointer' : 'default' }}
              >
                {processedData.map((entry) => (
                  <Cell
                    key={entry.category}
                    fill={entry.color}
                    stroke={
                      selectedCategory === entry.category ? STROKE_COLOR : 'transparent'
                    }
                    strokeWidth={selectedCategory === entry.category ? 2 : 0}
                    opacity={
                      selectedCategory && selectedCategory !== entry.category ? 0.5 : 1
                    }
                  />
                ))}
              </Pie>
              <Tooltip content={<PieTooltip />} />
              <Legend
                layout="vertical"
                align="right"
                verticalAlign="middle"
                formatter={(value) => (
                  <span className="text-sm text-foreground">{value}</span>
                )}
              />
            </PieChart>
          </ResponsiveContainer>
        )

      case 'bar':
        return (
          <ResponsiveContainer width="100%" height={height}>
            <BarChart
              data={processedData}
              layout="vertical"
              margin={{ top: 0, right: 20, left: 80, bottom: 0 }}
            >
              <CartesianGrid strokeDasharray="3 3" horizontal className="stroke-border" />
              <XAxis
                type="number"
                tickFormatter={(v) => formatCurrency(v)}
                tick={{ fontSize: 12 }}
                className="fill-muted-foreground"
              />
              <YAxis
                dataKey="category"
                type="category"
                tick={{ fontSize: 12 }}
                className="fill-muted-foreground"
                width={75}
              />
              <Tooltip
                formatter={(value) => formatCurrency(value as number)}
                labelStyle={{ fontWeight: 'bold' }}
              />
              <Bar
                dataKey="amount"
                radius={[0, 4, 4, 0]}
                onClick={(data) => onCategorySelect?.(data as unknown as CategoryData)}
                style={{ cursor: onCategorySelect ? 'pointer' : 'default' }}
              >
                {processedData.map((entry) => (
                  <Cell
                    key={entry.category}
                    fill={entry.color}
                    opacity={
                      selectedCategory && selectedCategory !== entry.category ? 0.5 : 1
                    }
                  />
                ))}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        )

      case 'list':
        return (
          <div style={{ height, overflowY: 'auto' }}>
            <CategoryList
              data={processedData}
              showComparison={showComparison}
              selectedCategory={selectedCategory}
              onCategorySelect={onCategorySelect}
            />
          </div>
        )

      default:
        return null
    }
  }

  return (
    <motion.div variants={fadeIn} initial="hidden" animate="visible">
      <Card className={className}>
        <CardHeader className="pb-2">
          <div className="flex items-center justify-between">
            <div className="space-y-1">
              <CardTitle className="text-lg">{title}</CardTitle>
              <p className="text-sm text-muted-foreground">
                Total: <span className="font-mono font-semibold">{formatCurrency(total)}</span>
              </p>
            </div>
            <div className="flex items-center gap-1">
              <Button
                variant={viewType === 'donut' ? 'secondary' : 'ghost'}
                size="icon"
                className="h-8 w-8"
                onClick={() => setViewType('donut')}
              >
                <PieChartIcon className="h-4 w-4" />
              </Button>
              <Button
                variant={viewType === 'bar' ? 'secondary' : 'ghost'}
                size="icon"
                className="h-8 w-8"
                onClick={() => setViewType('bar')}
              >
                <BarChart2 className="h-4 w-4" />
              </Button>
              <Button
                variant={viewType === 'list' ? 'secondary' : 'ghost'}
                size="icon"
                className="h-8 w-8"
                onClick={() => setViewType('list')}
              >
                <List className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <AnimatePresence mode="wait">
            <motion.div
              key={viewType}
              initial={{ opacity: 0, y: 10 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -10 }}
              transition={{ duration: 0.2 }}
            >
              {renderChart()}
            </motion.div>
          </AnimatePresence>
        </CardContent>
      </Card>
    </motion.div>
  )
}

export default CategoryBreakdown
