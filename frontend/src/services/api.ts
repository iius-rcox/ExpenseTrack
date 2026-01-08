import { PublicClientApplication } from '@azure/msal-browser'
import { msalConfig, apiScopes } from '@/auth/authConfig'
import { ApiError, type ProblemDetails } from '@/types/api'

// MSAL instance - shared across the app
let msalInstance: PublicClientApplication | null = null

export function setMsalInstance(instance: PublicClientApplication) {
  msalInstance = instance
}

export function getMsalInstance(): PublicClientApplication {
  if (!msalInstance) {
    // Create a new instance if one doesn't exist
    msalInstance = new PublicClientApplication(msalConfig)
  }
  return msalInstance
}

// Get API base URL from environment or default
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api'

async function getAuthHeaders(): Promise<HeadersInit> {
  const instance = getMsalInstance()
  const accounts = instance.getAllAccounts()

  if (accounts.length === 0) {
    throw new ApiError(401, 'No authenticated account')
  }

  try {
    const response = await instance.acquireTokenSilent({
      scopes: apiScopes.all,
      account: accounts[0],
    })
    // Use idToken since backend has AllowWebApiToBeAuthorizedByACL=true
    // ID token has aud=clientId which matches backend Audience config
    return {
      'Authorization': `Bearer ${response.idToken}`,
      'Content-Type': 'application/json',
    }
  } catch {
    // Token expired, redirect to login
    await instance.loginRedirect({
      scopes: apiScopes.all,
    })
    throw new ApiError(401, 'Token expired, redirecting to login')
  }
}

interface ApiFetchOptions extends RequestInit {
  /** Request timeout in milliseconds. Default: 30000 (30s) */
  timeout?: number
}

export async function apiFetch<T>(
  url: string,
  options?: ApiFetchOptions
): Promise<T> {
  const headers = await getAuthHeaders()
  const { timeout = 30000, ...fetchOptions } = options ?? {}

  // Create abort controller for timeout
  const controller = new AbortController()
  const timeoutId = setTimeout(() => controller.abort(), timeout)

  try {
    const response = await fetch(`${API_BASE_URL}${url}`, {
      ...fetchOptions,
      headers: { ...headers, ...fetchOptions?.headers },
      signal: controller.signal,
    })
    clearTimeout(timeoutId)

    if (!response.ok) {
      let errorDetail = response.statusText
      let errorTitle: string | undefined

      try {
        const problemDetails: ProblemDetails = await response.json()
        errorDetail = problemDetails.detail
        errorTitle = problemDetails.title
      } catch {
        // If we can't parse the error response, use the status text
        try {
          errorDetail = await response.text()
        } catch {
          // Ignore
        }
      }

      throw new ApiError(response.status, errorDetail, errorTitle)
    }

    // Handle 204 No Content
    if (response.status === 204) {
      return undefined as T
    }

    const json = await response.json()

    // DEBUG: Log raw API response for proposals endpoint
    if (url.includes('/matching/proposals')) {
      console.log('[apiFetch] Raw proposals response:', JSON.stringify(json, null, 2))
    }

    return json
  } catch (error) {
    clearTimeout(timeoutId)
    if (error instanceof Error && error.name === 'AbortError') {
      throw new ApiError(408, 'Request timed out')
    }
    throw error
  }
}

// File upload with FormData
export async function apiUpload<T>(
  url: string,
  formData: FormData,
  onProgress?: (progress: number) => void
): Promise<T> {
  const instance = getMsalInstance()
  const accounts = instance.getAllAccounts()

  if (accounts.length === 0) {
    throw new ApiError(401, 'No authenticated account')
  }

  const response = await instance.acquireTokenSilent({
    scopes: apiScopes.all,
    account: accounts[0],
  })

  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest()

    xhr.upload.addEventListener('progress', (event) => {
      if (event.lengthComputable && onProgress) {
        const progress = (event.loaded / event.total) * 100
        onProgress(progress)
      }
    })

    xhr.addEventListener('load', () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        try {
          resolve(JSON.parse(xhr.responseText))
        } catch {
          resolve(undefined as T)
        }
      } else {
        let errorDetail = xhr.statusText
        try {
          const problemDetails: ProblemDetails = JSON.parse(xhr.responseText)
          errorDetail = problemDetails.detail
        } catch {
          errorDetail = xhr.responseText || xhr.statusText
        }
        reject(new ApiError(xhr.status, errorDetail))
      }
    })

    xhr.addEventListener('error', () => {
      reject(new ApiError(0, 'Network error'))
    })

    xhr.open('POST', `${API_BASE_URL}${url}`)
    // Use idToken for consistency with getAuthHeaders()
    xhr.setRequestHeader('Authorization', `Bearer ${response.idToken}`)
    xhr.send(formData)
  })
}

// File download - returns blob URL
export async function apiDownload(url: string, filename: string): Promise<void> {
  const instance = getMsalInstance()
  const accounts = instance.getAllAccounts()

  if (accounts.length === 0) {
    throw new ApiError(401, 'No authenticated account')
  }

  const response = await instance.acquireTokenSilent({
    scopes: apiScopes.all,
    account: accounts[0],
  })

  const fetchResponse = await fetch(`${API_BASE_URL}${url}`, {
    headers: {
      // Use idToken for consistency with getAuthHeaders()
      'Authorization': `Bearer ${response.idToken}`,
    },
  })

  if (!fetchResponse.ok) {
    throw new ApiError(fetchResponse.status, fetchResponse.statusText)
  }

  const blob = await fetchResponse.blob()
  const blobUrl = window.URL.createObjectURL(blob)

  const link = document.createElement('a')
  link.href = blobUrl
  link.download = filename
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)
  window.URL.revokeObjectURL(blobUrl)
}
