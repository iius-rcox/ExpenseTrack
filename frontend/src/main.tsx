import ReactDOM from 'react-dom/client'
import { StrictMode } from 'react'
import { PublicClientApplication, EventType, EventMessage, AuthenticationResult } from '@azure/msal-browser'
import { MsalProvider } from '@azure/msal-react'
import { QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from '@tanstack/react-router'
import { msalConfig } from './auth/authConfig'
import { queryClient } from './lib/query-client'
import { router } from './router'
import { setMsalInstance } from './services/api'
import './globals.css'

// Create MSAL instance
const msalInstance = new PublicClientApplication(msalConfig)

// Share MSAL instance with API service
setMsalInstance(msalInstance)

// Initialize MSAL and handle authentication
async function initializeApp() {
  // Initialize MSAL instance
  await msalInstance.initialize()

  // Check if this is an OAuth callback (code in URL)
  const hash = window.location.hash
  const isOAuthCallback = hash.includes('code=') || hash.includes('error=')

  // CRITICAL: Wait for redirect promise to complete before rendering
  // This handles the case when the user returns from Microsoft login
  try {
    const response = await msalInstance.handleRedirectPromise()
    if (response) {
      // User just logged in via redirect - set them as active
      msalInstance.setActiveAccount(response.account)
      console.info('[Auth] Login successful, account set:', response.account?.username)

      // Clear the hash to prevent re-processing
      if (window.location.hash) {
        window.history.replaceState(null, '', window.location.pathname + window.location.search)
      }
    } else if (isOAuthCallback) {
      // We had an OAuth callback but got no response - this means MSAL couldn't process it
      // This can happen due to state mismatch (localStorage cleared, different browser tab, etc.)
      console.warn('[Auth] OAuth callback detected but handleRedirectPromise returned null')
      console.warn('[Auth] This usually means state mismatch - clearing hash and redirecting to login')

      // Clear the problematic hash to break the loop
      window.history.replaceState(null, '', window.location.pathname)

      // Small delay to ensure state is cleared before potential redirect
      await new Promise(resolve => setTimeout(resolve, 100))
    }
  } catch (error) {
    console.error('[Auth] Redirect error:', error)

    // Clear hash on error to prevent infinite loop
    if (window.location.hash) {
      window.history.replaceState(null, '', window.location.pathname)
    }
  }

  // Set active account on login success (for popup flow, if used later)
  msalInstance.addEventCallback((event: EventMessage) => {
    if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
      const payload = event.payload as AuthenticationResult
      msalInstance.setActiveAccount(payload.account)
    }
  })

  // Check if there's already an active account (returning user with cached session)
  const accounts = msalInstance.getAllAccounts()
  if (accounts.length > 0 && !msalInstance.getActiveAccount()) {
    msalInstance.setActiveAccount(accounts[0])
  }

  // Get the active account for router context - now properly set after redirect handling
  const account = msalInstance.getActiveAccount()

  // Render app
  ReactDOM.createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <MsalProvider instance={msalInstance}>
          <RouterProvider
            router={router}
            context={{
              queryClient,
              msalInstance,
              account,
              isAuthenticated: !!account,
            }}
          />
        </MsalProvider>
      </QueryClientProvider>
    </StrictMode>
  )
}

// Start the app
initializeApp()
