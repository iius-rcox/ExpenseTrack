/**
 * E2E Tests: Expense Report Manual Save Feature
 *
 * Tests the manual Save button functionality that allows users to:
 * - Edit expense lines in a draft report
 * - Click "Save" to persist all dirty lines to the database
 * - Continue editing after save (report remains in Draft status)
 *
 * Feature: Manual Save (implemented 2026-01-25)
 */

import { test, expect, Page } from '@playwright/test'
import { setupApiMocks, mockExpenseReport } from './fixtures/api-mocks'
import { navigateAuthenticated } from './fixtures/auth-helpers'
import { ReportEditorPage } from './pages/report-editor-page'

// =============================================================================
// Test Setup
// =============================================================================

test.describe('Report Manual Save Feature', () => {
  let reportEditor: ReportEditorPage

  test.beforeEach(async ({ page }) => {
    reportEditor = new ReportEditorPage(page)
  })

  // ===========================================================================
  // Journey 1: Create Draft and Save
  // ===========================================================================

  test.describe('Journey 1: Create Draft and Save', () => {
    test('can create draft, edit GL code, and save changes', async ({ page }) => {
      // Set up mocks with a draft that exists
      await setupApiMocksWithDraft(page)
      await navigateAuthenticated(page, '/reports/editor?period=2026-01')

      // Wait for the editor to load
      await reportEditor.waitForLoad()

      // Verify we're in draft mode
      const isDraft = await reportEditor.isInDraftMode()
      expect(isDraft).toBe(true)

      // Verify expense lines are loaded
      const rowCount = await reportEditor.getExpenseRowCount()
      expect(rowCount).toBeGreaterThan(0)

      // Take screenshot of initial state
      await page.screenshot({ path: 'playwright-report/save-test-initial.png' })

      // Edit a GL code on the first expense line
      await reportEditor.editGLCode(0, '65000')

      // Wait for the row to be marked as dirty
      await page.waitForTimeout(500)

      // Verify the line is marked as dirty (blue left border)
      const isDirty = await reportEditor.isRowDirty(0)
      expect(isDirty).toBe(true)

      // Verify the edited count in summary card increased
      const editedCount = await reportEditor.getEditedCount()
      expect(editedCount).toBeGreaterThanOrEqual(1)

      // Take screenshot showing dirty state
      await page.screenshot({ path: 'playwright-report/save-test-dirty-state.png' })

      // Verify Save button is now visible
      const saveVisible = await reportEditor.isSaveButtonVisible()
      expect(saveVisible).toBe(true)

      // Click Save button
      await reportEditor.clickSave()

      // Verify success toast appears
      await reportEditor.waitForToast(/Saved/)

      // Take screenshot after save
      await page.screenshot({ path: 'playwright-report/save-test-after-save.png' })

      // Verify dirty indicator is cleared
      await page.waitForTimeout(500)
      const isDirtyAfterSave = await reportEditor.isRowDirty(0)
      expect(isDirtyAfterSave).toBe(false)

      // Verify "Last saved" timestamp appears
      const lastSavedText = await reportEditor.getLastSavedText()
      expect(lastSavedText).toMatch(/Saved/)
    })

    test('shows Save button only when there are pending changes', async ({ page }) => {
      await setupApiMocksWithDraft(page)
      await navigateAuthenticated(page, '/reports/editor?period=2026-01')
      await reportEditor.waitForLoad()

      // Initially, Save button should NOT be visible (no changes)
      let saveVisible = await reportEditor.isSaveButtonVisible()
      expect(saveVisible).toBe(false)

      // Make an edit
      await reportEditor.editGLCode(0, '65000')
      await page.waitForTimeout(300)

      // Now Save button should be visible
      saveVisible = await reportEditor.isSaveButtonVisible()
      expect(saveVisible).toBe(true)
    })
  })

  // ===========================================================================
  // Journey 2: Save and Continue Editing
  // ===========================================================================

  test.describe('Journey 2: Save and Continue Editing', () => {
    test('can save, then make more edits', async ({ page }) => {
      await setupApiMocksWithDraft(page)
      await navigateAuthenticated(page, '/reports/editor?period=2026-01')
      await reportEditor.waitForLoad()

      // Make first edit
      await reportEditor.editGLCode(0, '65000')
      await page.waitForTimeout(300)

      // Save
      await reportEditor.clickSave()
      await reportEditor.waitForToast(/Saved/)
      await page.waitForTimeout(500)

      // Verify first row is no longer dirty
      const firstRowDirty = await reportEditor.isRowDirty(0)
      expect(firstRowDirty).toBe(false)

      // Make a second edit on a different line
      const rowCount = await reportEditor.getExpenseRowCount()
      if (rowCount > 1) {
        await reportEditor.editDepartmentCode(1, 'HR')
        await page.waitForTimeout(300)

        // Verify second row is now dirty
        const secondRowDirty = await reportEditor.isRowDirty(1)
        expect(secondRowDirty).toBe(true)

        // Verify first row is still not dirty
        const firstRowStillClean = await reportEditor.isRowDirty(0)
        expect(firstRowStillClean).toBe(false)

        // Verify Save button reappears
        const saveVisible = await reportEditor.isSaveButtonVisible()
        expect(saveVisible).toBe(true)
      }

      // Take screenshot
      await page.screenshot({ path: 'playwright-report/save-continue-editing.png' })
    })

    test('report remains editable after save (not locked)', async ({ page }) => {
      await setupApiMocksWithDraft(page)
      await navigateAuthenticated(page, '/reports/editor?period=2026-01')
      await reportEditor.waitForLoad()

      // Edit and save
      await reportEditor.editGLCode(0, '65000')
      await reportEditor.clickSave()
      await reportEditor.waitForToast(/Saved/)

      // Verify we're still in draft mode (not submitted/locked)
      const isDraft = await reportEditor.isInDraftMode()
      expect(isDraft).toBe(true)

      // Verify we can still edit
      await reportEditor.editDescription(0, 'Updated description after save')
      await page.waitForTimeout(300)

      // Should be able to mark as dirty again
      const isDirty = await reportEditor.isRowDirty(0)
      expect(isDirty).toBe(true)
    })
  })

  // ===========================================================================
  // Journey 3: Save with No Changes
  // ===========================================================================

  test.describe('Journey 3: Save with No Changes', () => {
    test('Save button is hidden when no changes exist', async ({ page }) => {
      await setupApiMocksWithDraft(page)
      await navigateAuthenticated(page, '/reports/editor?period=2026-01')
      await reportEditor.waitForLoad()

      // Wait for toast to dismiss if present
      await page.waitForTimeout(500)

      // Without making any edits, Save button should not be visible
      const saveVisible = await reportEditor.isSaveButtonVisible()
      expect(saveVisible).toBe(false)

      // Verify either "No changes" or a "Saved" timestamp is shown (not "Unsaved changes")
      const noChangesText = page.getByText('No changes')
      const savedText = page.getByText(/Saved/)
      const noChangesVisible = await noChangesText.isVisible().catch(() => false)
      const savedVisible = await savedText.isVisible().catch(() => false)
      // Either no changes or already saved should be shown
      expect(noChangesVisible || savedVisible).toBe(true)

      await page.screenshot({ path: 'playwright-report/save-no-changes.png' })
    })

    test('edited count shows zero when no changes', async ({ page }) => {
      await setupApiMocksWithDraft(page)
      await navigateAuthenticated(page, '/reports/editor?period=2026-01')
      await reportEditor.waitForLoad()

      // Edited count should be 0
      const editedCount = await reportEditor.getEditedCount()
      expect(editedCount).toBe(0)
    })
  })

  // ===========================================================================
  // Journey 4: Download PDF After Save
  // ===========================================================================

  test.describe('Journey 4: Download PDF After Save', () => {
    test('can download PDF after saving changes', async ({ page }) => {
      // Set up mock for PDF export
      await setupApiMocksWithDraft(page)
      await setupPdfExportMock(page)
      await navigateAuthenticated(page, '/reports/editor?period=2026-01')
      await reportEditor.waitForLoad()

      // Make an edit and save
      await reportEditor.editGLCode(0, '65000')
      await page.waitForTimeout(300)
      await reportEditor.clickSave()
      await reportEditor.waitForToast(/Saved/)

      // Wait for save toast to clear
      await page.waitForTimeout(1000)

      // Click Download PDF - verify the button is enabled
      await expect(reportEditor.downloadPdfButton).toBeEnabled()
      await reportEditor.downloadPdfButton.click()

      // Wait for download to initiate (the export hook handles the toast)
      await page.waitForTimeout(2000)

      // Take screenshot
      await page.screenshot({ path: 'playwright-report/save-download-pdf.png' })
    })
  })

  // ===========================================================================
  // API Integration Tests
  // ===========================================================================

  test.describe('Save API Integration', () => {
    test('calls batch save endpoint with correct payload', async ({ page }) => {
      let capturedRequest: any = null

      // Set up base mocks FIRST (before the capturing route)
      await setupApiMocks(page)

      // Override with a route that captures the request (more specific route registered later takes precedence)
      await page.route('**/api/reports/*/save', async (route) => {
        if (route.request().method() === 'POST') {
          capturedRequest = {
            url: route.request().url(),
            body: JSON.parse(route.request().postData() || '{}'),
          }

          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              reportId: 'report-001',
              updatedCount: 1,
              failedCount: 0,
              updatedAt: new Date().toISOString(),
              reportStatus: 'Draft',
              failedLines: [],
            }),
          })
        } else {
          await route.continue()
        }
      })

      await navigateAuthenticated(page, '/reports/editor?period=2026-01')
      await reportEditor.waitForLoad()

      // Edit and save
      await reportEditor.editGLCode(0, '65000')
      await page.waitForTimeout(500)
      await reportEditor.clickSave()

      // Wait for the request to complete
      await page.waitForTimeout(1500)

      // Verify the request was made
      expect(capturedRequest).not.toBeNull()
      expect(capturedRequest.url).toContain('/save')
      expect(capturedRequest.body.lines).toBeDefined()
      expect(Array.isArray(capturedRequest.body.lines)).toBe(true)
    })

    test('handles save API error gracefully', async ({ page }) => {
      // Set up base mocks FIRST
      await setupApiMocks(page)

      // Override with error response
      await page.route('**/api/reports/*/save', async (route) => {
        if (route.request().method() === 'POST') {
          await route.fulfill({
            status: 500,
            contentType: 'application/json',
            body: JSON.stringify({
              title: 'Internal Server Error',
              detail: 'Database connection failed',
            }),
          })
        } else {
          await route.continue()
        }
      })

      await navigateAuthenticated(page, '/reports/editor?period=2026-01')
      await reportEditor.waitForLoad()

      // Edit and try to save
      await reportEditor.editGLCode(0, '65000')
      await page.waitForTimeout(300)
      await reportEditor.clickSave()

      // Verify error toast appears
      await reportEditor.waitForToast(/Failed to save/)

      // Verify the row is still marked as dirty (changes not saved)
      const isDirty = await reportEditor.isRowDirty(0)
      expect(isDirty).toBe(true)

      await page.screenshot({ path: 'playwright-report/save-api-error.png' })
    })

    test('shows partial failure warning when some lines fail', async ({ page }) => {
      // Set up base mocks FIRST
      await setupApiMocks(page)

      // Override with partial failure response
      await page.route('**/api/reports/*/save', async (route) => {
        if (route.request().method() === 'POST') {
          await route.fulfill({
            status: 200,
            contentType: 'application/json',
            body: JSON.stringify({
              reportId: 'report-001',
              updatedCount: 1,
              failedCount: 1,
              updatedAt: new Date().toISOString(),
              reportStatus: 'Draft',
              failedLines: [
                { lineId: 'line-002', error: 'Invalid GL code' },
              ],
            }),
          })
        } else {
          await route.continue()
        }
      })

      await navigateAuthenticated(page, '/reports/editor?period=2026-01')
      await reportEditor.waitForLoad()

      // Edit multiple lines (if available)
      await reportEditor.editGLCode(0, '65000')
      const rowCount = await reportEditor.getExpenseRowCount()
      if (rowCount > 1) {
        await page.waitForTimeout(300)
        await reportEditor.editGLCode(1, '99999')
      }

      await page.waitForTimeout(300)
      await reportEditor.clickSave()

      // Should show warning toast about partial failure
      await reportEditor.waitForToast(/failed/)

      await page.screenshot({ path: 'playwright-report/save-partial-failure.png' })
    })
  })

  // ===========================================================================
  // Preview Mode (No Draft) Tests
  // ===========================================================================

  test.describe('Preview Mode (Before Draft Creation)', () => {
    test('Save button not visible in preview mode', async ({ page }) => {
      // Set up mocks with NO draft
      await setupApiMocksWithoutDraft(page)
      await navigateAuthenticated(page, '/reports/editor?period=2026-01')
      await reportEditor.waitForLoad()

      // Verify we're in preview mode
      const isPreview = await reportEditor.isInPreviewMode()
      expect(isPreview).toBe(true)

      // Save button should not exist
      const saveVisible = await reportEditor.isSaveButtonVisible()
      expect(saveVisible).toBe(false)

      // "Save as Draft" button should be visible instead
      await expect(reportEditor.createDraftButton).toBeVisible()

      await page.screenshot({ path: 'playwright-report/save-preview-mode.png' })
    })

    test('after creating draft, Save button becomes available on edit', async ({ page }) => {
      await setupApiMocksWithoutDraft(page)
      await setupGenerateDraftMock(page)
      await navigateAuthenticated(page, '/reports/editor?period=2026-01')
      await reportEditor.waitForLoad()

      // Create draft
      await reportEditor.createDraft()

      // Now edit a line
      await reportEditor.editGLCode(0, '65000')
      await page.waitForTimeout(300)

      // Save button should now be visible
      const saveVisible = await reportEditor.isSaveButtonVisible()
      expect(saveVisible).toBe(true)
    })
  })

  // ===========================================================================
  // Dirty State Visual Indicators
  // ===========================================================================

  test.describe('Dirty State Visual Indicators', () => {
    test('edited row shows blue left border', async ({ page }) => {
      await setupApiMocksWithDraft(page)
      await navigateAuthenticated(page, '/reports/editor?period=2026-01')
      await reportEditor.waitForLoad()

      // Edit a line
      await reportEditor.editGLCode(0, '65000')
      await page.waitForTimeout(300)

      // Take screenshot showing the blue border
      await page.screenshot({ path: 'playwright-report/save-dirty-border.png' })

      // Verify the row has the dirty class
      const firstRow = page.locator('tbody tr').first()
      await expect(firstRow).toHaveClass(/border-l-blue-500/)
    })

    test('dirty indicator clears after successful save', async ({ page }) => {
      await setupApiMocksWithDraft(page)
      await navigateAuthenticated(page, '/reports/editor?period=2026-01')
      await reportEditor.waitForLoad()

      // Edit
      await reportEditor.editGLCode(0, '65000')
      await page.waitForTimeout(300)

      // Verify dirty
      let isDirty = await reportEditor.isRowDirty(0)
      expect(isDirty).toBe(true)

      // Save
      await reportEditor.saveAndWaitForSuccess()

      // Verify no longer dirty
      await page.waitForTimeout(500)
      isDirty = await reportEditor.isRowDirty(0)
      expect(isDirty).toBe(false)
    })

    test('Edited summary card updates in real-time', async ({ page }) => {
      await setupApiMocksWithDraft(page)
      await navigateAuthenticated(page, '/reports/editor?period=2026-01')
      await reportEditor.waitForLoad()

      // Wait for initial load to complete
      await page.waitForTimeout(500)

      // Initial count should be 0
      let editedCount = await reportEditor.getEditedCount()
      expect(editedCount).toBe(0)

      // Edit first line
      await reportEditor.editGLCode(0, '65000')
      await page.waitForTimeout(500)
      editedCount = await reportEditor.getEditedCount()
      expect(editedCount).toBe(1)

      // Edit second line (if available)
      const rowCount = await reportEditor.getExpenseRowCount()
      if (rowCount > 1) {
        await reportEditor.editDepartmentCode(1, 'HR')
        await page.waitForTimeout(500)
        editedCount = await reportEditor.getEditedCount()
        expect(editedCount).toBe(2)
      }

      // Save and verify count resets
      await reportEditor.clickSave()
      await reportEditor.waitForToast(/Saved/)
      await page.waitForTimeout(1000)
      editedCount = await reportEditor.getEditedCount()
      expect(editedCount).toBe(0)
    })
  })

  // ===========================================================================
  // Unsaved Changes Warning
  // ===========================================================================

  test.describe('Unsaved Changes Warning', () => {
    test('tracks dirty state and shows updated save status after edit', async ({ page }) => {
      await setupApiMocksWithDraft(page)
      await navigateAuthenticated(page, '/reports/editor?period=2026-01')
      await reportEditor.waitForLoad()

      // Wait for initial toasts to clear
      await page.waitForTimeout(500)

      // Initially should show "No changes" or "Saved" (for resumed draft)
      const noChanges = page.getByText('No changes')
      const initialNoChanges = await noChanges.isVisible().catch(() => false)
      const initialSaved = (await reportEditor.getLastSavedText()) !== null

      // One of these should be visible initially
      expect(initialNoChanges || initialSaved).toBe(true)

      // Edit - the per-field auto-save updates lastSaved on success
      // So after editing, we should see "Saved just now" (not "Unsaved changes")
      await reportEditor.editGLCode(0, '65000')
      await page.waitForTimeout(500)

      // After successful per-field auto-save, should show "Saved just now"
      const lastSavedText = await reportEditor.getLastSavedText()
      expect(lastSavedText).toMatch(/Saved/)
    })
  })
})

