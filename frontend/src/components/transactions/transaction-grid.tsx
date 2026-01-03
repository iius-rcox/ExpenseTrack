/**
 * TransactionGrid Component (T056)
 *
 * Virtualized transaction grid with:
 * - @tanstack/react-virtual for efficient rendering of large lists
 * - Sortable column headers
 * - Multi-selection support with shift-click range selection
 * - Integrated filter panel
 * - Empty and loading states
 *
 * @see data-model.md Section 4.4 for TransactionGridProps specification
 */

import { useRef, useCallback, useMemo, useState, useEffect } from 'react';
import { useVirtualizer } from '@tanstack/react-virtual';
import { motion, AnimatePresence } from 'framer-motion';
import {
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
  FileX2,
  Loader2,
  Calendar,
} from 'lucide-react';
import {
  Table,
  TableBody,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Checkbox } from '@/components/ui/checkbox';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { TransactionRow, TransactionRowSkeleton } from './transaction-row';
import { SwipeActionRow } from '@/components/mobile/swipe-action-row';
import { cn } from '@/lib/utils';
import type {
  TransactionView,
  TransactionSortConfig,
  TransactionSortField,
  TransactionSelectionState,
} from '@/types/transaction';

/**
 * Props for the TransactionGrid component
 */
interface TransactionGridProps {
  /** List of transactions to display */
  transactions: TransactionView[];
  /** Loading state */
  isLoading?: boolean;
  /** Current sort configuration */
  sort: TransactionSortConfig;
  /** Current selection state */
  selection: TransactionSelectionState;
  /** Available categories for the dropdown */
  categories: { id: string; name: string }[];
  /** Callback when sort changes */
  onSortChange: (sort: TransactionSortConfig) => void;
  /** Callback when selection changes */
  onSelectionChange: (selection: TransactionSelectionState) => void;
  /** Callback when a transaction is edited inline */
  onTransactionEdit: (id: string, updates: Partial<TransactionView>) => void;
  /** Callback when a transaction row is clicked */
  onTransactionClick?: (transaction: TransactionView) => void;
  /** Whether any transactions are currently being saved */
  savingIds?: Set<string>;
  /** Height of the scrollable container */
  containerHeight?: number;
  /** Handler for confirming expense prediction (Feature 023) */
  onPredictionConfirm?: (predictionId: string) => void;
  /** Handler for rejecting expense prediction (Feature 023) */
  onPredictionReject?: (predictionId: string) => void;
  /** Handler for marking transaction as reimbursable */
  onMarkReimbursable?: (transactionId: string) => void;
  /** Handler for marking transaction as not reimbursable */
  onMarkNotReimbursable?: (transactionId: string) => void;
  /** Handler for clearing manual reimbursability override */
  onClearReimbursabilityOverride?: (transactionId: string) => void;
  /** Whether a prediction/reimbursability action is processing */
  isPredictionProcessing?: boolean;
}

/**
 * Row height constant for virtualization
 */
const ROW_HEIGHT = 56;

/**
 * Empty state component
 */
function EmptyState({ hasFilters }: { hasFilters: boolean }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      className="flex flex-col items-center justify-center py-16 text-center"
    >
      <FileX2 className="h-12 w-12 text-muted-foreground/50 mb-4" />
      <h3 className="text-lg font-medium text-foreground">
        {hasFilters ? 'No matching transactions' : 'No transactions yet'}
      </h3>
      <p className="text-sm text-muted-foreground mt-1 max-w-[300px]">
        {hasFilters
          ? 'Try adjusting your filters or search terms'
          : 'Import a statement to get started'}
      </p>
    </motion.div>
  );
}

/**
 * Loading skeleton for the grid
 */
function LoadingSkeleton({ count = 5 }: { count?: number }) {
  return (
    <>
      {Array.from({ length: count }).map((_, i) => (
        <TransactionRowSkeleton key={i} />
      ))}
    </>
  );
}

/**
 * Mobile loading skeleton
 */
