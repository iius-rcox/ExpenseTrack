# ExpenseFlow Development Guidelines

Auto-generated from feature plans. Last updated: 2025-12-17

## Tool Usage
### MCP Servers
- **ref.tools**: Use this to reference best practices when developing any feature
- **shad.cn**: Use this for all frontend components and design choices

## Project References
- **Constitution**: `.specify/memory/constitution.md` - Core principles and governance
- **Vista Integration**: `.specify/memory/vista-integration.md` - Viewpoint Vista ERP integration patterns

## Viewpoint Vista ERP Integration

Reference data (departments, projects, GL accounts) is synced from Viewpoint Vista:

| Setting | Value |
|---------|-------|
| Source Tables | PRDP (departments), JCCM (jobs/contracts) |
| Company Filter | JCCo = 1 / PRCo = 1 |
| Authentication | SQL Authentication (Key Vault) |
| Sync Frequency | Daily overnight |
| Status Filter | Active records only |
| Display Format | First 25 chars of name + (Code) |

**Key Rules**:
- Validate department/project IDs against local PostgreSQL cache (not Vista directly)
- Clear user preferences automatically when referenced records become inactive
- On sync failure: Alert ops immediately, continue with stale cache

## Active Technologies
- .NET 8 with C# 12 + ASP.NET Core Web API, Entity Framework Core 8, Npgsql, Hangfire, Microsoft.Identity.Web, Polly (002-core-backend-auth)
- PostgreSQL 15+ (Supabase self-hosted with pgvector), Azure Blob Storage (002-core-backend-auth)
- .NET 8 with C# 12 + ASP.NET Core Web API + Entity Framework Core 8, Npgsql, Hangfire, Azure.AI.FormRecognizer, Azure.Storage.Blobs, SkiaSharp (003-receipt-pipeline)
- PostgreSQL 15+ (Supabase self-hosted), Azure Blob Storage (ccproctemp2025) (003-receipt-pipeline)
- .NET 8 with C# 12 + ASP.NET Core Web API, Entity Framework Core 8, Npgsql, CsvHelper, ClosedXML (Excel), Semantic Kernel, Azure.AI.OpenAI (004-statement-fingerprinting)
- PostgreSQL 15+ with pgvector (Supabase self-hosted), Azure Blob Storage (ccproctemp2025) (004-statement-fingerprinting)
- .NET 8 with C# 12 + ASP.NET Core Web API, Entity Framework Core 8, Npgsql, Hangfire, F23.StringSimilarity (Levenshtein) (005-matching-engine)
- PostgreSQL 15+ (Supabase self-hosted with pgvector), Azure Blob Storage (ccproctemp2025) (005-matching-engine)
- .NET 8 with C# 12, ASP.NET Core Web API, Entity Framework Core 8, Npgsql, Hangfire, Microsoft.SemanticKernel 1.25.0, Microsoft.SemanticKernel.Connectors.AzureOpenAI, Microsoft.Extensions.Resilience (Polly v8) (006-ai-categorization)
- PostgreSQL 15+ with pgvector (Supabase self-hosted) for embedding similarity search (006-ai-categorization)
- .NET 8 with C# 12 + ASP.NET Core Web API, Entity Framework Core 8, Npgsql, Hangfire (recurring jobs for subscription alerts), System.Text.Json (JSON config storage) (007-advanced-features)
- PostgreSQL 15+ with pgvector (Supabase self-hosted), Tier 1 rule-based detection (007-advanced-features)
- .NET 8 with C# 12 (ASP.NET Core Web API) + Entity Framework Core 8, Npgsql, Semantic Kernel, Hangfire (008-draft-report-generation)
- .NET 8 with C# 12 (ASP.NET Core Web API) + Entity Framework Core 8, Npgsql, ClosedXML (Excel), PdfSharpCore (PDF), SixLabors.ImageSharp (image conversion) (009-output-analytics)
- .NET 8 with C# 12 + ASP.NET Core Web API, Entity Framework Core 8, Npgsql, Hangfire, Semantic Kernel (for embedding generation), NBomber or k6 (for load testing) (010-testing-cache-warming)
- TypeScript 5.7+ with React 18.3+ (011-unified-frontend)
- N/A (frontend consumes existing backend APIs) (011-unified-frontend)
- .NET 8 with C# 12 (cleanup endpoint), JSON (expected values file) + ExpenseFlow.Api (existing), test-data folder, staging API (012-automated-uat-testing)
- `test-data/receipts/` (19 images), `test-data/statements/chase.csv`, `test-data/expected-values.json` (new) (012-automated-uat-testing)
- TypeScript 5.7+ with React 18.3+ + TanStack Router, TanStack Query, Tailwind CSS 4.x, shadcn/ui, Framer Motion (new), Recharts (013-frontend-redesign)
- TypeScript 5.7+ with React 18.3+ + Tailwind CSS 4.x, shadcn/ui (Radix primitives), next-themes, Framer Motion, class-variance-authority (015-dual-theme-system)
- localStorage (theme preference via next-themes) (015-dual-theme-system)
- .NET 8 with C# 12 + ASP.NET Core Web API, Entity Framework Core 8, Microsoft.Identity.Web, FluentValidation (016-user-preferences-api)
- Markdown (GitHub Flavored Markdown) + None (documentation only) (017-how-to-guide)
- Repository files in `docs/user-guide/` directory (018-end-user-guide)
- .NET 8 with C# 12 + ASP.NET Core Web API, Entity Framework Core 8, Npgsql, FluentValidation (019-analytics-dashboard)
- PostgreSQL 15+ (Supabase self-hosted), existing Transaction/VendorAlias tables (019-analytics-dashboard)
- TypeScript 5.7+ with React 18.3+ + Vitest 4.x, Playwright 1.57+, MSW (Mock Service Worker), TanStack Query, TanStack Router (022-frontend-integration-tests)
- N/A (testing infrastructure only) (022-frontend-integration-tests)
- .NET 8 with C# 12 (backend), TypeScript 5.7+ with React 18.3+ (frontend) + ASP.NET Core Web API, Entity Framework Core 8, TanStack Query, shadcn/ui (023-expense-prediction)
- .NET 8 with C# 12 (backend), TypeScript 5.7+ with React 18.3+ (frontend) + ASP.NET Core Web API, Entity Framework Core 8, TanStack Query, shadcn/ui (024-extraction-editor-training)
- PostgreSQL 15+ (Supabase) (024-extraction-editor-training)
- .NET 8 with C# 12 + ASP.NET Core Web API, Entity Framework Core 8, existing VendorAliasService (025-vendor-extraction)
- PostgreSQL 15+ (Supabase) - existing VendorAlias table (025-vendor-extraction)
- .NET 8 with C# 12 (backend), TypeScript 5.7+ with React 18.3+ (frontend) + ASP.NET Core Web API, Entity Framework Core 8, TanStack Router, TanStack Query, shadcn/ui (026-missing-receipts-ui)
- PostgreSQL 15+ (Supabase self-hosted) - extends existing Transaction table (026-missing-receipts-ui)

