/**
 * Page Object: Report Editor Page
 *
 * Encapsulates all interactions with the expense report editor.
 * Used by E2E tests for the manual save feature and other report editing flows.
 */

import { Page, Locator, expect } from '@playwright/test'

export class ReportEditorPage {
  readonly page: Page

  // Draft Status Banner elements
  readonly saveButton: Locator
  readonly createDraftButton: Locator
  readonly discardDraftButton: Locator
  readonly refreshButton: Locator
  readonly draftBadge: Locator
  readonly previewBanner: Locator
  readonly draftBanner: Locator
  readonly savingIndicator: Locator
  readonly savedIndicator: Locator
  readonly unsavedChangesIndicator: Locator

  // Header elements
  readonly pageTitle: Locator
  readonly periodDisplay: Locator
  readonly prevPeriodButton: Locator
  readonly nextPeriodButton: Locator
  readonly addTransactionButton: Locator
  readonly downloadPdfButton: Locator
  readonly downloadExcelButton: Locator

  // Summary cards
  readonly totalExpensesCard: Locator
  readonly totalAmountCard: Locator
  readonly editedCountCard: Locator
  readonly warningsCountCard: Locator

  // Expense lines table
  readonly expenseTable: Locator
  readonly expenseRows: Locator
  readonly dirtyRows: Locator
  readonly emptyState: Locator

  // Toast notifications
  readonly toastContainer: Locator

  constructor(page: Page) {
    this.page = page

    // Draft Status Banner
    this.saveButton = page.locator('[data-testid="save-report-btn"]')
    this.createDraftButton = page.getByRole('button', { name: 'Save as Draft' })
    this.discardDraftButton = page.getByRole('button', { name: 'Discard Draft' })
    this.refreshButton = page.getByRole('button', { name: /Refresh/ })
    this.draftBadge = page.getByText('Draft', { exact: true }).locator('..')
    this.previewBanner = page.locator('.bg-blue-50, .bg-blue-950').first()
    this.draftBanner = page.locator('.bg-green-50, .bg-green-950').first()
    this.savingIndicator = page.getByText('Saving...')
    this.savedIndicator = page.getByText(/Saved/)
    this.unsavedChangesIndicator = page.getByText('Unsaved changes')

    // Header
    this.pageTitle = page.getByRole('heading', { name: /Quick Expense Export/i })
    this.periodDisplay = page.locator('.text-sm.font-medium.min-w-\\[100px\\]')
    this.prevPeriodButton = page.locator('button').filter({ has: page.locator('[data-lucide="chevron-left"]') }).first()
    this.nextPeriodButton = page.locator('button').filter({ has: page.locator('[data-lucide="chevron-right"]') }).first()
    this.addTransactionButton = page.locator('[data-testid="add-transaction-btn"]')
    this.downloadPdfButton = page.getByRole('button', { name: /Download PDF/i })
    this.downloadExcelButton = page.getByRole('button', { name: /Download Excel/i })

    // Summary cards
    this.totalExpensesCard = page.locator('text=Total Expenses').locator('..')
    this.totalAmountCard = page.locator('text=Total Amount').locator('..')
    this.editedCountCard = page.locator('text=Edited').locator('..')
    this.warningsCountCard = page.locator('text=Warnings').locator('..')

    // Table
    this.expenseTable = page.locator('table')
    this.expenseRows = page.locator('tbody tr')
    this.dirtyRows = page.locator('tbody tr.border-l-blue-500, tbody tr.border-l-2.border-l-blue-500')
    this.emptyState = page.getByText('No reimbursable expenses found')

    // Toasts
    this.toastContainer = page.locator('[data-sonner-toaster]')
  }

  /**
   * Navigate to the report editor for a specific period
   */
  async goto(period: string = '2026-01') {
    await this.page.goto(`/reports/editor?period=${period}`)
    await this.page.waitForLoadState('networkidle')
  }

  /**
   * Wait for the editor to fully load (draft or preview mode)
   */
  async waitForLoad() {
    // Wait for either the draft banner or preview banner to appear
    await Promise.race([
      this.draftBanner.waitFor({ state: 'visible', timeout: 10000 }),
      this.previewBanner.waitFor({ state: 'visible', timeout: 10000 }),
    ])

    // Wait for any loading skeletons to disappear
    await this.page.locator('.skeleton').waitFor({ state: 'hidden', timeout: 10000 }).catch(() => {
      // Skeleton may not exist
    })
  }

  /**
   * Check if the editor is in draft mode
   */
  async isInDraftMode(): Promise<boolean> {
    return await this.draftBanner.isVisible().catch(() => false)
  }

  /**
   * Check if the editor is in preview mode
   */
  async isInPreviewMode(): Promise<boolean> {
    return await this.previewBanner.isVisible().catch(() => false)
  }

  /**
   * Create a draft from preview mode
   */
  async createDraft() {
    await expect(this.createDraftButton).toBeVisible()
    await this.createDraftButton.click()
    await this.waitForToast(/Draft created/)
    await expect(this.draftBanner).toBeVisible()
  }

  /**
   * Click the Save button to persist all dirty lines
   */
  async clickSave() {
    await expect(this.saveButton).toBeVisible()
    await this.saveButton.click()
  }

  /**
   * Save the report and wait for success
   */
  async saveAndWaitForSuccess() {
    await this.clickSave()
    await this.waitForToast(/Saved/)
    // Wait for saving indicator to disappear
    await this.savingIndicator.waitFor({ state: 'hidden', timeout: 5000 }).catch(() => {})
  }

