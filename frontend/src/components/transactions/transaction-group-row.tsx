/**
 * TransactionGroupRow Component (Feature 028)
 *
 * Expandable row for transaction groups with accordion-style reveal:
 * - Collapsed: checkbox, chevron, name, item count badge, date, amount, match status
 * - Expanded: minimal child rows with date, amount, description, remove button
 *
 * Uses framer-motion for smooth expand/collapse animations.
 *
 * @see pattern-row.tsx for accordion pattern reference
 * @see transaction-row.tsx for row structure reference
 */

import { useState, useCallback, useRef, useEffect, memo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  ChevronRight,
  ChevronDown,
  Layers,
  X,
  Pencil,
  Check,
  Link2,
  Link2Off,
  Clock,
  Calendar,
  Loader2,
  AlertCircle,
  RefreshCw,
  MoreVertical,
  Trash2,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { TableCell, TableRow } from '@/components/ui/table';
import { Checkbox } from '@/components/ui/checkbox';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
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
import { Calendar as CalendarPicker } from '@/components/ui/calendar';
import type {
  TransactionGroupRowProps,
  GroupMatchStatus,
} from '@/types/transaction';

/**
 * Match status configuration for visual display
 */
const GROUP_MATCH_STATUS_CONFIG: Record<
  GroupMatchStatus,
  { icon: React.ElementType; color: string; label: string }
> = {
  matched: {
    icon: Link2,
    color: 'text-green-600 bg-green-100 dark:bg-green-900/30',
    label: 'Matched',
  },
  proposed: {
    icon: Clock,
    color: 'text-amber-600 bg-amber-100 dark:bg-amber-900/30',
    label: 'Proposed',
  },
  unmatched: {
    icon: Link2Off,
    color: 'text-muted-foreground bg-muted',
    label: 'Unmatched',
  },
};

/**
 * Format currency amount for display
 */
function formatAmount(amount: number): string {
  const isNegative = amount < 0;
  const formatted = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(Math.abs(amount));

  return isNegative ? `-${formatted}` : formatted;
}

/**
 * Format date for display
 */
function formatDate(date: Date): string {
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
}

/**
 * DEFENSIVE HELPER: Safely convert any value to a displayable string.
 * Guards against React Error #301 where empty objects {} might be in cached data.
 */
function safeDisplayString(value: unknown, fallback = ''): string {
  if (value === null || value === undefined) return fallback;
  if (typeof value === 'object' && !Array.isArray(value) && !(value instanceof Date)) {
    const keys = Object.keys(value as object);
    if (keys.length === 0) {
      console.warn('[TransactionGroupRow] Empty object detected, using fallback');
      return fallback;
    }
    return fallback;
  }
  return String(value);
}

/**
 * Format short date for child rows
 */
function formatShortDate(date: Date): string {
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
  });
}

/**
 * Transaction group row with expandable details
 */
