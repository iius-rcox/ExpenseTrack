/**
 * AllowanceList Component
 *
 * Displays a list of recurring expense allowances with actions
 * for editing, toggling active status, and deleting.
 */

import { useState } from 'react'
import { Switch } from '@/components/ui/switch'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
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
import { toast } from 'sonner'
import { Pencil, Trash2, Loader2 } from 'lucide-react'
import { useAllowances, useUpdateAllowance, useDeleteAllowance } from '@/hooks/queries/use-allowances'
import { AllowanceFormDialog } from './allowance-form-dialog'
import type { Allowance } from '@/types/allowance'
import { getFrequencyDisplayText } from '@/types/allowance'
import { formatCurrency } from '@/lib/utils'

export function AllowanceList() {
  // State for dialogs
  const [editDialogOpen, setEditDialogOpen] = useState(false)
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [selectedAllowance, setSelectedAllowance] = useState<Allowance | undefined>(undefined)
  const [allowanceToDelete, setAllowanceToDelete] = useState<Allowance | null>(null)

  // Queries and mutations
  const { data: allowancesData, isLoading, isError } = useAllowances()
  const { mutate: updateAllowance } = useUpdateAllowance()
  const { mutate: deleteAllowance, isPending: isDeleting } = useDeleteAllowance()

  const allowances = allowancesData?.items || []

  // Map API errors to user-friendly messages (avoid exposing internal details)
  const getUserFriendlyErrorMessage = (error: Error & { status?: number }): string => {
    if (error.status === 404) {
      return 'Allowance not found. It may have been deleted.'
    }
    if (error.status === 409) {
      return 'This allowance was modified by someone else. Please refresh.'
    }
    // Log full error for debugging, show generic message to user
    console.error('Allowance operation failed:', error)
    return 'Something went wrong. Please try again later.'
  }

  const handleEdit = (allowance: Allowance) => {
    setSelectedAllowance(allowance)
    setEditDialogOpen(true)
  }

  const handleToggleActive = (allowance: Allowance) => {
    updateAllowance(
      {
        id: allowance.id,
        data: { isActive: !allowance.isActive },
      },
      {
        onSuccess: () => {
          toast.success(`Allowance ${allowance.isActive ? 'deactivated' : 'activated'}`)
        },
        onError: (error) => {
          toast.error(getUserFriendlyErrorMessage(error))
        },
      }
    )
  }

  const handleDeleteClick = (allowance: Allowance) => {
    setAllowanceToDelete(allowance)
    setDeleteDialogOpen(true)
  }

  const handleDeleteConfirm = () => {
    if (!allowanceToDelete) return

    deleteAllowance(allowanceToDelete.id, {
      onSuccess: () => {
        toast.success('Allowance deleted')
        setAllowanceToDelete(null)
        setDeleteDialogOpen(false)
      },
      onError: (error) => {
        toast.error(getUserFriendlyErrorMessage(error))
      },
    })
  }

  const getFrequencyBadgeVariant = (frequency: number): 'default' | 'secondary' | 'outline' => {
    switch (frequency) {
      case 0: // Weekly
        return 'secondary'
      case 1: // Monthly
        return 'default'
      case 2: // Quarterly
        return 'outline'
      default:
        return 'outline'
    }
  }

  // Loading state
  if (isLoading) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 3 }).map((_, i) => (
          <Skeleton key={i} className="h-16 w-full" />
        ))}
      </div>
    )
  }

  // Error state
  if (isError) {
    return (
      <p className="text-sm text-destructive text-center py-4">
        Failed to load allowances. Please try again.
      </p>
    )
  }

  // Empty state
  if (allowances.length === 0) {
    return (
      <p className="text-sm text-muted-foreground text-center py-4">
        No recurring allowances yet. Add one to get started.
      </p>
    )
  }

  return (
    <>
      <div className="space-y-2">
        {allowances.map((allowance) => (
          <div
            key={allowance.id}
            className="flex items-center justify-between p-3 rounded-lg border"
          >
            <div className="flex items-center gap-3 flex-1 min-w-0">
              <Switch
                checked={allowance.isActive}
                onCheckedChange={() => handleToggleActive(allowance)}
              />
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2">
                  <p className={`font-medium truncate ${!allowance.isActive ? 'text-muted-foreground' : ''}`}>
                    {allowance.vendorName}
                  </p>
                  {!allowance.isActive && (
                    <Badge variant="outline" className="shrink-0">Inactive</Badge>
                  )}
                </div>
                <div className="flex items-center gap-2 text-xs text-muted-foreground">
                  <span className="font-medium">{formatCurrency(allowance.amount)}</span>
                  <span>/</span>
                  <Badge variant={getFrequencyBadgeVariant(allowance.frequency)} className="text-[10px] px-1.5 py-0">
                    {getFrequencyDisplayText(allowance.frequency)}
                  </Badge>
                  {allowance.glCode && (
                    <>
                      <span className="hidden sm:inline">|</span>
                      <span className="hidden sm:inline">GL: {allowance.glCode}</span>
                    </>
                  )}
                </div>
                {allowance.description && (
                  <p className="text-xs text-muted-foreground truncate mt-0.5">
                    {allowance.description}
                  </p>
                )}
              </div>
            </div>

            <div className="flex items-center gap-1 ml-2">
              <Button
                variant="ghost"
                size="icon"
                onClick={() => handleEdit(allowance)}
                aria-label={`Edit ${allowance.vendorName} allowance`}
              >
                <Pencil className="h-4 w-4" />
              </Button>
              <Button
                variant="ghost"
                size="icon"
                onClick={() => handleDeleteClick(allowance)}
                aria-label={`Delete ${allowance.vendorName} allowance`}
              >
                <Trash2 className="h-4 w-4 text-destructive" />
              </Button>
            </div>
          </div>
        ))}
      </div>

      {/* Edit Dialog */}
      <AllowanceFormDialog
        open={editDialogOpen}
        onOpenChange={(open) => {
          setEditDialogOpen(open)
          if (!open) setSelectedAllowance(undefined)
        }}
        allowance={selectedAllowance}
      />

      {/* Delete Confirmation Dialog */}
      <AlertDialog
        open={deleteDialogOpen}
        onOpenChange={(open) => {
          setDeleteDialogOpen(open)
          // Clear state when dialog closes (including outside click)
          if (!open) setAllowanceToDelete(null)
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Allowance?</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete the "{allowanceToDelete?.vendorName}" recurring allowance.
              This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>
              Cancel
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDeleteConfirm}
              disabled={isDeleting}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {isDeleting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  )
}
