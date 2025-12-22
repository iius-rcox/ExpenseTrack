# Data Model: Front-End Redesign with Refined Intelligence Design System

**Feature**: 013-frontend-redesign
**Date**: 2025-12-21
**Purpose**: Define TypeScript types, component props, and state structures for the frontend redesign

---

## Overview

This data model defines the frontend-specific types that power the Refined Intelligence design system. Since this feature consumes existing backend APIs, the focus is on:

1. **View Models**: Transformed API data optimized for rendering
2. **Component Props**: Type-safe interfaces for new components
3. **UI State**: Local state structures for interactions (undo, filters, selections)
4. **Design Tokens**: Type-safe design system values

---

## 1. Design System Types

### 1.1 Theme Tokens

```typescript
// lib/design-tokens.ts

export interface ColorTokens {
  slate: {
    950: string;
    900: string;
    800: string;
    700: string;
    600: string;
    500: string;
    400: string;
    300: string;
    200: string;
    100: string;
    50: string;
  };
  accent: {
    copper: string;
    copperLight: string;
    copperDark: string;
    emerald: string;
    amber: string;
    rose: string;
  };
}

export interface TypographyTokens {
  fontFamily: {
    serif: string;   // Display headings
    sans: string;    // Body text
    mono: string;    // Numbers, dates, code
  };
  fontSize: {
    xs: string;      // 12px
    sm: string;      // 14px
    base: string;    // 16px
    lg: string;      // 18px
    xl: string;      // 20px
    '2xl': string;   // 24px
    '3xl': string;   // 30px
    '4xl': string;   // 36px
  };
  fontWeight: {
    light: number;   // 300
    normal: number;  // 400
    medium: number;  // 500
    semibold: number; // 600
    bold: number;    // 700
  };
}

export interface AnimationTokens {
  duration: {
    instant: number;  // 0ms
    fast: number;     // 150ms
    normal: number;   // 300ms
    slow: number;     // 500ms
  };
  easing: {
    default: string;
    spring: string;
    bounce: string;
  };
}

export interface DesignTokens {
  colors: ColorTokens;
  typography: TypographyTokens;
  animation: AnimationTokens;
}
```

### 1.2 Confidence Levels

```typescript
// types/confidence.ts

export type ConfidenceLevel = 'high' | 'medium' | 'low';

export interface ConfidenceThresholds {
  high: number;    // >= 0.9
  medium: number;  // >= 0.7
  low: number;     // < 0.7
}

export function getConfidenceLevel(score: number): ConfidenceLevel {
  if (score >= 0.9) return 'high';
  if (score >= 0.7) return 'medium';
  return 'low';
}

export interface ConfidenceIndicatorProps {
  score: number;
  showLabel?: boolean;
  size?: 'sm' | 'md' | 'lg';
}
```

---

## 2. Dashboard Types

### 2.1 Dashboard Metrics

```typescript
// types/dashboard.ts

export interface DashboardMetrics {
  monthlyTotal: number;
  monthlyChange: number;        // Percentage change from previous month
  pendingReviewCount: number;
  matchingPercentage: number;
  categorizedPercentage: number;
  recentActivityCount: number;
}

export interface ExpenseStreamItem {
  id: string;
  type: 'receipt' | 'transaction' | 'match';
  title: string;
  amount?: number;
  timestamp: Date;
  status: 'pending' | 'processing' | 'complete' | 'error';
  confidence?: number;
}

export interface ActionQueueItem {
  id: string;
  type: 'review_match' | 'verify_receipt' | 'categorize' | 'resolve_conflict';
  priority: 'high' | 'medium' | 'low';
  title: string;
  description: string;
  createdAt: Date;
  actionUrl: string;
}

export interface CategoryBreakdown {
  category: string;
  amount: number;
  percentage: number;
  transactionCount: number;
  color: string;  // For visualization
}
```

### 2.2 Dashboard Component Props

