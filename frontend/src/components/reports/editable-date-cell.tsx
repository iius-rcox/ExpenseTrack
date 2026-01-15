import { useState } from 'react'
import { Input } from '@/components/ui/input'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip'
import { AlertTriangle, Calendar } from 'lucide-react'
import { cn } from '@/lib/utils'

interface EditableDateCellProps {
  value: string // ISO date string
  onChange: (value: string) => void
  error?: string | null
  className?: string
}

/**
 * Editable date cell with simple text input.
 * Users can type dates directly (YYYY-MM-DD format).
 * Future enhancement: Add calendar popover.
 */
export function EditableDateCell({
  value,
  onChange,
  error,
  className,
}: EditableDateCellProps) {
  const [isEditing, setIsEditing] = useState(false)
  const [editValue, setEditValue] = useState(value)

  const handleSave = () => {
    if (editValue !== value) {
      onChange(editValue)
    }
    setIsEditing(false)
  }

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      e.preventDefault()
      handleSave()
    } else if (e.key === 'Escape') {
      e.preventDefault()
      setEditValue(value)
      setIsEditing(false)
    }
  }

  if (isEditing) {
    return (
      <Input
        type="date"
        value={editValue}
        onChange={(e) => setEditValue(e.target.value)}
        onKeyDown={handleKeyDown}
        onBlur={handleSave}
        className={cn('h-8 text-sm', error && 'border-yellow-500', className)}
        autoFocus
      />
    )
  }

  const formattedDate = value ? new Date(value).toLocaleDateString() : ''

  return (
    <div
      className={cn(
        'group/cell flex items-center gap-2 cursor-pointer hover:bg-accent/50 px-2 py-1 rounded transition-colors min-h-[32px]',
        className
      )}
      onClick={() => setIsEditing(true)}
    >
      <Calendar className="h-3 w-3 text-muted-foreground flex-shrink-0" />
      <span className="flex-1 text-sm truncate">
        {formattedDate || 'Select date...'}
      </span>

      {error && (
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger>
              <AlertTriangle className="h-3.5 w-3.5 text-yellow-600 flex-shrink-0" />
            </TooltipTrigger>
            <TooltipContent>
              <p className="text-sm">{error}</p>
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      )}
    </div>
  )
}
