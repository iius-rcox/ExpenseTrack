# Research: Receipt Upload Pipeline

**Date**: 2025-12-05
**Feature Branch**: `003-receipt-pipeline`

## Summary

This document captures technical research for the Receipt Upload Pipeline feature. All "NEEDS CLARIFICATION" items from the Technical Context have been resolved.

---

## 1. Azure Document Intelligence Integration

### Decision
Use `Azure.AI.DocumentIntelligence` NuGet package (v1.0.0+) with the `prebuilt-receipt` model.

### Rationale
- Native .NET SDK with full async support
- Prebuilt receipt model extracts all required fields (vendor, date, amount, tax, line items)
- Pay-per-use pricing aligns with constitution cost principles (~$10/1000 receipts)
- Field-level confidence scores enable 60% threshold implementation

### Alternatives Considered
| Alternative | Rejected Because |
|-------------|------------------|
| Azure Form Recognizer (legacy SDK) | Deprecated in favor of Document Intelligence |
| Google Cloud Vision | Additional vendor complexity, not in Azure ecosystem |
| Tesseract OCR | No structured receipt extraction, manual parsing required |

### Implementation Details

**NuGet Package:**
```bash
dotnet add package Azure.AI.DocumentIntelligence --version 1.0.0
```

**Key Fields Extracted:**
- `MerchantName` - vendor name
- `TransactionDate` - receipt date
- `Total` - total amount (currency type)
- `TotalTax` - tax amount
- `Items` - line items array with Description, Quantity, Price, TotalPrice

**Confidence Score Pattern:**
```csharp
if (document.Fields.TryGetValue("MerchantName", out var merchantField))
{
    var vendor = merchantField.ValueString;
    var confidence = merchantField.Confidence; // 0.0 to 1.0

    if (confidence < 0.60)
    {
        receipt.Status = ReceiptStatus.ReviewRequired;
    }
}
```

**Error Handling:**
- Use Polly for retry on 429 (rate limit) and 5xx errors
- 3 retries with exponential backoff (1s, 2s, 4s)
- Fail immediately on 400 (bad request) - invalid document

---

## 2. HEIC to JPG Conversion

### Decision
Use `Magick.NET-Q16-AnyCPU` (ImageMagick binding) instead of SkiaSharp.

### Rationale
- **SkiaSharp does NOT support HEIC** - confirmed via GitHub issues #1700 and #2887
- Magick.NET is mature, feature-rich, and handles HEIC natively
- Cross-platform support with native library bundled
- Simple API for format conversion

### Alternatives Considered
| Alternative | Rejected Because |
|-------------|------------------|
| SkiaSharp | Does not support HEIC format (patent/licensing issues) |
| Openize.HEIC | Requires WPF dependencies for bitmap output, less mature |
| Cloud conversion service | Additional latency and cost |

### Implementation Details

**NuGet Package:**
```bash
dotnet add package Magick.NET-Q16-AnyCPU
```

**Conversion Pattern:**
```csharp
using ImageMagick;

public async Task<Stream> ConvertHeicToJpgAsync(Stream heicStream)
{
    heicStream.Position = 0;
    using var image = new MagickImage(heicStream);
    image.Format = MagickFormat.Jpeg;
    image.Quality = 85;

    var outputStream = new MemoryStream();
    await image.WriteAsync(outputStream);
    outputStream.Position = 0;
    return outputStream;
}
```

**Memory Considerations:**
- Process files up to 25MB (per FR-004)
- Use `MagickReadSettings { Density = 150 }` for large files
- Dispose images immediately after conversion

---

## 3. Azure Blob Lifecycle Management

### Decision
Configure Azure Blob Storage lifecycle management policy for 30-day auto-deletion.

### Rationale
- Built-in Azure feature, no custom code needed
- Free to configure (only charged for underlying operations)
- Supports prefix-based filtering for `receipts/{userId}/` paths
- Database cleanup handled separately via Hangfire job

