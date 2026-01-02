# Data Model: Missing API Endpoints

**Feature**: 021-missing-api-endpoints
**Date**: 2026-01-01

## Entity Changes

### 1. ReportStatus Enum (Modified)

**File**: `backend/src/ExpenseFlow.Shared/Enums/ReportStatus.cs`

```csharp
public enum ReportStatus : short
{
    /// <summary>Draft - being edited by user</summary>
    Draft = 0,

    /// <summary>Generated - finalized and locked for editing</summary>
    Generated = 1,

    /// <summary>Submitted - marked as complete for audit trail</summary>
    Submitted = 2

    // Future states:
    // Approved = 3,
    // Exported = 4
}
```

**State Transitions**:
```
Draft → Generated → Submitted
  ↑         ↓
  └─────────┘ (cannot go back once Generated)
```

**Validation Rules**:
- `Draft → Generated`: Report must have ≥1 line, each line must have category + amount > $0 + receipt
- `Generated → Submitted`: No additional validation (tracking only)

### 2. ExpenseReport Entity (Modified)

**File**: `backend/src/ExpenseFlow.Core/Entities/ExpenseReport.cs`

**New Fields**:
| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `GeneratedAt` | `DateTimeOffset?` | Yes | Timestamp when report was finalized |
| `SubmittedAt` | `DateTimeOffset?` | Yes | Timestamp when report was submitted |

```csharp
/// <summary>Timestamp when report was finalized (status changed to Generated)</summary>
public DateTimeOffset? GeneratedAt { get; set; }

/// <summary>Timestamp when report was submitted (status changed to Submitted)</summary>
public DateTimeOffset? SubmittedAt { get; set; }
```

**Existing Fields Used**:
- `Status` (ReportStatus) - updated to new enum values
- `RowVersion` (uint) - used for optimistic concurrency on generate

### 3. EF Core Configuration Update

**File**: `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/ExpenseReportConfiguration.cs`

Add to existing configuration:
```csharp
builder.Property(e => e.GeneratedAt)
    .HasColumnType("timestamptz");

builder.Property(e => e.SubmittedAt)
    .HasColumnType("timestamptz");
```

## New DTOs

### 1. AnalyticsExportRequestDto

**File**: `backend/src/ExpenseFlow.Shared/DTOs/AnalyticsExportRequestDto.cs`

```csharp
/// <summary>
/// Request parameters for analytics data export.
/// </summary>
public class AnalyticsExportRequestDto
{
    /// <summary>Start date for export range (ISO format YYYY-MM-DD)</summary>
    [Required]
    public string StartDate { get; set; } = string.Empty;

    /// <summary>End date for export range (ISO format YYYY-MM-DD)</summary>
    [Required]
    public string EndDate { get; set; } = string.Empty;

    /// <summary>Export format: "csv" or "xlsx"</summary>
    [Required]
    [RegularExpression("^(csv|xlsx)$", ErrorMessage = "Format must be 'csv' or 'xlsx'")]
    public string Format { get; set; } = "csv";

    /// <summary>
    /// Sections to include (comma-separated): trends, categories, vendors, transactions.
    /// If empty, defaults to all aggregated summaries (trends, categories, vendors).
    /// </summary>
    public string? Sections { get; set; }
}
```

### 2. ReportValidationResultDto

**File**: `backend/src/ExpenseFlow.Shared/DTOs/ReportValidationResultDto.cs`

```csharp
/// <summary>
/// Result of report validation before generation.
/// </summary>
public class ReportValidationResultDto
{
    /// <summary>Whether the report passed all validation rules</summary>
    public bool IsValid => !Errors.Any();

    /// <summary>Validation errors that must be fixed before generating</summary>
    public List<ValidationErrorDto> Errors { get; set; } = new();

    /// <summary>Non-blocking warnings</summary>
    public List<ValidationWarningDto> Warnings { get; set; } = new();
}

/// <summary>
/// A validation error on a specific line or report-level.
/// </summary>
public class ValidationErrorDto
{
    /// <summary>Line ID if error is on a specific line, null for report-level errors</summary>
    public Guid? LineId { get; set; }

    /// <summary>Field name that has the error</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>Error message</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Error code for programmatic handling</summary>
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// A non-blocking warning.
/// </summary>
public class ValidationWarningDto
{
    public Guid? LineId { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
```

