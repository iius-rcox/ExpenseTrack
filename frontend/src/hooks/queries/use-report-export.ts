import { useMutation } from '@tanstack/react-query'
import { getMsalInstance } from '@/services/api'
import { apiScopes } from '@/auth/authConfig'
import { ApiError } from '@/types/api'
import type { ExportPreviewRequest } from '@/types/report-editor'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api'

/**
 * Downloads a file from a blob response.
 */
async function downloadBlob(blob: Blob, filename: string) {
  const blobUrl = window.URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = blobUrl
  link.download = filename
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  window.URL.revokeObjectURL(blobUrl)
}

/**
 * Gets auth token for API calls.
 */
async function getAuthToken() {
  const instance = getMsalInstance()
  const accounts = instance.getAllAccounts()

  if (accounts.length === 0) {
    throw new ApiError(401, 'No authenticated account')
  }

  const tokenResponse = await instance.acquireTokenSilent({
    scopes: apiScopes.all,
    account: accounts[0],
  })

  return tokenResponse.idToken
}

/**
 * Exports edited expense lines to Excel or PDF without database persistence.
 * POST request sends edited data directly to export endpoint.
 */
/**
 * Exports edited expense lines to Excel or PDF without database persistence.
 * POST request sends edited data directly to export endpoint.
 * Use this for preview exports (no saved draft) or Excel exports.
 */
export function useExportPreview() {
  return useMutation({
    mutationFn: async ({
      request,
      format,
    }: {
      request: ExportPreviewRequest
      format: 'excel' | 'pdf'
    }) => {
      const token = await getAuthToken()

      const response = await fetch(
        `${API_BASE_URL}/reports/export/preview/${format}`,
        {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(request),
        }
      )

      if (!response.ok) {
        let errorDetail = response.statusText
        try {
          const problemDetails = await response.json()
          errorDetail = problemDetails.detail || problemDetails.title
        } catch {
          // Ignore JSON parse errors
        }
        throw new ApiError(response.status, errorDetail)
      }

      const blob = await response.blob()
      const filename = `expense-report-${request.period}.${format === 'excel' ? 'xlsx' : 'pdf'}`

      await downloadBlob(blob, filename)

      return { filename, format }
    },
  })
}

/**
 * Exports a complete PDF report with itemized expenses AND receipt images.
 * Use this when a draft report exists and you want the full PDF with receipts.
 * The complete PDF includes:
 * - Itemized expense list with split allocations
 * - Receipt images with cross-reference numbers
 * - Missing receipt placeholders where applicable
 */
export function useExportCompletePdf() {
  return useMutation({
    mutationFn: async ({
      reportId,
      period,
    }: {
      reportId: string
      period: string
    }) => {
      const token = await getAuthToken()

      const response = await fetch(
        `${API_BASE_URL}/reports/${reportId}/export/complete`,
        {
          method: 'GET',
          headers: {
            'Authorization': `Bearer ${token}`,
          },
        }
      )

      if (!response.ok) {
        let errorDetail = response.statusText
        try {
          const problemDetails = await response.json()
          errorDetail = problemDetails.detail || problemDetails.title
        } catch {
          // Ignore JSON parse errors
        }
        throw new ApiError(response.status, errorDetail)
      }

      const blob = await response.blob()
      const filename = `expense-report-${period}-complete.pdf`

      await downloadBlob(blob, filename)

      return { filename, format: 'pdf' as const }
    },
  })
}
