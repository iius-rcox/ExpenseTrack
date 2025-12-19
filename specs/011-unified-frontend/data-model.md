# Data Model: Unified Frontend Experience

**Feature Branch**: `011-unified-frontend`
**Date**: 2025-12-18

This document defines the TypeScript types and state structures for the frontend application.

## 1. API Response Types

These types mirror the backend DTOs returned from the .NET API.

### Receipt Types

```typescript
// src/types/api/receipts.ts

export type ReceiptStatus =
  | 'Pending'
  | 'Processing'
  | 'Processed'
  | 'Unmatched'
  | 'Matched'
  | 'Error'

export interface ReceiptSummary {
  id: string
  thumbnailUrl: string | null
  originalFilename: string
  status: ReceiptStatus
  vendor: string | null
  date: string | null  // ISO date string
  amount: number | null
  currency: string
  createdAt: string  // ISO datetime
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
```

### Transaction Types

```typescript
// src/types/api/transactions.ts

export interface TransactionSummary {
  id: string
  transactionDate: string  // ISO date
  description: string
  amount: number
  hasMatchedReceipt: boolean
}

export interface TransactionDetail extends TransactionSummary {
  postDate: string | null
  originalDescription: string
  matchedReceiptId: string | null
  importId: string
  importFileName: string
  createdAt: string
}

export interface TransactionListResponse {
  transactions: TransactionSummary[]
  totalCount: number
  page: number
  pageSize: number
  unmatchedCount: number
}
```

### Matching Types

```typescript
// src/types/api/matching.ts

export type MatchStatus = 'Proposed' | 'Confirmed' | 'Rejected'

export interface MatchReceiptSummary {
  id: string
  vendorExtracted: string | null
  dateExtracted: string | null  // ISO date
  amountExtracted: number | null
  currency: string | null
  thumbnailUrl: string | null
  originalFilename: string
}

export interface MatchTransactionSummary {
  id: string
  description: string
  originalDescription: string
  transactionDate: string  // ISO date
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

// Request DTOs
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
```

### Report Types

```typescript
// src/types/api/reports.ts

export type ReportStatus = 'Draft' | 'Submitted' | 'Approved' | 'Rejected'

export interface ExpenseReport {
  id: string
  period: string  // YYYY-MM format
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
  period: string  // YYYY-MM format
}
```

### Analytics Types

```typescript
// src/types/api/analytics.ts

export interface MonthlyComparison {
  currentPeriod: string
  previousPeriod: string
  currentTotal: number
  previousTotal: number
  percentageChange: number
  newVendors: VendorSummary[]
  missingRecurring: VendorSummary[]
  significantChanges: VendorChange[]
  categoryBreakdown: CategoryBreakdown[]
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
  percentageChange: number
}

export interface CategoryBreakdown {
  category: string
  currentAmount: number
  previousAmount: number
  percentageChange: number
}

export interface CacheStatisticsResponse {
  period: string
  totalOperations: number
  hitRate: number
  estimatedCostSaved: number
  tierBreakdown: TierBreakdown[]
  dailyBreakdown?: DailyBreakdown[]
}

export interface TierBreakdown {
  tier: number
  tierName: string
  count: number
  percentage: number
  estimatedCost: number
}

export interface DailyBreakdown {
  date: string
  operations: number
  hitRate: number
}
```

### Statement Types (Existing)

```typescript
// src/types/api/statements.ts

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
```

## 2. Route Search Param Schemas

