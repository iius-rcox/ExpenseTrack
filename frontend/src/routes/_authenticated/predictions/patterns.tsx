'use client'

/**
 * Expense Pattern Dashboard (T075, T077, T078)
 *
 * Displays learned expense patterns with management capabilities.
 * Users can view pattern statistics and toggle suppression status.
 *
 * Features:
 * - Paginated pattern list with search/filter
 * - Pattern statistics (frequency, average amount, accuracy)
 * - Suppress/unsuppress toggle for each pattern
 * - Delete pattern with confirmation
 * - Link to rebuild patterns from historical data
 *
 * @see specs/023-expense-prediction/spec.md Section 5.3 for requirements
 */

import { useState } from 'react'

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
import { createFileRoute, Link } from '@tanstack/react-router'
import { motion } from 'framer-motion'
import {
  usePatterns,
  useUpdatePatternSuppression,
  useDeletePattern,
  useRebuildPatterns,
  usePredictionStats,
} from '@/hooks/queries/use-predictions'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Switch } from '@/components/ui/switch'
import { Skeleton } from '@/components/ui/skeleton'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { formatCurrency, cn } from '@/lib/utils'
import { staggerContainer, staggerChild } from '@/lib/animations'
import { toast } from 'sonner'
import {
  ArrowLeft,
  Sparkles,
  Search,
  RefreshCcw,
  Trash2,
  Eye,
  EyeOff,
  Loader2,
  AlertCircle,
  CheckCircle,
  Store,
  Percent,
} from 'lucide-react'
import type { PatternSummary } from '@/types/prediction'

export const Route = createFileRoute('/_authenticated/predictions/patterns')({
  component: PatternDashboard,
})

