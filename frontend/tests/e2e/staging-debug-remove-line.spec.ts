/**
 * Staging Environment Debug Test: Remove Line Feature
 *
 * This test runs against the ACTUAL staging environment (no mocks)
 * to debug why the remove line feature isn't working in production.
 *
 * Run with: npx playwright test staging-debug-remove-line --headed
 *
 * IMPORTANT: This requires manual authentication since staging uses Azure AD.
 * The test will pause at login for manual intervention.
 */

import { test, Request, Response } from '@playwright/test'

// Staging environment URLs
const STAGING_FRONTEND = 'https://staging.expense.ii-us.com'
const STAGING_API = 'https://staging-api.expense.ii-us.com'

interface NetworkLog {
  timestamp: string
  type: 'request' | 'response'
  method: string
  url: string
  status?: number
  body?: unknown
  error?: string
}

const networkLogs: NetworkLog[] = []

async function logRequest(request: Request) {
  const log: NetworkLog = {
    timestamp: new Date().toISOString(),
    type: 'request',
    method: request.method(),
    url: request.url(),
  }

  // Try to capture request body for POST/PATCH/DELETE
  if (['POST', 'PATCH', 'DELETE'].includes(request.method())) {
    try {
      const postData = request.postData()
      if (postData) {
        log.body = JSON.parse(postData)
      }
    } catch {
      log.body = request.postData()
    }
  }

  networkLogs.push(log)
  console.log(`📤 ${log.method} ${log.url}`)
  if (log.body) {
    console.log(`   Body:`, JSON.stringify(log.body, null, 2))
  }
}

async function logResponse(response: Response) {
  const log: NetworkLog = {
    timestamp: new Date().toISOString(),
    type: 'response',
    method: response.request().method(),
    url: response.url(),
    status: response.status(),
  }

  // Try to capture response body for API calls
  if (response.url().includes('/api/')) {
    try {
      const contentType = response.headers()['content-type'] || ''
      if (contentType.includes('application/json')) {
        log.body = await response.json()
      }
    } catch {
      // Response may not be JSON
    }
  }

  networkLogs.push(log)

  const emoji = log.status && log.status >= 400 ? '❌' : '✅'
  console.log(`${emoji} ${log.status} ${log.method} ${log.url}`)
  if (log.body && (log.status && log.status >= 400)) {
    console.log(`   Response:`, JSON.stringify(log.body, null, 2))
  }
}

