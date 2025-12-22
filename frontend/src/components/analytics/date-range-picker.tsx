'use client'

/**
 * DateRangePicker Component (T083)
 *
 * Allows selection of date ranges for analytics.
 * Supports presets and custom date selection.
 */

import { useState, useMemo, useCallback } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { fadeIn } from '@/lib/animations'
import { cn } from '@/lib/utils'
import {
  Calendar,
  ChevronDown,
  Clock,
  CalendarDays,
  CalendarRange,
  Check,
} from 'lucide-react'
import type { AnalyticsDateRange, DateRangePreset } from '@/types/analytics'
import { getDateRangeFromPreset } from '@/hooks/queries/use-analytics'

// Preset definitions
const PRESETS: Array<{
  value: DateRangePreset
  label: string
  description?: string
  icon?: 'recent' | 'month' | 'quarter' | 'year'
}> = [
  { value: 'last7days', label: 'Last 7 Days', icon: 'recent' },
  { value: 'last30days', label: 'Last 30 Days', icon: 'recent' },
  { value: 'last90days', label: 'Last 90 Days', icon: 'recent' },
  { value: 'thisMonth', label: 'This Month', icon: 'month' },
  { value: 'lastMonth', label: 'Last Month', icon: 'month' },
  { value: 'thisQuarter', label: 'This Quarter', icon: 'quarter' },
  { value: 'lastQuarter', label: 'Last Quarter', icon: 'quarter' },
  { value: 'thisYear', label: 'This Year', icon: 'year' },
  { value: 'lastYear', label: 'Last Year', icon: 'year' },
]

function PresetIcon({ type }: { type?: 'recent' | 'month' | 'quarter' | 'year' }) {
  switch (type) {
    case 'recent':
      return <Clock className="h-4 w-4" />
    case 'month':
      return <Calendar className="h-4 w-4" />
    case 'quarter':
      return <CalendarRange className="h-4 w-4" />
    case 'year':
      return <CalendarDays className="h-4 w-4" />
    default:
      return <Calendar className="h-4 w-4" />
  }
}

