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
 * Exports edited expense lines to Excel or PDF without database persistence.
 * POST request sends edited data directly to export endpoint.
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
      const instance = getMsalInstance()
      const accounts = instance.getAllAccounts()

      if (accounts.length === 0) {
        throw new ApiError(401, 'No authenticated account')
      }

      const tokenResponse = await instance.acquireTokenSilent({
        scopes: apiScopes.all,
        account: accounts[0],
      })

      const response = await fetch(
        `${API_BASE_URL}/reports/export/preview/${format}`,
        {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${tokenResponse.idToken}`,
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
