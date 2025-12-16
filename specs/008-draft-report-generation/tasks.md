# Implementation Tasks: Draft Report Generation

**Feature Branch**: `008-draft-report-generation`
**Generated**: 2025-12-16
**Input Sources**: spec.md, plan.md, data-model.md, research.md, contracts/reports-api.yaml

## Task Overview

| Phase | Description | Task Count |
|-------|-------------|------------|
| Phase 0 | Foundation (Entities, Enums, Migrations) | 5 |
| Phase 1 | Repository & Data Access | 3 |
| Phase 2 | Core Service (Draft Generation) | 4 |
| Phase 3 | API Layer (Controllers & DTOs) | 4 |
| Phase 4 | Learning Loop Integration | 2 |
| Phase 5 | Testing & Validation | 3 |
| **Total** | | **21** |

---

## Phase 0: Foundation (Entities, Enums, Migrations)

### Task 0.1: Create ReportStatus Enum
**Priority**: P1 | **Story**: US1 | **Estimate**: XS

**Description**: Create the ReportStatus enum in ExpenseFlow.Shared.

**Files**:
- Create: `backend/src/ExpenseFlow.Shared/Enums/ReportStatus.cs`

**Implementation**:
```csharp
public enum ReportStatus : short
{
    Draft = 0
    // Future: Submitted = 1, Approved = 2, Exported = 3
}
```

**Acceptance**:
- [ ] Enum compiles without errors
- [ ] Uses `short` backing type per data-model.md

**Dependencies**: None

---

### Task 0.2: Create MissingReceiptJustification Enum
**Priority**: P1 | **Story**: US2 | **Estimate**: XS

**Description**: Create the MissingReceiptJustification enum for missing receipt explanations.

**Files**:
- Create: `backend/src/ExpenseFlow.Shared/Enums/MissingReceiptJustification.cs`

**Implementation**:
```csharp
public enum MissingReceiptJustification : short
{
    None = 0,
    NotProvided = 1,
    Lost = 2,
    DigitalSubscription = 3,
    UnderThreshold = 4,
    Other = 5
}
```

**Acceptance**:
- [ ] Enum compiles without errors
- [ ] Values match spec FR-013 requirements

**Dependencies**: None

---

### Task 0.3: Create ExpenseReport Entity
**Priority**: P1 | **Story**: US1 | **Estimate**: S

**Description**: Create the ExpenseReport entity in ExpenseFlow.Core with all properties per data-model.md.

**Files**:
- Create: `backend/src/ExpenseFlow.Core/Entities/ExpenseReport.cs`

**Implementation**:
Per data-model.md:
- Id (Guid), UserId (Guid), Period (string), Status (ReportStatus)
- TotalAmount (decimal), LineCount (int), MissingReceiptCount (int)
- Tier1HitCount, Tier2HitCount, Tier3HitCount (int)
- IsDeleted (bool), CreatedAt (DateTime), UpdatedAt (DateTime?)
- RowVersion (uint) for optimistic concurrency
- Navigation: User, Lines collection

**Acceptance**:
- [ ] Entity has all properties from data-model.md
- [ ] Navigation properties to User and Lines defined
- [ ] RowVersion property for optimistic concurrency

**Dependencies**: Task 0.1

---

### Task 0.4: Create ExpenseLine Entity
**Priority**: P1 | **Story**: US1 | **Estimate**: S

**Description**: Create the ExpenseLine entity with categorization and tier tracking fields.

**Files**:
- Create: `backend/src/ExpenseFlow.Core/Entities/ExpenseLine.cs`

**Implementation**:
Per data-model.md:
- Id (Guid), ReportId (Guid), ReceiptId? (Guid?), TransactionId? (Guid?)
- LineOrder (int), ExpenseDate (DateOnly), Amount (decimal)
- OriginalDescription, NormalizedDescription, VendorName (strings)
- GLCode, GLCodeSuggested, GLCodeSource (strings), GLCodeTier (int?)
- DepartmentCode, DepartmentSuggested, DepartmentSource (strings), DepartmentTier (int?)
- HasReceipt (bool), MissingReceiptJustification (enum?), JustificationNote (string?)
- IsUserEdited (bool), CreatedAt, UpdatedAt
- Navigation: Report, Receipt, Transaction

