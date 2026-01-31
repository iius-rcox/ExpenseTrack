/**
 * TransactionRow Component (T055)
 *
 * Individual transaction row with inline editing capabilities:
 * - Checkbox for multi-select operations
 * - Category dropdown for quick re-categorization
 * - Click-to-edit notes field
 * - Match status indicator with visual feedback
 * - Keyboard shortcuts (Enter to save, Escape to cancel)
 *
 * @see data-model.md Section 4.3 for TransactionRowProps specification
 */

import { useState, useRef, useEffect, useCallback, memo } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  Check,
  X,
  Pencil,
  Link2,
  Link2Off,
  Clock,
  ExternalLink,
  FileX,
} from 'lucide-react';
import { cn, safeDisplayString } from '@/lib/utils';
import { TableCell, TableRow } from '@/components/ui/table';
import { Checkbox } from '@/components/ui/checkbox';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { ExpenseBadge } from '@/components/predictions/expense-badge';
import { ReimbursabilityActions, MissingReceiptBadge } from '@/components/transactions/reimbursability-actions';
import type {
  TransactionView,
  TransactionMatchStatus,
} from '@/types/transaction';

/**
 * Props for the TransactionRow component.
 * Extended from TransactionRowProps with additional category data.
 */
interface TransactionRowComponentProps {
  /** Transaction data to display */
  transaction: TransactionView;
  /** Whether this row is selected */
  isSelected: boolean;
  /** Available categories for the dropdown */
  categories: { id: string; name: string }[];
  /** Selection callback (shiftKey for range selection) */
  onSelect: (shiftKey: boolean) => void;
  /** Edit callback for inline changes */
  onEdit: (updates: Partial<TransactionView>) => void;
  /** Click callback for navigation/detail view */
  onClick: () => void;
  /** Whether the row is currently being saved */
  isSaving?: boolean;
  /** Handler for confirming expense prediction (Feature 023) */
  onPredictionConfirm?: (predictionId: string) => void;
  /** Handler for rejecting expense prediction (Feature 023) */
  onPredictionReject?: (predictionId: string) => void;
  /** Whether a prediction action is processing */
  isPredictionProcessing?: boolean;
  /** Handler for marking transaction as reimbursable */
  onMarkReimbursable?: (transactionId: string) => void;
  /** Handler for marking transaction as not reimbursable */
  onMarkNotReimbursable?: (transactionId: string) => void;
  /** Handler for clearing manual reimbursability override */
  onClearReimbursabilityOverride?: (transactionId: string) => void;
}

/**
 * Match status configuration for visual display
 */
const MATCH_STATUS_CONFIG: Record<
  TransactionMatchStatus,
  { icon: React.ElementType; color: string; label: string }