### 3. GenerateReportResponseDto

**File**: `backend/src/ExpenseFlow.Shared/DTOs/GenerateReportResponseDto.cs`

```csharp
/// <summary>
/// Response after successfully generating (finalizing) a report.
/// </summary>
public class GenerateReportResponseDto
{
    /// <summary>Report ID</summary>
    public Guid ReportId { get; set; }

    /// <summary>New report status (Generated)</summary>
    public ReportStatus Status { get; set; }

    /// <summary>Timestamp when the report was finalized</summary>
    public DateTimeOffset GeneratedAt { get; set; }

    /// <summary>Number of expense lines in the report</summary>
    public int LineCount { get; set; }

    /// <summary>Total amount of all expense lines</summary>
    public decimal TotalAmount { get; set; }
}
```

### 4. SubmitReportResponseDto

**File**: `backend/src/ExpenseFlow.Shared/DTOs/SubmitReportResponseDto.cs`

```csharp
/// <summary>
/// Response after successfully submitting a report.
/// </summary>
public class SubmitReportResponseDto
{
    /// <summary>Report ID</summary>
    public Guid ReportId { get; set; }

    /// <summary>New report status (Submitted)</summary>
    public ReportStatus Status { get; set; }

    /// <summary>Timestamp when the report was submitted</summary>
    public DateTimeOffset SubmittedAt { get; set; }
}
```

## Database Migration

**Migration Name**: `AddReportStatusTimestamps`

```sql
-- Add new columns to ExpenseReports table
ALTER TABLE "ExpenseReports"
ADD COLUMN "GeneratedAt" timestamptz NULL,
ADD COLUMN "SubmittedAt" timestamptz NULL;

-- No data migration needed: all existing reports are Draft (no timestamps)
```

## Validation Rules Summary

| Rule | Error Code | Field | Message |
|------|------------|-------|---------|
| Report has no lines | `REPORT_EMPTY` | (report) | Report must have at least one expense line |
| Line missing category | `LINE_NO_CATEGORY` | CategoryId | Expense line must have a category assigned |
| Line amount ≤ 0 | `LINE_INVALID_AMOUNT` | Amount | Expense line amount must be greater than zero |
| Line missing receipt | `LINE_NO_RECEIPT` | ReceiptId | Expense line must have an attached receipt |
| Report not in Draft | `REPORT_NOT_DRAFT` | Status | Report must be in Draft status to generate |
| Report not Generated | `REPORT_NOT_GENERATED` | Status | Report must be Generated before submitting |
| Report already Generated | `REPORT_ALREADY_GENERATED` | Status | Report has already been finalized |
| Report already Submitted | `REPORT_ALREADY_SUBMITTED` | Status | Report has already been submitted |

## Relationships Diagram

```
┌─────────────────────┐
│   ExpenseReport     │
├─────────────────────┤
│ Id: Guid (PK)       │
│ UserId: Guid (FK)   │
│ Period: string      │
│ Status: short       │◄──── ReportStatus enum
│ TotalAmount: decimal│
│ LineCount: int      │
│ GeneratedAt: datetime? ◄── NEW
│ SubmittedAt: datetime? ◄── NEW
│ RowVersion: uint    │
└─────────┬───────────┘
          │ 1:N
          ▼
┌─────────────────────┐
│    ExpenseLine      │
├─────────────────────┤
│ Id: Guid (PK)       │
│ ReportId: Guid (FK) │
│ CategoryId: Guid?   │◄── Validated: NOT NULL for generate
│ Amount: decimal     │◄── Validated: > 0 for generate
│ ReceiptId: Guid?    │◄── Validated: NOT NULL for generate
└─────────────────────┘
```