```typescript
// src/types/routes.ts
import { z } from 'zod'

// Receipts page search params
export const receiptSearchSchema = z.object({
  page: z.number().optional().default(1),
  pageSize: z.number().optional().default(20),
  status: z.enum(['Pending', 'Processing', 'Processed', 'Unmatched', 'Matched', 'Error']).optional(),
  fromDate: z.string().optional(),
  toDate: z.string().optional(),
})
export type ReceiptSearchParams = z.infer<typeof receiptSearchSchema>

// Transactions page search params
export const transactionSearchSchema = z.object({
  page: z.number().optional().default(1),
  pageSize: z.number().optional().default(50),
  startDate: z.string().optional(),
  endDate: z.string().optional(),
  matched: z.boolean().optional(),
  importId: z.string().uuid().optional(),
  search: z.string().optional(),
})
export type TransactionSearchParams = z.infer<typeof transactionSearchSchema>

// Matching page search params
export const matchingSearchSchema = z.object({
  page: z.number().optional().default(1),
  pageSize: z.number().optional().default(20),
  tab: z.enum(['proposals', 'unmatched-receipts', 'unmatched-transactions']).optional().default('proposals'),
})
export type MatchingSearchParams = z.infer<typeof matchingSearchSchema>

// Reports page search params
export const reportSearchSchema = z.object({
  page: z.number().optional().default(1),
  pageSize: z.number().optional().default(20),
  status: z.enum(['Draft', 'Submitted', 'Approved', 'Rejected']).optional(),
  period: z.string().optional(),
})
export type ReportSearchParams = z.infer<typeof reportSearchSchema>

// Analytics page search params
export const analyticsSearchSchema = z.object({
  currentPeriod: z.string().optional(),
  previousPeriod: z.string().optional(),
  view: z.enum(['comparison', 'cache-stats']).optional().default('comparison'),
})
export type AnalyticsSearchParams = z.infer<typeof analyticsSearchSchema>

// Login redirect
export const loginSearchSchema = z.object({
  redirect: z.string().optional(),
})
export type LoginSearchParams = z.infer<typeof loginSearchSchema>
```

## 3. UI State Types

```typescript
// src/types/ui.ts

// Navigation state
export interface NavigationState {
  currentPath: string
  breadcrumbs: Breadcrumb[]
  sidebarCollapsed: boolean
}

export interface Breadcrumb {
  label: string
  href: string
}

// Dashboard summary
export interface DashboardSummary {
  pendingMatchesCount: number
  unprocessedReceiptsCount: number
  recentTransactionsCount: number
  recentActivity: ActivityItem[]
}

export interface ActivityItem {
  id: string
  type: 'receipt_uploaded' | 'statement_imported' | 'match_confirmed' | 'report_generated'
  description: string
  timestamp: string
}

// Upload state
export interface UploadState {
  files: UploadFile[]
  isUploading: boolean
  progress: number
}

export interface UploadFile {
  id: string
  file: File
  status: 'pending' | 'uploading' | 'success' | 'error'
  progress: number
  error?: string
  receiptId?: string
}

// Modal/Dialog state
export interface DialogState {
  isOpen: boolean
  type: DialogType | null
  data: unknown
}

export type DialogType =
  | 'delete-receipt'
  | 'delete-transaction'
  | 'confirm-match'
  | 'reject-match'
  | 'manual-match'
  | 'generate-report'
  | 'export-report'

// Toast notifications (managed by Sonner)
export type ToastType = 'success' | 'error' | 'info' | 'warning'
```

## 4. Query Key Structure

Consistent query key patterns for TanStack Query cache management.

