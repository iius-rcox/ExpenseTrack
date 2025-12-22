/**
 * Analytics Types (T077)
 *
 * Type definitions for the analytics and reporting components.
 * Extends base types from api.ts and dashboard.ts with analytics-specific types.
 */

// Re-export base types that are commonly used in analytics
export type {
  CategoryBreakdownData,
  SpendingTrendPoint,
} from './dashboard'

export type {
  MonthlyComparison,
  VendorSummary,
  VendorChange,
  CategoryBreakdown,
} from './api'

// ============================================================================
// Date Range Types
// ============================================================================

/**
 * Predefined date range options for analytics filtering.
 */
export type DateRangePreset =
  | 'last7days'
  | 'last30days'
  | 'last90days'
  | 'thisMonth'
  | 'lastMonth'
  | 'thisQuarter'
  | 'lastQuarter'
  | 'thisYear'
  | 'lastYear'
  | 'custom'

/**
 * Date range for analytics queries.
 */
export interface AnalyticsDateRange {
  /** Start date (ISO date string YYYY-MM-DD) */
  startDate: string
  /** End date (ISO date string YYYY-MM-DD) */
  endDate: string
  /** Preset that generated this range (if any) */
  preset?: DateRangePreset
  /** Human-readable label for the range */
  label?: string
}

/**
 * Comparison period configuration.
 */
export interface ComparisonPeriod {
  /** The primary date range being analyzed */
  current: AnalyticsDateRange
  /** The comparison date range (typically previous period) */
  previous: AnalyticsDateRange
  /** Type of comparison */
  type: 'period-over-period' | 'year-over-year' | 'custom'
}

// ============================================================================
// Spending Trend Types
// ============================================================================

/**
 * Granularity options for time-series data.
 */
export type TimeGranularity = 'day' | 'week' | 'month' | 'quarter' | 'year'

/**
 * Extended spending trend data with additional analytics.
 */
export interface SpendingTrend {
  /** Period identifier (ISO date or period string) */
  period: string
  /** Display label for the period */
  periodLabel: string
  /** Total spending amount */
  amount: number
  /** Previous period amount for comparison */
  previousAmount?: number
  /** Percentage change from previous period */
  changePercent?: number
  /** Number of transactions */
  transactionCount: number
  /** Average transaction amount */
  averageTransaction: number
  /** Per-category breakdown for this period */
  categories?: CategorySpending[]
}

/**
 * Category spending within a time period.
 */
export interface CategorySpending {
  /** Category name */
  category: string
  /** Category identifier */
  categoryId?: string
  /** Amount spent */
  amount: number
  /** Percentage of total for this period */
  percentage: number
  /** Transaction count */
  transactionCount: number
  /** Color for visualization */
  color?: string
}

/**
 * Response from spending trends API endpoint.
 */
export interface SpendingTrendsResponse {
  /** Time-series data points */
  trends: SpendingTrend[]
  /** Summary statistics */
  summary: {
    totalAmount: number
    averagePerPeriod: number
    highestPeriod: { period: string; amount: number }
    lowestPeriod: { period: string; amount: number }
    transactionCount: number
  }
  /** Granularity used for the data */
  granularity: TimeGranularity
  /** Date range for the data */
  dateRange: AnalyticsDateRange
}

// ============================================================================
// Merchant Analytics Types
// ============================================================================

/**
 * Merchant spending analytics.
 */
export interface TopMerchant {
  /** Merchant/vendor name (normalized) */
  merchantName: string
  /** Alternative display name if set */
  displayName?: string
  /** Total amount spent at this merchant */
  totalAmount: number
  /** Number of transactions */
  transactionCount: number
  /** Average transaction amount */
  averageAmount: number
  /** Percentage of total spending */
  percentageOfTotal: number
  /** Previous period amount (for comparison) */
  previousAmount?: number
  /** Change from previous period */
  changePercent?: number
  /** Primary category for this merchant */
  primaryCategory?: string
  /** Trend direction based on recent activity */
  trend?: 'increasing' | 'decreasing' | 'stable'
  /** Recent transaction dates */
  recentDates?: string[]
}

/**
 * Response from merchant analytics API endpoint.
 */
