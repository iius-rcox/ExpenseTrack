# Implementation Plan: Backend API User Preferences

**Branch**: `016-user-preferences-api` | **Date**: 2025-12-23 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/016-user-preferences-api/spec.md`

## Summary

Implement backend API endpoints for user preferences management to support the frontend's theme persistence, default department/project selection, and profile display. The feature extends the existing `User` entity with a new `UserPreferences` entity following the established Clean Architecture pattern.

## Technical Context

**Language/Version**: .NET 8 with C# 12
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, Microsoft.Identity.Web, FluentValidation
**Storage**: PostgreSQL 15+ (Supabase self-hosted)
**Testing**: xUnit, Moq, Microsoft.AspNetCore.Mvc.Testing (integration tests)
**Target Platform**: Linux container on Azure Kubernetes Service (AKS)
**Project Type**: Web application (backend API extending existing codebase)
**Performance Goals**: <200ms p95 for preference read/write operations
**Constraints**: Must integrate with existing UsersController, cannot break existing /users/me endpoint
**Scale/Scope**: Single user preferences per authenticated user (~1000 active users)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The project constitution is a template placeholder. Applying general best practices:

| Gate | Status | Notes |
|------|--------|-------|
| Test-First Development | ✅ Pass | Unit tests for service, integration tests for API |
| Clean Architecture | ✅ Pass | Follows existing 4-project structure |
| API Versioning | ✅ Pass | Uses existing `/api/` prefix |
| Logging/Observability | ✅ Pass | Serilog already configured |
| Input Validation | ✅ Pass | FluentValidation for DTOs |

## Project Structure

### Documentation (this feature)

```text
specs/016-user-preferences-api/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── user-preferences-api.yaml  # OpenAPI spec
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Api/
│   │   └── Controllers/
│   │       └── UsersController.cs          # Extended with preferences endpoints
│   ├── ExpenseFlow.Core/
│   │   ├── Entities/
│   │   │   ├── User.cs                     # Add navigation to UserPreferences
│   │   │   └── UserPreferences.cs          # NEW
│   │   └── Interfaces/
│   │       └── IUserPreferencesService.cs  # NEW
│   ├── ExpenseFlow.Infrastructure/
│   │   ├── Data/
│   │   │   ├── ExpenseFlowDbContext.cs     # Add DbSet<UserPreferences>
│   │   │   └── Configurations/
│   │   │       └── UserPreferencesConfiguration.cs  # NEW (EF config)
│   │   └── Services/
│   │       └── UserPreferencesService.cs   # NEW
│   └── ExpenseFlow.Shared/
│       └── DTOs/
│           ├── UserPreferencesResponse.cs  # NEW
│           └── UpdatePreferencesRequest.cs # NEW
└── tests/
    └── ExpenseFlow.Api.Tests/
        ├── Controllers/
        │   └── UsersControllerPreferencesTests.cs  # NEW
        └── Services/
            └── UserPreferencesServiceTests.cs      # NEW
```

**Structure Decision**: Extends existing Clean Architecture layout. New files follow established naming conventions and placement patterns.

## Complexity Tracking

No constitution violations requiring justification. Design follows existing patterns with minimal additions:
- 1 new entity (UserPreferences)
- 1 new service interface + implementation
- 2 new DTOs
- 3 new API endpoints on existing controller
