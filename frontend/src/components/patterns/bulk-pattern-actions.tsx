/**
 * BulkPatternActions Component
 *
 * Floating action bar for bulk operations on selected patterns:
 * - Suppress multiple patterns
 * - Enable multiple patterns
 * - Delete multiple patterns (with confirmation)
 *
 * @see specs/023-expense-prediction/plan.md for pattern management requirements
 */

import { useState, useCallback } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { EyeOff, Eye, Trash2, X } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'

export interface BulkPatternActionsProps {
  /** Number of selected patterns */
  selectedCount: number
  /** Callback to suppress selected patterns */
  onSuppress: () => void
  /** Callback to enable selected patterns */
  onEnable: () => void
  /** Callback to delete selected patterns */
  onDelete: () => void
  /** Callback to clear selection */
  onClearSelection: () => void
  /** Whether a bulk operation is in progress */
  isProcessing?: boolean
  /** Additional CSS classes */
  className?: string
}

/**
 * Animation variants for the floating bar
 */
const barVariants = {
  hidden: {
    opacity: 0,
    y: 20,
    scale: 0.95,
  },
  visible: {
    opacity: 1,
    y: 0,
    scale: 1,
    transition: {
      type: 'spring' as const,
      damping: 20,
      stiffness: 300,
    },
  },
  exit: {
    opacity: 0,
    y: 20,
    scale: 0.95,
    transition: {
      duration: 0.15,
    },
  },
}

/**
 * BulkPatternActions floating bar component
 */
export function BulkPatternActions({
  selectedCount,
  onSuppress,
  onEnable,
  onDelete,
  onClearSelection,
  isProcessing = false,
  className,
}: BulkPatternActionsProps) {
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false)

  const handleDeleteConfirm = useCallback(() => {
    onDelete()
    setShowDeleteConfirm(false)
  }, [onDelete])

  // Don't render if nothing is selected
  if (selectedCount === 0) {
    return null
  }

  return (
    <>
      <AnimatePresence>
        <motion.div
          variants={barVariants}
          initial="hidden"
          animate="visible"
          exit="exit"
          className={cn(
            'fixed bottom-6 left-1/2 -translate-x-1/2 z-50',
            'flex items-center gap-3 px-4 py-3 rounded-xl',
            'bg-background/95 backdrop-blur-sm border shadow-lg',
            'supports-[backdrop-filter]:bg-background/80',
            className
          )}
        >
          {/* Selection Count */}
          <div className="flex items-center gap-2 pr-3 border-r">
            <Badge variant="secondary" className="font-mono">
              {selectedCount}
            </Badge>
            <span className="text-sm text-muted-foreground">
              pattern{selectedCount !== 1 ? 's' : ''} selected
            </span>
          </div>

          {/* Suppress Button */}
          <Button
            variant="outline"
            size="sm"
            onClick={onSuppress}
            disabled={isProcessing}
            className="gap-1.5"
          >
            <EyeOff className="h-4 w-4" />
            <span className="hidden sm:inline">Suppress</span>
          </Button>

          {/* Enable Button */}
          <Button
            variant="outline"
            size="sm"
            onClick={onEnable}
            disabled={isProcessing}
            className="gap-1.5"
          >
            <Eye className="h-4 w-4" />
            <span className="hidden sm:inline">Enable</span>
          </Button>

          {/* Delete Button */}
          <Button
            variant="outline"
            size="sm"
            onClick={() => setShowDeleteConfirm(true)}
            disabled={isProcessing}
            className="gap-1.5 text-destructive hover:text-destructive hover:bg-destructive/10"
          >
            <Trash2 className="h-4 w-4" />
            <span className="hidden sm:inline">Delete</span>
          </Button>

          {/* Clear Selection Button */}
          <Button
            variant="ghost"
            size="icon"
            onClick={onClearSelection}
            className="ml-1 h-8 w-8 text-muted-foreground"
          >
            <X className="h-4 w-4" />
            <span className="sr-only">Clear selection</span>
          </Button>
        </motion.div>
      </AnimatePresence>

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={showDeleteConfirm} onOpenChange={setShowDeleteConfirm}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete {selectedCount} Pattern{selectedCount !== 1 ? 's' : ''}?</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete {selectedCount === 1 ? 'this pattern' : 'these patterns'} and all associated predictions.
              The system will need to relearn {selectedCount === 1 ? 'this vendor' : 'these vendors'} from future expense reports.
              This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDeleteConfirm}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Delete Pattern{selectedCount !== 1 ? 's' : ''}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}
