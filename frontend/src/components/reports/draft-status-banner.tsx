import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { FileText, CheckCircle2, Loader2, RefreshCw } from 'lucide-react'

// Simple time ago formatter (no date-fns dependency)
function timeAgo(date: Date): string {
  const seconds = Math.floor((Date.now() - date.getTime()) / 1000)
  if (seconds < 60) return 'just now'
  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) return `${minutes} min ago`
  const hours = Math.floor(minutes / 60)
  return `${hours} hour${hours > 1 ? 's' : ''} ago`
}

interface DraftStatusBannerProps {
  useDraft: boolean
  lastSaved: Date | null
  isSaving: boolean
  onCreateDraft: () => void
  onDiscardDraft: () => void
  onRegenerateDraft?: () => void
}

export function DraftStatusBanner({
  useDraft,
  lastSaved,
  isSaving,
  onCreateDraft,
  onDiscardDraft,
  onRegenerateDraft,
}: DraftStatusBannerProps) {
  if (!useDraft) {
    // Preview mode - show "Save as Draft" button
    return (
      <div className="bg-blue-50 dark:bg-blue-950 border border-blue-200 dark:border-blue-800 rounded-lg p-3 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <FileText className="h-4 w-4 text-blue-600" />
          <span className="text-sm text-blue-900 dark:text-blue-100">
            Viewing preview - edits are temporary
          </span>
        </div>
        <Button onClick={onCreateDraft} size="sm" variant="outline">
          Save as Draft
        </Button>
      </div>
    )
  }

  // Draft mode - show auto-save status
  return (
    <div className="bg-green-50 dark:bg-green-950 border border-green-200 dark:border-green-800 rounded-lg p-3 flex items-center justify-between">
      <div className="flex items-center gap-3">
        <FileText className="h-4 w-4 text-green-600" />
        <Badge variant="outline" className="bg-green-100 dark:bg-green-900 border-green-300">
          Draft
        </Badge>

        {isSaving ? (
          <span className="text-sm text-muted-foreground flex items-center gap-1">
            <Loader2 className="h-3 w-3 animate-spin" />
            Saving...
          </span>
        ) : lastSaved ? (
          <span className="text-sm text-muted-foreground flex items-center gap-1">
            <CheckCircle2 className="h-3 w-3 text-green-600" />
            Auto-saved {timeAgo(lastSaved)}
          </span>
        ) : (
          <span className="text-sm text-muted-foreground">Unsaved changes</span>
        )}
      </div>

      <div className="flex items-center gap-2">
        {onRegenerateDraft && (
          <Button onClick={onRegenerateDraft} size="sm" variant="ghost" title="Refresh from bank data">
            <RefreshCw className="h-4 w-4 mr-1" />
            Refresh
          </Button>
        )}
        <Button onClick={onDiscardDraft} size="sm" variant="ghost">
          Discard Draft
        </Button>
      </div>
    </div>
  )
}