function MobileLoadingSkeleton({ count = 5 }: { count?: number }) {
  return (
    <div className="space-y-3">
      {Array.from({ length: count }).map((_, i) => (
        <Card key={i} className="animate-pulse">
          <CardContent className="p-4">
            <div className="flex justify-between items-start mb-2">
              <div className="h-5 w-32 bg-muted rounded" />
              <div className="h-5 w-20 bg-muted rounded" />
            </div>
            <div className="h-4 w-24 bg-muted rounded mb-2" />
            <div className="h-4 w-16 bg-muted rounded" />
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

/**
 * Mobile transaction card component
 */
interface MobileTransactionCardProps {
  transaction: TransactionView;
  isSelected: boolean;
  onSelect: () => void;
  onClick?: () => void;
  onEdit?: () => void;
  onDelete?: () => void;
}

function MobileTransactionCard({
  transaction,
  isSelected,
  onSelect,
  onClick,
  onEdit,
  onDelete,
}: MobileTransactionCardProps) {
  const formattedDate = transaction.date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
  });

  const formattedAmount = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(Math.abs(transaction.amount));

  const isDebit = transaction.amount < 0;

  return (
    <SwipeActionRow
      leftActions={onEdit ? [{ id: 'edit', type: 'edit', onAction: onEdit }] : undefined}
      rightActions={onDelete ? [{ id: 'delete', type: 'delete', onAction: onDelete }] : undefined}
    >
      <Card
        className={cn(
          'cursor-pointer transition-colors',
          isSelected && 'ring-2 ring-primary bg-primary/5'
        )}
        onClick={onClick}
      >
        <CardContent className="p-4">
          <div className="flex justify-between items-start">
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2">
                <Checkbox
                  checked={isSelected}
                  onCheckedChange={(checked) => {
                    if (checked) onSelect();
                  }}
                  onClick={(e) => e.stopPropagation()}
                  className="h-5 w-5"
                />
                <span className="font-medium truncate">
                  {transaction.merchant || transaction.description}
                </span>
              </div>
              <div className="flex items-center gap-2 mt-1 text-sm text-muted-foreground">
                <Calendar className="h-3.5 w-3.5" />
                <span>{formattedDate}</span>
              </div>
            </div>
            <div className={cn(
              'text-lg font-semibold tabular-nums',
              isDebit ? 'text-foreground' : 'text-green-600'
            )}>
              {isDebit ? '-' : '+'}{formattedAmount}
            </div>
          </div>

          {/* Category & Tags */}
          <div className="flex items-center gap-2 mt-3 flex-wrap">
            {transaction.category && (
              <Badge variant="secondary" className="text-xs">
                {transaction.category}
              </Badge>
            )}
            {transaction.matchStatus === 'matched' && (
              <Badge variant="outline" className="text-xs text-green-600 border-green-600">
                Matched
              </Badge>
            )}
            {transaction.matchStatus === 'pending' && (
              <Badge variant="outline" className="text-xs text-amber-600 border-amber-600">
                Pending
              </Badge>
            )}
          </div>
        </CardContent>
      </Card>
    </SwipeActionRow>
  );
}

/**
 * Virtualized transaction grid component
 */
export function TransactionGrid({
  transactions,
  isLoading = false,
  sort,
  selection,
  categories,
  onSortChange,
  onSelectionChange,
  onTransactionEdit,
  onTransactionClick,
  savingIds = new Set(),
  containerHeight = 600,
  onPredictionConfirm,
  onPredictionReject,
  onMarkReimbursable,
  onMarkNotReimbursable,
  onClearReimbursabilityOverride,
  isPredictionProcessing = false,
}: TransactionGridProps) {
  const parentRef = useRef<HTMLDivElement>(null);
  const [isMobile, setIsMobile] = useState(false);

  // Detect mobile viewport
  useEffect(() => {
    const checkMobile = () => setIsMobile(window.innerWidth < 768);
    checkMobile();
    window.addEventListener('resize', checkMobile);
    return () => window.removeEventListener('resize', checkMobile);
  }, []);

  // Virtual row count
  const rowCount = transactions.length;

  // Setup virtualizer
  const virtualizer = useVirtualizer({
    count: rowCount,
    getScrollElement: () => parentRef.current,
    estimateSize: () => ROW_HEIGHT,
    overscan: 5, // Render 5 extra rows above/below viewport
  });

  // Get visible range
  const virtualRows = virtualizer.getVirtualItems();
  const totalHeight = virtualizer.getTotalSize();

  // Handle sort column click
  const handleSortClick = useCallback(
    (field: TransactionSortField) => {
      if (sort.field === field) {
        // Toggle direction
        onSortChange({
          field,
          direction: sort.direction === 'asc' ? 'desc' : 'asc',
        });
      } else {
        // New field, default to descending
        onSortChange({
          field,
          direction: 'desc',
        });
      }
    },
    [sort, onSortChange]
  );

  // Handle select all toggle
  const handleSelectAll = useCallback(() => {
    if (selection.isSelectAll) {
      // Deselect all
      onSelectionChange({
        selectedIds: new Set(),
        lastSelectedId: null,
        isSelectAll: false,
      });
    } else {
      // Select all
      onSelectionChange({
        selectedIds: new Set(transactions.map((t) => t.id)),
        lastSelectedId: null,
        isSelectAll: true,
      });
    }
  }, [selection.isSelectAll, transactions, onSelectionChange]);

  // Handle row selection
  const handleRowSelect = useCallback(
    (transactionId: string, shiftKey: boolean) => {
      const newSelection = new Set(selection.selectedIds);

      if (shiftKey && selection.lastSelectedId) {
        // Range selection
        const lastIndex = transactions.findIndex(
          (t) => t.id === selection.lastSelectedId
        );
        const currentIndex = transactions.findIndex(
          (t) => t.id === transactionId
        );

        if (lastIndex !== -1 && currentIndex !== -1) {
          const start = Math.min(lastIndex, currentIndex);
          const end = Math.max(lastIndex, currentIndex);

          for (let i = start; i <= end; i++) {
            newSelection.add(transactions[i].id);
          }
        }
      } else {
        // Toggle single selection
        if (newSelection.has(transactionId)) {
          newSelection.delete(transactionId);
        } else {
          newSelection.add(transactionId);
        }
      }

      onSelectionChange({
        selectedIds: newSelection,
        lastSelectedId: transactionId,
        isSelectAll: newSelection.size === transactions.length,
      });
    },
    [selection, transactions, onSelectionChange]
  );

  // Handle row edit
  const handleRowEdit = useCallback(
    (transactionId: string) => (updates: Partial<TransactionView>) => {
      onTransactionEdit(transactionId, updates);
    },
    [onTransactionEdit]
  );

  // Handle row click
  const handleRowClick = useCallback(
    (transaction: TransactionView) => () => {
      onTransactionClick?.(transaction);
    },
    [onTransactionClick]
  );

  // Render sort indicator
  const renderSortIndicator = (field: TransactionSortField) => {
    if (sort.field !== field) {
      return <ArrowUpDown className="h-3.5 w-3.5 ml-1 opacity-50" />;
    }
    return sort.direction === 'asc' ? (
      <ArrowUp className="h-3.5 w-3.5 ml-1" />
    ) : (
      <ArrowDown className="h-3.5 w-3.5 ml-1" />
    );
  };

  // Check if any filters are active
  const hasActiveFilters = useMemo(() => {
    return selection.selectedIds.size > 0;
  }, [selection.selectedIds.size]);

  // Indeterminate state for select-all checkbox
  const isIndeterminate =
    selection.selectedIds.size > 0 &&
    selection.selectedIds.size < transactions.length;

  // Mobile View
  if (isMobile) {
    return (
      <div className="space-y-3">
        {/* Mobile Header with Sort */}
        <div className="flex items-center justify-between px-1">
          <div className="flex items-center gap-2">
            <Checkbox
              checked={selection.isSelectAll}
              onCheckedChange={handleSelectAll}
              aria-label="Select all transactions"
              className="h-5 w-5"
            />
            <span className="text-sm text-muted-foreground">
              {selection.selectedIds.size > 0
                ? `${selection.selectedIds.size} selected`
                : `${transactions.length} transactions`}
            </span>
          </div>
          <Button
            variant="ghost"
            size="sm"
            className="h-9"
            onClick={() => handleSortClick(sort.field)}
          >
            Sort by {sort.field}
            {sort.direction === 'asc' ? (
              <ArrowUp className="h-4 w-4 ml-1" />
            ) : (
              <ArrowDown className="h-4 w-4 ml-1" />
            )}
          </Button>
        </div>

        {/* Mobile Transaction List */}
        {isLoading ? (
          <MobileLoadingSkeleton count={5} />
        ) : transactions.length === 0 ? (
          <EmptyState hasFilters={hasActiveFilters} />
        ) : (
          <div className="space-y-2">
            {transactions.map((transaction) => (
              <MobileTransactionCard
                key={transaction.id}
                transaction={transaction}
                isSelected={selection.selectedIds.has(transaction.id)}
                onSelect={() => handleRowSelect(transaction.id, false)}
                onClick={() => onTransactionClick?.(transaction)}
                onEdit={() => onTransactionEdit(transaction.id, {})}
              />
            ))}
          </div>
        )}
      </div>
    );
  }

  // Desktop/Tablet View
  return (
    <div className="space-y-0">
      {/* Table Container */}
      <div className="rounded-md border">
        <Table>
          {/* Table Header (Fixed) */}
          <TableHeader>
            <TableRow className="hover:bg-transparent">
              {/* Select All Checkbox */}
              <TableHead className="w-[40px]">
                <Checkbox
                  checked={selection.isSelectAll}
                  onCheckedChange={handleSelectAll}
                  aria-label="Select all transactions"
                  ref={(el) => {
                    if (el) {
                      // Handle indeterminate state
                      (el as unknown as HTMLInputElement).indeterminate = isIndeterminate;
                    }
                  }}
                />
              </TableHead>

              {/* Date Column */}
              <TableHead className="w-[100px]">
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-8 px-2 -ml-2 font-medium"
                  onClick={() => handleSortClick('date')}
                >
                  Date
                  {renderSortIndicator('date')}
                </Button>
              </TableHead>

              {/* Description Column */}
              <TableHead className="min-w-[200px]">
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-8 px-2 -ml-2 font-medium"
                  onClick={() => handleSortClick('merchant')}
                >
                  Description
                  {renderSortIndicator('merchant')}
                </Button>
              </TableHead>

              {/* Expense/Reimbursability Column */}
              <TableHead className="w-[180px]">Expense</TableHead>

              {/* Amount Column */}
              <TableHead className="w-[100px] text-right">
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-8 px-2 -mr-2 font-medium ml-auto"
                  onClick={() => handleSortClick('amount')}
                >
                  Amount
                  {renderSortIndicator('amount')}
                </Button>
              </TableHead>

              {/* Category Column */}
              <TableHead className="w-[150px]">
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-8 px-2 -ml-2 font-medium"
                  onClick={() => handleSortClick('category')}
                >
                  Category
                  {renderSortIndicator('category')}
                </Button>
              </TableHead>

              {/* Notes Column */}
              <TableHead className="min-w-[150px]">Notes</TableHead>

              {/* Status Column */}
              <TableHead className="w-[120px]">Status</TableHead>

              {/* Tags Column */}
              <TableHead className="min-w-[100px]">Tags</TableHead>

              {/* Actions Column */}
              <TableHead className="w-[50px]" />
            </TableRow>
          </TableHeader>
        </Table>

        {/* Virtualized Body */}
        <div
          ref={parentRef}
          style={{ height: containerHeight, overflow: 'auto' }}
          className="relative"
        >
          {isLoading ? (
            <Table>
              <TableBody>
                <LoadingSkeleton count={8} />
              </TableBody>
            </Table>
          ) : transactions.length === 0 ? (
            <EmptyState hasFilters={hasActiveFilters} />
          ) : (
            <div
              style={{
                height: totalHeight,
                width: '100%',
                position: 'relative',
              }}
            >
              <Table>
                <TableBody>
                  {virtualRows.map((virtualRow) => {
                    const transaction = transactions[virtualRow.index];
                    return (
                      <TransactionRow
                        key={transaction.id}
                        transaction={transaction}
                        isSelected={selection.selectedIds.has(transaction.id)}
                        categories={categories}
                        onSelect={(shiftKey) =>
                          handleRowSelect(transaction.id, shiftKey)
                        }
                        onEdit={handleRowEdit(transaction.id)}
                        onClick={handleRowClick(transaction)}
                        isSaving={savingIds.has(transaction.id)}
                        onPredictionConfirm={onPredictionConfirm}
                        onPredictionReject={onPredictionReject}
                        onMarkReimbursable={onMarkReimbursable}
                        onMarkNotReimbursable={onMarkNotReimbursable}
                        onClearReimbursabilityOverride={onClearReimbursabilityOverride}
                        isPredictionProcessing={isPredictionProcessing}
                      />
                    );
                  })}
                </TableBody>
              </Table>
            </div>
          )}
        </div>
      </div>

      {/* Loading Overlay */}
      <AnimatePresence>
        {isLoading && transactions.length > 0 && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="absolute inset-0 bg-background/50 flex items-center justify-center"
          >
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  );
}

export default TransactionGrid;
