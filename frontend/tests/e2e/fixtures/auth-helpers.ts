/**
 * Playwright Authentication Helpers for E2E Tests
 *
 * These functions help inject MSAL authentication state into the browser
 * to simulate authenticated users without hitting real Azure AD.
 *
 * The key insight is that MSAL stores auth state in localStorage with
 * specific key patterns. By injecting the right data structure, we can
 * trick the app into thinking a user is authenticated.
 */

import { Page } from '@playwright/test'

// =============================================================================
// Mock Auth Data
// =============================================================================

/**
 * Azure AD App Registration client ID from authConfig.ts
 * MSAL uses this to prefix all cache keys in localStorage
 */
export const MSAL_CLIENT_ID = '00435dee-8aff-429b-bab6-762973c091c4'

/**
 * Mock MSAL account that simulates a logged-in user
 */
export const mockAccount = {
  homeAccountId: 'test-home-account-id.test-tenant-id',
  environment: 'login.microsoftonline.com',
  tenantId: 'test-tenant-id',
  username: 'testuser@expenseflow.example.com',
  localAccountId: 'test-local-account-id',
  name: 'Test User',
  idTokenClaims: {
    aud: MSAL_CLIENT_ID, // Real client ID from authConfig
    iss: 'https://login.microsoftonline.com/test-tenant-id/v2.0',
    name: 'Test User',
    preferred_username: 'testuser@expenseflow.example.com',
    oid: 'test-object-id',
    sub: 'test-subject-id',
  },
}

/**
 * Generate mock tokens with configurable expiry
 */
export function generateMockTokens(expiresInSeconds = 3600) {
  const expiresOn = new Date(Date.now() + expiresInSeconds * 1000)

  // These are fake JWT tokens - they have valid structure but are not cryptographically valid
  // The app will use them for display but API calls need to be mocked anyway
  const idToken = [
    btoa(JSON.stringify({ alg: 'RS256', typ: 'JWT' })),
    btoa(JSON.stringify({
      sub: mockAccount.localAccountId,
      name: mockAccount.name,
      oid: mockAccount.idTokenClaims.oid,
      preferred_username: mockAccount.username,
      tid: mockAccount.tenantId,
      aud: mockAccount.idTokenClaims.aud,
      iss: mockAccount.idTokenClaims.iss,
      exp: Math.floor(expiresOn.getTime() / 1000),
      iat: Math.floor(Date.now() / 1000),
    })),
    'fake-signature',
  ].join('.')

  return {
    idToken,
    accessToken: idToken, // We use ID token as access token (matches authConfig.ts)
    expiresOn: expiresOn.toISOString(),
    expiresOnTimestamp: Math.floor(expiresOn.getTime() / 1000),
  }
}

// =============================================================================
// Auth State Injection
// =============================================================================

/**
 * Injects authenticated state into the page's localStorage
 *
 * This uses the E2E test mode approach which is more reliable than
 * trying to fake MSAL's internal cache format (which is complex and
 * version-specific).
 *
 * The app checks for these localStorage keys in the route guards
 * and bypasses MSAL when they're present.
 */
export async function injectAuthenticatedState(page: Page, options?: {
  expired?: boolean
  account?: Partial<typeof mockAccount>
}): Promise<void> {
  const tokens = generateMockTokens(options?.expired ? -3600 : 3600)
  const account = { ...mockAccount, ...options?.account }

  await page.evaluate(
    ({ account, tokens }) => {
      // Enable E2E test mode
      localStorage.setItem('e2e_test_mode', 'true')

      // Store mock account for route guard checks
      localStorage.setItem('e2e_mock_account', JSON.stringify(account))

      // Store mock token for API calls
      localStorage.setItem('e2e_mock_token', tokens.accessToken)
    },
    { account, tokens }
  )
}

