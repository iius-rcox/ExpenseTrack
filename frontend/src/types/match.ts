/**
 * Match Types (T062)
 *
 * Type definitions for the Match Review Workspace (User Story 4).
 * Supports split-pane comparison, keyboard navigation, and batch operations.
 */

import type { ReceiptPreview } from './receipt'
import type { TransactionView } from './transaction'

// ============================================================================
// Core Match Data Types
// ============================================================================

/**
 * Factor types that contribute to match confidence score
 */
export type MatchingFactorType = 'amount' | 'date' | 'merchant' | 'category'

/**
 * Status of a match suggestion
 */
export type MatchStatus = 'pending' | 'approved' | 'rejected'

/**
 * Individual matching factor with contribution details
 */
export interface MatchingFactor {
  /** Type of factor (amount, date, merchant, category) */
  type: MatchingFactorType
  /** Weight contribution to overall confidence (0-1) */
  weight: number
  /** Value from the receipt */
  receiptValue: string
  /** Value from the transaction */
  transactionValue: string
  /** Whether values are an exact match */
  isExactMatch: boolean
}

/**
 * AI-suggested match between a receipt and transaction
 */
export interface MatchSuggestion {
  /** Unique identifier for the match suggestion */
  id: string
  /** Receipt being matched */
  receipt: ReceiptPreview
  /** Transaction being matched */
  transaction: TransactionView
  /** Overall confidence score (0-1) */
  confidence: number
  /** Individual factors contributing to the match */
  matchingFactors: MatchingFactor[]
  /** Current status of the match */
  status: MatchStatus
  /** When the match was reviewed (if applicable) */
  reviewedAt?: Date
  /** Who reviewed the match (if applicable) */
  reviewedBy?: string
}

// ============================================================================
// Match Review State
// ============================================================================

/**
 * State for the match review workspace navigation
 */
export interface MatchReviewState {
  /** Current position in the queue (0-indexed) */
  currentIndex: number
  /** Queue of matches to review */
  queue: MatchSuggestion[]
  /** Whether batch mode is enabled */
  batchMode: boolean
  /** Confidence threshold for batch auto-approve */
  batchThreshold: number
}

/**
 * Keyboard shortcut bindings for match review
 */
export interface MatchKeyboardShortcuts {
  approve: string  // Default: 'a'
  reject: string   // Default: 'r'
  next: string     // Default: 'ArrowDown' or 'j'
  previous: string // Default: 'ArrowUp' or 'k'
  manual: string   // Default: 'm'
  batch: string    // Default: 'b'
}

/**
 * Default keyboard shortcuts for match review
 */
export const DEFAULT_MATCH_SHORTCUTS: MatchKeyboardShortcuts = {
  approve: 'a',
  reject: 'r',
  next: 'ArrowDown',
  previous: 'ArrowUp',
  manual: 'm',
  batch: 'b',
}

// ============================================================================
// API Response Types
// ============================================================================

/**
 * Response from GET /api/matching/pending
 */
export interface PendingMatchesResponse {
  items: MatchSuggestion[]
  pagination: {
    page: number
    limit: number
    total: number
  }
}

/**
 * Response from match approve/reject endpoints
 */
export interface MatchActionResponse {
  success: boolean
  matchId: string
  receiptId?: string
  transactionId?: string
}

/**
 * Request for manual match creation
 */
export interface ManualMatchRequest {
  receiptId: string
  transactionId: string
}

/**
 * Request for batch approve operation
 */
export interface BatchApproveRequest {
  ids?: string[]
  minConfidence?: number
}

/**
 * Response from batch approve
 */
export interface BatchApproveResponse {
  approved: number
  skipped: number
}

// ============================================================================
// Component Props Types
// ============================================================================

/**
 * Props for MatchReviewWorkspace component
 */
