"use client"

import { createFileRoute, redirect, useNavigate } from '@tanstack/react-router'
import React, { useEffect } from 'react'
import { useMsal } from '@azure/msal-react'
import { Loader2 } from 'lucide-react'
import { isTestModeAuthenticated } from '@/auth/testAuth'

export const Route = createFileRoute('/')({
  beforeLoad: ({ context }) => {
    // IMPORTANT: Don't redirect if this is an MSAL OAuth callback
    // Azure AD returns with code/state in query string (auth code flow) or hash (implicit flow)
    const hash = typeof window !== 'undefined' ? window.location.hash : ''
    const search = typeof window !== 'undefined' ? window.location.search : ''
    if ((hash && (hash.includes('code=') || hash.includes('error='))) ||
        (search && (search.includes('code=') || search.includes('error=')))) {
      // This is an OAuth callback - don't redirect, let MSAL handle it
      // The page will re-render after MSAL processes the response
      return
    }

    // Loop detection: Check if we've been redirecting too many times
    const loopKey = 'auth_redirect_count'
    const loopTimestampKey = 'auth_redirect_timestamp'
    const now = Date.now()
    const lastTimestamp = parseInt(sessionStorage.getItem(loopTimestampKey) || '0', 10)
    let redirectCount = parseInt(sessionStorage.getItem(loopKey) || '0', 10)

    // Reset count if more than 10 seconds have passed
    if (now - lastTimestamp > 10000) {
      redirectCount = 0
    }

    // If we've redirected more than 5 times in 10 seconds, we're in a loop
    if (redirectCount >= 5) {
      console.error('[Auth] Redirect loop detected! Breaking out.')
      sessionStorage.removeItem(loopKey)
      sessionStorage.removeItem(loopTimestampKey)
      // Clear any problematic MSAL state
      localStorage.removeItem('msal.interaction.status')
      // Don't redirect, show the callback handler with an error state
      return
    }

    // Check for E2E test mode authentication first
    if (isTestModeAuthenticated()) {
      throw redirect({ to: '/dashboard' })
    }

    // Check MSAL instance directly for current auth state
    const account = context.msalInstance.getActiveAccount()
    const accounts = context.msalInstance.getAllAccounts()
    const isAuthenticated = !!account || accounts.length > 0

    // If authenticated, redirect to dashboard
    if (isAuthenticated) {
      // Reset loop counter on successful auth
      sessionStorage.removeItem(loopKey)
      sessionStorage.removeItem(loopTimestampKey)
      throw redirect({ to: '/dashboard' })
    }

    // Track redirect for loop detection
    sessionStorage.setItem(loopKey, String(redirectCount + 1))
    sessionStorage.setItem(loopTimestampKey, String(now))

    // Otherwise redirect to login
    throw redirect({ to: '/login' })
  },
  component: OAuthCallbackHandler,
})

// Component that handles the OAuth callback and redirects after MSAL processes
function OAuthCallbackHandler() {
  const navigate = useNavigate()
  const { accounts, instance } = useMsal()
  const [error, setError] = React.useState<string | null>(null)

  useEffect(() => {
    // Check if we broke out of a redirect loop
    const hash = window.location.hash
    const hasOAuthCode = hash.includes('code=')

    // If there's still a code in the URL but no accounts, something went wrong
    if (hasOAuthCode && accounts.length === 0) {
      // Wait a bit for MSAL to process
      const checkTimer = setTimeout(() => {
        if (instance.getAllAccounts().length === 0) {
          setError('Sign-in could not be completed. This may be due to a browser issue or expired session. Please try again.')
          // Clear the hash
          window.history.replaceState(null, '', window.location.pathname)
        }
      }, 2000)

      return () => clearTimeout(checkTimer)
    }

    // MSAL should have already processed the redirect in main.tsx
    // Wait a tick for state to settle, then redirect based on auth status
    const timer = setTimeout(() => {
      if (accounts.length > 0) {
        navigate({ to: '/dashboard', replace: true })
      } else if (!hasOAuthCode) {
        // Only redirect to login if there's no OAuth code being processed
        navigate({ to: '/login', replace: true })
      }
    }, 100)

    return () => clearTimeout(timer)
  }, [accounts, navigate, instance])

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="flex flex-col items-center gap-4 max-w-md text-center p-4">
          <div className="h-12 w-12 rounded-full bg-destructive/10 flex items-center justify-center">
            <span className="text-destructive text-2xl">!</span>
          </div>
          <h2 className="text-lg font-semibold">Sign-in Problem</h2>
          <p className="text-muted-foreground">{error}</p>
          <button
            onClick={() => {
              // Clear all MSAL state and try again
              sessionStorage.clear()
              localStorage.removeItem('msal.interaction.status')
              window.location.href = '/login'
            }}
            className="px-4 py-2 bg-primary text-primary-foreground rounded-md hover:bg-primary/90"
          >
            Try Again
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <div className="flex flex-col items-center gap-4">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
        <p className="text-muted-foreground">Completing sign in...</p>
      </div>
    </div>
  )
}
