/**
 * API client functions for Missing Receipts (Feature 026)
 *
 * Provides typed API functions for managing transactions marked as reimbursable
 * but lacking matched receipts.
 */
import { apiFetch } from '@/services/api'
import type {
  MissingReceiptsListResponse,
  MissingReceiptsWidget,
  MissingReceiptSummary,
  UpdateReceiptUrlRequest,
  DismissReceiptRequest,
} from '@/types/api'

export interface MissingReceiptsParams {
  page?: number
  pageSize?: number
  sortBy?: 'date' | 'amount' | 'vendor'
  sortOrder?: 'asc' | 'desc'
  includeDismissed?: boolean
}

/**
 * Fetches a paginated list of missing receipts.
 * Missing receipts are transactions marked as reimbursable but without matched receipts.
 */
export async function getMissingReceipts(
  params: MissingReceiptsParams = {}
): Promise<MissingReceiptsListResponse> {
  const { page = 1, pageSize = 25, sortBy = 'date', sortOrder = 'desc', includeDismissed = false } = params

  const searchParams = new URLSearchParams()
  searchParams.set('page', String(page))
  searchParams.set('pageSize', String(pageSize))
  searchParams.set('sortBy', sortBy)
  searchParams.set('sortOrder', sortOrder)
  if (includeDismissed) {
    searchParams.set('includeDismissed', 'true')
  }

  return apiFetch<MissingReceiptsListResponse>(`/missing-receipts?${searchParams}`)
}

/**
 * Fetches widget summary data for the missing receipts dashboard card.
 * Returns total count and top 3 most recent items.
 */
export async function getMissingReceiptsWidget(): Promise<MissingReceiptsWidget> {
  return apiFetch<MissingReceiptsWidget>('/missing-receipts/widget')
}

/**
 * Updates the receipt URL for a transaction.
 * Pass null or empty string to clear the URL.
 */
export async function updateReceiptUrl(
  transactionId: string,
  receiptUrl: string | null
): Promise<MissingReceiptSummary> {
  const body: UpdateReceiptUrlRequest = { receiptUrl }

  return apiFetch<MissingReceiptSummary>(`/missing-receipts/${transactionId}/url`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}

/**
 * Dismisses or restores a transaction from the missing receipts list.
 * @param dismiss - true to dismiss, false/null to restore
 */
export async function dismissMissingReceipt(
  transactionId: string,
  dismiss: boolean | null
): Promise<MissingReceiptSummary> {
  const body: DismissReceiptRequest = { dismiss }

  return apiFetch<MissingReceiptSummary>(`/missing-receipts/${transactionId}/dismiss`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
}