export interface MerchantAnalyticsResponse {
  /** Top merchants by spend */
  topMerchants: TopMerchant[]
  /** New merchants this period */
  newMerchants: TopMerchant[]
  /** Merchants with significant changes */
  significantChanges: TopMerchant[]
  /** Total unique merchant count */
  totalMerchantCount: number
  /** Date range for the analysis */
  dateRange: AnalyticsDateRange
}

// ============================================================================
// Subscription Detection Types
// ============================================================================

/**
 * Detected recurring charge/subscription frequency.
 */
export type SubscriptionFrequency =
  | 'weekly'
  | 'biweekly'
  | 'monthly'
  | 'quarterly'
  | 'annual'
  | 'unknown'

/**
 * Confidence level for subscription detection.
 */
export type DetectionConfidence = 'high' | 'medium' | 'low'

/**
 * Detected subscription or recurring charge.
 */
export interface SubscriptionDetection {
  /** Unique identifier */
  id: string
  /** Merchant/service name */
  merchantName: string
  /** Detected frequency of charges */
  frequency: SubscriptionFrequency
  /** Typical charge amount */
  amount: number
  /** Amount variance (for variable subscriptions) */
  amountVariance?: number
  /** Detection confidence level */
  confidence: DetectionConfidence
  /** Confidence score (0-1) */
  confidenceScore: number
  /** Category assigned */
  category?: string
  /** Detected on this date */
  detectedAt: string
  /** First occurrence date */
  firstSeen: string
  /** Last occurrence date */
  lastSeen: string
  /** Expected next charge date */
  nextExpected?: string
  /** Number of occurrences used for detection */
  occurrenceCount: number
  /** Total amount spent on this subscription */
  totalSpent: number
  /** Whether user has acknowledged/confirmed this subscription */
  isAcknowledged?: boolean
  /** Whether this is marked as an expected subscription */
  isExpected?: boolean
  /** User notes */
  notes?: string
}

/**
 * Summary of subscription spending.
 */
export interface SubscriptionSummary {
  /** Total detected subscriptions */
  subscriptionCount: number
  /** Total monthly cost (normalized to monthly) */
  estimatedMonthlyTotal: number
  /** Estimated annual cost */
  estimatedAnnualTotal: number
  /** By frequency breakdown */
  byFrequency: {
    frequency: SubscriptionFrequency
    count: number
    monthlyEquivalent: number
  }[]
  /** By category breakdown */
  byCategory: {
    category: string
    count: number
    monthlyEquivalent: number
  }[]
}

/**
 * Response from subscription detection API endpoint.
 */
export interface SubscriptionDetectionResponse {
  /** Detected subscriptions */
  subscriptions: SubscriptionDetection[]
  /** Summary statistics */
  summary: SubscriptionSummary
  /** New subscriptions since last check */
  newSubscriptions: SubscriptionDetection[]
  /** Subscriptions that may have stopped */
  possiblyEnded: SubscriptionDetection[]
  /** Date of last analysis */
  analyzedAt: string
}

// ============================================================================
// Chart Configuration Types
// ============================================================================

/**
 * Chart type options for spending visualization.
 */
export type ChartType = 'line' | 'bar' | 'area' | 'pie' | 'donut' | 'treemap'

/**
 * Configuration for spending charts.
 */
export interface ChartConfig {
  /** Chart type */
  type: ChartType
  /** Show comparison data */
  showComparison?: boolean
  /** Show trendline */
  showTrendline?: boolean
  /** Stack categories (for bar/area charts) */
  stacked?: boolean
  /** Animation duration in ms */
  animationDuration?: number
  /** Show data labels */
  showLabels?: boolean
  /** Show legend */
  showLegend?: boolean
  /** Color scheme */
  colorScheme?: 'default' | 'category' | 'gradient'
}

// ============================================================================
// Component Props Types
// ============================================================================

/**
 * Props for the SpendingTrendChart component.
 */
