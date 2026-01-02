'use client'

/**
 * SpendingTrendChart Component (T079)
 *
 * Displays spending trends over time using Recharts.
 * Supports area, line, and bar chart types with optional category breakdown.
 */

import { useMemo } from 'react'
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import { motion } from 'framer-motion'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Badge } from '@/components/ui/badge'
import { fadeIn } from '@/lib/animations'
import { TrendingUp, TrendingDown, Minus } from 'lucide-react'
import { cn } from '@/lib/utils'

// Chart color palette - theme-aware colors that work in both light and dark mode
// Primary colors adapt via CSS variables in globals.css
const CHART_COLORS = {
  primary: '#2d5f4f',    // emerald (primary in light theme)
  secondary: '#4a8f75',  // lighter emerald
  comparison: '#94a3b8', // slate-400 (muted)
  grid: '#e2e8f0',       // slate-200 (border)
  text: '#64748b',       // slate-500 (muted foreground)
  background: '#f8fafc', // slate-50
  categories: [
    '#2d5f4f', // emerald/primary
    '#10b981', // green
    '#f59e0b', // amber
    '#f43f5e', // rose
    '#4a8f75', // lighter emerald
    '#64748b', // slate-500
  ],
}

// Format currency for tooltips and axes
function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(value)
}

// Format dates for x-axis
function formatDate(dateStr: string, granularity: 'day' | 'week' | 'month'): string {
  const date = new Date(dateStr)
  switch (granularity) {
    case 'day':
      return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
    case 'week':
      return `Week ${Math.ceil(date.getDate() / 7)}`
    case 'month':
      return date.toLocaleDateString('en-US', { month: 'short', year: '2-digit' })
    default:
      return dateStr
  }
}

interface SpendingTrendPoint {
  period: string
  periodLabel?: string
  amount: number
  previousAmount?: number
  changePercent?: number
  transactionCount: number
  averageTransaction?: number
  categories?: Array<{
    category: string
    amount: number
    percentage: number
  }>
}

interface SpendingTrendChartProps {
  data?: SpendingTrendPoint[]
  isLoading?: boolean
  chartType?: 'area' | 'line' | 'bar'
  granularity?: 'day' | 'week' | 'month'
  showCategories?: boolean
  selectedCategories?: string[]
  showComparison?: boolean
  showLegend?: boolean
  height?: number
  className?: string
  title?: string
  onPointClick?: (point: SpendingTrendPoint) => void
}

// Custom tooltip component
function ChartTooltip({
  active,
  payload,
  label,
  granularity,
}: {
  active?: boolean
  payload?: Array<{ value: number; name: string; color: string }>
  label?: string
  granularity: 'day' | 'week' | 'month'
}) {
  if (!active || !payload?.length) return null

  return (
    <div className="rounded-lg border bg-background p-3 shadow-lg">
      <p className="mb-2 text-sm font-medium text-muted-foreground">
        {formatDate(label || '', granularity)}
      </p>
      {payload.map((entry, index) => (
        <div key={index} className="flex items-center justify-between gap-4">
          <div className="flex items-center gap-2">
            <div
              className="h-2 w-2 rounded-full"
              style={{ backgroundColor: entry.color }}
            />
            <span className="text-sm capitalize">{entry.name}</span>
          </div>
          <span className="font-mono text-sm font-semibold">
            {formatCurrency(entry.value)}
          </span>
        </div>
      ))}
    </div>
  )
}

// Summary stats component
function TrendSummary({
  data,
}: {
  data: SpendingTrendPoint[]
}) {
  const summary = useMemo(() => {
    if (!data.length) return null

    const total = data.reduce((sum, d) => sum + d.amount, 0)
    const average = total / data.length
    const highest = data.reduce((max, d) => (d.amount > max.amount ? d : max), data[0])
    const lowest = data.reduce((min, d) => (d.amount < min.amount ? d : min), data[0])

    // Calculate overall trend
    const firstHalf = data.slice(0, Math.floor(data.length / 2))
    const secondHalf = data.slice(Math.floor(data.length / 2))
    const firstAvg = firstHalf.reduce((sum, d) => sum + d.amount, 0) / firstHalf.length
    const secondAvg = secondHalf.reduce((sum, d) => sum + d.amount, 0) / secondHalf.length
    const trendPercent = firstAvg ? ((secondAvg - firstAvg) / firstAvg) * 100 : 0

    return { total, average, highest, lowest, trendPercent }
  }, [data])

  if (!summary) return null

  const TrendIcon = summary.trendPercent > 5
    ? TrendingUp
    : summary.trendPercent < -5
    ? TrendingDown
    : Minus

  const trendColor = summary.trendPercent > 5
    ? 'text-rose-500'
    : summary.trendPercent < -5
    ? 'text-emerald-500'
    : 'text-slate-500'

  return (
    <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
      <div>
        <p className="text-xs text-muted-foreground">Total</p>
        <p className="font-mono text-lg font-semibold">
          {formatCurrency(summary.total)}
        </p>
      </div>
      <div>
        <p className="text-xs text-muted-foreground">Average</p>
        <p className="font-mono text-lg font-semibold">
          {formatCurrency(summary.average)}
        </p>
      </div>
      <div>
        <p className="text-xs text-muted-foreground">Trend</p>
        <div className="flex items-center gap-1">
          <TrendIcon className={cn('h-4 w-4', trendColor)} />
          <span className={cn('font-mono text-lg font-semibold', trendColor)}>
            {Math.abs(summary.trendPercent).toFixed(1)}%
          </span>
        </div>
      </div>
      <div>
        <p className="text-xs text-muted-foreground">Peak</p>
        <p className="font-mono text-lg font-semibold">
          {formatCurrency(summary.highest.amount)}
        </p>
      </div>
    </div>
  )
}

