# Research: Core Backend & Authentication

**Feature**: 002-core-backend-auth
**Date**: 2025-12-04

## Technology Decisions

### 1. Entra ID Authentication with ASP.NET Core

**Decision**: Use Microsoft.Identity.Web for JWT bearer token authentication

**Rationale**:
- First-party Microsoft library for Azure AD/Entra ID integration
- Handles token validation, claims mapping, and refresh automatically
- Supports both user tokens (delegated) and app tokens (application permissions)
- Well-documented integration with ASP.NET Core authentication middleware

**Alternatives Considered**:
- Manual JWT validation with System.IdentityModel.Tokens.Jwt - More control but significant boilerplate
- IdentityServer - Overkill for single-tenant enterprise app with existing IdP
- Azure AD B2C - Designed for consumer scenarios, not enterprise SSO

**Implementation Notes**:
- Configure in Program.cs with `AddMicrosoftIdentityWebApi()`
- App registration required in Azure portal (or via Terraform)
- Scopes: `api://{client-id}/access_as_user` for delegated access
- Use `[Authorize]` attribute on controllers, `[AllowAnonymous]` for health endpoints

### 2. Entity Framework Core with PostgreSQL

**Decision**: Use EF Core 8 with Npgsql provider and code-first migrations

**Rationale**:
- Constitution specifies EF Core with Npgsql
- Code-first enables version-controlled schema changes
- Npgsql has native pgvector support via `Npgsql.EntityFrameworkCore.PostgreSQL.Pgvector`
- Strong LINQ support reduces raw SQL needs

**Alternatives Considered**:
- Dapper - Faster but more boilerplate, no change tracking
- Raw ADO.NET - Maximum performance but significant development overhead
- Supabase client SDK - JavaScript-focused, not ideal for .NET backend

**Implementation Notes**:
- Use `Vector` type from Npgsql for embedding columns
- Create IVFFlat index on embedding columns for similarity search
- Connection string from Azure Key Vault via SecretClient
- Use `HasIndex()` fluent API for unique constraints on hash columns

### 3. Hangfire for Background Jobs

**Decision**: Hangfire with PostgreSQL storage

**Rationale**:
- Constitution mandates Hangfire over Azure Service Bus ($10/month saved)
- PostgreSQL storage uses existing database (no additional infrastructure)
- Built-in dashboard for job monitoring (FR-013)
- Supports recurring jobs (FR-014), retries (FR-015), and persistence (FR-016)

**Alternatives Considered**:
- Azure Service Bus + Azure Functions - More scalable but adds $10/month cost
- Quartz.NET - More complex configuration, less intuitive dashboard
- Background services with IHostedService - No persistence or dashboard

**Implementation Notes**:
- Install `Hangfire.AspNetCore` and `Hangfire.PostgreSql`
- Dashboard accessible at `/hangfire` with admin authorization
- Use `[AutomaticRetry(Attempts = 3)]` attribute for retry policy
- Recurring jobs configured via `RecurringJob.AddOrUpdate()`

### 4. Managed Identity for SQL Server Access

**Decision**: Use Azure Managed Identity for passwordless SQL Server authentication

**Rationale**:
- Clarification session specified managed identity (passwordless)
- Eliminates credential management and rotation
- AKS Workload Identity already enabled on cluster
- More secure than connection string with password

**Alternatives Considered**:
- SQL authentication with password in Key Vault - Works but credentials to manage
- Windows Integrated Authentication - Not applicable in Linux containers

**Implementation Notes**:
- Use `Azure.Identity.DefaultAzureCredential` for token acquisition
- SQL connection string: `Server=...;Database=...;Authentication=Active Directory Default`
- Grant managed identity `db_datareader` role on source database
- Use Polly for retry on transient failures

### 5. pgvector for Embedding Storage

**Decision**: Use pgvector extension with IVFFlat indexing

**Rationale**:
- Already enabled in Supabase (Sprint 1)
- Supports 1536-dimension embeddings from text-embedding-3-small
- IVFFlat index provides O(sqrt(n)) query performance
- Native PostgreSQL integration, no separate vector database needed

**Alternatives Considered**:
- Pinecone/Weaviate - External service, adds cost and latency
- HNSW index - Better recall but slower index builds
- No index (brute force) - O(n) not acceptable for 10k+ embeddings

**Implementation Notes**:
- Column type: `VECTOR(1536)`
- Index: `CREATE INDEX ... USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100)`
- Query: `ORDER BY embedding <=> $1 LIMIT 5` for top-5 similar
- Tune `lists` parameter based on data size (sqrt(n) rule)

### 6. Clean Architecture Project Structure

**Decision**: 4-project solution (Api, Core, Infrastructure, Shared)

**Rationale**:
- Follows .NET conventions and Microsoft guidance
- Clear dependency direction: Api → Core ← Infrastructure
- Shared project for DTOs prevents circular dependencies
- Supports unit testing of Core without infrastructure dependencies

**Alternatives Considered**:
- Single project - Too simple, becomes messy as features grow
- Vertical slices - Good for large teams but overhead for small scope
- Microservices - Massive overkill for 10-20 user app

**Implementation Notes**:
- Core: Entities, interfaces, domain services (no dependencies)
- Infrastructure: EF DbContext, repositories, external service clients
- Api: Controllers, middleware, Program.cs configuration
- Shared: DTOs, validation attributes, common utilities

## Best Practices Applied

### Authentication
- Use policy-based authorization for role checks
- Implement custom ClaimsPrincipal extension for common claims
- Log authentication failures with correlation ID
- Cache user profiles to avoid repeated DB lookups

### Database
- Use async/await throughout for I/O operations
- Implement repository pattern for testability
- Use `AsNoTracking()` for read-only queries
- Apply database indexes for all query predicates

### Background Jobs
- Use fire-and-forget pattern for non-critical jobs
- Implement idempotency for retryable jobs
- Log job start/end with execution time
- Use dependency injection in job classes

### Error Handling
- Use Problem Details (RFC 7807) for error responses
- Implement global exception middleware
- Return specific error codes for client handling
- Log exceptions with full stack traces internally

## Dependencies Summary

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.Identity.Web | 2.x | Entra ID authentication |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.x | PostgreSQL EF Core provider |
| Npgsql.EntityFrameworkCore.PostgreSQL.Pgvector | 8.x | pgvector support |
| Hangfire.AspNetCore | 1.8.x | Background job processing |
| Hangfire.PostgreSql | 1.20.x | PostgreSQL storage for Hangfire |
| Polly | 8.x | Resilience patterns |
| Azure.Identity | 1.x | Managed Identity authentication |
| Microsoft.Data.SqlClient | 5.x | SQL Server connectivity |
| Swashbuckle.AspNetCore | 6.x | OpenAPI/Swagger documentation |
