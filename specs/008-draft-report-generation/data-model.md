# Data Model: Draft Report Generation

**Feature**: 008-draft-report-generation
**Date**: 2025-12-16

## Entity Overview

```
┌─────────────────┐      ┌─────────────────┐
│  ExpenseReport  │──────│   ExpenseLine   │
│                 │ 1  * │                 │
└─────────────────┘      └─────────────────┘
        │                        │
        │                        ├──────────────────────┐
        ▼                        ▼                      ▼
┌─────────────────┐      ┌─────────────────┐    ┌─────────────────┐
│      User       │      │     Receipt     │    │   Transaction   │
│   (existing)    │      │   (existing)    │    │   (existing)    │
└─────────────────┘      └─────────────────┘    └─────────────────┘
```

---

## New Entities

### ExpenseReport

Represents a draft expense report for a specific period.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, default gen_random_uuid() | Unique identifier |
| UserId | Guid | FK → Users, NOT NULL | Owner of the report |
| Period | string(7) | NOT NULL, format "YYYY-MM" | Reporting month (e.g., "2025-01") |
| Status | ReportStatus | NOT NULL, default Draft | Draft only in this sprint |
| TotalAmount | decimal(18,2) | NOT NULL | Sum of all expense line amounts |
| LineCount | int | NOT NULL | Number of expense lines |
| MissingReceiptCount | int | NOT NULL | Lines without receipts |
| Tier1HitCount | int | NOT NULL | Suggestions from cache |
| Tier2HitCount | int | NOT NULL | Suggestions from embeddings |
| Tier3HitCount | int | NOT NULL | Suggestions from AI |
| IsDeleted | bool | NOT NULL, default false | Soft delete flag |
| CreatedAt | DateTime | NOT NULL, default UTC now | Creation timestamp |
| UpdatedAt | DateTime | NULL | Last modification timestamp |
| RowVersion | uint | xmin | Optimistic concurrency (PostgreSQL) |

**Indexes**:
- `IX_ExpenseReport_UserId_Period` (UserId, Period) - UNIQUE WHERE NOT IsDeleted
- `IX_ExpenseReport_UserId_CreatedAt` (UserId, CreatedAt DESC) - for listing

**Constraints**:
- Period format validated: regex `^\d{4}-(0[1-9]|1[0-2])$`
- One active draft per user per period (unique index with IsDeleted filter)

---

### ExpenseLine

Individual expense item within a report.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, default gen_random_uuid() | Unique identifier |
| ReportId | Guid | FK → ExpenseReports, NOT NULL | Parent report |
| ReceiptId | Guid? | FK → Receipts, NULL | Linked receipt (if matched) |
| TransactionId | Guid? | FK → Transactions, NULL | Linked transaction |
| LineOrder | int | NOT NULL | Display order in report |
| ExpenseDate | DateOnly | NOT NULL | Date of expense |
| Amount | decimal(18,2) | NOT NULL | Expense amount |
| OriginalDescription | string(500) | NOT NULL | Raw bank description |
| NormalizedDescription | string(500) | NOT NULL | Human-readable description |
| VendorName | string(255) | NULL | Extracted/matched vendor |
| GLCode | string(10) | NULL | Selected GL account code |
| GLCodeSuggested | string(10) | NULL | System-suggested GL code |
| GLCodeTier | int? | 1, 2, or 3 | Tier that provided GL suggestion |
| GLCodeSource | string(50) | NULL | "VendorAlias", "EmbeddingSimilarity", "AIInference" |
| DepartmentCode | string(20) | NULL | Selected department code |
| DepartmentSuggested | string(20) | NULL | System-suggested department |
| DepartmentTier | int? | 1, 2, or 3 | Tier that provided dept suggestion |
| DepartmentSource | string(50) | NULL | Source description |
| HasReceipt | bool | NOT NULL | True if receipt linked |
| MissingReceiptJustification | int? | enum value | Justification if no receipt |
| JustificationNote | string(500) | NULL | Custom note for "Other" justification |
| IsUserEdited | bool | NOT NULL, default false | True if user modified categorization |
| CreatedAt | DateTime | NOT NULL, default UTC now | Creation timestamp |
| UpdatedAt | DateTime | NULL | Last modification timestamp |