**Acceptance**:
- [ ] All properties from data-model.md present
- [ ] Nullable types used correctly for optional fields
- [ ] Navigation properties defined

**Dependencies**: Task 0.2, Task 0.3

---

### Task 0.5: Create EF Configurations and Migration
**Priority**: P1 | **Story**: US1 | **Estimate**: M

**Description**: Create Entity Framework configurations and generate database migration.

**Files**:
- Create: `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/ExpenseReportConfiguration.cs`
- Create: `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/ExpenseLineConfiguration.cs`
- Modify: `backend/src/ExpenseFlow.Infrastructure/Data/ExpenseFlowDbContext.cs`

**Implementation**:
1. Create ExpenseReportConfiguration per data-model.md:
   - Table name: `expense_reports`
   - Unique index on (UserId, Period) with IsDeleted filter
   - Index on (UserId, CreatedAt DESC)
   - Configure RowVersion as xmin
2. Create ExpenseLineConfiguration per data-model.md:
   - Table name: `expense_lines`
   - Index on (ReportId, LineOrder)
   - Index on TransactionId
   - Cascade delete from Report
3. Add DbSets to ExpenseFlowDbContext
4. Generate migration: `dotnet ef migrations add AddExpenseReports`

**Acceptance**:
- [ ] Both configurations follow patterns in data-model.md
- [ ] DbSets added: `ExpenseReports`, `ExpenseLines`
- [ ] Migration generates successfully
- [ ] Migration applies without errors

**Dependencies**: Task 0.3, Task 0.4

---

## Phase 1: Repository & Data Access

### Task 1.1: Create IExpenseReportRepository Interface
**Priority**: P1 | **Story**: US1 | **Estimate**: S

**Description**: Define repository interface for expense report data access.

**Files**:
- Create: `backend/src/ExpenseFlow.Core/Interfaces/IExpenseReportRepository.cs`

**Implementation**:
```csharp
public interface IExpenseReportRepository
{
    Task<ExpenseReport?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ExpenseReport?> GetByIdWithLinesAsync(Guid id, CancellationToken ct = default);
    Task<ExpenseReport?> GetDraftByUserAndPeriodAsync(Guid userId, string period, CancellationToken ct = default);
    Task<List<ExpenseReport>> GetByUserAsync(Guid userId, ReportStatus? status, string? period, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetCountByUserAsync(Guid userId, ReportStatus? status, string? period, CancellationToken ct = default);
    Task<ExpenseReport> AddAsync(ExpenseReport report, CancellationToken ct = default);
    Task UpdateAsync(ExpenseReport report, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task<ExpenseLine?> GetLineByIdAsync(Guid reportId, Guid lineId, CancellationToken ct = default);
    Task UpdateLineAsync(ExpenseLine line, CancellationToken ct = default);
}
```

**Acceptance**:
- [ ] Interface supports all API operations from contracts/reports-api.yaml
- [ ] Pagination parameters included
- [ ] Soft delete method present

**Dependencies**: Task 0.3, Task 0.4

---

### Task 1.2: Implement ExpenseReportRepository
**Priority**: P1 | **Story**: US1 | **Estimate**: M

**Description**: Implement the repository with EF Core queries.

**Files**:
- Create: `backend/src/ExpenseFlow.Infrastructure/Repositories/ExpenseReportRepository.cs`

**Implementation**:
- Implement all interface methods
- Use `AsNoTracking()` for read operations
- Apply soft delete filter (`WHERE NOT IsDeleted`)
- Include Lines with eager loading where needed
- Use proper index hints for period/user queries

**Acceptance**:
- [ ] All interface methods implemented
- [ ] Soft delete filter applied globally
- [ ] Pagination works correctly
- [ ] Eager loading for GetByIdWithLinesAsync

**Dependencies**: Task 1.1, Task 0.5

---

### Task 1.3: Register Repository in DI
**Priority**: P1 | **Story**: US1 | **Estimate**: XS

**Description**: Register the repository in dependency injection.

**Files**:
- Modify: `backend/src/ExpenseFlow.Infrastructure/ServiceCollectionExtensions.cs`

**Implementation**:
```csharp
// Sprint 8: Draft Report Generation
services.AddScoped<IExpenseReportRepository, ExpenseReportRepository>();
```

**Acceptance**:
- [ ] Repository registered in correct section
- [ ] Comment indicates sprint number

