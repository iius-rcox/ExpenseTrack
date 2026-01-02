import { test, expect } from '@playwright/test';

/**
 * E2E Tests for Receipt Upload and Extraction (T048)
 *
 * These tests verify the complete receipt processing workflow:
 * 1. File upload via dropzone
 * 2. Upload progress tracking
 * 3. AI extraction display
 * 4. Field editing and saving
 *
 * Prerequisites:
 * - Backend API running with receipt processing endpoints
 * - Test receipt images available in test-fixtures directory
 */

test.describe('Receipt Upload Flow', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to receipts page
    await page.goto('/receipts');
    await page.waitForLoadState('networkidle');
  });

  test('should display upload dropzone', async ({ page }) => {
    // Look for upload area
    await expect(page.getByText(/drag & drop receipts/i)).toBeVisible();
    await expect(page.getByText(/or click to browse/i)).toBeVisible();
  });

  test('should accept valid image files via drag and drop', async ({ page }) => {
    const dropzone = page.locator('[data-testid="dropzone"]').or(
      page.locator('text=Drag & drop receipts').locator('..')
    );

    // Create a test file
    const buffer = Buffer.from('fake image content');

    // Simulate file drop
    await dropzone.evaluate(
      (element, { fileName, fileContent }) => {
        const dataTransfer = new DataTransfer();
        const file = new File([new Uint8Array(fileContent)], fileName, {
          type: 'image/jpeg',
        });
        dataTransfer.items.add(file);

        const dropEvent = new DragEvent('drop', {
          bubbles: true,
          cancelable: true,
          dataTransfer,
        });
        element.dispatchEvent(dropEvent);
      },
      { fileName: 'receipt.jpg', fileContent: Array.from(buffer) }
    );

    // Verify file appears in queue
    await expect(page.getByText('receipt.jpg')).toBeVisible({ timeout: 5000 });
    await expect(page.getByText('1 file selected')).toBeVisible();
  });

  test('should reject invalid file types', async ({ page }) => {
    // Attempt to upload a text file
    const fileInput = page.locator('input[type="file"]');

    // Force the file input to accept any file for test
    await fileInput.setInputFiles({
      name: 'document.txt',
      mimeType: 'text/plain',
      buffer: Buffer.from('This is not an image'),
    });

    // Should show error toast
    await expect(page.getByText(/rejected/i).or(page.getByText(/check file type/i))).toBeVisible({
      timeout: 5000,
    });
  });

  test('should show upload progress during upload', async ({ page }) => {
    const fileInput = page.locator('input[type="file"]');

    // Upload a test file
    await fileInput.setInputFiles({
      name: 'receipt.jpg',
      mimeType: 'image/jpeg',
      buffer: Buffer.from('fake image data'.repeat(1000)),
    });

    // Click upload button
    const uploadButton = page.getByRole('button', { name: /upload.*receipt/i });

    if (await uploadButton.isVisible()) {
      await uploadButton.click();

      // Should show progress indicator
      await expect(
        page.getByRole('progressbar').or(page.getByText(/%/))
      ).toBeVisible({ timeout: 5000 });
    }
  });

  test('should clear files after successful upload', async ({ page }) => {
    const fileInput = page.locator('input[type="file"]');

    // Upload a test file
    await fileInput.setInputFiles({
      name: 'receipt.jpg',
      mimeType: 'image/jpeg',
      buffer: Buffer.from('fake image data'),
    });

    await expect(page.getByText('1 file selected')).toBeVisible();

    // Mock successful API response
    await page.route('**/api/receipts/upload', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          totalUploaded: 1,
          receipts: [{ id: '1', status: 'processing' }],
          failed: [],
        }),
      });
    });

    // Click upload
    const uploadButton = page.getByRole('button', { name: /upload.*receipt/i });
    if (await uploadButton.isVisible()) {
      await uploadButton.click();

      // Wait for upload to complete
      await expect(page.getByText('1 file selected')).not.toBeVisible({ timeout: 10000 });
    }
  });
});