export interface MatchReviewWorkspaceProps {
  /** Current match being reviewed */
  match: MatchSuggestion
  /** Position in the queue */
  position: {
    current: number
    total: number
  }
  /** Approve current match */
  onApprove: () => void
  /** Reject current match */
  onReject: () => void
  /** Navigate to next match */
  onNext: () => void
  /** Navigate to previous match */
  onPrevious: () => void
  /** Open manual match dialog */
  onManualMatch: () => void
  /** Whether an action is in progress */
  isProcessing?: boolean
}

/**
 * Props for MatchComparisonView component
 */
export interface MatchComparisonViewProps {
  /** Receipt being compared */
  receipt: ReceiptPreview
  /** Transaction being compared */
  transaction: TransactionView
  /** Factors that contributed to the match */
  matchingFactors: MatchingFactor[]
  /** Overall confidence score */
  confidence: number
  /** Highlight specific factor */
  highlightedFactor?: MatchingFactorType
  /** Callback when factor is hovered */
  onFactorHover?: (factor: MatchingFactorType | null) => void
}

/**
 * Props for MatchingFactors component
 */
export interface MatchingFactorsProps {
  /** List of matching factors */
  factors: MatchingFactor[]
  /** Overall confidence */
  confidence: number
  /** Currently highlighted factor */
  highlightedFactor?: MatchingFactorType
  /** Factor hover callback */
  onHover?: (factor: MatchingFactorType | null) => void
  /** Compact display mode */
  compact?: boolean
}

/**
 * Props for BatchReviewPanel component
 */
export interface BatchReviewPanelProps {
  /** All matches in current view */
  matches: MatchSuggestion[]
  /** Current confidence threshold */
  threshold: number
  /** Callback when threshold changes */
  onThresholdChange: (value: number) => void
  /** Approve all matches above threshold */
  onApproveAll: () => void
  /** Reject all matches below threshold */
  onRejectAll: () => void
  /** Number of matches above threshold */
  eligibleCount: number
  /** Whether batch operation is in progress */
  isProcessing?: boolean
}

/**
 * Props for ManualMatchDialog component
 */
export interface ManualMatchDialogProps {
  /** Receipt to find a match for */
  receipt: ReceiptPreview
  /** Available transactions to match with */
  transactions: TransactionView[]
  /** Whether dialog is open */
  isOpen: boolean
  /** Close dialog callback */
  onClose: () => void
  /** Match selected transaction */
  onMatch: (transactionId: string) => void
  /** Current search query */
  searchQuery: string
  /** Search query change callback */
  onSearchChange: (query: string) => void
  /** Whether transactions are loading */
  isLoading?: boolean
}

// ============================================================================
// Defaults
// ============================================================================

/**
 * Default match review state
 */
export const DEFAULT_MATCH_REVIEW_STATE: MatchReviewState = {
  currentIndex: 0,
  queue: [],
  batchMode: false,
  batchThreshold: 0.9,
}

/**
 * Confidence thresholds for visual indicators
 */
export const MATCH_CONFIDENCE_THRESHOLDS = {
  high: 0.9,   // Auto-approve eligible
  medium: 0.7, // Needs review
  low: 0.5,    // Likely mismatch
} as const

// ============================================================================
// Utility Functions
// ============================================================================

/**
 * Get confidence level from numeric score
 */
export function getMatchConfidenceLevel(
  confidence: number
): 'high' | 'medium' | 'low' {
  if (confidence >= MATCH_CONFIDENCE_THRESHOLDS.high) return 'high'
  if (confidence >= MATCH_CONFIDENCE_THRESHOLDS.medium) return 'medium'
  return 'low'
}

/**
 * Calculate how many matches are above a confidence threshold
 */
export function countMatchesAboveThreshold(
  matches: MatchSuggestion[],
  threshold: number
): number {
  return matches.filter((m) => m.confidence >= threshold).length
}

/**
 * Sort matching factors by weight (highest first)
 */
export function sortFactorsByWeight(factors: MatchingFactor[]): MatchingFactor[] {
  return [...factors].sort((a, b) => b.weight - a.weight)
}