/**
 * Clears all authentication state from localStorage and sessionStorage
 *
 * This removes:
 * - E2E test mode keys
 * - MSAL cache keys (for completeness)
 */
export async function clearAuthState(page: Page): Promise<void> {
  await page.evaluate(() => {
    // Clear E2E test mode keys
    localStorage.removeItem('e2e_test_mode')
    localStorage.removeItem('e2e_mock_account')
    localStorage.removeItem('e2e_mock_token')

    // Also clear any MSAL keys (for completeness)
    const keysToRemove = Object.keys(localStorage).filter(
      key => key.startsWith('msal.') ||
             key.includes('login.microsoftonline') ||
             key.includes('-idtoken-') ||
             key.includes('-accesstoken-')
    )
    keysToRemove.forEach(key => localStorage.removeItem(key))

    // Clear sessionStorage
    const sessionKeysToRemove = Object.keys(sessionStorage).filter(
      key => key.startsWith('msal.')
    )
    sessionKeysToRemove.forEach(key => sessionStorage.removeItem(key))
  })
}

/**
 * Simulates an OAuth callback URL (what Azure AD redirects to after login)
 *
 * This is useful for testing the redirect flow handling.
 */
export function buildOAuthCallbackUrl(baseUrl: string, options?: {
  error?: string
  errorDescription?: string
}): string {
  const url = new URL(baseUrl)

  if (options?.error) {
    url.searchParams.set('error', options.error)
    url.searchParams.set('error_description', options.errorDescription || 'An error occurred')
  } else {
    // Simulate successful auth code response
    url.searchParams.set('code', 'mock-auth-code-12345')
    url.searchParams.set('state', 'mock-state-67890')
    url.searchParams.set('session_state', 'mock-session-state')
  }

  return url.toString()
}

// =============================================================================
// Test Helpers
// =============================================================================

/**
 * Waits for the app to recognize the authenticated state
 *
 * After injecting auth state, MSAL needs to pick it up.
 * This helper navigates and waits for auth to be recognized.
 */
export async function waitForAuthentication(page: Page, options?: {
  timeout?: number
}): Promise<void> {
  const timeout = options?.timeout ?? 10000

  // Navigate to trigger route evaluation
  await page.goto('/')

  // Wait for redirect to dashboard (authenticated) or for dashboard content
  await page.waitForURL(/\/(dashboard)?$/, { timeout })
}

/**
 * Sets up a fully authenticated test context
 *
 * Combines auth injection, API mocking, and verification.
 * IMPORTANT: This reloads the page after injecting state so MSAL picks it up.
 */
export async function setupAuthenticatedTest(page: Page): Promise<void> {
  // Go to app first to establish origin
  await page.goto('/')

  // Inject auth state
  await injectAuthenticatedState(page)

  // Reload to pick up the auth state - MSAL reads localStorage on init
  await page.reload()
  await page.waitForLoadState('networkidle')
}

/**
 * Injects auth state and navigates to a specific page
 *
 * This is the recommended way to test authenticated routes:
 * 1. Navigate to root to establish origin
 * 2. Inject auth state into localStorage
 * 3. Force a full page reload (not client-side navigation) so MSAL reinitializes
 * 4. Navigate to target page
 *
 * The key insight is that MSAL reads localStorage during initialization,
 * so we must reload the page after injecting state for MSAL to pick it up.
 */
export async function navigateAuthenticated(page: Page, path: string): Promise<void> {
  // First navigate to establish origin
  await page.goto('/')
  await page.waitForLoadState('networkidle')

  // Inject auth state into localStorage
  await injectAuthenticatedState(page)

  // Force a full page reload so MSAL reinitializes and reads the injected state
  // Using page.reload() ensures we don't do client-side navigation
  await page.reload()
  await page.waitForLoadState('networkidle')

  // Now navigate to the target path if it's not root
  if (path !== '/') {
    await page.goto(path)
    await page.waitForLoadState('domcontentloaded')
  }
}