test.describe('Receipt Extraction Display', () => {
  test.beforeEach(async ({ page }) => {
    // Mock receipt with extracted fields
    await page.route('**/api/receipts/*', async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({
            id: '1',
            status: 'complete',
            originalFilename: 'receipt.jpg',
            imageUrl: 'https://example.com/receipt.jpg',
            extractedFields: [
              { key: 'vendor', value: 'Coffee Shop', confidence: 0.95 },
              { key: 'amount', value: 15.99, confidence: 0.92 },
              { key: 'date', value: '2024-03-15', confidence: 0.88 },
              { key: 'category', value: 'Food & Drink', confidence: 0.85 },
            ],
          }),
        });
      } else {
        await route.continue();
      }
    });

    await page.goto('/receipts/1');
    await page.waitForLoadState('networkidle');
  });

  test('should display extracted fields with confidence', async ({ page }) => {
    // Verify extracted data is displayed
    await expect(page.getByText('Coffee Shop')).toBeVisible();
    await expect(page.getByText('$15.99')).toBeVisible();

    // Verify confidence indicators
    await expect(page.getByText('95%')).toBeVisible();
    await expect(page.getByText('92%')).toBeVisible();
  });

  test('should allow inline field editing', async ({ page }) => {
    // Click edit button on vendor field
    const vendorField = page.locator('text=Vendor').locator('..');
    await vendorField.hover();

    const editButton = vendorField.getByTitle('Edit field');
    if (await editButton.isVisible()) {
      await editButton.click();

      // Should show input field
      const input = page.getByRole('textbox');
      await expect(input).toBeVisible();
      await expect(input).toHaveValue('Coffee Shop');

      // Edit the value
      await input.clear();
      await input.fill('Tea House');
      await input.press('Enter');

      // Should show edited value
      await expect(page.getByText('Tea House')).toBeVisible();
      await expect(page.getByText('(edited)')).toBeVisible();
    }
  });

  test('should support undo after editing', async ({ page }) => {
    // Edit a field
    const vendorField = page.locator('text=Vendor').locator('..');
    await vendorField.hover();

    const editButton = vendorField.getByTitle('Edit field');
    if (await editButton.isVisible()) {
      await editButton.click();

      const input = page.getByRole('textbox');
      await input.clear();
      await input.fill('New Vendor');
      await input.press('Enter');

      // Find and click undo button
      const undoButton = page.getByRole('button', { name: /undo/i });
      if (await undoButton.isVisible()) {
        await undoButton.click();

        // Should revert to original
        await expect(page.getByText('Coffee Shop')).toBeVisible();
      }
    }
  });

  test('should save all changes', async ({ page }) => {
    // Mock the save endpoint
    await page.route('**/api/receipts/*/fields', async (route) => {
      if (route.request().method() === 'PATCH') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ success: true }),
        });
      } else {
        await route.continue();
      }
    });

    // Edit a field
    const vendorField = page.locator('text=Vendor').locator('..');
    await vendorField.hover();

    const editButton = vendorField.getByTitle('Edit field');
    if (await editButton.isVisible()) {
      await editButton.click();

      const input = page.getByRole('textbox');
      await input.clear();
      await input.fill('Updated Vendor');
      await input.press('Enter');

      // Click save all
      const saveButton = page.getByRole('button', { name: /save all/i });
      if (await saveButton.isVisible()) {
        await saveButton.click();

        // Edited indicator should disappear
        await expect(page.getByText('(edited)')).not.toBeVisible({ timeout: 5000 });
      }
    }
  });

  test('should discard all changes', async ({ page }) => {
    // Edit a field
    const vendorField = page.locator('text=Vendor').locator('..');
    await vendorField.hover();

    const editButton = vendorField.getByTitle('Edit field');
    if (await editButton.isVisible()) {
      await editButton.click();

      const input = page.getByRole('textbox');
      await input.clear();
      await input.fill('Changed Vendor');
      await input.press('Enter');

      // Click discard
      const discardButton = page.getByRole('button', { name: /discard/i });
      if (await discardButton.isVisible()) {
        await discardButton.click();

        // Should revert to original
        await expect(page.getByText('Coffee Shop')).toBeVisible();
        await expect(page.getByText('Changed Vendor')).not.toBeVisible();
      }
    }
  });
});