```typescript
// components/dashboard/types.ts

export interface StatCardProps {
  label: string;
  value: string | number;
  trend?: number;           // Percentage change
  trendDirection?: 'up' | 'down' | 'neutral';
  highlight?: boolean;
  icon?: React.ReactNode;
}

export interface ExpenseStreamProps {
  items: ExpenseStreamItem[];
  isLoading?: boolean;
  onItemClick?: (item: ExpenseStreamItem) => void;
}

export interface ActionQueueProps {
  items: ActionQueueItem[];
  isLoading?: boolean;
  onItemAction?: (item: ActionQueueItem) => void;
}

export interface CategoryBreakdownChartProps {
  data: CategoryBreakdown[];
  height?: number;
  interactive?: boolean;
}
```

---

## 3. Receipt Intelligence Types

### 3.1 Receipt Data

```typescript
// types/receipt.ts

export interface ExtractedField {
  key: 'merchant' | 'amount' | 'date' | 'category' | 'taxAmount' | 'tip';
  value: string | number | Date | null;
  confidence: number;
  boundingBox?: {
    x: number;
    y: number;
    width: number;
    height: number;
  };
  isEdited: boolean;
  originalValue?: string | number | Date | null;
}

export interface ReceiptPreview {
  id: string;
  imageUrl: string;
  thumbnailUrl: string;
  uploadedAt: Date;
  status: 'uploading' | 'processing' | 'complete' | 'error';
  processingProgress?: number;  // 0-100
  extractedFields: ExtractedField[];
  matchedTransactionId?: string;
}

export interface ReceiptUploadState {
  file: File;
  preview: string;           // Object URL for preview
  progress: number;          // 0-100
  status: 'pending' | 'uploading' | 'processing' | 'complete' | 'error';
  error?: string;
  receiptId?: string;        // Assigned after upload
}
```

### 3.2 Receipt Component Props

```typescript
// components/receipts/types.ts

export interface ReceiptCardProps {
  receipt: ReceiptPreview;
  isSelected?: boolean;
  onSelect?: () => void;
  onEdit?: () => void;
  onDelete?: () => void;
  showConfidence?: boolean;
}

export interface ReceiptIntelligencePanelProps {
  receipt: ReceiptPreview;
  onFieldEdit: (field: ExtractedField, newValue: string | number | Date) => void;
  onUndo?: () => void;
  canUndo?: boolean;
}

export interface ReceiptUploadDropzoneProps {
  onUpload: (files: File[]) => void;
  isUploading?: boolean;
  maxFiles?: number;
  maxSize?: number;          // in bytes
  acceptedTypes?: string[];
}

export interface BatchUploadQueueProps {
  uploads: ReceiptUploadState[];
  onCancel?: (index: number) => void;
  onRetry?: (index: number) => void;
}
```

---

## 4. Transaction Explorer Types

### 4.1 Transaction Data

```typescript
// types/transaction.ts

export interface TransactionView {
  id: string;
  date: Date;
  description: string;
  merchant: string;
  amount: number;
  category: string;
  categoryId: string;
  tags: string[];
  notes: string;
  matchStatus: 'matched' | 'pending' | 'unmatched' | 'manual';
  matchedReceiptId?: string;
  matchConfidence?: number;
  source: 'import' | 'manual' | 'api';
  isEditing?: boolean;
}

export interface TransactionFilters {
  search: string;
  dateRange: {
    start: Date | null;
    end: Date | null;
  };
  categories: string[];
  amountRange: {
    min: number | null;
    max: number | null;
  };
  matchStatus: ('matched' | 'pending' | 'unmatched' | 'manual')[];
  tags: string[];
}

export interface TransactionSortConfig {
  field: 'date' | 'amount' | 'merchant' | 'category';
  direction: 'asc' | 'desc';
}

export interface TransactionSelectionState {
  selectedIds: Set<string>;
  lastSelectedId: string | null;
  isSelectAll: boolean;
}
```

### 4.2 Transaction Component Props

