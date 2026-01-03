/**
 * Expense Prediction Types (Feature 023)
 *
 * Frontend-specific types for the expense prediction feature.
 * These types mirror the backend DTOs and add UI-specific state.
 *
 * @see specs/023-expense-prediction/data-model.md for entity definitions
 * @see specs/023-expense-prediction/contracts/predictions-api.yaml for API contracts
 */

// =============================================================================
// Enums
// =============================================================================

/**
 * Prediction confidence level.
 * Matches backend PredictionConfidence enum.
 */
export type PredictionConfidence = 'Low' | 'Medium' | 'High';

/**
 * Prediction lifecycle status.
 * Matches backend PredictionStatus enum.
 */
export type PredictionStatus = 'Pending' | 'Confirmed' | 'Rejected' | 'Ignored';

/**
 * User feedback type.
 * Matches backend FeedbackType enum.
 */
export type FeedbackType = 'Confirmed' | 'Rejected';

// =============================================================================
// Pattern Types
// =============================================================================

/**
 * Summary view of an expense pattern for list displays.
 */
export interface PatternSummary {
  /** Pattern ID */
  id: string;
  /** Human-readable vendor name */
  displayName: string;
  /** Most common category for this vendor */
  category: string | null;
  /** Running average of expense amounts */
  averageAmount: number;
  /** Number of times this vendor appeared in reports */
  occurrenceCount: number;
  /** Most recent occurrence date */
  lastSeenAt: Date;
  /** True if user suppressed predictions for this vendor */
  isSuppressed: boolean;
  /** When true, predictions only generated for transactions with confirmed receipt matches */
  requiresReceiptMatch: boolean;
  /** Calculated accuracy from confirm/reject ratio */
  accuracyRate: number;
}

/**
 * Detailed view of an expense pattern.
 */
export interface PatternDetail extends PatternSummary {
  /** Normalized vendor name (system identifier) */
  normalizedVendor: string;
  /** Minimum amount seen */
  minAmount: number;
  /** Maximum amount seen */
  maxAmount: number;
  /** Default GL code for this vendor */
  defaultGLCode: string | null;
  /** Default department for this vendor */
  defaultDepartment: string | null;
  /** Number of confirmed predictions */
  confirmCount: number;
  /** Number of rejected predictions */
  rejectCount: number;
  /** Pattern creation date */
  createdAt: Date;
  /** Last update date */
  updatedAt: Date;
}

// =============================================================================
// Prediction Types
// =============================================================================

/**
 * Summary view of a prediction for badge display.
 */
export interface PredictionSummary {
  /** Prediction ID */
  id: string;
  /** Transaction ID this prediction applies to */
  transactionId: string;
  /** Pattern that generated this prediction */
  patternId: string;
  /** Vendor display name from pattern */
  vendorName: string;
  /** Confidence score (0.00 - 1.00) */
  confidenceScore: number;
  /** Confidence level (High, Medium) */
  confidenceLevel: PredictionConfidence;
  /** Prediction status */
  status: PredictionStatus;
  /** Suggested category from pattern */
  suggestedCategory: string | null;
  /** Suggested GL code from pattern */
  suggestedGLCode: string | null;
}

/**
 * Detailed view of a prediction.
 */
export interface PredictionDetail extends PredictionSummary {
  /** Suggested department from pattern */
  suggestedDepartment: string | null;
  /** Pattern's average amount for comparison */
  patternAverageAmount: number;
  /** Number of occurrences supporting this pattern */
  patternOccurrenceCount: number;
  /** When the prediction was created */
  createdAt: Date;
  /** When the user acted on the prediction (nullable) */
  resolvedAt: Date | null;
}

/**
 * Transaction with prediction data attached for display.
 */
export interface PredictionTransaction {
  /** Transaction ID */
  id: string;
  /** Transaction date */
  transactionDate: Date;
  /** Transaction description */
  description: string;
  /** Transaction amount */
  amount: number;
  /** Whether transaction has a matched receipt */
  hasMatchedReceipt: boolean;
  /** Prediction summary if available */
  prediction: PredictionSummary | null;
}

// =============================================================================
// Dashboard Types
// =============================================================================

