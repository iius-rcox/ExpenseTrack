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
 * Detailed warning logger for safeDisplayString.
 * Provides comprehensive debugging information for React Error #301.
 */
function logSafeDisplayWarning(
  type: 'null_undefined' | 'empty_object' | 'non_empty_object' | 'array' | 'function' | 'symbol',
  value: unknown,
  fallback: string,
  context?: string
): void {
  // Skip logging for expected null/undefined without context (not unusual)
  if (type === 'null_undefined' && !context) return

  const contextLabel = context ? ` [${context}]` : ''
  const timestamp = new Date().toISOString()

  // Create detailed log message
  const typeLabels: Record<typeof type, string> = {
    null_undefined: '⚠️ Null/Undefined Value',
    empty_object: '🔴 EMPTY OBJECT (React Error #301 source)',
    non_empty_object: '🟠 Non-Empty Object (cannot render)',
    array: '🟡 Array (cannot render as child)',
    function: '🔵 Function (cannot render)',
    symbol: '🟣 Symbol (cannot render)',
  }

  console.group(`${typeLabels[type]}${contextLabel}`)
  console.warn(`Timestamp: ${timestamp}`)
  console.warn(`Context: ${context || 'Not provided'}`)
  console.warn(`Value Type: ${typeof value}`)
  console.warn(`Fallback Used: "${fallback}"`)

  // Log the actual value with safeguards
  try {
    if (type === 'empty_object' || type === 'non_empty_object') {
      const serialized = JSON.stringify(value, null, 2)
      const truncated = serialized.length > 500
        ? serialized.slice(0, 500) + '... (truncated)'
        : serialized
      console.warn(`Value (JSON):`, truncated)
      console.warn(`Object Keys:`, Object.keys(value as object))
    } else if (type === 'array') {
      console.warn(`Array Length:`, (value as unknown[]).length)
      console.warn(`First 3 Items:`, (value as unknown[]).slice(0, 3))
    } else if (type === 'function') {
      console.warn(`Function Name:`, (value as () => void).name || '(anonymous)')
    } else {
      console.warn(`Raw Value:`, value)
    }
  } catch (e) {
    console.warn(`Value: [Could not serialize - ${e}]`)
  }

  // Add stack trace for debugging (helps identify the source)
  if (type === 'empty_object' || type === 'non_empty_object') {
    console.warn(`Stack Trace (to find data source):`)
    console.trace()
  }

  console.groupEnd()
}