```typescript
// components/transactions/types.ts

export interface TransactionGridProps {
  transactions: TransactionView[];
  isLoading?: boolean;
  filters: TransactionFilters;
  sort: TransactionSortConfig;
  selection: TransactionSelectionState;
  onFilterChange: (filters: TransactionFilters) => void;
  onSortChange: (sort: TransactionSortConfig) => void;
  onSelectionChange: (selection: TransactionSelectionState) => void;
  onTransactionEdit: (id: string, updates: Partial<TransactionView>) => void;
  onTransactionClick?: (transaction: TransactionView) => void;
}

export interface TransactionRowProps {
  transaction: TransactionView;
  isSelected: boolean;
  isEditing: boolean;
  onSelect: (shiftKey: boolean) => void;
  onEdit: (updates: Partial<TransactionView>) => void;
  onClick: () => void;
}

export interface BulkActionsBarProps {
  selectedCount: number;
  onCategorize: (categoryId: string) => void;
  onTag: (tags: string[]) => void;
  onExport: () => void;
  onDelete: () => void;
  onClearSelection: () => void;
}

export interface TransactionFilterPanelProps {
  filters: TransactionFilters;
  categories: { id: string; name: string }[];
  tags: string[];
  onChange: (filters: TransactionFilters) => void;
  onReset: () => void;
}
```

---

## 5. Match Review Types

### 5.1 Match Data

```typescript
// types/match.ts

export interface MatchSuggestion {
  id: string;
  receipt: ReceiptPreview;
  transaction: TransactionView;
  confidence: number;
  matchingFactors: MatchingFactor[];
  status: 'pending' | 'approved' | 'rejected';
  reviewedAt?: Date;
}

export interface MatchingFactor {
  type: 'amount' | 'date' | 'merchant' | 'category';
  weight: number;           // 0-1 contribution to overall score
  receiptValue: string;
  transactionValue: string;
  isExactMatch: boolean;
}

export interface MatchReviewState {
  currentIndex: number;
  queue: MatchSuggestion[];
  batchMode: boolean;
  batchThreshold: number;   // Auto-approve above this confidence
}
```

### 5.2 Match Component Props

```typescript
// components/matching/types.ts

export interface MatchReviewWorkspaceProps {
  match: MatchSuggestion;
  position: {
    current: number;
    total: number;
  };
  onApprove: () => void;
  onReject: () => void;
  onNext: () => void;
  onPrevious: () => void;
  onManualMatch: () => void;
}

export interface MatchComparisonViewProps {
  receipt: ReceiptPreview;
  transaction: TransactionView;
  matchingFactors: MatchingFactor[];
  confidence: number;
}

export interface BatchReviewPanelProps {
  matches: MatchSuggestion[];
  threshold: number;
  onThresholdChange: (value: number) => void;
  onApproveAll: () => void;
  onRejectAll: () => void;
  eligibleCount: number;    // Matches above threshold
}

export interface ManualMatchDialogProps {
  receipt: ReceiptPreview;
  transactions: TransactionView[];
  isOpen: boolean;
  onClose: () => void;
  onMatch: (transactionId: string) => void;
  searchQuery: string;
  onSearchChange: (query: string) => void;
}
```

---

## 6. Analytics Types

### 6.1 Analytics Data

```typescript
// types/analytics.ts

export interface SpendingTrend {
  period: Date;
  amount: number;
  transactionCount: number;
  categoryBreakdown: CategoryBreakdown[];
}

export interface TopMerchant {
  merchant: string;
  totalAmount: number;
  transactionCount: number;
  averageAmount: number;
  trend: number;            // Percentage change
}

export interface SubscriptionDetection {
  merchant: string;
  estimatedAmount: number;
  frequency: 'weekly' | 'monthly' | 'quarterly' | 'yearly';
  lastCharge: Date;
  nextExpected: Date;
  confidence: number;
}

export interface AnalyticsDateRange {
  start: Date;
  end: Date;
  preset?: 'week' | 'month' | 'quarter' | 'year' | 'custom';
}
```

