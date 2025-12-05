# Implementation Plan: Core Backend & Authentication

**Branch**: `002-core-backend-auth` | **Date**: 2025-12-04 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-core-backend-auth/spec.md`

## Summary

Implement the core .NET 8 Web API backend with Entra ID authentication, establish the 5 cache tables required for the Cost-First AI Architecture, configure Hangfire for background job processing, and set up weekly reference data synchronization from external SQL Server.

## Technical Context

**Language/Version**: .NET 8 with C# 12
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, Npgsql, Hangfire, Microsoft.Identity.Web, Polly
**Storage**: PostgreSQL 15+ (Supabase self-hosted with pgvector), Azure Blob Storage
**Testing**: xUnit, FluentAssertions, Testcontainers, Moq
**Target Platform**: Linux containers on AKS (dev-aks cluster)
**Project Type**: Web API (backend only this sprint)
**Performance Goals**: 500ms p95 for authenticated requests, 50ms for cache lookups, 500ms for vector similarity
**Constraints**: <$25/month infrastructure, Hangfire over Service Bus, Supabase over Azure PostgreSQL
**Scale/Scope**: 10-20 users, ~1,000 GL codes, ~100 departments, ~500 projects

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. Cost-First AI Architecture | ✅ PASS | Cache tables (FR-006 to FR-011) establish Tier 1/2 foundation; no AI calls in this sprint |
| II. Self-Improving System | ✅ PASS | Cache tables support learning loops; hit_count tracking (FR-011) |
| III. Receipt Accountability | N/A | Not applicable to Sprint 2 (infrastructure) |
| IV. Infrastructure Optimization | ✅ PASS | Hangfire with PostgreSQL (not Service Bus), deploying to existing AKS |
| V. Cache-First Design | ✅ PASS | All 5 cache tables created with proper indexing for O(1) lookups |

**Technology Constraints Alignment:**
- ✅ .NET 8 with ASP.NET Core Web API
- ✅ Entity Framework Core with Npgsql
- ✅ Hangfire for background job processing
- ✅ PostgreSQL 15+ with pgvector (Supabase)
- ✅ Entra ID authentication

## Project Structure

### Documentation (this feature)

```text
specs/002-core-backend-auth/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── api-spec.yaml    # OpenAPI specification
│   └── validation-tests.md
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Api/           # ASP.NET Core Web API project
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Program.cs
│   │   └── appsettings.json
│   ├── ExpenseFlow.Core/          # Domain models and interfaces
│   │   ├── Entities/
│   │   ├── Interfaces/
│   │   └── Services/
│   ├── ExpenseFlow.Infrastructure/ # EF Core, Hangfire, external services
│   │   ├── Data/
│   │   ├── Jobs/
│   │   └── Services/
│   └── ExpenseFlow.Shared/        # Shared DTOs and utilities
│       └── DTOs/
├── tests/
│   ├── ExpenseFlow.Api.Tests/
│   ├── ExpenseFlow.Core.Tests/
│   └── ExpenseFlow.Infrastructure.Tests/
├── Dockerfile
└── ExpenseFlow.sln

infrastructure/
├── kubernetes/
│   ├── deployment.yaml
│   ├── service.yaml
│   └── ingress.yaml
└── helm/
    └── expenseflow/
```

**Structure Decision**: Clean Architecture with 4 projects - Api (presentation), Core (domain), Infrastructure (data access/external services), Shared (DTOs). This follows .NET conventions and supports testability while keeping complexity minimal.

## Complexity Tracking

No constitution violations requiring justification. All choices align with stated constraints.