> = {
  matched: {
    icon: Link2,
    color: 'text-green-600 bg-green-100 dark:bg-green-900/30',
    label: 'Matched',
  },
  pending: {
    icon: Clock,
    color: 'text-amber-600 bg-amber-100 dark:bg-amber-900/30',
    label: 'Pending Review',
  },
  unmatched: {
    icon: Link2Off,
    color: 'text-muted-foreground bg-muted',
    label: 'Unmatched',
  },
  manual: {
    icon: Check,
    color: 'text-blue-600 bg-blue-100 dark:bg-blue-900/30',
    label: 'Manual Match',
  },
  'missing-receipt': {
    icon: FileX,
    color: 'text-red-600 bg-red-100 dark:bg-red-900/30',
    label: 'Missing Receipt',
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
 * Individual transaction row with inline editing
 */
export const TransactionRow = memo(function TransactionRow({
  transaction,
  isSelected,
  categories,
  onSelect,
  onEdit,
  onClick,
  isSaving = false,
  onPredictionConfirm,
  onPredictionReject,
  isPredictionProcessing = false,
  onMarkReimbursable,
  onMarkNotReimbursable,
  onClearReimbursabilityOverride,
}: TransactionRowComponentProps) {
  // Editing state
  const [editingField, setEditingField] = useState<'notes' | null>(null);
  const [editValue, setEditValue] = useState('');
  const notesInputRef = useRef<HTMLInputElement>(null);

  // Focus input when editing starts
  useEffect(() => {
    if (editingField === 'notes' && notesInputRef.current) {
      notesInputRef.current.focus();
      notesInputRef.current.select();
    }
  }, [editingField]);

  // Start editing notes
  const handleStartEditNotes = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      // Use safeDisplayString to handle empty objects in cached data
      setEditValue(safeDisplayString(transaction.notes, '', 'TransactionRow.notes.edit'));
      setEditingField('notes');
    },
    [transaction.notes]
  );

  // Save notes edit
  const handleSaveNotes = useCallback(() => {
    const safeNotes = safeDisplayString(transaction.notes, '', 'TransactionRow.notes.save');
    if (editValue !== safeNotes) {
      onEdit({ notes: editValue });
    }
    setEditingField(null);
    setEditValue('');
  }, [editValue, transaction.notes, onEdit]);

  // Cancel editing
  const handleCancelEdit = useCallback(() => {
    setEditingField(null);
    setEditValue('');
  }, []);

  // Keyboard handling for edit mode
  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Enter') {
        e.preventDefault();
        handleSaveNotes();
      } else if (e.key === 'Escape') {
        e.preventDefault();
        handleCancelEdit();
      }
    },
    [handleSaveNotes, handleCancelEdit]
  );

  // Handle category change
  const handleCategoryChange = useCallback(
    (categoryId: string) => {
      const category = categories.find((c) => c.id === categoryId);
      if (category) {
        onEdit({
          categoryId,
          category: category.name,
        });
      }
    },
    [categories, onEdit]
  );

  // Handle checkbox click
  const handleCheckboxClick = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      onSelect(e.shiftKey);
    },
    [onSelect]
  );

  // Handle row click (for navigation)
  const handleRowClick = useCallback(
    (e: React.MouseEvent) => {
      // Don't trigger row click when clicking on interactive elements
      const target = e.target as HTMLElement;
      if (
        target.closest('button') ||
        target.closest('[role="checkbox"]') ||
        target.closest('[role="combobox"]') ||
        target.closest('input')
      ) {
        return;
      }
      onClick();
    },
    [onClick]
  );

  // Get match status config
  const matchConfig = MATCH_STATUS_CONFIG[transaction.matchStatus];
  const MatchIcon = matchConfig.icon;

  return (
    <TableRow
      className={cn(
        'cursor-pointer transition-colors',
        isSelected && 'bg-primary/5',
        isSaving && 'opacity-60 pointer-events-none'
      )}
      data-state={isSelected ? 'selected' : undefined}
      onClick={handleRowClick}
    >
      {/* Selection Checkbox - larger hit box for easier clicking */}
      <TableCell className="w-[50px] p-0">
        <div
          onClick={handleCheckboxClick}
          className="flex items-center justify-center w-full h-full min-h-[48px] px-3 cursor-pointer hover:bg-muted/50 transition-colors"
        >
          <Checkbox
            checked={isSelected}
            onCheckedChange={() => {}}
            aria-label={`Select transaction ${safeDisplayString(transaction.description, 'unknown', 'TransactionRow.checkbox.ariaLabel')}`}
          />
        </div>
      </TableCell>

      {/* Date */}
      <TableCell className="w-[100px] whitespace-nowrap">
        <span className="text-sm font-medium">
          {formatDate(transaction.date)}
        </span>
      </TableCell>

      {/* Description / Merchant */}
      <TableCell className="w-[180px]">
        <div className="space-y-0.5">
          <div className="font-medium truncate" title={safeDisplayString(transaction.description, '', 'TransactionRow.description.title')}>
            {safeDisplayString(transaction.merchant, '', 'TransactionRow.merchant.display') || safeDisplayString(transaction.description, '', 'TransactionRow.description.display')}
          </div>
          {safeDisplayString(transaction.merchant, '', 'TransactionRow.merchant.check') &&
           safeDisplayString(transaction.merchant, '', 'TransactionRow.merchant.compare') !== safeDisplayString(transaction.description, '', 'TransactionRow.description.compare') && (
            <div
              className="text-xs text-muted-foreground truncate"
              title={safeDisplayString(transaction.description, '', 'TransactionRow.description.subTitle')}
            >
              {safeDisplayString(transaction.description, '', 'TransactionRow.description.sub')}
            </div>
          )}
        </div>
      </TableCell>

      {/* Expense Prediction Badge / Reimbursability Status (Feature 023) */}
      <TableCell className="w-[140px]">
        {/* Show ExpenseBadge for pending predictions */}
        {transaction.prediction &&
          transaction.prediction.status === 'Pending' &&
          transaction.prediction.confidenceLevel !== 'Low' ? (
            <ExpenseBadge
              prediction={transaction.prediction}
              onConfirm={onPredictionConfirm}
              onReject={onPredictionReject}
              isProcessing={isPredictionProcessing}
              compact
            />
          ) : (
            /* Show ReimbursabilityActions for confirmed/rejected or no prediction */
            <ReimbursabilityActions
              transactionId={transaction.id}
              prediction={transaction.prediction}
              onMarkReimbursable={onMarkReimbursable}
              onMarkNotReimbursable={onMarkNotReimbursable}
              onClearOverride={onClearReimbursabilityOverride}
              isProcessing={isPredictionProcessing}
            />
          )}
      </TableCell>

      {/* Amount */}
      <TableCell className="w-[100px] text-right">
        <span
          className={cn(
            'font-semibold tabular-nums',
            transaction.amount >= 0 ? 'text-foreground' : 'text-green-600'
          )}
        >
          {formatAmount(transaction.amount)}
        </span>
      </TableCell>

      {/* Category (Editable Dropdown) */}
      <TableCell className="w-[150px]" onClick={(e) => e.stopPropagation()}>
        <Select
          value={safeDisplayString(transaction.categoryId, '', 'TransactionRow.categoryId.value') || undefined}
          onValueChange={handleCategoryChange}
          disabled={isSaving}
        >
          <SelectTrigger className="h-8 text-xs">
            <SelectValue placeholder="Select category" />
          </SelectTrigger>
          <SelectContent>
            {categories.map((category, index) => (
              <SelectItem key={safeDisplayString(category.id, `cat-${index}`, 'TransactionRow.category.key')} value={safeDisplayString(category.id, `unknown-${index}`, 'TransactionRow.category.value')}>
                {safeDisplayString(category.name, 'Unknown', 'TransactionRow.category.name')}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </TableCell>

      {/* Notes (Editable) */}
      <TableCell className="min-w-[150px] max-w-[200px]">
        <AnimatePresence mode="wait">
          {editingField === 'notes' ? (
            <motion.div
              key="edit"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="flex items-center gap-1"
              onClick={(e) => e.stopPropagation()}
            >
              <Input
                ref={notesInputRef}
                type="text"
                value={editValue}
                onChange={(e) => setEditValue(e.target.value)}
                onKeyDown={handleKeyDown}
                onBlur={handleSaveNotes}
                className="h-7 text-xs"
                placeholder="Add notes..."
                disabled={isSaving}
              />
              <Button
                size="icon"
                variant="ghost"
                className="h-6 w-6 text-green-600 hover:text-green-700"
                onClick={handleSaveNotes}
                disabled={isSaving}
              >
                <Check className="h-3 w-3" />
              </Button>
              <Button
                size="icon"
                variant="ghost"
                className="h-6 w-6 text-muted-foreground"
                onClick={handleCancelEdit}
                disabled={isSaving}
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
              className="group/notes flex items-center gap-1"
            >
              <span
                className={cn(
                  'text-xs truncate flex-1',
                  safeDisplayString(transaction.notes, '', 'TransactionRow.notes.check')
                    ? 'text-foreground'
                    : 'text-muted-foreground italic'
                )}
                title={safeDisplayString(transaction.notes, '', 'TransactionRow.notes.title') || 'No notes'}
              >
                {safeDisplayString(transaction.notes, '', 'TransactionRow.notes.display') || 'Add notes...'}
              </span>
              <Button
                size="icon"
                variant="ghost"
                className="h-6 w-6 opacity-0 group-hover/notes:opacity-100 transition-opacity"
                onClick={handleStartEditNotes}
                disabled={isSaving}
              >
                <Pencil className="h-3 w-3" />
              </Button>
            </motion.div>
          )}
        </AnimatePresence>
      </TableCell>

      {/* Match Status */}
      <TableCell className="w-[120px]">
        {/* Show Missing Receipt badge for Business expenses without matched receipts */}
        {transaction.prediction?.status === 'Confirmed' && transaction.matchStatus !== 'matched' && transaction.matchStatus !== 'manual' ? (
          <MissingReceiptBadge
            isReimbursable={true}
            isMatched={false}
          />
        ) : (
          <TooltipProvider>
            <Tooltip>
              <TooltipTrigger asChild>
                <Badge
                  variant="outline"
                  className={cn(
                    'gap-1 cursor-default',
                    matchConfig.color
                  )}
                >
                  <MatchIcon className="h-3 w-3" />
                  <span className="text-xs">{matchConfig.label}</span>
                </Badge>
              </TooltipTrigger>
              <TooltipContent side="top">
                {transaction.matchStatus === 'matched' && transaction.matchConfidence && (
                  <p>
                    Confidence: {Math.round(transaction.matchConfidence * 100)}%
                  </p>
                )}
                {transaction.matchStatus === 'unmatched' && (
                  <p>No receipt matched to this transaction</p>
                )}
                {transaction.matchStatus === 'pending' && (
                  <p>Receipt match pending review</p>
                )}
                {transaction.matchStatus === 'manual' && (
                  <p>Manually matched by user</p>
                )}
              </TooltipContent>
            </Tooltip>
          </TooltipProvider>
        )}
      </TableCell>

      {/* Tags */}
      <TableCell className="min-w-[100px]">
        {Array.isArray(transaction.tags) && transaction.tags.length > 0 ? (
          <div className="flex flex-wrap gap-1">
            {transaction.tags.slice(0, 2).map((tag, idx) => (
              <Badge
                key={safeDisplayString(tag, `tag-${idx}`, 'TransactionRow.tag.key')}
                variant="secondary"
                className="text-xs px-1.5 py-0"
              >
                {safeDisplayString(tag, '', 'TransactionRow.tag.display')}
              </Badge>
            ))}
            {transaction.tags.length > 2 && (
              <Badge variant="secondary" className="text-xs px-1.5 py-0">
                +{transaction.tags.length - 2}
              </Badge>
            )}
          </div>
        ) : (
          <span className="text-xs text-muted-foreground">—</span>
        )}
      </TableCell>

      {/* Actions */}
      <TableCell className="w-[50px]">
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                size="icon"
                variant="ghost"
                className="h-7 w-7"
                onClick={(e) => {
                  e.stopPropagation();
                  onClick();
                }}
              >
                <ExternalLink className="h-4 w-4" />
              </Button>
            </TooltipTrigger>
            <TooltipContent>View details</TooltipContent>
          </Tooltip>
        </TooltipProvider>
      </TableCell>
    </TableRow>
  );
});

/**
 * Skeleton loader for TransactionRow
 */
export function TransactionRowSkeleton() {
  return (
    <TableRow className="animate-pulse">
      <TableCell className="w-[40px]">
        <div className="h-4 w-4 rounded bg-muted" />
      </TableCell>
      <TableCell className="w-[100px]">
        <div className="h-4 w-16 rounded bg-muted" />
      </TableCell>
      <TableCell className="min-w-[200px]">
        <div className="space-y-1">
          <div className="h-4 w-32 rounded bg-muted" />
          <div className="h-3 w-48 rounded bg-muted" />
        </div>
      </TableCell>
      <TableCell className="w-[180px]">
        {/* Prediction badge skeleton (sometimes empty) */}
      </TableCell>
      <TableCell className="w-[100px]">
        <div className="h-4 w-16 rounded bg-muted ml-auto" />
      </TableCell>
      <TableCell className="w-[150px]">
        <div className="h-8 w-full rounded bg-muted" />
      </TableCell>
      <TableCell className="min-w-[150px]">
        <div className="h-4 w-24 rounded bg-muted" />
      </TableCell>
      <TableCell className="w-[120px]">
        <div className="h-5 w-20 rounded-full bg-muted" />
      </TableCell>
      <TableCell className="min-w-[100px]">
        <div className="h-4 w-12 rounded bg-muted" />
      </TableCell>
      <TableCell className="w-[50px]">
        <div className="h-7 w-7 rounded bg-muted" />
      </TableCell>
    </TableRow>
  );
}

export default TransactionRow;
