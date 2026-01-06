'use client'

/**
 * MissingReceiptsErrorBoundary Component (T034)
 *
 * Error boundary for the missing receipts feature.
 * Catches errors and provides a user-friendly fallback with retry option.
 *
 * Part of Feature 026: Missing Receipts UI - Polish Phase
 */

import { Component, type ReactNode } from 'react'
import { useQueryErrorResetBoundary } from '@tanstack/react-query'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { AlertTriangle, RefreshCcw, Receipt } from 'lucide-react'

interface ErrorBoundaryState {
  hasError: boolean
  error: Error | null
}

interface ErrorBoundaryProps {
  children: ReactNode
  fallback?: ReactNode
}

/**
 * Class-based error boundary for catching render errors.
 * Wraps the QueryErrorResetBoundary for proper TanStack Query integration.
 */
class ErrorBoundaryInner extends Component<
  ErrorBoundaryProps & { onReset: () => void },
  ErrorBoundaryState
> {
  constructor(props: ErrorBoundaryProps & { onReset: () => void }) {
    super(props)
    this.state = { hasError: false, error: null }
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error }
  }

  componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
    console.error('MissingReceipts Error:', error, errorInfo)
  }

  handleReset = () => {
    this.props.onReset()
    this.setState({ hasError: false, error: null })
  }

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) {
        return this.props.fallback
      }

      return (
        <MissingReceiptsErrorFallback
          error={this.state.error}
          resetErrorBoundary={this.handleReset}
        />
      )
    }

    return this.props.children
  }
}

/**
 * Fallback UI when an error occurs in the missing receipts feature.
 */
export function MissingReceiptsErrorFallback({
  error,
  resetErrorBoundary,
}: {
  error: Error | null
  resetErrorBoundary: () => void
}) {
  return (
    <Card>
      <CardContent className="flex flex-col items-center justify-center py-12 text-center">
        <div className="rounded-full bg-destructive/10 p-3 mb-4">
          <AlertTriangle className="h-8 w-8 text-destructive" />
        </div>
        <h3 className="text-lg font-semibold">Something went wrong</h3>
        <p className="text-sm text-muted-foreground mt-1 max-w-md">
          {error?.message || 'We encountered an error while loading missing receipts.'}
        </p>
        <div className="flex gap-2 mt-4">
          <Button onClick={resetErrorBoundary}>
            <RefreshCcw className="mr-2 h-4 w-4" />
            Try Again
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}

/**
 * Wrapper component that integrates with TanStack Query's error reset boundary.
 * Use this to wrap the missing receipts list or widget.
 */
export function MissingReceiptsErrorBoundary({ children, fallback }: ErrorBoundaryProps) {
  const { reset } = useQueryErrorResetBoundary()

  return (
    <ErrorBoundaryInner onReset={reset} fallback={fallback}>
      {children}
    </ErrorBoundaryInner>
  )
}

/**
 * Inline error state for smaller components (like widget).
 * Use when a full error boundary is overkill.
 */
export function InlineErrorState({
  message = 'Failed to load data',
  onRetry,
}: {
  message?: string
  onRetry?: () => void
}) {
  return (
    <div className="flex flex-col items-center justify-center py-6 text-center">
      <Receipt className="h-8 w-8 text-muted-foreground" />
      <p className="mt-2 text-sm text-muted-foreground">{message}</p>
      {onRetry && (
        <Button variant="ghost" size="sm" className="mt-2" onClick={onRetry}>
          <RefreshCcw className="mr-2 h-3 w-3" />
          Retry
        </Button>
      )}
    </div>
  )
}
