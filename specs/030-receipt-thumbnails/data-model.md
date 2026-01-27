# Data Model: Receipt Thumbnail Previews

**Feature**: 030-receipt-thumbnails
**Date**: 2026-01-08

## Overview

This feature requires minimal data model changes since the core `Receipt` entity already includes thumbnail support. The focus is on documenting existing structure and any minor extensions.

---

## Existing Entities (No Changes Required)

### Receipt Entity

**Location**: `backend/src/ExpenseFlow.Core/Entities/Receipt.cs`

The Receipt entity already contains all necessary fields for thumbnail support:

```csharp
public class Receipt : BaseEntity
{
    // ... other properties ...

    /// <summary>URL to 200x200 thumbnail (stored in Azure Blob)</summary>
    public string? ThumbnailUrl { get; set; }  // Line 17

    // ... other properties ...
}
```

**Note**: The comment says "200x200" but implementation will use 150x150 per spec. Comment should be updated during implementation.

### Blob Storage Path Convention

Thumbnails are stored alongside original receipts with a predictable path:

```
Container: receipts
Path: users/{userId}/receipts/{receiptId}/
├── original.{ext}     # Original uploaded file
└── thumb.jpg          # Generated thumbnail (JPEG, 150x150)
```

**Location**: `backend/src/ExpenseFlow.Infrastructure/Services/BlobStorageService.cs`

---

## New Entity: ThumbnailBackfillProgress (Optional)

For large-scale backfill operations, an optional progress tracking table may be useful:

```csharp
/// <summary>
/// Tracks progress of thumbnail backfill job for resumability.
/// Optional - can use Hangfire job state instead.
/// </summary>
public class ThumbnailBackfillProgress
{
    public int Id { get; set; }

    /// <summary>Last processed receipt ID for resumption</summary>
    public Guid LastProcessedReceiptId { get; set; }

    /// <summary>Total receipts processed in this run</summary>
    public int ProcessedCount { get; set; }

    /// <summary>Total receipts that failed</summary>
    public int FailedCount { get; set; }

    /// <summary>Job start timestamp</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>Job completion timestamp</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Status: Running, Completed, Failed, Paused</summary>
    public string Status { get; set; } = "Running";
}
```

**Decision**: This entity is **OPTIONAL**. The Hangfire job system provides sufficient tracking for most use cases. Only implement if explicit progress reporting is required for admin UI.

---

## Database Schema

### Existing Column (Already Migrated)

```sql
-- Already exists in Receipts table
ALTER TABLE "Receipts"
ADD COLUMN IF NOT EXISTS "ThumbnailUrl" text;
```

### Index Recommendation

For the backfill job to efficiently find receipts without thumbnails:

```sql
-- Partial index for backfill queries
CREATE INDEX IF NOT EXISTS "IX_Receipts_ThumbnailUrl_Null"
ON "Receipts" ("Id")
WHERE "ThumbnailUrl" IS NULL;
```

**Note**: This index is optional and only beneficial if backfill query performance is an issue.

---

## Entity Relationships

```
┌─────────────────────────────────────────────────────────────────┐
│                          Receipt                                 │
├─────────────────────────────────────────────────────────────────┤
│ Id: Guid (PK)                                                   │
│ UserId: Guid (FK → Users)                                       │
│ BlobUrl: string (original file)                                 │
│ ThumbnailUrl: string? (generated thumbnail)  ◄─── THIS FEATURE │
│ OriginalFilename: string                                        │
│ ContentType: string                                             │
│ Status: ReceiptStatus                                           │
│ ...other fields...                                              │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ 1:1 (optional)
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Azure Blob Storage                          │
├─────────────────────────────────────────────────────────────────┤
│ Container: receipts                                             │
│ ├── users/{userId}/receipts/{receiptId}/original.{ext}        │
│ └── users/{userId}/receipts/{receiptId}/thumb.jpg              │
└─────────────────────────────────────────────────────────────────┘
```

---

## Validation Rules

### ThumbnailUrl

| Rule | Description |
|------|-------------|
| Nullable | Thumbnails may not exist for receipts pending processing |
| Format | Must be valid URL when present |
| Prefix | Must start with blob storage base URL |

### Thumbnail File

| Rule | Description |
|------|-------------|
| Format | JPEG (image/jpeg) |
| Dimensions | Maximum 150x150 pixels (fit-within) |
| Quality | 80% JPEG compression |
| Max Size | ~50KB typical for 150x150 JPEG |

---

## State Transitions

Thumbnail generation follows the receipt processing lifecycle:

```
Receipt Created (Upload)
         │
         ▼
    Status: Uploaded
    ThumbnailUrl: null
         │
         ▼
    Status: Processing
    (ProcessReceiptJob running)
         │
         ├── Success ──────────────────┐
         │                              ▼
         │                    Status: Ready/ReviewRequired
         │                    ThumbnailUrl: "https://..."
         │
         └── Failure ──────────────────┐
                                        ▼
                              Status: Error
                              ThumbnailUrl: null (fallback icon shown)
```

---

## DTO Mapping

### Existing DTOs (Already Include ThumbnailUrl)

| DTO | Field | Type | Location |
|-----|-------|------|----------|
| ReceiptSummaryDto | ThumbnailUrl | string? | `Shared/DTOs/ReceiptSummaryDto.cs` |
| ReceiptDetailDto | ThumbnailUrl | string? | `Shared/DTOs/ReceiptDetailDto.cs` |
| MatchReceiptSummary | ThumbnailUrl | string? | (API response) |

### Frontend Types (Already Include thumbnailUrl)

| Type | Field | Location |
|------|-------|----------|
| ReceiptSummary | thumbnailUrl | `frontend/src/types/api.ts:16` |
| MatchReceiptSummary | thumbnailUrl | `frontend/src/types/api.ts:123` |

---

## Migration Requirements

**No new migrations required** - the `ThumbnailUrl` column already exists in the database schema.

If the optional partial index is desired:

```sql
-- Migration: AddThumbnailNullIndex
CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Receipts_ThumbnailUrl_Null"
ON "Receipts" ("Id")
WHERE "ThumbnailUrl" IS NULL;
```

---

## Summary

| Entity | Action | Reason |
|--------|--------|--------|
| Receipt | No change | ThumbnailUrl already exists |
| Blob paths | Document only | Convention already established |
| ThumbnailBackfillProgress | Optional | Use Hangfire state by default |
| Partial index | Optional | Performance optimization |