- **Language/Version**: YAML/Helm (Kubernetes manifests), Bash/PowerShell (scripts)
- **Primary Dependencies**: cert-manager v1.19.x, Supabase Helm chart, Azure CLI, kubectl
- **Database**: PostgreSQL 15 with pgvector extension (Supabase self-hosted)
- **Database UI**: Supabase Studio (graphical database explorer)
- **Storage**: Azure Blob Storage (`ccproctemp2025`), Azure Premium SSD (PVC)
- **Testing**: kubectl validation, connectivity tests, cert-manager readiness checks
- **Target Platform**: Azure Kubernetes Service (`dev-aks`, Kubernetes 1.33.3)
- **Project Type**: Infrastructure-as-Code (Kubernetes manifests + Helm)

## Project Structure

```text
infrastructure/
├── namespaces/
│   ├── expenseflow-dev.yaml
│   ├── expenseflow-staging.yaml
│   ├── resource-quotas.yaml
│   └── network-policies.yaml
├── cert-manager/
│   ├── namespace.yaml
│   ├── cert-manager-values.yaml
│   └── cluster-issuer.yaml
├── supabase/
│   ├── values.yaml           # Supabase Helm values (Auth/Storage disabled)
│   ├── backup-pvc.yaml       # PVC for backup storage
│   └── backup-cronjob.yaml   # Daily pg_dump CronJob
├── storage/
│   ├── blob-container-setup.ps1
│   └── secrets.yaml
├── monitoring/
│   └── alerts.yaml
└── scripts/
    ├── deploy-all.ps1
    ├── validate-deployment.ps1
    └── test-connectivity.ps1

specs/001-infrastructure-setup/
├── spec.md              # Feature specification
├── plan.md              # Implementation plan
├── research.md          # Technology decisions
├── data-model.md        # Kubernetes resource definitions
├── quickstart.md        # Deployment instructions
└── contracts/
    └── validation-tests.md
```

