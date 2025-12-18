# Data Model: Testing & Cache Warming

**Feature Branch**: `010-testing-cache-warming`
**Date**: 2025-12-17

## New Entities

### ImportJob

Tracks the status and progress of cache warming import operations.

```csharp
public class ImportJob : BaseEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SourceFileName { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public ImportJobStatus Status { get; set; }
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int CachedDescriptions { get; set; }
    public int CreatedAliases { get; set; }
    public int GeneratedEmbeddings { get; set; }
    public int SkippedRecords { get; set; }
    public string? ErrorLog { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}

public enum ImportJobStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
```

**Fields**:
| Field | Type | Description | Constraints |
|-------|------|-------------|-------------|
| Id | Guid | Primary key | Required, Generated |
| UserId | Guid | User who initiated import | Required, FK to Users |
| SourceFileName | string | Original uploaded filename | Required, Max 255 |
| BlobUrl | string | Azure Blob Storage URL | Required, Max 500 |
| Status | enum | Current job status | Required |
| TotalRecords | int | Total records in source file | Required, ≥0 |
| ProcessedRecords | int | Records processed so far | Required, ≥0 |
| CachedDescriptions | int | New DescriptionCache entries | Required, ≥0 |
| CreatedAliases | int | New VendorAlias entries | Required, ≥0 |
| GeneratedEmbeddings | int | New ExpenseEmbedding entries | Required, ≥0 |
| SkippedRecords | int | Records skipped due to errors | Required, ≥0 |
| ErrorLog | string? | Error details (JSON array) | Optional, Max 10000 |
| StartedAt | DateTime | When processing began | Required |
| CompletedAt | DateTime? | When processing finished | Nullable |

**Indexes**:
- `IX_ImportJob_UserId_Status` on (UserId, Status) - Filter by user and status
- `IX_ImportJob_StartedAt` on (StartedAt DESC) - Recent jobs first

## Existing Entities (Utilized)

### DescriptionCache

Already exists. Used to store normalized descriptions from historical data.

**Key Operations During Import**:
- Check if `RawDescriptionHash` already exists
- If exists: increment `HitCount`
- If new: insert with `Verified = true` (from historical data)

### VendorAlias

Already exists. Used to store vendor patterns extracted from historical data.

**Key Operations During Import**:
- Extract vendor pattern from description (e.g., "DELTA" from "DELTA AIR 0062363598531")
- Check if `CanonicalName` already exists
- If exists: update `MatchCount`, verify GL code matches
- If new: create with `DefaultGLCode` and `DefaultDepartment` from historical record

### ExpenseEmbedding

Already exists. Used to store embeddings for verified historical expense descriptions.

**Key Operations During Import**:
- Generate embedding for normalized description
- Check if similar embedding exists (cosine similarity >0.98)
- If very similar exists: skip (avoid near-duplicates)
- If new enough: insert with `Verified = true`

### StatementFingerprint

Already exists. Pre-configured fingerprints can be added during cache warming.

**Key Operations During Import** (Optional):
- If source statement format info provided, create fingerprint entry
- Enables Tier 1 column mapping for future imports

## Entity Relationships

```
User (1) ──────────── (*) ImportJob
                           │
                           ├── Creates (*) DescriptionCache entries
                           ├── Creates (*) VendorAlias entries
                           └── Creates (*) ExpenseEmbedding entries
```

## Database Configuration

### ImportJob Configuration (EF Core)

```csharp
public class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.ToTable("ImportJobs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceFileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.BlobUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.ErrorLog)
            .HasMaxLength(10000);

        builder.HasIndex(x => new { x.UserId, x.Status })
            .HasDatabaseName("IX_ImportJob_UserId_Status");

        builder.HasIndex(x => x.StartedAt)
            .HasDatabaseName("IX_ImportJob_StartedAt")
            .IsDescending();

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

## State Transitions

### ImportJob Status Flow

```
                    ┌─────────────┐
                    │   Pending   │
                    └──────┬──────┘
                           │ Job picked up
                           ▼
                    ┌─────────────┐
        ┌───────────│ Processing  │───────────┐
        │           └──────┬──────┘           │
        │ User cancels     │ Completes        │ Error
        ▼                  ▼                  ▼
┌─────────────┐     ┌─────────────┐    ┌─────────────┐
│  Cancelled  │     │  Completed  │    │   Failed    │
└─────────────┘     └─────────────┘    └─────────────┘
```

**Transition Rules**:
- `Pending → Processing`: Automatic when Hangfire job starts
- `Processing → Completed`: All records processed successfully
- `Processing → Failed`: Unrecoverable error (file not found, invalid format)
- `Processing → Cancelled`: User requests cancellation via API
- No transitions from terminal states (Completed, Failed, Cancelled)

## Validation Rules

### ImportJob Validation

| Rule | Implementation |
|------|----------------|
| SourceFileName must be valid Excel | Extension must be .xlsx, file must parse |
| ProcessedRecords ≤ TotalRecords | Enforced during progress updates |
| CompletedAt required for terminal states | Set when transitioning to Completed/Failed/Cancelled |
| ErrorLog is JSON array format | Validated on write, `[{"line": 5, "error": "..."}]` |

### Historical Data Record Validation

| Rule | Action if Invalid |
|------|-------------------|
| Missing required columns | Skip record, log error |
| Invalid date format | Skip record, log error |
| Invalid amount (non-numeric) | Skip record, log error |
| Description too short (<3 chars) | Skip record, log error |
| GL Code not in GLAccounts | Log warning, proceed (may be historical code) |

## Migration Script

```sql
-- Migration: Add ImportJobs table for cache warming
CREATE TABLE "ImportJobs" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "SourceFileName" varchar(255) NOT NULL,
    "BlobUrl" varchar(500) NOT NULL,
    "Status" integer NOT NULL DEFAULT 0,
    "TotalRecords" integer NOT NULL DEFAULT 0,
    "ProcessedRecords" integer NOT NULL DEFAULT 0,
    "CachedDescriptions" integer NOT NULL DEFAULT 0,
    "CreatedAliases" integer NOT NULL DEFAULT 0,
    "GeneratedEmbeddings" integer NOT NULL DEFAULT 0,
    "SkippedRecords" integer NOT NULL DEFAULT 0,
    "ErrorLog" varchar(10000),
    "StartedAt" timestamp NOT NULL DEFAULT NOW(),
    "CompletedAt" timestamp,
    "CreatedAt" timestamp NOT NULL DEFAULT NOW(),
    "UpdatedAt" timestamp,
    CONSTRAINT "PK_ImportJobs" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ImportJobs_Users" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_ImportJob_UserId_Status" ON "ImportJobs" ("UserId", "Status");
CREATE INDEX "IX_ImportJob_StartedAt" ON "ImportJobs" ("StartedAt" DESC);
```
