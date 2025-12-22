# Research: API Error Resolution

**Feature**: 014-api-error-resolution
**Date**: 2025-12-22

## Executive Summary

The API 401/404 errors on the staging dashboard stem from two distinct issues:

1. **Authentication Mismatch (401)**: Frontend acquires ID tokens; backend expects access tokens
2. **Missing Endpoints (404)**: Two endpoints referenced by frontend don't exist in backend

Both issues are straightforward to resolve with existing patterns in the codebase.

---

## Research Area 1: Azure AD Token Configuration

### Problem

The frontend's `authConfig.ts` uses:
```typescript
export const apiScopes = {
  all: ['openid', 'profile', 'email'],
};
```

These are **ID token scopes**, not API access scopes. The backend's `appsettings.json` expects:
```json
"AzureAd": {
  "Audience": "api://{client-id}"
}
```

This audience expects an **access token** with the API scope, which the frontend never requests.

### Decision: Configure API Scope in Azure AD

**What**: Expose an API scope on the backend app registration and update frontend to request it.

**Rationale**:
- Microsoft Identity Platform requires explicit API scopes for access tokens
- ID tokens are for user identity; access tokens are for API authorization
- This aligns with Microsoft's recommended practices for SPA + API architecture

**Alternatives Considered**:

| Alternative | Why Rejected |
|------------|--------------|
| Accept ID tokens in backend | Security anti-pattern; ID tokens not designed for API authorization |
| Use client credentials flow | Not suitable for user-delegated access; would bypass user identity |
| Disable authentication on dashboard endpoints | Defeats purpose of auth; exposes user data |

### Implementation Steps

1. **Azure AD Portal** (manual step):
   - Navigate to App Registration for backend
   - Go to "Expose an API"
   - Add scope: `api://{client-id}/access_as_user`
   - Grant admin consent if required

2. **Frontend** (`authConfig.ts`):
   ```typescript
   export const apiScopes = {
     all: [`api://${import.meta.env.VITE_API_CLIENT_ID}/access_as_user`],
   };
   ```

3. **Backend** (verify `appsettings.Staging.json`):
   ```json
   "AzureAd": {
     "Instance": "https://login.microsoftonline.com/",
     "TenantId": "953922e6-5370-4a01-a3d5-773a30df726b",
     "ClientId": "00435dee-8aff-429b-bab6-762973c091c4",
     "Audience": "api://00435dee-8aff-429b-bab6-762973c091c4"
   }
   ```

---

## Research Area 2: Missing Dashboard Actions Endpoint

### Problem

Frontend calls `GET /api/dashboard/actions` but the endpoint doesn't exist.

### Decision: Add GetActions endpoint to DashboardController

**What**: New endpoint returning pending user actions (match reviews, categorization approvals).

**Rationale**:
- Follows existing pattern in DashboardController (GetMetrics, GetRecentActivity)
- Data already exists in `ReceiptTransactionMatches` table
- Consistent with spec FR-003

**Implementation Pattern** (from existing GetRecentActivity):
```csharp
[HttpGet("actions")]
public async Task<ActionResult<List<PendingActionDto>>> GetActions([FromQuery] int limit = 10)
{
    var user = await _userService.GetOrCreateUserAsync(User);

    var pendingMatches = await _dbContext.ReceiptTransactionMatches
        .Where(m => m.UserId == user.Id && m.Status == MatchProposalStatus.Proposed)
        .OrderByDescending(m => m.CreatedAt)
        .Take(limit)
        .Select(m => new PendingActionDto { ... })
        .ToListAsync();

    return Ok(pendingMatches);
}
```

---

## Research Area 3: Missing Analytics Categories Endpoint

### Problem

Frontend calls `GET /api/analytics/categories` but the endpoint doesn't exist.

### Decision: Add GetCategories endpoint to AnalyticsController

**What**: New endpoint returning expense breakdown by category for a given period.

**Rationale**:
- Follows existing pattern in AnalyticsController (GetComparison, GetCacheStatistics)
- Leverages existing categorization data in transactions
- Required by spec User Story 3

**Implementation Pattern** (following GetComparison style):
```csharp
[HttpGet("categories")]
public async Task<ActionResult<CategoryBreakdownDto>> GetCategories(
    [FromQuery] string? period,
    CancellationToken ct)
{
    var user = await _userService.GetOrCreateUserAsync(User);

    // Default to current month
    var targetPeriod = period ?? $"{DateTime.UtcNow:yyyy-MM}";

    var categories = await _dbContext.Transactions
        .Where(t => t.UserId == user.Id && t.TransactionDate.ToString("yyyy-MM") == targetPeriod)
        .GroupBy(t => t.Category ?? "Uncategorized")
        .Select(g => new CategorySpendingDto { ... })
        .ToListAsync(ct);

    return Ok(new CategoryBreakdownDto { Period = targetPeriod, Categories = categories });
}
```

---

## Dependencies

| Dependency | Version | Already Installed | Notes |
|-----------|---------|-------------------|-------|
| Microsoft.Identity.Web | 2.x | ✅ Yes | Backend auth |
| @azure/msal-browser | 3.x | ✅ Yes | Frontend auth |
| Entity Framework Core | 8.x | ✅ Yes | Database queries |

No new dependencies required.

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Azure AD scope already exists | Low | Low | Check portal before creating |
| Token refresh breaks existing flows | Low | Medium | Test all authenticated endpoints |
| Category query performance on large datasets | Low | Low | Add index on TransactionDate if needed |

---

## Conclusion

All research areas resolve to straightforward implementation tasks:
1. Azure AD configuration (portal + config file changes)
2. Two new controller endpoints following existing patterns
3. Frontend scope update (single file change)

No external libraries, architectural changes, or complex research required.