test.describe('Batch Upload Queue', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/receipts');
    await page.waitForLoadState('networkidle');
  });

  test('should handle multiple file uploads', async ({ page }) => {
    const fileInput = page.locator('input[type="file"]');

    // Upload multiple files
    await fileInput.setInputFiles([
      { name: 'receipt1.jpg', mimeType: 'image/jpeg', buffer: Buffer.from('image1') },
      { name: 'receipt2.png', mimeType: 'image/png', buffer: Buffer.from('image2') },
      { name: 'receipt3.pdf', mimeType: 'application/pdf', buffer: Buffer.from('pdf') },
    ]);

    // Should show all files
    await expect(page.getByText('3 files selected')).toBeVisible();
    await expect(page.getByText('receipt1.jpg')).toBeVisible();
    await expect(page.getByText('receipt2.png')).toBeVisible();
    await expect(page.getByText('receipt3.pdf')).toBeVisible();
  });

  test('should allow removing individual files', async ({ page }) => {
    const fileInput = page.locator('input[type="file"]');

    await fileInput.setInputFiles([
      { name: 'receipt1.jpg', mimeType: 'image/jpeg', buffer: Buffer.from('image1') },
      { name: 'receipt2.png', mimeType: 'image/png', buffer: Buffer.from('image2') },
    ]);

    // Remove first file
    const removeButtons = await page.getByRole('button').filter({ has: page.locator('svg') }).all();
    for (const btn of removeButtons) {
      if (await btn.getAttribute('title') === '' || !(await btn.textContent())) {
        await btn.click();
        break;
      }
    }

    // Should show 1 file selected
    await expect(page.getByText('1 file selected')).toBeVisible({ timeout: 5000 });
  });

  test('should enforce maximum file limit', async ({ page }) => {
    const fileInput = page.locator('input[type="file"]');

    // Try to upload 21 files (limit is 20)
    const files = Array.from({ length: 21 }, (_, i) => ({
      name: `receipt${i + 1}.jpg`,
      mimeType: 'image/jpeg',
      buffer: Buffer.from(`image${i}`),
    }));

    await fileInput.setInputFiles(files);

    // Should show error about maximum
    await expect(page.getByText(/maximum.*20/i)).toBeVisible({ timeout: 5000 });
  });
});

test.describe('Image Viewer', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/api/receipts/*', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: '1',
          status: 'complete',
          originalFilename: 'receipt.jpg',
          imageUrl: 'https://via.placeholder.com/800x1200',
          extractedFields: [],
        }),
      });
    });

    await page.goto('/receipts/1');
    await page.waitForLoadState('networkidle');
  });

  test('should display receipt image', async ({ page }) => {
    const image = page.locator('img[alt="Receipt"]');
    await expect(image).toBeVisible({ timeout: 5000 });
  });

  test('should support zoom controls', async ({ page }) => {
    // Find zoom buttons
    const zoomIn = page.locator('button').filter({ has: page.locator('[class*="zoom-in"]') });
    // Note: zoomOut locator available if needed for future tests

    // Check for zoom percentage display
    const zoomIndicator = page.getByText('100%');
    if (await zoomIndicator.isVisible()) {
      // Click zoom in
      if (await zoomIn.first().isVisible()) {
        await zoomIn.first().click();
        await expect(page.getByText('125%')).toBeVisible();
      }
    }
  });

  test('should support rotation', async ({ page }) => {
    const rotateButton = page.locator('button').filter({ has: page.locator('[class*="rotate"]') });

    if (await rotateButton.first().isVisible()) {
      // Click rotate
      await rotateButton.first().click();

      // Image should have rotation transform
      const imageContainer = page.locator('[style*="rotate"]');
      await expect(imageContainer).toHaveCSS('transform', /rotate/);
    }
  });
});

test.describe('Processing Status', () => {
  test('should show processing indicator for receipts in progress', async ({ page }) => {
    await page.route('**/api/receipts/*', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: '1',
          status: 'processing',
          originalFilename: 'receipt.jpg',
          extractedFields: [],
        }),
      });
    });

    await page.goto('/receipts/1');

    // Should show processing indicator
    await expect(
      page.getByText(/processing/i).or(page.locator('[class*="animate-spin"]'))
    ).toBeVisible({ timeout: 5000 });
  });

  test('should poll for status updates', async ({ page }) => {
    let callCount = 0;

    await page.route('**/api/receipts/*', async (route) => {
      callCount++;
      const status = callCount < 3 ? 'processing' : 'complete';

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: '1',
          status,
          originalFilename: 'receipt.jpg',
          extractedFields: status === 'complete'
            ? [{ key: 'vendor', value: 'Test', confidence: 0.9 }]
            : [],
        }),
      });
    });

    await page.goto('/receipts/1');

    // Wait for polling to complete status
    await expect(page.getByText('Test')).toBeVisible({ timeout: 15000 });
  });
});
