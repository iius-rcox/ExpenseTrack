# Implementation Plan: API Error Resolution

**Branch**: `014-api-error-resolution` | **Date**: 2025-12-22 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/014-api-error-resolution/spec.md`

## Summary

Resolve API 401 (Unauthorized) and 404 (Not Found) errors affecting the staging dashboard. The root causes are:

1. **401 Errors**: Frontend uses ID tokens (`openid`, `profile`, `email` scopes) while backend expects access tokens with `api://{client-id}` audience
2. **404 Errors**: Two endpoints called by frontend (`/api/dashboard/actions`, `/api/analytics/categories`) are not implemented in the backend

The fix requires:
- Configuring Azure AD to expose an API scope for the backend
- Updating frontend to acquire proper access tokens
- Implementing the missing dashboard/actions and analytics/categories endpoints

## Technical Context

**Language/Version**:
- Backend: .NET 8 with C# 12
- Frontend: TypeScript 5.7+ with React 18.3+

**Primary Dependencies**:
- Backend: ASP.NET Core Web API, Microsoft.Identity.Web, Entity Framework Core 8
- Frontend: @azure/msal-browser, TanStack Query, TanStack Router

**Storage**: PostgreSQL 15+ (Supabase self-hosted with pgvector)

**Testing**:
- Backend: xUnit with CustomWebApplicationFactory
- Frontend: Manual browser testing, future: Playwright

**Target Platform**: Azure Kubernetes Service (`dev-aks`), staging environment

**Project Type**: Web application (backend + frontend)

**Performance Goals**: Dashboard loads within 3 seconds (SC-003)

**Constraints**: Zero 401/404 errors for valid sessions (SC-001, SC-002)

**Scale/Scope**: Single-user testing initially, multi-tenant production later

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| Entra ID for auth | ✅ Pass | Using Microsoft.Identity.Web with Azure AD |
| Azure Blob for storage | ✅ Pass | No changes to storage layer |
| Supabase PostgreSQL | ✅ Pass | Existing database, no schema changes |
| Test coverage | ✅ Pass | Will add unit tests for new endpoints |
| No "latest" image tags | ✅ Pass | All images use semantic versioning |

**Gate Result**: PASS - No constitution violations

## Project Structure

### Documentation (this feature)

```text
specs/014-api-error-resolution/
├── plan.md              # This file
├── research.md          # Phase 0 output - auth configuration research
├── data-model.md        # Phase 1 output - DTO definitions
├── quickstart.md        # Phase 1 output - deployment steps
├── contracts/           # Phase 1 output - API contracts
│   ├── dashboard-actions.yaml
│   └── analytics-categories.yaml
└── tasks.md             # Phase 2 output (from /speckit.tasks)
```

### Source Code (repository root)

```text
backend/
├── src/ExpenseFlow.Api/
│   ├── Controllers/
│   │   ├── DashboardController.cs    # Add GetActions endpoint
│   │   └── AnalyticsController.cs    # Add GetCategories endpoint
│   └── appsettings.*.json            # Verify AzureAd configuration
└── tests/ExpenseFlow.Api.Tests/
    └── Controllers/
        ├── DashboardControllerTests.cs
        └── AnalyticsControllerTests.cs

frontend/
├── src/
│   ├── auth/
│   │   └── authConfig.ts             # Update API scopes
│   └── services/
│       └── api.ts                    # Token handling (no changes expected)
└── [no new test files this sprint]

infrastructure/kubernetes/staging/
└── [no changes expected]
```

**Structure Decision**: Web application structure with existing backend and frontend directories. No new projects needed.

## Complexity Tracking

> No Constitution Check violations - this section not required.

## Phase 0: Research Summary

### Research Tasks Identified

1. **Azure AD API Scope Configuration**: How to expose an API scope on the backend app registration
2. **MSAL Token Acquisition**: Frontend changes to request access tokens instead of ID tokens
3. **Missing Endpoint Patterns**: Review existing endpoints for implementation patterns

### Research Approach

Since the existing codebase already has:
- Working Azure AD integration (just needs scope alignment)
- Established controller patterns (DashboardController, AnalyticsController exist)
- Entity Framework queries for user-scoped data

No external research required. Implementation will follow existing patterns.

## Phase 1: Design Summary

### Authentication Fix

**Root Cause**: Frontend requests ID tokens, backend expects access tokens.

**Solution**:
1. In Azure AD portal: Expose an API scope on the backend app registration (e.g., `api://{client-id}/access_as_user`)
2. Update frontend `authConfig.ts` to include the API scope in `apiScopes.all`
3. Verify backend `appsettings.json` has correct `Audience` value

### Missing Endpoints

**`GET /api/dashboard/actions`** - Pending user actions (match reviews, categorization approvals)

Returns items from:
- `ReceiptTransactionMatches` where `Status = Proposed`
- `Categorization` suggestions pending review

**`GET /api/analytics/categories`** - Expense breakdown by category

Returns aggregated spending per category for a given period (default: current month).

### DTOs to Add

```csharp
// Dashboard actions
public record PendingActionDto
{
    public required string Id { get; init; }
    public required string Type { get; init; }  // "match_review" | "categorization"
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required DateTime CreatedAt { get; init; }
}

// Category breakdown
public record CategoryBreakdownDto
{
    public required string Period { get; init; }
    public required decimal TotalSpending { get; init; }
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

## Artifacts Generated

| Artifact | Description |
|----------|-------------|
| `research.md` | Azure AD scope configuration details |
| `data-model.md` | DTO definitions for new endpoints |
| `contracts/dashboard-actions.yaml` | OpenAPI spec for actions endpoint |
| `contracts/analytics-categories.yaml` | OpenAPI spec for categories endpoint |
| `quickstart.md` | Step-by-step deployment guide |

## Next Steps

Run `/speckit.tasks` to generate the implementation task breakdown.
