"use client"

import { createFileRoute, redirect } from '@tanstack/react-router'
import { AppShell } from '@/components/layout/app-shell'

export const Route = createFileRoute('/_authenticated')({
  beforeLoad: async ({ context }) => {
    // Check MSAL instance directly for current auth state
    // The static context.isAuthenticated may be stale after redirect
    const account = context.msalInstance.getActiveAccount()
    const accounts = context.msalInstance.getAllAccounts()

    // User is authenticated if there's an active account OR any accounts exist
    const isAuthenticated = !!account || accounts.length > 0

    if (!isAuthenticated) {
      throw redirect({
        to: '/login',
        search: {
          redirect: window.location.pathname,
        },
      })
    }

    // Ensure active account is set if we have accounts
    if (!account && accounts.length > 0) {
      context.msalInstance.setActiveAccount(accounts[0])
    }
  },
  component: AuthenticatedLayout,
})

function AuthenticatedLayout() {
  return <AppShell />
}
