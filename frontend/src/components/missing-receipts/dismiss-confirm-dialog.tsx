'use client'

/**
 * DismissConfirmDialog Component (T028)
 *
 * Confirmation dialog for dismissing a transaction from the missing receipts list.
 * Explains that dismissed items can be restored via the "Show Dismissed" filter.
 *
 * Part of Feature 026: Missing Receipts UI - User Story 5
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
import { X } from 'lucide-react'

interface DismissConfirmDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** Callback when dialog should close */
  onOpenChange: (open: boolean) => void
  /** Transaction description for context */
  transactionDescription: string
  /** Callback when user confirms dismissal */
  onConfirm: () => void
}

export function DismissConfirmDialog({
  open,
  onOpenChange,
  transactionDescription,
  onConfirm,
}: DismissConfirmDialogProps) {
  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle className="flex items-center gap-2">
            <X className="h-5 w-5" />
            Dismiss Missing Receipt
          </AlertDialogTitle>
          <AlertDialogDescription className="space-y-2">
            <span className="block">
              Are you sure you want to dismiss this transaction?
            </span>
            <span className="block font-medium text-foreground">
              "{transactionDescription}"
            </span>
            <span className="block text-xs mt-2">
              Dismissed items won't appear in the missing receipts list but can be
              restored anytime using the "Show Dismissed" filter.
            </span>
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Cancel</AlertDialogCancel>
          <AlertDialogAction onClick={onConfirm}>
            Dismiss
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
