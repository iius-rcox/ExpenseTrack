/**
 * Test Authentication Utilities
 *
 * This module provides mock authentication for E2E testing.
 * When VITE_E2E_TEST_MODE is set, the app uses mock auth instead of MSAL.
 *
 * SECURITY NOTE: This should NEVER be enabled in production builds.
 * The environment variable is only set during Playwright test runs.
 */

export interface MockAccount {
  homeAccountId: string
  environment: string
  tenantId: string
  username: string
  localAccountId: string
  name: string
  idTokenClaims: {
    aud: string
    iss: string
    name: string
    preferred_username: string
    oid: string
    sub: string
  }
}

/**
 * Check if we're running in E2E test mode
 */
export function isE2ETestMode(): boolean {
  // Check for localStorage flag set by Playwright tests
  if (typeof window !== 'undefined') {
    return localStorage.getItem('e2e_test_mode') === 'true'
  }
  return false
}

/**
 * Get mock account from localStorage (set by E2E test helpers)
 */
export function getMockAccount(): MockAccount | null {
  if (!isE2ETestMode()) return null

  try {
    const mockAccountJson = localStorage.getItem('e2e_mock_account')
    if (mockAccountJson) {
      return JSON.parse(mockAccountJson) as MockAccount
    }
  } catch {
    // Invalid JSON, ignore
  }
  return null
}

/**
 * Check if user is authenticated in test mode
 */
export function isTestModeAuthenticated(): boolean {
  if (!isE2ETestMode()) return false
  return getMockAccount() !== null
}

/**
 * Get mock token for API calls in test mode
 */
export function getMockToken(): string | null {
  if (!isE2ETestMode()) return null

  const mockToken = localStorage.getItem('e2e_mock_token')
  return mockToken || null
}
