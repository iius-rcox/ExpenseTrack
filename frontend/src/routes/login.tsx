"use client"

import { createFileRoute } from '@tanstack/react-router'
import { useMsal } from '@azure/msal-react'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { loginRequest } from '@/auth/authConfig'

const loginSearchSchema = z.object({
  redirect: z.string().optional(),
})

export const Route = createFileRoute('/login')({
  validateSearch: loginSearchSchema,
  component: LoginPage,
})

function LoginPage() {
  const { instance } = useMsal()
  const { redirect: redirectPath } = Route.useSearch()

  const handleLogin = async () => {
    try {
      await instance.loginRedirect({
        ...loginRequest,
        redirectStartPage: redirectPath || '/',
      })
    } catch (error) {
      console.error('Login failed:', error)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl font-bold">ExpenseFlow</CardTitle>
          <CardDescription>
            Sign in with your Microsoft account to access the expense management system
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button onClick={handleLogin} className="w-full" size="lg">
            Sign in with Microsoft
          </Button>
        </CardContent>
      </Card>
    </div>
  )
}