// Format date for display
function formatDisplayDate(dateStr: string): string {
  const date = new Date(dateStr)
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

// Calculate days in range
function getDayCount(startDate: string, endDate: string): number {
  const start = new Date(startDate)
  const end = new Date(endDate)
  return Math.ceil((end.getTime() - start.getTime()) / (1000 * 60 * 60 * 24)) + 1
}

interface DateRangePickerProps {
  value: AnalyticsDateRange
  onChange: (range: AnalyticsDateRange) => void
  className?: string
  showDayCount?: boolean
  allowCustom?: boolean
  size?: 'sm' | 'md' | 'lg'
}

export function DateRangePicker({
  value,
  onChange,
  className,
  showDayCount = true,
  allowCustom = true,
  size = 'md',
}: DateRangePickerProps) {
  const [isOpen, setIsOpen] = useState(false)
  const [customStart, setCustomStart] = useState(value.startDate)
  const [customEnd, setCustomEnd] = useState(value.endDate)
  const [showCustom, setShowCustom] = useState(value.preset === 'custom')

  // Calculate day count
  const dayCount = useMemo(
    () => getDayCount(value.startDate, value.endDate),
    [value.startDate, value.endDate]
  )

  // Handle preset selection
  const handlePresetSelect = useCallback(
    (preset: DateRangePreset) => {
      const range = getDateRangeFromPreset(preset)
      onChange(range)
      setShowCustom(false)
      setIsOpen(false)
    },
    [onChange]
  )

  // Handle custom date application
  const handleApplyCustom = useCallback(() => {
    if (customStart && customEnd && customStart <= customEnd) {
      onChange({
        startDate: customStart,
        endDate: customEnd,
        preset: 'custom',
        label: `${formatDisplayDate(customStart)} - ${formatDisplayDate(customEnd)}`,
      })
      setIsOpen(false)
    }
  }, [customStart, customEnd, onChange])

  // Validate custom dates
  const isCustomValid = useMemo(() => {
    if (!customStart || !customEnd) return false
    return customStart <= customEnd
  }, [customStart, customEnd])

  // Size classes
  const sizeClasses = {
    sm: 'h-8 text-xs px-2',
    md: 'h-9 text-sm px-3',
    lg: 'h-10 text-base px-4',
  }

  return (
    <DropdownMenu open={isOpen} onOpenChange={setIsOpen}>
      <DropdownMenuTrigger asChild>
        <Button
          variant="outline"
          className={cn(
            'justify-between gap-2 font-normal',
            sizeClasses[size],
            className
          )}
        >
          <div className="flex items-center gap-2">
            <Calendar className="h-4 w-4 text-muted-foreground" />
            <span>{value.label}</span>
          </div>
          <div className="flex items-center gap-2">
            {showDayCount && (
              <Badge variant="secondary" className="text-xs font-mono">
                {dayCount}d
              </Badge>
            )}
            <ChevronDown className="h-4 w-4 text-muted-foreground" />
          </div>
        </Button>
      </DropdownMenuTrigger>

      <DropdownMenuContent align="start" className="w-64">
        <DropdownMenuLabel>Date Range</DropdownMenuLabel>
        <DropdownMenuSeparator />

        {/* Preset options */}
        {PRESETS.map((preset) => (
          <DropdownMenuItem
            key={preset.value}
            className="flex items-center justify-between cursor-pointer"
            onClick={() => handlePresetSelect(preset.value)}
          >
            <div className="flex items-center gap-2">
              <PresetIcon type={preset.icon} />
              <span>{preset.label}</span>
            </div>
            {value.preset === preset.value && (
              <Check className="h-4 w-4 text-primary" />
            )}
          </DropdownMenuItem>
        ))}

        {/* Custom date section */}
        {allowCustom && (
          <>
            <DropdownMenuSeparator />
            <div className="p-2">
              <button
                type="button"
                className={cn(
                  'flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-sm transition-colors',
                  showCustom
                    ? 'bg-accent text-accent-foreground'
                    : 'hover:bg-accent hover:text-accent-foreground'
                )}
                onClick={() => setShowCustom(!showCustom)}
              >
                <CalendarRange className="h-4 w-4" />
                <span>Custom Range</span>
                {value.preset === 'custom' && <Check className="ml-auto h-4 w-4 text-primary" />}
              </button>

              <AnimatePresence>
                {showCustom && (
                  <motion.div
                    initial={{ height: 0, opacity: 0 }}
                    animate={{ height: 'auto', opacity: 1 }}
                    exit={{ height: 0, opacity: 0 }}
                    transition={{ duration: 0.2 }}
                    className="overflow-hidden"
                  >
                    <div className="mt-3 space-y-3">
                      <div className="space-y-1.5">
                        <label className="text-xs text-muted-foreground">Start Date</label>
                        <Input
                          type="date"
                          value={customStart}
                          onChange={(e) => setCustomStart(e.target.value)}
                          className="h-8 text-sm"
                        />
                      </div>
                      <div className="space-y-1.5">
                        <label className="text-xs text-muted-foreground">End Date</label>
                        <Input
                          type="date"
                          value={customEnd}
                          onChange={(e) => setCustomEnd(e.target.value)}
                          className="h-8 text-sm"
                          max={new Date().toISOString().split('T')[0]}
                        />
                      </div>
                      <Button
                        size="sm"
                        className="w-full"
                        disabled={!isCustomValid}
                        onClick={handleApplyCustom}
                      >
                        Apply Range
                      </Button>
                      {!isCustomValid && customStart && customEnd && (
                        <p className="text-xs text-rose-500">
                          End date must be after start date
                        </p>
                      )}
                    </div>
                  </motion.div>
                )}
              </AnimatePresence>
            </div>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

// Comparison range picker for analytics
interface ComparisonDateRangeProps {
  primaryRange: AnalyticsDateRange
  comparisonRange?: AnalyticsDateRange
  onComparisonChange: (range: AnalyticsDateRange | undefined) => void
  className?: string
}

export function ComparisonDateRange({
  primaryRange,
  comparisonRange,
  onComparisonChange,
  className,
}: ComparisonDateRangeProps) {
  const [enabled, setEnabled] = useState(!!comparisonRange)

  // Calculate previous period automatically
  const previousPeriod = useMemo(() => {
    const start = new Date(primaryRange.startDate)
    const end = new Date(primaryRange.endDate)
    const duration = end.getTime() - start.getTime()

    const prevEnd = new Date(start.getTime() - 1) // Day before primary start
    const prevStart = new Date(prevEnd.getTime() - duration)

    return {
      startDate: prevStart.toISOString().split('T')[0],
      endDate: prevEnd.toISOString().split('T')[0],
      preset: 'custom' as const,
      label: 'Previous Period',
    }
  }, [primaryRange])

  const handleToggle = useCallback(() => {
    if (enabled) {
      onComparisonChange(undefined)
      setEnabled(false)
    } else {
      onComparisonChange(previousPeriod)
      setEnabled(true)
    }
  }, [enabled, previousPeriod, onComparisonChange])

  return (
    <motion.div
      variants={fadeIn}
      initial="hidden"
      animate="visible"
      className={cn('flex items-center gap-2', className)}
    >
      <Button
        variant={enabled ? 'secondary' : 'outline'}
        size="sm"
        onClick={handleToggle}
        className="gap-2"
      >
        <CalendarRange className="h-4 w-4" />
        {enabled ? 'Comparing' : 'Compare'}
      </Button>
      {enabled && comparisonRange && (
        <Badge variant="outline" className="font-normal">
          vs {formatDisplayDate(comparisonRange.startDate)} -{' '}
          {formatDisplayDate(comparisonRange.endDate)}
        </Badge>
      )}
    </motion.div>
  )
}

// Quick preset buttons for inline use
interface QuickPresetsProps {
  value: AnalyticsDateRange
  onChange: (range: AnalyticsDateRange) => void
  presets?: DateRangePreset[]
  className?: string
}

export function QuickPresets({
  value,
  onChange,
  presets = ['last7days', 'last30days', 'thisMonth', 'last90days'],
  className,
}: QuickPresetsProps) {
  return (
    <div className={cn('flex flex-wrap gap-1', className)}>
      {presets.map((presetValue) => {
        const preset = PRESETS.find((p) => p.value === presetValue)
        if (!preset) return null

        return (
          <Button
            key={preset.value}
            variant={value.preset === preset.value ? 'secondary' : 'ghost'}
            size="sm"
            onClick={() => onChange(getDateRangeFromPreset(preset.value))}
            className="h-7 text-xs"
          >
            {preset.label}
          </Button>
        )
      })}
    </div>
  )
}

export default DateRangePicker
