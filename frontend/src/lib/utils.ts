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
export function getStatusVariant(status: string): 'default' | 'secondary' | 'destructive' | 'outline' {
  switch (status.toLowerCase()) {
    case 'matched':
    case 'confirmed':
    case 'approved':
    case 'completed':
      return 'default'
    case 'pending':
    case 'processing':
    case 'proposed':
    case 'draft':
      return 'secondary'
    case 'error':
    case 'rejected':
    case 'failed':
      return 'destructive'
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
