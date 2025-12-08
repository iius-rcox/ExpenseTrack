# Data Model: Statement Import & Fingerprinting

**Feature**: 004-statement-fingerprinting
**Date**: 2025-12-05

## Entity Diagram

```
┌─────────────────────┐       ┌─────────────────────┐
│       User          │       │  StatementImport    │
├─────────────────────┤       ├─────────────────────┤
│ Id (PK)             │◄──────│ UserId (FK)         │
│ EntraObjectId       │       │ Id (PK)             │
│ Email               │       │ FileName            │
│ DisplayName         │       │ TierUsed            │
│ CreatedAt           │       │ TransactionCount    │
└─────────────────────┘       │ SkippedCount        │
         │                    │ DuplicateCount      │
         │                    │ FingerprintId (FK)  │
         │                    │ CreatedAt           │
         │                    └─────────────────────┘
         │                              │
         ▼                              │
┌─────────────────────┐                 │
│ StatementFingerprint│                 │
├─────────────────────┤                 │
│ Id (PK)             │◄────────────────┘
│ UserId (FK, nullable)│  (null = system)
│ SourceName          │
│ HeaderHash          │
│ ColumnMapping (JSON)│
│ DateFormat          │
│ AmountSign          │
│ HitCount            │
│ LastUsedAt          │
│ CreatedAt           │
└─────────────────────┘

┌─────────────────────┐       ┌─────────────────────┐
│    Transaction      │       │      Receipt        │
├─────────────────────┤       ├─────────────────────┤
│ Id (PK)             │       │ Id (PK)             │
│ UserId (FK)         │       │ ...existing...      │
│ ImportId (FK)       │───────│                     │
│ TransactionDate     │       └─────────────────────┘
│ PostDate            │               ▲
│ Description         │               │ (future: Sprint 5)
│ Amount              │               │
│ OriginalDescription │       ┌───────┴───────┐
│ DuplicateHash       │       │MatchedReceiptId│
│ MatchedReceiptId(FK)│───────┘   (nullable)
│ CreatedAt           │
└─────────────────────┘
```

## New Entities

### Transaction

Represents a single imported credit card transaction.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, default gen_random_uuid() | Unique identifier |
| UserId | Guid | FK → Users.Id, NOT NULL | Owner of transaction |
| ImportId | Guid | FK → StatementImports.Id, NOT NULL | Source import batch |
| TransactionDate | DateOnly | NOT NULL | When transaction occurred |
| PostDate | DateOnly | nullable | When transaction posted |
| Description | string(500) | NOT NULL | Parsed/normalized description |
| OriginalDescription | string(500) | NOT NULL | Raw description from statement |
| Amount | decimal(18,2) | NOT NULL | Transaction amount (positive = expense) |
| DuplicateHash | string(64) | NOT NULL, indexed | SHA-256 of date+amount+description |
| MatchedReceiptId | Guid | FK → Receipts.Id, nullable | Linked receipt (Sprint 5) |
| CreatedAt | DateTime | NOT NULL, default NOW() | Import timestamp |

**Indexes**:
- `IX_Transaction_UserId` on (UserId)
- `IX_Transaction_ImportId` on (ImportId)
- `IX_Transaction_DuplicateHash` on (UserId, DuplicateHash) - for fast duplicate check
- `IX_Transaction_TransactionDate` on (UserId, TransactionDate) - for date range queries

**Validation Rules**:
- Amount must not be zero
- TransactionDate must be within last 2 years
- Description must not be empty after trimming

### StatementImport

Audit record for each statement import operation.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, default gen_random_uuid() | Unique identifier |
| UserId | Guid | FK → Users.Id, NOT NULL | User who performed import |
| FingerprintId | Guid | FK → StatementFingerprints.Id, nullable | Fingerprint used (null if AI inferred) |
| FileName | string(255) | NOT NULL | Original uploaded filename |
| FileSize | long | NOT NULL | File size in bytes |
| TierUsed | int | NOT NULL | 1 = fingerprint cache, 3 = AI inference |
| TransactionCount | int | NOT NULL | Successfully imported count |
| SkippedCount | int | NOT NULL | Rows skipped (missing fields) |
| DuplicateCount | int | NOT NULL | Duplicate rows not imported |
| CreatedAt | DateTime | NOT NULL, default NOW() | Import timestamp |

**Indexes**:
- `IX_StatementImport_UserId` on (UserId)
- `IX_StatementImport_CreatedAt` on (CreatedAt DESC) - for recent imports list

### StatementFingerprint (Extended)

