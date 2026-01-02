import { AlertTriangle, RefreshCw } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'

/**
 * Props for the ErrorFallback component
 */
export interface ErrorFallbackProps {
  /** The error that was caught */
  error?: Error

  /** Function to reset the error state and retry */
  resetError?: () => void

  /** Custom title for the error message */
  title?: string

  /** Custom description for the error message */
  description?: string

  /** Whether to show the error details (useful for development) */
  showDetails?: boolean

  /** Optional CSS class name */
  className?: string
}

/**
 * ErrorFallback Component
 *
 * A user-friendly error UI component to display when an error boundary
 * catches an error. Provides a retry button to attempt recovery.
 *
 * Features:
 * - Clean, accessible error message display
 * - Retry button to reset error state
 * - Optional error details for debugging
 * - Consistent styling with the design system
 *
 * Usage:
 * ```tsx
 * <ErrorBoundary
 *   fallback={(error, resetError) => (
 *     <ErrorFallback
 *       error={error}
 *       resetError={resetError}
 *       title="Failed to load analytics"
 *     />
 *   )}
 * >
 *   <AnalyticsSection />
 * </ErrorBoundary>
 * ```
 */
export function ErrorFallback({
  error,
  resetError,
  title = 'Something went wrong',
  description = "We're sorry, but something unexpected happened. Please try again.",
  // Show details in development or staging (staging URL contains 'staging')
  showDetails = process.env.NODE_ENV !== 'production' ||
                (typeof window !== 'undefined' && window.location.hostname.includes('staging')),
  className,
}: ErrorFallbackProps) {
  return (
    <Card
      className={className}
      role="alert"
      data-testid="error-fallback"
    >
      <CardHeader>
        <div className="flex items-center gap-2">
          <AlertTriangle className="h-5 w-5 text-destructive" />
          <CardTitle className="text-destructive">{title}</CardTitle>
        </div>
        <CardDescription>{description}</CardDescription>
      </CardHeader>

      {showDetails && error && (
        <CardContent>
          <details className="text-sm">
            <summary className="cursor-pointer text-muted-foreground hover:text-foreground">
              Error Details
            </summary>
            <pre className="mt-2 rounded-md bg-muted p-3 text-xs overflow-auto max-h-40">
              <code>{error.message}</code>
              {error.stack && (
                <>
                  {'\n\n'}
                  <code className="text-muted-foreground">{error.stack}</code>
                </>
              )}
            </pre>
          </details>
        </CardContent>
      )}

      <CardFooter>
        {resetError && (
          <Button onClick={resetError} variant="default" size="sm">
            <RefreshCw className="mr-2 h-4 w-4" />
            Try Again
          </Button>
        )}
      </CardFooter>
    </Card>
  )
}

/**
 * Compact version of ErrorFallback for use in smaller sections
 */
export function CompactErrorFallback({
  resetError,
  title = 'Error loading content',
}: Pick<ErrorFallbackProps, 'resetError' | 'title'>) {
  return (
    <div
      className="flex items-center justify-between rounded-md border border-destructive/20 bg-destructive/5 p-3"
      role="alert"
      data-testid="compact-error-fallback"
    >
      <div className="flex items-center gap-2">
        <AlertTriangle className="h-4 w-4 text-destructive" />
        <span className="text-sm text-destructive">{title}</span>
      </div>
      {resetError && (
        <Button
          onClick={resetError}
          variant="ghost"
          size="sm"
          className="h-7 px-2 text-xs"
        >
          <RefreshCw className="mr-1 h-3 w-3" />
          Retry
        </Button>
      )}
    </div>
  )
}

export default ErrorFallback