export const TransactionGroupRow = memo(function TransactionGroupRow({
  group,
  isSelected,
  isExpanded,
  onSelect,
  onToggleExpand,
  onEditName,
  onEditDate,
  onRemoveTransaction,
  onDeleteGroup,
  isProcessing = false,
  isLoadingChildren = false,
  childLoadError = null,
  onRetryLoadChildren,
}: TransactionGroupRowProps) {
  // Editing state
  const [isEditingName, setIsEditingName] = useState(false);
  const [editNameValue, setEditNameValue] = useState(group.name);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [isDatePickerOpen, setIsDatePickerOpen] = useState(false);
  const nameInputRef = useRef<HTMLInputElement>(null);

  // Focus input when editing starts
  useEffect(() => {
    if (isEditingName && nameInputRef.current) {
      nameInputRef.current.focus();
      nameInputRef.current.select();
    }
  }, [isEditingName]);

  // Start editing name
  const handleStartEditName = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      setEditNameValue(group.name);
      setIsEditingName(true);
    },
    [group.name]
  );

  // Save name edit
  const handleSaveName = useCallback(() => {
    if (editNameValue !== group.name && editNameValue.trim() && onEditName) {
      onEditName(editNameValue.trim());
    }
    setIsEditingName(false);
  }, [editNameValue, group.name, onEditName]);

  // Cancel editing
  const handleCancelEdit = useCallback(() => {
    setIsEditingName(false);
    setEditNameValue(group.name);
  }, [group.name]);

  // Keyboard handling for edit mode
  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Enter') {
        e.preventDefault();
        handleSaveName();
      } else if (e.key === 'Escape') {
        e.preventDefault();
        handleCancelEdit();
      }
    },
    [handleSaveName, handleCancelEdit]
  );

  // Handle checkbox click
  const handleCheckboxClick = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      onSelect(e.shiftKey);
    },
    [onSelect]
  );

  // Handle row click (toggle expand)
  const handleRowClick = useCallback(
    (e: React.MouseEvent) => {
      const target = e.target as HTMLElement;
      if (
        target.closest('button') ||
        target.closest('[role="checkbox"]') ||
        target.closest('input')
      ) {
        return;
      }
      onToggleExpand();
    },
    [onToggleExpand]
  );

  // Handle date selection
  const handleDateSelect = useCallback(
    (date: Date | undefined) => {
      if (date && onEditDate) {
        onEditDate(date);
        setIsDatePickerOpen(false);
      }
    },
    [onEditDate]
  );

  // Handle remove transaction
  const handleRemoveTransaction = useCallback(
    (transactionId: string, e: React.MouseEvent) => {
      e.stopPropagation();
      if (onRemoveTransaction) {
        onRemoveTransaction(transactionId);
      }
    },
    [onRemoveTransaction]
  );

  // Handle delete group confirmation
  const handleDeleteConfirm = useCallback(() => {
    if (onDeleteGroup) {
      onDeleteGroup();
    }
    setShowDeleteConfirm(false);
  }, [onDeleteGroup]);

  // Get match status config
  const matchConfig = GROUP_MATCH_STATUS_CONFIG[group.matchStatus];
  const MatchIcon = matchConfig.icon;

  // Calculate column count for expanded row (must match transaction grid)
  const columnCount = 10;

  return (
    <>
      {/* Main group row (collapsed view) */}
      <TableRow
        className={cn(
          'cursor-pointer transition-colors',
          isSelected && 'bg-primary/5',
          isExpanded && 'bg-accent/50',
          isProcessing && 'opacity-60 pointer-events-none'
        )}
        data-state={isSelected ? 'selected' : undefined}
        onClick={handleRowClick}
      >
        {/* Selection Checkbox */}
        <TableCell className="w-[40px]">
          <div className="flex items-center gap-2">
            <div onClick={handleCheckboxClick}>
              <Checkbox
                checked={isSelected}
                onCheckedChange={() => {}}
                aria-label={`Select group ${safeDisplayString(group.name, 'unnamed')}`}
              />
            </div>
            {/* Expand/Collapse Chevron */}
            <Button
              variant="ghost"
              size="icon"
              className="h-6 w-6"
              onClick={(e) => {
                e.stopPropagation();
                onToggleExpand();
              }}
              aria-expanded={isExpanded}
              aria-controls={`group-${group.id}-transactions`}
              aria-label={isExpanded ? `Collapse group ${safeDisplayString(group.name, 'unnamed')}` : `Expand group ${safeDisplayString(group.name, 'unnamed')}`}
            >
              {isExpanded ? (
                <ChevronDown className="h-4 w-4" />
              ) : (
                <ChevronRight className="h-4 w-4" />
              )}
            </Button>
          </div>
        </TableCell>

        {/* Date (with override indicator) */}
        <TableCell className="w-[100px] whitespace-nowrap">
          <div className="flex items-center gap-1">
            <span className="text-sm font-medium">
              {formatDate(group.displayDate)}
            </span>
            {group.isDateOverridden && (
              <TooltipProvider>
                <Tooltip>
                  <TooltipTrigger>
                    <Calendar className="h-3 w-3 text-muted-foreground" />
                  </TooltipTrigger>
                  <TooltipContent>Date manually overridden</TooltipContent>
                </Tooltip>
              </TooltipProvider>
            )}
          </div>
        </TableCell>

        {/* Group Name (Editable) */}
        <TableCell className="min-w-[200px] max-w-[300px]">
          <AnimatePresence mode="wait">
            {isEditingName ? (
              <motion.div
                key="edit"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{ opacity: 0 }}
                className="flex items-center gap-1"
                onClick={(e) => e.stopPropagation()}
              >
                <Input
                  ref={nameInputRef}
                  type="text"
                  value={editNameValue}
                  onChange={(e) => setEditNameValue(e.target.value)}
                  onKeyDown={handleKeyDown}
                  onBlur={handleSaveName}
                  className="h-7 text-sm"
                  disabled={isProcessing}
                />
                <Button
                  size="icon"
                  variant="ghost"
                  className="h-6 w-6 text-green-600"
                  onClick={handleSaveName}
                  aria-label="Save group name"
                >
                  <Check className="h-3 w-3" />
                </Button>
                <Button
                  size="icon"
                  variant="ghost"
                  className="h-6 w-6"
                  onClick={handleCancelEdit}
                  aria-label="Cancel editing"
                >
                  <X className="h-3 w-3" />
                </Button>
              </motion.div>
            ) : (
              <motion.div
                key="display"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{ opacity: 0 }}
                className="flex items-center gap-2 group/name"
              >
                <Layers className="h-4 w-4 text-muted-foreground" />
                <span className="font-medium truncate" title={safeDisplayString(group.name)}>
                  {safeDisplayString(group.name)}
                </span>
                <Badge variant="secondary" className="text-xs px-1.5">
                  {group.transactionCount} items
                </Badge>
                {onEditName && (
                  <Button
                    size="icon"
                    variant="ghost"
                    className="h-6 w-6 opacity-0 group-hover/name:opacity-100 transition-opacity focus:opacity-100"
                    onClick={handleStartEditName}
                    aria-label={`Edit name for group ${safeDisplayString(group.name, 'unnamed')}`}
                  >
                    <Pencil className="h-3 w-3" />
                  </Button>
                )}
              </motion.div>
            )}
          </AnimatePresence>
        </TableCell>

        {/* Empty cell for prediction column */}
        <TableCell className="w-[180px]">
          {/* Groups don't have individual predictions */}
        </TableCell>

        {/* Combined Amount */}
        <TableCell className="w-[100px] text-right">
          <span className="font-semibold tabular-nums">
            {formatAmount(group.combinedAmount)}
          </span>
        </TableCell>

        {/* Empty cell for category column */}
        <TableCell className="w-[150px]">
          {/* Groups don't have categories */}
        </TableCell>

        {/* Date Override Action */}
        <TableCell className="min-w-[150px]">
          {onEditDate && (
            <Popover open={isDatePickerOpen} onOpenChange={setIsDatePickerOpen}>
              <PopoverTrigger asChild>
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-7 text-xs text-muted-foreground hover:text-foreground"
                  onClick={(e) => e.stopPropagation()}
                  aria-label={`Override display date for group ${safeDisplayString(group.name, 'unnamed')}`}
                >
                  <Calendar className="h-3 w-3 mr-1" />
                  Override date
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-auto p-0" align="start">
                <CalendarPicker
                  mode="single"
                  selected={group.displayDate}
                  onSelect={handleDateSelect}
                  initialFocus
                />
              </PopoverContent>
            </Popover>
          )}
        </TableCell>

        {/* Match Status */}
        <TableCell className="w-[120px]">
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <Badge
                  variant="outline"
                  className={cn('gap-1 cursor-default', matchConfig.color)}
                >
                  <MatchIcon className="h-3 w-3" />
                  <span className="text-xs">{matchConfig.label}</span>
                </Badge>
              </TooltipTrigger>
              <TooltipContent side="top">
                {group.matchStatus === 'matched' && (
                  <p>Group matched to receipt</p>
                )}
                {group.matchStatus === 'unmatched' && (
                  <p>No receipt matched to this group</p>
                )}
                {group.matchStatus === 'proposed' && (
                  <p>Receipt match proposed, awaiting review</p>
                )}
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
        </TableCell>

        {/* Empty cell for tags column */}
        <TableCell className="min-w-[100px]">
          {/* Groups don't have tags */}
        </TableCell>

        {/* Actions Menu */}
        <TableCell className="w-[50px]">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                className="h-8 w-8"
                onClick={(e) => e.stopPropagation()}
                disabled={isProcessing}
              >
                <MoreVertical className="h-4 w-4" />
                <span className="sr-only">Group actions</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {onEditName && (
                <DropdownMenuItem onClick={handleStartEditName}>
                  <Pencil className="h-4 w-4 mr-2" />
                  Rename
                </DropdownMenuItem>
              )}
              {onEditDate && (
                <DropdownMenuItem onClick={() => setIsDatePickerOpen(true)}>
                  <Calendar className="h-4 w-4 mr-2" />
                  Change Date
                </DropdownMenuItem>
              )}
              {onDeleteGroup && (
                <>
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    className="text-destructive focus:text-destructive"
                    onClick={() => setShowDeleteConfirm(true)}
                  >
                    <Trash2 className="h-4 w-4 mr-2" />
                    Ungroup All
                  </DropdownMenuItem>
                </>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        </TableCell>
      </TableRow>

      {/* Expanded child transactions */}
      <AnimatePresence>
        {isExpanded && (
          <TableRow>
            <TableCell colSpan={columnCount} className="p-0 border-b">
              <motion.div
                initial={{ height: 0, opacity: 0 }}
                animate={{ height: 'auto', opacity: 1 }}
                exit={{ height: 0, opacity: 0 }}
                transition={{ duration: 0.2, ease: 'easeInOut' }}
                className="overflow-hidden"
              >
                <div
                  id={`group-${group.id}-transactions`}
                  role="region"
                  aria-label={`Transactions in group ${safeDisplayString(group.name, 'unnamed')}`}
                  className="bg-muted/30 px-4 py-2 ml-10"
                >
                  {/* Child transaction list */}
                  <div className="space-y-1" role="list">
                    {/* Error state */}
                    {childLoadError && (
                      <div className="flex items-center justify-center gap-3 py-4 text-sm text-destructive">
                        <AlertCircle className="h-4 w-4" />
                        <span>{childLoadError}</span>
                        {onRetryLoadChildren && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={onRetryLoadChildren}
                            className="gap-1 h-7"
                          >
                            <RefreshCw className="h-3 w-3" />
                            Retry
                          </Button>
                        )}
                      </div>
                    )}

                    {/* Loading state */}
                    {!childLoadError && isLoadingChildren && (
                      <div className="flex items-center justify-center gap-2 py-4 text-sm text-muted-foreground">
                        <Loader2 className="h-4 w-4 animate-spin" />
                        <span>Loading transactions...</span>
                      </div>
                    )}

                    {/* Loaded transactions */}
                    {!childLoadError && !isLoadingChildren && group.transactions && group.transactions.length > 0 ? (
                      group.transactions.map((tx, index) => (
                        <motion.div
                          key={tx.id}
                          role="listitem"
                          initial={{ opacity: 0, x: -10 }}
                          animate={{ opacity: 1, x: 0 }}
                          transition={{ delay: index * 0.05 }}
                          className="flex items-center justify-between py-1.5 px-2 rounded hover:bg-muted/50 group/child"
                        >
                          <div className="flex items-center gap-4 flex-1 min-w-0">
                            <span className="text-xs text-muted-foreground w-16 flex-shrink-0">
                              {formatShortDate(tx.date)}
                            </span>
                            <span className="text-sm truncate flex-1" title={safeDisplayString(tx.description)}>
                              {safeDisplayString(tx.description)}
                            </span>
                          </div>
                          <div className="flex items-center gap-2">
                            <span className="text-sm font-medium tabular-nums w-20 text-right">
                              {formatAmount(tx.amount)}
                            </span>
                            {onRemoveTransaction && group.transactionCount > 2 && (
                              <Button
                                variant="ghost"
                                size="icon"
                                className="h-6 w-6 opacity-0 group-hover/child:opacity-100 transition-opacity focus:opacity-100 text-destructive hover:text-destructive"
                                onClick={(e) => handleRemoveTransaction(tx.id, e)}
                                disabled={isProcessing}
                                aria-label={`Remove transaction ${tx.description} from group`}
                              >
                                <X className="h-3 w-3" />
                              </Button>
                            )}
                          </div>
                        </motion.div>
                      ))
                    ) : null}

                    {/* Empty state (no transactions loaded and not loading/error) */}
                    {!childLoadError && !isLoadingChildren && (!group.transactions || group.transactions.length === 0) && (
                      <div className="text-sm text-muted-foreground py-2 text-center">
                        No transactions found
                      </div>
                    )}
                  </div>

                  {/* Summary row */}
                  <div className="flex items-center justify-between mt-2 pt-2 border-t border-border/50">
                    <span className="text-xs text-muted-foreground">
                      {group.transactionCount} transactions combined
                    </span>
                    <span className="text-sm font-semibold">
                      Total: {formatAmount(group.combinedAmount)}
                    </span>
                  </div>
                </div>
              </motion.div>
            </TableCell>
          </TableRow>
        )}
      </AnimatePresence>

      {/* Delete Confirmation Dialog */}
      <AlertDialog open={showDeleteConfirm} onOpenChange={setShowDeleteConfirm}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle className="flex items-center gap-2">
              <Trash2 className="h-5 w-5 text-destructive" />
              Ungroup "{group.name}"?
            </AlertDialogTitle>
            <AlertDialogDescription>
              This will dissolve the group and return all {group.transactionCount} transactions
              to the main list. Any receipt match on this group will also be removed.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={handleDeleteConfirm}
            >
              Ungroup All
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
});

