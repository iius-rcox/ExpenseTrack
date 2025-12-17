# Data Model: Output Generation & Analytics

**Feature**: 009-output-analytics
**Date**: 2025-12-16

## Entity Overview

```
┌─────────────────────┐     ┌─────────────────────┐
│   ExpenseReport     │────►│     ExpenseLine     │
│   (from Sprint 8)   │ 1 * │   (from Sprint 8)   │
└─────────────────────┘     └─────────────────────┘
         │                           │
         │ export to                 │ references
         ▼                           ▼
┌─────────────────────┐     ┌─────────────────────┐
│  ExcelExport (DTO)  │     │   Receipt (existing)│
│  (not persisted)    │     └─────────────────────┘
└─────────────────────┘
         │
         ▼
┌─────────────────────┐
│  ReceiptPdf (DTO)   │
│  (not persisted)    │
└─────────────────────┘

┌─────────────────────┐     ┌─────────────────────┐
│   TierUsageLog      │     │ MonthlyComparison   │
│   (from Sprint 6)   │     │      (DTO)          │
└─────────────────────┘     └─────────────────────┘
```

## No New Database Entities

This feature operates on existing entities from Sprint 6-8:
- **ExpenseReport**: Report metadata and summary statistics
- **ExpenseLine**: Individual expense items with receipt linkage
- **Receipt**: Stored receipt files with blob URLs
- **TierUsageLog**: Tier usage metrics for cache statistics

All outputs (Excel, PDF, analytics) are generated on-demand from existing data.

---

## Existing Entities Used

### ExpenseReport (from Sprint 8)

| Field | Type | Usage in Sprint 9 |
|-------|------|-------------------|
| Id | Guid | Report identification for exports |
| UserId | Guid | Access control, filtering |
| Period | string(7) | MoM comparison period identification |
| TotalAmount | decimal | Summary display |
| LineCount | int | Summary display |
| MissingReceiptCount | int | Placeholder generation trigger |
| Tier1HitCount | int | Cache statistics |
| Tier2HitCount | int | Cache statistics |
| Tier3HitCount | int | Cache statistics |

### ExpenseLine (from Sprint 8)

| Field | Type | Usage in Sprint 9 |
|-------|------|-------------------|
| Id | Guid | Line identification |
| LineOrder | int | PDF receipt ordering, Excel row order |
| ExpenseDate | DateOnly | Excel export column A |
| Amount | decimal | Excel export column F |
| NormalizedDescription | string | Excel export column D |
| GLCode | string | Excel export column B |
| DepartmentCode | string | Excel export column C |
| HasReceipt | bool | Placeholder generation decision |
| ReceiptId | Guid? | Receipt file lookup for PDF |
| MissingReceiptJustification | enum | Placeholder content |
| JustificationNote | string? | Placeholder content (for "Other") |
| VendorName | string? | MoM vendor comparison |

### Receipt (from Sprint 3)

| Field | Type | Usage in Sprint 9 |
|-------|------|-------------------|
| Id | Guid | Receipt identification |
| BlobUrl | string | PDF consolidation source |
| FileName | string | File type detection |
| FileType | string | PDF vs image handling |

### TierUsageLog (from Sprint 6)

| Field | Type | Usage in Sprint 9 |
|-------|------|-------------------|
| UserId | Guid | User-specific statistics |
| OperationType | string | Filter by operation type |
| TierUsed | int | Aggregation by tier |
| ResponseTimeMs | int | Performance metrics |
| CreatedAt | timestamp | Date range filtering |

---

## DTOs (Data Transfer Objects)

### ExcelExportDto

Represents the Excel file download response.

| Field | Type | Description |
|-------|------|-------------|
| FileName | string | "{Period}-expense-report.xlsx" |
| ContentType | string | "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" |
| FileContents | byte[] | Serialized Excel file |

### ReceiptPdfDto

Represents the consolidated receipt PDF download response.

| Field | Type | Description |
|-------|------|-------------|
| FileName | string | "{Period}-receipts.pdf" |
| ContentType | string | "application/pdf" |
| FileContents | byte[] | Serialized PDF file |
| PageCount | int | Total pages including placeholders |
| PlaceholderCount | int | Number of missing receipt placeholders |

### MissingReceiptPlaceholderDto

Data for generating a placeholder page.

| Field | Type | Description |
|-------|------|-------------|
| ExpenseDate | DateOnly | Expense date |
| VendorName | string | Vendor name (or "Unknown") |
| Amount | decimal | Expense amount |
| Description | string | Normalized description |
| Justification | string | Human-readable justification |
| JustificationNote | string? | Custom note if "Other" |
| EmployeeName | string | User display name |
| ReportId | string | Report identifier |

### MonthlyComparisonDto

Represents MoM comparison results.

| Field | Type | Description |
|-------|------|-------------|
| CurrentPeriod | string | "YYYY-MM" |
| PreviousPeriod | string | "YYYY-MM" |
| Summary | ComparisonSummaryDto | Totals and change metrics |
| NewVendors | List<VendorAmountDto> | Vendors new in current period |
| MissingRecurring | List<VendorAmountDto> | Expected vendors missing |
| SignificantChanges | List<VendorChangeDto> | Vendors with >50% change |

### ComparisonSummaryDto

| Field | Type | Description |
|-------|------|-------------|
| CurrentTotal | decimal | Total spending current period |
| PreviousTotal | decimal | Total spending previous period |
| Change | decimal | Absolute change |
| ChangePercent | decimal | Percentage change |

### VendorAmountDto

| Field | Type | Description |
|-------|------|-------------|
| VendorName | string | Vendor name |
| Amount | decimal | Spending amount |

### VendorChangeDto