/**
 * Dashboard summary for expense predictions.
 */
export interface PredictionDashboard {
  /** Total pending predictions (Medium + High confidence only) */
  pendingCount: number;
  /** High confidence pending predictions */
  highConfidenceCount: number;
  /** Medium confidence pending predictions */
  mediumConfidenceCount: number;
  /** Total active patterns */
  activePatternCount: number;
  /** Overall prediction accuracy (confirm rate) */
  overallAccuracyRate: number;
  /** Top predicted transactions for quick action */
  topPredictions: PredictionTransaction[];
}

/**
 * Prediction accuracy statistics.
 */
export interface PredictionAccuracyStats {
  /** Total predictions made */
  totalPredictions: number;
  /** Total confirmed predictions */
  confirmedCount: number;
  /** Total rejected predictions */
  rejectedCount: number;
  /** Total ignored predictions */
  ignoredCount: number;
  /** Overall accuracy rate (confirmed / (confirmed + rejected)) */
  accuracyRate: number;
  /** High confidence accuracy rate */
  highConfidenceAccuracyRate: number;
  /** Medium confidence accuracy rate */
  mediumConfidenceAccuracyRate: number;
}

/**
 * Response for prediction availability check.
 */
export interface PredictionAvailability {
  /** True if user has at least one expense pattern */
  isAvailable: boolean;
  /** Number of learned patterns */
  patternCount: number;
  /** User-friendly message explaining availability status */
  message: string;
}

// =============================================================================
// Request Types
// =============================================================================

/**
 * Request to confirm a prediction.
 */
export interface ConfirmPredictionRequest {
  /** Prediction ID to confirm */
  predictionId: string;
  /** Optional override for GL code */
  glCodeOverride?: string;
  /** Optional override for department */
  departmentOverride?: string;
}

/**
 * Request to reject a prediction.
 */
export interface RejectPredictionRequest {
  /** Prediction ID to reject */
  predictionId: string;
}

/**
 * Request for bulk prediction actions.
 */
export interface BulkPredictionActionRequest {
  /** List of prediction IDs to act on */
  predictionIds: string[];
  /** Action to perform: Confirm or Reject */
  action: FeedbackType;
}

/**
 * Request to update pattern suppression.
 */
export interface UpdatePatternSuppressionRequest {
  /** Pattern ID to update */
  patternId: string;
  /** Whether to suppress predictions for this pattern */
  isSuppressed: boolean;
}

/**
 * Request to update pattern receipt match requirement.
 */
export interface UpdatePatternReceiptMatchRequest {
  /** Pattern ID to update */
  patternId: string;
  /** Whether to require receipt matches for predictions from this pattern */
  requiresReceiptMatch: boolean;
}

// =============================================================================
// Response Types
// =============================================================================

/**
 * Response for prediction action results.
 */
export interface PredictionActionResponse {
  /** Whether the action succeeded */
  success: boolean;
  /** Updated prediction status */
  newStatus: PredictionStatus;
  /** Message describing the result */
  message: string;
  /**
   * True if the pattern was auto-suppressed due to low accuracy.
   * Only set on reject actions when pattern hits threshold (>3 rejects, <30% confirm rate).
   */
  patternSuppressed?: boolean;
}

/**
 * Response for bulk prediction actions.
 */
export interface BulkPredictionActionResponse {
  /** Number of predictions successfully updated */
  successCount: number;
  /** Number of predictions that failed to update */
  failedCount: number;
  /** IDs of predictions that failed (if any) */
  failedIds: string[];
  /** Summary message */
  message: string;
}

/**
 * Paginated response for predictions list.
 */
export interface PredictionListResponse {
  /** List of predictions */
  predictions: PredictionSummary[];
  /** Total count matching filters */
  totalCount: number;
  /** Current page number */
  page: number;
  /** Page size */
  pageSize: number;
  /** Count of pending predictions */
  pendingCount: number;
  /** Count of high confidence predictions */
  highConfidenceCount: number;
}

/**
 * Paginated response for patterns list.
 */
