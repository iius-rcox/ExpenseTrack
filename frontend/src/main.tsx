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

// Handle redirect promise on page load
msalInstance.initialize().then(() => {
  // Handle redirect response if coming back from login
  msalInstance.handleRedirectPromise().then((response) => {
    if (response) {
      msalInstance.setActiveAccount(response.account)
    }
  }).catch((error) => {
    console.error('Redirect error:', error)
  })

  // Set active account on login success
  msalInstance.addEventCallback((event: EventMessage) => {
    if (event.eventType === EventType.LOGIN_SUCCESS && event.payload) {
      const payload = event.payload as AuthenticationResult
      msalInstance.setActiveAccount(payload.account)
    }
  })

  // Check if there's already an active account
  const accounts = msalInstance.getAllAccounts()
  if (accounts.length > 0) {
    msalInstance.setActiveAccount(accounts[0])
  }

  // Get the active account for router context
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
})
