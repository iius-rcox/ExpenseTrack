import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { EditableTextCell } from './editable-text-cell'
import { Plus, X, CheckCircle2, AlertTriangle, XCircle } from 'lucide-react'
import { formatCurrency, cn } from '@/lib/utils'
import type { SplitAllocation } from '@/types/report-editor'

interface SplitExpansionPanelProps {
  parentId: string
  parentAmount: number
  allocations: SplitAllocation[]
  onAddAllocation: () => void
  onRemoveAllocation: (allocationId: string) => void
  onUpdateAllocation: (allocationId: string, field: keyof SplitAllocation, value: any) => void
  onToggleEntryMode: (allocationId: string) => void
  onApply: () => void
  onCancel: () => void
}

export function SplitExpansionPanel({
  parentAmount,
  allocations,
  onAddAllocation,
  onRemoveAllocation,
  onUpdateAllocation,
  onToggleEntryMode,
  onApply,
  onCancel,
}: SplitExpansionPanelProps) {
  // Calculate totals
  const totalAmount = allocations.reduce((sum, a) => sum + a.amount, 0)
  const totalPercentage = allocations.reduce((sum, a) => sum + a.percentage, 0)
  const isValid = Math.abs(totalPercentage - 100) < 0.01

  return (
    <div className="p-4 bg-muted/30 border-l-2 border-l-blue-500">
      <div className="space-y-3">
        <div className="flex items-center justify-between mb-2">
          <h4 className="text-sm font-medium">Split Allocations</h4>
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
                className="h-8 text-sm"
                step="0.01"
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
