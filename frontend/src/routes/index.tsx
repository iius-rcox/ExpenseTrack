"use client"

import { createFileRoute, redirect } from '@tanstack/react-router'

export const Route = createFileRoute('/')({
  beforeLoad: ({ context }) => {
    // If authenticated, redirect to dashboard
    if (context.isAuthenticated && context.account) {
      throw redirect({ to: '/dashboard' })
    }
    // Otherwise redirect to login
    throw redirect({ to: '/login' })
  },
})
