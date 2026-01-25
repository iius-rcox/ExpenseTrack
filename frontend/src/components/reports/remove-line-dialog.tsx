/**
 * RemoveLineDialog - Confirmation dialog for removing an expense line from a report
 *
 * Shows the line details (vendor, amount) and requires explicit confirmation
 * before calling the remove mutation. Prevents accidental data loss.
 */

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
import { Loader2 } from 'lucide-react'

interface RemoveLineDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  lineId: string
  vendor: string
  amount: number
  onConfirm: () => void
  isRemoving: boolean
}

export function RemoveLineDialog({
  open,
  onOpenChange,
  vendor,
  amount,
  onConfirm,
  isRemoving,
}: RemoveLineDialogProps) {
  const formattedAmount = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(amount)

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Remove expense line?</AlertDialogTitle>
          <AlertDialogDescription>
            This will remove the {vendor ? `"${vendor}"` : 'expense'} line ({formattedAmount}) from
            this report. The transaction will become available to add to other reports.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isRemoving}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            onClick={(e) => {
              e.preventDefault()
              onConfirm()
            }}
            disabled={isRemoving}
            className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            data-testid="confirm-delete"
          >
            {isRemoving ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Removing...
              </>
            ) : (
              'Remove'
            )}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
