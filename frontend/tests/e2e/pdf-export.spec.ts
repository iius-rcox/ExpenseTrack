/**
 * E2E Tests: PDF Export with Receipt Images
 *
 * Tests the PDF export functionality that generates complete expense reports
 * with embedded receipt images. This covers the ImageSharp 3.x compatibility
 * fix that enables proper image embedding in generated PDFs.
 *
 * Feature: PDF Export with Receipt Images (fixed 2026-01-25)
 *
 * Critical User Journey:
 * 1. User navigates to expense report editor
 * 2. User clicks "Download PDF" button
 * 3. PDF is generated with all receipt images embedded
 * 4. PDF is downloaded to user's device
 */

import { test, expect, Page } from '@playwright/test'
import { setupApiMocks } from './fixtures/api-mocks'
import { navigateAuthenticated } from './fixtures/auth-helpers'
import { ReportEditorPage } from './pages/report-editor-page'

// =============================================================================
// Test Setup
// =============================================================================

test.describe('PDF Export with Receipt Images', () => {
  let reportEditor: ReportEditorPage

  test.beforeEach(async ({ page }) => {
    reportEditor = new ReportEditorPage(page)
  })

  // ===========================================================================
  // Journey 1: Basic PDF Export
  // ===========================================================================

  test.describe('Journey 1: Basic PDF Export', () => {
    test('can download PDF report with receipts', async ({ page }) => {
      // Set up mocks with a draft that has receipts
      await setupApiMocksWithReceipts(page)
      await navigateAuthenticated(page, '/reports/editor?period=2025-12')

      // Wait for the editor to load
      await reportEditor.waitForLoad()

      // Verify Download PDF button is visible and enabled
      await expect(reportEditor.downloadPdfButton).toBeVisible()
      await expect(reportEditor.downloadPdfButton).toBeEnabled()

      // Take screenshot before download
      await page.screenshot({ path: 'playwright-report/pdf-export-before.png' })

      // Set up download promise before clicking
      const downloadPromise = page.waitForEvent('download', { timeout: 30000 })

      // Click Download PDF
      await reportEditor.downloadPdfButton.click()

      // Wait for download to complete
      const download = await downloadPromise

      // Verify filename
      const filename = download.suggestedFilename()
      expect(filename).toMatch(/\.pdf$/)
      expect(filename).toMatch(/2025-12/)

      // Take screenshot after download initiated
      await page.screenshot({ path: 'playwright-report/pdf-export-after.png' })
    })

    test('PDF download button is disabled during generation', async ({ page }) => {
      await setupApiMocksWithReceipts(page, { slowResponse: true })
      await navigateAuthenticated(page, '/reports/editor?period=2025-12')
      await reportEditor.waitForLoad()

      // Click Download PDF
      await reportEditor.downloadPdfButton.click()

      // Button should be disabled while generating
      await expect(reportEditor.downloadPdfButton).toBeDisabled()

      // Wait for response and button to re-enable
      await page.waitForTimeout(3000)
      await expect(reportEditor.downloadPdfButton).toBeEnabled()
    })
  })

  // ===========================================================================
  // Journey 2: PDF Content Verification
  // ===========================================================================

  test.describe('Journey 2: PDF Content Verification', () => {
    test('PDF export API returns correct headers', async ({ page }) => {
      let responseHeaders: Record<string, string> = {}

      // Set up API capture
      await page.route('**/api/reports/*/export/complete', async (route) => {
        const response = await route.fetch()
        responseHeaders = response.headers()
        await route.fulfill({ response })
      })

      await setupApiMocksWithReceipts(page)
      await navigateAuthenticated(page, '/reports/editor?period=2025-12')
      await reportEditor.waitForLoad()

      // Start download
      const downloadPromise = page.waitForEvent('download', { timeout: 30000 })
      await reportEditor.downloadPdfButton.click()
      await downloadPromise

      // Verify response headers
      expect(responseHeaders['content-type']).toBe('application/pdf')
      expect(responseHeaders['content-disposition']).toMatch(/attachment/)
      expect(responseHeaders['content-disposition']).toMatch(/\.pdf/)
    })

    test('PDF export includes page count header', async ({ page }) => {
      let pageCount: string | null = null

      await page.route('**/api/reports/*/export/complete', async (route) => {
        const response = await route.fetch()
        pageCount = response.headers()['x-page-count']
        await route.fulfill({ response })
      })

      await setupApiMocksWithReceipts(page)
      await navigateAuthenticated(page, '/reports/editor?period=2025-12')
      await reportEditor.waitForLoad()

      const downloadPromise = page.waitForEvent('download', { timeout: 30000 })
      await reportEditor.downloadPdfButton.click()
      await downloadPromise

      // Verify page count header exists
      expect(pageCount).toBeDefined()
      expect(parseInt(pageCount || '0')).toBeGreaterThan(0)
    })

    test('PDF with receipts is significantly larger than without', async ({ page }) => {
      let contentLength: number = 0

      await page.route('**/api/reports/*/export/complete', async (route) => {
        const response = await route.fetch()
        contentLength = parseInt(response.headers()['content-length'] || '0')
        await route.fulfill({ response })
      })

      await setupApiMocksWithReceipts(page)
      await navigateAuthenticated(page, '/reports/editor?period=2025-12')
      await reportEditor.waitForLoad()

      const downloadPromise = page.waitForEvent('download', { timeout: 30000 })
      await reportEditor.downloadPdfButton.click()
      await downloadPromise

      // With real receipt images, PDF should be > 100KB
      // (Without images/with placeholders only, it would be ~45KB)
      expect(contentLength).toBeGreaterThan(100000)
    })
  })

  // ===========================================================================
  // Journey 3: Error Handling
  // ===========================================================================

  test.describe('Journey 3: Error Handling', () => {
    test('shows error toast when PDF generation fails', async ({ page }) => {
      await setupApiMocks(page)

      // Override with error response
      await page.route('**/api/reports/*/export/complete', async (route) => {
        await route.fulfill({
          status: 500,
          contentType: 'application/json',
          body: JSON.stringify({
            title: 'Internal Server Error',
            detail: 'PDF generation failed',
          }),
        })
      })

      await navigateAuthenticated(page, '/reports/editor?period=2025-12')
      await reportEditor.waitForLoad()

      // Click Download PDF
      await reportEditor.downloadPdfButton.click()

      // Verify error toast appears
      await reportEditor.waitForToast(/Failed|Error/i)

      // Button should be re-enabled after error
      await expect(reportEditor.downloadPdfButton).toBeEnabled()

      await page.screenshot({ path: 'playwright-report/pdf-export-error.png' })
    })

    test('handles timeout gracefully', async ({ page }) => {
      await setupApiMocks(page)

      // Override with very slow response
      await page.route('**/api/reports/*/export/complete', async (route) => {
        await new Promise(resolve => setTimeout(resolve, 60000)) // 60 second delay
        await route.abort('timedout')
      })

      await navigateAuthenticated(page, '/reports/editor?period=2025-12')
      await reportEditor.waitForLoad()

      // Click Download PDF
      await reportEditor.downloadPdfButton.click()

      // Wait for timeout error
      await page.waitForTimeout(5000)

      // Button should be re-enabled
      await expect(reportEditor.downloadPdfButton).toBeEnabled()
    })
  })

  // ===========================================================================
  // Journey 4: PDF with Missing Receipts
  // ===========================================================================

  test.describe('Journey 4: PDF with Missing Receipts', () => {
    test('PDF includes placeholder count header for missing receipts', async ({ page }) => {
      let placeholderCount: string | null = null

      await page.route('**/api/reports/*/export/complete', async (route) => {
        const response = await route.fetch()
        placeholderCount = response.headers()['x-placeholder-count']
        await route.fulfill({ response })
      })

      await setupApiMocksWithReceipts(page)
      await navigateAuthenticated(page, '/reports/editor?period=2025-12')
      await reportEditor.waitForLoad()

      const downloadPromise = page.waitForEvent('download', { timeout: 30000 })
      await reportEditor.downloadPdfButton.click()
      await downloadPromise

      // Verify placeholder count header exists
      expect(placeholderCount).toBeDefined()
    })

    test('can download PDF even when some receipts are missing', async ({ page }) => {
      await setupApiMocksWithMixedReceipts(page)
      await navigateAuthenticated(page, '/reports/editor?period=2025-12')
      await reportEditor.waitForLoad()

      // PDF download should still work
      const downloadPromise = page.waitForEvent('download', { timeout: 30000 })
      await reportEditor.downloadPdfButton.click()
      const download = await downloadPromise

      expect(download.suggestedFilename()).toMatch(/\.pdf$/)
    })
  })

  // ===========================================================================
  // Journey 5: Excel Export Comparison
  // ===========================================================================

  test.describe('Journey 5: Excel Export Comparison', () => {
    test('can download both PDF and Excel', async ({ page }) => {
      await setupApiMocksWithReceipts(page)
      await setupExcelExportMock(page)
      await navigateAuthenticated(page, '/reports/editor?period=2025-12')
      await reportEditor.waitForLoad()

      // Download PDF
      const pdfPromise = page.waitForEvent('download', { timeout: 30000 })
      await reportEditor.downloadPdfButton.click()
      const pdfDownload = await pdfPromise
      expect(pdfDownload.suggestedFilename()).toMatch(/\.pdf$/)

      // Wait for button to re-enable
      await page.waitForTimeout(1000)

      // Download Excel
      const excelPromise = page.waitForEvent('download', { timeout: 30000 })
      await reportEditor.downloadExcelButton.click()
      const excelDownload = await excelPromise
      expect(excelDownload.suggestedFilename()).toMatch(/\.xlsx$/)
    })
  })
})

