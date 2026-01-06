"use client"

import { useState, useCallback } from 'react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Card, CardContent } from '@/components/ui/card'
import { useReportPreview } from '@/hooks/queries/use-reports'
import {
  useCreateReportJob,
  useActiveReportJob,
} from '@/hooks/queries/use-report-jobs'
import { ReportGenerationProgress } from './report-generation-progress'
import { formatCurrency, formatPeriod, getCurrentPeriod, getPreviousPeriod } from '@/lib/utils'
import { toast } from 'sonner'
import {
  Plus,
  FileText,
  Calendar,
  DollarSign,
  Loader2,
  ChevronLeft,
  ChevronRight,
  AlertTriangle,
} from 'lucide-react'

interface GenerateReportDialogProps {
  onSuccess?: (reportId: string) => void
}

type DialogState = 'select' | 'generating'

export function GenerateReportDialog({ onSuccess }: GenerateReportDialogProps) {
  const [open, setOpen] = useState(false)
  const [selectedPeriod, setSelectedPeriod] = useState(getCurrentPeriod())
  const [dialogState, setDialogState] = useState<DialogState>('select')
  const [currentJobId, setCurrentJobId] = useState<string | null>(null)

  const { data: preview, isLoading: loadingPreview } = useReportPreview(selectedPeriod)
  const { data: activeJob } = useActiveReportJob(selectedPeriod, { enabled: open })
  const { mutate: createJob, isPending: isCreating } = useCreateReportJob()

  const handlePreviousPeriod = () => {
    setSelectedPeriod(getPreviousPeriod(selectedPeriod))
    setCurrentJobId(null) // Reset job when period changes
  }

  const handleNextPeriod = () => {
    const [year, month] = selectedPeriod.split('-').map(Number)
    const date = new Date(year, month, 1) // next month
    setSelectedPeriod(`${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`)
    setCurrentJobId(null) // Reset job when period changes
  }

  const handleGenerate = () => {
    createJob(
      { period: selectedPeriod },
      {
        onSuccess: (job) => {
          setCurrentJobId(job.id)
          setDialogState('generating')
        },
        onError: (error) => {
          toast.error(`Failed to start report generation: ${error.message}`)
        },
      }
    )
  }

  const handleResumeExistingJob = () => {
    if (activeJob?.job) {
      setCurrentJobId(activeJob.job.id)
      setDialogState('generating')
    }
  }

  const handleComplete = useCallback((reportId: string) => {
    toast.success(`Report for ${formatPeriod(selectedPeriod)} generated successfully`)
    setOpen(false)
    setDialogState('select')
    setCurrentJobId(null)
    onSuccess?.(reportId)
  }, [selectedPeriod, onSuccess])

  const handleError = useCallback((error: string) => {
    toast.error(`Report generation failed: ${error}`)
    // Keep dialog open to show error state
  }, [])

  const handleCancel = useCallback(() => {
    toast.info('Report generation cancelled')
    setDialogState('select')
    setCurrentJobId(null)
  }, [])

  const handleDialogChange = (isOpen: boolean) => {
    setOpen(isOpen)
    if (!isOpen) {
      // Reset state when dialog closes
      setDialogState('select')
      setCurrentJobId(null)
    }
  }

  const totalAmount = preview?.reduce((sum, line) => sum + line.amount, 0) ?? 0
  const lineCount = preview?.length ?? 0

  return (
    <Dialog open={open} onOpenChange={handleDialogChange}>
      <DialogTrigger asChild>
        <Button>
          <Plus className="mr-2 h-4 w-4" />
          Generate Report
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Generate Expense Report</DialogTitle>
          <DialogDescription>
            {dialogState === 'select'
              ? 'Create a new expense report for a specific month'
              : 'Report generation in progress'}
          </DialogDescription>
        </DialogHeader>

        {dialogState === 'select' ? (
          <div className="space-y-6">
            {/* Period Selector */}
            <div className="space-y-2">
              <Label>Select Period</Label>
              <div className="flex items-center gap-2">
                <Button variant="outline" size="icon" onClick={handlePreviousPeriod}>
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <div className="flex-1 text-center">
                  <p className="font-medium text-lg">{formatPeriod(selectedPeriod)}</p>
                </div>
                <Button
                  variant="outline"
                  size="icon"
                  onClick={handleNextPeriod}
                  disabled={selectedPeriod >= getCurrentPeriod()}
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>

            {/* Active Job Warning */}
            {activeJob?.hasActiveJob && activeJob.job && (
              <Card className="border-amber-500/50 bg-amber-50 dark:bg-amber-950/20">
                <CardContent className="pt-4 pb-4">
                  <div className="flex items-start gap-3">
                    <AlertTriangle className="h-5 w-5 text-amber-600 dark:text-amber-500 shrink-0 mt-0.5" />
                    <div className="flex-1">
                      <p className="text-sm font-medium text-amber-800 dark:text-amber-200">
                        Report generation already in progress
                      </p>
                      <p className="text-xs text-amber-600 dark:text-amber-400 mt-1">
                        {activeJob.job.progressPercent}% complete ({activeJob.job.processedLines}/{activeJob.job.totalLines} lines)
                      </p>
                      <Button
                        variant="outline"
                        size="sm"
                        className="mt-2"
                        onClick={handleResumeExistingJob}
                      >
                        View Progress
                      </Button>
                    </div>
                  </div>
                </CardContent>
              </Card>
            )}

            {/* Preview Summary */}
            <Card>
              <CardContent className="pt-6">
                <div className="flex items-center gap-4 mb-4">
                  <FileText className="h-8 w-8 text-muted-foreground" />
                  <div>
                    <p className="font-medium">Report Preview</p>
                    <p className="text-sm text-muted-foreground">
                      Summary for {formatPeriod(selectedPeriod)}
                    </p>
                  </div>
                </div>
                {loadingPreview ? (
                  <div className="flex items-center justify-center py-4">
                    <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
                  </div>
                ) : (
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-1">
                      <p className="text-xs text-muted-foreground flex items-center gap-1">
                        <Calendar className="h-3 w-3" /> Expenses
                      </p>
                      <p className="text-xl font-bold">{lineCount}</p>
                    </div>
                    <div className="space-y-1">
                      <p className="text-xs text-muted-foreground flex items-center gap-1">
                        <DollarSign className="h-3 w-3" /> Total
                      </p>
                      <p className="text-xl font-bold">{formatCurrency(totalAmount)}</p>
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>

            {lineCount === 0 && !loadingPreview && (
              <p className="text-sm text-muted-foreground text-center">
                No matched expenses found for this period.
              </p>
            )}

            <div className="flex justify-end gap-2">
              <Button variant="outline" onClick={() => setOpen(false)}>
                Cancel
              </Button>
              <Button
                onClick={handleGenerate}
                disabled={isCreating || loadingPreview || lineCount === 0 || activeJob?.hasActiveJob}
              >
                {isCreating ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    Starting...
                  </>
                ) : (
                  <>
                    <FileText className="mr-2 h-4 w-4" />
                    Generate Report
                  </>
                )}
              </Button>
            </div>
          </div>
        ) : (
          <div className="space-y-4">
            {currentJobId && (
              <ReportGenerationProgress
                jobId={currentJobId}
                onComplete={handleComplete}
                onError={handleError}
                onCancel={handleCancel}
              />
            )}

            <div className="flex justify-end">
              <Button variant="outline" onClick={() => setOpen(false)}>
                Close
              </Button>
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
