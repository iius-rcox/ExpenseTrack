# Research: Missing API Endpoints

**Feature**: 021-missing-api-endpoints
**Date**: 2026-01-01

## Research Tasks

### 1. Analytics Export Implementation Patterns

**Decision**: Use existing CsvHelper and ClosedXML packages for export generation

**Rationale**:
- CsvHelper 31.0.0 already in project (`ExpenseFlow.Infrastructure.csproj:29`)
- ClosedXML 0.102.2 already in project (`ExpenseFlow.Infrastructure.csproj:30`)
- Existing `IExcelExportService` in `ReportsController` shows established pattern
- No need for additional dependencies

**Alternatives Considered**:
- EPPlus: License cost for commercial use
- NPOI: More complex API, less C# idiomatic
- Custom CSV writing: Reinventing the wheel

**Implementation Pattern**:
```csharp
// CSV: Use CsvHelper with typed records
using var writer = new StringWriter();
using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
csv.WriteRecords(data);

// Excel: Use ClosedXML with multiple worksheets
using var workbook = new XLWorkbook();
var trendsSheet = workbook.AddWorksheet("Spending Trends");
var categoriesSheet = workbook.AddWorksheet("Categories");
var vendorsSheet = workbook.AddWorksheet("Vendors");
```

### 2. Report Status Enum Extension

**Decision**: Add `Generated = 1` and `Submitted = 2` to existing `ReportStatus` enum

**Rationale**:
- Current enum only has `Draft = 0` with comments indicating future states
- Values 1, 2 match the commented placeholders
- No breaking change to existing database records (all current reports are Draft)

**Current State** (`ExpenseFlow.Shared/Enums/ReportStatus.cs`):
```csharp
public enum ReportStatus : short
{
    Draft = 0
    // Future states (Sprint 9+):
    // Submitted = 1,
    // Approved = 2,
    // Exported = 3
}
```

**New State**:
```csharp
public enum ReportStatus : short
{
    Draft = 0,
    Generated = 1,
    Submitted = 2
    // Future states:
    // Approved = 3,
    // Exported = 4
}
```

**Note**: Existing comments suggested `Submitted = 1`, but our spec requires `Generated` before `Submitted`, so `Generated = 1` is the logical order.

### 3. Report Validation Logic

**Decision**: Implement validation in `ReportService` with `ReportValidationResult` DTO

**Rationale**:
- Strict validation per clarification: category + amount > $0 + receipt required
- Return structured errors allowing UI to highlight specific issues
- Use FluentValidation patterns already in project

**Validation Rules**:
1. Report must have at least one expense line
2. Each line must have:
   - `CategoryId` not null/empty
   - `Amount > 0`
   - `ReceiptId` not null (attached receipt)
3. Report must be in `Draft` status

**Implementation Pattern**:
```csharp
public class ReportValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<ValidationError> Errors { get; set; } = new();
    public List<ValidationWarning> Warnings { get; set; } = new();
}

public class ValidationError
{
    public Guid? LineId { get; set; }
    public string Field { get; set; }
    public string Message { get; set; }
}
```

### 4. Immutability Enforcement for Generated Reports

**Decision**: Add status check in `UpdateLineAsync` to reject modifications on generated reports

**Rationale**:
- FR-005: "Generated reports MUST be immutable"
- Simplest enforcement: check status before any modification
- Return 400 Bad Request with clear message

**Implementation Pattern**:
```csharp
public async Task<ExpenseLineDto?> UpdateLineAsync(...)
{
    var report = await GetReportAsync(reportId);
    if (report.Status != ReportStatus.Draft)
    {
        throw new InvalidOperationException("Cannot modify a finalized report");
    }
    // ... existing update logic
}
```

### 5. Concurrency Handling for Generate Endpoint

**Decision**: Use EF Core optimistic concurrency with RowVersion

**Rationale**:
- Edge case in spec: concurrent generate requests → first wins, second gets 409
- `ExpenseReport` entity likely already has `RowVersion` for concurrency
- Standard EF Core pattern for optimistic locking

**Implementation Pattern**:
```csharp
try
{
    report.Status = ReportStatus.Generated;
    report.GeneratedAt = DateTime.UtcNow;
    await _dbContext.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
    throw new ConflictException("Report was modified by another request");
}
```

### 6. Audit Trail for Status Transitions

**Decision**: Add `GeneratedAt` and `SubmittedAt` timestamp fields to `ExpenseReport` entity

**Rationale**:
- FR-011: "Report status transitions MUST be logged for audit trail"
- Nullable DateTimeOffset fields on entity
- Serilog logging for audit events

**New Fields**:
```csharp
public DateTimeOffset? GeneratedAt { get; set; }
public DateTimeOffset? SubmittedAt { get; set; }
```

### 7. Contract Test Path Corrections

**Decision**: Update contract tests to use actual endpoint paths

**Rationale**:
- 12+ tests fail due to naming convention differences, not missing functionality
- Quick win: fix paths → tests pass immediately
- No code changes required for these

**Path Mappings**:
| Contract Test Path | Actual Path |
|-------------------|-------------|
| `/api/analytics/spending-summary` | `/api/analytics/categories` |
| `/api/analytics/category-breakdown` | `/api/analytics/spending-by-category` |
| `/api/analytics/trends` | `/api/analytics/spending-trend` |
| `/api/analytics/vendor-insights` | `/api/analytics/spending-by-vendor` |
| `/api/analytics/budget-comparison` | `/api/analytics/comparison` |
| `/api/receipts/{id}/image` | `/api/receipts/{id}/download` |
| `/api/receipts/{id}/reprocess` | `/api/receipts/{id}/retry` |
| `/api/transactions/{id}/categorize` | `/api/categorization/transactions/{id}/confirm` |

## Dependencies Summary

| Component | Package/Version | Status |
|-----------|-----------------|--------|
| CSV Export | CsvHelper 31.0.0 | ✅ Already installed |
| Excel Export | ClosedXML 0.102.2 | ✅ Already installed |
| Validation | FluentValidation | ✅ Already installed |
| Logging | Serilog | ✅ Already configured |

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Large export times | Medium | Medium | Enforce 5-year max date range; use streaming for large datasets |
| Status transition race | Low | Low | Optimistic concurrency with clear 409 response |
| Breaking existing reports | Low | High | All existing reports are Draft; new statuses are additive |

## Next Steps

1. ✅ Phase 0 complete - all unknowns resolved
2. → Proceed to Phase 1: data-model.md, contracts/, quickstart.md
