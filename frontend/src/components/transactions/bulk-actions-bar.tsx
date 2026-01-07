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
  CircleCheck,
  CircleX,
  Layers,
} from 'lucide-react';
import { cn, safeDisplayString } from '@/lib/utils';
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
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { Checkbox } from '@/components/ui/checkbox';
import type { BulkActionsBarProps } from '@/types/transaction';

/**
 * Selection composition for mixed lists (Feature 028)
 */
export interface SelectionComposition {
  /** Number of transactions in selection */
  transactionCount: number;
  /** Number of groups in selection */
  groupCount: number;
  /** Whether selection contains only transactions */
  hasOnlyTransactions: boolean;
  /** Whether selection contains only groups */
  hasOnlyGroups: boolean;
  /** Whether selection is mixed (transactions + groups) */
  isMixed: boolean;
}

/**
 * Compute selection composition from items and selection state.
 * Use this to determine which bulk actions should be enabled.
 */
export function computeSelectionComposition(
  items: Array<{ id: string; type?: 'transaction' | 'group' }>,
  selectedIds: Set<string>
): SelectionComposition {
  let transactionCount = 0;
  let groupCount = 0;

  for (const item of items) {
    if (selectedIds.has(item.id)) {
      if (item.type === 'group') {
        groupCount++;
      } else {
        transactionCount++;
      }
    }
  }

  return {
    transactionCount,
    groupCount,
    hasOnlyTransactions: transactionCount > 0 && groupCount === 0,
    hasOnlyGroups: groupCount > 0 && transactionCount === 0,
    isMixed: transactionCount > 0 && groupCount > 0,
  };
}

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
  /** Callback to group selected transactions (Feature 028) */
  onGroup?: () => void;
  /** Whether grouping is allowed for current selection */
  canGroup?: boolean;
  /** Selection composition for mixed lists (Feature 028) */
  selectionComposition?: SelectionComposition;
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
  onMarkReimbursable,
  onMarkNotReimbursable,
  isProcessing = false,
  onGroup,
  canGroup = false,
  selectionComposition,
}: BulkActionsBarComponentProps) {
  // Determine if transaction-specific actions should be disabled
  // Groups don't have categories or tags, so these actions are only valid for transactions
  const hasGroups = (selectionComposition?.groupCount ?? 0) > 0;
  const transactionActionsDisabled = isProcessing || hasGroups;
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
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <div>
                  <Select onValueChange={handleCategoryChange} disabled={transactionActionsDisabled}>
                    <SelectTrigger className="w-[160px] h-9">
                      <FolderOpen className="h-4 w-4 mr-2" />
                      <SelectValue placeholder="Set category" />
                    </SelectTrigger>
                    <SelectContent>
                      {categories.map((category, index) => (
                        <SelectItem
                          key={safeDisplayString(category.id, `cat-${index}`, 'BulkActionsBar.category.id')}
                          value={safeDisplayString(category.id, `unknown-${index}`, 'BulkActionsBar.category.id')}
                        >
                          {safeDisplayString(category.name, 'Unknown', 'BulkActionsBar.category.name')}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </TooltipTrigger>
              {hasGroups && (
                <TooltipContent>
                  <p>Groups cannot be categorized. Select only transactions.</p>
                </TooltipContent>
              )}
            </Tooltip>
          </TooltipProvider>

          {/* Group Action (Feature 028) */}
          {onGroup && canGroup && selectedCount >= 2 && (
            <Button
              variant="outline"
              size="sm"
              className="gap-2"
              onClick={onGroup}
              disabled={isProcessing}
            >
              <Layers className="h-4 w-4" />
              Group ({selectedCount})
            </Button>
          )}

          {/* Reimbursability Actions */}
          {onMarkReimbursable && (
            <TooltipProvider>
              <Tooltip>
                <TooltipTrigger asChild>
                  <span>
                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-2 text-green-600 hover:text-green-700 hover:bg-green-50 dark:hover:bg-green-900/20"
                      onClick={onMarkReimbursable}
                      disabled={transactionActionsDisabled}
                    >
                      <CircleCheck className="h-4 w-4" />
                      Business
                    </Button>
                  </span>
                </TooltipTrigger>
                {hasGroups && (
                  <TooltipContent>
                    <p>Groups cannot be marked as reimbursable. Select only transactions.</p>
                  </TooltipContent>
                )}
              </Tooltip>
            </TooltipProvider>
          )}

          {onMarkNotReimbursable && (
            <TooltipProvider>
              <Tooltip>
                <TooltipTrigger asChild>
                  <span>
                    <Button
                      variant="outline"
                      size="sm"
                      className="gap-2 text-red-600 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-900/20"
                      onClick={onMarkNotReimbursable}
                      disabled={transactionActionsDisabled}
                    >
                      <CircleX className="h-4 w-4" />
                      Personal
                    </Button>
                  </span>
                </TooltipTrigger>
                {hasGroups && (
                  <TooltipContent>
                    <p>Groups cannot be marked as personal. Select only transactions.</p>
                  </TooltipContent>
                )}
              </Tooltip>
            </TooltipProvider>
          )}

          {/* Tag Action */}
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <span>
                  <DropdownMenu
                    open={isTagDropdownOpen && !transactionActionsDisabled}
                    onOpenChange={(open) => !transactionActionsDisabled && setIsTagDropdownOpen(open)}
                  >
                    <DropdownMenuTrigger asChild>
                      <Button
                        variant="outline"
                        size="sm"
                        className="gap-2"
                        disabled={transactionActionsDisabled}
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
                    availableTags.filter((tag) => safeDisplayString(tag, '', 'BulkActionsBar.tag.filter')).map((tag, index) => (
                      <label
                        key={safeDisplayString(tag, `tag-${index}`, 'BulkActionsBar.tag.key')}
                        className="flex items-center gap-2 cursor-pointer"
                      >
                        <Checkbox
                          checked={selectedTags.includes(tag)}
                          onCheckedChange={() => handleTagToggle(tag)}
                        />
                        <span className="text-sm">{safeDisplayString(tag, '', 'BulkActionsBar.tag.display')}</span>
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
                </span>
              </TooltipTrigger>
              {hasGroups && (
                <TooltipContent>
                  <p>Groups cannot have tags. Select only transactions.</p>
                </TooltipContent>
              )}
            </Tooltip>
          </TooltipProvider>

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