test.describe('Staging Debug: Remove Line', () => {
  test.setTimeout(300000) // 5 minutes for manual auth

  test('debug remove line on staging', async ({ page }) => {
    // Enable detailed logging
    page.on('request', logRequest)
    page.on('response', logResponse)

    // Log console messages from the page
    page.on('console', (msg) => {
      const type = msg.type()
      if (type === 'error' || type === 'warning') {
        console.log(`🖥️ Console ${type}:`, msg.text())
      }
    })

    // Log page errors
    page.on('pageerror', (err) => {
      console.log(`💥 Page Error:`, err.message)
    })

    console.log('\n' + '='.repeat(60))
    console.log('STAGING DEBUG: Remove Line Feature')
    console.log('='.repeat(60))
    console.log(`Frontend: ${STAGING_FRONTEND}`)
    console.log(`API: ${STAGING_API}`)
    console.log('='.repeat(60) + '\n')

    // Navigate to staging
    console.log('📍 Step 1: Navigating to staging...')
    await page.goto(STAGING_FRONTEND)
    await page.screenshot({ path: 'test-results/staging-1-initial.png' })

    // Wait for Azure AD redirect and login
    console.log('\n⏳ Step 2: Waiting for authentication...')
    console.log('   (If running --headed, please log in manually)')

    // Wait for the app to load (authenticated)
    // Look for something that indicates successful login
    try {
      await page.waitForURL('**/expense.ii-us.com/**', { timeout: 120000 })
      await page.waitForSelector('nav, [data-testid="main-nav"], .sidebar', { timeout: 60000 })
      console.log('✅ Authentication successful')
    } catch (e) {
      console.log('⚠️ Auth timeout - may need manual intervention')
      await page.screenshot({ path: 'test-results/staging-2-auth-state.png' })
    }

    await page.screenshot({ path: 'test-results/staging-2-after-auth.png' })

    // Navigate to reports editor
    console.log('\n📍 Step 3: Navigating to reports editor...')
    await page.goto(`${STAGING_FRONTEND}/reports/editor?period=2026-01`)
    await page.waitForLoadState('networkidle')
    await page.screenshot({ path: 'test-results/staging-3-reports-editor.png' })

    // Wait for report data to load
    console.log('\n⏳ Step 4: Waiting for report data to load...')
    await page.waitForTimeout(3000) // Give API time to respond
    await page.screenshot({ path: 'test-results/staging-4-report-loaded.png' })

    // Check for expense lines
    console.log('\n🔍 Step 5: Looking for expense lines...')
    const lineRows = page.locator('tr').filter({ has: page.locator('td') })
    const lineCount = await lineRows.count()
    console.log(`   Found ${lineCount} table rows`)

    // Look for remove buttons
    console.log('\n🔍 Step 6: Looking for remove buttons...')
    const removeButtons = page.locator('[data-testid^="remove-line-"], button:has-text("Remove"), button[aria-label*="remove"], button[aria-label*="delete"]')
    const removeButtonCount = await removeButtons.count()
    console.log(`   Found ${removeButtonCount} remove buttons`)

    if (removeButtonCount === 0) {
      console.log('\n⚠️ No remove buttons found! Checking page state...')

      // Take a full-page screenshot
      await page.screenshot({ path: 'test-results/staging-5-no-buttons.png', fullPage: true })

      // Check if we're in draft mode
      const draftIndicator = await page.locator('text=Draft, [data-testid="draft-badge"]').count()
      console.log(`   Draft indicator visible: ${draftIndicator > 0}`)

      // Check for any error messages
      const errorMessages = await page.locator('.error, [role="alert"], .text-destructive').allTextContents()
      if (errorMessages.length > 0) {
        console.log(`   Error messages:`, errorMessages)
      }

      // Log page content for debugging
      const bodyText = await page.locator('body').innerText()
      console.log('\n📄 Page content (first 2000 chars):')
      console.log(bodyText.substring(0, 2000))
    } else {
      // Try to click a remove button
      console.log('\n📍 Step 7: Clicking first remove button...')
      await removeButtons.first().click()
      await page.screenshot({ path: 'test-results/staging-6-after-remove-click.png' })

      // Wait for confirmation dialog
      await page.waitForTimeout(1000)
      const confirmButton = page.locator('[data-testid="confirm-delete"], button:has-text("Confirm"), button:has-text("Delete"), button:has-text("Yes")')
      const confirmVisible = await confirmButton.first().isVisible().catch(() => false)

      if (confirmVisible) {
        console.log('\n📍 Step 8: Clicking confirm button...')
        await confirmButton.first().click()
        await page.waitForTimeout(2000)
        await page.screenshot({ path: 'test-results/staging-7-after-confirm.png' })
      }

      // Wait for API response
      await page.waitForTimeout(3000)
      await page.screenshot({ path: 'test-results/staging-8-final-state.png' })
    }

    // Print network log summary
    console.log('\n' + '='.repeat(60))
    console.log('NETWORK LOG SUMMARY')
    console.log('='.repeat(60))

    const apiCalls = networkLogs.filter((l) => l.url.includes('/api/'))
    console.log(`\nTotal API calls: ${apiCalls.length}`)

    // Focus on reports-related calls
    const reportsCalls = apiCalls.filter((l) => l.url.includes('/reports'))
    console.log(`Reports API calls: ${reportsCalls.length}`)

    reportsCalls.forEach((log) => {
      const status = log.status ? `[${log.status}]` : ''
      console.log(`  ${log.type === 'request' ? '→' : '←'} ${status} ${log.method} ${log.url}`)
      if (log.body) {
        console.log(`     ${JSON.stringify(log.body).substring(0, 200)}`)
      }
    })

    // Look specifically for DELETE calls
    const deleteCalls = networkLogs.filter((l) => l.method === 'DELETE')
    if (deleteCalls.length > 0) {
      console.log('\n🗑️ DELETE calls made:')
      deleteCalls.forEach((log) => {
        console.log(`  ${log.url}`)
        console.log(`  Status: ${log.status}`)
        if (log.body) {
          console.log(`  Response: ${JSON.stringify(log.body)}`)
        }
      })
    } else {
      console.log('\n⚠️ No DELETE calls were made!')
    }

    // Check for errors
    const errors = networkLogs.filter((l) => l.status && l.status >= 400)
    if (errors.length > 0) {
      console.log('\n❌ Failed API calls:')
      errors.forEach((log) => {
        console.log(`  ${log.status} ${log.method} ${log.url}`)
        if (log.body) {
          console.log(`  Response: ${JSON.stringify(log.body)}`)
        }
      })
    }

    console.log('\n' + '='.repeat(60))
    console.log('Screenshots saved to test-results/staging-*.png')
    console.log('='.repeat(60) + '\n')
  })

  test('inspect staging API directly', async ({ request }) => {
    // This test makes direct API calls to staging to inspect responses
    // Requires a valid auth token - will skip if not available

    console.log('\n' + '='.repeat(60))
    console.log('DIRECT API INSPECTION')
    console.log('='.repeat(60))

    // Try to check if API is reachable
    try {
      const healthResponse = await request.get(`${STAGING_API}/health`, {
        timeout: 10000,
      })
      console.log(`API Health: ${healthResponse.status()}`)
    } catch (e) {
      console.log('API not directly reachable (expected if CORS/auth required)')
    }

    // This test is mainly for documentation
    console.log('\nTo debug the API directly, use:')
    console.log(`  curl -H "Authorization: Bearer <token>" ${STAGING_API}/api/reports/draft/exists?period=2026-01`)
    console.log(`  curl -X DELETE -H "Authorization: Bearer <token>" ${STAGING_API}/api/reports/<id>/lines/<lineId>`)
  })
})