**Indexes**:
- `IX_ExpenseLine_ReportId_LineOrder` (ReportId, LineOrder)
- `IX_ExpenseLine_TransactionId` (TransactionId) - for duplicate prevention

**Constraints**:
- At least one of ReceiptId or TransactionId must be non-null
- If HasReceipt = false, MissingReceiptJustification required before export (enforced in application)
- LineOrder must be sequential starting from 1

---

## New Enums

### ReportStatus (ExpenseFlow.Shared.Enums)

```csharp
public enum ReportStatus : short
{
    /// <summary>Draft - being edited by user</summary>
    Draft = 0

    // Future states (Sprint 9+):
    // Submitted = 1,
    // Approved = 2,
    // Exported = 3
}
```

### MissingReceiptJustification (ExpenseFlow.Shared.Enums)

```csharp
public enum MissingReceiptJustification : short
{
    /// <summary>Not specified yet</summary>
    None = 0,

    /// <summary>Vendor did not provide a receipt</summary>
    NotProvided = 1,

    /// <summary>Receipt was lost</summary>
    Lost = 2,

    /// <summary>Digital subscription with no physical/email receipt</summary>
    DigitalSubscription = 3,

    /// <summary>Amount under company threshold requiring receipt</summary>
    UnderThreshold = 4,

    /// <summary>Other reason - see JustificationNote</summary>
    Other = 5
}
```

---

## Existing Entity Impacts

### No schema changes required

The following entities are referenced but not modified:
- **User**: ExpenseReport.UserId foreign key
- **Receipt**: ExpenseLine.ReceiptId foreign key
- **Transaction**: ExpenseLine.TransactionId foreign key
- **ReceiptTransactionMatch**: Queried during report generation (no changes)

---

## Entity Framework Configurations

### ExpenseReportConfiguration

```csharp
public class ExpenseReportConfiguration : IEntityTypeConfiguration<ExpenseReport>
{
    public void Configure(EntityTypeBuilder<ExpenseReport> builder)
    {
        builder.ToTable("expense_reports");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.Period)
            .HasMaxLength(7)
            .IsRequired();

        builder.Property(e => e.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(e => e.Status)
            .HasConversion<short>();

        builder.Property(e => e.RowVersion)
            .IsRowVersion();

        builder.HasIndex(e => new { e.UserId, e.Period })
            .IsUnique()
            .HasFilter("NOT is_deleted");

        builder.HasIndex(e => new { e.UserId, e.CreatedAt })
            .IsDescending(false, true);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### ExpenseLineConfiguration

```csharp
public class ExpenseLineConfiguration : IEntityTypeConfiguration<ExpenseLine>
{
    public void Configure(EntityTypeBuilder<ExpenseLine> builder)
    {
        builder.ToTable("expense_lines");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(e => e.OriginalDescription)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.NormalizedDescription)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.VendorName)
            .HasMaxLength(255);

        builder.Property(e => e.GLCode)
            .HasMaxLength(10);

        builder.Property(e => e.GLCodeSource)
            .HasMaxLength(50);

        builder.Property(e => e.DepartmentCode)
            .HasMaxLength(20);

        builder.Property(e => e.DepartmentSource)
            .HasMaxLength(50);

        builder.Property(e => e.JustificationNote)
            .HasMaxLength(500);

        builder.Property(e => e.Amount)
            .HasPrecision(18, 2);

        builder.Property(e => e.MissingReceiptJustification)
            .HasConversion<short>();

        builder.HasIndex(e => new { e.ReportId, e.LineOrder });
        builder.HasIndex(e => e.TransactionId);

        builder.HasOne(e => e.Report)
            .WithMany(r => r.Lines)
            .HasForeignKey(e => e.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Receipt)
            .WithMany()
            .HasForeignKey(e => e.ReceiptId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Transaction)
            .WithMany()
            .HasForeignKey(e => e.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
```

---

## Migration Summary

**Migration Name**: `AddExpenseReports`

Creates:
1. `expense_reports` table with indexes
2. `expense_lines` table with indexes and foreign keys
3. Check constraint on Period format (optional - can be application-level)

**Estimated Data Volume**:
- 10-20 users × 12 months × 1 draft = ~240 reports/year
- 50 lines/report average = ~12,000 lines/year
- Minimal storage impact
