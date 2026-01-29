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
export type TransactionMatchStatus = 'matched' | 'pending' | 'unmatched' | 'manual' | 'missing-receipt';

/**
 * Transaction reimbursability status for expense filtering.
 *
 * Based on TransactionPrediction.Status:
 * - 'business': Confirmed prediction (user marked as reimbursable)
 * - 'personal': Rejected prediction (user marked as not reimbursable)
 * - 'uncategorized': No prediction or pending status (not yet classified)
 */
export type TransactionReimbursability = 'business' | 'personal' | 'uncategorized';

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
  /** ID of the transaction group this belongs to (Feature 028) */
  groupId?: string;
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
  /** Filter by reimbursability status (OR logic): business, personal, uncategorized */
  reimbursability: TransactionReimbursability[];
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
  reimbursability: [],
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

// =============================================================================
// Transaction Groups (Feature 028)
// =============================================================================

/**
 * Match status for groups (mirrors backend enum).
 */
export type GroupMatchStatus = 'unmatched' | 'proposed' | 'matched';

/**
 * Simplified child transaction for expanded group view.
 * Only includes data needed for the collapsed list display.
 */
export interface GroupMemberTransaction {
  /** Transaction ID */
  id: string;
  /** Transaction date */
  date: Date;
  /** Transaction amount */
  amount: number;
  /** Transaction description */
  description: string;
}

/**
 * View model for transaction group display.
 * Used in the mixed list alongside regular transactions.
 */
export interface TransactionGroupView {
  /** Unique group ID */
  id: string;
  /** Discriminator for mixed list rendering */
  type: 'group';
  /** Display name (e.g., "Twilio (3 charges)") */
  name: string;
  /** Display date (max of children or override) */
  displayDate: Date;
  /** Whether the display date was manually overridden */
  isDateOverridden: boolean;
  /** Combined total of all child transactions */
  combinedAmount: number;
  /** Number of transactions in the group */
  transactionCount: number;
  /** Current match status */
  matchStatus: GroupMatchStatus;
  /** ID of matched receipt (if matched) */
  matchedReceiptId?: string;
  /** Child transactions (loaded when expanded) */
  transactions?: GroupMemberTransaction[];
  /** When the group was created */
  createdAt: Date;
  /** Whether this group is reimbursable (Business=true, Personal=false, Unknown=undefined) */
  isReimbursable?: boolean;
}

/**
 * Extends TransactionView with discriminator for mixed list.
 */
export interface TransactionViewWithType extends TransactionView {
  /** Discriminator for mixed list rendering */
  type: 'transaction';
}

/**
 * Union type for mixed transaction/group list items.
 * Use type discriminator to handle each case.
 */
export type TransactionListItem = TransactionViewWithType | TransactionGroupView;

/**
 * Props for the TransactionGroupRow component.
 */
export interface TransactionGroupRowProps {
  /** Group data */
  group: TransactionGroupView;
  /** Whether this row is selected */
  isSelected: boolean;
  /** Whether the row is expanded */
  isExpanded: boolean;
  /** Selection callback */
  onSelect: (shiftKey: boolean) => void;
  /** Toggle expansion callback */
  onToggleExpand: () => void;
  /** Callback when group name is edited */
  onEditName?: (name: string) => void;
  /** Callback when display date is overridden */
  onEditDate?: (date: Date) => void;
  /** Callback when a transaction is removed from the group */
  onRemoveTransaction?: (transactionId: string) => void;
  /** Callback to delete/dissolve the entire group */
  onDeleteGroup?: () => void;
  /** Whether group actions are processing */
  isProcessing?: boolean;
  /** Whether child transactions are loading (for expansion) */
  isLoadingChildren?: boolean;
  /** Error message if loading children failed */
  childLoadError?: string | null;
  /** Callback to retry loading children */
  onRetryLoadChildren?: () => void;
}

/**
 * Props for the CreateGroupDialog component.
 */
export interface CreateGroupDialogProps {
  /** Whether the dialog is open */
  open: boolean;
  /** Callback to close the dialog */
  onOpenChange: (open: boolean) => void;
  /** Transactions to be grouped */
  transactions: TransactionView[];
  /** Callback when group is created */
  onCreateGroup: (name: string, dateOverride?: Date) => void;
  /** Whether creation is in progress */
  isCreating?: boolean;
}

/**
 * Request body for creating a transaction group.
 */
export interface CreateGroupRequest {
  /** Transaction IDs to group */
  transactionIds: string[];
  /** Optional custom name (auto-generated if not provided) */
  name?: string;
  /** Optional date override */
  displayDateOverride?: string; // ISO date
}

/**
 * Request body for updating a transaction group.
 */
export interface UpdateGroupRequest {
  /** New group name */
  name?: string;
  /** New display date (sets isDateOverridden = true) */
  displayDate?: string; // ISO date
}

/**
 * Request body for adding transactions to a group.
 */
export interface AddToGroupRequest {
  /** Transaction IDs to add */
  transactionIds: string[];
}