## Commands

```powershell
# Get AKS credentials
az aks get-credentials --resource-group rg_prod --name dev-aks

# Apply Kubernetes manifests
kubectl apply -f infrastructure/namespaces/

# Install cert-manager
helm install cert-manager jetstack/cert-manager --namespace cert-manager --create-namespace --version v1.19.1 --set crds.enabled=true

# Install Supabase (clone from GitHub - no Helm repo available)
git clone --depth 1 https://github.com/supabase-community/supabase-kubernetes.git $env:TEMP\supabase-kubernetes
helm install supabase "$env:TEMP\supabase-kubernetes\charts\supabase" --namespace expenseflow-dev --values infrastructure/supabase/values.yaml

# Check Supabase pods
kubectl get pods -n expenseflow-dev -l app.kubernetes.io/instance=supabase

# Port-forward to Supabase Studio (for local access)
kubectl port-forward svc/supabase-studio 3000:3000 -n expenseflow-dev

# Validate deployment
./infrastructure/scripts/validate-deployment.ps1
```

## Code Style

### Kubernetes Manifests (YAML)
- Use 2-space indentation
- Include `metadata.labels` for all resources
- Use namespaced resources where possible
- Follow naming convention: `expenseflow-{resource}-{environment}`

### PowerShell Scripts
- Use `$ErrorActionPreference = 'Stop'` for fail-fast behavior
- Use descriptive variable names with PascalCase
- Include comment headers for complex functions
- Use splatting for commands with many parameters

## Recent Changes
- 028-group-matching: Added .NET 8 with C# 12 + ASP.NET Core Web API, Entity Framework Core 8, Npgsql, FluentValidation
- 026-missing-receipts-ui: Added .NET 8 with C# 12 (backend), TypeScript 5.7+ with React 18.3+ (frontend) + ASP.NET Core Web API, Entity Framework Core 8, TanStack Router, TanStack Query, shadcn/ui
- 025-vendor-extraction: Added .NET 8 with C# 12 + ASP.NET Core Web API, Entity Framework Core 8, existing VendorAliasService
  - Entities: ImportJob (for tracking cache warming import jobs)
  - Services: CacheWarmingService (historical data import, job management)
  - Jobs: CacheWarmingJob (Hangfire background processing)
  - Controller: CacheWarmingController (import upload, job tracking, cache statistics)
  - Load Tests: NBomber scenarios for batch receipt processing (50 in 5min) and concurrent users (20 users, <2s P95)
  - UAT: 7 test cases (TC-001 through TC-007) covering all Sprint 3-9 features
  - InMemory Database: DbContext conditionally ignores pgvector/jsonb properties for testing
  - Entities: TravelPeriod, DetectedSubscription, SubscriptionAlert, SplitPattern, SplitAllocation
  - Services: TravelDetectionService, SubscriptionDetectionService, ExpenseSplittingService
  - Jobs: SubscriptionAlertJob (Hangfire monthly recurring)
  - All detection uses Tier 1 (rule-based) with logging for tier usage

### 001-infrastructure-setup

## Key Decisions

- **Supabase self-hosted**: Provides PostgreSQL + pgvector + Studio UI for graphical database exploration
- **Auth/Storage disabled**: Using Entra ID for auth, Azure Blob for storage (per constitution)
- **Realtime enabled**: Required `APP_NAME` and `DB_SSL: "false"` env vars (not "disable")
- **Let's Encrypt staging issuer**: Use for testing to avoid rate limits before switching to prod
- **Zero-trust network policies**: Default-deny with explicit allow rules for security

## Azure Policy Compliance

The AKS cluster has Azure Policy (Gatekeeper) enforcing:

1. **No "latest" image tag**: All containers must have explicit version tags
2. **Init containers need resources**: Must specify requests and limits for init containers
3. **Container resource limits**: All containers must have CPU/memory limits

