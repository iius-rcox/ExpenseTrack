import { type ClassValue, clsx } from 'clsx'
import { twMerge } from 'tailwind-merge'
import { format, parseISO, formatDistanceToNow } from 'date-fns'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

// Currency formatting
export function formatCurrency(amount: number, currency: string = 'USD'): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
  }).format(amount)
}

// Date formatting
export function formatDate(dateString: string | null | undefined): string {
  if (!dateString) return '-'
  try {
    return format(parseISO(dateString), 'MMM d, yyyy')
  } catch {
    return dateString
  }
}

export function formatDateTime(dateString: string | null | undefined): string {
  if (!dateString) return '-'
  try {
    return format(parseISO(dateString), 'MMM d, yyyy h:mm a')
  } catch {
    return dateString
  }
}

export function formatRelativeTime(dateString: string | null | undefined): string {
  if (!dateString) return '-'
  try {
    return formatDistanceToNow(parseISO(dateString), { addSuffix: true })
  } catch {
    return dateString
  }
}

// Percentage formatting
export function formatPercentage(value: number, decimals: number = 0): string {
  return `${(value * 100).toFixed(decimals)}%`
}

// File size formatting
export function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 Bytes'
  const k = 1024
  const sizes = ['Bytes', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

// Status badge variants
export function getStatusVariant(status: string | number): 'default' | 'secondary' | 'destructive' | 'outline' {
  // Handle numeric status values (backward compatibility)
  const statusStr = String(status).toLowerCase()
  switch (statusStr) {
    case 'matched':
    case 'confirmed':
    case 'approved':
    case 'completed':
    case 'ready':
      return 'default'
    case 'pending':
    case 'processing':
    case 'proposed':
    case 'draft':
    case 'uploaded':
      return 'secondary'
    case 'error':
    case 'rejected':
    case 'failed':
      return 'destructive'
    case 'reviewrequired':
    case 'unmatched':
      return 'outline'
    default:
      return 'outline'
  }
}

// Confidence score color
export function getConfidenceColor(score: number): string {
  if (score >= 0.8) return 'text-green-600'
  if (score >= 0.5) return 'text-yellow-600'
  return 'text-red-600'
}

// Truncate text with ellipsis
export function truncate(text: string, maxLength: number): string {
  if (text.length <= maxLength) return text
  return text.slice(0, maxLength - 3) + '...'
}

// Period formatting (YYYY-MM to readable)
export function formatPeriod(period: string): string {
  try {
    const [year, month] = period.split('-')
    const date = new Date(parseInt(year), parseInt(month) - 1, 1)
    return format(date, 'MMMM yyyy')
  } catch {
    return period
  }
}

// Get current period as YYYY-MM
export function getCurrentPeriod(): string {
  return format(new Date(), 'yyyy-MM')
}

// Get previous period as YYYY-MM
export function getPreviousPeriod(period?: string): string {
  const date = period ? parseISO(`${period}-01`) : new Date()
  date.setMonth(date.getMonth() - 1)
  return format(date, 'yyyy-MM')
}

// Report status badge variants
export function getReportStatusVariant(status: string): 'default' | 'secondary' | 'destructive' | 'outline' {
  switch (status) {
    case 'Approved':
      return 'default'
    case 'Draft':
      return 'outline'
    case 'Submitted':
      return 'secondary'
    case 'Rejected':
      return 'destructive'
    default:
      return 'outline'
  }
}

/**
 * DEFENSIVE HELPER: Safely convert any value to a displayable string.
 * Guards against React Error #301 where objects might be rendered as children.
 *
 * @param value - The value to convert to a string
 * @param fallback - Fallback string if value is invalid (default: '')
 * @param context - Optional context for debugging (e.g., 'group.name', 'category.id')
 * @returns A safe string that can be rendered in React
 *
 * @example
 * // Basic usage
 * safeDisplayString(user.name) // "John"
 * safeDisplayString(undefined) // ""
 *
 * // With context for debugging
 * safeDisplayString(group.name, 'Unknown Group', 'TransactionGroupRow.group.name')
 */
export function safeDisplayString(
  value: unknown,
  fallback: string = '',
  context?: string
): string {
  // Fast path for valid primitives
  if (typeof value === 'string') return value
  if (typeof value === 'number' || typeof value === 'boolean') return String(value)

  // Handle null/undefined
  if (value === null || value === undefined) {
    logSafeDisplayWarning('null_undefined', value, fallback, context)
    return fallback
  }

  // Handle Date objects (valid for String conversion)
  if (value instanceof Date) {
    return isNaN(value.getTime()) ? fallback : value.toISOString()
  }

  // Handle arrays (common mistake - arrays should not be rendered as children)
  if (Array.isArray(value)) {
    logSafeDisplayWarning('array', value, fallback, context)
    return fallback
  }

  // Handle objects (the main source of React Error #301)
  if (typeof value === 'object') {
    const keys = Object.keys(value as object)
    if (keys.length === 0) {
      logSafeDisplayWarning('empty_object', value, fallback, context)
    } else {
      logSafeDisplayWarning('non_empty_object', value, fallback, context)
    }
    return fallback
  }

  // Handle functions (should never be rendered)
  if (typeof value === 'function') {
    logSafeDisplayWarning('function', value, fallback, context)
    return fallback
  }

  // Handle symbols
  if (typeof value === 'symbol') {
    logSafeDisplayWarning('symbol', value, fallback, context)
    return fallback
  }

  // Fallback for any other type
  return String(value)
}

/**
 * Warning logger for safeDisplayString (disabled in production).
 * Re-enable console statements for debugging React Error #301 issues.
 */
function logSafeDisplayWarning(
  _type: 'null_undefined' | 'empty_object' | 'non_empty_object' | 'array' | 'function' | 'symbol',
  _value: unknown,
  _fallback: string,
  _context?: string
): void {
  // No-op in production - re-enable for debugging if needed
}

/**
 * DEFENSIVE HELPER: Safely convert any value to a displayable number.
 * Guards against React Error #301 where objects might be rendered as children.
 *
 * This is particularly important for numeric fields that might become {}
 * due to serialization issues (e.g., transactionCount, combinedAmount).
 *
 * @param value - The value to convert to a number for display
 * @param fallback - Fallback number if value is invalid (default: 0)
 * @param context - Optional context for debugging (e.g., 'group.transactionCount')
 * @returns A safe number that can be rendered in React
 *
 * @example
 * // Basic usage
 * safeDisplayNumber(group.transactionCount) // 5
 * safeDisplayNumber(undefined) // 0
 * safeDisplayNumber({}) // 0 (logs warning)
 *
 * // With context for debugging
 * safeDisplayNumber(group.transactionCount, 0, 'TransactionGroupRow.transactionCount')
 */
export function safeDisplayNumber(
  value: unknown,
  fallback: number = 0,
  context?: string
): number {
  // Fast path for valid numbers
  if (typeof value === 'number' && !isNaN(value)) return value

  // Handle string numbers
  if (typeof value === 'string') {
    const parsed = Number(value)
    if (!isNaN(parsed)) return parsed
    logSafeNumberWarning('invalid_string', value, fallback, context)
    return fallback
  }

  // Handle null/undefined
  if (value === null || value === undefined) {
    if (context) {
      logSafeNumberWarning('null_undefined', value, fallback, context)
    }
    return fallback
  }

  // Handle NaN
  if (typeof value === 'number' && isNaN(value)) {
    logSafeNumberWarning('nan', value, fallback, context)
    return fallback
  }

  // Handle objects (the main source of React Error #301)
  if (typeof value === 'object') {
    const keys = Object.keys(value as object)
    if (keys.length === 0) {
      logSafeNumberWarning('empty_object', value, fallback, context)
    } else {
      logSafeNumberWarning('non_empty_object', value, fallback, context)
    }
    return fallback
  }

  // Handle boolean
  if (typeof value === 'boolean') {
    return value ? 1 : 0
  }

  // Fallback for any other type
  logSafeNumberWarning('unknown_type', value, fallback, context)
  return fallback
}

/**
 * Warning logger for safeDisplayNumber (disabled in production).
 * Re-enable console statements for debugging React Error #301 issues.
 */
function logSafeNumberWarning(
  _type: 'null_undefined' | 'empty_object' | 'non_empty_object' | 'nan' | 'invalid_string' | 'unknown_type',
  _value: unknown,
  _fallback: number,
  _context?: string
): void {
  // No-op in production - re-enable for debugging if needed
}
