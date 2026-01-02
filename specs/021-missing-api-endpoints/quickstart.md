# Quickstart: Missing API Endpoints

**Feature**: 021-missing-api-endpoints
**Date**: 2026-01-01

## Prerequisites

- Docker Desktop running
- .NET SDK 9.0 (via Docker)
- PostgreSQL (Supabase) accessible

## Quick Test

```bash
# Run all tests to verify baseline
cd /Users/rogercox/ExpenseTrack
docker run --rm -v "$(pwd)":/app -w /app mcr.microsoft.com/dotnet/sdk:9.0 \
  dotnet test backend/ExpenseFlow.sln --verbosity normal
```

## Implementation Order

### Phase 1: Update Contract Tests (Quick Win)

**Goal**: Make 12+ tests pass immediately by fixing path mismatches.

**Files to modify**:
- `backend/tests/ExpenseFlow.Contracts.Tests/AnalyticsEndpointContractTests.cs`
- `backend/tests/ExpenseFlow.Contracts.Tests/ReportEndpointContractTests.cs`
- `backend/tests/ExpenseFlow.Contracts.Tests/ReceiptEndpointContractTests.cs`
- `backend/tests/ExpenseFlow.Contracts.Tests/TransactionEndpointContractTests.cs`

**Path corrections**:
```
/api/analytics/spending-summary → /api/analytics/categories
/api/analytics/category-breakdown → /api/analytics/spending-by-category
/api/analytics/trends → /api/analytics/spending-trend
/api/analytics/vendor-insights → /api/analytics/spending-by-vendor
/api/analytics/budget-comparison → /api/analytics/comparison
/api/receipts/{id}/image → /api/receipts/{id}/download
/api/receipts/{id}/reprocess → /api/receipts/{id}/retry
```

### Phase 2: Report Generation Endpoint (P1)

**Goal**: Add `POST /api/reports/{id}/generate` endpoint.

**Files to create/modify**:
1. `ExpenseFlow.Shared/Enums/ReportStatus.cs` - Add `Generated = 1, Submitted = 2`
2. `ExpenseFlow.Core/Entities/ExpenseReport.cs` - Add `GeneratedAt`, `SubmittedAt` fields
3. `ExpenseFlow.Shared/DTOs/ReportValidationResultDto.cs` - New DTO
4. `ExpenseFlow.Shared/DTOs/GenerateReportResponseDto.cs` - New DTO
5. `ExpenseFlow.Core/Interfaces/IReportService.cs` - Add `GenerateAsync` method
6. `ExpenseFlow.Infrastructure/Services/ReportService.cs` - Implement validation + status change
7. `ExpenseFlow.Api/Controllers/ReportsController.cs` - Add generate endpoint
8. Migration for new columns

**Endpoint signature**:
```csharp
[HttpPost("{reportId:guid}/generate")]
[ProducesResponseType(typeof(GenerateReportResponseDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status409Conflict)]
public async Task<ActionResult<GenerateReportResponseDto>> Generate(
    Guid reportId,
    CancellationToken ct)
```

### Phase 3: Analytics Export Endpoint (P2)

**Goal**: Add `GET /api/analytics/export` endpoint.

**Files to create/modify**:
1. `ExpenseFlow.Shared/DTOs/AnalyticsExportRequestDto.cs` - New DTO
2. `ExpenseFlow.Core/Interfaces/IAnalyticsExportService.cs` - New interface
3. `ExpenseFlow.Infrastructure/Services/AnalyticsExportService.cs` - New service
4. `ExpenseFlow.Api/Controllers/AnalyticsController.cs` - Add export endpoint

**Endpoint signature**:
```csharp
[HttpGet("export")]
[ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> Export(
    [FromQuery] string startDate,
    [FromQuery] string endDate,
    [FromQuery] string format,
    [FromQuery] string? sections,
    CancellationToken ct)
```

### Phase 4: Report Submission Endpoint (P3)

**Goal**: Add `POST /api/reports/{id}/submit` endpoint.

**Files to modify**:
1. `ExpenseFlow.Shared/DTOs/SubmitReportResponseDto.cs` - New DTO
2. `ExpenseFlow.Core/Interfaces/IReportService.cs` - Add `SubmitAsync` method
3. `ExpenseFlow.Infrastructure/Services/ReportService.cs` - Implement status change
4. `ExpenseFlow.Api/Controllers/ReportsController.cs` - Add submit endpoint

**Endpoint signature**:
```csharp
[HttpPost("{reportId:guid}/submit")]
[ProducesResponseType(typeof(SubmitReportResponseDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status409Conflict)]
public async Task<ActionResult<SubmitReportResponseDto>> Submit(
    Guid reportId,
    CancellationToken ct)
```

## Testing Commands

```bash
# Run contract tests only
docker run --rm -v "$(pwd)":/app -w /app mcr.microsoft.com/dotnet/sdk:9.0 \
  dotnet test backend/tests/ExpenseFlow.Contracts.Tests --verbosity normal

# Run API tests only
docker run --rm -v "$(pwd)":/app -w /app mcr.microsoft.com/dotnet/sdk:9.0 \
  dotnet test backend/tests/ExpenseFlow.Api.Tests --verbosity normal

# Run all tests
docker run --rm -v "$(pwd)":/app -w /app mcr.microsoft.com/dotnet/sdk:9.0 \
  dotnet test backend/ExpenseFlow.sln --verbosity normal
```

## Database Migration

```bash
# Generate migration
docker run --rm -v "$(pwd)":/app -w /app/backend/src/ExpenseFlow.Api \
  mcr.microsoft.com/dotnet/sdk:9.0 \
  dotnet ef migrations add AddReportStatusTimestamps -p ../ExpenseFlow.Infrastructure

# Apply migration (requires database connection)
docker run --rm -v "$(pwd)":/app -w /app/backend/src/ExpenseFlow.Api \
  mcr.microsoft.com/dotnet/sdk:9.0 \
  dotnet ef database update
```

## Success Verification

After implementation, verify:

1. **Contract tests**: All 17+ contract tests pass
2. **Analytics export**: Returns CSV/XLSX with selected sections
3. **Report generate**: Validates lines, changes status, records timestamp
4. **Report submit**: Changes status, records timestamp
5. **Immutability**: Generated reports reject modifications (400)
6. **Concurrency**: Concurrent generate attempts return 409
