"use client"

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
import {
  useReportDetail,
  useSubmitReport,
  useUnlockReport,
  useDeleteReport,
  useExportReport,
} from '@/hooks/queries/use-reports'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
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
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { formatCurrency, formatDate, formatPeriod, getReportStatusVariant } from '@/lib/utils'
import { toast } from 'sonner'
import {
  ArrowLeft,
  FileText,
  AlertCircle,
  Send,
  Download,
  Trash2,
  Calendar,
  DollarSign,
  Receipt,
  Loader2,
  X,
  LockOpen,
  Pencil,
} from 'lucide-react'
import { AutoSuggestedBadge, AutoSuggestedSummary } from '@/components/predictions'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'

export const Route = createFileRoute('/_authenticated/reports/$reportId')({
  component: ReportDetailPage,
  parseParams: (params: Record<string, string>) => ({
    reportId: params.reportId,
  }),
})

function ReportDetailPage() {
  const params = Route.useParams()
  const reportId = params.reportId
  const navigate = Route.useNavigate()

  const { data: report, isLoading, error } = useReportDetail(reportId)
  const { mutate: submitReport, isPending: isSubmitting } = useSubmitReport()
  const { mutate: unlockReport, isPending: isUnlocking } = useUnlockReport()
  const { mutate: deleteReport, isPending: isDeleting } = useDeleteReport()
  const { mutate: exportReport, isPending: isExporting } = useExportReport()

  const handleSubmit = () => {
    submitReport(reportId, {
      onSuccess: () => {
        toast.success('Report submitted successfully')
      },
      onError: (error) => {
        toast.error(`Failed to submit report: ${error.message}`)
      },
    })
  }

  const handleUnlock = () => {
    unlockReport(reportId, {
      onSuccess: () => {
        toast.success('Report unlocked - you can now edit it')
      },
      onError: (error) => {
        toast.error(`Failed to unlock report: ${error.message}`)
      },
    })
  }

  const handleDelete = () => {
    deleteReport(reportId, {
      onSuccess: () => {
        toast.success('Report deleted')
        navigate({ to: '/reports' })
      },
      onError: (error) => {
        toast.error(`Failed to delete report: ${error.message}`)
      },
    })
  }

  const handleExport = (format: 'pdf' | 'excel') => {
    exportReport(
      { reportId, format },
      {
        onSuccess: (result) => {
          toast.success(`Report exported as ${result.filename}`)
        },
        onError: (error) => {
          toast.error(`Export failed: ${error.message}`)
        },
      }
    )
  }

  if (isLoading) {
    return <ReportDetailSkeleton />
  }

  if (error) {
    return (
      <div className="space-y-6">
        <Button variant="ghost" asChild>
          <Link to="/reports">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to Reports
          </Link>
        </Button>
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Error loading report</AlertTitle>
          <AlertDescription>
            {error.message || 'Failed to load report details. Please try again.'}
          </AlertDescription>
        </Alert>
      </div>
    )
  }

  if (!report) {
    return null
  }

  const statusVariant = getReportStatusVariant(report.status)

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" asChild>
            <Link to="/reports">
              <ArrowLeft className="h-4 w-4" />
            </Link>
          </Button>
          <div>
            <div className="flex items-center gap-3">
              <h1 className="text-2xl font-bold">{report.title}</h1>
              <Badge variant={statusVariant}>{report.status}</Badge>
            </div>
            <p className="text-sm text-muted-foreground">
              {formatPeriod(report.period)} • {report.lineCount} expenses
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {report.status === 'Draft' && (
            <>
              <Button variant="outline" asChild>
                <Link to="/reports/editor" search={{ period: report.period }}>
                  <Pencil className="mr-2 h-4 w-4" />
                  Edit Report
                </Link>
              </Button>
              <Button onClick={handleSubmit} disabled={isSubmitting}>
                {isSubmitting ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <Send className="mr-2 h-4 w-4" />
                )}
                Submit Report
              </Button>
              <AlertDialog>
                <AlertDialogTrigger asChild>
                  <Button variant="destructive" disabled={isDeleting}>
                    {isDeleting ? (
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    ) : (
                      <Trash2 className="mr-2 h-4 w-4" />
                    )}
                    Delete
                  </Button>
                </AlertDialogTrigger>
                <AlertDialogContent>
                  <AlertDialogHeader>
                    <AlertDialogTitle>Delete Report?</AlertDialogTitle>
                    <AlertDialogDescription>
                      This will permanently delete this draft report. This action cannot be undone.
                    </AlertDialogDescription>
                  </AlertDialogHeader>
                  <AlertDialogFooter>
                    <AlertDialogCancel>Cancel</AlertDialogCancel>
                    <AlertDialogAction onClick={handleDelete}>Delete Report</AlertDialogAction>
                  </AlertDialogFooter>
                </AlertDialogContent>
              </AlertDialog>
            </>
          )}
          {report.status !== 'Draft' && (
            <>
              {/* Unlock button - only for Submitted reports */}
              {report.status === 'Submitted' && (
                <AlertDialog>
                  <AlertDialogTrigger asChild>
                    <Button variant="outline" disabled={isUnlocking}>
                      {isUnlocking ? (
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      ) : (
                        <LockOpen className="mr-2 h-4 w-4" />
                      )}
                      Unlock
                    </Button>
                  </AlertDialogTrigger>
                  <AlertDialogContent>
                    <AlertDialogHeader>
                      <AlertDialogTitle>Unlock Report?</AlertDialogTitle>
                      <AlertDialogDescription>
                        This will return the report to Draft status, allowing you to make edits.
                        You will need to submit the report again when finished.
                      </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                      <AlertDialogCancel>Cancel</AlertDialogCancel>
                      <AlertDialogAction onClick={handleUnlock}>Unlock Report</AlertDialogAction>
                    </AlertDialogFooter>
                  </AlertDialogContent>
                </AlertDialog>
              )}
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" disabled={isExporting}>
                    {isExporting ? (
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    ) : (
                      <Download className="mr-2 h-4 w-4" />
                    )}
                    Export
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent>
                  <DropdownMenuItem onClick={() => handleExport('pdf')}>
                    Export as PDF
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleExport('excel')}>
                    Export as Excel
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </>
          )}
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-2 text-muted-foreground text-sm">
              <Calendar className="h-4 w-4" />
              Period
            </div>
            <p className="text-2xl font-bold mt-2">{formatPeriod(report.period)}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-2 text-muted-foreground text-sm">
              <FileText className="h-4 w-4" />
              Expenses
            </div>
            <p className="text-2xl font-bold mt-2">{report.lineCount}</p>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-2 text-muted-foreground text-sm">
              <DollarSign className="h-4 w-4" />
              Total Amount
            </div>
            <p className="text-2xl font-bold mt-2">{formatCurrency(report.totalAmount)}</p>
          </CardContent>
        </Card>
      </div>

      {/* Feature 023: Auto-Suggested Summary */}
      {report.lines.some((line) => line.isAutoSuggested) && (
        <AutoSuggestedSummary
          autoSuggestedCount={report.lines.filter((line) => line.isAutoSuggested).length}
          totalCount={report.lineCount}
        />
      )}

      {/* Expense Lines Table */}
      <Card>
        <CardHeader>
          <CardTitle>Expense Lines</CardTitle>
          <CardDescription>All expenses included in this report</CardDescription>
        </CardHeader>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-[100px]">Date</TableHead>
                <TableHead>Description</TableHead>
                <TableHead>Vendor</TableHead>
                <TableHead>Category</TableHead>
                <TableHead className="text-right">Amount</TableHead>
                <TableHead className="w-[80px] text-center">Receipt</TableHead>
                <TableHead className="w-[120px] text-center">Source</TableHead>
                {report.status === 'Draft' && (
                  <TableHead className="w-[60px]"></TableHead>
                )}
              </TableRow>
            </TableHeader>
            <TableBody>
              {report.lines.map((line) => (
                <TableRow key={line.id}>
                  <TableCell className="font-medium">
                    {formatDate(line.transactionDate)}
                  </TableCell>
                  <TableCell>
                    <div>
                      <p className="truncate max-w-[300px]">{line.description}</p>
                      {line.normalizedDescription &&
                        line.normalizedDescription !== line.description && (
                          <p className="text-xs text-muted-foreground truncate max-w-[300px]">
                            {line.normalizedDescription}
                          </p>
                        )}
                    </div>
                  </TableCell>
                  <TableCell>{line.vendor || '-'}</TableCell>
                  <TableCell>
                    {safeDisplayString(line.category) ? (
                      <Badge variant="outline">{safeDisplayString(line.category)}</Badge>
                    ) : (
                      '-'
                    )}
                  </TableCell>
                  <TableCell className="text-right font-medium">
                    {formatCurrency(line.amount)}
                  </TableCell>
                  <TableCell className="text-center">
                    {line.hasReceipt ? (
                      <Receipt className="h-4 w-4 text-green-500 mx-auto" />
                    ) : (
                      <span className="text-xs text-muted-foreground">-</span>
                    )}
                  </TableCell>
                  <TableCell className="text-center">
                    {line.isAutoSuggested ? (
                      <AutoSuggestedBadge compact />
                    ) : (
                      <span className="text-xs text-muted-foreground">Manual</span>
                    )}
                  </TableCell>
                  {report.status === 'Draft' && (
                    <TableCell className="text-center">
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 text-muted-foreground hover:text-destructive"
                        title="Remove from report"
                      >
                        <X className="h-4 w-4" />
                      </Button>
                    </TableCell>
                  )}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Metadata */}
      <Card>
        <CardHeader>
          <CardTitle>Report Information</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3 text-sm">
          <div className="flex justify-between">
            <span className="text-muted-foreground">Report ID</span>
            <span className="font-mono">{report.id}</span>
          </div>
          <Separator />
          <div className="flex justify-between">
            <span className="text-muted-foreground">Created</span>
            <span>{formatDate(report.createdAt)}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Last Updated</span>
            <span>{formatDate(report.updatedAt)}</span>
          </div>
          {report.submittedAt && (
            <div className="flex justify-between">
              <span className="text-muted-foreground">Submitted</span>
              <span>{formatDate(report.submittedAt)}</span>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

function ReportDetailSkeleton() {
  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Skeleton className="h-10 w-10" />
        <div className="space-y-2">
          <Skeleton className="h-8 w-64" />
          <Skeleton className="h-4 w-32" />
        </div>
      </div>
      <div className="grid gap-4 md:grid-cols-3">
        {Array.from({ length: 3 }).map((_, i) => (
          <Card key={i}>
            <CardContent className="pt-6">
              <Skeleton className="h-4 w-16" />
              <Skeleton className="h-8 w-24 mt-2" />
            </CardContent>
          </Card>
        ))}
      </div>
      <Card>
        <CardHeader>
          <Skeleton className="h-6 w-32" />
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} className="h-12 w-full" />
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
