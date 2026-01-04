/**
 * Transaction Types (T049)
 *
 * Frontend-specific view models and state types for the Transaction Explorer.
 * These transform API types (TransactionSummary, TransactionDetail) into
 * UI-optimized structures.
 *
 * @see src/types/api.ts for backend DTO types
 * @see data-model.md Section 4 for type specifications
 */

import type { PredictionSummary } from './prediction';

// =============================================================================
// View Models
// =============================================================================

/**
 * Transaction match status in the UI.
 *
 * Maps from backend hasMatchedReceipt boolean + matchedReceiptId:
 * - 'matched': hasMatchedReceipt === true
 * - 'pending': matchedReceiptId exists but not confirmed
 * - 'unmatched': no receipt linked
 * - 'manual': user manually linked receipt
 */
export type TransactionMatchStatus = 'matched' | 'pending' | 'unmatched' | 'manual';

/**
 * Transaction source type.
 */
export type TransactionSource = 'import' | 'manual' | 'api';

/**
 * View model for transaction grid display.
 *
 * Transforms TransactionDetail from API into UI-optimized structure with:
 * - Parsed Date objects instead of ISO strings
 * - UI state fields (isEditing)
 * - Computed match status
 */
export interface TransactionView {
  /** Unique transaction ID */
  id: string;
  /** Transaction date (parsed from ISO string) */
  date: Date;
  /** Cleaned/normalized description for display */
  description: string;
  /** Extracted or inferred merchant name */
  merchant: string;
  /** Transaction amount (positive = expense, negative = credit) */
  amount: number;
  /** Assigned category name */
  category: string;
  /** Category ID for updates */
  categoryId: string;
  /** User-applied tags */
  tags: string[];
  /** User notes */
  notes: string;
  /** Current match status with receipts */
  matchStatus: TransactionMatchStatus;
  /** ID of matched receipt if any */
  matchedReceiptId?: string;
  /** AI confidence score for the match (0-1) */
  matchConfidence?: number;
  /** How this transaction was created */
  source: TransactionSource;
  /** UI state: currently in edit mode */
  isEditing?: boolean;
  /** Statement ID for grouping */
  statementId?: string;
  /** Import file name for display */
  importFileName?: string;
  /** Expense prediction for this transaction (Feature 023) */
  prediction?: PredictionSummary | null;
}

// =============================================================================
// Filter & Sort State
// =============================================================================

/**
 * Filter configuration for transaction grid.
 *
 * All filters are optional - undefined means "no filter applied".
 * Multiple values in arrays use OR logic within the filter,
 * different filters use AND logic between them.
 */
export interface TransactionFilters {
  /** Full-text search across description, merchant, notes */
  search: string;
  /** Date range filter (inclusive on both ends) */
  dateRange: {
    start: Date | null;
    end: Date | null;
  };
  /** Filter by category IDs (OR logic) */
  categories: string[];
  /** Amount range filter (inclusive) */
  amountRange: {
    min: number | null;
    max: number | null;
  };
  /** Filter by match status (OR logic) */
  matchStatus: TransactionMatchStatus[];
  /** Filter by tags (AND logic - must have all) */
  tags: string[];
  /** Filter to show only transactions with pending expense predictions (Feature 023) */
  hasPendingPrediction: boolean;
}

/**
 * Default/empty filter state.
 */
export const DEFAULT_TRANSACTION_FILTERS: TransactionFilters = {
  search: '',
  dateRange: { start: null, end: null },
  categories: [],
  amountRange: { min: null, max: null },
  matchStatus: [],
  tags: [],
  hasPendingPrediction: false,
};

/**
 * Sortable fields in the transaction grid.
 */
export type TransactionSortField = 'date' | 'amount' | 'merchant' | 'category';

/**
 * Sort configuration for transaction grid.
 */
export interface TransactionSortConfig {
  /** Field to sort by */
  field: TransactionSortField;
  /** Sort direction */
  direction: 'asc' | 'desc';
}

/**
 * Default sort configuration (newest first).
 */
export const DEFAULT_TRANSACTION_SORT: TransactionSortConfig = {
  field: 'date',
  direction: 'desc',
};

// =============================================================================
// Selection State
// =============================================================================

/**
 * Multi-selection state for bulk operations.
 *
 * Uses a Set for O(1) lookups when dealing with large lists.
 */