export interface SpendingTrendChartProps {
  /** Trend data to display */
  data?: SpendingTrend[]
  /** Loading state */
  isLoading?: boolean
  /** Chart configuration */
  config?: Partial<ChartConfig>
  /** Time granularity */
  granularity?: TimeGranularity
  /** Show category breakdown */
  showCategories?: boolean
  /** Selected categories to highlight */
  selectedCategories?: string[]
  /** Height of the chart */
  height?: number
  /** Additional CSS classes */
  className?: string
  /** Click handler for data points */
  onPointClick?: (trend: SpendingTrend) => void
}

/**
 * Props for the CategoryBreakdown component (analytics version).
 */
export interface CategoryAnalyticsProps {
  /** Category data */
  data?: CategorySpending[]
  /** Loading state */
  isLoading?: boolean
  /** Chart type for visualization */
  chartType?: 'pie' | 'donut' | 'bar' | 'treemap'
  /** Show comparison to previous period */
  showComparison?: boolean
  /** Number of categories to show (rest grouped as "Other") */
  maxCategories?: number
  /** Additional CSS classes */
  className?: string
  /** Click handler for category selection */
  onCategorySelect?: (category: CategorySpending) => void
}

/**
 * Props for the MerchantAnalytics component.
 */
export interface MerchantAnalyticsProps {
  /** Merchant data */
  data?: MerchantAnalyticsResponse
  /** Loading state */
  isLoading?: boolean
  /** Number of top merchants to display */
  topCount?: number
  /** Show trend indicators */
  showTrends?: boolean
  /** Sort order */
  sortBy?: 'amount' | 'count' | 'change'
  /** Additional CSS classes */
  className?: string
  /** Click handler for merchant selection */
  onMerchantSelect?: (merchant: TopMerchant) => void
}

/**
 * Props for the SubscriptionDetector component.
 */
export interface SubscriptionDetectorProps {
  /** Subscription data */
  data?: SubscriptionDetectionResponse
  /** Loading state */
  isLoading?: boolean
  /** Group subscriptions by category */
  groupByCategory?: boolean
  /** Show summary at top */
  showSummary?: boolean
  /** Filter by frequency */
  frequencyFilter?: SubscriptionFrequency[]
  /** Additional CSS classes */
  className?: string
  /** Handler for acknowledging subscriptions */
  onAcknowledge?: (subscriptionId: string, acknowledged: boolean) => void
  /** Handler for clicking on a subscription */
  onSubscriptionClick?: (subscription: SubscriptionDetection) => void
}

/**
 * Props for the DateRangePicker component.
 */
export interface DateRangePickerProps {
  /** Current date range */
  value: AnalyticsDateRange
  /** Change handler */
  onChange: (range: AnalyticsDateRange) => void
  /** Available presets */
  presets?: DateRangePreset[]
  /** Allow custom date selection */
  allowCustom?: boolean
  /** Minimum selectable date */
  minDate?: string
  /** Maximum selectable date */
  maxDate?: string
  /** Alignment of the picker */
  align?: 'start' | 'center' | 'end'
  /** Additional CSS classes */
  className?: string
}

/**
 * Props for the AnalyticsDashboard page.
 */
export interface AnalyticsDashboardProps {
  /** Initial date range */
  initialDateRange?: AnalyticsDateRange
  /** Default granularity for charts */
  defaultGranularity?: TimeGranularity
  /** Sections to display */
  sections?: ('trends' | 'categories' | 'merchants' | 'subscriptions')[]
  /** Additional CSS classes */
  className?: string
}

// ============================================================================
// Query Parameter Types
// ============================================================================

/**
 * Query parameters for analytics API requests.
 */
export interface AnalyticsQueryParams {
  /** Start date (ISO) */
  startDate: string
  /** End date (ISO) */
  endDate: string
  /** Time granularity */
  granularity?: TimeGranularity
  /** Categories to include (empty = all) */
  categories?: string[]
  /** Include comparison period data */
  includeComparison?: boolean
}

/**
 * Query parameters for subscription detection.
 */
export interface SubscriptionQueryParams {
  /** Minimum confidence level */
  minConfidence?: DetectionConfidence
  /** Filter by frequency */
  frequency?: SubscriptionFrequency[]
  /** Include acknowledged subscriptions */
  includeAcknowledged?: boolean
  /** Include possibly ended */
  includeEnded?: boolean
}
