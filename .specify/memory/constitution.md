# ExpenseFlow Constitution

## Core Principles

### I. Clean Architecture
All code follows the 4-layer Clean Architecture pattern:
- **Core**: Domain entities, interfaces (no external dependencies)
- **Shared**: DTOs for cross-layer data transfer
- **Infrastructure**: EF Core, external services, repositories
- **Api**: Controllers, middleware, DI configuration

Dependencies flow inward: Api → Infrastructure → Core ← Shared

### II. Test-First Development
- Unit tests for service logic
- Integration tests for API endpoints
- All tests must pass before merge to main

### III. ERP Integration (Viewpoint Vista)
Reference data (departments, projects, GL accounts) is sourced from Viewpoint Vista ERP:
- **Access Method**: Direct SQL to Vista database
- **Authentication**: SQL Authentication (credentials in Azure Key Vault)
- **Sync Frequency**: Daily overnight sync via Hangfire job
- **Failure Handling**: Alert ops team immediately, continue with stale cache
- See: [Vista Integration Reference](./vista-integration.md)

### IV. API Design
- RESTful endpoints under `/api/` prefix
- ProblemDetails (RFC 7807) for error responses
- FluentValidation for request validation
- OpenAPI/Swagger documentation auto-generated

### V. Observability
- Serilog structured logging
- Log levels: Debug for dev, Information for production
- Named parameters in log templates (no string interpolation)

### VI. Security
- Azure AD / Entra ID for authentication
- JWT bearer tokens
- [Authorize] attribute on all controllers except health checks

## Technology Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 8 with C# 12 |
| API Framework | ASP.NET Core Web API |
| ORM | Entity Framework Core 8 |
| Database | PostgreSQL 15+ (Supabase) |
| ERP Source | Viewpoint Vista (SQL Server) |
| Background Jobs | Hangfire |
| Container | Docker (linux/amd64 for AKS) |
| Orchestration | Azure Kubernetes Service |

## Deployment

- **Registry**: Azure Container Registry (iiusacr.azurecr.io)
- **GitOps**: ArgoCD syncs from main branch
- **Build Requirement**: `docker buildx build --platform linux/amd64` (Apple Silicon dev machines)

## Governance

- Constitution supersedes feature-specific decisions
- Amendments require documentation update + PR review
- All PRs must verify compliance with these principles

**Version**: 1.0.0 | **Ratified**: 2025-12-23 | **Last Amended**: 2025-12-23
