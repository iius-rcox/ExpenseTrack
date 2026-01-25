'use client'

import { useEffect, useState } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { z } from 'zod'
import { useReportPreview, useCheckDraftExists, useGenerateReport, useReportDetail, useUpdateReportLine } from '@/hooks/queries/use-reports'
import { useReportEditor } from '@/hooks/use-report-editor'
import { useExportPreview } from '@/hooks/queries/use-report-export'
import { useGLAccounts, lookupGLName } from '@/hooks/queries/use-reference-data'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { EditableTextCell } from '@/components/reports/editable-text-cell'
import { EditableDateCell } from '@/components/reports/editable-date-cell'
import { SplitExpansionPanel } from '@/components/reports/split-expansion-panel'
import { SplitIndicatorBadge } from '@/components/reports/split-indicator-badge'
import { DraftStatusBanner } from '@/components/reports/draft-status-banner'
import { toast } from 'sonner'
import { FileSpreadsheet, FileText, ChevronLeft, ChevronRight, CheckCircle2, XCircle, Split, ChevronDown, ChevronRight as ChevronRightIcon } from 'lucide-react'
import { formatCurrency, cn } from '@/lib/utils'
import type { ExportPreviewRequest, ExportLineDto } from '@/types/report-editor'

const editorSearchSchema = z.object({
  period: z.string().optional().default(new Date().toISOString().slice(0, 7)),
})

export const Route = createFileRoute('/_authenticated/reports/editor' as any)({
  validateSearch: editorSearchSchema,
  component: ReportEditorPage,
})

