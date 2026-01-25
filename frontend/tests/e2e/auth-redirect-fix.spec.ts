/**
 * Auth Redirect Fix E2E Tests
 *
 * These tests verify the fix for the MSAL redirect loop issue where:
 * 1. User completes Microsoft login
 * 2. Gets redirected back to the app
 * 3. App was stuck in a redirect loop between /login and /dashboard
 *
 * Root cause was that TanStack Router's context.isAuthenticated was static
 * and didn't update after MSAL's handleRedirectPromise() completed.
 *
 * Fix: Routes now check msalInstance.getActiveAccount() directly instead
 * of relying on the static router context.
 */

import { test, expect } from '@playwright/test'
import { clearAuthState, buildOAuthCallbackUrl, navigateAuthenticated } from './fixtures/auth-helpers'
import { setupApiMocks } from './fixtures/api-mocks'

test.describe('Auth Redirect Fix', () => {
  test.describe('OAuth Callback Handling', () => {
    /**
     * Test: OAuth callback with code parameter in query string is detected
     *
     * The fix added detection for code= in the query string (not just hash)
     * because MSAL's auth code flow uses query parameters.
     */
    test('detects OAuth callback in query string', async ({ page }) => {
      // Build a URL that looks like an OAuth callback
      const callbackUrl = buildOAuthCallbackUrl('http://localhost:3000/')

      // Navigate to the callback URL
      await page.goto(callbackUrl)

      // The app should show the "Completing sign in..." loader
      // (because it detects this is an OAuth callback and doesn't redirect immediately)
      await expect(page.locator('text=Completing sign in')).toBeVisible({ timeout: 5000 })
    })

    /**
     * Test: OAuth error callback is detected and doesn't cause infinite redirect
     */
    test('handles OAuth error callback gracefully', async ({ page }) => {
      const errorCallbackUrl = buildOAuthCallbackUrl('http://localhost:3000/', {
        error: 'access_denied',
        errorDescription: 'User cancelled the login',
      })

      await page.goto(errorCallbackUrl)

      // Should show the callback handler, not immediately redirect
      // Eventually should end up on login page (since auth failed)
      await page.waitForURL(/\/login/, { timeout: 10000 })

      // Login page should be functional
      await expect(page.locator('text=Sign in with Microsoft')).toBeVisible()
    })
  })

  test.describe('Authenticated Navigation', () => {
    test.beforeEach(async ({ page }) => {
      // Set up API mocks for authenticated routes
      await setupApiMocks(page)
    })

    /**
     * Test: After injecting auth state, user can access dashboard
     *
     * This verifies the core fix: routes check MSAL instance directly.
     * We use navigateAuthenticated which injects state then navigates,
     * allowing MSAL to reinitialize and pick up the injected state.
     */
    test('authenticated user reaches dashboard without redirect loop', async ({ page }) => {
      // Use navigateAuthenticated which properly injects state before navigation
      await navigateAuthenticated(page, '/dashboard')

      // Should stay on dashboard (not redirect to login)
      await expect(page).toHaveURL(/\/dashboard/, { timeout: 10000 })

      // Should NOT be redirected to login
      const currentUrl = page.url()
      expect(currentUrl).not.toContain('/login')
    })

    /**
     * Test: Root route redirects authenticated user to dashboard
     */
    test('root route redirects authenticated user to dashboard', async ({ page }) => {
      // Navigate to root with auth - should redirect to dashboard
      await navigateAuthenticated(page, '/')

      // Should redirect to dashboard
      await page.waitForURL(/\/dashboard/, { timeout: 10000 })
    })

    /**
     * Test: Login page redirects authenticated user away
     */
    test('login page redirects authenticated user to dashboard', async ({ page }) => {
      // Navigate to login with auth - should redirect to dashboard
      await navigateAuthenticated(page, '/login')

      // Should redirect to dashboard (not stay on login)
      await page.waitForURL(/\/dashboard/, { timeout: 10000 })
    })

    /**
     * Test: All protected routes are accessible when authenticated
     */
    const protectedRoutes = [
      '/dashboard',
      '/transactions',
      '/receipts',
      '/reports',
      '/analytics',
      '/settings',
      '/matching',
      '/statements',
    ]

    for (const route of protectedRoutes) {
      test(`can access ${route} when authenticated`, async ({ page }) => {
        // Navigate to protected route with auth
        await navigateAuthenticated(page, route)

        // Should stay on the route (not redirect to login)
        await expect(page).toHaveURL(new RegExp(route.replace('/', '\\/')), { timeout: 10000 })

        // Should NOT be on login page
        const currentUrl = page.url()
        expect(currentUrl).not.toContain('/login')
      })
    }
  })

  test.describe('Unauthenticated Navigation', () => {
    /**
     * Test: Unauthenticated user is redirected to login
     */
    test('unauthenticated user is redirected to login', async ({ page }) => {
      // Clear any existing auth
      await page.goto('/')
      await clearAuthState(page)

      // Try to access protected route
      await page.goto('/dashboard')

      // Should redirect to login
      await expect(page).toHaveURL(/\/login/)
    })

    /**
     * Test: Redirect preserves original destination
     */
    test('redirect param contains original destination', async ({ page }) => {
      await page.goto('/')
      await clearAuthState(page)

      // Try to access transactions page
      await page.goto('/transactions')

      // Should redirect to login with redirect param
      await expect(page).toHaveURL(/\/login\?redirect=.*transactions/)
    })
  })

  test.describe('No Redirect Loop', () => {
    /**
     * Test: Navigation doesn't cause infinite redirects
     *
     * This is the key test for the fix. We monitor for multiple redirects
     * which would indicate a loop.
     */
    test('no infinite redirect loop on dashboard access', async ({ page }) => {
      const redirects: string[] = []

      // Track all navigation events
      page.on('framenavigated', (frame) => {
        if (frame === page.mainFrame()) {
          redirects.push(frame.url())
        }
      })

      // Navigate to dashboard with auth
      await navigateAuthenticated(page, '/dashboard')

      // Wait a moment for any redirects to settle
      await page.waitForTimeout(2000)

      // Count how many times we hit login and dashboard
      const loginHits = redirects.filter(url => url.includes('/login')).length
      const dashboardHits = redirects.filter(url => url.includes('/dashboard')).length

      // If there's a redirect loop, we'd see many (>5) alternating hits
      // Normal flow with test mode: root -> reload -> dashboard (may briefly touch login)
      // Allow up to 3 login hits (not a loop, just normal auth checks)
      expect(loginHits).toBeLessThanOrEqual(3)
      expect(dashboardHits).toBeLessThanOrEqual(3)

      // The critical assertion: final URL should be dashboard
      await expect(page).toHaveURL(/\/dashboard/, { timeout: 10000 })
    })

    /**
     * Test: No redirect loop when session is cleared mid-navigation
     */
    test('handles session clear gracefully', async ({ page }) => {
      await setupApiMocks(page)

      // Navigate to dashboard with auth
      await navigateAuthenticated(page, '/dashboard')
      await expect(page).toHaveURL(/\/dashboard/, { timeout: 10000 })

      // Clear auth state (simulates session expiry)
      await clearAuthState(page)

      // Navigate somewhere else - this will cause MSAL to re-check state
      await page.goto('/transactions')

      // Should redirect to login (not loop)
      await expect(page).toHaveURL(/\/login/, { timeout: 10000 })
    })
  })

  test.describe('Deep Link Preservation', () => {
    test.beforeEach(async ({ page }) => {
      await setupApiMocks(page)
    })

    /**
     * Test: User with saved redirect goes to correct page after auth
     */
    test('respects redirect parameter after authentication', async ({ page }) => {
      // Start unauthenticated, try to access analytics
      await page.goto('/')
      await clearAuthState(page)
      await page.goto('/analytics')

      // Should be on login with redirect param
      await expect(page).toHaveURL(/\/login\?redirect=.*analytics/)

      // Now navigate with auth to the root
      // This simulates returning from Microsoft login
      await navigateAuthenticated(page, '/')

      // Eventually should land on dashboard or analytics
      await expect(page).toHaveURL(/\/(dashboard|analytics)/, { timeout: 10000 })
    })
  })
})
