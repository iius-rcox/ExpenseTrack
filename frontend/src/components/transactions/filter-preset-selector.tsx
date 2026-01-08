/**
 * Filter Preset Selector Component
 *
 * Dropdown menu for managing saved filter presets.
 * Allows users to save current filters, load existing presets, and delete presets.
 */

import { useState } from 'react'
import {
  Bookmark,
  BookmarkPlus,
  ChevronDown,
  Trash2,
  Clock,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useFilterPresets, type FilterPreset } from '@/hooks/use-filter-presets'
import type { TransactionFilters } from '@/types/transaction'
import { hasActiveFilters } from '@/hooks/queries/use-transactions'

interface FilterPresetSelectorProps {
  /** Current filter state */
  filters: TransactionFilters
  /** Callback when a preset is loaded */
  onLoadPreset: (filters: TransactionFilters) => void
}

/**
 * Format relative time for preset display.
 */
function formatRelativeTime(dateStr: string): string {
  const date = new Date(dateStr)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMs / 3600000)
  const diffDays = Math.floor(diffMs / 86400000)

  if (diffMins < 1) return 'Just now'
  if (diffMins < 60) return `${diffMins}m ago`
  if (diffHours < 24) return `${diffHours}h ago`
  if (diffDays < 7) return `${diffDays}d ago`
  return date.toLocaleDateString()
}

export function FilterPresetSelector({
  filters,
  onLoadPreset,
}: FilterPresetSelectorProps) {
  const {
    presets,
    savePreset,
    loadPreset,
    deletePreset,
    hasPresets,
  } = useFilterPresets()

  const [saveDialogOpen, setSaveDialogOpen] = useState(false)
  const [presetName, setPresetName] = useState('')

  const canSave = hasActiveFilters(filters)

  const handleSave = () => {
    if (!presetName.trim()) return
    savePreset(presetName, filters)
    setPresetName('')
    setSaveDialogOpen(false)
  }

  const handleLoad = (preset: FilterPreset) => {
    const loaded = loadPreset(preset.id)
    if (loaded) {
      onLoadPreset(loaded)
    }
  }

  const handleDelete = (presetId: string, e: React.MouseEvent) => {
    e.stopPropagation()
    deletePreset(presetId)
  }

  // Sort presets by most recently used
  const sortedPresets = [...presets].sort((a, b) => {
    const aTime = a.lastUsedAt || a.createdAt
    const bTime = b.lastUsedAt || b.createdAt
    return new Date(bTime).getTime() - new Date(aTime).getTime()
  })

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="outline" size="sm" className="gap-2">
            <Bookmark className="h-4 w-4" />
            <span className="hidden sm:inline">Presets</span>
            <ChevronDown className="h-3 w-3" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-56">
          {/* Save current filters */}
          <DropdownMenuItem
            onClick={() => setSaveDialogOpen(true)}
            disabled={!canSave}
          >
            <BookmarkPlus className="mr-2 h-4 w-4" />
            Save current filters
          </DropdownMenuItem>

          {hasPresets && (
            <>
              <DropdownMenuSeparator />
              {/* List of saved presets */}
              {sortedPresets.map((preset) => (
                <DropdownMenuItem
                  key={preset.id}
                  onClick={() => handleLoad(preset)}
                  className="flex items-center justify-between"
                >
                  <div className="flex items-center gap-2 flex-1 min-w-0">
                    <Bookmark className="h-4 w-4 shrink-0" />
                    <span className="truncate">{preset.name}</span>
                  </div>
                  <div className="flex items-center gap-1">
                    {preset.lastUsedAt && (
                      <span className="text-xs text-muted-foreground">
                        <Clock className="h-3 w-3 inline mr-1" />
                        {formatRelativeTime(preset.lastUsedAt)}
                      </span>
                    )}
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-6 w-6 shrink-0"
                      onClick={(e) => handleDelete(preset.id, e)}
                    >
                      <Trash2 className="h-3 w-3 text-destructive" />
                    </Button>
                  </div>
                </DropdownMenuItem>
              ))}
            </>
          )}

          {!hasPresets && (
            <>
              <DropdownMenuSeparator />
              <div className="px-2 py-1.5 text-sm text-muted-foreground">
                No saved presets
              </div>
            </>
          )}
        </DropdownMenuContent>
      </DropdownMenu>

      {/* Save preset dialog */}
      <Dialog open={saveDialogOpen} onOpenChange={setSaveDialogOpen}>
        <DialogContent className="sm:max-w-[400px]">
          <DialogHeader>
            <DialogTitle>Save Filter Preset</DialogTitle>
            <DialogDescription>
              Save your current filters as a preset for quick access later.
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-4 py-4">
            <div className="grid gap-2">
              <Label htmlFor="preset-name">Preset name</Label>
              <Input
                id="preset-name"
                placeholder="e.g., This Month's Travel"
                value={presetName}
                onChange={(e) => setPresetName(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') handleSave()
                }}
                autoFocus
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setSaveDialogOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleSave} disabled={!presetName.trim()}>
              Save Preset
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
