# Data Model: Extraction Editor with Model Training

**Feature**: 024-extraction-editor-training
**Date**: 2026-01-03

## Entity Relationship Diagram

```
┌─────────────────┐       ┌──────────────────────┐
│      User       │       │       Receipt        │
├─────────────────┤       ├──────────────────────┤
│ Id (PK)         │───┐   │ Id (PK)              │
│ Email           │   │   │ UserId (FK)          │──┐
│ Name            │   │   │ VendorExtracted      │  │
│ ...             │   │   │ DateExtracted        │  │
└─────────────────┘   │   │ AmountExtracted      │  │
                      │   │ TaxExtracted         │  │
                      │   │ Currency             │  │
                      │   │ LineItems (JSONB)    │  │
                      │   │ ConfidenceScores     │  │
                      │   │ RowVersion (xmin)    │  │ ← NEW: Concurrency token
                      │   │ ...                  │  │
                      │   └──────────────────────┘  │
                      │              │              │
                      │              │ 1:N          │
                      │              ▼              │
                      │   ┌──────────────────────┐  │
                      │   │ ExtractionCorrection │  │ ← NEW ENTITY
                      │   ├──────────────────────┤  │
                      └──▶│ Id (PK)              │  │
                          │ ReceiptId (FK)       │◀─┘
                          │ UserId (FK)          │◀────────┐
                          │ FieldName            │         │
                          │ OriginalValue        │         │
                          │ CorrectedValue       │         │
                          │ CreatedAt            │         │
                          └──────────────────────┘         │
                                                           │
                                    User ──────────────────┘
```

## New Entity: ExtractionCorrection

### Purpose

Records user corrections to AI-extracted receipt fields for model training feedback. Each record captures what the AI extracted vs. what the user corrected, enabling future model improvement cycles.

### Schema

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| `id` | `uuid` | No | Primary key (BaseEntity) |
| `receipt_id` | `uuid` | No | FK to receipts.id |
| `user_id` | `uuid` | No | FK to users.id (who made the correction) |
| `field_name` | `varchar(50)` | No | Field that was corrected (e.g., "vendor", "amount") |
| `original_value` | `text` | Yes | JSON-serialized original AI extraction |
| `corrected_value` | `text` | Yes | JSON-serialized user correction |
| `created_at` | `timestamptz` | No | When correction was submitted |

### Indexes

| Name | Columns | Type | Purpose |
|------|---------|------|---------|
| `ix_extraction_corrections_receipt_id` | `receipt_id` | B-tree | Query corrections by receipt |
| `ix_extraction_corrections_user_id` | `user_id` | B-tree | Query corrections by user |
| `ix_extraction_corrections_created_at` | `created_at DESC` | B-tree | Order by most recent |
| `ix_extraction_corrections_field_name` | `field_name` | B-tree | Filter by field type |

### Constraints

| Constraint | Type | Definition |
|------------|------|------------|
| `pk_extraction_corrections` | Primary Key | `id` |
| `fk_extraction_corrections_receipt` | Foreign Key | `receipt_id` → `receipts(id)` ON DELETE CASCADE |
| `fk_extraction_corrections_user` | Foreign Key | `user_id` → `users(id)` ON DELETE CASCADE |
| `ck_extraction_corrections_field_name` | Check | `field_name IN ('vendor', 'amount', 'date', 'tax', 'currency', 'line_item')` |

### Entity Class

```csharp
namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Records a user correction to an AI-extracted receipt field.
/// Used as training feedback for model improvement.
/// Retained indefinitely as permanent training corpus.
/// </summary>
public class ExtractionCorrection : BaseEntity
{
    /// <summary>FK to Receipt being corrected</summary>
    public Guid ReceiptId { get; set; }

    /// <summary>FK to User who made the correction</summary>
    public Guid UserId { get; set; }

    /// <summary>Name of the field that was corrected</summary>
    /// <example>vendor, amount, date, tax, currency, line_item</example>
    public string FieldName { get; set; } = null!;

    /// <summary>Original AI-extracted value (JSON-serialized)</summary>
    public string? OriginalValue { get; set; }

    /// <summary>User-corrected value (JSON-serialized)</summary>
    public string? CorrectedValue { get; set; }

    /// <summary>When the correction was submitted</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Receipt Receipt { get; set; } = null!;
    public User User { get; set; } = null!;
}
```

### EF Core Configuration