**Dependencies**: Task 1.2

---

## Phase 2: Core Service (Draft Generation)

### Task 2.1: Create IReportService Interface
**Priority**: P1 | **Story**: US1 | **Estimate**: S

**Description**: Define the report service interface for draft generation and management.

**Files**:
- Create: `backend/src/ExpenseFlow.Core/Interfaces/IReportService.cs`

**Implementation**:
```csharp
public interface IReportService
{
    Task<ExpenseReport> GenerateDraftAsync(Guid userId, string period, bool replaceExisting, CancellationToken ct = default);
    Task<ExpenseReport?> GetReportAsync(Guid userId, Guid reportId, CancellationToken ct = default);
    Task<(List<ExpenseReport> Items, int TotalCount)> ListReportsAsync(Guid userId, ReportStatus? status, string? period, int page, int pageSize, CancellationToken ct = default);
    Task<ExpenseLine> UpdateLineAsync(Guid userId, Guid reportId, Guid lineId, UpdateLineRequest request, CancellationToken ct = default);
    Task DeleteReportAsync(Guid userId, Guid reportId, CancellationToken ct = default);
    Task<ReportSummaryDto> GetReportSummaryAsync(Guid userId, Guid reportId, CancellationToken ct = default);
}
```

**Acceptance**:
- [ ] Methods match API operations from contracts/reports-api.yaml
- [ ] UserId parameter for authorization checks
- [ ] CancellationToken on all async methods

**Dependencies**: Task 0.3, Task 0.4

---

### Task 2.2: Implement ReportService - Draft Generation
**Priority**: P1 | **Story**: US1 | **Estimate**: L

**Description**: Implement the core draft generation logic that orchestrates matching, categorization, and normalization services.

**Files**:
- Create: `backend/src/ExpenseFlow.Infrastructure/Services/ReportService.cs`

**Implementation**:
Per research.md decisions:
1. Query confirmed ReceiptTransactionMatches for period (IMatchRepository)
2. Query unmatched transactions for period (MatchStatus = Unmatched)
3. For each item, call ICategorizationService.GetCategorizationAsync
4. For each item, call IDescriptionNormalizationService.NormalizeAsync
5. Assemble ExpenseReport with ExpenseLines
6. Track tier counts (Tier1/2/3HitCount)
7. Persist in single transaction

**Key Logic**:
```csharp
// Parallel categorization for performance (per research.md Topic 6)
var categorizationTasks = items.Select(async item => {
    var result = await _categorizationService.GetCategorizationAsync(item.Description, ct);
    return (item, result);
});
var results = await Task.WhenAll(categorizationTasks);
```

**Acceptance**:
- [ ] Generates draft from matches and unmatched transactions
- [ ] Calls existing ICategorizationService (no duplication)
- [ ] Calls existing IDescriptionNormalizationService
- [ ] Tracks tier hit counts
- [ ] Handles replaceExisting=true (soft deletes old draft)
- [ ] Performance: <30 seconds for 50 expenses (per plan.md)

**Dependencies**: Task 1.3, Task 2.1

---

### Task 2.3: Implement ReportService - CRUD Operations
**Priority**: P1 | **Story**: US2 | **Estimate**: M

**Description**: Implement get, list, update, and delete operations.

**Files**:
- Modify: `backend/src/ExpenseFlow.Infrastructure/Services/ReportService.cs`

**Implementation**:
1. GetReportAsync: Verify ownership, return with lines
2. ListReportsAsync: Filter by status/period, paginate
3. UpdateLineAsync: Update GL/dept/description, mark IsUserEdited=true
4. DeleteReportAsync: Verify ownership, soft delete
5. GetReportSummaryAsync: Calculate tier hit rates, completion rates

**Acceptance**:
- [ ] All CRUD operations implemented
- [ ] Ownership checks on all operations (userId match)
- [ ] UpdateLineAsync marks IsUserEdited=true
- [ ] Summary calculates percentages correctly

**Dependencies**: Task 2.2

---

### Task 2.4: Register ReportService in DI
**Priority**: P1 | **Story**: US1 | **Estimate**: XS

**Description**: Register the service in dependency injection.

**Files**:
- Modify: `backend/src/ExpenseFlow.Infrastructure/ServiceCollectionExtensions.cs`

