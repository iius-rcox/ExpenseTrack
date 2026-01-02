import { Component, ErrorInfo, ReactNode } from 'react'

/**
 * Props for the ErrorBoundary component
 */
export interface ErrorBoundaryProps {
  /** Child components to wrap */
  children: ReactNode

  /** Custom fallback UI to render when an error is caught */
  fallback?: ReactNode | ((error: Error, resetError: () => void) => ReactNode)

  /** Callback when an error is caught */
  onError?: (error: Error, errorInfo: ErrorInfo) => void

  /** Optional unique identifier for this boundary */
  boundaryId?: string
}

/**
 * State for the ErrorBoundary component
 */
interface ErrorBoundaryState {
  hasError: boolean
  error: Error | null
}

/**
 * ErrorBoundary Component
 *
 * A React error boundary that catches JavaScript errors anywhere in its
 * child component tree and displays a fallback UI instead of crashing
 * the entire application.
 *
 * Features:
 * - Catches render errors, lifecycle method errors, and constructor errors
 * - Provides error reset functionality via retry button
 * - Supports custom fallback UI via render prop or ReactNode
 * - Reports errors via optional onError callback
 *
 * Usage:
 * ```tsx
 * <ErrorBoundary fallback={<ErrorFallback />}>
 *   <ComponentThatMightFail />
 * </ErrorBoundary>
 * ```
 *
 * @example With render prop fallback
 * ```tsx
 * <ErrorBoundary
 *   fallback={(error, resetError) => (
 *     <div>
 *       <p>Error: {error.message}</p>
 *       <button onClick={resetError}>Retry</button>
 *     </div>
 *   )}
 * >
 *   <ComponentThatMightFail />
 * </ErrorBoundary>
 * ```
 */
export class ErrorBoundary extends Component<
  ErrorBoundaryProps,
  ErrorBoundaryState
> {
  constructor(props: ErrorBoundaryProps) {
    super(props)
    this.state = { hasError: false, error: null }
  }

  /**
   * Static lifecycle method called when an error is thrown during rendering.
   * Updates state to trigger fallback UI on next render.
   */
  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error }
  }

  /**
   * Lifecycle method called after an error is caught.
   * Use this to log errors or report to error tracking services.
   */
  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    const { onError, boundaryId } = this.props

    // Log to console in development
    if (process.env.NODE_ENV !== 'production') {
      console.group(`[ErrorBoundary${boundaryId ? `: ${boundaryId}` : ''}]`)
      console.error('Caught error:', error)
      console.error('Component stack:', errorInfo.componentStack)
      console.groupEnd()
    }

    // Call optional error handler
    if (onError) {
      onError(error, errorInfo)
    }
  }

  /**
   * Resets the error state, allowing the children to re-render.
   * Call this from a "Retry" button in the fallback UI.
   */
  resetError = (): void => {
    this.setState({ hasError: false, error: null })
  }

  render(): ReactNode {
    const { hasError, error } = this.state
    const { children, fallback } = this.props

    if (hasError && error) {
      // Render custom fallback if provided
      if (fallback) {
        // Check if fallback is a render prop function
        if (typeof fallback === 'function') {
          return fallback(error, this.resetError)
        }
        return fallback
      }

      // Default fallback UI
      return (
        <div
          role="alert"
          data-testid="error-boundary-fallback"
          style={{
            padding: '1rem',
            backgroundColor: 'hsl(var(--destructive) / 0.1)',
            borderRadius: '0.5rem',
            border: '1px solid hsl(var(--destructive) / 0.2)',
          }}
        >
          <h3 style={{ margin: 0, color: 'hsl(var(--destructive))' }}>
            Something went wrong
          </h3>
          <p style={{ color: 'hsl(var(--muted-foreground))' }}>
            {error.message || 'An unexpected error occurred'}
          </p>
          <button
            onClick={this.resetError}
            style={{
              padding: '0.5rem 1rem',
              backgroundColor: 'hsl(var(--primary))',
              color: 'hsl(var(--primary-foreground))',
              border: 'none',
              borderRadius: '0.25rem',
              cursor: 'pointer',
            }}
          >
            Try Again
          </button>
        </div>
      )
    }

    return children
  }
}

export default ErrorBoundary
