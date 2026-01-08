# Quickstart: HTML Receipt Parsing

**Feature**: 029-html-receipt-parsing
**Date**: 2026-01-08

## Prerequisites

- .NET 8 SDK installed
- Docker Desktop running (for headless Chrome)
- Azure OpenAI access configured (existing ExpenseFlow setup)

## Setup Steps

### 1. Install New NuGet Packages

```bash
cd backend/src/ExpenseFlow.Infrastructure
dotnet add package PuppeteerSharp --version 19.0.2
dotnet add package HtmlSanitizer --version 8.1.870
dotnet add package HtmlAgilityPack --version 1.11.61
```

### 2. Update Configuration

Add to `appsettings.json`:

```json
{
  "ReceiptProcessing": {
    "AllowedContentTypes": [
      "image/jpeg",
      "image/png",
      "image/heic",
      "image/heif",
      "application/pdf",
      "text/html"
    ],
    "Html": {
      "MaxSizeBytes": 5242880,
      "ExtractionPromptVersion": "1.0",
      "ThumbnailViewportWidth": 800,
      "ThumbnailViewportHeight": 600,
      "StoreFailedExtractions": true,
      "ConfidenceThreshold": 0.60
    }
  }
}
```

### 3. Download Chromium for PuppeteerSharp

On first run, PuppeteerSharp will download Chromium. For Docker, add to Dockerfile:

```dockerfile
# Install Chromium dependencies
RUN apt-get update && apt-get install -y \
    chromium \
    --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium
```

### 4. Register Services

In `ServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<IHtmlReceiptExtractionService, HtmlReceiptExtractionService>();
services.AddScoped<IHtmlSanitizationService, HtmlSanitizationService>();
services.AddScoped<IHtmlThumbnailService, HtmlThumbnailService>();
```

## Quick Test

### Upload HTML Receipt

```bash
# Save a receipt email as .html file, then:
curl -X POST https://localhost:5001/api/receipts \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@amazon-receipt.html;type=text/html"
```

### Expected Response

```json
{
  "id": "abc123...",
  "status": "Processing",
  "contentType": "text/html",
  "originalFilename": "amazon-receipt.html"
}
```

### Check Processing Status

```bash
curl https://localhost:5001/api/receipts/abc123... \
  -H "Authorization: Bearer $TOKEN"
```

After processing completes:

```json
{
  "id": "abc123...",
  "status": "Ready",
  "contentType": "text/html",
  "vendorExtracted": "Amazon.com",
  "dateExtracted": "2025-12-15",
  "amountExtracted": 127.43,
  "confidenceScore": 0.92,
  "thumbnailUrl": "https://...",
  "sanitizedHtmlUrl": "/api/receipts/abc123.../html"
}
```

## Sample HTML Receipts for Testing

Create test files in `test-data/html-receipts/`:

1. `amazon-order.html` - Amazon order confirmation
2. `uber-ride.html` - Uber ride receipt
3. `airline-booking.html` - Airline confirmation email
4. `malformed.html` - Invalid HTML for error testing
5. `no-receipt-data.html` - HTML without receipt content

## Verification Checklist

### Backend Verification (T040)

Run these checks against the staging API:

#### 1. Upload HTML Receipt
- [ ] Upload `test-data/html-receipts/amazon-order.html` via API or frontend
- [ ] Verify 201 Created response with status "Uploaded"
- [ ] Confirm contentType is "text/html"

#### 2. Processing Verification
- [ ] Poll receipt status until "Ready" (should complete within 30 seconds)
- [ ] Verify extracted vendor: "Amazon.com" or similar
- [ ] Verify extracted date: "2026-01-05" (January 5, 2026)
- [ ] Verify extracted amount: $127.43
- [ ] Verify tax extracted: $6.48
- [ ] Confirm confidence scores present (vendor, date, amount)

#### 3. Thumbnail Generation
- [ ] Verify thumbnailUrl is populated
- [ ] Thumbnail displays in receipts list view
- [ ] Fallback placeholder shown if Chromium unavailable

#### 4. HTML Viewing (GET /api/receipts/{id}/html)
- [ ] Returns 200 OK with Content-Type: text/html
- [ ] Content-Security-Policy header present (script-src 'none')
- [ ] HTML content is sanitized (no script tags)
- [ ] Renders correctly in frontend document viewer
- [ ] Returns 400 Bad Request for non-HTML receipts (e.g., PDF)

### Frontend Verification

- [ ] HTML receipts show "(sanitized, scripts blocked)" label in viewer
- [ ] Loading spinner appears while fetching HTML content
- [ ] Error state displayed if HTML fetch fails
- [ ] "Open Original" button links to raw blob storage URL

### Edge Case Testing

Using test files in `test-data/html-receipts/`:

| Test File | Expected Result |
|-----------|-----------------|
| `amazon-order.html` | Full extraction: vendor, date, amount, line items |
| `uber-ride.html` | Full extraction: Uber, $57.00, ride details |
| `airline-booking.html` | Full extraction: United Airlines, $549.40, flight info |
| `malformed.html` | Graceful handling, partial extraction, XSS stripped |
| `no-receipt-data.html` | Low confidence or error status (not a receipt) |

### Extraction Metrics Logging

Check application logs for structured logging entries:
- [ ] `HtmlReceiptExtraction.Started` - on extraction begin
- [ ] `HtmlReceiptExtraction.Completed` - with processing time, confidence scores
- [ ] `HtmlReceiptExtraction.Failed` - on extraction errors with reason

## Troubleshooting

### "Chromium not found" Error

```bash
# Force download Chromium
dotnet run --project ExpenseFlow.Api -- --download-chromium
```

### "AI extraction timeout"

Check Azure OpenAI quota limits. HTML receipts use ~2000 tokens per extraction.

### Thumbnail generation fails

Verify Docker container has Chromium dependencies:

```bash
docker exec -it expenseflow-api chromium --version
```
