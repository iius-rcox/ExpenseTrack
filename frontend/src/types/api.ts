// API Response Types - mirrors backend DTOs
import type { PredictionSummary } from './prediction';

// Receipt Types
export type ReceiptStatus =
  | 'Uploaded'
  | 'Processing'
  | 'Ready'
  | 'ReviewRequired'
  | 'Error'
  | 'Unmatched'
  | 'Matched'

export interface ReceiptSummary {
  id: string
  thumbnailUrl: string | null
  originalFilename: string
  status: ReceiptStatus
  vendor: string | null
  date: string | null // ISO date string
  amount: number | null
  currency: string
  createdAt: string // ISO datetime
}

export interface ReceiptDetail extends ReceiptSummary {
  blobUrl: string
  contentType: string
  fileSize: number
  tax: number | null
  lineItems: LineItem[]
  confidenceScores: Record<string, number>
  errorMessage: string | null
  retryCount: number
  pageCount: number
  processedAt: string | null
  /** Concurrency token for optimistic locking (Feature 024) */
  rowVersion: number
}

export interface LineItem {
  description: string
  quantity: number | null
  unitPrice: number | null
  totalPrice: number | null
  confidence: number
}

export interface ReceiptListResponse {
  items: ReceiptSummary[]
  totalCount: number
  pageNumber: number
  pageSize: number
}

export interface ReceiptStatusCounts {
  counts: Record<string, number>
  total: number
}

export interface UploadResponse {
  receipts: ReceiptSummary[]
  failed: UploadFailure[]
  totalUploaded: number
}

export interface UploadFailure {
  filename: string
  error: string
}

// Transaction Types
export interface TransactionSummary {
  id: string
  transactionDate: string // ISO date
  description: string
  amount: number
  hasMatchedReceipt: boolean
  /** Prediction data for expense reimbursability (null if no prediction exists) */
  prediction?: PredictionSummary | null
}

export interface TransactionDetail extends TransactionSummary {
  postDate: string | null
  rawDescription: string
  originalDescription: string
  merchantName: string | null
  category: string | null
  matchedReceiptId: string | null
  statementId: string
  importId: string
  importFileName: string
  createdAt: string
  matchedReceipt: MatchedReceiptInfo | null
}

export interface MatchedReceiptInfo {
  id: string
  vendor: string | null
  date: string | null
  amount: number | null
  thumbnailUrl: string | null
  matchConfidence: number
}

export interface TransactionListResponse {
  transactions: TransactionSummary[]
  totalCount: number
  page: number
  pageSize: number
  unmatchedCount: number
}

// Matching Types
export type MatchStatus = 'Proposed' | 'Confirmed' | 'Rejected'

export interface MatchReceiptSummary {
  id: string
  vendorExtracted: string | null
  dateExtracted: string | null // ISO date
  amountExtracted: number | null
  currency: string | null
  thumbnailUrl: string | null
  originalFilename: string
}

export interface MatchTransactionSummary {
  id: string
  description: string
  originalDescription: string
  transactionDate: string // ISO date
  postDate: string | null
  amount: number
}

export interface MatchProposal {
  matchId: string
  receiptId: string
  transactionId: string
  confidenceScore: number
  amountScore: number
  dateScore: number
  vendorScore: number
  matchReason: string
  status: string
  receipt: MatchReceiptSummary | null
  transaction: MatchTransactionSummary | null
  createdAt: string
}

export interface MatchDetail extends MatchProposal {
  confirmedAt: string | null
  isManualMatch: boolean
  vendorAlias: VendorAliasSummary | null
}

export interface VendorAliasSummary {
  id: string
  canonicalName: string
  displayName: string
  aliasPattern: string
  defaultGLCode: string | null
  defaultDepartment: string | null
  matchCount: number
}

export interface ProposalListResponse {
  items: MatchProposal[]
  totalCount: number
  page: number
  pageSize: number
}

export interface MatchingStats {
  matchedCount: number
  proposedCount: number
  unmatchedReceiptsCount: number
  unmatchedTransactionsCount: number
  autoMatchRate: number
  averageConfidence: number
}

export interface AutoMatchResponse {
  proposedCount: number
  processedCount: number
  ambiguousCount: number
  durationMs: number
  proposals: MatchProposal[]
}