function PatternDashboard() {
  const [page, setPage] = useState(1)
  const [includeSuppressed, setIncludeSuppressed] = useState(false)
  const [searchQuery, setSearchQuery] = useState('')

  const { data: patternsData, isLoading, error, refetch } = usePatterns({
    page,
    pageSize: 20,
    includeSuppressed,
  })

  const { data: stats } = usePredictionStats()
  const { mutate: updateSuppression, isPending: isUpdatingSuppression } = useUpdatePatternSuppression()
  const { mutate: deletePattern, isPending: isDeleting } = useDeletePattern()
  const { mutate: rebuildPatterns, isPending: isRebuilding } = useRebuildPatterns()

  // Filter patterns by search query
  const filteredPatterns = patternsData?.patterns.filter((pattern) =>
    pattern.displayName.toLowerCase().includes(searchQuery.toLowerCase()) ||
    (pattern.category?.toLowerCase().includes(searchQuery.toLowerCase()) ?? false)
  ) ?? []

  const handleToggleSuppression = (pattern: PatternSummary) => {
    updateSuppression(
      { id: pattern.id, isSuppressed: !pattern.isSuppressed },
      {
        onSuccess: () => {
          toast.success(
            pattern.isSuppressed
              ? `${pattern.displayName} pattern re-enabled`
              : `${pattern.displayName} pattern suppressed`
          )
        },
        onError: () => {
          toast.error('Failed to update pattern')
        },
      }
    )
  }

  const handleDeletePattern = (pattern: PatternSummary) => {
    deletePattern(pattern.id, {
      onSuccess: () => {
        toast.success(`${pattern.displayName} pattern deleted`)
      },
      onError: () => {
        toast.error('Failed to delete pattern')
      },
    })
  }

  const handleRebuildPatterns = () => {
    rebuildPatterns(undefined, {
      onSuccess: (count) => {
        toast.success(`Rebuilt ${count} patterns from historical reports`)
        refetch()
      },
      onError: () => {
        toast.error('Failed to rebuild patterns')
      },
    })
  }

  if (error) {
    return (
      <div className="space-y-6">
        <Header />
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Error loading patterns</AlertTitle>
          <AlertDescription>
            {error.message || 'Failed to load expense patterns. Please try again.'}
          </AlertDescription>
        </Alert>
      </div>
    )
  }

  return (
    <motion.div
      className="space-y-6"
      variants={staggerContainer}
      initial="hidden"
      animate="visible"
    >
      {/* Header */}
      <motion.div variants={staggerChild}>
        <Header />
      </motion.div>

      {/* Stats Cards */}
      <motion.div variants={staggerChild}>
        <StatsCards
          totalPatterns={patternsData?.totalCount ?? 0}
          activePatterns={patternsData?.activeCount ?? 0}
          suppressedPatterns={patternsData?.suppressedCount ?? 0}
          accuracyRate={stats?.accuracyRate ?? 0}
          isLoading={isLoading}
        />
      </motion.div>

      {/* Controls */}
      <motion.div variants={staggerChild}>
        <Card>
          <CardContent className="pt-6">
            <div className="flex flex-col sm:flex-row gap-4 items-start sm:items-center justify-between">
              <div className="flex-1 w-full sm:max-w-sm">
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input
                    placeholder="Search vendors or categories..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    className="pl-9"
                  />
                </div>
              </div>
              <div className="flex items-center gap-4">
                <div className="flex items-center gap-2">
                  <Switch
                    id="show-suppressed"
                    checked={includeSuppressed}
                    onCheckedChange={setIncludeSuppressed}
                  />
                  <Label htmlFor="show-suppressed" className="text-sm">
                    Show suppressed
                  </Label>
                </div>
                <TooltipProvider>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={handleRebuildPatterns}
                        disabled={isRebuilding}
                      >
                        {isRebuilding ? (
                          <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        ) : (
                          <RefreshCcw className="mr-2 h-4 w-4" />
                        )}
                        Rebuild
                      </Button>
                    </TooltipTrigger>
                    <TooltipContent>
                      Rebuild all patterns from historical approved reports
                    </TooltipContent>
                  </Tooltip>
                </TooltipProvider>
              </div>
            </div>
          </CardContent>
        </Card>
      </motion.div>

      {/* Pattern List */}
      <motion.div variants={staggerChild}>
        <Card>
          <CardHeader>
            <CardTitle>Learned Patterns</CardTitle>
            <CardDescription>
              {patternsData?.totalCount ?? 0} patterns learned from your expense reports
            </CardDescription>
          </CardHeader>
          <CardContent className="p-0">
            {isLoading ? (
              <PatternTableSkeleton />
            ) : filteredPatterns.length === 0 ? (
              <EmptyState searchQuery={searchQuery} />
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-[200px]">Vendor</TableHead>
                    <TableHead>Category</TableHead>
                    <TableHead className="text-right">Avg. Amount</TableHead>
                    <TableHead className="text-center">Frequency</TableHead>
                    <TableHead className="text-center">Accuracy</TableHead>
                    <TableHead className="text-center">Status</TableHead>
                    <TableHead className="w-[100px] text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filteredPatterns.map((pattern) => (
                    <PatternRow
                      key={pattern.id}
                      pattern={pattern}
                      onToggleSuppression={handleToggleSuppression}
                      onDelete={handleDeletePattern}
                      isUpdating={isUpdatingSuppression}
                      isDeleting={isDeleting}
                    />
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      </motion.div>

      {/* Pagination */}
      {patternsData && patternsData.totalCount > patternsData.pageSize && (
        <motion.div variants={staggerChild}>
          <Pagination
            page={page}
            pageSize={patternsData.pageSize}
            totalCount={patternsData.totalCount}
            onPageChange={setPage}
          />
        </motion.div>
      )}
    </motion.div>
  )
}

function Header() {
  return (
    <div className="flex items-center gap-4">
      <Button variant="ghost" size="icon" asChild>
        <Link to="/analytics">
          <ArrowLeft className="h-4 w-4" />
        </Link>
      </Button>
      <div>
        <div className="flex items-center gap-3">
          <h1 className="text-2xl font-bold">Expense Patterns</h1>
          <Badge variant="secondary" className="gap-1">
            <Sparkles className="h-3 w-3" />
            AI-Learned
          </Badge>
        </div>
        <p className="text-sm text-muted-foreground">
          Manage your learned expense patterns and prediction accuracy
        </p>
      </div>
    </div>
  )
}

interface StatsCardsProps {
  totalPatterns: number
  activePatterns: number
  suppressedPatterns: number
  accuracyRate: number
  isLoading: boolean
}

function StatsCards({
  totalPatterns,
  activePatterns,
  suppressedPatterns,
  accuracyRate,
  isLoading,
}: StatsCardsProps) {
  const stats = [
    {
      label: 'Total Patterns',
      value: totalPatterns,
      icon: Store,
      color: 'text-blue-500',
    },
    {
      label: 'Active',
      value: activePatterns,
      icon: CheckCircle,
      color: 'text-green-500',
    },
    {
      label: 'Suppressed',
      value: suppressedPatterns,
      icon: EyeOff,
      color: 'text-amber-500',
    },
    {
      label: 'Accuracy Rate',
      value: `${Math.round(accuracyRate * 100)}%`,
      icon: Percent,
      color: accuracyRate >= 0.8 ? 'text-green-500' : accuracyRate >= 0.6 ? 'text-amber-500' : 'text-red-500',
    },
  ]

  return (
    <div className="grid gap-4 md:grid-cols-4">
      {stats.map((stat) => (
        <Card key={stat.label}>
          <CardContent className="pt-6">
            <div className="flex items-center gap-2 text-muted-foreground text-sm">
              <stat.icon className={cn('h-4 w-4', stat.color)} />
              {stat.label}
            </div>
            {isLoading ? (
              <Skeleton className="h-8 w-16 mt-2" />
            ) : (
              <p className="text-2xl font-bold mt-2">{stat.value}</p>
            )}
          </CardContent>
        </Card>
      ))}
    </div>
  )
}

interface PatternRowProps {
  pattern: PatternSummary
  onToggleSuppression: (pattern: PatternSummary) => void
  onDelete: (pattern: PatternSummary) => void
  isUpdating: boolean
  isDeleting: boolean
}

function PatternRow({
  pattern,
  onToggleSuppression,
  onDelete,
  isUpdating,
  isDeleting,
}: PatternRowProps) {
  const accuracyColor =
    pattern.accuracyRate >= 0.8
      ? 'text-green-600 dark:text-green-400'
      : pattern.accuracyRate >= 0.6
        ? 'text-amber-600 dark:text-amber-400'
        : 'text-red-600 dark:text-red-400'

  return (
    <TableRow className={cn(pattern.isSuppressed && 'opacity-60')}>
      <TableCell>
        <div className="flex items-center gap-2">
          <Store className="h-4 w-4 text-muted-foreground" />
          <span className="font-medium">{pattern.displayName}</span>
        </div>
      </TableCell>
      <TableCell>
        {safeDisplayString(pattern.category) ? (
          <Badge variant="outline">{safeDisplayString(pattern.category)}</Badge>
        ) : (
          <span className="text-muted-foreground">-</span>
        )}
      </TableCell>
      <TableCell className="text-right font-medium">
        {formatCurrency(pattern.averageAmount)}
      </TableCell>
      <TableCell className="text-center">
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger>
              <Badge variant="secondary">{pattern.occurrenceCount}x</Badge>
            </TooltipTrigger>
            <TooltipContent>
              Seen {pattern.occurrenceCount} times in expense reports
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      </TableCell>
      <TableCell className="text-center">
        <span className={cn('font-medium', accuracyColor)}>
          {Math.round(pattern.accuracyRate * 100)}%
        </span>
      </TableCell>
      <TableCell className="text-center">
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => onToggleSuppression(pattern)}
                disabled={isUpdating}
                className={cn(
                  'h-8 px-2',
                  pattern.isSuppressed
                    ? 'text-amber-600 dark:text-amber-400'
                    : 'text-green-600 dark:text-green-400'
                )}
              >
                {pattern.isSuppressed ? (
                  <>
                    <EyeOff className="h-4 w-4 mr-1" />
                    Suppressed
                  </>
                ) : (
                  <>
                    <Eye className="h-4 w-4 mr-1" />
                    Active
                  </>
                )}
              </Button>
            </TooltipTrigger>
            <TooltipContent>
              {pattern.isSuppressed
                ? 'Click to re-enable predictions for this vendor'
                : 'Click to suppress predictions for this vendor'}
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      </TableCell>
      <TableCell className="text-right">
        <AlertDialog>
          <AlertDialogTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 text-muted-foreground hover:text-destructive"
              disabled={isDeleting}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Delete Pattern?</AlertDialogTitle>
              <AlertDialogDescription>
                This will permanently delete the "{pattern.displayName}" pattern and all associated
                predictions. This action cannot be undone.
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>Cancel</AlertDialogCancel>
              <AlertDialogAction
                onClick={() => onDelete(pattern)}
                className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              >
                Delete Pattern
              </AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </TableCell>
    </TableRow>
  )
}

function PatternTableSkeleton() {
  return (
    <div className="p-4 space-y-4">
      {Array.from({ length: 5 }).map((_, i) => (
        <div key={i} className="flex items-center gap-4">
          <Skeleton className="h-6 w-6" />
          <Skeleton className="h-6 flex-1" />
          <Skeleton className="h-6 w-20" />
          <Skeleton className="h-6 w-16" />
          <Skeleton className="h-6 w-16" />
          <Skeleton className="h-6 w-24" />
          <Skeleton className="h-8 w-8" />
        </div>
      ))}
    </div>
  )
}

function EmptyState({ searchQuery }: { searchQuery: string }) {
  return (
    <div className="flex flex-col items-center justify-center py-12 px-4 text-center">
      <div className="rounded-full bg-muted p-3 mb-4">
        <Sparkles className="h-6 w-6 text-muted-foreground" />
      </div>
      {searchQuery ? (
        <>
          <h3 className="font-medium text-lg">No matching patterns</h3>
          <p className="text-muted-foreground mt-1 max-w-sm">
            No patterns match "{searchQuery}". Try a different search term.
          </p>
        </>
      ) : (
        <>
          <h3 className="font-medium text-lg">No patterns learned yet</h3>
          <p className="text-muted-foreground mt-1 max-w-sm">
            Submit expense reports to help the system learn your expense patterns. Patterns are
            automatically created from approved reports.
          </p>
        </>
      )}
    </div>
  )
}

interface PaginationProps {
  page: number
  pageSize: number
  totalCount: number
  onPageChange: (page: number) => void
}

function Pagination({ page, pageSize, totalCount, onPageChange }: PaginationProps) {
  const totalPages = Math.ceil(totalCount / pageSize)
  const start = (page - 1) * pageSize + 1
  const end = Math.min(page * pageSize, totalCount)

  return (
    <div className="flex items-center justify-between">
      <p className="text-sm text-muted-foreground">
        Showing {start}-{end} of {totalCount} patterns
      </p>
      <div className="flex items-center gap-2">
        <Button
          variant="outline"
          size="sm"
          onClick={() => onPageChange(page - 1)}
          disabled={page <= 1}
        >
          Previous
        </Button>
        <span className="text-sm">
          Page {page} of {totalPages}
        </span>
        <Button
          variant="outline"
          size="sm"
          onClick={() => onPageChange(page + 1)}
          disabled={page >= totalPages}
        >
          Next
        </Button>
      </div>
    </div>
  )
}
