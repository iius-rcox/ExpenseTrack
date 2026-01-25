import { useState, useEffect, useRef, forwardRef, useImperativeHandle } from 'react'
import { Input } from '@/components/ui/input'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip'
import { Pencil, AlertTriangle } from 'lucide-react'
import { cn } from '@/lib/utils'

interface EditableTextCellProps {
  value: string
  onChange: (value: string) => void
  placeholder?: string
  error?: string | null
  className?: string
  /** Custom tab index for vertical navigation in splits */
  tabIndex?: number
  /** Called when Tab is pressed in edit mode */
  onTabNavigation?: (direction: 'next' | 'prev') => void
  /** Data attribute for field type identification */
  'data-field-type'?: string
}

/**
 * Editable text cell with click-to-edit pattern.
 * Supports forwardRef for focus management in split allocation panels.
 */
export const EditableTextCell = forwardRef<HTMLDivElement, EditableTextCellProps>(
  function EditableTextCell(
    {
      value,
      onChange,
      placeholder = 'Click to edit...',
      error,
      className,
      tabIndex,
      onTabNavigation,
      'data-field-type': dataFieldType,
    },
    ref
  ) {
    const [isEditing, setIsEditing] = useState(false)
    const [editValue, setEditValue] = useState(value)
    const inputRef = useRef<HTMLInputElement>(null)
    const cellRef = useRef<HTMLDivElement>(null)

    // Expose the cell div ref for external focus management
    useImperativeHandle(ref, () => cellRef.current as HTMLDivElement, [])

    // Update edit value when prop changes (external updates)
    useEffect(() => {
      if (!isEditing) {
        setEditValue(value)
      }
    }, [value, isEditing])

    // Auto-focus and select text when entering edit mode
    useEffect(() => {
      if (isEditing && inputRef.current) {
        inputRef.current.focus()
        inputRef.current.select()
      }
    }, [isEditing])

    const handleSave = () => {
      if (editValue !== value) {
        onChange(editValue)
      }
      setIsEditing(false)
    }

    const handleCancel = () => {
      setEditValue(value)
      setIsEditing(false)
    }

    const handleKeyDown = (e: React.KeyboardEvent) => {
      if (e.key === 'Enter') {
        e.preventDefault()
        handleSave()
      } else if (e.key === 'Escape') {
        e.preventDefault()
        handleCancel()
      } else if (e.key === 'Tab' && onTabNavigation) {
        e.preventDefault()
        handleSave()
        onTabNavigation(e.shiftKey ? 'prev' : 'next')
      }
    }

    // Handle keyboard events on the non-editing cell (for Tab navigation)
    const handleCellKeyDown = (e: React.KeyboardEvent) => {
      if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault()
        setIsEditing(true)
      } else if (e.key === 'Tab' && onTabNavigation) {
        e.preventDefault()
        onTabNavigation(e.shiftKey ? 'prev' : 'next')
      }
    }

    if (isEditing) {
      return (
        <Input
          ref={inputRef}
          value={editValue}
          onChange={(e) => setEditValue(e.target.value)}
          onKeyDown={handleKeyDown}
          onBlur={handleSave}
          className={cn('h-8 text-sm', error && 'border-yellow-500', className)}
          placeholder={placeholder}
          data-field-type={dataFieldType}
        />
      )
    }

    return (
      <div
        ref={cellRef}
        className={cn(
          'group/cell flex items-center gap-2 cursor-text hover:bg-accent/50 px-2 py-1 rounded transition-colors min-h-[32px]',
          'focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-1',
          className
        )}
        onClick={() => setIsEditing(true)}
        onKeyDown={handleCellKeyDown}
        tabIndex={tabIndex ?? 0}
        role="button"
        aria-label={`Edit ${dataFieldType || 'field'}: ${value || placeholder}`}
        data-field-type={dataFieldType}
      >
        <span
          className={cn(
            'flex-1 text-sm truncate',
            !value && 'text-muted-foreground italic'
          )}
          title={value}
        >
          {value || placeholder}
        </span>

        {error && (
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger>
                <AlertTriangle className="h-3.5 w-3.5 text-yellow-600 flex-shrink-0" />
              </TooltipTrigger>
              <TooltipContent className="max-w-xs">
                <p className="text-sm">{error}</p>
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
        )}

        <Pencil className="h-3 w-3 opacity-0 group-hover/cell:opacity-100 transition-opacity flex-shrink-0 text-muted-foreground" />
      </div>
    )
  }
)