**Implementation**:
```csharp
services.AddScoped<IReportService, ReportService>();
```

**Acceptance**:
- [ ] Service registered correctly

**Dependencies**: Task 2.3

---

## Phase 3: API Layer (Controllers & DTOs)

### Task 3.1: Create DTOs
**Priority**: P1 | **Story**: US1 | **Estimate**: S

**Description**: Create DTOs matching the OpenAPI contract.

**Files**:
- Create: `backend/src/ExpenseFlow.Shared/DTOs/ExpenseReportDto.cs`
- Create: `backend/src/ExpenseFlow.Shared/DTOs/ExpenseLineDto.cs`
- Create: `backend/src/ExpenseFlow.Shared/DTOs/ReportSummaryDto.cs`
- Create: `backend/src/ExpenseFlow.Shared/DTOs/GenerateDraftRequest.cs`
- Create: `backend/src/ExpenseFlow.Shared/DTOs/UpdateLineRequest.cs`
- Create: `backend/src/ExpenseFlow.Shared/DTOs/ReportListResponse.cs`
- Create: `backend/src/ExpenseFlow.Shared/DTOs/ReportListItemDto.cs`

**Implementation**:
Per contracts/reports-api.yaml schemas.

**Acceptance**:
- [ ] All DTOs match OpenAPI schemas
- [ ] Proper nullable annotations
- [ ] Data annotation validators where needed

**Dependencies**: Task 0.1, Task 0.2

---

### Task 3.2: Create Mapping Extension Methods
**Priority**: P1 | **Story**: US1 | **Estimate**: S

**Description**: Create extension methods for entity-to-DTO conversions, following the existing codebase pattern of manual mapping.

**Files**:
- Create: `backend/src/ExpenseFlow.Shared/Extensions/ReportMappingExtensions.cs`

**Implementation**:
```csharp
public static class ReportMappingExtensions
{
    public static ExpenseReportDto ToDto(this ExpenseReport report) => new()
    {
        Id = report.Id,
        Period = report.Period,
        Status = report.Status,
        TotalAmount = report.TotalAmount,
        // ... all properties
        Lines = report.Lines?.Select(l => l.ToDto()).ToList()
    };

    public static ReportListItemDto ToListItemDto(this ExpenseReport report) => new()
    {
        Id = report.Id,
        Period = report.Period,
        // ... summary properties only
    };

    public static ExpenseLineDto ToDto(this ExpenseLine line) => new()
    {
        Id = line.Id,
        LineOrder = line.LineOrder,
        // ... all properties
    };
}
```

**Acceptance**:
- [ ] All mapping methods implemented (ToDto, ToListItemDto)
- [ ] Consistent with existing codebase patterns (manual mapping, no AutoMapper)
- [ ] Null handling for optional navigation properties

**Dependencies**: Task 3.1

---

### Task 3.3: Create ReportsController
**Priority**: P1 | **Story**: US1, US2 | **Estimate**: M

**Description**: Implement REST API endpoints per contracts/reports-api.yaml.

**Files**:
- Create: `backend/src/ExpenseFlow.Api/Controllers/ReportsController.cs`

**Implementation**:
Endpoints per OpenAPI contract:
- `POST /api/reports/draft` - GenerateDraft (201 Created)
- `GET /api/reports` - ListReports (200 OK)
- `GET /api/reports/{reportId}` - GetReport (200 OK)
- `DELETE /api/reports/{reportId}` - DeleteReport (204 No Content)
- `PUT /api/reports/{reportId}/lines/{lineId}` - UpdateLine (200 OK)
- `GET /api/reports/{reportId}/summary` - GetReportSummary (200 OK)

**Acceptance**:
- [ ] All endpoints implemented per OpenAPI contract
- [ ] Proper HTTP status codes (201, 200, 204, 400, 404, 409)
- [ ] [Authorize] attribute on all endpoints
- [ ] Returns ProblemDetails for errors
- [ ] Period format validation (YYYY-MM)

**Dependencies**: Task 2.4, Task 3.2

---

### Task 3.4: Add Request Validation
**Priority**: P2 | **Story**: US2 | **Estimate**: S

**Description**: Add FluentValidation validators for request DTOs.

**Files**:
- Create: `backend/src/ExpenseFlow.Shared/Validators/GenerateDraftRequestValidator.cs`
- Create: `backend/src/ExpenseFlow.Shared/Validators/UpdateLineRequestValidator.cs`

