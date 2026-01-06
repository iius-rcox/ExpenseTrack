'use client'

/**
 * ReceiptUrlDialog Component (T019)
 *
 * Dialog for adding, editing, or clearing a receipt URL for a transaction.
 * URLs are stored as plain text without validation (user responsibility).
 *
 * Part of Feature 026: Missing Receipts UI - User Story 2
 */

import { useState, useEffect } from 'react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Link2, ExternalLink, Trash2, Loader2 } from 'lucide-react'

interface ReceiptUrlDialogProps {
  /** Whether the dialog is open */
  open: boolean
  /** Callback when dialog should close */
  onOpenChange: (open: boolean) => void
  /** Current URL value (null if no URL set) */
  currentUrl: string | null
  /** Transaction description for context */
  transactionDescription: string
  /** Callback when URL is saved */
  onSave: (url: string | null) => void
  /** Whether save operation is in progress */
  isSaving?: boolean
}

export function ReceiptUrlDialog({
  open,
  onOpenChange,
  currentUrl,
  transactionDescription,
  onSave,
  isSaving = false,
}: ReceiptUrlDialogProps) {
  const [url, setUrl] = useState(currentUrl || '')

  // Reset form when dialog opens with new value
  useEffect(() => {
    if (open) {
      setUrl(currentUrl || '')
    }
  }, [open, currentUrl])

  const hasChanges = url !== (currentUrl || '')
  const isEditing = !!currentUrl

  const handleSave = () => {
    // Trim and save (empty string becomes null to clear)
    const trimmedUrl = url.trim()
    onSave(trimmedUrl || null)
  }

  const handleClear = () => {
    onSave(null)
  }

  const handleOpenUrl = () => {
    if (url.trim()) {
      window.open(url.trim(), '_blank', 'noopener,noreferrer')
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Link2 className="h-5 w-5" />
            {isEditing ? 'Edit Receipt URL' : 'Add Receipt URL'}
          </DialogTitle>
          <DialogDescription>
            Store a link to where you can retrieve this receipt (e.g., airline portal, hotel booking confirmation).
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          {/* Transaction context */}
          <div className="rounded-md bg-muted p-3">
            <p className="text-sm font-medium truncate" title={transactionDescription}>
              {transactionDescription}
            </p>
          </div>

          {/* URL input */}
          <div className="space-y-2">
            <Label htmlFor="receipt-url">Receipt URL</Label>
            <div className="flex gap-2">
              <Input
                id="receipt-url"
                type="url"
                placeholder="https://example.com/receipt/123"
                value={url}
                onChange={(e) => setUrl(e.target.value)}
                disabled={isSaving}
                className="flex-1"
              />
              {url.trim() && (
                <Button
                  type="button"
                  variant="outline"
                  size="icon"
                  onClick={handleOpenUrl}
                  title="Open URL in new tab"
                >
                  <ExternalLink className="h-4 w-4" />
                </Button>
              )}
            </div>
            <p className="text-xs text-muted-foreground">
              The URL is stored as-is without validation. Make sure it's correct before saving.
            </p>
          </div>
        </div>

        <DialogFooter className="flex-col sm:flex-row gap-2">
          {/* Clear button (only show if editing) */}
          {isEditing && (
            <Button
              type="button"
              variant="outline"
              onClick={handleClear}
              disabled={isSaving}
              className="sm:mr-auto"
            >
              <Trash2 className="mr-2 h-4 w-4" />
              Clear URL
            </Button>
          )}

          <Button
            type="button"
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={isSaving}
          >
            Cancel
          </Button>

          <Button
            type="button"
            onClick={handleSave}
            disabled={!hasChanges || isSaving}
          >
            {isSaving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {isEditing ? 'Update URL' : 'Save URL'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
