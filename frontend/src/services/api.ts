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
    return {
      'Authorization': `Bearer ${response.idToken}`,
      'Content-Type': 'application/json',
    }
  } catch (error) {
    // Token expired, redirect to login
    await instance.loginRedirect({
      scopes: apiScopes.all,
    })
    throw new ApiError(401, 'Token expired, redirecting to login')
  }
}

export async function apiFetch<T>(
  url: string,
  options?: RequestInit
): Promise<T> {
  const headers = await getAuthHeaders()

  const response = await fetch(`${API_BASE_URL}${url}`, {
    ...options,
    headers: { ...headers, ...options?.headers },
  })

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

  return response.json()
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