| Field | Type | Description |
|-------|------|-------------|
| VendorName | string | Vendor name |
| CurrentAmount | decimal | Current period amount |
| PreviousAmount | decimal | Previous period amount |
| Change | decimal | Absolute change |
| ChangePercent | decimal | Percentage change |

### CacheStatisticsDto

Represents cache tier usage statistics.

| Field | Type | Description |
|-------|------|-------------|
| Period | string | Statistics period (e.g., "2025-01" or "last30days") |
| Tier1Hits | int | Count of Tier 1 (cache) hits |
| Tier2Hits | int | Count of Tier 2 (embedding) hits |
| Tier3Hits | int | Count of Tier 3 (AI) hits |
| TotalOperations | int | Total categorization operations |
| Tier1HitRate | decimal | Tier 1 percentage (target: 50%+) |
| Tier2HitRate | decimal | Tier 2 percentage |
| Tier3HitRate | decimal | Tier 3 percentage |
| EstimatedMonthlyCost | decimal | Projected AI cost based on usage |
| AvgResponseTimeMs | int | Average response time across all tiers |
| BelowTarget | bool | True if Tier1HitRate < 50% |

### CacheStatsByOperationDto

| Field | Type | Description |
|-------|------|-------------|
| OperationType | string | 'normalization', 'gl_suggestion', 'dept_suggestion' |
| Tier1Hits | int | Tier 1 count for this operation |
| Tier2Hits | int | Tier 2 count |
| Tier3Hits | int | Tier 3 count |
| Tier1HitRate | decimal | Percentage |

---

## Query Patterns

### MoM Comparison Query

```sql
WITH current_vendors AS (
    SELECT
        el.vendor_name,
        SUM(el.amount) as total_amount
    FROM expense_lines el
    INNER JOIN expense_reports er ON el.report_id = er.id
    WHERE er.user_id = @userId
      AND er.period = @currentPeriod
      AND er.is_deleted = false
    GROUP BY el.vendor_name
),
previous_vendors AS (
    SELECT
        el.vendor_name,
        SUM(el.amount) as total_amount
    FROM expense_lines el
    INNER JOIN expense_reports er ON el.report_id = er.id
    WHERE er.user_id = @userId
      AND er.period = @previousPeriod
      AND er.is_deleted = false
    GROUP BY el.vendor_name
),
recurring_vendors AS (
    -- Vendors appearing in 2+ consecutive months before previous
    SELECT DISTINCT el.vendor_name
    FROM expense_lines el
    INNER JOIN expense_reports er ON el.report_id = er.id
    WHERE er.user_id = @userId
      AND er.period < @previousPeriod
      AND er.is_deleted = false
    GROUP BY el.vendor_name
    HAVING COUNT(DISTINCT er.period) >= 2
)
SELECT
    COALESCE(c.vendor_name, p.vendor_name) as vendor_name,
    c.total_amount as current_amount,
    p.total_amount as previous_amount,
    CASE
        WHEN p.vendor_name IS NULL THEN 'NEW'
        WHEN c.vendor_name IS NULL AND r.vendor_name IS NOT NULL THEN 'MISSING_RECURRING'
        WHEN c.vendor_name IS NULL THEN 'MISSING'
        WHEN ABS(c.total_amount - p.total_amount) / NULLIF(p.total_amount, 0) > 0.5 THEN 'SIGNIFICANT_CHANGE'
        ELSE 'NORMAL'
    END as change_type
FROM current_vendors c
FULL OUTER JOIN previous_vendors p ON c.vendor_name = p.vendor_name
LEFT JOIN recurring_vendors r ON p.vendor_name = r.vendor_name
WHERE c.vendor_name IS NOT NULL OR p.vendor_name IS NOT NULL;
```

### Cache Statistics Query

```sql
SELECT
    tier_used,
    operation_type,
    COUNT(*) as hit_count,
    AVG(response_time_ms) as avg_response_ms
FROM tier_usage_logs
WHERE user_id = @userId
  AND created_at >= @startDate
  AND created_at <= @endDate
GROUP BY tier_used, operation_type
ORDER BY operation_type, tier_used;
```

### Cost Estimation Formula

```csharp
public decimal EstimateMonthlyAICost(CacheStatisticsDto stats)
{
    const decimal Tier2CostPerOp = 0.00002m;  // Embedding lookup
    const decimal Tier3CostPerOp = 0.0003m;   // GPT-4o-mini
    const decimal Tier4CostPerOp = 0.01m;     // GPT-4o/Claude (rare)

    var tier2Cost = stats.Tier2Hits * Tier2CostPerOp;
    var tier3Cost = stats.Tier3Hits * Tier3CostPerOp;
    // Tier 4 would be logged separately if used

    return tier2Cost + tier3Cost;
}
```

---

## Migration Notes

**No database migrations required** for Sprint 9.

All functionality operates on existing tables from Sprints 2-8:
- `expense_reports` (Sprint 8)
- `expense_lines` (Sprint 8)
- `receipts` (Sprint 3)
- `tier_usage_logs` (Sprint 6)

---

## File Storage Patterns

### Excel Template Location

```
Azure Blob Storage (ccproctemp2025)
└── templates/
    └── expense-report-template.xlsx
```

### Generated Files (Transient)

Generated files are returned directly in HTTP response as byte streams.
Not persisted to blob storage (user can re-generate at any time).

---

## Data Volume Estimates

| Entity | Monthly Growth | Query Frequency |
|--------|---------------|-----------------|
| ExpenseReport | ~20 (10-20 users) | High (list, export) |
| ExpenseLine | ~1,000 (50/user) | High (export, MoM) |
| TierUsageLog | ~5,000 (50 ops/user/month) | Medium (dashboard) |
| Generated Excel | N/A (transient) | Medium |
| Generated PDF | N/A (transient) | Medium |
