"use client"

import { createFileRoute, redirect, useNavigate } from '@tanstack/react-router'
import { useEffect } from 'react'
import { useMsal } from '@azure/msal-react'
import { Loader2 } from 'lucide-react'

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

    // Check MSAL instance directly for current auth state
    const account = context.msalInstance.getActiveAccount()
    const accounts = context.msalInstance.getAllAccounts()
    const isAuthenticated = !!account || accounts.length > 0

    // If authenticated, redirect to dashboard
    if (isAuthenticated) {
      throw redirect({ to: '/dashboard' })
    }
    // Otherwise redirect to login
    throw redirect({ to: '/login' })
  },
  component: OAuthCallbackHandler,
})

// Component that handles the OAuth callback and redirects after MSAL processes
function OAuthCallbackHandler() {
  const navigate = useNavigate()
  const { accounts } = useMsal()

  useEffect(() => {
    // MSAL should have already processed the redirect in main.tsx
    // Wait a tick for state to settle, then redirect based on auth status
    const timer = setTimeout(() => {
      if (accounts.length > 0) {
        navigate({ to: '/dashboard', replace: true })
      } else {
        navigate({ to: '/login', replace: true })
      }
    }, 100)

    return () => clearTimeout(timer)
  }, [accounts, navigate])

  return (
    <div className="min-h-screen flex items-center justify-center bg-background">
      <div className="flex flex-col items-center gap-4">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
        <p className="text-muted-foreground">Completing sign in...</p>
      </div>
    </div>
  )
}
