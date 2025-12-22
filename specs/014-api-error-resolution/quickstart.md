# Quickstart: API Error Resolution

**Feature**: 014-api-error-resolution
**Date**: 2025-12-22

## Prerequisites

- Azure AD access to the ExpenseFlow app registration
- Access to the `dev-aks` Kubernetes cluster
- Local development environment set up for backend and frontend

## Step 1: Configure Azure AD API Scope (One-time Setup)

1. Go to [Azure Portal](https://portal.azure.com) → Azure Active Directory → App registrations
2. Select the ExpenseFlow backend app registration (`00435dee-8aff-429b-bab6-762973c091c4`)
3. Navigate to **Expose an API**
4. If not set, configure Application ID URI: `api://00435dee-8aff-429b-bab6-762973c091c4`
5. Click **Add a scope**:
   - Scope name: `access_as_user`
   - Admin consent display name: "Access ExpenseFlow API"
   - Admin consent description: "Allows the app to access ExpenseFlow API on behalf of the signed-in user"
   - State: Enabled
6. Under **Authorized client applications**, add the frontend client ID if different

## Step 2: Update Backend Configuration

Verify `appsettings.Staging.json` (or create if missing):

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "953922e6-5370-4a01-a3d5-773a30df726b",
    "ClientId": "00435dee-8aff-429b-bab6-762973c091c4",
    "Audience": "api://00435dee-8aff-429b-bab6-762973c091c4"
  }
}
```

## Step 3: Update Frontend Auth Configuration

Edit `frontend/src/auth/authConfig.ts`:

```typescript
// Before
export const apiScopes = {
  all: ['openid', 'profile', 'email'],
};

// After
const API_CLIENT_ID = import.meta.env.VITE_API_CLIENT_ID || '00435dee-8aff-429b-bab6-762973c091c4';

export const apiScopes = {
  all: [`api://${API_CLIENT_ID}/access_as_user`],
};

// Also update loginRequest to include API scope
export const loginRequest = {
  scopes: ['openid', 'profile', 'email', `api://${API_CLIENT_ID}/access_as_user`],
};
```

Add to `frontend/.env.staging`:
```
VITE_API_CLIENT_ID=00435dee-8aff-429b-bab6-762973c091c4
```

## Step 4: Add Missing DTOs

Create `backend/src/ExpenseFlow.Shared/DTOs/PendingActionDto.cs`:

```csharp
namespace ExpenseFlow.Shared.DTOs;

public record PendingActionDto
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required DateTime CreatedAt { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}
```

Create `backend/src/ExpenseFlow.Shared/DTOs/CategoryBreakdownDto.cs`:

```csharp
namespace ExpenseFlow.Shared.DTOs;

public record CategoryBreakdownDto
{
    public required string Period { get; init; }
    public required decimal TotalSpending { get; init; }
    public required int TransactionCount { get; init; }
    public required List<CategorySpendingDto> Categories { get; init; }
}

