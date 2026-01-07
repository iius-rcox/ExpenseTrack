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

import { useRef, useCallback, useMemo, useState, useEffect, useLayoutEffect } from 'react';
import { useVirtualizer } from '@tanstack/react-virtual';
import { motion, AnimatePresence } from 'framer-motion';
import {
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
  FileX2,
  Loader2,
  Calendar,
  Layers,
  ChevronDown,
  ChevronRight,
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
import { cn, safeDisplayString, safeDisplayNumber } from '@/lib/utils';
import type {
  TransactionView,
  TransactionListItem,
  TransactionGroupView,
  TransactionSortConfig,
  TransactionSortField,
  TransactionSelectionState,
} from '@/types/transaction';
import { TransactionGroupRow } from './transaction-group-row';

/**
 * Props for the TransactionGrid component
 */
interface TransactionGridProps {
  /** List of items to display (transactions and/or groups) */
  items: TransactionListItem[];
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
  // =========================================================================
  // Group-specific handlers (Feature 028)
  // =========================================================================
  /** Set of expanded group IDs */
  expandedGroupIds?: Set<string>;
  /** Callback when group expansion is toggled */
  onGroupToggle?: (groupId: string) => void;
  /** Callback when group name is edited */
  onGroupEditName?: (groupId: string, name: string) => void;
  /** Callback when group date is edited */
  onGroupEditDate?: (groupId: string, date: Date) => void;
  /** Callback when transaction is removed from group */
  onGroupRemoveTransaction?: (groupId: string, transactionId: string) => void;
  /** Callback when group is deleted/dissolved */
  onGroupDelete?: (groupId: string) => void;
  /** Whether group operations are processing */
  isGroupProcessing?: boolean;
}

/**
 * Legacy prop name support - accepts either 'items' or 'transactions'
 */
type TransactionGridPropsWithLegacy = TransactionGridProps | (Omit<TransactionGridProps, 'items'> & {
  transactions: TransactionView[];
});

/**
 * Row height constants for virtualization
 */
const ROW_HEIGHT = 56;
const CHILD_ROW_HEIGHT = 36; // Height of each child transaction in expanded group
const GROUP_HEADER_PADDING = 48; // Extra padding for group summary row when expanded

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
                  {safeDisplayString(transaction.merchant) || safeDisplayString(transaction.description)}
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
            {safeDisplayString(transaction.category) && (
              <Badge variant="secondary" className="text-xs">
                {safeDisplayString(transaction.category)}
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
 * Helper to check if an item is a group.
 * Defensive check verifies required group properties exist at runtime.
 */
function isGroup(item: TransactionListItem): item is TransactionGroupView {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const itemAny = item as any; // Capture before type narrowing for diagnostics

  const hasType = item.type === 'group';
  const hasTransactionCount = 'transactionCount' in item;
  const hasCombinedAmount = 'combinedAmount' in item;
  const hasDisplayDate = 'displayDate' in item;

  // DIAGNOSTIC: Log when an item has type='group' but fails other checks
  if (hasType && (!hasTransactionCount || !hasCombinedAmount || !hasDisplayDate)) {
    console.error('[TransactionGrid] ⚠️ MALFORMED GROUP DETECTED - item has type="group" but missing required properties:', {
      id: itemAny.id,
      type: itemAny.type,
      hasTransactionCount,
      hasCombinedAmount,
      hasDisplayDate,
      itemKeys: Object.keys(item),
      item,
    });
  }

  // DIAGNOSTIC: Log when item.type is an object (could be empty object)
  if (typeof itemAny.type === 'object') {
    console.error('[TransactionGrid] ⚠️ item.type IS AN OBJECT (not string):', {
      id: itemAny.id,
      typeValue: itemAny.type,
      typeType: typeof itemAny.type,
      isEmptyObject: typeof itemAny.type === 'object' && Object.keys(itemAny.type as object).length === 0,
    });
  }

  return hasType && hasTransactionCount && hasCombinedAmount && hasDisplayDate;
}

/**
 * Format amount for display in mobile group cards
 */
function formatAmount(amount: number): string {
  const formatted = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(Math.abs(amount));
  return amount < 0 ? `-${formatted}` : formatted;
}

/**
 * Format date for display in mobile group cards (short format)
 */
function formatShortDate(date: Date): string {
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
  });
}

/**
 * Virtualized transaction grid component
 *
 * Supports rendering both transactions and transaction groups in a single list.
 * Groups are rendered as expandable accordion rows.
 */
// DIAGNOSTIC: Render counter for TransactionGrid
let gridRenderCount = 0;

export function TransactionGrid(props: TransactionGridPropsWithLegacy) {
  gridRenderCount++;
  const gridRender = gridRenderCount;
  console.log(`[TransactionGrid] Starting render #${gridRender}`, {
    hasItems: 'items' in props,
    itemCount: ('items' in props ? props.items : props.transactions)?.length ?? 0,
    isLoading: props.isLoading,
    categoriesCount: props.categories?.length ?? 0,
  });

  // Support legacy 'transactions' prop name for backwards compatibility
  const items: TransactionListItem[] = 'items' in props
    ? props.items
    : (props.transactions || []).map(t => ({ ...t, type: 'transaction' as const }));

  const {
    isLoading = false,
    sort: rawSort,
    selection: rawSelection,
    categories: rawCategories,
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
    // Group-specific props (Feature 028)
    expandedGroupIds: rawExpandedGroupIds = new Set(),
    onGroupToggle,
    onGroupEditName,
    onGroupEditDate,
    onGroupRemoveTransaction,
    onGroupDelete,
    isGroupProcessing = false,
  } = props as TransactionGridProps;

  // DEFENSIVE: Validate and sanitize props to prevent React Error #301
  // IMPORTANT: Use useMemo to maintain stable references and prevent infinite re-renders!
  // Sets can become {} when serialized, so ensure they're proper Sets
  const selection = useMemo(() => ({
    selectedIds: rawSelection?.selectedIds instanceof Set
      ? rawSelection.selectedIds
      : new Set<string>(),
    lastSelectedId: typeof rawSelection?.lastSelectedId === 'string'
      ? rawSelection.lastSelectedId
      : null,
    isSelectAll: rawSelection?.isSelectAll === true,
  }), [rawSelection?.selectedIds, rawSelection?.lastSelectedId, rawSelection?.isSelectAll]);

  // Validate sort config - empty objects are truthy so explicit type checks needed
  const sort = useMemo(() => ({
    field: (typeof rawSort?.field === 'string' && ['date', 'amount', 'merchant', 'category'].includes(rawSort.field))
      ? rawSort.field as 'date' | 'amount' | 'merchant' | 'category'
      : 'date' as const,
    direction: (typeof rawSort?.direction === 'string' && (rawSort.direction === 'asc' || rawSort.direction === 'desc'))
      ? rawSort.direction
      : 'desc' as const,
  }), [rawSort?.field, rawSort?.direction]);

  // Validate categories array - filter out any malformed entries
  const categories = useMemo(() => Array.isArray(rawCategories)
    ? rawCategories.filter((cat): cat is { id: string; name: string } =>
        cat !== null &&
        typeof cat === 'object' &&
        typeof cat.id === 'string' &&
        typeof cat.name === 'string' &&
        cat.id.length > 0
      )
    : []
  , [rawCategories]);

  // Validate expandedGroupIds
  const expandedGroupIds = useMemo(() => rawExpandedGroupIds instanceof Set
    ? rawExpandedGroupIds
    : new Set<string>()
  , [rawExpandedGroupIds]);

  // DIAGNOSTIC: Log if we had to fix any props
  if (rawSelection?.selectedIds && !(rawSelection.selectedIds instanceof Set)) {
    console.error(`[TransactionGrid] ⚠️ FIXED selection.selectedIds - was not a Set:`, rawSelection.selectedIds);
  }
  if (rawSort && (typeof rawSort.field !== 'string' || typeof rawSort.direction !== 'string')) {
    console.error(`[TransactionGrid] ⚠️ FIXED sort config:`, rawSort);
  }
  if (rawCategories && rawCategories.length !== categories.length) {
    console.error(`[TransactionGrid] ⚠️ Filtered ${rawCategories.length - categories.length} malformed categories`);
  }

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
  const rowCount = items.length;

  // Calculate dynamic row height based on whether group is expanded
  // IMPORTANT: Must be memoized to prevent infinite re-renders in useVirtualizer
  const getRowHeight = useCallback(
    (index: number): number => {
      const item = items[index];
      if (!item) return ROW_HEIGHT;

      // Check if this is an expanded group
      if (isGroup(item) && expandedGroupIds?.has(item.id)) {
        // Base height + child rows + summary padding
        const childCount = item.transactions?.length ?? item.transactionCount;
        return ROW_HEIGHT + (childCount * CHILD_ROW_HEIGHT) + GROUP_HEADER_PADDING;
      }

      return ROW_HEIGHT;
    },
    [items, expandedGroupIds]
  );

  // IMPORTANT: Memoize getScrollElement to prevent infinite re-renders
  // Inline functions in useVirtualizer config cause it to reconfigure on every render
  const getScrollElement = useCallback(() => parentRef.current, []);

  // IMPORTANT: Memoize getItemKey to prevent infinite re-renders
  // This function generates stable keys for virtualized rows
  const getItemKey = useCallback(
    (index: number) => {
      const item = items[index];
      if (!item) return index;
      const isExpanded = isGroup(item) && expandedGroupIds?.has(item.id);
      return `${item.id}-${isExpanded}`;
    },
    [items, expandedGroupIds]
  );

  // Setup virtualizer with dynamic row heights
  // All config functions MUST be memoized to prevent infinite loop
  const virtualizer = useVirtualizer({
    count: rowCount,
    getScrollElement,
    estimateSize: getRowHeight,
    overscan: 5, // Render 5 extra rows above/below viewport
    getItemKey,
  });

  // Get visible range
  const virtualRows = virtualizer.getVirtualItems();
  const totalHeight = virtualizer.getTotalSize();

  // Recalculate measurements when expanded groups change
  // Using useLayoutEffect to measure synchronously before paint,
  // preventing visual flickering during expand/collapse
  // NOTE: Do NOT include virtualizer in deps - it changes on every render
  // and calling measure() would create an infinite loop
  useLayoutEffect(() => {
    virtualizer.measure();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [expandedGroupIds]);

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
        selectedIds: new Set(items.map((t) => t.id)),
        lastSelectedId: null,
        isSelectAll: true,
      });
    }
  }, [selection.isSelectAll, items, onSelectionChange]);

  // Handle row selection
  const handleRowSelect = useCallback(
    (transactionId: string, shiftKey: boolean) => {
      const newSelection = new Set(selection.selectedIds);

      if (shiftKey && selection.lastSelectedId) {
        // Range selection
        const lastIndex = items.findIndex(
          (t) => t.id === selection.lastSelectedId
        );
        const currentIndex = items.findIndex(
          (t) => t.id === transactionId
        );

        if (lastIndex !== -1 && currentIndex !== -1) {
          const start = Math.min(lastIndex, currentIndex);
          const end = Math.max(lastIndex, currentIndex);

          for (let i = start; i <= end; i++) {
            newSelection.add(items[i].id);
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
        isSelectAll: newSelection.size === items.length,
      });
    },
    [selection, items, onSelectionChange]
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
    selection.selectedIds.size < items.length;

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
                : `${items.length} items`}
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
        ) : items.length === 0 ? (
          <EmptyState hasFilters={hasActiveFilters} />
        ) : (
          <div className="space-y-2">
            {items.map((item) => {
              if (isGroup(item)) {
                const isGroupExpanded = expandedGroupIds?.has(item.id) ?? false;
                const isGroupSelected = selection.selectedIds.has(item.id);
                return (
                  <Card
                    key={item.id}
                    className={cn(
                      "overflow-hidden transition-colors",
                      isGroupSelected && "ring-2 ring-primary bg-primary/5"
                    )}
                  >
                    {/* Group Header */}
                    <div
                      className="p-4 flex items-center gap-3 cursor-pointer"
                      onClick={() => onGroupToggle?.(item.id)}
                    >
                      <Checkbox
                        checked={isGroupSelected}
                        onCheckedChange={() => handleRowSelect(item.id, false)}
                        onClick={(e) => e.stopPropagation()}
                        aria-label={`Select group ${safeDisplayString(item.name, 'unnamed')}`}
                      />
                      <Layers className="h-4 w-4 text-muted-foreground flex-shrink-0" />
                      <div className="flex-1 min-w-0">
                        <div className="font-medium truncate">{safeDisplayString(item.name)}</div>
                        <div className="text-xs text-muted-foreground">
                          {safeDisplayNumber(item.transactionCount, 0, 'TransactionGrid.mobile.transactionCount')} items • {formatAmount(safeDisplayNumber(item.combinedAmount, 0, 'TransactionGrid.mobile.combinedAmount'))}
                        </div>
                      </div>
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-8 w-8 flex-shrink-0"
                        aria-expanded={isGroupExpanded}
                        aria-label={isGroupExpanded ? 'Collapse group' : 'Expand group'}
                      >
                        {isGroupExpanded ? (
                          <ChevronDown className="h-4 w-4" />
                        ) : (
                          <ChevronRight className="h-4 w-4" />
                        )}
                      </Button>
                    </div>

                    {/* Expanded Child Transactions */}
                    <AnimatePresence>
                      {isGroupExpanded && (
                        <motion.div
                          initial={{ height: 0, opacity: 0 }}
                          animate={{ height: 'auto', opacity: 1 }}
                          exit={{ height: 0, opacity: 0 }}
                          className="border-t bg-muted/30"
                        >
                          <div className="p-3 space-y-2">
                            {isGroupProcessing ? (
                              <div className="flex items-center justify-center gap-2 py-3 text-sm text-muted-foreground">
                                <Loader2 className="h-4 w-4 animate-spin" />
                                <span>Loading...</span>
                              </div>
                            ) : item.transactions && item.transactions.length > 0 ? (
                              item.transactions.map((tx) => (
                                <div key={tx.id} className="flex justify-between text-sm py-1.5 px-2 rounded bg-background/50">
                                  <div className="flex-1 min-w-0">
                                    <div className="truncate">{safeDisplayString(tx.description)}</div>
                                    <div className="text-xs text-muted-foreground">
                                      {formatShortDate(tx.date)}
                                    </div>
                                  </div>
                                  <div className="font-medium tabular-nums">
                                    {formatAmount(tx.amount)}
                                  </div>
                                </div>
                              ))
                            ) : (
                              <div className="text-sm text-muted-foreground text-center py-2">
                                No transactions loaded
                              </div>
                            )}
                          </div>
                        </motion.div>
                      )}
                    </AnimatePresence>
                  </Card>
                );
              }
              const transaction = item as TransactionView;
              return (
                <MobileTransactionCard
                  key={transaction.id}
                  transaction={transaction}
                  isSelected={selection.selectedIds.has(transaction.id)}
                  onSelect={() => handleRowSelect(transaction.id, false)}
                  onClick={() => onTransactionClick?.(transaction)}
                  onEdit={() => onTransactionEdit(transaction.id, {})}
                />
              );
            })}
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
          ) : items.length === 0 ? (
            <EmptyState hasFilters={hasActiveFilters} />
          ) : (
            <div
              style={{
                height: totalHeight,
                width: '100%',
                position: 'relative',
              }}
            >
              {virtualRows.map((virtualRow) => {
                const item = items[virtualRow.index];

                // DIAGNOSTIC: Check for undefined or empty object items
                if (!item) {
                  console.error(`[TransactionGrid] ⚠️ UNDEFINED ITEM at index ${virtualRow.index}`, {
                    itemsLength: items.length,
                    virtualRowIndex: virtualRow.index,
                  });
                  return null; // Skip rendering to prevent crash
                }
                if (typeof item === 'object' && Object.keys(item).length === 0) {
                  console.error(`[TransactionGrid] ⚠️ EMPTY OBJECT ITEM at index ${virtualRow.index}`, {
                    itemsLength: items.length,
                    virtualRowIndex: virtualRow.index,
                    item,
                  });
                  return null; // Skip rendering to prevent crash
                }
                // Also check if item has no id (malformed data)
                if (!item.id) {
                  console.error(`[TransactionGrid] ⚠️ ITEM WITHOUT ID at index ${virtualRow.index}`, {
                    item,
                    itemType: typeof item,
                    itemKeys: Object.keys(item),
                  });
                  return null; // Skip rendering to prevent crash
                }

                // Render group row (Feature 028)
                if (isGroup(item)) {
                  return (
                    <div
                      key={item.id}
                      style={{
                        position: 'absolute',
                        top: 0,
                        left: 0,
                        width: '100%',
                        minHeight: `${virtualRow.size}px`,
                        transform: `translateY(${virtualRow.start}px)`,
                      }}
                    >
                      <Table>
                        <TableBody>
                          <TransactionGroupRow
                            group={item}
                            isSelected={selection.selectedIds.has(item.id)}
                            isExpanded={expandedGroupIds.has(item.id)}
                            onSelect={(shiftKey) => handleRowSelect(item.id, shiftKey)}
                            onToggleExpand={() => onGroupToggle?.(item.id)}
                            onEditName={onGroupEditName ? (name) => onGroupEditName(item.id, name) : undefined}
                            onEditDate={onGroupEditDate ? (date) => onGroupEditDate(item.id, date) : undefined}
                            onRemoveTransaction={onGroupRemoveTransaction ? (txId) => onGroupRemoveTransaction(item.id, txId) : undefined}
                            onDeleteGroup={onGroupDelete ? () => onGroupDelete(item.id) : undefined}
                            isProcessing={isGroupProcessing}
                          />
                        </TableBody>
                      </Table>
                    </div>
                  );
                }

                // Render transaction row
                const transaction = item as TransactionView;

                // DIAGNOSTIC: Check transaction fields that could cause React Error #301
                const txFields = ['id', 'description', 'merchant', 'category', 'notes'];
                for (const field of txFields) {
                  const value = (transaction as unknown as Record<string, unknown>)[field];
                  if (typeof value === 'object' && value !== null && !(value instanceof Date) && Object.keys(value as object).length === 0) {
                    console.error(`[TransactionGrid] ⚠️ EMPTY OBJECT in transaction.${field}`, {
                      transactionId: transaction.id,
                      field,
                      value,
                    });
                  }
                }

                return (
                  <div
                    key={transaction.id}
                    style={{
                      position: 'absolute',
                      top: 0,
                      left: 0,
                      width: '100%',
                      height: `${virtualRow.size}px`,
                      transform: `translateY(${virtualRow.start}px)`,
                    }}
                  >
                    <Table>
                      <TableBody>
                        <TransactionRow
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
                      </TableBody>
                    </Table>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>

      {/* Loading Overlay */}
      <AnimatePresence>
        {isLoading && items.length > 0 && (
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