### Alternatives Considered
| Alternative | Rejected Because |
|-------------|------------------|
| Custom Hangfire job for deletion | More code to maintain, potential for orphaned blobs |
| Azure Functions timer trigger | Additional infrastructure component |
| Manual deletion | Not scalable, easy to forget |

### Implementation Details

**Azure CLI Configuration:**
```bash
az storage account management-policy create \
  --account-name ccproctemp2025 \
  --resource-group rg_prod \
  --policy @lifecycle-policy.json
```

**lifecycle-policy.json:**
```json
{
  "rules": [
    {
      "name": "deleteReceiptsAfter30Days",
      "enabled": true,
      "type": "Lifecycle",
      "definition": {
        "filters": {
          "blobTypes": ["blockBlob"],
          "prefixMatch": ["receipts/"]
        },
        "actions": {
          "baseBlob": {
            "delete": {
              "daysAfterModificationGreaterThan": 30
            }
          }
        }
      }
    }
  ]
}
```

**Database Cleanup (Hangfire):**
```csharp
// Separate job to delete Receipt records where created_at > 30 days
RecurringJob.AddOrUpdate<ReceiptCleanupJob>(
    "cleanup-expired-receipts",
    job => job.ExecuteAsync(CancellationToken.None),
    "0 3 * * *", // Daily at 3 AM
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Local });
```

---

## 4. Microsoft Defender for Storage

### Decision
Enable Microsoft Defender for Storage on `ccproctemp2025` storage account.

### Rationale
- Native Azure service, automatic scanning of all blob uploads
- No code changes required - scans happen automatically
- Alerts on malware detection integrated with Azure Security Center
- Per spec clarification: FR-027 requires malware scanning

### Implementation Details

**Azure CLI Enablement:**
```bash
az security pricing create \
  --name StorageAccounts \
  --tier Standard
```

**Verification:**
```bash
az security pricing show --name StorageAccounts
```

**Cost:** ~$10/month for first 1M transactions, then $0.15 per additional 10K transactions

---

## 5. Thumbnail Generation

### Decision
Generate thumbnails during ProcessReceiptJob using Magick.NET (already added for HEIC).

### Rationale
- Single library for both HEIC conversion and thumbnail generation
- Generate 200x200 thumbnails for list view
- Store thumbnails in separate blob path: `thumbnails/{userId}/{year}/{month}/`

### Implementation Details

```csharp
public async Task<Stream> GenerateThumbnailAsync(Stream imageStream)
{
    imageStream.Position = 0;
    using var image = new MagickImage(imageStream);
    image.Resize(new MagickGeometry(200, 200) {
        IgnoreAspectRatio = false,
        Greater = true  // Only shrink, don't enlarge
    });
    image.Format = MagickFormat.Jpeg;
    image.Quality = 75;

    var outputStream = new MemoryStream();
    await image.WriteAsync(outputStream);
    outputStream.Position = 0;
    return outputStream;
}
```

---

## Updated Dependencies

Based on research, the final NuGet package list for Sprint 3:

| Package | Version | Purpose |
|---------|---------|---------|
| Azure.AI.DocumentIntelligence | 1.0.0 | Receipt OCR extraction |
| Azure.Storage.Blobs | 12.x | Blob storage operations |
| Magick.NET-Q16-AnyCPU | 14.x | HEIC conversion + thumbnails |
| Microsoft.Extensions.Http.Polly | 8.x | Retry policies |

**Note:** SkiaSharp removed from plan - does not support HEIC.

---

## References

- [Azure Document Intelligence .NET SDK](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/ai.documentintelligence-readme)
- [Prebuilt Receipt Model](https://learn.microsoft.com/en-us/azure/ai-services/document-intelligence/prebuilt/receipt)
- [Magick.NET Documentation](https://github.com/dlemstra/Magick.NET)
- [Azure Blob Lifecycle Management](https://learn.microsoft.com/en-us/azure/storage/blobs/lifecycle-management-overview)
- [Microsoft Defender for Storage](https://learn.microsoft.com/en-us/azure/defender-for-cloud/defender-for-storage-introduction)
