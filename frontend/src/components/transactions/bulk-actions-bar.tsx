/**
 * BulkActionsBar Component (T057)
 *
 * Floating action bar for bulk operations on selected transactions:
 * - Categorize multiple transactions
 * - Add tags to multiple transactions
 * - Export selected transactions
 * - Delete selected transactions (with confirmation)
 *
 * @see data-model.md Section 4.5 for BulkActionsBarProps specification
 */

import { useState, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Tag,
  FolderOpen,
  Download,
  Trash2,
  X,
  Check,
  AlertTriangle,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { Checkbox } from '@/components/ui/checkbox';
import type { BulkActionsBarProps } from '@/types/transaction';

/**
 * Extended props with additional data
 */
interface BulkActionsBarComponentProps extends BulkActionsBarProps {
  /** Available categories for bulk categorization */
  categories: { id: string; name: string }[];
  /** Available tags for bulk tagging */
  availableTags: string[];
  /** Whether a bulk operation is in progress */
  isProcessing?: boolean;
}

/**
 * Bulk actions bar component
 */
export function BulkActionsBar({
  selectedCount,
  categories,
  availableTags,
  onCategorize,
  onTag,
  onExport,
  onDelete,
  onClearSelection,
  isProcessing = false,
}: BulkActionsBarComponentProps) {
  // State for dialogs
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [selectedTags, setSelectedTags] = useState<string[]>([]);
  const [isTagDropdownOpen, setIsTagDropdownOpen] = useState(false);

  // Handle category selection
  const handleCategoryChange = useCallback(
    (categoryId: string) => {
      onCategorize(categoryId);
    },
    [onCategorize]
  );

  // Handle tag toggle
  const handleTagToggle = useCallback((tag: string) => {
    setSelectedTags((prev) =>
      prev.includes(tag) ? prev.filter((t) => t !== tag) : [...prev, tag]
    );
  }, []);

  // Apply selected tags
  const handleApplyTags = useCallback(() => {
    if (selectedTags.length > 0) {
      onTag(selectedTags);
      setSelectedTags([]);
      setIsTagDropdownOpen(false);
    }
  }, [selectedTags, onTag]);

  // Handle delete confirmation
  const handleDeleteConfirm = useCallback(() => {
    onDelete();
    setShowDeleteConfirm(false);
  }, [onDelete]);

  // Don't render if nothing is selected
  if (selectedCount === 0) {
    return null;
  }

  return (
    <>
      <AnimatePresence>
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: 20 }}
          transition={{ type: 'spring', damping: 20, stiffness: 300 }}
          className={cn(
            'fixed bottom-6 left-1/2 -translate-x-1/2 z-50',
            'flex items-center gap-3 px-4 py-3',
            'bg-background/95 backdrop-blur-sm',
            'border rounded-xl shadow-lg',
            isProcessing && 'opacity-75 pointer-events-none'
          )}
        >
          {/* Selection Count */}
          <div className="flex items-center gap-2 pr-3 border-r">
            <Badge variant="secondary" className="text-sm font-semibold">
              {selectedCount}
            </Badge>
            <span className="text-sm text-muted-foreground">
              {selectedCount === 1 ? 'item selected' : 'items selected'}
            </span>
          </div>

          {/* Categorize Action */}
          <Select onValueChange={handleCategoryChange} disabled={isProcessing}>
            <SelectTrigger className="w-[160px] h-9">
              <FolderOpen className="h-4 w-4 mr-2" />
              <SelectValue placeholder="Set category" />
            </SelectTrigger>
            <SelectContent>
              {categories.map((category) => (
                <SelectItem key={category.id} value={category.id}>
                  {category.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          {/* Tag Action */}
          <DropdownMenu
            open={isTagDropdownOpen}
            onOpenChange={setIsTagDropdownOpen}
          >
            <DropdownMenuTrigger asChild>
              <Button
                variant="outline"
                size="sm"
                className="gap-2"
                disabled={isProcessing}
              >
                <Tag className="h-4 w-4" />
                Add tags
                {selectedTags.length > 0 && (
                  <Badge variant="secondary" className="ml-1">
                    {selectedTags.length}
                  </Badge>
                )}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent className="w-56" align="center">
              <div className="p-2">
                <div className="font-medium mb-2">Select Tags</div>
                <div className="space-y-2 max-h-48 overflow-y-auto">
                  {availableTags.length > 0 ? (
                    availableTags.map((tag) => (
                      <label
                        key={tag}
                        className="flex items-center gap-2 cursor-pointer"
                      >
                        <Checkbox
                          checked={selectedTags.includes(tag)}
                          onCheckedChange={() => handleTagToggle(tag)}
                        />
                        <span className="text-sm">{tag}</span>
                      </label>
                    ))
                  ) : (
                    <p className="text-sm text-muted-foreground">
                      No tags available
                    </p>
                  )}
                </div>
                {selectedTags.length > 0 && (
                  <Button
                    size="sm"
                    className="w-full mt-3"
                    onClick={handleApplyTags}
                  >
                    <Check className="h-4 w-4 mr-2" />
                    Apply {selectedTags.length} tag
                    {selectedTags.length !== 1 && 's'}
                  </Button>
                )}
              </div>
            </DropdownMenuContent>
          </DropdownMenu>

          {/* Export Action */}
          <Button
            variant="outline"
            size="sm"
            className="gap-2"
            onClick={onExport}
            disabled={isProcessing}
          >
            <Download className="h-4 w-4" />
            Export
          </Button>

          {/* Delete Action */}
          <Button
            variant="outline"
            size="sm"
            className="gap-2 text-destructive hover:text-destructive hover:bg-destructive/10"
            onClick={() => setShowDeleteConfirm(true)}
            disabled={isProcessing}
          >
            <Trash2 className="h-4 w-4" />
            Delete
          </Button>

          {/* Clear Selection */}
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8"
            onClick={onClearSelection}
            disabled={isProcessing}
          >
            <X className="h-4 w-4" />
          </Button>
        </motion.div>
      </AnimatePresence>

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={showDeleteConfirm} onOpenChange={setShowDeleteConfirm}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle className="flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-destructive" />
              Delete {selectedCount} transaction{selectedCount !== 1 && 's'}?
            </AlertDialogTitle>
            <AlertDialogDescription>
              This action cannot be undone. The selected transactions will be
              permanently removed from your records.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={handleDeleteConfirm}
            >
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}

export default BulkActionsBar;