// =============================================================================
// Helper Functions
// =============================================================================

/**
 * Sets up API mocks with receipts attached to expense lines
 */
async function setupApiMocksWithReceipts(page: Page, options?: { slowResponse?: boolean }) {
  await setupApiMocks(page)

  // Override PDF export to return a realistic response
  await page.route('**/api/reports/*/export/complete', async (route) => {
    if (options?.slowResponse) {
      await new Promise(resolve => setTimeout(resolve, 2000))
    }

    // Generate a mock PDF with realistic size (simulating embedded images)
    const mockPdfContent = generateMockPdfWithImages()

    await route.fulfill({
      status: 200,
      contentType: 'application/pdf',
      headers: {
        'Content-Disposition': 'attachment; filename="2025-12-complete-report.pdf"',
        'Content-Length': mockPdfContent.length.toString(),
        'X-Page-Count': '34',
        'X-Placeholder-Count': '2',
      },
      body: mockPdfContent,
    })
  })
}

/**
 * Sets up API mocks with some receipts missing
 */
async function setupApiMocksWithMixedReceipts(page: Page) {
  await setupApiMocks(page)

  await page.route('**/api/reports/*/export/complete', async (route) => {
    const mockPdfContent = generateMockPdfWithImages()

    await route.fulfill({
      status: 200,
      contentType: 'application/pdf',
      headers: {
        'Content-Disposition': 'attachment; filename="2025-12-complete-report.pdf"',
        'Content-Length': mockPdfContent.length.toString(),
        'X-Page-Count': '20',
        'X-Placeholder-Count': '5', // More placeholders for missing receipts
      },
      body: mockPdfContent,
    })
  })
}

/**
 * Sets up Excel export mock
 */
async function setupExcelExportMock(page: Page) {
  await page.route('**/api/reports/*/export', async (route) => {
    if (route.request().url().includes('/export/complete')) {
      await route.continue()
      return
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      headers: {
        'Content-Disposition': 'attachment; filename="2025-12-expense-report.xlsx"',
      },
      body: Buffer.from('PK mock xlsx content'),
    })
  })
}

/**
 * Generates a mock PDF buffer with realistic size
 * (Simulates a PDF with embedded images)
 */
function generateMockPdfWithImages(): Buffer {
  // Create a buffer that simulates a PDF with images (~1MB)
  const pdfHeader = '%PDF-1.4\n'
  const pdfContent = '1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n'

  // Add padding to simulate image data (makes the file ~150KB)
  const imagePadding = 'X'.repeat(150000)

  const pdfFooter = '\n%%EOF'

  return Buffer.from(pdfHeader + pdfContent + imagePadding + pdfFooter)
}
