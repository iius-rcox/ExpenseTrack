/**
 * Error Boundary Integration Tests
 *
 * Tests error boundary components for graceful degradation.
 * Verifies:
 * - Error boundaries catch component errors
 * - Fallback UI displays correctly
 * - Retry functionality works
 * - Section isolation (errors in one section don't affect others)
 */

import { useState } from 'react'
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { ErrorBoundary } from '@/components/error-boundary/ErrorBoundary'
import { ErrorFallback, CompactErrorFallback } from '@/components/error-boundary/ErrorFallback'

// =============================================================================
// Test Components
// =============================================================================

/**
 * Component that throws an error on render
 */
function ThrowingComponent({ shouldThrow = true }: { shouldThrow?: boolean }) {
  if (shouldThrow) {
    throw new Error('Test error message')
  }
  return <div data-testid="working-component">Component rendered successfully</div>
}

/**
 * Component that throws on specific interaction
 */
function ConditionalThrowingComponent() {
  const [shouldThrow, setShouldThrow] = useState(false)
  if (shouldThrow) {
    throw new Error('Conditional error')
  }
  return (
    <div>
      <button onClick={() => setShouldThrow(true)}>Trigger Error</button>
    </div>
  )
}

/**
 * Component that throws after initial render
 */
function DelayedThrowingComponent({ throwAfter = 0 }: { throwAfter?: number }) {
  const [hasThrown, setHasThrown] = useState(false)

  if (hasThrown) {
    throw new Error('Delayed error')
  }

  if (throwAfter > 0) {
    setTimeout(() => setHasThrown(true), throwAfter)
  }

  return <div>Waiting to throw...</div>
}

// =============================================================================
// ErrorBoundary Tests
// =============================================================================

describe('ErrorBoundary Component', () => {
  // Suppress console.error for expected errors in tests
  const originalError = console.error
  beforeEach(() => {
    console.error = vi.fn()
  })

  afterEach(() => {
    console.error = originalError
  })

  describe('Error Catching', () => {
    /**
     * Test: Given component throws, When wrapped in boundary, Then fallback shown
     */
    it('catches errors and displays fallback UI', () => {
      render(
        <ErrorBoundary>
          <ThrowingComponent />
        </ErrorBoundary>
      )

      expect(screen.getByTestId('error-boundary-fallback')).toBeInTheDocument()
      expect(screen.getByText('Something went wrong')).toBeInTheDocument()
      expect(screen.getByText('Test error message')).toBeInTheDocument()
    })

    it('renders children when no error occurs', () => {
      render(
        <ErrorBoundary>
          <ThrowingComponent shouldThrow={false} />
        </ErrorBoundary>
      )

      expect(screen.getByTestId('working-component')).toBeInTheDocument()
      expect(
        screen.getByText('Component rendered successfully')
      ).toBeInTheDocument()
    })

    it('calls onError callback when error is caught', () => {
      const onError = vi.fn()

      render(
        <ErrorBoundary onError={onError}>
          <ThrowingComponent />
        </ErrorBoundary>
      )

      expect(onError).toHaveBeenCalledTimes(1)
      expect(onError).toHaveBeenCalledWith(
        expect.any(Error),
        expect.objectContaining({
          componentStack: expect.any(String),
        })
      )
    })
  })

  describe('Custom Fallback', () => {
    it('renders custom fallback ReactNode', () => {
      render(
        <ErrorBoundary fallback={<div data-testid="custom-fallback">Custom Error</div>}>
          <ThrowingComponent />
        </ErrorBoundary>
      )

      expect(screen.getByTestId('custom-fallback')).toBeInTheDocument()
      expect(screen.getByText('Custom Error')).toBeInTheDocument()
    })

    it('renders custom fallback render prop with error and reset', () => {
      render(
        <ErrorBoundary
          fallback={(error, resetError) => (
            <div data-testid="render-prop-fallback">
              <span data-testid="error-message">{error.message}</span>
              <button data-testid="reset-button" onClick={resetError}>
                Reset
              </button>
            </div>
          )}
        >
          <ThrowingComponent />
        </ErrorBoundary>
      )

      expect(screen.getByTestId('render-prop-fallback')).toBeInTheDocument()
      expect(screen.getByTestId('error-message')).toHaveTextContent(
        'Test error message'
      )
      expect(screen.getByTestId('reset-button')).toBeInTheDocument()
    })
  })

  describe('Retry Functionality', () => {
    /**
     * Test: Given error caught, When user clicks retry, Then component re-renders
     */
    it('resets error state when retry button clicked', async () => {
      let shouldThrow = true

      function ToggleComponent() {
        if (shouldThrow) {
          throw new Error('Toggle error')
        }
        return <div data-testid="recovered">Recovered!</div>
      }

      render(
        <ErrorBoundary>
          <ToggleComponent />
        </ErrorBoundary>
      )

      // Verify error state
      expect(screen.getByTestId('error-boundary-fallback')).toBeInTheDocument()

      // Stop throwing
      shouldThrow = false

      // Click retry
      fireEvent.click(screen.getByText('Try Again'))

      // Should re-render successfully
      await waitFor(() => {
        expect(screen.getByTestId('recovered')).toBeInTheDocument()
      })
    })
  })

  describe('Boundary ID', () => {
    it('includes boundary ID in error logging', () => {
      render(
        <ErrorBoundary boundaryId="analytics-section">
          <ThrowingComponent />
        </ErrorBoundary>
      )

      // Error should be caught and logged with boundary ID
      expect(screen.getByTestId('error-boundary-fallback')).toBeInTheDocument()
    })
  })
})

