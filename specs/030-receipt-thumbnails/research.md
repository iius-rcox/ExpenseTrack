# Research: Receipt Thumbnail Previews

**Feature**: 030-receipt-thumbnails
**Date**: 2026-01-08

## Executive Summary

This feature is primarily about leveraging and extending existing infrastructure rather than introducing new technology. The core thumbnail generation services already exist and work correctly. Research focuses on validating existing approaches, identifying optimal configurations, and documenting decisions for the remaining gaps.

---

## Research Item 1: Thumbnail Dimensions

### Question
Should thumbnail dimensions be 150x150 (per spec) or 200x200 (current implementation)?

### Decision
**Use 150x150 as configurable default**

### Rationale
- Spec explicitly states 150x150 in FR-010
- 150x150 provides sufficient visual identification in list views
- Smaller thumbnails = faster loading, less storage
- Current 200x200 is slightly oversized for typical UI display at 64px-100px

### Alternatives Considered
| Option | Pros | Cons |
|--------|------|------|
| Keep 200x200 | No changes needed, higher quality | Larger files, spec mismatch |
| 150x150 (chosen) | Spec compliant, optimized size | Minor resize logic change |
| Multiple sizes | Future-proof | Over-engineering for current needs |

### Implementation
```csharp
// appsettings.json
"ReceiptProcessing": {
  "Thumbnail": {
    "Width": 150,
    "Height": 150,
    "Quality": 80
  }
}
```

---

## Research Item 2: Image Thumbnail Library

### Question
Is Magick.NET the right choice for image/PDF thumbnail generation?

### Decision
**Keep Magick.NET (ImageMagick)**

### Rationale
- Already implemented and working (`ThumbnailService.cs`)
- Excellent PDF rendering via Ghostscript backend
- Handles HEIC/HEIF (Apple photos) natively
- Battle-tested, production-ready

### Alternatives Considered
| Option | Pros | Cons |
|--------|------|------|
| Magick.NET (current) | Full format support, PDF first-page | Large dependency |
| SixLabors.ImageSharp | Pure .NET, lightweight | No PDF support |
| SkiaSharp | Fast, cross-platform | Limited PDF support |
| System.Drawing | Built-in | Not cross-platform safe |

### Existing Implementation
```csharp
// ThumbnailService.cs line 45-58 - already handles PDFs
if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
{
    var settings = new MagickReadSettings
    {
        Density = new Density(150),
        FrameIndex = 0,
        FrameCount = 1
    };
    image = new MagickImage(inputStream, settings);
}
```

---

## Research Item 3: HTML Thumbnail Rendering

### Question
Is PuppeteerSharp the right choice for HTML receipt thumbnails?

### Decision
**Keep PuppeteerSharp**

### Rationale
- Already implemented and working (`HtmlThumbnailService.cs`)
- Accurate rendering of complex HTML (email receipts)
- Shared browser instance for efficiency
- Graceful degradation when Chromium unavailable

### Alternatives Considered
| Option | Pros | Cons |
|--------|------|------|
| PuppeteerSharp (current) | Full CSS/JS support, accurate | Heavy (Chromium), slow startup |
| Playwright .NET | Similar to Puppeteer, newer | Same overhead, different API |
| wkhtmltoimage | Lightweight | Deprecated, limited CSS3 |
| Html2Canvas + Node | JavaScript-based | Requires Node.js service |

### Existing Implementation
Key features already handled:
- Lazy browser initialization (line 154-238)
- Viewport configuration (800x600 default)
- DOM load waiting
- Screenshot cropping to thumbnail size

---

## Research Item 4: Backfill Job Strategy

### Question
How should thumbnails be generated for existing receipts?

### Decision
**Hangfire recurring job with batch processing**

### Rationale
- Consistent with existing background job pattern (ProcessReceiptJob)
- Batching prevents memory/resource exhaustion
- Resume capability on failure
- Can run during off-peak hours

