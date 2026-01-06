"use client"

import { useEffect } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Progress } from '@/components/ui/progress'
import { Badge } from '@/components/ui/badge'
import {
  useReportJob,
  useCancelReportJob,
  isTerminalStatus,
} from '@/hooks/queries/use-report-jobs'
import type { ReportJob, ReportJobStatus } from '@/types/api'
import {
  Loader2,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Clock,
  FileText,
  Ban,
} from 'lucide-react'
import { formatDistanceToNow } from 'date-fns'

interface ReportGenerationProgressProps {
  jobId: string
  /** Called when job completes successfully with the generated report ID */
  onComplete?: (reportId: string) => void
  /** Called when job fails with error message */
  onError?: (error: string) => void
  /** Called when user cancels the job */
  onCancel?: () => void
}

/**
 * Displays real-time progress for an async report generation job.
 * Automatically polls for updates and provides cancellation support.
 */
export function ReportGenerationProgress({
  jobId,
  onComplete,
  onError,
  onCancel,
}: ReportGenerationProgressProps) {
  const { data: job, isLoading, error: fetchError } = useReportJob(jobId)
  const { mutate: cancelJob, isPending: isCancelling } = useCancelReportJob()

  // Trigger callbacks on terminal states
  useEffect(() => {
    if (!job) return

    if (job.status === 'Completed' && job.generatedReportId) {
      onComplete?.(job.generatedReportId)
    } else if (job.status === 'Failed' && job.errorMessage) {
      onError?.(job.errorMessage)
    } else if (job.status === 'Cancelled') {
      onCancel?.()
    }
  }, [job?.status, job?.generatedReportId, job?.errorMessage, onComplete, onError, onCancel])

  const handleCancel = () => {
    cancelJob(jobId)
  }

  if (isLoading && !job) {
    return (
      <Card>
        <CardContent className="flex items-center justify-center py-8">
          <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
        </CardContent>
      </Card>
    )
  }

  if (fetchError) {
    return (
      <Card className="border-destructive">
        <CardContent className="flex items-center gap-3 py-6">
          <XCircle className="h-5 w-5 text-destructive" />
          <p className="text-sm text-destructive">Failed to load job status</p>
        </CardContent>
      </Card>
    )
  }

  if (!job) {
    return null
  }

  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <FileText className="h-5 w-5 text-muted-foreground" />
            <CardTitle className="text-lg">Generating Report</CardTitle>
          </div>
          <StatusBadge status={job.status} />
        </div>
        <CardDescription>
          {job.period} - {getStatusDescription(job)}
        </CardDescription>
      </CardHeader>

      <CardContent className="space-y-4">
        {/* Progress bar */}
        <div className="space-y-2">
          <div className="flex items-center justify-between text-sm">
            <span className="text-muted-foreground">
              {job.processedLines} of {job.totalLines} lines processed
            </span>
            <span className="font-medium">{job.progressPercent}%</span>
          </div>
          <Progress value={job.progressPercent} className="h-2" />
        </div>

        {/* Stats row */}
        <div className="flex items-center gap-6 text-sm">
          {job.failedLines > 0 && (
            <div className="flex items-center gap-1.5 text-amber-600 dark:text-amber-500">
              <AlertTriangle className="h-4 w-4" />
              <span>{job.failedLines} failed</span>
            </div>
          )}

          {job.estimatedCompletionAt && !isTerminalStatus(job.status) && (
            <div className="flex items-center gap-1.5 text-muted-foreground">
              <Clock className="h-4 w-4" />
              <span>
                Est. {formatDistanceToNow(new Date(job.estimatedCompletionAt), { addSuffix: true })}
              </span>
            </div>
          )}

          {job.startedAt && (
            <div className="flex items-center gap-1.5 text-muted-foreground">
              <Clock className="h-4 w-4" />
              <span>
                Started {formatDistanceToNow(new Date(job.startedAt), { addSuffix: true })}
              </span>
            </div>
          )}
        </div>

        {/* Error message */}
        {job.errorMessage && job.status === 'Failed' && (
          <div className="flex items-start gap-2 rounded-md bg-destructive/10 p-3">
            <XCircle className="h-4 w-4 mt-0.5 text-destructive shrink-0" />
            <p className="text-sm text-destructive">{job.errorMessage}</p>
          </div>
        )}

        {/* Cancellation message */}
        {job.status === 'Cancelled' && (
          <div className="flex items-start gap-2 rounded-md bg-muted p-3">
            <Ban className="h-4 w-4 mt-0.5 text-muted-foreground shrink-0" />
            <p className="text-sm text-muted-foreground">Report generation was cancelled</p>
          </div>
        )}

        {/* Action buttons */}
        {!isTerminalStatus(job.status) && (
          <div className="flex justify-end pt-2">
            <Button
              variant="outline"
              size="sm"
              onClick={handleCancel}
              disabled={isCancelling || job.status === 'CancellationRequested'}
            >
              {isCancelling || job.status === 'CancellationRequested' ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Cancelling...
                </>
              ) : (
                <>
                  <XCircle className="mr-2 h-4 w-4" />
                  Cancel
                </>
              )}
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  )
}

function StatusBadge({ status }: { status: ReportJobStatus }) {
  switch (status) {
    case 'Pending':
      return (
        <Badge variant="secondary">
          <Clock className="mr-1 h-3 w-3" />
          Pending
        </Badge>
      )
    case 'Processing':
      return (
        <Badge variant="default">
          <Loader2 className="mr-1 h-3 w-3 animate-spin" />
          Processing
        </Badge>
      )
    case 'Completed':
      return (
        <Badge variant="default" className="bg-green-600">
          <CheckCircle2 className="mr-1 h-3 w-3" />
          Completed
        </Badge>
      )
    case 'Failed':
      return (
        <Badge variant="destructive">
          <XCircle className="mr-1 h-3 w-3" />
          Failed
        </Badge>
      )
    case 'Cancelled':
    case 'CancellationRequested':
      return (
        <Badge variant="secondary">
          <Ban className="mr-1 h-3 w-3" />
          Cancelled
        </Badge>
      )
    default:
      return null
  }
}

function getStatusDescription(job: ReportJob): string {
  switch (job.status) {
    case 'Pending':
      return 'Waiting in queue...'
    case 'Processing':
      return 'Categorizing expenses...'
    case 'Completed':
      return 'Report ready!'
    case 'Failed':
      return 'Generation failed'
    case 'CancellationRequested':
      return 'Cancelling...'
    case 'Cancelled':
      return 'Cancelled by user'
    default:
      return ''
  }
}