// =============================================================================
// Helper Functions
// =============================================================================

/**
 * Sets up API mocks with an existing draft
 */
async function setupApiMocksWithDraft(page: Page) {
  await setupApiMocks(page)

  // Override to ensure draft exists
  await page.route(/\/api\/reports\/draft\/exists/, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        exists: true,
        reportId: mockExpenseReport.id,
      }),
    })
  })

  // Mock the save endpoint
  await page.route(/\/api\/reports\/[^/]+\/save/, async (route) => {
    if (route.request().method() === 'POST') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          reportId: mockExpenseReport.id,
          updatedCount: 1,
          failedCount: 0,
          updatedAt: new Date().toISOString(),
          reportStatus: 'Draft',
          failedLines: [],
        }),
      })
    } else {
      await route.continue()
    }
  })
}

/**
 * Sets up API mocks with NO existing draft (preview mode)
 */
async function setupApiMocksWithoutDraft(page: Page) {
  await setupApiMocks(page)

  // Override to indicate no draft exists
  await page.route(/\/api\/reports\/draft\/exists/, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        exists: false,
        reportId: null,
      }),
    })
  })
}

/**
 * Sets up mock for draft generation
 */
async function setupGenerateDraftMock(page: Page) {
  await page.route(/\/api\/reports\/generate/, async (route) => {
    if (route.request().method() === 'POST') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          ...mockExpenseReport,
          id: 'new-draft-001',
          status: 'Draft',
        }),
      })
    } else {
      await route.continue()
    }
  })
}

/**
 * Sets up mock for PDF export
 */
async function setupPdfExportMock(page: Page) {
  await page.route(/\/api\/reports\/[^/]+\/export\/complete/, async (route) => {
    // Return a mock PDF blob
    await route.fulfill({
      status: 200,
      contentType: 'application/pdf',
      headers: {
        'Content-Disposition': 'attachment; filename="expense-report.pdf"',
      },
      body: Buffer.from('%PDF-1.4 mock pdf content'),
    })
  })
}
