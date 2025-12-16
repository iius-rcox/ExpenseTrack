# Quickstart: Draft Report Generation

**Feature**: 008-draft-report-generation
**Date**: 2025-12-16

## Prerequisites

Before implementing this feature, ensure:

1. **Sprint 5 Complete**: Matching engine operational (ReceiptTransactionMatch entity exists)
2. **Sprint 6 Complete**: Tiered categorization working (ICategorizationService, IDescriptionNormalizationService)
3. **Database Access**: PostgreSQL with Supabase running
4. **Development Environment**: .NET 8 SDK, VS Code or Visual Studio

## Quick Setup

### 1. Create Migration

```bash
cd backend/src/ExpenseFlow.Infrastructure
dotnet ef migrations add AddExpenseReports --startup-project ../ExpenseFlow.Api
```

### 2. Apply Migration

```bash
cd backend/src/ExpenseFlow.Api
dotnet ef database update
```

### 3. Register Services

Add to `ServiceCollectionExtensions.cs`:

```csharp
// Sprint 8: Draft Report Generation
services.AddScoped<IExpenseReportRepository, ExpenseReportRepository>();
services.AddScoped<IReportService, ReportService>();
```

### 4. Add DbSet to Context

Add to `ExpenseFlowDbContext.cs`:

```csharp
// Sprint 8: Draft Report Generation
public DbSet<ExpenseReport> ExpenseReports => Set<ExpenseReport>();
public DbSet<ExpenseLine> ExpenseLines => Set<ExpenseLine>();
```

## Key Implementation Steps

### Step 1: Create Entities (ExpenseFlow.Core)

1. Create `ExpenseReport.cs` in `/Entities/`
2. Create `ExpenseLine.cs` in `/Entities/`
3. Add enums to `ExpenseFlow.Shared/Enums/`

### Step 2: Create Repository (ExpenseFlow.Infrastructure)

1. Create `IExpenseReportRepository.cs` interface
2. Implement `ExpenseReportRepository.cs`
3. Add EF configurations

### Step 3: Create Service (ExpenseFlow.Infrastructure)

1. Create `IReportService.cs` interface
2. Implement `ReportService.cs` with:
   - Draft generation logic
   - Integration with ICategorizationService
   - Integration with IDescriptionNormalizationService
   - Learning loop (ConfirmCategorizationAsync calls)

### Step 4: Create Controller (ExpenseFlow.Api)

1. Create `ReportsController.cs`
2. Implement endpoints per contracts/reports-api.yaml

### Step 5: Create DTOs (ExpenseFlow.Shared)

1. Create `ExpenseReportDto.cs`
2. Create `ExpenseLineDto.cs`
3. Create `ReportSummaryDto.cs`

## Testing the Feature

### Generate a Draft (cURL)

```bash
curl -X POST https://localhost:5001/api/reports/draft \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"period": "2025-01", "replaceExisting": false}'
```

### Expected Response

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "period": "2025-01",
  "status": "Draft",
  "totalAmount": 1250.00,
  "lineCount": 25,
  "missingReceiptCount": 3,
  "tier1HitCount": 18,
  "tier2HitCount": 5,
  "tier3HitCount": 2,
  "createdAt": "2025-01-15T10:30:00Z",
  "lines": [
    {
      "id": "...",
      "lineOrder": 1,
      "expenseDate": "2025-01-10",
      "amount": 425.00,
      "originalDescription": "DELTA AIR 0062363598531",
      "normalizedDescription": "Delta Airlines Flight",
      "glCode": "66300",
      "glCodeTier": 1,
      "glCodeSource": "VendorAlias",
      "hasReceipt": true,
      "receiptThumbnailUrl": "https://..."
    }
  ]
}
```

### Update a Line

```bash
curl -X PUT https://localhost:5001/api/reports/{reportId}/lines/{lineId} \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"glCode": "63300", "departmentCode": "07"}'
```

## Validation Checklist

- [ ] Draft generation completes in <30 seconds for 50 expenses
- [ ] All matched receipts appear in draft
- [ ] Unmatched transactions flagged as missing receipt
- [ ] GL codes pre-populated with tier indicator
- [ ] Departments pre-populated with tier indicator
- [ ] Descriptions normalized
- [ ] User edits persist correctly
- [ ] User edits trigger ConfirmCategorizationAsync
- [ ] Missing receipt justification required before export (Sprint 9)
- [ ] Tier usage logged for each suggestion

## Common Issues

### Issue: "No expenses found for period"

**Cause**: No confirmed matches or unmatched transactions for the period.
**Solution**: Ensure matching has been run and confirmed for the period.

### Issue: Categorization returns null

**Cause**: Transaction has no description or vendor alias not found.
**Solution**: Check that ICategorizationService is handling edge cases gracefully.

### Issue: Slow generation (>30s)

**Cause**: Too many Tier 3 (AI) calls.
**Solution**:
1. Check cache hit rates
2. Ensure description cache is populated
3. Consider parallel processing for AI calls

## Architecture Notes

```
┌─────────────────┐     ┌─────────────────────┐
│ ReportsController│────▶│    ReportService    │
└─────────────────┘     └─────────────────────┘
                                │
                    ┌───────────┼───────────┐
                    ▼           ▼           ▼
           ┌────────────┐ ┌──────────┐ ┌─────────────┐
           │MatchRepo   │ │Categoriz.│ │Description  │
           │(Sprint 5)  │ │Service   │ │Normalization│
           └────────────┘ │(Sprint 6)│ │(Sprint 6)   │
                          └──────────┘ └─────────────┘
```

The ReportService orchestrates but delegates all categorization and normalization to existing services to maintain the tiered cost architecture.