public record CategorySpendingDto
{
    public required string Category { get; init; }
    public required decimal Amount { get; init; }
    public required decimal Percentage { get; init; }
    public required int TransactionCount { get; init; }
}
```

## Step 5: Add Dashboard Actions Endpoint

Add to `backend/src/ExpenseFlow.Api/Controllers/DashboardController.cs`:

```csharp
[HttpGet("actions")]
[ProducesResponseType(typeof(List<PendingActionDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<List<PendingActionDto>>> GetActions([FromQuery] int limit = 10)
{
    if (limit < 1) limit = 10;
    if (limit > 50) limit = 50;

    var user = await _userService.GetOrCreateUserAsync(User);

    _logger.LogInformation("Pending actions requested by user {UserId}, limit {Limit}", user.Id, limit);

    var pendingMatches = await _dbContext.ReceiptTransactionMatches
        .Where(m => m.UserId == user.Id && m.Status == MatchProposalStatus.Proposed)
        .OrderByDescending(m => m.CreatedAt)
        .Take(limit)
        .Include(m => m.Receipt)
        .Include(m => m.Transaction)
        .Select(m => new PendingActionDto
        {
            Id = $"match_{m.Id}",
            Type = "match_review",
            Title = "Review match",
            Description = $"{m.Receipt.VendorExtracted ?? "Receipt"} matches transaction for ${m.Transaction.Amount:N2}",
            CreatedAt = m.CreatedAt,
            Metadata = new Dictionary<string, object>
            {
                ["confidenceScore"] = m.ConfidenceScore,
                ["matchId"] = m.Id
            }
        })
        .ToListAsync();

    return Ok(pendingMatches);
}
```

## Step 6: Add Analytics Categories Endpoint

Add to `backend/src/ExpenseFlow.Api/Controllers/AnalyticsController.cs`:

```csharp
[HttpGet("categories")]
[ProducesResponseType(typeof(CategoryBreakdownDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<CategoryBreakdownDto>> GetCategories(
    [FromQuery] string? period,
    CancellationToken ct)
{
    // Default to current month
    if (string.IsNullOrWhiteSpace(period))
    {
        period = $"{DateTime.UtcNow:yyyy-MM}";
    }
    else if (!IsValidPeriod(period))
    {
        return BadRequest(new ProblemDetailsResponse
        {
            Title = "Validation Error",
            Detail = "Invalid period format. Expected YYYY-MM (e.g., 2025-12)."
        });
    }

    var user = await _userService.GetOrCreateUserAsync(User);

    _logger.LogInformation("Category breakdown requested by user {UserId} for period {Period}", user.Id, period);

    var parts = period.Split('-');
    var year = int.Parse(parts[0]);
    var month = int.Parse(parts[1]);
    var periodStart = new DateOnly(year, month, 1);
    var periodEnd = periodStart.AddMonths(1);

    var transactions = await _dbContext.Transactions
        .Where(t => t.UserId == user.Id &&
                   t.TransactionDate >= periodStart &&
                   t.TransactionDate < periodEnd &&
                   t.Amount > 0)
        .GroupBy(t => t.Category ?? "Uncategorized")
        .Select(g => new
        {
            Category = g.Key,
            Amount = g.Sum(t => t.Amount),
            Count = g.Count()
        })
        .ToListAsync(ct);

    var totalSpending = transactions.Sum(t => t.Amount);
    var totalCount = transactions.Sum(t => t.Count);

    var categories = transactions
        .OrderByDescending(t => t.Amount)
        .Select(t => new CategorySpendingDto
        {
            Category = t.Category,
            Amount = t.Amount,
            Percentage = totalSpending > 0 ? Math.Round(t.Amount / totalSpending * 100, 1) : 0,
            TransactionCount = t.Count
        })
        .ToList();

    return Ok(new CategoryBreakdownDto
    {
        Period = period,
        TotalSpending = totalSpending,
        TransactionCount = totalCount,
        Categories = categories
    });
}
```

## Step 7: Build and Deploy

```bash
# Build backend
cd backend
dotnet build

# Run tests
dotnet test

# Build frontend
cd ../frontend
npm run build

# Build and push Docker images
docker buildx build --platform linux/amd64 -t iiusacr.azurecr.io/expenseflow-api:v2.8.0-$(git rev-parse --short HEAD) --push .
docker buildx build --platform linux/amd64 -t iiusacr.azurecr.io/expenseflow-frontend:v2.8.0-$(git rev-parse --short HEAD) --push .

# Update Kubernetes manifests with new image tags
# ... edit infrastructure/kubernetes/staging/*.yaml

# Commit and push
git add .
git commit -m "feat(api): Add dashboard/actions and analytics/categories endpoints"
git push origin 014-api-error-resolution
```

## Step 8: Verify

1. Open staging site: https://staging.expense.ii-us.com
2. Login with Azure AD
3. Verify dashboard loads without 401/404 errors
4. Check browser DevTools console for any remaining errors
5. Verify metric cards display data or appropriate empty states

## Rollback

If issues occur:

```bash
# Revert to previous image version
kubectl set image deployment/expenseflow-api api=iiusacr.azurecr.io/expenseflow-api:v2.7.2-a3b6f62 -n expenseflow-staging
kubectl set image deployment/expenseflow-frontend frontend=iiusacr.azurecr.io/expenseflow-frontend:v2.7.2-a3b6f62 -n expenseflow-staging
```
