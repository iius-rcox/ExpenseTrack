# Quickstart: Testing & Cache Warming

**Feature Branch**: `010-testing-cache-warming`
**Date**: 2025-12-17

## Prerequisites

Before starting implementation, ensure:

1. **Sprints 1-9 Complete**: All prior features implemented and tested
2. **Staging Namespace Exists**: `kubectl get ns expenseflow-staging` returns namespace
3. **Historical Data Available**: Excel export from legacy expense system (6 months)
4. **Test Users Identified**: 3-5 users committed to UAT participation
5. **Development Environment**: .NET 8 SDK, Docker, kubectl configured

## Quick Setup

### 1. Create Feature Branch (Already Done)

```bash
git checkout 010-testing-cache-warming
```

### 2. Add NBomber Package for Load Testing

```bash
cd backend
dotnet new classlib -n ExpenseFlow.LoadTests -o tests/ExpenseFlow.LoadTests
dotnet sln add tests/ExpenseFlow.LoadTests/ExpenseFlow.LoadTests.csproj
cd tests/ExpenseFlow.LoadTests
dotnet add package NBomber
dotnet add package NBomber.Http
dotnet add reference ../../src/ExpenseFlow.Shared/ExpenseFlow.Shared.csproj
```

### 3. Create ImportJob Entity

Add to `backend/src/ExpenseFlow.Core/Entities/ImportJob.cs`:

```csharp
public class ImportJob : BaseEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SourceFileName { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public ImportJobStatus Status { get; set; }
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int CachedDescriptions { get; set; }
    public int CreatedAliases { get; set; }
    public int GeneratedEmbeddings { get; set; }
    public int SkippedRecords { get; set; }
    public string? ErrorLog { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public User User { get; set; } = null!;
}

public enum ImportJobStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
```

### 4. Add EF Migration

```bash
cd backend/src/ExpenseFlow.Infrastructure
dotnet ef migrations add AddImportJobTable -s ../ExpenseFlow.Api
```

### 5. Deploy to Staging

```bash
# Apply staging namespace (already exists)
kubectl apply -f infrastructure/namespaces/expenseflow-staging.yaml

# Deploy Supabase to staging (use existing Helm values with staging overrides)
helm install supabase-staging supabase/supabase \
  -n expenseflow-staging \
  -f infrastructure/supabase/values.yaml \
  --set global.postgresql.auth.password=$STAGING_DB_PASSWORD

# Deploy API to staging
kubectl apply -f infrastructure/kubernetes/staging/
```

## Implementation Phases

### Phase 1: Cache Warming Service (Days 1-3)

1. **Create ICacheWarmingService interface**
   - `ImportHistoricalDataAsync(Stream file, Guid userId)`
   - `GetImportJobAsync(Guid jobId)`
   - `CancelImportJobAsync(Guid jobId)`

2. **Implement CacheWarmingService**
   - Excel parsing with ClosedXML
   - Chunked processing (100 records per batch)
   - Embedding generation via IEmbeddingService
   - Progress tracking via ImportJob entity

3. **Create CacheWarmingController**
   - POST `/api/cache-warming/import`
   - GET `/api/cache-warming/jobs`
   - GET `/api/cache-warming/jobs/{id}`
   - DELETE `/api/cache-warming/jobs/{id}`

4. **Create CacheWarmingJob (Hangfire)**
   - Background job for async processing
   - Progress updates
   - Error handling with retry

### Phase 2: Staging Environment (Days 4-5)

1. **Create staging Kubernetes manifests**
   - Deployment with staging config
   - ConfigMap with staging values
   - Secret references from Key Vault

2. **Deploy and validate**
   - Health checks pass
   - Authentication works
   - Basic operations functional

### Phase 3: UAT Test Plan (Days 6-8)

1. **Create test case documents**
   - TC-001 through TC-007 per spec
   - Document in `/docs/uat/test-cases/`

2. **Execute UAT with test users**
   - Track results in test case documents
   - Log defects as GitHub Issues

3. **Fix P1/P2 defects**
   - Prioritize critical path issues
   - Re-test after fixes

### Phase 4: Performance Testing (Days 9-10)

1. **Implement NBomber scenarios**
   - Batch receipt processing (50 receipts)
   - Concurrent user simulation (20 users)

2. **Execute load tests**
   - Run against staging
   - Collect percentile data

3. **Optimize slow queries**
   - Enable pg_stat_statements
   - Identify and fix queries >500ms

## Key Files to Create

| File | Purpose |
|------|---------|
| `ExpenseFlow.Core/Entities/ImportJob.cs` | Import job entity |
| `ExpenseFlow.Core/Interfaces/ICacheWarmingService.cs` | Service interface |
| `ExpenseFlow.Infrastructure/Services/CacheWarmingService.cs` | Implementation |
| `ExpenseFlow.Infrastructure/Jobs/CacheWarmingJob.cs` | Hangfire job |
| `ExpenseFlow.Api/Controllers/CacheWarmingController.cs` | API endpoints |
| `ExpenseFlow.Shared/DTOs/CacheWarmingDtos.cs` | Request/response DTOs |
| `tests/ExpenseFlow.LoadTests/` | Load test project |
| `docs/uat/test-plan.md` | UAT master document |
| `docs/uat/test-cases/*.md` | Individual test cases |
| `infrastructure/kubernetes/staging/*.yaml` | Staging K8s manifests |

## Verification Commands

```bash
# Build and test
cd backend
dotnet build
dotnet test

# Check staging deployment
kubectl get pods -n expenseflow-staging
kubectl logs -n expenseflow-staging deployment/api

# Run load tests
cd tests/ExpenseFlow.LoadTests
dotnet run -- --target https://staging.expenseflow.example.com

# Check cache statistics after warming
curl -H "Authorization: Bearer $TOKEN" \
  https://staging.expenseflow.example.com/api/cache/statistics/warming-summary
```

## Success Criteria Checklist

- [ ] Cache warming imports 6 months of historical data
- [ ] >500 descriptions cached
- [ ] >100 vendor aliases created
- [ ] >500 embeddings generated
- [ ] <1% import error rate
- [ ] Staging environment deployed and accessible
- [ ] All 7 UAT test cases executed
- [ ] All P1/P2 defects resolved
- [ ] 3+ users sign off on UAT
- [ ] 50 receipts processed in <5 minutes
- [ ] 95th percentile <2s with 20 users
- [ ] All queries <500ms