### 6.2 Analytics Component Props

```typescript
// components/analytics/types.ts

export interface SpendingTrendChartProps {
  data: SpendingTrend[];
  dateRange: AnalyticsDateRange;
  showCategoryBreakdown?: boolean;
  height?: number;
}

export interface CategoryTreemapProps {
  data: CategoryBreakdown[];
  onCategoryClick?: (category: string) => void;
  height?: number;
}

export interface TopMerchantsListProps {
  merchants: TopMerchant[];
  limit?: number;
  onMerchantClick?: (merchant: string) => void;
}

export interface SubscriptionListProps {
  subscriptions: SubscriptionDetection[];
  onSubscriptionClick?: (subscription: SubscriptionDetection) => void;
}

export interface DateRangeSelectorProps {
  value: AnalyticsDateRange;
  onChange: (range: AnalyticsDateRange) => void;
  presets?: ('week' | 'month' | 'quarter' | 'year')[];
}
```

---

## 7. UI State Types

### 7.1 Undo Stack

```typescript
// hooks/ui/types.ts

export interface UndoState<T> {
  current: T;
  history: T[];
  pointer: number;
  canUndo: boolean;
  canRedo: boolean;
}

export interface UndoActions<T> {
  push: (value: T) => void;
  undo: () => void;
  redo: () => void;
  reset: (value: T) => void;
}

export type UseUndoReturn<T> = UndoState<T> & UndoActions<T>;
```

### 7.2 Polling State

```typescript
// hooks/ui/types.ts

export interface PollingConfig {
  interval: number;          // milliseconds
  enabled: boolean;
  pauseOnHidden: boolean;    // Pause when tab is not visible
}

export interface PollingState {
  isPolling: boolean;
  lastPolledAt: Date | null;
  nextPollAt: Date | null;
  error: Error | null;
}
```

### 7.3 Loading States

```typescript
// types/loading.ts

export type LoadingState = 'idle' | 'loading' | 'success' | 'error';

export interface AsyncState<T> {
  data: T | null;
  status: LoadingState;
  error: Error | null;
  isLoading: boolean;
  isSuccess: boolean;
  isError: boolean;
}
```

---

## 8. Entity Relationships

```text
┌─────────────────┐       ┌─────────────────┐
│  DashboardMetrics│       │  ExpenseStream  │
│                 │◄──────│  Items[]        │
└────────┬────────┘       └─────────────────┘
         │
         │ aggregates
         ▼
┌─────────────────┐       ┌─────────────────┐
│  TransactionView│◄──────│  MatchSuggestion│
│                 │       │                 │
│  - matchStatus  │       │  - confidence   │
│  - receiptId    │       │  - factors[]    │
└────────┬────────┘       └────────┬────────┘
         │                         │
         │ links to                │ pairs with
         ▼                         ▼
┌─────────────────┐       ┌─────────────────┐
│  ReceiptPreview │◄──────┤                 │
│                 │       │                 │
│  - extractedFields[]    │                 │
│  - confidence   │       │                 │
└─────────────────┘       └─────────────────┘

┌─────────────────┐
│  AnalyticsData  │
│                 │
│  - trends[]     │──────► SpendingTrend[]
│  - categories[] │──────► CategoryBreakdown[]
│  - merchants[]  │──────► TopMerchant[]
│  - subscriptions│──────► SubscriptionDetection[]
└─────────────────┘
```

---

## Validation Rules

| Entity | Field | Rule |
|--------|-------|------|
| ReceiptUploadState | file.size | ≤ 20MB |
| ReceiptUploadState | file.type | JPEG, PNG, HEIC, PDF |
| ExtractedField | confidence | 0.0 - 1.0 |
| TransactionFilters | dateRange | start ≤ end |
| TransactionFilters | amountRange | min ≤ max |
| MatchSuggestion | confidence | 0.0 - 1.0 |
| BatchReviewPanel | threshold | 0.0 - 1.0 |
| AnalyticsDateRange | range | start < end, max 1 year |
