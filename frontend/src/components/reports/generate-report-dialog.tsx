"use client"

import { useState } from 'react'
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
import { useGenerateReport, useReportPreview } from '@/hooks/queries/use-reports'
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
} from 'lucide-react'

interface GenerateReportDialogProps {
  onSuccess?: (reportId: string) => void
}

export function GenerateReportDialog({ onSuccess }: GenerateReportDialogProps) {
  const [open, setOpen] = useState(false)
  const [selectedPeriod, setSelectedPeriod] = useState(getCurrentPeriod())

  const { data: preview, isLoading: loadingPreview } = useReportPreview(selectedPeriod)
  const { mutate: generateReport, isPending: isGenerating } = useGenerateReport()

  const handlePreviousPeriod = () => {
    setSelectedPeriod(getPreviousPeriod(selectedPeriod))
  }

  const handleNextPeriod = () => {
    const [year, month] = selectedPeriod.split('-').map(Number)
    const date = new Date(year, month, 1) // next month
    setSelectedPeriod(`${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`)
  }

  const handleGenerate = () => {
    generateReport(
      { period: selectedPeriod },
      {
        onSuccess: (report) => {
          toast.success(`Report for ${formatPeriod(selectedPeriod)} generated successfully`)
          setOpen(false)
          onSuccess?.(report.id)
        },
        onError: (error) => {
          toast.error(`Failed to generate report: ${error.message}`)
        },
      }
    )
  }

  const totalAmount = preview?.reduce((sum, line) => sum + line.amount, 0) ?? 0
  const lineCount = preview?.length ?? 0

  return (
    <Dialog open={open} onOpenChange={setOpen}>
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
            Create a new expense report for a specific month
          </DialogDescription>
        </DialogHeader>

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
              disabled={isGenerating || loadingPreview || lineCount === 0}
            >
              {isGenerating ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Generating...
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
      </DialogContent>
    </Dialog>
  )
}