When deploying Helm charts, patch templates if needed:
```bash
# Example: Add resources to init container in Helm template
sed -i '/- name: init-db/a\          resources:\n            requests:\n              cpu: 50m\n              memory: 64Mi\n            limits:\n              cpu: 100m\n              memory: 128Mi' deployment.yaml
```

## Azure Resources

| Resource | Name | Purpose |
|----------|------|---------|
| AKS Cluster | `dev-aks` | Kubernetes host |
| Key Vault | `iius-akv` | Secrets management |
| Storage Account | `ccproctemp2025` | Receipt blob storage |
| Container Registry | `iiusacr` | Container images |

<!-- MANUAL ADDITIONS START -->

## Docker Build Requirements

**CRITICAL**: When building Docker images for AKS deployment, ALWAYS use `--platform linux/amd64`:

```bash
# ✅ CORRECT - Always use this for AKS deployments
docker buildx build --platform linux/amd64 -t iiusacr.azurecr.io/IMAGE_NAME:TAG --push .

# ❌ WRONG - Will fail on AKS (builds for local architecture, e.g., ARM64 on Apple Silicon)
docker build -t iiusacr.azurecr.io/IMAGE_NAME:TAG .
```

**Why**: The development machine (Apple Silicon Mac) uses ARM64 architecture, but AKS nodes run on AMD64 (x86_64). Without `--platform linux/amd64`, the image manifest won't match and Kubernetes will fail to pull with "no match for platform in manifest".

### Frontend Deployment Workflow

```bash
# 1. Build AMD64 image and push to ACR
cd frontend
docker buildx build --platform linux/amd64 -t iiusacr.azurecr.io/expenseflow-frontend:vX.Y.Z-COMMIT --push .

# 2. Update staging manifest
# Edit infrastructure/kubernetes/staging/frontend-deployment.yaml with new image tag

# 3. Commit and push (ArgoCD auto-syncs from main branch)
git add . && git commit -m "chore(deploy): Update staging frontend to vX.Y.Z" && git push origin main
```

## Database Migrations (EF Core)

**CRITICAL**: EF Core migrations are NOT automatically applied on deployment. You must manually apply them after deploying code that adds new columns/tables.

### Applying Migrations to Staging

```bash
# 1. Connect to the Supabase PostgreSQL pod
kubectl exec -it $(kubectl get pods -n expenseflow-dev -l app.kubernetes.io/name=supabase-db -o jsonpath='{.items[0].metadata.name}') -n expenseflow-dev -- psql -U postgres -d expenseflow_staging

# 2. Run your migration SQL (example: adding a column)
ALTER TABLE table_name ADD COLUMN IF NOT EXISTS column_name boolean NOT NULL DEFAULT false;

# 3. Record migration in EF history (required for EF Core to recognize it)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260104000000_MigrationName', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

# 4. Exit psql
\q

# 5. Restart the API to pick up schema changes
kubectl rollout restart deployment/expenseflow-api -n expenseflow-staging
```

### Database Connection Details

| Environment | Database Name | Namespace |
|-------------|---------------|-----------|
| Staging | `expenseflow_staging` | `expenseflow-dev` (shared Supabase) |
| Dev | `postgres` | `expenseflow-dev` |

**Common Error**: `42703: column X does not exist` means migration wasn't applied before deployment.

### Migration Best Practices

1. **Use `IF NOT EXISTS`**: Makes migrations idempotent (safe to re-run)
2. **Add columns as nullable first**: Or with `DEFAULT` to avoid locking large tables
3. **Deploy migration before code**: When possible, deploy backward-compatible schema changes first
4. **Check `__EFMigrationsHistory`**: Verify migration was recorded: `SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 5;`

## React Component Type Detection

**CRITICAL**: When checking if a prop is a React component vs a ReactNode, `typeof === 'function'` is NOT sufficient.

### The Problem
Lucide icons (and many UI library components) use `React.forwardRef()`, which creates an object with `typeof === 'object'`, not `'function'`. This caused React Error #31 when the component was passed directly as a child instead of being rendered as JSX.