export function SpendingTrendChart({
  data = [],
  isLoading = false,
  chartType = 'area',
  granularity = 'day',
  showCategories = false,
  selectedCategories = [],
  showComparison = false,
  showLegend = true,
  height = 300,
  className,
  title = 'Spending Trends',
  onPointClick,
}: SpendingTrendChartProps) {
  // Transform data for chart
  const chartData = useMemo(() => {
    return data.map((point) => ({
      ...point,
      label: formatDate(point.period, granularity),
      // Flatten categories for stacked charts
      ...(showCategories && point.categories
        ? point.categories.reduce(
            (acc, cat) => ({ ...acc, [cat.category]: cat.amount }),
            {}
          )
        : {}),
    }))
  }, [data, granularity, showCategories])

  // Get unique categories for legend
  const categories = useMemo(() => {
    if (!showCategories || !data.length) return []
    const cats = new Set<string>()
    data.forEach((point) => {
      point.categories?.forEach((cat) => cats.add(cat.category))
    })
    return Array.from(cats).slice(0, 6) // Limit to 6 categories
  }, [data, showCategories])

  // Filter categories if selection provided
  const visibleCategories = useMemo(() => {
    if (!selectedCategories.length) return categories
    return categories.filter((cat) => selectedCategories.includes(cat))
  }, [categories, selectedCategories])

  // Handle chart click events - must be before early returns (Rules of Hooks)
  const handleChartClick = useMemo(() => {
    if (!onPointClick) return undefined
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return (data: any) => {
      if (data?.activePayload?.[0]?.payload) {
        onPointClick(data.activePayload[0].payload as SpendingTrendPoint)
      }
    }
  }, [onPointClick])

  if (isLoading) {
    return (
      <Card className={className}>
        <CardHeader>
          <Skeleton className="h-6 w-40" />
        </CardHeader>
        <CardContent>
          <Skeleton className="h-[300px] w-full" />
        </CardContent>
      </Card>
    )
  }

  if (!data.length) {
    return (
      <Card className={className}>
        <CardHeader>
          <CardTitle className="text-lg">{title}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex h-[300px] items-center justify-center text-muted-foreground">
            No spending data available for this period
          </div>
        </CardContent>
      </Card>
    )
  }

  // Common chart props
  const commonProps = {
    data: chartData,
    margin: { top: 10, right: 10, left: 0, bottom: 0 },
    onClick: handleChartClick,
  }

  const renderChart = () => {
    if (showCategories && categories.length > 0) {
      // Stacked chart for category breakdown
      if (chartType === 'bar') {
        return (
          <BarChart {...commonProps}>
            <CartesianGrid strokeDasharray="3 3" stroke={CHART_COLORS.grid} />
            <XAxis
              dataKey="label"
              tick={{ fontSize: 12, fill: CHART_COLORS.text }}
              tickLine={false}
            />
            <YAxis
              tick={{ fontSize: 12, fill: CHART_COLORS.text }}
              tickFormatter={(v) => formatCurrency(v)}
              tickLine={false}
              axisLine={false}
            />
            <Tooltip content={<ChartTooltip granularity={granularity} />} />
            {showLegend && <Legend />}
            {visibleCategories.map((category, index) => (
              <Bar
                key={category}
                dataKey={category}
                name={category}
                stackId="categories"
                fill={CHART_COLORS.categories[index % CHART_COLORS.categories.length]}
                radius={index === visibleCategories.length - 1 ? [4, 4, 0, 0] : [0, 0, 0, 0]}
              />
            ))}
          </BarChart>
        )
      }

      return (
        <AreaChart {...commonProps}>
          <CartesianGrid strokeDasharray="3 3" stroke={CHART_COLORS.grid} />
          <XAxis
            dataKey="label"
            tick={{ fontSize: 12, fill: CHART_COLORS.text }}
            tickLine={false}
          />
          <YAxis
            tick={{ fontSize: 12, fill: CHART_COLORS.text }}
            tickFormatter={(v) => formatCurrency(v)}
            tickLine={false}
            axisLine={false}
          />
          <Tooltip content={<ChartTooltip granularity={granularity} />} />
          {showLegend && <Legend />}
          {visibleCategories.map((category, index) => (
            <Area
              key={category}
              type="monotone"
              dataKey={category}
              name={category}
              stackId="categories"
              stroke={CHART_COLORS.categories[index % CHART_COLORS.categories.length]}
              fill={CHART_COLORS.categories[index % CHART_COLORS.categories.length]}
              fillOpacity={0.6}
            />
          ))}
        </AreaChart>
      )
    }

    // Simple single-series chart
    switch (chartType) {
      case 'bar':
        return (
          <BarChart {...commonProps}>
            <CartesianGrid strokeDasharray="3 3" stroke={CHART_COLORS.grid} />
            <XAxis
              dataKey="label"
              tick={{ fontSize: 12, fill: CHART_COLORS.text }}
              tickLine={false}
            />
            <YAxis
              tick={{ fontSize: 12, fill: CHART_COLORS.text }}
              tickFormatter={(v) => formatCurrency(v)}
              tickLine={false}
              axisLine={false}
            />
            <Tooltip content={<ChartTooltip granularity={granularity} />} />
            {showLegend && <Legend />}
            <Bar
              dataKey="amount"
              name="Spending"
              fill={CHART_COLORS.primary}
              radius={[4, 4, 0, 0]}
            />
            {showComparison && (
              <Bar
                dataKey="previousAmount"
                name="Previous Period"
                fill={CHART_COLORS.comparison}
                radius={[4, 4, 0, 0]}
              />
            )}
          </BarChart>
        )

      case 'line':
        return (
          <LineChart {...commonProps}>
            <CartesianGrid strokeDasharray="3 3" stroke={CHART_COLORS.grid} />
            <XAxis
              dataKey="label"
              tick={{ fontSize: 12, fill: CHART_COLORS.text }}
              tickLine={false}
            />
            <YAxis
              tick={{ fontSize: 12, fill: CHART_COLORS.text }}
              tickFormatter={(v) => formatCurrency(v)}
              tickLine={false}
              axisLine={false}
            />
            <Tooltip content={<ChartTooltip granularity={granularity} />} />
            {showLegend && <Legend />}
            <Line
              type="monotone"
              dataKey="amount"
              name="Spending"
              stroke={CHART_COLORS.primary}
              strokeWidth={2}
              dot={{ fill: CHART_COLORS.primary, strokeWidth: 0, r: 3 }}
              activeDot={{ r: 5, fill: CHART_COLORS.primary }}
            />
            {showComparison && (
              <Line
                type="monotone"
                dataKey="previousAmount"
                name="Previous Period"
                stroke={CHART_COLORS.comparison}
                strokeWidth={2}
                strokeDasharray="5 5"
                dot={false}
              />
            )}
          </LineChart>
        )

      case 'area':
      default:
        return (
          <AreaChart {...commonProps}>
            <defs>
              <linearGradient id="spendingGradient" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor={CHART_COLORS.primary} stopOpacity={0.3} />
                <stop offset="95%" stopColor={CHART_COLORS.primary} stopOpacity={0} />
              </linearGradient>
              <linearGradient id="comparisonGradient" x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor={CHART_COLORS.comparison} stopOpacity={0.2} />
                <stop offset="95%" stopColor={CHART_COLORS.comparison} stopOpacity={0} />
              </linearGradient>
            </defs>
            <CartesianGrid strokeDasharray="3 3" stroke={CHART_COLORS.grid} />
            <XAxis
              dataKey="label"
              tick={{ fontSize: 12, fill: CHART_COLORS.text }}
              tickLine={false}
            />
            <YAxis
              tick={{ fontSize: 12, fill: CHART_COLORS.text }}
              tickFormatter={(v) => formatCurrency(v)}
              tickLine={false}
              axisLine={false}
            />
            <Tooltip content={<ChartTooltip granularity={granularity} />} />
            {showLegend && <Legend />}
            {showComparison && (
              <Area
                type="monotone"
                dataKey="previousAmount"
                name="Previous Period"
                stroke={CHART_COLORS.comparison}
                fill="url(#comparisonGradient)"
                strokeWidth={1}
              />
            )}
            <Area
              type="monotone"
              dataKey="amount"
              name="Spending"
              stroke={CHART_COLORS.primary}
              fill="url(#spendingGradient)"
              strokeWidth={2}
            />
          </AreaChart>
        )
    }
  }

  return (
    <motion.div variants={fadeIn} initial="hidden" animate="visible">
      <Card className={className}>
        <CardHeader className="pb-2">
          <div className="flex items-center justify-between">
            <CardTitle className="text-lg">{title}</CardTitle>
            <div className="flex gap-1">
              <Badge variant="outline" className="text-xs">
                {data.length} periods
              </Badge>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-4">
          <TrendSummary data={data} />
          <ResponsiveContainer width="100%" height={height}>
            {renderChart()}
          </ResponsiveContainer>
        </CardContent>
      </Card>
    </motion.div>
  )
}

export default SpendingTrendChart
