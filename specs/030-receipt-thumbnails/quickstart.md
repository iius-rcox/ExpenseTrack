# Quickstart: Receipt Thumbnail Previews

**Feature**: 030-receipt-thumbnails
**Date**: 2026-01-08

## Prerequisites

- Docker Desktop running (for local development)
- .NET 8 SDK installed
- Node.js 20+ installed
- Azure Storage Emulator or connection to ccproctemp2025

## Quick Verification Steps

### 1. Verify Existing Thumbnail Generation

Thumbnails are already generated on upload. Verify with:

```bash
# Start the backend
cd backend
dotnet run --project src/ExpenseFlow.Api

# In another terminal, upload a test receipt
curl -X POST "http://localhost:5000/api/receipts/upload" \
  -H "Authorization: Bearer $DEV_TOKEN" \
  -F "files=@test-data/receipts/coffee_receipt.jpg"

# Check the response includes thumbnailUrl
# Response should contain:
# {
#   "receipts": [{
#     "thumbnailUrl": "https://ccproctemp2025.blob.core.windows.net/receipts/users/.../thumb.jpg",
#     ...
#   }]
# }
```

### 2. Verify Thumbnail Display in UI

```bash
# Start the frontend
cd frontend
pnpm install
pnpm dev

# Navigate to http://localhost:5173/receipts
# Uploaded receipts should display thumbnail images
# Receipts without thumbnails show placeholder icon
```

### 3. Verify PDF Thumbnail

```bash
# Upload a PDF receipt
curl -X POST "http://localhost:5000/api/receipts/upload" \
  -H "Authorization: Bearer $DEV_TOKEN" \
  -F "files=@test-data/receipts/hotel_invoice.pdf"

# The thumbnail should show the first page of the PDF
```

### 4. Verify HTML Thumbnail (if Chromium available)

```bash
# Check if HTML thumbnail service is available
curl "http://localhost:5000/api/health" | jq '.checks.htmlThumbnails'

# If available, upload an HTML receipt
curl -X POST "http://localhost:5000/api/receipts/upload" \
  -H "Authorization: Bearer $DEV_TOKEN" \
  -F "files=@test-data/receipts/amazon_order.html"
```

## Configuration Options

### appsettings.json

```json
{
  "ReceiptProcessing": {
    "Thumbnail": {
      "Width": 150,
      "Height": 150,
      "Quality": 80
    },
    "Html": {
      "ThumbnailViewportWidth": 800,
      "ThumbnailViewportHeight": 600
    }
  },
  "ThumbnailBackfill": {
    "BatchSize": 50,
    "DelayBetweenBatchesSeconds": 10
  }
}
```

### Environment Variables

| Variable | Purpose | Default |
|----------|---------|---------|
| `PUPPETEER_EXECUTABLE_PATH` | Path to Chromium for HTML thumbnails | Auto-detect |
| `THUMBNAIL_WIDTH` | Override thumbnail width | 150 |
| `THUMBNAIL_HEIGHT` | Override thumbnail height | 150 |

## Running Tests

### Unit Tests

```bash
cd backend
dotnet test --filter "FullyQualifiedName~ThumbnailService"
```

### Integration Tests

```bash
cd backend
dotnet test --filter "Category=Integration&FullyQualifiedName~Thumbnail"
```

### E2E Tests

```bash
cd frontend
pnpm playwright test thumbnail-display.spec.ts
```

## Troubleshooting

### Thumbnails Not Appearing

1. **Check ProcessReceiptJob logs**:
   ```bash
   kubectl logs -l app=expenseflow-api -n expenseflow-staging | grep -i thumbnail
   ```

2. **Verify blob storage access**:
   ```bash
   az storage blob list --container-name receipts --account-name ccproctemp2025 \
     --prefix "users/" --query "[?contains(name, 'thumb')]" | head -20
   ```

3. **Check receipt status**:
   ```bash
   curl "http://localhost:5000/api/receipts/{id}" \
     -H "Authorization: Bearer $DEV_TOKEN" | jq '.thumbnailUrl'
   ```

### HTML Thumbnails Not Working

1. **Check Chromium installation**:
   ```bash
   which chromium || which google-chrome
   ```

2. **Check service availability**:
   ```bash
   curl "http://localhost:5000/api/health" | jq '.checks'
   ```

3. **Set Chromium path**:
   ```bash
   export PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium
   ```

### PDF Thumbnails Failing

1. **Check Ghostscript installation** (required by Magick.NET):
   ```bash
   gs --version
   ```

2. **View PDF processing logs**:
   ```bash
   grep -i "pdf" /var/log/expenseflow/api.log
   ```

## Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| Thumbnail returns 404 | Blob not uploaded | Check ProcessReceiptJob completed |
| Thumbnail is blank | PDF has no first page content | Expected behavior for empty PDFs |
| HTML thumbnail service unavailable | Chromium not installed | Set `PUPPETEER_EXECUTABLE_PATH` or install Chromium |
| Slow thumbnail generation | Large PDF/HTML | Expected - processing is async |

## Next Steps After Implementation

1. **Run thumbnail backfill** for existing receipts:
   ```bash
   curl -X POST "http://localhost:5000/api/admin/thumbnails/backfill" \
     -H "Authorization: Bearer $ADMIN_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"batchSize": 50}'
   ```

2. **Monitor backfill progress**:
   ```bash
   curl "http://localhost:5000/api/admin/thumbnails/backfill/status" \
     -H "Authorization: Bearer $ADMIN_TOKEN"
   ```

3. **Verify all receipts have thumbnails**:
   ```sql
   SELECT COUNT(*) as total,
          COUNT(CASE WHEN "ThumbnailUrl" IS NOT NULL THEN 1 END) as with_thumbnail
   FROM "Receipts";
   ```