// Match Request DTOs
export interface ConfirmMatchRequest {
  vendorDisplayName?: string
  defaultGLCode?: string
  defaultDepartment?: string
}

export interface ManualMatchRequest {
  receiptId: string
  transactionId: string
  vendorDisplayName?: string
  defaultGLCode?: string
  defaultDepartment?: string
}

// Report Types
export type ReportStatus = 'Draft' | 'Submitted' | 'Approved' | 'Rejected'

export interface ExpenseReport {
  id: string
  period: string // YYYY-MM format
  status: ReportStatus
  title: string
  totalAmount: number
  lineCount: number
  createdAt: string
  updatedAt: string
  submittedAt: string | null
  lines: ExpenseLine[]
}

export interface ExpenseLine {
  id: string
  transactionId: string
  receiptId: string | null
  description: string
  normalizedDescription: string | null
  vendor: string | null
  amount: number
  transactionDate: string
  category: string | null
  glCode: string | null
  department: string | null
  project: string | null
  hasReceipt: boolean
  missingReceiptJustification: string | null
  notes: string | null
  splitAllocations: SplitAllocation[]
  /** Feature 023: True if this line was auto-suggested by expense prediction */
  isAutoSuggested?: boolean
  /** Feature 023: The prediction ID that suggested this line */
  predictionId?: string | null
}

export interface SplitAllocation {
  id: string
  department: string
  project: string | null
  percentage: number
  amount: number
}

export interface ReportListResponse {
  items: ReportSummary[]
  totalCount: number
  page: number
  pageSize: number
}

export interface ReportSummary {
  id: string
  period: string
  status: ReportStatus
  title: string
  totalAmount: number
  lineCount: number
  createdAt: string
}

export interface UpdateLineRequest {
  category?: string
  glCode?: string
  department?: string
  project?: string
  notes?: string
  missingReceiptJustification?: string
  splitAllocations?: SplitAllocationInput[]
}

export interface SplitAllocationInput {
  department: string
  project?: string
  percentage: number
}

export interface GenerateDraftRequest {
  period: string // YYYY-MM format
}

// Analytics Types
export interface ComparisonSummary {
  currentTotal: number
  previousTotal: number
  change: number
  changePercent: number
}

export interface MonthlyComparison {
  currentPeriod: string
  previousPeriod: string
  summary: ComparisonSummary
  newVendors: VendorSummary[]
  missingRecurring: VendorSummary[]
  significantChanges: VendorChange[]
}

export interface VendorSummary {
  vendorName: string
  amount: number
  transactionCount: number
}

export interface VendorChange {
  vendorName: string
  currentAmount: number
  previousAmount: number
  change: number
  changePercent: number
}

export interface CategoryBreakdown {
  category: string
  currentAmount: number
  previousAmount: number
  percentageChange: number
}

/**
 * Backend API response for cache statistics.
 * Contains nested Overall object with per-tier statistics.
 */
export interface CacheStatisticsApiResponse {
  period: string
  overall: CacheStatisticsDto
  byOperation?: CacheStatsByOperationDto[]
}

/**
 * Cache tier usage statistics from backend.
 */
export interface CacheStatisticsDto {
  tier1Hits: number
  tier2Hits: number
  tier3Hits: number
  totalOperations: number
  tier1HitRate: number
  tier2HitRate: number
  tier3HitRate: number
  estimatedMonthlyCost: number
  avgResponseTimeMs: number
  belowTarget: boolean
}

/**
 * Per-operation cache statistics from backend.
 */
export interface CacheStatsByOperationDto {
  operationType: string
  tier1Hits: number
  tier2Hits: number
  tier3Hits: number
  tier1HitRate: number
}

/**
 * Transformed cache statistics for UI display.
 * Flattened structure with computed fields.
 */
export interface CacheStatisticsResponse {
  period: string
  totalOperations: number
  hitRate: number
  estimatedCostSaved: number
  tierBreakdown: TierBreakdown[]
  avgResponseTimeMs: number
  belowTarget: boolean
}

export interface TierBreakdown {
  tier: number
  tierName: string
  count: number
  percentage: number
}

// Statement Types
export interface StatementImport {
  id: string
  fileName: string
  fileSize: number
  transactionCount: number
  duplicateCount: number
  status: ImportStatus
  createdAt: string
  processedAt: string | null
}

export type ImportStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed'

