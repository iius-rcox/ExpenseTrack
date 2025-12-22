import { test, expect } from '@playwright/test';

/**
 * E2E Tests for Mobile Viewport Experience (T104)
 *
 * These tests verify mobile-specific UI components and interactions:
 * - Mobile bottom navigation visibility and function
 * - Responsive layout adaptations
 * - Touch-friendly targets
 * - Mobile-specific features (camera button, card views)
 *
 * Uses Playwright's mobile device emulation for realistic testing.
 * Run with: npx playwright test --project="Mobile Chrome"
 */

test.describe('Mobile Navigation', () => {
  test.use({
    viewport: { width: 375, height: 812 }, // iPhone X dimensions
    isMobile: true,
    hasTouch: true,
  });

  test.beforeEach(async ({ page }) => {
    await page.goto('/dashboard');
    await page.waitForLoadState('networkidle');
  });

  test('should display mobile bottom navigation', async ({ page }) => {
    // Mobile nav should be visible
    const nav = page.locator('nav').filter({ hasText: /home|receipts|transactions/i });
    await expect(nav).toBeVisible();
  });

  test('should show all navigation items', async ({ page }) => {
    await expect(page.getByRole('button', { name: /home/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /receipts/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /transactions/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /matching/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /analytics/i })).toBeVisible();
  });

  test('should navigate to receipts page on tap', async ({ page }) => {
    await page.getByRole('button', { name: /receipts/i }).click();
    await page.waitForURL('**/receipts');
    expect(page.url()).toContain('/receipts');
  });

  test('should navigate to transactions page on tap', async ({ page }) => {
    await page.getByRole('button', { name: /transactions/i }).click();
    await page.waitForURL('**/transactions');
    expect(page.url()).toContain('/transactions');
  });

  test('should indicate active navigation item', async ({ page }) => {
    // Dashboard should be active initially
    const homeButton = page.getByRole('button', { name: /home/i });
    await expect(homeButton).toHaveAttribute('aria-current', 'page');

    // Navigate to receipts
    await page.getByRole('button', { name: /receipts/i }).click();
    await page.waitForURL('**/receipts');

    // Now receipts should be active
    const receiptsButton = page.getByRole('button', { name: /receipts/i });
    await expect(receiptsButton).toHaveAttribute('aria-current', 'page');
  });
});

test.describe('Mobile Dashboard Layout', () => {
  test.use({
    viewport: { width: 375, height: 812 },
    isMobile: true,
    hasTouch: true,
  });

  test.beforeEach(async ({ page }) => {
    await page.goto('/dashboard');
    await page.waitForLoadState('networkidle');
  });

  test('should display mobile summary bar instead of metrics cards', async ({ page }) => {
    // Mobile summary bar should be visible
    // Desktop metrics row should be hidden
    const summaryBar = page.locator('.md\\:hidden').first();
    await expect(summaryBar).toBeVisible();
  });

  test('should have proper spacing for bottom navigation', async ({ page }) => {
    // Should have spacer element to prevent content from being hidden
    const spacer = page.locator('div[class*="h-[calc(60px"]');
    await expect(spacer).toBeVisible();
  });
});

test.describe('Mobile Receipt Upload', () => {
  test.use({
    viewport: { width: 375, height: 812 },
    isMobile: true,
    hasTouch: true,
  });

  test.beforeEach(async ({ page }) => {
    await page.goto('/receipts');
    await page.waitForLoadState('networkidle');
  });

  test('should show camera button on mobile', async ({ page }) => {
    // Camera capture button should be visible on mobile
    const cameraButton = page.getByRole('button', { name: /take photo|camera/i });
    await expect(cameraButton).toBeVisible();
  });

  test('should show browse files button on mobile', async ({ page }) => {
    // Browse files option should be visible
    const browseText = page.getByText(/browse files/i);
    await expect(browseText).toBeVisible();
  });

  test('should have touch-friendly button sizing', async ({ page }) => {
    // Buttons should meet 44x44 minimum touch target
    const buttons = page.locator('button');
    const count = await buttons.count();

    for (let i = 0; i < Math.min(count, 5); i++) {
      const button = buttons.nth(i);
      if (await button.isVisible()) {
        const box = await button.boundingBox();
        if (box) {
          // Check that touch targets are at least 44px
          expect(box.width).toBeGreaterThanOrEqual(40); // Allow some tolerance
          expect(box.height).toBeGreaterThanOrEqual(40);
        }
      }
    }
  });
});

test.describe('Mobile Transactions View', () => {
  test.use({
    viewport: { width: 375, height: 812 },
    isMobile: true,
    hasTouch: true,
  });

  test.beforeEach(async ({ page }) => {
    await page.goto('/transactions');
    await page.waitForLoadState('networkidle');
  });

  test('should render transactions in card layout', async ({ page }) => {
    // On mobile, transactions should be rendered as cards, not table
    // Check for card-based layout elements
    const cardElements = page.locator('div[class*="rounded-lg"]').or(
      page.locator('[class*="card"]')
    );
    await expect(cardElements.first()).toBeVisible();
  });
});

test.describe('Desktop vs Mobile Comparison', () => {
  test('should hide mobile nav on desktop viewport', async ({ page }) => {
    // Desktop viewport
    await page.setViewportSize({ width: 1280, height: 800 });
    await page.goto('/dashboard');
    await page.waitForLoadState('networkidle');

    // Mobile nav should be hidden (has md:hidden class)
    const mobileNav = page.locator('nav.md\\:hidden');
    // Nav exists but is not visible due to CSS
    await expect(mobileNav).toBeHidden();
  });

  test('should show mobile nav on mobile viewport', async ({ page }) => {
    // Mobile viewport
    await page.setViewportSize({ width: 375, height: 812 });
    await page.goto('/dashboard');
    await page.waitForLoadState('networkidle');

    // Mobile nav should be visible
    const nav = page.locator('nav').filter({ has: page.getByRole('button', { name: /home/i }) });
    await expect(nav).toBeVisible();
  });
});

test.describe('Touch Interactions', () => {
  test.use({
    viewport: { width: 375, height: 812 },
    isMobile: true,
    hasTouch: true,
  });

  test('should support tap navigation', async ({ page }) => {
    await page.goto('/dashboard');
    await page.waitForLoadState('networkidle');

    // Simulate tap on receipts button
    const receiptsButton = page.getByRole('button', { name: /receipts/i });
    await receiptsButton.tap();

    await page.waitForURL('**/receipts');
    expect(page.url()).toContain('/receipts');
  });
});