export interface PatternListResponse {
  /** List of patterns */
  patterns: PatternSummary[];
  /** Total count matching filters */
  totalCount: number;
  /** Current page number */
  page: number;
  /** Page size */
  pageSize: number;
  /** Count of active (non-suppressed) patterns */
  activeCount: number;
  /** Count of suppressed patterns */
  suppressedCount: number;
}

// =============================================================================
// UI State Types
// =============================================================================

/**
 * Filter configuration for prediction lists.
 */
export interface PredictionFilters {
  /** Filter by status */
  status: PredictionStatus | null;
  /** Minimum confidence level to show */
  minConfidence: PredictionConfidence;
  /** Search by vendor name */
  search: string;
}

/**
 * Default prediction filters.
 */
export const DEFAULT_PREDICTION_FILTERS: PredictionFilters = {
  status: null,
  minConfidence: 'Medium', // Low confidence is suppressed by default
  search: '',
};

/**
 * Filter configuration for pattern lists.
 */
export interface PatternFilters {
  /** Include suppressed patterns */
  includeSuppressed: boolean;
  /** Search by vendor name */
  search: string;
}

/**
 * Default pattern filters.
 */
export const DEFAULT_PATTERN_FILTERS: PatternFilters = {
  includeSuppressed: false,
  search: '',
};

/**
 * Sort configuration for pattern lists.
 */
export interface PatternSortConfig {
  /** Field to sort by */
  field: 'displayName' | 'occurrenceCount' | 'averageAmount' | 'lastSeenAt' | 'accuracyRate';
  /** Sort direction */
  direction: 'asc' | 'desc';
}

/**
 * Default pattern sort (most frequent first).
 */
export const DEFAULT_PATTERN_SORT: PatternSortConfig = {
  field: 'occurrenceCount',
  direction: 'desc',
};

// =============================================================================
// Badge Display Types
// =============================================================================

/**
 * Badge variant based on confidence level.
 */
export type PredictionBadgeVariant = 'high' | 'medium';

/**
 * Props for the prediction badge component.
 */
export interface PredictionBadgeProps {
  /** Confidence level for styling */
  confidenceLevel: PredictionConfidence;
  /** Suggested category to display */
  suggestedCategory: string | null;
  /** Click handler for quick actions */
  onClick?: () => void;
  /** Whether badge is in compact mode */
  compact?: boolean;
}

/**
 * Helper to map confidence level to badge variant.
 */
export function getConfidenceBadgeVariant(confidence: PredictionConfidence): PredictionBadgeVariant {
  return confidence === 'High' ? 'high' : 'medium';
}

/**
 * Helper to format confidence score as percentage.
 */
export function formatConfidenceScore(score: number): string {
  return `${Math.round(score * 100)}%`;
}

// =============================================================================
// Pattern Grid UI Types
// =============================================================================

/**
 * Selection state for pattern grid.
 */
export interface PatternSelectionState {
  /** Selected pattern IDs */
  selectedIds: Set<string>;
  /** Last selected ID for range selection */
  lastSelectedId: string | null;
  /** Whether all patterns are selected */
  isSelectAll: boolean;
}

/**
 * Default pattern selection state.
 */
export const DEFAULT_PATTERN_SELECTION: PatternSelectionState = {
  selectedIds: new Set(),
  lastSelectedId: null,
  isSelectAll: false,
};

/**
 * Status filter for patterns.
 */
export type PatternStatusFilter = 'all' | 'active' | 'suppressed';

/**
 * Extended pattern filters for grid UI.
 */
export interface PatternGridFilters extends PatternFilters {
  /** Status filter */
  status: PatternStatusFilter;
  /** Category filter */
  category: string | null;
}

/**
 * Default pattern grid filters.
 */
export const DEFAULT_PATTERN_GRID_FILTERS: PatternGridFilters = {
  includeSuppressed: true, // Show all by default, use status filter
  search: '',
  status: 'all',
  category: null,
};

/**
 * Bulk action types for patterns.
 */
export type PatternBulkAction = 'suppress' | 'enable' | 'delete';

/**
 * Request for bulk pattern actions.
 */
export interface BulkPatternActionRequest {
  /** Pattern IDs to act on */
  patternIds: string[];
  /** Action to perform */
  action: PatternBulkAction;
}