// =============================================================================
// ErrorFallback Tests
// =============================================================================

describe('ErrorFallback Component', () => {
  const mockResetError = vi.fn()
  const testError = new Error('Test error with stack')

  beforeEach(() => {
    mockResetError.mockClear()
  })

  describe('Rendering', () => {
    it('renders with default props', () => {
      render(<ErrorFallback />)

      expect(screen.getByTestId('error-fallback')).toBeInTheDocument()
      expect(screen.getByText('Something went wrong')).toBeInTheDocument()
    })

    it('renders with custom title and description', () => {
      render(
        <ErrorFallback
          title="Custom Error Title"
          description="Custom error description"
        />
      )

      expect(screen.getByText('Custom Error Title')).toBeInTheDocument()
      expect(screen.getByText('Custom error description')).toBeInTheDocument()
    })

    it('shows error details in development mode', () => {
      render(<ErrorFallback error={testError} showDetails />)

      expect(screen.getByText('Error Details')).toBeInTheDocument()
    })

    it('hides error details when showDetails is false', () => {
      render(<ErrorFallback error={testError} showDetails={false} />)

      expect(screen.queryByText('Error Details')).not.toBeInTheDocument()
    })
  })

  describe('Retry Button', () => {
    it('renders retry button when resetError provided', () => {
      render(<ErrorFallback resetError={mockResetError} />)

      expect(screen.getByText('Try Again')).toBeInTheDocument()
    })

    it('does not render retry button when resetError not provided', () => {
      render(<ErrorFallback />)

      expect(screen.queryByText('Try Again')).not.toBeInTheDocument()
    })

    it('calls resetError when retry button clicked', () => {
      render(<ErrorFallback resetError={mockResetError} />)

      fireEvent.click(screen.getByText('Try Again'))

      expect(mockResetError).toHaveBeenCalledTimes(1)
    })
  })
})

// =============================================================================
// CompactErrorFallback Tests
// =============================================================================

describe('CompactErrorFallback Component', () => {
  const mockResetError = vi.fn()

  beforeEach(() => {
    mockResetError.mockClear()
  })

  it('renders with default title', () => {
    render(<CompactErrorFallback />)

    expect(screen.getByTestId('compact-error-fallback')).toBeInTheDocument()
    expect(screen.getByText('Error loading content')).toBeInTheDocument()
  })

  it('renders with custom title', () => {
    render(<CompactErrorFallback title="Custom compact error" />)

    expect(screen.getByText('Custom compact error')).toBeInTheDocument()
  })

  it('renders retry button when resetError provided', () => {
    render(<CompactErrorFallback resetError={mockResetError} />)

    expect(screen.getByText('Retry')).toBeInTheDocument()
  })

  it('calls resetError when retry button clicked', () => {
    render(<CompactErrorFallback resetError={mockResetError} />)

    fireEvent.click(screen.getByText('Retry'))

    expect(mockResetError).toHaveBeenCalledTimes(1)
  })
})