  /**
   * Get the number of dirty (edited) rows
   */
  async getDirtyRowCount(): Promise<number> {
    // Look for rows with the blue left border (dirty indicator)
    const rows = await this.page.locator('tbody tr').all()
    let dirtyCount = 0
    for (const row of rows) {
      const classList = await row.getAttribute('class')
      if (classList?.includes('border-l-blue-500')) {
        dirtyCount++
      }
    }
    return dirtyCount
  }

  /**
   * Get the edited count from the summary card
   */
  async getEditedCount(): Promise<number> {
    // The structure is CardHeader > CardDescription("Edited") + CardTitle(count)
    // CardTitle is a sibling div with class font-semibold
    const card = this.page.locator('text=Edited').locator('..').locator('.font-semibold')
    await card.waitFor({ state: 'visible', timeout: 5000 })
    const text = await card.textContent()
    return parseInt(text || '0', 10)
  }

  /**
   * Edit the GL code of a specific expense line (0-indexed)
   */
  async editGLCode(rowIndex: number, newValue: string) {
    const row = this.expenseRows.nth(rowIndex)

    // Find the GL code cell (4th data cell in the row after the expansion chevron column)
    const glCodeCell = row.locator('td').nth(3).locator('[role="button"], input').first()

    // Click to enter edit mode
    await glCodeCell.click()

    // Wait for input to appear and clear + type new value
    const input = row.locator('td').nth(3).locator('input')
    await expect(input).toBeVisible({ timeout: 2000 })
    await input.clear()
    await input.fill(newValue)

    // Press Enter to save
    await input.press('Enter')

    // Wait for edit mode to exit
    await this.page.waitForTimeout(300)
  }

  /**
   * Edit the department code of a specific expense line
   */
  async editDepartmentCode(rowIndex: number, newValue: string) {
    const row = this.expenseRows.nth(rowIndex)

    // Department is the 6th column (index 5)
    const deptCell = row.locator('td').nth(5).locator('[role="button"], input').first()

    await deptCell.click()

    const input = row.locator('td').nth(5).locator('input')
    await expect(input).toBeVisible({ timeout: 2000 })
    await input.clear()
    await input.fill(newValue)
    await input.press('Enter')

    await this.page.waitForTimeout(300)
  }

  /**
   * Edit the description of a specific expense line
   */
  async editDescription(rowIndex: number, newValue: string) {
    const row = this.expenseRows.nth(rowIndex)

    // Description is the 7th column (index 6)
    const descCell = row.locator('td').nth(6).locator('[role="button"], input').first()

    await descCell.click()

    const input = row.locator('td').nth(6).locator('input')
    await expect(input).toBeVisible({ timeout: 2000 })
    await input.clear()
    await input.fill(newValue)
    await input.press('Enter')

    await this.page.waitForTimeout(300)
  }

  /**
   * Check if the Save button is visible
   */
  async isSaveButtonVisible(): Promise<boolean> {
    return await this.saveButton.isVisible().catch(() => false)
  }

  /**
   * Check if the Save button is disabled
   */
  async isSaveButtonDisabled(): Promise<boolean> {
    const disabled = await this.saveButton.getAttribute('disabled')
    return disabled !== null
  }

  /**
   * Wait for a specific toast message to appear
   * Sonner toasts use data-sonner-toast and li elements
   */
  async waitForToast(messagePattern: RegExp | string, timeout = 5000) {
    // Try multiple selectors for Sonner toast library
    const toastLocator = typeof messagePattern === 'string'
      ? this.page.locator(`[data-sonner-toast]:has-text("${messagePattern}"), li:has-text("${messagePattern}"), .sonner-toast:has-text("${messagePattern}")`)
      : this.page.locator('[data-sonner-toast], li[data-sonner-toast], .sonner-toast').filter({ hasText: messagePattern })

    await toastLocator.first().waitFor({ state: 'visible', timeout })
  }

  /**
   * Check if the "Last saved" timestamp is visible and updated
   * Returns the text from the banner's saved status (not the toast)
   */
  async getLastSavedText(): Promise<string | null> {
    // Look specifically in the banner area (bg-green class), not toasts
    const savedText = this.page.locator('.bg-green-50, .bg-green-950').getByText(/Saved/)
    if (await savedText.first().isVisible().catch(() => false)) {
      return await savedText.first().textContent()
    }
    return null
  }

  /**
   * Get the total number of expense rows
   */
  async getExpenseRowCount(): Promise<number> {
    return await this.expenseRows.count()
  }

  /**
   * Check if a specific row is marked as dirty (has blue left border)
   */
  async isRowDirty(rowIndex: number): Promise<boolean> {
    const row = this.expenseRows.nth(rowIndex)
    const classList = await row.getAttribute('class')
    return classList?.includes('border-l-blue-500') || false
  }

  /**
   * Download PDF and verify success
   */
  async downloadPdf() {
    await this.downloadPdfButton.click()
    await this.waitForToast(/Downloaded/)
  }

  /**
   * Navigate to the previous period
   */
  async goToPreviousPeriod() {
    await this.prevPeriodButton.click()
    await this.page.waitForLoadState('networkidle')
  }

  /**
   * Navigate to the next period
   */
  async goToNextPeriod() {
    await this.nextPeriodButton.click()
    await this.page.waitForLoadState('networkidle')
  }
}