**Implementation**:
- Period regex: `^\d{4}-(0[1-9]|1[0-2])$`
- GL code max length: 10
- Department code max length: 20
- Justification note max length: 500

**Acceptance**:
- [ ] Period format validated
- [ ] Max lengths enforced per data-model.md
- [ ] Validators registered in DI

**Dependencies**: Task 3.1

---

## Phase 4: Learning Loop Integration

### Task 4.1: Implement Learning on Line Update
**Priority**: P2 | **Story**: US3 | **Estimate**: M

**Description**: When users edit GL/department, call ConfirmCategorizationAsync to update vendor aliases and embeddings.

**Files**:
- Modify: `backend/src/ExpenseFlow.Infrastructure/Services/ReportService.cs`

**Implementation**:
In UpdateLineAsync:
```csharp
if (request.GlCode != null && request.GlCode != line.GLCodeSuggested)
{
    await _categorizationService.ConfirmCategorizationAsync(
        new TransactionCategorizationConfirmation
        {
            Description = line.OriginalDescription,
            VendorName = line.VendorName,
            GlCode = request.GlCode,
            DepartmentCode = request.DepartmentCode ?? line.DepartmentCode,
            IsUserConfirmed = true
        }, ct);
}
```

**Acceptance**:
- [ ] ConfirmCategorizationAsync called when user changes GL/department
- [ ] Only called when value differs from suggestion
- [ ] VendorAlias updates occur (per existing service behavior)
- [ ] ExpenseEmbedding created (per existing service behavior)

**Dependencies**: Task 2.3

---

### Task 4.2: Track User Edits
**Priority**: P2 | **Story**: US3 | **Estimate**: S

**Description**: Ensure IsUserEdited flag is set correctly and persists.

**Files**:
- Modify: `backend/src/ExpenseFlow.Infrastructure/Services/ReportService.cs`

**Implementation**:
- Set `IsUserEdited = true` when user modifies any categorization field
- Update `UpdatedAt` timestamp
- Recalculate report totals if needed

**Acceptance**:
- [ ] IsUserEdited set to true on any edit
- [ ] UpdatedAt timestamp updated
- [ ] Original suggestions preserved (GLCodeSuggested, DepartmentSuggested)

**Dependencies**: Task 4.1

---

## Phase 5: Testing & Validation

### Task 5.1: Unit Tests for ReportService
**Priority**: P1 | **Story**: US1, US2, US3 | **Estimate**: L

**Description**: Create unit tests for draft generation and CRUD operations.

**Files**:
- Create: `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/ReportServiceTests.cs`

**Test Cases**:
1. GenerateDraftAsync creates report with correct line count
2. GenerateDraftAsync includes matched and unmatched transactions
3. GenerateDraftAsync pre-populates GL codes from categorization service
4. GenerateDraftAsync tracks tier hit counts correctly
5. GenerateDraftAsync replaces existing draft when replaceExisting=true
6. GenerateDraftAsync throws conflict when draft exists and replaceExisting=false
7. UpdateLineAsync marks line as user edited
8. UpdateLineAsync calls ConfirmCategorizationAsync for learning
9. DeleteReportAsync soft deletes report
10. GetReportSummaryAsync calculates tier rates correctly
11. GetReportSummaryAsync returns correct GL code completion percentage (FR-015)
12. GetReportSummaryAsync returns correct department completion percentage (FR-015)
13. GetReportSummaryAsync counts lines with missing receipt justifications

**Acceptance**:
- [ ] All 13 test cases pass
- [ ] Mocks used for ICategorizationService, IDescriptionNormalizationService
- [ ] Edge cases covered (empty period, missing receipts, all lines missing GL codes)

**Dependencies**: Task 4.2

---

### Task 5.2: Integration Tests for API
**Priority**: P2 | **Story**: US1, US2 | **Estimate**: M

**Description**: Create integration tests for API endpoints.

**Files**:
- Create: `backend/tests/ExpenseFlow.Api.Tests/Controllers/ReportsControllerTests.cs`