// =============================================================================
// Section Isolation Tests
// =============================================================================

describe('Section Isolation', () => {
  const originalError = console.error
  beforeEach(() => {
    console.error = vi.fn()
  })

  afterEach(() => {
    console.error = originalError
  })

  /**
   * Test: Given error in one section, When user interacts with others, Then others work
   */
  it('isolates errors to specific sections', () => {
    render(
      <div>
        <ErrorBoundary boundaryId="section-1">
          <ThrowingComponent />
        </ErrorBoundary>
        <ErrorBoundary boundaryId="section-2">
          <ThrowingComponent shouldThrow={false} />
        </ErrorBoundary>
      </div>
    )

    // Section 1 should show error fallback
    expect(screen.getByTestId('error-boundary-fallback')).toBeInTheDocument()

    // Section 2 should render successfully
    expect(screen.getByTestId('working-component')).toBeInTheDocument()
  })

  it('allows multiple sections to fail independently', () => {
    render(
      <div>
        <ErrorBoundary boundaryId="section-a">
          <ThrowingComponent />
        </ErrorBoundary>
        <ErrorBoundary boundaryId="section-b">
          <ThrowingComponent />
        </ErrorBoundary>
        <ErrorBoundary boundaryId="section-c">
          <ThrowingComponent shouldThrow={false} />
        </ErrorBoundary>
      </div>
    )

    // Two error fallbacks for failing sections
    const fallbacks = screen.getAllByTestId('error-boundary-fallback')
    expect(fallbacks).toHaveLength(2)

    // One working component
    expect(screen.getByTestId('working-component')).toBeInTheDocument()
  })

  it('retrying one section does not affect others', async () => {
    let section1ShouldThrow = true

    function Section1() {
      if (section1ShouldThrow) {
        throw new Error('Section 1 error')
      }
      return <div data-testid="section-1-content">Section 1 Content</div>
    }

    render(
      <div>
        <ErrorBoundary boundaryId="section-1">
          <Section1 />
        </ErrorBoundary>
        <ErrorBoundary boundaryId="section-2">
          <div data-testid="section-2-content">Section 2 Content</div>
        </ErrorBoundary>
      </div>
    )

    // Verify initial state
    expect(screen.getByTestId('error-boundary-fallback')).toBeInTheDocument()
    expect(screen.getByTestId('section-2-content')).toBeInTheDocument()

    // Fix section 1 and retry
    section1ShouldThrow = false
    fireEvent.click(screen.getByText('Try Again'))

    // Both sections should now work
    await waitFor(() => {
      expect(screen.getByTestId('section-1-content')).toBeInTheDocument()
    })
    expect(screen.getByTestId('section-2-content')).toBeInTheDocument()
  })
})

// =============================================================================
// Integration with ErrorFallback
// =============================================================================

describe('ErrorBoundary with ErrorFallback Integration', () => {
  const originalError = console.error
  beforeEach(() => {
    console.error = vi.fn()
  })

  afterEach(() => {
    console.error = originalError
  })

  it('works with ErrorFallback as render prop', () => {
    render(
      <ErrorBoundary
        fallback={(error, resetError) => (
          <ErrorFallback error={error} resetError={resetError} />
        )}
      >
        <ThrowingComponent />
      </ErrorBoundary>
    )

    expect(screen.getByTestId('error-fallback')).toBeInTheDocument()
    expect(screen.getByText('Something went wrong')).toBeInTheDocument()
    expect(screen.getByText('Try Again')).toBeInTheDocument()
  })

  it('works with CompactErrorFallback as render prop', () => {
    render(
      <ErrorBoundary
        fallback={(error, resetError) => (
          <CompactErrorFallback error={error} resetError={resetError} />
        )}
      >
        <ThrowingComponent />
      </ErrorBoundary>
    )

    expect(screen.getByTestId('compact-error-fallback')).toBeInTheDocument()
    expect(screen.getByText('Error loading content')).toBeInTheDocument()
    expect(screen.getByText('Retry')).toBeInTheDocument()
  })
})
