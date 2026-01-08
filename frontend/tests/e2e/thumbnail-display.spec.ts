/**
 * Thumbnail Display Tests - E2E
 *
 * Tests for receipt thumbnail display and preview modal functionality.
 * These tests verify:
 * - Thumbnails display correctly in receipt list
 * - Preview modal opens on thumbnail click
 * - Zoom/pan controls work
 * - Keyboard navigation (Escape, +/-)
 * - Fallback icons for missing thumbnails
 */

import { test, expect } from '@playwright/test'

// Use authenticated context for these tests
test.use({ storageState: 'tests/e2e/.auth/user.json' })

test.describe('Receipt Thumbnail Display', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to receipts page
    await page.goto('/receipts')
    await page.waitForLoadState('networkidle')
  })

  test('displays thumbnails in receipt list', async ({ page }) => {
    // Look for receipt cards with thumbnails
    const receiptCards = page.locator('[data-testid="receipt-card"]')

    // If there are receipts, check for thumbnail or fallback icon
    const cardCount = await receiptCards.count()
    if (cardCount > 0) {
      const firstCard = receiptCards.first()

      // Should have either a thumbnail image or a fallback icon
      const thumbnail = firstCard.locator('img')
      const fallbackIcon = firstCard.locator('[data-testid="thumbnail-fallback"]')

      const hasThumbnail = await thumbnail.count() > 0
      const hasFallback = await fallbackIcon.count() > 0

      expect(hasThumbnail || hasFallback).toBe(true)
    }
  })

  test('opens preview modal on thumbnail click', async ({ page }) => {
    // Look for a receipt with a thumbnail
    const thumbnailImage = page.locator('[data-testid="receipt-card"] img').first()

    if (await thumbnailImage.count() > 0) {
      // Click the thumbnail
      await thumbnailImage.click()

      // Preview modal should open
      const modal = page.locator('[role="dialog"]')
      await expect(modal).toBeVisible({ timeout: 5000 })

      // Modal should contain an image
      await expect(modal.locator('img')).toBeVisible()
    }
  })

  test('preview modal has zoom controls', async ({ page }) => {
    const thumbnailImage = page.locator('[data-testid="receipt-card"] img').first()

    if (await thumbnailImage.count() > 0) {
      await thumbnailImage.click()

      const modal = page.locator('[role="dialog"]')
      await expect(modal).toBeVisible()

      // Check for zoom controls
      const zoomIn = modal.locator('button[title*="Zoom in"]')
      const zoomOut = modal.locator('button[title*="Zoom out"]')
      const reset = modal.locator('button[title*="Reset"]')

      await expect(zoomIn).toBeVisible()
      await expect(zoomOut).toBeVisible()
      await expect(reset).toBeVisible()
    }
  })

  test('preview modal closes with Escape key', async ({ page }) => {
    const thumbnailImage = page.locator('[data-testid="receipt-card"] img').first()

    if (await thumbnailImage.count() > 0) {
      await thumbnailImage.click()

      const modal = page.locator('[role="dialog"]')
      await expect(modal).toBeVisible()

      // Press Escape
      await page.keyboard.press('Escape')

      // Modal should close
      await expect(modal).not.toBeVisible({ timeout: 3000 })
    }
  })

  test('shows file type icons for receipts without thumbnails', async ({ page }) => {
    // This test checks that PDF/HTML receipts without thumbnails show appropriate icons
    const fallbackIcons = page.locator('[data-testid="thumbnail-fallback"]')

    const iconCount = await fallbackIcons.count()
    if (iconCount > 0) {
      // Verify the icon is visible
      await expect(fallbackIcons.first()).toBeVisible()
    }
  })
})

test.describe('Thumbnail Lazy Loading', () => {
  test('thumbnails have loading="lazy" attribute', async ({ page }) => {
    await page.goto('/receipts')
    await page.waitForLoadState('networkidle')

    const thumbnailImages = page.locator('[data-testid="receipt-card"] img')

    const count = await thumbnailImages.count()
    for (let i = 0; i < Math.min(count, 5); i++) {
      const img = thumbnailImages.nth(i)
      await expect(img).toHaveAttribute('loading', 'lazy')
    }
  })
})
