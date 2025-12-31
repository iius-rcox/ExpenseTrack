# Quickstart: Analytics Dashboard API Endpoints

**Feature**: 019-analytics-dashboard
**Time to First Endpoint**: ~30 minutes

## Prerequisites

- .NET 8 SDK installed
- PostgreSQL database running (via Docker or Supabase)
- Backend solution builds successfully
- Sample transaction data exists for testing

## Quick Verification

```bash
# 1. Ensure you're on the feature branch
git checkout 019-analytics-dashboard

# 2. Build the solution
cd backend
dotnet build

# 3. Run existing tests to verify nothing is broken
dotnet test

# 4. Start the API locally
cd src/ExpenseFlow.Api
dotnet run

# 5. Test an existing analytics endpoint (should work)
curl -X GET "https://localhost:7001/api/analytics/comparison?currentPeriod=2025-01" \
  -H "Authorization: Bearer YOUR_DEV_TOKEN"
```

## Implementation Order

### Step 1: Create DTOs (15 min)

Create `backend/src/ExpenseFlow.Shared/DTOs/AnalyticsDtos.cs`:

```csharp
namespace ExpenseFlow.Shared.DTOs;

public record SpendingTrendItemDto
{
    public required string Date { get; init; }
    public decimal Amount { get; init; }
    public int TransactionCount { get; init; }
}

public record SpendingByCategoryItemDto
{
    public required string Category { get; init; }
    public decimal Amount { get; init; }
    public int TransactionCount { get; init; }
    public decimal PercentageOfTotal { get; init; }
}

// ... (see data-model.md for complete DTOs)
```

### Step 2: Add IAnalyticsService Interface (5 min)

Create `backend/src/ExpenseFlow.Core/Interfaces/IAnalyticsService.cs`:

```csharp
namespace ExpenseFlow.Core.Interfaces;

public interface IAnalyticsService
{
    Task<List<SpendingTrendItemDto>> GetSpendingTrendAsync(
        Guid userId, DateOnly startDate, DateOnly endDate,
        string granularity, CancellationToken ct);

    Task<List<SpendingByCategoryItemDto>> GetSpendingByCategoryAsync(
        Guid userId, DateOnly startDate, DateOnly endDate, CancellationToken ct);

    // ... additional methods
}
```

### Step 3: Implement AnalyticsService (30 min)

Create `backend/src/ExpenseFlow.Infrastructure/Services/AnalyticsService.cs`:

```csharp
public class AnalyticsService : IAnalyticsService
{
    private readonly ExpenseFlowDbContext _dbContext;

    public async Task<List<SpendingTrendItemDto>> GetSpendingTrendAsync(...)
    {
        // Use existing DeriveCategory pattern from AnalyticsController
        // Database-side aggregation with GroupBy
    }
}
```

### Step 4: Register Service (2 min)

Add to `ServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<IAnalyticsService, AnalyticsService>();
```

### Step 5: Add Controller Endpoints (15 min)

Extend `AnalyticsController.cs` with new endpoints:

```csharp
[HttpGet("spending-trend")]
public async Task<ActionResult<List<SpendingTrendItemDto>>> GetSpendingTrend(
    [FromQuery] string startDate,
    [FromQuery] string endDate,
    [FromQuery] string granularity = "day",
    CancellationToken ct = default)
{
    // Validate dates, call service, return results
}
```

### Step 6: Add Validation (10 min)

Create `AnalyticsValidators.cs`:

```csharp
public static class AnalyticsValidation
{
    public static bool ValidateDateRange(string startDate, string endDate,
        out DateOnly start, out DateOnly end, out string? error)
    {
        // Parse dates, check range <= 5 years, start <= end
    }
}
```

### Step 7: Integration Tests (20 min)

Add tests to verify:
- Each endpoint returns 200 with valid data
- 400 for invalid date ranges
- 401 without authentication
- Empty arrays for no matching data

## Verification Checklist

- [ ] `GET /api/analytics/spending-trend` returns data for valid date range
- [ ] `GET /api/analytics/spending-by-category` returns categorized breakdown
- [ ] `GET /api/analytics/spending-by-vendor` returns vendor breakdown
- [ ] `GET /api/analytics/merchants` returns top merchants with comparison
- [ ] `GET /api/analytics/subscriptions` returns subscription data
- [ ] All endpoints require authentication (401 without token)
- [ ] Date range > 5 years returns 400
- [ ] startDate > endDate returns 400
- [ ] Frontend analytics page loads without 404 errors

## Common Issues

### "No data returned"

Ensure transactions exist in the date range for the test user:

```sql
SELECT COUNT(*) FROM transactions
WHERE user_id = 'YOUR_USER_ID'
AND transaction_date BETWEEN '2025-01-01' AND '2025-01-31';
```

### "Category derivation not working"

The `DeriveCategory()` method matches uppercase patterns. Ensure transaction descriptions are being normalized.

### "Weekly aggregation off by one"

ISO weeks start on Monday. Verify the week calculation uses `CalendarWeekRule.FirstFourDayWeek`.

## Next Steps

After verification:
1. Run full test suite: `dotnet test`
2. Build Docker image: `docker buildx build --platform linux/amd64 -t iiusacr.azurecr.io/expenseflow-api:vX.Y.Z --push .`
3. Update staging deployment and verify analytics page works