```typescript
// src/lib/query-keys.ts

export const queryKeys = {
  // Receipts
  receipts: {
    all: ['receipts'] as const,
    lists: () => [...queryKeys.receipts.all, 'list'] as const,
    list: (params: ReceiptSearchParams) => [...queryKeys.receipts.lists(), params] as const,
    details: () => [...queryKeys.receipts.all, 'detail'] as const,
    detail: (id: string) => [...queryKeys.receipts.details(), id] as const,
    counts: () => [...queryKeys.receipts.all, 'counts'] as const,
  },

  // Transactions
  transactions: {
    all: ['transactions'] as const,
    lists: () => [...queryKeys.transactions.all, 'list'] as const,
    list: (params: TransactionSearchParams) => [...queryKeys.transactions.lists(), params] as const,
    details: () => [...queryKeys.transactions.all, 'detail'] as const,
    detail: (id: string) => [...queryKeys.transactions.details(), id] as const,
  },

  // Matching
  matching: {
    all: ['matching'] as const,
    proposals: (params: { page: number; pageSize: number }) => [...queryKeys.matching.all, 'proposals', params] as const,
    stats: () => [...queryKeys.matching.all, 'stats'] as const,
    unmatchedReceipts: (params: { page: number; pageSize: number }) => [...queryKeys.matching.all, 'unmatched-receipts', params] as const,
    unmatchedTransactions: (params: { page: number; pageSize: number }) => [...queryKeys.matching.all, 'unmatched-transactions', params] as const,
    detail: (id: string) => [...queryKeys.matching.all, 'detail', id] as const,
  },

  // Reports
  reports: {
    all: ['reports'] as const,
    lists: () => [...queryKeys.reports.all, 'list'] as const,
    list: (params: ReportSearchParams) => [...queryKeys.reports.lists(), params] as const,
    details: () => [...queryKeys.reports.all, 'detail'] as const,
    detail: (id: string) => [...queryKeys.reports.details(), id] as const,
    draftExists: (period: string) => [...queryKeys.reports.all, 'draft-exists', period] as const,
  },

  // Analytics
  analytics: {
    all: ['analytics'] as const,
    comparison: (currentPeriod: string, previousPeriod?: string) =>
      [...queryKeys.analytics.all, 'comparison', currentPeriod, previousPeriod] as const,
    cacheStats: (period: string, groupBy?: string) =>
      [...queryKeys.analytics.all, 'cache-stats', period, groupBy] as const,
  },

  // Statements
  statements: {
    all: ['statements'] as const,
    imports: () => [...queryKeys.statements.all, 'imports'] as const,
    fingerprints: () => [...queryKeys.statements.all, 'fingerprints'] as const,
  },

  // Dashboard
  dashboard: {
    all: ['dashboard'] as const,
    summary: () => [...queryKeys.dashboard.all, 'summary'] as const,
  },

  // Reference data
  reference: {
    all: ['reference'] as const,
    glCodes: () => [...queryKeys.reference.all, 'gl-codes'] as const,
    departments: () => [...queryKeys.reference.all, 'departments'] as const,
    projects: () => [...queryKeys.reference.all, 'projects'] as const,
  },
} as const
```

## 5. Component Props Interfaces

```typescript
// src/types/components.ts

// Receipt components
export interface ReceiptCardProps {
  receipt: ReceiptSummary
  onView: (id: string) => void
  onDelete: (id: string) => void
}

export interface ReceiptUploadProps {
  onUploadComplete: (response: UploadResponse) => void
  maxFiles?: number
}

// Transaction components
export interface TransactionTableProps {
  transactions: TransactionSummary[]
  isLoading: boolean
  onRowClick: (id: string) => void
  onSort: (column: string, direction: 'asc' | 'desc') => void
}

// Matching components
export interface MatchProposalCardProps {
  proposal: MatchProposal
  onConfirm: () => void
  onReject: () => void
  isConfirming: boolean
  isRejecting: boolean
}

export interface ManualMatchDialogProps {
  receipt: MatchReceiptSummary
  isOpen: boolean
  onClose: () => void
  onMatch: (transactionId: string) => void
}

// Report components
export interface ExpenseLineRowProps {
  line: ExpenseLine
  onEdit: (lineId: string, updates: UpdateLineRequest) => void
  isEditing: boolean
}

export interface ReportPreviewProps {
  report: ExpenseReport
  onExportExcel: () => void
  onExportPdf: () => void
  isExporting: boolean
}

// Layout components
export interface SidebarNavItemProps {
  icon: React.ComponentType<{ className?: string }>
  title: string
  href: string
  isActive: boolean
  badge?: number
}

export interface PageHeaderProps {
  title: string
  description?: string
  actions?: React.ReactNode
}

export interface EmptyStateProps {
  icon: React.ComponentType<{ className?: string }>
  title: string
  description: string
  action?: {
    label: string
    onClick: () => void
  }
}
```

## 6. Router Context Type

```typescript
// src/types/router.ts
import type { QueryClient } from '@tanstack/react-query'
import type { IPublicClientApplication, AccountInfo } from '@azure/msal-browser'

export interface RouterContext {
  queryClient: QueryClient
  msalInstance: IPublicClientApplication
  account: AccountInfo | null
  isAuthenticated: boolean
}

// Augment TanStack Router types
declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}
```

## 7. API Error Types

```typescript
// src/types/errors.ts

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

export interface ProblemDetails {
  title: string
  detail: string
  status?: number
  type?: string
  instance?: string
}
```
