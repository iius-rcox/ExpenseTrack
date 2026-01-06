'use client'

/**
 * EmptyMissingReceipts Component (T015)
 *
 * Empty state for the missing receipts list when no items are found.
 * Provides contextual messaging based on current filter state.
 *
 * Part of Feature 026: Missing Receipts UI
 */

import { EmptyState } from '@/components/design-system/empty-state'
import { CheckCircle2, ArchiveX } from 'lucide-react'

interface EmptyMissingReceiptsProps {
  /** Whether the list is filtered to show dismissed items */
  showingDismissed?: boolean
  /** Callback when primary action is clicked (context-dependent) */
  onUpload?: () => void
  /** Callback when "Import Statement" is clicked */
  onImport?: () => void
}

export function EmptyMissingReceipts({
  showingDismissed = false,
  onUpload,
  onImport,
}: EmptyMissingReceiptsProps) {
  // Empty state when viewing dismissed items
  if (showingDismissed) {
    return (
      <EmptyState
        icon={ArchiveX}
        title="No dismissed receipts"
        description="Items you dismiss will appear here. Toggle 'Show dismissed' off to view active missing receipts."
        action={
          onUpload
            ? {
                label: 'View Active Items',
                onClick: onUpload,
              }
            : undefined
        }
      />
    )
  }

  // Primary empty state - all caught up!
  return (
    <EmptyState
      icon={CheckCircle2}
      title="All receipts accounted for!"
      description="You have no reimbursable expenses missing receipts. Nice work keeping your records organized."
      action={
        onUpload
          ? {
              label: 'Upload Receipt',
              onClick: onUpload,
            }
          : undefined
      }
      secondaryAction={
        onImport
          ? {
              label: 'Import Statement',
              onClick: onImport,
            }
          : undefined
      }
    />
  )
}

/**
 * Empty state specifically for the widget (compact version)
 */
export function EmptyMissingReceiptsWidget() {
  return (
    <div className="flex flex-col items-center justify-center py-6 text-center">
      <div className="rounded-full bg-green-100 dark:bg-green-900/30 p-3">
        <CheckCircle2 className="h-6 w-6 text-green-600 dark:text-green-500" />
      </div>
      <p className="mt-3 text-sm font-medium">All caught up!</p>
      <p className="mt-1 text-xs text-muted-foreground">
        No missing receipts to track
      </p>
    </div>
  )
}