**Test Cases**:
1. POST /reports/draft returns 201 with valid period
2. POST /reports/draft returns 400 with invalid period format
3. POST /reports/draft returns 409 when draft exists
4. GET /reports returns paginated list
5. GET /reports/{id} returns 404 for nonexistent report
6. PUT /reports/{id}/lines/{lineId} updates and returns 200
7. DELETE /reports/{id} returns 204
8. GET /reports/{id}/summary returns correct tier hit rates and completion percentages

**Acceptance**:
- [ ] All 8 HTTP status codes verified
- [ ] Response bodies match OpenAPI contract
- [ ] Authentication required on all endpoints
- [ ] Summary endpoint returns ReportSummaryDto per contract

**Dependencies**: Task 3.3

---

### Task 5.3: Manual Validation per Checklist
**Priority**: P1 | **Story**: All | **Estimate**: S

**Description**: Execute manual validation against quickstart.md checklist.

**Validation Steps**:
Per quickstart.md:
- [ ] Draft generation completes in <30 seconds for 50 expenses
- [ ] All matched receipts appear in draft
- [ ] Unmatched transactions flagged as missing receipt
- [ ] GL codes pre-populated with tier indicator
- [ ] Departments pre-populated with tier indicator
- [ ] Descriptions normalized
- [ ] User edits persist correctly
- [ ] User edits trigger ConfirmCategorizationAsync
- [ ] Missing receipt justification UI works
- [ ] Tier usage logged for each suggestion

**Dependencies**: Task 5.1, Task 5.2

---

## Dependency Graph

```
Phase 0 (Foundation)
├── Task 0.1: ReportStatus Enum
├── Task 0.2: MissingReceiptJustification Enum
├── Task 0.3: ExpenseReport Entity ──────────┐
├── Task 0.4: ExpenseLine Entity ────────────┤
└── Task 0.5: EF Configurations & Migration ─┴──┐
                                                │
Phase 1 (Repository)                            │
├── Task 1.1: IExpenseReportRepository ─────────┤
├── Task 1.2: ExpenseReportRepository ──────────┤
└── Task 1.3: Register Repository ──────────────┤
                                                │
Phase 2 (Service)                               │
├── Task 2.1: IReportService ───────────────────┤
├── Task 2.2: ReportService - Generation ───────┤
├── Task 2.3: ReportService - CRUD ─────────────┤
└── Task 2.4: Register Service ─────────────────┤
                                                │
Phase 3 (API)                                   │
├── Task 3.1: DTOs ─────────────────────────────┤
├── Task 3.2: Mapping Extensions ───────────────┤
├── Task 3.3: ReportsController ────────────────┤
└── Task 3.4: Request Validation ───────────────┤
                                                │
Phase 4 (Learning)                              │
├── Task 4.1: Learning on Line Update ──────────┤
└── Task 4.2: Track User Edits ─────────────────┤
                                                │
Phase 5 (Testing)                               │
├── Task 5.1: Unit Tests ───────────────────────┤
├── Task 5.2: Integration Tests ────────────────┤
└── Task 5.3: Manual Validation ────────────────┘
```

## Size Legend

| Size | Description | Typical Duration |
|------|-------------|------------------|
| XS | Trivial change, single file | < 15 min |
| S | Small, well-defined task | 15-30 min |
| M | Medium complexity, multiple files | 30-60 min |
| L | Large, significant implementation | 1-2 hours |
| XL | Very large, consider splitting | 2+ hours |

---

## Future Considerations

### Metrics Tracking for Success Criteria (SC-002, SC-003)

The success criteria specify 70% accuracy targets for GL code and department pre-population:
- **SC-002**: At least 70% of expense lines have GL codes pre-populated correctly
- **SC-003**: At least 70% of expense lines have departments pre-populated correctly

**Current Coverage**: The `ReportSummaryDto` tracks completion rates (GL/dept fields filled), but not *correction rates* (how often users change the suggested values).

**Future Enhancement**: Consider adding tracking in a future sprint:
1. Add `GLCodeWasCorrected` and `DepartmentWasCorrected` boolean fields to ExpenseLine
2. Set to `true` when user updates to a different value than `*Suggested`
3. Expose correction rate in `ReportSummaryDto` or a dedicated analytics endpoint
4. This enables measuring actual prediction accuracy vs. the 70% target

This is deferred because:
- The learning loop (Task 4.1) already feeds corrections back to improve future suggestions
- Tier hit rates provide a proxy metric for system improvement
- Explicit correction tracking adds complexity without immediate user benefit