/**
 * Skeleton loader for TransactionGroupRow
 */
export function TransactionGroupRowSkeleton() {
  return (
    <TableRow className="animate-pulse">
      <TableCell className="w-[40px]">
        <div className="flex items-center gap-2">
          <div className="h-4 w-4 rounded bg-muted" />
          <div className="h-4 w-4 rounded bg-muted" />
        </div>
      </TableCell>
      <TableCell className="w-[100px]">
        <div className="h-4 w-16 rounded bg-muted" />
      </TableCell>
      <TableCell className="min-w-[200px]">
        <div className="flex items-center gap-2">
          <div className="h-4 w-4 rounded bg-muted" />
          <div className="h-4 w-32 rounded bg-muted" />
          <div className="h-5 w-16 rounded-full bg-muted" />
        </div>
      </TableCell>
      <TableCell className="w-[180px]" />
      <TableCell className="w-[100px]">
        <div className="h-4 w-16 rounded bg-muted ml-auto" />
      </TableCell>
      <TableCell className="w-[150px]" />
      <TableCell className="min-w-[150px]">
        <div className="h-6 w-24 rounded bg-muted" />
      </TableCell>
      <TableCell className="w-[120px]">
        <div className="h-5 w-20 rounded-full bg-muted" />
      </TableCell>
      <TableCell className="min-w-[100px]" />
      <TableCell className="w-[50px]" />
    </TableRow>
  );
}

export default TransactionGroupRow;