Existing entity with new fields for usage tracking and system fingerprints.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK | *Existing* |
| UserId | Guid | FK → Users.Id, **NULLABLE** | null = system-wide fingerprint |
| SourceName | string(100) | NOT NULL | *Existing* |
| HeaderHash | string(64) | NOT NULL | *Existing* |
| ColumnMapping | string (JSON) | NOT NULL | *Existing* |
| DateFormat | string(50) | nullable | *Existing* |
| AmountSign | string(20) | NOT NULL, default 'negative_charges' | *Existing* |
| **HitCount** | int | NOT NULL, default 0 | **New**: Times used successfully |
| **LastUsedAt** | DateTime | nullable | **New**: Last successful use |
| CreatedAt | DateTime | NOT NULL | *Existing* |

**Index Changes**:
- Modify unique constraint: `UQ_Fingerprint_UserHash` on (UserId, HeaderHash) with NULLS NOT DISTINCT

**Migration Notes**:
- Add HitCount column with default 0
- Add LastUsedAt column as nullable
- Modify UserId FK to allow NULL
- Update unique constraint for nullable UserId handling

## Column Mapping JSON Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "additionalProperties": {
    "type": "string",
    "enum": ["date", "post_date", "description", "amount", "category", "memo", "reference", "ignore"]
  },
  "required": [],
  "examples": [
    {
      "Transaction Date": "date",
      "Post Date": "post_date",
      "Description": "description",
      "Amount": "amount",
      "Category": "ignore",
      "Memo": "memo"
    }
  ]
}
```

## State Transitions

### Transaction Lifecycle

```
[Created] ──► [Unmatched] ──► [Matched]
                  │               │
                  │               ▼
                  │         [Reported]
                  │               │
                  ▼               ▼
              [Deleted]      [Submitted]
```

States are implicit based on:
- **Unmatched**: MatchedReceiptId IS NULL
- **Matched**: MatchedReceiptId IS NOT NULL (Sprint 5)
- **Reported**: Transaction appears in ExpenseReport (Sprint 8)
- **Submitted**: ExpenseReport submitted to AP (Sprint 8)
- **Deleted**: Soft delete via DeletedAt timestamp (future)

### StatementImport Lifecycle

```
[Pending] ──► [Processing] ──► [Completed]
                   │
                   ▼
              [Failed]
```

States tracked via combination of fields:
- **Pending**: Not used (imports are synchronous)
- **Processing**: During API call (not persisted)
- **Completed**: TransactionCount > 0 or SkippedCount > 0
- **Failed**: Error thrown (not recorded in DB, logged only)

## Seed Data

### System Fingerprints

```sql
-- Chase Business Card (system fingerprint)
INSERT INTO statement_fingerprints (
    id, user_id, source_name, header_hash, column_mapping, date_format, amount_sign, hit_count, created_at
) VALUES (
    'a0000000-0000-0000-0000-000000000001',
    NULL,  -- System fingerprint
    'Chase Business Card',
    -- Hash computed from: "amount,category,description,memo,post date,transaction date,type"
    'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855',
    '{"Transaction Date":"date","Post Date":"post_date","Description":"description","Category":"ignore","Type":"ignore","Amount":"amount","Memo":"memo"}',
    'MM/dd/yyyy',
    'negative_charges',
    0,
    NOW()
);

-- American Express Business (system fingerprint)
INSERT INTO statement_fingerprints (
    id, user_id, source_name, header_hash, column_mapping, date_format, amount_sign, hit_count, created_at
) VALUES (
    'a0000000-0000-0000-0000-000000000002',
    NULL,  -- System fingerprint
    'American Express Business',
    -- Hash computed from: "amount,date,description"
    'a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd',
    '{"Date":"date","Description":"description","Amount":"amount"}',
    'MM/dd/yyyy',
    'positive_charges',
    0,
    NOW()
);
```

## EF Core Configuration

### TransactionConfiguration.cs

```csharp
public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(t => t.Description).HasMaxLength(500).IsRequired();
        builder.Property(t => t.OriginalDescription).HasMaxLength(500).IsRequired();
        builder.Property(t => t.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.DuplicateHash).HasMaxLength(64).IsRequired();

        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Import)
            .WithMany(i => i.Transactions)
            .HasForeignKey(t => t.ImportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.MatchedReceipt)
            .WithMany()
            .HasForeignKey(t => t.MatchedReceiptId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => t.ImportId);
        builder.HasIndex(t => new { t.UserId, t.DuplicateHash });
        builder.HasIndex(t => new { t.UserId, t.TransactionDate });
    }
}
```

### StatementImportConfiguration.cs

```csharp
public class StatementImportConfiguration : IEntityTypeConfiguration<StatementImport>
{
    public void Configure(EntityTypeBuilder<StatementImport> builder)
    {
        builder.ToTable("statement_imports");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(i => i.FileName).HasMaxLength(255).IsRequired();
        builder.Property(i => i.TierUsed).IsRequired();

        builder.HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Fingerprint)
            .WithMany()
            .HasForeignKey(i => i.FingerprintId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(i => i.UserId);
        builder.HasIndex(i => i.CreatedAt).IsDescending();
    }
}
```
