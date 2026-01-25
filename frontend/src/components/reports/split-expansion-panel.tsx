import { useRef, useCallback, useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { EditableTextCell } from './editable-text-cell'
import { Plus, X, CheckCircle2, AlertTriangle, XCircle, ClipboardPaste } from 'lucide-react'
import { formatCurrency, cn } from '@/lib/utils'
import { parseExcelPaste, isExcelData } from '@/lib/parse-excel-paste'
import type { SplitAllocation } from '@/types/report-editor'
import { toast } from 'sonner'

interface SplitExpansionPanelProps {
  parentId: string
  parentAmount: number
  allocations: SplitAllocation[]
  onAddAllocation: () => void
  onRemoveAllocation: (allocationId: string) => void
  onUpdateAllocation: (allocationId: string, field: keyof SplitAllocation, value: any) => void
  onToggleEntryMode: (allocationId: string) => void
  onBulkPaste: (allocations: { glCode?: string; departmentCode: string; amount: number }[]) => void
  onApply: () => void
  onCancel: () => void
}

// Field types for vertical navigation
type FieldType = 'glCode' | 'department' | 'amount'
const FIELD_ORDER: FieldType[] = ['glCode', 'department', 'amount']

export function SplitExpansionPanel({
  parentAmount,
  allocations,
  onAddAllocation,
  onRemoveAllocation,
  onUpdateAllocation,
  onToggleEntryMode,
  onBulkPaste,
  onApply,
  onCancel,
}: SplitExpansionPanelProps) {
  // Refs for navigation - stores refs by allocation index and field type
  const cellRefs = useRef<Map<string, HTMLElement | null>>(new Map())
  const panelRef = useRef<HTMLDivElement>(null)
  const [pasteHint, setPasteHint] = useState(false)

  // Handle paste event for Excel data
  const handlePaste = useCallback((e: ClipboardEvent) => {
    const clipboardText = e.clipboardData?.getData('text/plain')
    if (!clipboardText) return

    // Check if it looks like Excel data (tab-separated)
    if (!isExcelData(clipboardText)) return

    // Prevent default paste behavior
    e.preventDefault()

    // Parse the Excel data
    const result = parseExcelPaste(clipboardText)

    if (!result.success) {
      toast.error('Could not parse clipboard data', {
        description: result.errors[0] || 'Invalid format',
      })
      return
    }

    // Apply the bulk paste
    onBulkPaste(result.allocations)

    // Show success message
    toast.success(`Pasted ${result.allocations.length} allocations`, {
      description: result.errors.length > 0
        ? `${result.errors.length} row(s) skipped`
        : undefined,
    })
  }, [onBulkPaste])

  // Attach paste listener to panel
  useEffect(() => {
    const panel = panelRef.current
    if (!panel) return

    panel.addEventListener('paste', handlePaste)
    return () => panel.removeEventListener('paste', handlePaste)
  }, [handlePaste])

  // Calculate totals
  const totalAmount = allocations.reduce((sum, a) => sum + a.amount, 0)
  const totalPercentage = allocations.reduce((sum, a) => sum + a.percentage, 0)
  const isValid = Math.abs(totalPercentage - 100) < 0.01

  // Get ref key for a cell
  const getRefKey = (rowIndex: number, fieldType: FieldType) => `${rowIndex}-${fieldType}`

  // Register a cell ref
  const registerRef = useCallback((rowIndex: number, fieldType: FieldType, el: HTMLElement | null) => {
    const key = getRefKey(rowIndex, fieldType)
    cellRefs.current.set(key, el)
  }, [])

  // Navigate to a cell
  const focusCell = useCallback((rowIndex: number, fieldType: FieldType) => {
    const key = getRefKey(rowIndex, fieldType)
    const el = cellRefs.current.get(key)
    if (el) {
      el.focus()
      return true
    }
    return false
  }, [])

  // Handle vertical tab navigation
  const handleTabNavigation = useCallback((rowIndex: number, fieldType: FieldType, direction: 'next' | 'prev') => {
    const numRows = allocations.length
    const fieldIndex = FIELD_ORDER.indexOf(fieldType)

    if (direction === 'next') {
      // Try next row in same column
      if (rowIndex < numRows - 1) {
        focusCell(rowIndex + 1, fieldType)
      } else {
        // At last row - move to first row of next column
        const nextFieldIndex = fieldIndex + 1
        if (nextFieldIndex < FIELD_ORDER.length) {
          focusCell(0, FIELD_ORDER[nextFieldIndex])
        }
        // If at last field of last row, let browser handle (default behavior)
      }
    } else {
      // Shift+Tab: go to previous row in same column
      if (rowIndex > 0) {
        focusCell(rowIndex - 1, fieldType)
      } else {
        // At first row - move to last row of previous column
        const prevFieldIndex = fieldIndex - 1
        if (prevFieldIndex >= 0) {
          focusCell(numRows - 1, FIELD_ORDER[prevFieldIndex])
        }
        // If at first field of first row, let browser handle (default behavior)
      }
    }
  }, [allocations.length, focusCell])

  // Handle Tab on amount input (native input field)
  const handleAmountKeyDown = useCallback((e: React.KeyboardEvent, rowIndex: number) => {
    if (e.key === 'Tab') {
      e.preventDefault()
      handleTabNavigation(rowIndex, 'amount', e.shiftKey ? 'prev' : 'next')
    }
  }, [handleTabNavigation])

  return (
    <div
      ref={panelRef}
      className="p-4 bg-muted/30 border-l-2 border-l-blue-500 focus:outline-none"
      data-testid="split-expansion-panel"
      tabIndex={0}
      onFocus={() => setPasteHint(true)}
      onBlur={() => setPasteHint(false)}
    >
      <div className="space-y-3">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center gap-2">
            <h4 className="text-sm font-medium">Split Allocations</h4>
            {pasteHint && (
              <span className="text-xs text-muted-foreground flex items-center gap-1">
                <ClipboardPaste className="h-3 w-3" />
                Paste from Excel (Ctrl+V)
              </span>
            )}
          </div>
          <Badge variant={isValid ? 'default' : 'destructive'} className="gap-1">
            {isValid ? (
              <>
                <CheckCircle2 className="h-3 w-3" />
                100% Complete
              </>
            ) : totalPercentage > 100 ? (
              <>
                <AlertTriangle className="h-3 w-3" />
                Over by {(totalPercentage - 100).toFixed(2)}%
              </>
            ) : (
              <>
                <XCircle className="h-3 w-3" />
                Remaining: {(100 - totalPercentage).toFixed(2)}%
              </>
            )}
          </Badge>
        </div>

        {/* Allocation Rows */}
        {allocations.map((alloc, index) => (
          <div
            key={alloc.id}
            className="flex items-center gap-3 p-3 bg-background rounded-lg border"
            data-testid={`allocation-row-${index}`}
          >
            <span className="text-sm text-muted-foreground w-16">#{index + 1}</span>

            {/* GL Code */}
            <div className="flex-1">
              <Label className="text-xs mb-1">GL Code</Label>
              <EditableTextCell
                value={alloc.glCode}
                onChange={(value) => onUpdateAllocation(alloc.id, 'glCode', value)}
                placeholder="Enter GL code..."
                className="h-8"
                data-field-type="glCode"
                onTabNavigation={(dir) => handleTabNavigation(index, 'glCode', dir)}
                ref={(el) => registerRef(index, 'glCode', el as HTMLElement | null)}
              />
            </div>

            {/* Department */}
            <div className="flex-1">
              <Label className="text-xs mb-1">Department</Label>
              <EditableTextCell
                value={alloc.departmentCode}
                onChange={(value) => onUpdateAllocation(alloc.id, 'departmentCode', value)}
                placeholder="Dept..."
                className="h-8"
                data-field-type="department"
                onTabNavigation={(dir) => handleTabNavigation(index, 'department', dir)}
                ref={(el) => registerRef(index, 'department', el as HTMLElement | null)}
              />
            </div>

            {/* Amount Input */}
            <div className="w-32">
              <Label className="text-xs mb-1 flex items-center gap-1">
                Amount
                <button
                  type="button"
                  onClick={() => onToggleEntryMode(alloc.id)}
                  className="text-xs text-blue-600 hover:underline"
                  tabIndex={-1}
                >
                  (use %)
                </button>
              </Label>
              <Input
                type="number"
                value={alloc.amount.toFixed(2)}
                onChange={(e) =>
                  onUpdateAllocation(alloc.id, 'amount', parseFloat(e.target.value) || 0)
                }
                onKeyDown={(e) => handleAmountKeyDown(e, index)}
                className="h-8 text-sm"
                step="0.01"
                data-field-type="amount"
                data-testid={`amount-input-${index}`}
                ref={(el) => registerRef(index, 'amount', el as HTMLElement | null)}
              />
            </div>

            {/* Calculated Percentage */}
            <div className="w-20">
              <Label className="text-xs mb-1">%</Label>
              <div className="h-8 flex items-center">
                <Badge variant="outline" className="text-xs">
                  {alloc.percentage.toFixed(1)}%
                </Badge>
              </div>
            </div>

            {/* Remove */}
            <Button
              variant="ghost"
              size="icon"
              onClick={() => onRemoveAllocation(alloc.id)}
              disabled={allocations.length <= 2}
              className="h-8 w-8"
              tabIndex={-1}
            >
              <X className="h-4 w-4" />
            </Button>
          </div>
        ))}

        {/* Add Allocation */}
        {allocations.length < 10 && (
          <Button
            variant="outline"
            size="sm"
            onClick={onAddAllocation}
            className="w-full gap-2"
          >
            <Plus className="h-4 w-4" />
            Add Allocation
          </Button>
        )}

        {/* Total Summary */}
        <div className="flex items-center justify-between p-3 bg-background rounded-lg border-2 border-primary/20">
          <div className="flex items-center gap-4">
            <span className="text-sm font-medium">Total:</span>
            <span className="font-mono">{formatCurrency(totalAmount)}</span>
            <span className="text-muted-foreground">/</span>
            <span className="font-mono">{formatCurrency(parentAmount)}</span>
          </div>
          <Badge
            variant={isValid ? 'default' : 'destructive'}
            className={cn('font-mono', isValid && 'bg-green-600')}
          >
            {totalPercentage.toFixed(2)}%
          </Badge>
        </div>

        {/* Actions */}
        <div className="flex items-center gap-2 justify-end">
          <Button variant="ghost" size="sm" onClick={onCancel}>
            Cancel
          </Button>
          <Button onClick={onApply} disabled={!isValid} size="sm">
            Apply Split
          </Button>
        </div>
      </div>
    </div>
  )
}
