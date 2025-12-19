"use client"

import { createFileRoute } from '@tanstack/react-router'
import { z } from 'zod'
import { useReportList, useExportReport } from '@/hooks/queries/use-reports'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { ReportCard } from '@/components/reports/report-card'
import { GenerateReportDialog } from '@/components/reports/generate-report-dialog'
import { toast } from 'sonner'
import {
  FileText,
  RefreshCcw,
  Clock,
  Send,
  CheckCircle2,
  XCircle,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react'

const reportsSearchSchema = z.object({
  page: z.coerce.number().optional().default(1),
  pageSize: z.coerce.number().optional().default(20),
  status: z.string().optional(),
})

export const Route = createFileRoute('/_authenticated/reports/')({
  validateSearch: reportsSearchSchema,
  component: ReportsPage,
})

function ReportsPage() {
  const search = Route.useSearch()
  const navigate = Route.useNavigate()

  const { data: reports, isLoading, refetch } = useReportList({
    page: search.page,
    pageSize: search.pageSize,
    status: search.status,
  })
  const { mutate: exportReport } = useExportReport()

  const handleStatusFilter = (status: string | undefined) => {
    navigate({
      search: {
        ...search,
        status,
        page: 1,
      },
    })
  }

  const handlePageChange = (newPage: number) => {
    navigate({
      search: {
        ...search,
        page: newPage,
      },
    })
  }

  const handleExport = (reportId: string, format: 'pdf' | 'excel') => {
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

  const handleReportGenerated = (reportId: string) => {
    refetch()
    navigate({
      to: '/reports/$reportId',
      params: { reportId },
    })
  }

  const totalPages = reports
    ? Math.ceil(reports.totalCount / search.pageSize)
    : 0

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Expense Reports</h1>
          <p className="text-muted-foreground">
            Generate and manage monthly expense reports
          </p>
        </div>
        <GenerateReportDialog onSuccess={handleReportGenerated} />
      </div>

      {/* Status Tabs and Refresh */}
      <div className="flex items-center justify-between">
        <Tabs value={search.status || ''} onValueChange={(v) => handleStatusFilter(v || undefined)}>
          <TabsList>
            <TabsTrigger value="">
              <FileText className="mr-2 h-4 w-4" />
              All
            </TabsTrigger>
            <TabsTrigger value="Draft">
              <Clock className="mr-2 h-4 w-4" />
              Draft
            </TabsTrigger>
            <TabsTrigger value="Submitted">
              <Send className="mr-2 h-4 w-4" />
              Submitted
            </TabsTrigger>
            <TabsTrigger value="Approved">
              <CheckCircle2 className="mr-2 h-4 w-4" />
              Approved
            </TabsTrigger>
            <TabsTrigger value="Rejected">
              <XCircle className="mr-2 h-4 w-4" />
              Rejected
            </TabsTrigger>
          </TabsList>
        </Tabs>
        <Button variant="ghost" size="icon" onClick={() => refetch()} disabled={isLoading}>
          <RefreshCcw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
        </Button>
      </div>

      {/* Reports List */}
      {isLoading ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <Card key={i}>
              <CardContent className="p-6">
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <Skeleton className="h-5 w-32" />
                    <Skeleton className="h-5 w-16" />
                  </div>
                  <div className="grid grid-cols-3 gap-4">
                    <Skeleton className="h-12 w-full" />
                    <Skeleton className="h-12 w-full" />
                    <Skeleton className="h-12 w-full" />
                  </div>
                  <div className="flex justify-between">
                    <Skeleton className="h-4 w-24" />
                    <Skeleton className="h-8 w-20" />
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      ) : reports?.items.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <FileText className="h-12 w-12 text-muted-foreground" />
            <h3 className="mt-4 text-lg font-semibold">No reports found</h3>
            <p className="text-sm text-muted-foreground mt-1">
              {search.status
                ? `No ${search.status.toLowerCase()} reports yet`
                : 'Generate your first expense report to get started'}
            </p>
            {!search.status && (
              <GenerateReportDialog onSuccess={handleReportGenerated} />
            )}
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {reports?.items.map((report) => (
            <ReportCard
              key={report.id}
              report={report}
              onExport={(format) => handleExport(report.id, format)}
            />
          ))}
        </div>
      )}

      {/* Pagination */}
      {reports && reports.totalCount > search.pageSize && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            Showing {((search.page - 1) * search.pageSize) + 1} to{' '}
            {Math.min(search.page * search.pageSize, reports.totalCount)} of{' '}
            {reports.totalCount} reports
          </p>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="icon"
              onClick={() => handlePageChange(search.page - 1)}
              disabled={search.page <= 1}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <span className="text-sm">
              Page {search.page} of {totalPages}
            </span>
            <Button
              variant="outline"
              size="icon"
              onClick={() => handlePageChange(search.page + 1)}
              disabled={search.page >= totalPages}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