```csharp
namespace ExpenseFlow.Infrastructure.Data.Configurations;

public class ExtractionCorrectionConfiguration : IEntityTypeConfiguration<ExtractionCorrection>
{
    public void Configure(EntityTypeBuilder<ExtractionCorrection> builder)
    {
        builder.ToTable("extraction_corrections");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FieldName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.OriginalValue)
            .HasColumnType("text");

        builder.Property(e => e.CorrectedValue)
            .HasColumnType("text");

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(e => e.ReceiptId)
            .HasDatabaseName("ix_extraction_corrections_receipt_id");

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("ix_extraction_corrections_user_id");

        builder.HasIndex(e => e.CreatedAt)
            .IsDescending()
            .HasDatabaseName("ix_extraction_corrections_created_at");

        builder.HasIndex(e => e.FieldName)
            .HasDatabaseName("ix_extraction_corrections_field_name");

        // Relationships
        builder.HasOne(e => e.Receipt)
            .WithMany()
            .HasForeignKey(e => e.ReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

## Modified Entity: Receipt

### Changes

Add concurrency token for optimistic locking (FR-012):

```csharp
// Add to Receipt.cs
/// <summary>Concurrency token for optimistic locking</summary>
[Timestamp]
public uint RowVersion { get; set; }
```

### Configuration Update

```csharp
// Add to ReceiptConfiguration.cs
builder.Property(e => e.RowVersion)
    .HasColumnName("xmin")
    .HasColumnType("xid")
    .ValueGeneratedOnAddOrUpdate()
    .IsConcurrencyToken();
```

---

## Field Name Enumeration

Valid values for `ExtractionCorrection.FieldName`:

| Value | Description | Value Type |
|-------|-------------|------------|
| `vendor` | Merchant/vendor name | `string` |
| `amount` | Total amount | `decimal` |
| `date` | Transaction date | `DateOnly` |
| `tax` | Tax amount | `decimal` |
| `currency` | Currency code | `string` |
| `line_item` | Line item correction | `LineItemDto` (JSON) |

For `line_item` corrections, the value format includes the line item index:
```json
{
  "index": 0,
  "field": "description",
  "value": "Office Supplies"
}
```

---

## Migration SQL

```sql
-- Migration: 20260104000000_AddExtractionCorrections

CREATE TABLE extraction_corrections (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    receipt_id uuid NOT NULL,
    user_id uuid NOT NULL,
    field_name varchar(50) NOT NULL,
    original_value text,
    corrected_value text,
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT pk_extraction_corrections PRIMARY KEY (id),
    CONSTRAINT fk_extraction_corrections_receipt
        FOREIGN KEY (receipt_id) REFERENCES receipts(id) ON DELETE CASCADE,
    CONSTRAINT fk_extraction_corrections_user
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT ck_extraction_corrections_field_name
        CHECK (field_name IN ('vendor', 'amount', 'date', 'tax', 'currency', 'line_item'))
);

CREATE INDEX ix_extraction_corrections_receipt_id ON extraction_corrections(receipt_id);
CREATE INDEX ix_extraction_corrections_user_id ON extraction_corrections(user_id);
CREATE INDEX ix_extraction_corrections_created_at ON extraction_corrections(created_at DESC);
CREATE INDEX ix_extraction_corrections_field_name ON extraction_corrections(field_name);

-- Add comment for documentation
COMMENT ON TABLE extraction_corrections IS 'Training feedback: user corrections to AI-extracted receipt fields. Retained indefinitely.';
```

---

## Data Flow

### Correction Submission

```
User edits field → Frontend batches edits → PUT /api/receipts/{id}
                                                    │
                                                    ▼
                                        ┌─────────────────────┐
                                        │ ReceiptService      │
                                        │ .UpdateReceiptAsync │
                                        └─────────────────────┘
                                                    │
                                    ┌───────────────┴───────────────┐
                                    ▼                               ▼
                          Update Receipt fields        Create ExtractionCorrection
                          (vendor, amount, etc.)       records for each correction
                                    │                               │
                                    └───────────────┬───────────────┘
                                                    ▼
                                           SaveChangesAsync()
                                           (single transaction)
```

### Feedback History Query

```
Admin requests history → GET /api/extraction-corrections?page=1&fieldName=vendor
                                         │
                                         ▼
                              ┌────────────────────────┐
                              │ ExtractionCorrection   │
                              │ Service.GetCorrections │
                              └────────────────────────┘
                                         │
                                         ▼
                              Query with filters:
                              - fieldName (optional)
                              - dateRange (optional)
                              - userId (optional)
                              - receiptId (optional)
                                         │
                                         ▼
                              Return paginated results
```

---

## Retention Policy

Per clarification: **Training feedback is retained indefinitely** as a permanent training corpus.

- No automatic purging
- Cascade delete when receipt is deleted (FK constraint)
- Cascade delete when user is deleted (FK constraint)

Future consideration: If storage becomes a concern, implement archival to cold storage rather than deletion.
