/**
 * Smoke Tests - E2E Infrastructure Verification
 *
 * These tests verify the basic E2E infrastructure is working:
 * - Dev server starts and responds
 * - Login page loads correctly
 * - Basic navigation works
 *
 * These should pass even in CI without full auth setup.
 */

import { test, expect } from '@playwright/test'

test.describe('Smoke Tests', () => {
  test('dev server responds and login page loads', async ({ page }) => {
    // Navigate to login page (should work without auth)
    await page.goto('/login')

    // Verify the page loaded (not a 404 or error)
    await expect(page).toHaveTitle(/ExpenseFlow|Expense/i)

    // Verify login button is visible
    await expect(page.locator('text=Sign in with Microsoft')).toBeVisible({
      timeout: 10000,
    })
  })

  test('app redirects unauthenticated users to login', async ({ page }) => {
    // Try to access protected route
    await page.goto('/dashboard')

    // Should redirect to login
    await expect(page).toHaveURL(/login/)
  })
})