### Implementation Design
```csharp
[Queue("thumbnails")]
public class ThumbnailBackfillJob
{
    private const int BatchSize = 50;

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(int? skip = null)
    {
        // Get receipts without thumbnails
        var receipts = await _receiptRepository
            .GetReceiptsWithoutThumbnailsAsync(BatchSize, skip ?? 0);

        foreach (var receipt in receipts)
        {
            await GenerateThumbnailAsync(receipt);
        }

        // Schedule next batch if more exist
        if (receipts.Count == BatchSize)
        {
            BackgroundJob.Schedule<ThumbnailBackfillJob>(
                x => x.ExecuteAsync((skip ?? 0) + BatchSize),
                TimeSpan.FromSeconds(10));
        }
    }
}
```

### Configuration
```json
"ThumbnailBackfill": {
  "BatchSize": 50,
  "DelayBetweenBatchesSeconds": 10,
  "MaxConcurrentJobs": 1
}
```

---

## Research Item 5: Cascade Delete Behavior

### Question
How are thumbnails deleted when receipts are deleted?

### Decision
**No additional implementation needed**

### Rationale
Current behavior is correct:
1. `Receipt.ThumbnailUrl` stores the blob URL
2. When receipt is deleted, `BlobStorageService.DeleteAsync` is called
3. The thumbnail URL follows predictable pattern: `users/{userId}/receipts/{receiptId}/thumb.jpg`
4. Blob deletion handles both original and thumbnail

### Verification Needed
- Confirm `ReceiptsController.DeleteAsync` or repository calls blob cleanup
- Add explicit test for cascade behavior

---

## Research Item 6: Frontend Preview Component

### Question
What technology should power the receipt preview modal with zoom/pan?

### Decision
**Use react-zoom-pan-pinch library**

### Rationale
- Purpose-built for image zoom/pan interactions
- Works well with existing shadcn/ui Dialog
- Lightweight (~15KB gzipped)
- Touch-friendly for mobile support

### Alternatives Considered
| Option | Pros | Cons |
|--------|------|------|
| react-zoom-pan-pinch | Purpose-built, lightweight | New dependency |
| CSS transform manual | No dependency | Complex gesture handling |
| react-image-gallery | Full gallery features | Overkill for single preview |
| PhotoSwipe | Feature-rich | Heavy, complex integration |

### Implementation Sketch
```tsx
import { TransformWrapper, TransformComponent } from 'react-zoom-pan-pinch';

function ReceiptPreviewModal({ receipt }: { receipt: ReceiptDetail }) {
  return (
    <Dialog>
      <DialogContent className="max-w-4xl">
        <TransformWrapper>
          <TransformComponent>
            <img
              src={receipt.blobUrl}
              alt={receipt.originalFilename}
              className="max-h-[80vh] object-contain"
            />
          </TransformComponent>
        </TransformWrapper>
      </DialogContent>
    </Dialog>
  );
}
```

---

## Research Item 7: Progressive Loading

### Question
How should thumbnails load in long lists?

### Decision
**Native browser lazy loading + intersection observer fallback**

### Rationale
- Browser native `loading="lazy"` attribute works in all modern browsers
- Intersection Observer for progressive enhancement
- No additional library needed
- Placeholder shown until thumbnail loads

### Implementation
```tsx
// Already partially implemented in ReceiptCard
<img
  src={receipt.thumbnailUrl}
  alt={receipt.originalFilename}
  loading="lazy"  // Add this attribute
  className="object-cover w-full h-full"
/>
```

---

## Summary of Decisions

| Decision | Choice | Reason |
|----------|--------|--------|
| Thumbnail Size | 150x150 | Spec compliance, optimized storage |
| Image Library | Magick.NET | Already working, full format support |
| HTML Library | PuppeteerSharp | Already working, accurate rendering |
| Backfill Strategy | Hangfire batch job | Consistent pattern, resumable |
| Cascade Delete | Existing behavior | Already correct |
| Preview Zoom | react-zoom-pan-pinch | Lightweight, purpose-built |
| Lazy Loading | Native + IO | No new dependencies |

---

## Open Questions Resolved

All NEEDS CLARIFICATION items from the spec have been addressed:

1. ✅ Aspect ratio handling → Fit-within with padding (clarified in spec)
2. ✅ Retry strategy → Exponential backoff 1min/5min/30min (clarified in spec)
3. ✅ Cascade delete → Immediate deletion (clarified in spec)