### Correct Pattern
```typescript
// ❌ WRONG - Fails for forwardRef components
if (typeof Icon === 'function') {
  return <Icon className="..." />;
}
return Icon; // BUG: Returns raw {$$typeof, render, displayName} object

// ✅ CORRECT - Detects both function components AND forwardRef
const isComponentType =
  typeof Icon === 'function' ||
  (typeof Icon === 'object' &&
    Icon !== null &&
    typeof (Icon as { render?: unknown }).render === 'function');

if (isComponentType) {
  const IconComponent = Icon as React.ElementType;
  return <IconComponent className="..." />;
}
return Icon; // Already a ReactNode (JSX element)
```

### ForwardRef Internal Structure
```typescript
// What a forwardRef component looks like internally:
{
  $$typeof: Symbol(react.forward_ref),
  render: ƒ,        // The actual render function
  displayName: "IconName"
}
```

**Files affected by this pattern**: `frontend/src/components/design-system/empty-state.tsx`

## Private AKS Cluster Access

The `dev-aks` cluster uses a **private API server endpoint**. When kubectl commands fail with DNS lookup errors:

```bash
# ❌ Default credentials use private FQDN (requires VPN)
az aks get-credentials --resource-group rg_prod --name dev-aks

# ✅ Use --public-fqdn for external access
az aks get-credentials --resource-group rg_prod --name dev-aks --public-fqdn
```

**Error signature**: `dial tcp: lookup *.private.southcentralus.azmk8s.io: no such host`

## Testing Infrastructure (020-testing-suite)

ExpenseFlow implements a **three-layer testing strategy**:

| Layer | CI Workflow | Target Duration | Test Categories |
|-------|-------------|-----------------|-----------------|
| Fast Feedback | ci-quick.yml | <3 min | Unit, Contract |
| PR Validation | ci-full.yml | <15 min | All except Chaos |
| Nightly Resilience | ci-nightly.yml | 30-60 min | Chaos, Property, Load |

### Test Projects

| Project | Purpose | Key Packages |
|---------|---------|--------------|
| `ExpenseFlow.Contracts.Tests` | OpenAPI contract validation | Microsoft.OpenApi.Readers |
| `ExpenseFlow.PropertyTests` | Property-based testing | FsCheck 3.0.0-rc3 |
| `ExpenseFlow.Scenarios.Tests` | Integration + chaos testing | Testcontainers, WireMock, Polly |
| `ExpenseFlow.TestCommon` | Shared utilities | - |

### Running Tests Locally

```bash
# Fast tests (unit + contract) - use for development
./scripts/test-quick.ps1

# Full test suite (requires Docker)
./scripts/test-all.ps1

# Start test infrastructure
docker-compose -f docker-compose.test.yml up -d

# Run specific test category
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Contract"
dotnet test --filter "Category=Scenario"
dotnet test --filter "Category=Property"
```

### Chaos Testing

```bash
# Enable chaos injection
export CHAOS_ENABLED=true
export CHAOS_INJECTION_RATE=0.10  # 10% failure rate
export CHAOS_MAX_LATENCY_MS=3000

# Run chaos tests
dotnet test backend/tests/ExpenseFlow.Scenarios.Tests --filter "Category=Chaos"

# Run resilience tests
dotnet test backend/tests/ExpenseFlow.Scenarios.Tests --filter "Category=Resilience"
```

### Test Categories

Use `[Trait("Category", TestCategories.X)]` for test filtering:

| Category | Description | When Run |
|----------|-------------|----------|
| `Unit` | Isolated unit tests | Every commit |
| `Contract` | API contract validation | Every commit |
| `Integration` | Component integration | PRs |
| `Scenario` | End-to-end workflows | PRs |
| `Property` | Property-based (FsCheck) | PRs + Nightly |
| `Chaos` | Fault injection | Nightly |
| `Resilience` | Recovery verification | Nightly |
| `Load` | Performance tests | Nightly |
| `Quarantined` | Flaky tests | Excluded |

### Coverage Requirements

- **Target**: 80% overall, 80% on new code (patches)
- **Enforcement**: Codecov PR checks (blocks merge if below threshold)
- **Config**: `.github/codecov.yml`

### Documentation

- **Architecture**: `docs/testing/architecture.md`
- **Chaos Runbook**: `docs/testing/chaos-runbook.md`
- **Category Validation**: `./scripts/validate-test-categories.ps1`

<!-- MANUAL ADDITIONS END -->