function ReportEditorPage() {
  const search = Route.useSearch() as { period: string }
  const period = search.period
  const navigate = Route.useNavigate()

  // Check if draft exists for this period
  const { data: draftCheck, isLoading: checkingDraft } = useCheckDraftExists(period)
  const hasDraft = draftCheck?.exists && draftCheck?.reportId

  // Load existing draft or preview
  const { data: existingDraft, isLoading: loadingDraft } = useReportDetail(draftCheck?.reportId || '')
  const { data: previewLines, isLoading: loadingPreview } = useReportPreview(period)

  // Draft management
  const [useDraft, setUseDraft] = useState(false)
  const [reportId, setReportId] = useState<string | null>(null)
  const [lastSaved, setLastSaved] = useState<Date | null>(null)
  const { mutate: generateDraft, isPending: generatingDraft } = useGenerateReport()
  const { mutate: updateLine, isPending: savingLine } = useUpdateReportLine()

  // Fetch reference data for GL name lookups
  const { data: glAccounts = [] } = useGLAccounts()

  // Editor state
  const { state, dispatch, metrics } = useReportEditor(period)

  // Export mutation
  const { mutate: exportReport, isPending: isExporting } = useExportPreview()

  // Track if we've loaded data for the current period to prevent re-loading on refetch
  const [loadedPeriod, setLoadedPeriod] = useState<string | null>(null)

  const isLoading = checkingDraft || loadingDraft || loadingPreview || generatingDraft

  // Helper: Strip index suffix from line ID for API calls
  const getDbLineId = (lineId: string): string => {
    // Frontend IDs have "-index" suffix, backend expects just the GUID
    return lineId.split('-').slice(0, 5).join('-') // Take first 5 parts of GUID
  }

  // Helper: Update GL Code and auto-lookup GL Name
  const handleGLCodeChange = (lineId: string, newGLCode: string) => {
    const glName = lookupGLName(newGLCode, glAccounts)

    // Update both GL Code and GL Name in UI
    dispatch({ type: 'UPDATE_LINE', id: lineId, field: 'glCode', value: newGLCode })
    dispatch({ type: 'UPDATE_LINE', id: lineId, field: 'glName', value: glName })

    // Save to database if using draft
    if (useDraft && reportId) {
      const dbLineId = getDbLineId(lineId)
      updateLine(
        { reportId, lineId: dbLineId, data: { glCode: newGLCode } },
        {
          onSuccess: () => {
            setLastSaved(new Date())
          },
          onError: (error) => {
            toast.error(`Failed to save: ${error.message}`)
          },
        }
      )
    }
  }

  // Helper: Update any field with auto-save
  const handleFieldUpdate = (lineId: string, field: string, value: any) => {
    // Update UI immediately (optimistic)
    dispatch({ type: 'UPDATE_LINE', id: lineId, field: field as any, value })

    // Save to database if using draft
    if (useDraft && reportId) {
      const updateData: any = {}
      if (field === 'departmentCode') updateData.departmentCode = value
      if (field === 'description') updateData.description = value
      // GL Code handled by handleGLCodeChange which also updates glName

      if (Object.keys(updateData).length > 0) {
        const dbLineId = getDbLineId(lineId)
        updateLine(
          { reportId, lineId: dbLineId, data: updateData },
          {
            onSuccess: () => {
              setLastSaved(new Date())
            },
            onError: (error) => {
              toast.error(`Failed to save: ${error.message}`)
            },
          }
        )
      }
    }
  }

  // Load data into editor (draft or preview) - ONLY ONCE per period
  // This prevents refetches from overwriting user edits
  useEffect(() => {
    // Skip if we've already loaded this period's data
    if (loadedPeriod === period) {
      return
    }

    // Skip if GL accounts aren't loaded yet
    if (glAccounts.length === 0) {
      return
    }

    if (existingDraft?.lines) {
      // Load from existing draft and populate glName from GL lookup
      const linesWithGLNames = existingDraft.lines.map((line: any) => ({
        ...line,
        glName: line.glName || lookupGLName(line.glCode || '', glAccounts),
      }))
      dispatch({ type: 'LOAD_PREVIEW', lines: linesWithGLNames })
      setUseDraft(true)
      setReportId(existingDraft.id)
      setLastSaved(new Date(existingDraft.updatedAt || existingDraft.createdAt))
      setLoadedPeriod(period)
      toast.info('Resuming your draft...')
    } else if (previewLines && !hasDraft) {
      // Load preview (no draft exists yet) and populate glName
      const linesWithGLNames = previewLines.map((line: any) => ({
        ...line,
        glName: line.glName || lookupGLName(line.glCode || '', glAccounts),
      }))
      dispatch({ type: 'LOAD_PREVIEW', lines: linesWithGLNames })
      setUseDraft(false)
      setReportId(null)
      setLoadedPeriod(period)
    }
  }, [existingDraft, previewLines, hasDraft, glAccounts, dispatch, period, loadedPeriod])

  // Unsaved changes warning
  useEffect(() => {
    if (metrics.dirtyCount > 0) {
      const handler = (e: BeforeUnloadEvent) => {
        e.preventDefault()
        e.returnValue = ''
      }
      window.addEventListener('beforeunload', handler)
      return () => window.removeEventListener('beforeunload', handler)
    }
  }, [metrics.dirtyCount])

  const handleExport = (format: 'excel' | 'pdf') => {
    const request: ExportPreviewRequest = {
      period,
      lines: state.lines.map((line) => {
        const exportLine: ExportLineDto = {
          expenseDate: line.expenseDate,
          vendorName: line.vendor,
          glCode: line.glCode,
          departmentCode: line.departmentCode,
          description: line.description,
          hasReceipt: line.hasReceipt,
          amount: line.originalAmount,
        }

        // If line has split allocations, include as child allocations
        if (line.isSplit && line.appliedAllocations && line.appliedAllocations.length > 0) {
          exportLine.childAllocations = line.appliedAllocations.map((alloc) => ({
            expenseDate: line.expenseDate,
            vendorName: line.vendor,
            glCode: alloc.glCode,
            departmentCode: alloc.departmentCode,
            description: line.description,
            hasReceipt: line.hasReceipt,
            amount: alloc.amount,
          }))
        }

        return exportLine
      }),
    }

    exportReport(
      { request, format },
      {
        onSuccess: ({ filename }) => {
          toast.success(`Downloaded ${filename}`)
        },
        onError: (err) => {
          toast.error(`Export failed: ${err.message}`)
        },
      }
    )
  }

  const handlePeriodChange = (direction: 'prev' | 'next') => {
    const [year, month] = period.split('-').map(Number)
    const date = new Date(year, month - 1, 1)
    date.setMonth(date.getMonth() + (direction === 'next' ? 1 : -1))
    const newPeriod = date.toISOString().slice(0, 7)
    // Reset loaded state so new period data will be loaded
    setLoadedPeriod(null)
    navigate({ search: { period: newPeriod } } as any)
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-96 w-full" />
      </div>
    )
  }

  const handleCreateDraft = () => {
    generateDraft(
      { period },
      {
        onSuccess: (draft) => {
          setReportId(draft.id)
          setUseDraft(true)
          dispatch({ type: 'LOAD_PREVIEW', lines: draft.lines })
          toast.success('Draft created! Your edits will now be saved automatically.')
        },
        onError: (error) => {
          toast.error(`Failed to create draft: ${error.message}`)
        },
      }
    )
  }

  return (
    <div className="space-y-6">
      {/* Draft Status Banner */}
      <DraftStatusBanner
        useDraft={useDraft}
        lastSaved={lastSaved}
        isSaving={savingLine}
        onCreateDraft={handleCreateDraft}
        onDiscardDraft={() => toast.info('Discard draft - to be implemented')}
      />

      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Quick Expense Export</h1>
          <p className="text-muted-foreground mt-1">
            Edit expense details and download as Excel or PDF
          </p>
        </div>

        <div className="flex items-center gap-4">
          {/* Period Navigation */}
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="icon"
              onClick={() => handlePeriodChange('prev')}
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <div className="text-sm font-medium min-w-[100px] text-center">
              {(() => {
                const [year, month] = period.split('-').map(Number)
                const date = new Date(year, month - 1, 1) // month is 0-indexed in Date constructor
                return date.toLocaleDateString('en-US', {
                  year: 'numeric',
                  month: 'long',
                })
              })()}
            </div>
            <Button
              variant="outline"
              size="icon"
              onClick={() => handlePeriodChange('next')}
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>

          {/* Export Buttons */}
          <Button
            onClick={() => handleExport('pdf')}
            disabled={isExporting || state.lines.length === 0}
            className="gap-2"
          >
            <FileText className="h-4 w-4" />
            Download PDF
          </Button>

          <Button
            variant="outline"
            onClick={() => handleExport('excel')}
            disabled={isExporting || state.lines.length === 0}
            className="gap-2"
          >
            <FileSpreadsheet className="h-4 w-4" />
            Download Excel
          </Button>
        </div>
      </div>

      {/* Summary Cards */}
      <div className="grid gap-4 md:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Total Expenses</CardDescription>
            <CardTitle>{state.lines.length}</CardTitle>
          </CardHeader>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Total Amount</CardDescription>
            <CardTitle>{formatCurrency(metrics.totalAmount)}</CardTitle>
          </CardHeader>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Edited</CardDescription>
            <CardTitle className="text-blue-600">{metrics.dirtyCount}</CardTitle>
          </CardHeader>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardDescription>Warnings</CardDescription>
            <CardTitle className="text-yellow-600">{metrics.warningCount}</CardTitle>
          </CardHeader>
        </Card>
      </div>

      {/* Editable Table */}
      <Card>
        <CardHeader>
          <CardTitle>Expense Lines</CardTitle>
          <CardDescription>
            Click any cell to edit. Changes are not saved - download to export your edits.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {state.lines.length === 0 ? (
            <div className="text-center py-12 text-muted-foreground">
              <FileSpreadsheet className="h-12 w-12 mx-auto mb-4 opacity-50" />
              <p>No reimbursable expenses found for this period</p>
              <p className="text-sm mt-2">Submit receipts or adjust the period</p>
            </div>
          ) : (
            <div className="border rounded-lg overflow-hidden">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-[40px]"></TableHead>
                    <TableHead className="w-[120px]">Date</TableHead>
                    <TableHead className="min-w-[180px]">Vendor</TableHead>
                    <TableHead className="w-[90px]">GL Code</TableHead>
                    <TableHead className="min-w-[200px]">GL Name</TableHead>
                    <TableHead className="w-[100px]">Department</TableHead>
                    <TableHead className="min-w-[250px]">Description</TableHead>
                    <TableHead className="w-[80px] text-center">Receipt</TableHead>
                    <TableHead className="w-[100px] text-right">Amount</TableHead>
                    <TableHead className="w-[100px]">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {state.lines.map((line) => (
                    <>
                      <TableRow
                        key={line.id}
                        className={cn(line.isDirty && 'border-l-2 border-l-blue-500')}
                      >
                        {/* Expansion chevron */}
                        <TableCell>
                          {line.isSplit && line.appliedAllocations && line.appliedAllocations.length > 0 && (
                            <button
                              onClick={() => dispatch({ type: 'TOGGLE_EXPANSION', id: line.id })}
                              className="p-1 hover:bg-accent rounded"
                            >
                              {line.isExpanded ? (
                                <ChevronDown className="h-4 w-4" />
                              ) : (
                                <ChevronRightIcon className="h-4 w-4" />
                              )}
                            </button>
                          )}
                        </TableCell>

                        <TableCell>
                        <EditableDateCell
                          value={line.expenseDate}
                          onChange={(value) =>
                            dispatch({
                              type: 'UPDATE_LINE',
                              id: line.id,
                              field: 'expenseDate',
                              value,
                            })
                          }
                          error={line.validationWarnings.find((w) => w.startsWith('expenseDate'))?.split(': ')[1]}
                        />
                      </TableCell>

                      <TableCell>
                        <EditableTextCell
                          value={line.vendor}
                          onChange={(value) =>
                            dispatch({
                              type: 'UPDATE_LINE',
                              id: line.id,
                              field: 'vendor',
                              value,
                            })
                          }
                          placeholder="Enter vendor..."
                          error={line.validationWarnings.find((w) => w.startsWith('vendor'))?.split(': ')[1]}
                        />
                      </TableCell>

                      <TableCell>
                        {line.isSplit && line.appliedAllocations ? (
                          <SplitIndicatorBadge
                            count={line.appliedAllocations.length}
                            isExpanded={line.isExpanded}
                            onClick={() => dispatch({ type: 'TOGGLE_EXPANSION', id: line.id })}
                          />
                        ) : (
                          <EditableTextCell
                            value={line.glCode}
                            onChange={(value) => handleGLCodeChange(line.id, value)}
                            placeholder="GL code..."
                            error={line.validationWarnings.find((w) => w.startsWith('glCode'))?.split(': ')[1]}
                          />
                        )}
                      </TableCell>

                      {/* GL Name (Vista Description) - Read-only */}
                      <TableCell>
                        <span className="text-sm text-muted-foreground italic">
                          {line.glName || '-'}
                        </span>
                      </TableCell>

                      <TableCell>
                        {line.isSplit && line.appliedAllocations ? (
                          <span className="text-sm text-muted-foreground">-</span>
                        ) : (
                          <EditableTextCell
                            value={line.departmentCode}
                            onChange={(value) => handleFieldUpdate(line.id, 'departmentCode', value)}
                            placeholder="Dept..."
                            error={line.validationWarnings.find((w) => w.startsWith('departmentCode'))?.split(': ')[1]}
                          />
                        )}
                      </TableCell>

                      <TableCell>
                        <EditableTextCell
                          value={line.description}
                          onChange={(value) => handleFieldUpdate(line.id, 'description', value)}
                          placeholder="Description..."
                          error={line.validationWarnings.find((w) => w.startsWith('description'))?.split(': ')[1]}
                        />
                      </TableCell>

                      <TableCell className="text-center">
                        {line.hasReceipt ? (
                          <CheckCircle2 className="h-4 w-4 text-green-600 mx-auto" />
                        ) : (
                          <XCircle className="h-4 w-4 text-muted-foreground mx-auto" />
                        )}
                      </TableCell>

                      <TableCell className="text-right font-mono text-sm">
                        {formatCurrency(line.originalAmount)}
                      </TableCell>

                      {/* Actions Column */}
                      <TableCell>
                        {line.isSplit && line.appliedAllocations ? (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => dispatch({ type: 'REMOVE_SPLIT', id: line.id })}
                          >
                            Remove Split
                          </Button>
                        ) : line.isExpanded ? null : (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => dispatch({ type: 'START_SPLIT', id: line.id })}
                            className="gap-1"
                          >
                            <Split className="h-3 w-3" />
                            Split
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>

                    {/* Split Expansion Panel */}
                    {line.isExpanded && line.allocations.length > 0 && (
                      <TableRow>
                        <TableCell colSpan={9} className="p-0">
                          <SplitExpansionPanel
                            parentId={line.id}
                            parentAmount={line.originalAmount}
                            allocations={line.allocations}
                            onAddAllocation={() => dispatch({ type: 'ADD_ALLOCATION', parentId: line.id })}
                            onRemoveAllocation={(allocationId) =>
                              dispatch({ type: 'REMOVE_ALLOCATION', parentId: line.id, allocationId })
                            }
                            onUpdateAllocation={(allocationId, field, value) =>
                              dispatch({ type: 'UPDATE_ALLOCATION', parentId: line.id, allocationId, field, value })
                            }
                            onToggleEntryMode={(allocationId) =>
                              dispatch({ type: 'TOGGLE_ENTRY_MODE', parentId: line.id, allocationId })
                            }
                            onBulkPaste={(allocations) =>
                              dispatch({ type: 'BULK_PASTE_ALLOCATIONS', parentId: line.id, allocations })
                            }
                            onApply={() => dispatch({ type: 'APPLY_SPLIT', parentId: line.id })}
                            onCancel={() => dispatch({ type: 'CANCEL_SPLIT', id: line.id })}
                          />
                        </TableCell>
                      </TableRow>
                    )}
                    </>
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