export interface TransactionSelectionState {
  /** Set of selected transaction IDs */
  selectedIds: Set<string>;
  /** Last selected ID for shift-click range selection */
  lastSelectedId: string | null;
  /** Whether "select all" is active */
  isSelectAll: boolean;
}

/**
 * Default/empty selection state.
 */
export const DEFAULT_TRANSACTION_SELECTION: TransactionSelectionState = {
  selectedIds: new Set(),
  lastSelectedId: null,
  isSelectAll: false,
};

// =============================================================================
// Component Props
// =============================================================================

/**
 * Props for the main transaction grid component.
 */
export interface TransactionGridProps {
  /** List of transactions to display */
  transactions: TransactionView[];
  /** Loading state */
  isLoading?: boolean;
  /** Current filter configuration */
  filters: TransactionFilters;
  /** Current sort configuration */
  sort: TransactionSortConfig;
  /** Current selection state */
  selection: TransactionSelectionState;
  /** Callback when filters change */
  onFilterChange: (filters: TransactionFilters) => void;
  /** Callback when sort changes */
  onSortChange: (sort: TransactionSortConfig) => void;
  /** Callback when selection changes */
  onSelectionChange: (selection: TransactionSelectionState) => void;
  /** Callback when a transaction is edited inline */
  onTransactionEdit: (id: string, updates: Partial<TransactionView>) => void;
  /** Callback when a transaction row is clicked */
  onTransactionClick?: (transaction: TransactionView) => void;
}

/**
 * Props for individual transaction row component.
 */
export interface TransactionRowProps {
  /** Transaction data */
  transaction: TransactionView;
  /** Whether this row is selected */
  isSelected: boolean;
  /** Whether this row is in edit mode */
  isEditing: boolean;
  /** Selection callback (shiftKey for range selection) */
  onSelect: (shiftKey: boolean) => void;
  /** Edit callback for inline changes */
  onEdit: (updates: Partial<TransactionView>) => void;
  /** Click callback for navigation/detail view */
  onClick: () => void;
}

/**
 * Props for bulk actions bar component.
 */
export interface BulkActionsBarProps {
  /** Number of selected transactions */
  selectedCount: number;
  /** Callback to categorize selected transactions */
  onCategorize: (categoryId: string) => void;
  /** Callback to add tags to selected transactions */
  onTag: (tags: string[]) => void;
  /** Callback to export selected transactions */
  onExport: () => void;
  /** Callback to delete selected transactions */
  onDelete: () => void;
  /** Callback to clear selection */
  onClearSelection: () => void;
  /** Callback to mark transactions as reimbursable (business expense) */
  onMarkReimbursable?: () => void;
  /** Callback to mark transactions as not reimbursable (personal expense) */
  onMarkNotReimbursable?: () => void;
}

/**
 * Props for filter panel component.
 */
export interface TransactionFilterPanelProps {
  /** Current filter state */
  filters: TransactionFilters;
  /** Available categories for filter dropdown */
  categories: { id: string; name: string }[];
  /** Available tags for filter dropdown */
  tags: string[];
  /** Callback when filters change */
  onChange: (filters: TransactionFilters) => void;
  /** Callback to reset all filters */
  onReset: () => void;
}

// =============================================================================
// API Request/Response Helpers
// =============================================================================

/**
 * Query parameters for transaction list API.
 * Matches GET /api/transactions query params.
 */
export interface TransactionQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  startDate?: string; // ISO date
  endDate?: string; // ISO date
  categories?: string[];
  minAmount?: number;
  maxAmount?: number;
  matchStatus?: string[];
  tags?: string[];
  sortBy?: TransactionSortField;
  sortOrder?: 'asc' | 'desc';
}

/**
 * Request body for transaction updates.
 * Matches PATCH /api/transactions/:id body.
 */
export interface TransactionUpdateRequest {
  category?: string;
  notes?: string;
  tags?: string[];
}

/**
 * Request body for bulk transaction updates.
 * Matches POST /api/transactions/bulk body.
 */
export interface TransactionBulkUpdateRequest {
  ids: string[];
  updates: TransactionUpdateRequest;
}

/**
 * Export format options.
 */
export type TransactionExportFormat = 'csv' | 'xlsx';

/**
 * Request params for transaction export.
 */
export interface TransactionExportParams {
  format: TransactionExportFormat;
  ids?: string[]; // If empty, exports all matching current filters
  filters?: TransactionFilters;
}