export interface StatementFingerprint {
  id: string
  name: string
  bankName: string | null
  accountType: string | null
  matchCount: number
  createdAt: string
}

// Reference Data Types
export interface GLCode {
  code: string
  description: string
}

export interface Department {
  code: string
  name: string
}

export interface Project {
  code: string
  name: string
  department: string
}

// User Types
export interface UserInfo {
  id: string
  email: string
  displayName: string
  preferences: UserPreferences
}

export interface UserPreferences {
  defaultDepartment?: string
  defaultProject?: string
  theme: 'light' | 'dark' | 'system'
}

// Dashboard Types
export interface DashboardSummary {
  pendingMatchesCount: number
  unprocessedReceiptsCount: number
  recentTransactionsCount: number
  recentActivity: ActivityItem[]
}

export interface DashboardMetrics {
  pendingReceiptsCount: number
  unmatchedTransactionsCount: number
  matchedTransactionsCount: number
  pendingMatchesCount: number
  draftReportsCount: number
  monthlySpending: {
    currentMonth: number
    previousMonth: number
    percentChange: number
  }
}

/**
 * Backend API activity item type.
 * For UI components, prefer ExpenseStreamItem from '@/types/dashboard' which has
 * additional fields (status, confidence, thumbnailUrl) needed for rich rendering.
 *
 * Type mapping:
 * - 'receipt_uploaded' → ExpenseStreamEventType 'receipt'
 * - 'statement_imported' → ExpenseStreamEventType 'transaction'
 * - 'match_confirmed' → ExpenseStreamEventType 'match'
 * - 'report_generated' → ExpenseStreamEventType 'report'
 */
export interface ActivityItem {
  id: string
  type: 'receipt_uploaded' | 'statement_imported' | 'match_confirmed' | 'report_generated'
  description: string
  timestamp: string
}

/**
 * @deprecated Use ExpenseStreamItem from '@/types/dashboard' for UI components.
 * This type is kept for backward compatibility with useRecentActivity hook.
 * New code should use useExpenseStream hook which returns ExpenseStreamItem[].
 */
export interface RecentActivityItem {
  type: string
  title: string
  description: string
  timestamp: string
}

// Missing Receipts Types (Feature 026)
export type ReimbursabilitySource = 'UserOverride' | 'AIPrediction'

export interface MissingReceiptSummary {
  transactionId: string
  transactionDate: string // ISO date
  description: string
  amount: number
  daysSinceTransaction: number
  receiptUrl: string | null
  isDismissed: boolean
  source: ReimbursabilitySource
}

export interface MissingReceiptsListResponse {
  items: MissingReceiptSummary[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

export interface MissingReceiptsWidget {
  totalCount: number
  recentItems: MissingReceiptSummary[]
}

export interface UpdateReceiptUrlRequest {
  receiptUrl?: string | null
}

export interface DismissReceiptRequest {
  dismiss?: boolean | null
}

// Error Types
export interface ProblemDetails {
  title: string
  detail: string
  status?: number
  type?: string
  instance?: string
}

export class ApiError extends Error {
  constructor(
    public status: number,
    public detail: string,
    public title?: string
  ) {
    super(detail)
    this.name = 'ApiError'
  }

  get isNotFound(): boolean {
    return this.status === 404
  }

  get isUnauthorized(): boolean {
    return this.status === 401
  }

  get isConflict(): boolean {
    return this.status === 409
  }

  get isValidationError(): boolean {
    return this.status === 400
  }
}

// Feature 027: Report Job Types (Async Report Generation)
export type ReportJobStatus =
  | 'Pending'
  | 'Processing'
  | 'Completed'
  | 'Failed'
  | 'Cancelled'
  | 'CancellationRequested'

export interface ReportJob {
  id: string
  period: string
  status: ReportJobStatus
  totalLines: number
  processedLines: number
  failedLines: number
  /** Computed progress percentage (0-100) */
  progressPercent: number
  errorMessage: string | null
  estimatedCompletionAt: string | null
  startedAt: string | null
  completedAt: string | null
  createdAt: string
  generatedReportId: string | null
}

export interface ReportJobListResponse {
  items: ReportJob[]
  totalCount: number
  page: number
  pageSize: number
}

export interface CreateReportJobRequest {
  period: string // YYYY-MM format
}

export interface ActiveJobResponse {
  hasActiveJob: boolean
  job: ReportJob | null
}
