# Quickstart: Matching Engine

**Feature**: 005-matching-engine
**Date**: 2025-12-15

## Prerequisites

Before implementing this feature, ensure:

1. **Sprint 3 Complete**: Receipt pipeline working (receipts with extracted vendor/date/amount)
2. **Sprint 4 Complete**: Statement import working (transactions imported)
3. **Sprint 2 Tables**: VendorAliases table exists with schema from Sprint 2

## Quick Setup

### 1. Add NuGet Package

```bash
cd backend/src/ExpenseFlow.Infrastructure
dotnet add package F23.StringSimilarity --version 5.1.0
```

### 2. Create Migration

```bash
cd backend
dotnet ef migrations add AddReceiptTransactionMatch \
  -p src/ExpenseFlow.Infrastructure \
  -s src/ExpenseFlow.Api \
  -o Data/Migrations
```

### 3. Apply Migration

```bash
# Port-forward to Supabase PostgreSQL
kubectl port-forward svc/supabase-postgresql 5432:5432 -n expenseflow-dev

# Apply migration
dotnet ef database update \
  -p src/ExpenseFlow.Infrastructure \
  -s src/ExpenseFlow.Api
```

### 4. Register Services

Add to `Program.cs`:

```csharp
// Matching services
builder.Services.AddScoped<IMatchingService, MatchingService>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<IFuzzyMatchingService, FuzzyMatchingService>();

// Background job
builder.Services.AddScoped<AliasConfidenceDecayJob>();
```

Add Hangfire job registration:

```csharp
// After app.UseHangfireDashboard()
RecurringJob.AddOrUpdate<AliasConfidenceDecayJob>(
    "alias-confidence-decay",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Weekly(DayOfWeek.Sunday, 2, 0));
```

## Key Implementation Files

| File | Purpose |
|------|---------|
| `ExpenseFlow.Core/Entities/ReceiptTransactionMatch.cs` | New entity |
| `ExpenseFlow.Core/Interfaces/IMatchingService.cs` | Service interface |
| `ExpenseFlow.Infrastructure/Services/MatchingService.cs` | Core matching algorithm |
| `ExpenseFlow.Infrastructure/Services/FuzzyMatchingService.cs` | Levenshtein wrapper |
| `ExpenseFlow.Api/Controllers/MatchingController.cs` | API endpoints |
| `ExpenseFlow.Infrastructure/Jobs/AliasConfidenceDecayJob.cs` | Weekly maintenance |

## Testing the Feature

### 1. Run Auto-Match

```bash
# Trigger auto-match for all unmatched receipts
curl -X POST https://localhost:5001/api/matching/auto \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json"
```

Expected response:
```json
{
  "proposedCount": 7,
  "processedCount": 10,
  "ambiguousCount": 1,
  "durationMs": 1250
}
```

### 2. Review Proposals

```bash
curl https://localhost:5001/api/matching/proposals \
  -H "Authorization: Bearer $TOKEN"
```

### 3. Confirm a Match

```bash
curl -X POST https://localhost:5001/api/matching/{matchId}/confirm \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"defaultGLCode": "66300"}'
```

### 4. Check Stats

```bash
curl https://localhost:5001/api/matching/stats \
  -H "Authorization: Bearer $TOKEN"
```

## Confidence Scoring Reference

| Component | Condition | Points |
|-----------|-----------|--------|
| **Amount** | ±$0.10 | 40 |
| | ±$1.00 | 20 |
| **Date** | Same day | 35 |
| | ±1 day | 30 |
| | ±2-3 days | 25 |
| | ±4-7 days | 10 |
| **Vendor** | Exact alias | 25 |
| | Fuzzy >70% | 15 |

**Threshold**: 70 points minimum to propose match

## Common Issues

### Conflict Error (409)

```json
{
  "title": "Conflict",
  "status": 409,
  "detail": "This match was modified by another user. Please refresh and try again."
}
```

**Cause**: Another user confirmed/rejected the match
**Solution**: Refresh proposal list and retry

### No Matches Found

**Possible causes**:
1. Receipts missing extracted data (vendor/date/amount null)
2. Date range mismatch (>7 days apart)
3. Amount difference >$1.00

**Debug**: Check receipt extraction status and transaction import data

## Development Workflow

1. **Unit tests**: Run matching algorithm tests
   ```bash
   dotnet test backend/tests/ExpenseFlow.UnitTests \
     --filter "FullyQualifiedName~MatchingService"
   ```

2. **Integration tests**: Run API endpoint tests
   ```bash
   dotnet test backend/tests/ExpenseFlow.IntegrationTests \
     --filter "FullyQualifiedName~Matching"
   ```

3. **Manual testing**: Use Swagger UI at `/swagger`

## Performance Notes

- Batch auto-match: Target <60 seconds for 50 receipts
- Individual confirmation: Target <500ms
- VendorAlias lookup: Indexed for O(1) pattern matching
- Fuzzy matching: Only triggered on alias cache miss

## Next Steps

After completing this sprint:
1. Run `/speckit.tasks` to generate implementation tasks
2. Implement in priority order (P1 user stories first)
3. Update `MatchStatus` on Receipt and Transaction entities when confirming
4. Verify vendor alias learning works end-to-end
