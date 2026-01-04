/**
 * Playwright Auth Setup
 *
 * This file handles authentication state persistence for E2E tests.
 * It creates a storageState file that can be reused across tests,
 * avoiding the need to log in for every test.
 *
 * Usage:
 *   In playwright.config.ts, add a setup project that runs first
 *   Other projects can use: storageState: 'playwright/.auth/user.json'
 */

import { test as setup } from '@playwright/test'
import path from 'path'
import { fileURLToPath } from 'url'

// ESM-compatible __dirname equivalent
const __filename = fileURLToPath(import.meta.url)
const __dirname = path.dirname(__filename)

/**
 * Path to store authenticated session state
 */
export const STORAGE_STATE_PATH = path.join(
  __dirname,
  '../../playwright/.auth/user.json'
)

/**
 * Mock authentication tokens that simulate MSAL authentication
 * These are used for testing without hitting real Azure AD
 */
export const mockAuthTokens = {
  accessToken:
    'eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0LXVzZXItaWQiLCJuYW1lIjoiVGVzdCBVc2VyIiwiaWF0IjoxNTE2MjM5MDIyfQ.test-signature',
  idToken:
    'eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0LXVzZXItaWQiLCJuYW1lIjoiVGVzdCBVc2VyIiwib2lkIjoiMTIzNDU2Nzg5MCIsInRpZCI6InRlc3QtdGVuYW50LWlkIn0.test-signature',
  expiresOn: new Date(Date.now() + 3600 * 1000).toISOString(),
  account: {
    homeAccountId: 'test-home-account-id.test-tenant-id',
    environment: 'login.microsoftonline.com',
    tenantId: 'test-tenant-id',
    username: 'testuser@expenseflow.example.com',
    localAccountId: 'test-local-account-id',
    name: 'Test User',
    idTokenClaims: {
      aud: 'test-client-id',
      iss: 'https://login.microsoftonline.com/test-tenant-id/v2.0',
      name: 'Test User',
      preferred_username: 'testuser@expenseflow.example.com',
    },
  },
}

/**
 * Setup test that creates authenticated state
 * This runs before other tests and saves the auth state to a file
 */
setup('authenticate', async ({ page }) => {
  // Navigate to the app
  await page.goto('/')

  // Inject mock MSAL state into localStorage
  // MSAL stores auth state in localStorage with specific key patterns
  const msalAccountKey = `${mockAuthTokens.account.homeAccountId}-${mockAuthTokens.account.environment}-${mockAuthTokens.account.tenantId}`

  await page.evaluate(
    ({ account, accessToken, idToken, expiresOn, accountKey }) => {
      // Store account info
      localStorage.setItem(
        `msal.account.keys`,
        JSON.stringify([accountKey])
      )

      localStorage.setItem(accountKey, JSON.stringify(account))

      // Store active account reference
      localStorage.setItem(
        'msal.active-account',
        account.homeAccountId
      )

      // Store token cache entries
      const accessTokenKey = `msal.token.${account.homeAccountId}.accesstoken`
      localStorage.setItem(
        accessTokenKey,
        JSON.stringify({
          credentialType: 'AccessToken',
          secret: accessToken,
          expiresOn: Math.floor(new Date(expiresOn).getTime() / 1000),
          homeAccountId: account.homeAccountId,
          environment: account.environment,
          realm: account.tenantId,
          target: 'api://expenseflow/.default',
        })
      )

      const idTokenKey = `msal.token.${account.homeAccountId}.idtoken`
      localStorage.setItem(
        idTokenKey,
        JSON.stringify({
          credentialType: 'IdToken',
          secret: idToken,
          homeAccountId: account.homeAccountId,
          environment: account.environment,
          realm: account.tenantId,
        })
      )
    },
    {
      account: mockAuthTokens.account,
      accessToken: mockAuthTokens.accessToken,
      idToken: mockAuthTokens.idToken,
      expiresOn: mockAuthTokens.expiresOn,
      accountKey: msalAccountKey,
    }
  )

  // Save storage state to file for reuse
  await page.context().storageState({ path: STORAGE_STATE_PATH })
})

/**
 * Helper to create expired token state for testing token refresh
 */
export function createExpiredTokenState() {
  return {
    ...mockAuthTokens,
    expiresOn: new Date(Date.now() - 3600 * 1000).toISOString(), // Expired 1 hour ago
  }
}
