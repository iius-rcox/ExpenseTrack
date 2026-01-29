# ExpenseFlow Development Guidelines

Last updated: 2026-01-29

## Tool Usage

### MCP Servers
- **ref.tools**: Use this to reference best practices when developing any feature
- **shad.cn**: Use this for all frontend components and design choices

## Technology Stack

### Backend
| Category | Technology |
|----------|------------|
| Framework | .NET 8 with C# 12, ASP.NET Core Web API |
| ORM | Entity Framework Core 8, Npgsql |
| Database | PostgreSQL 15+ with pgvector (Supabase self-hosted) |
| Auth | Azure Entra ID (Microsoft.Identity.Web) |
| Background Jobs | Hangfire (PostgreSQL backend) |
| AI/ML | Microsoft.SemanticKernel 1.25.0, Azure.AI.OpenAI, Azure.AI.DocumentIntelligence |
| Resilience | Polly v8 (Microsoft.Extensions.Resilience) |
| Documents | PdfSharpCore, ClosedXML, HtmlAgilityPack, PuppeteerSharp |
| Images | SixLabors.ImageSharp, SkiaSharp |
| Validation | FluentValidation |
| Matching | F23.StringSimilarity (Levenshtein) |
| CSV/Excel | CsvHelper, ClosedXML |

### Frontend
| Category | Technology |
|----------|------------|
| Framework | React 18.3+ with TypeScript 5.7+ |
| Build | Vite 6.0 |
| Routing | TanStack Router v1.141+ |
| State | TanStack Query v5.90+ |
| Styling | Tailwind CSS 4.x, shadcn/ui (Radix primitives) |
| Auth | Azure MSAL |
| Animations | Framer Motion |
| Charts | Recharts |
| Icons | Lucide React |
| Theming | next-themes |
| Validation | Zod |

### Infrastructure
| Category | Technology |
|----------|------------|
| Orchestration | Azure Kubernetes Service (dev-aks, K8s 1.33.3) |
| Manifests | YAML + Helm |
| TLS | Let's Encrypt + cert-manager v1.19.x |
| Database UI | Supabase Studio |
| Storage | Azure Blob Storage (ccproctemp2025) |
| CI/CD | GitHub Actions → ACR → ArgoCD |

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

## Project Structure

```text
ExpenseTrack/
├── backend/
│   ├── src/
│   │   ├── ExpenseFlow.Api/           # Controllers, middleware, endpoints
│   │   ├── ExpenseFlow.Core/          # Domain models, entities, business logic
│   │   ├── ExpenseFlow.Infrastructure/ # EF Core context, external services
│   │   └── ExpenseFlow.Shared/        # DTOs, shared utilities
│   ├── tests/
│   │   ├── ExpenseFlow.Api.Tests/
│   │   ├── ExpenseFlow.Core.Tests/
│   │   ├── ExpenseFlow.Contracts.Tests/    # OpenAPI validation
│   │   ├── ExpenseFlow.PropertyTests/      # FsCheck property tests
│   │   ├── ExpenseFlow.Scenarios.Tests/    # Testcontainers, WireMock
│   │   └── ExpenseFlow.TestCommon/
│   └── Dockerfile
├── frontend/
│   ├── src/
│   │   ├── components/         # Reusable UI components
│   │   ├── pages/              # Page components
│   │   ├── routes/             # TanStack Router config
│   │   ├── services/           # API service layer
│   │   ├── hooks/              # Custom React hooks
│   │   ├── auth/               # Azure MSAL config
│   │   ├── providers/          # Context providers
│   │   ├── types/              # TypeScript definitions
│   │   └── lib/                # Utilities
│   ├── tests/
│   │   ├── unit/               # Vitest unit tests
│   │   ├── integration/        # Component tests
│   │   └── e2e/                # Playwright E2E
│   └── Dockerfile
├── infrastructure/
│   ├── namespaces/             # K8s namespace configs
│   ├── cert-manager/           # TLS certificate management
│   ├── supabase/               # Supabase Helm values
│   ├── kubernetes/             # Deployment manifests
│   └── scripts/                # Deployment scripts
├── specs/                      # 31 feature specifications
├── docs/                       # Documentation
├── test-data/                  # Sample receipts/statements
├── .github/workflows/          # CI/CD pipelines
└── CLAUDE.md
```

## Commands

### Backend Development
```bash
# Run backend locally
cd backend && dotnet run --project src/ExpenseFlow.Api

# Run backend tests
dotnet test backend/tests/ExpenseFlow.Api.Tests
dotnet test backend/tests/ExpenseFlow.Core.Tests

# Run all backend tests with coverage
dotnet test backend --collect:"XPlat Code Coverage"

# Add EF Core migration
dotnet ef migrations add MigrationName --project backend/src/ExpenseFlow.Infrastructure --startup-project backend/src/ExpenseFlow.Api
```

### Frontend Development
```bash
# Install dependencies
cd frontend && npm install

# Run development server
npm run dev

# Run tests
npm run test          # Vitest unit tests
npm run test:e2e      # Playwright E2E tests

# Build for production
npm run build
```

### Infrastructure / Kubernetes
```powershell
# Get AKS credentials (use --public-fqdn for external access)
az aks get-credentials --resource-group rg_prod --name dev-aks --public-fqdn

# Apply Kubernetes manifests
kubectl apply -f infrastructure/namespaces/

# Check Supabase pods
kubectl get pods -n expenseflow-dev -l app.kubernetes.io/instance=supabase

# Port-forward to Supabase Studio (for local access)
kubectl port-forward svc/supabase-studio 3000:3000 -n expenseflow-dev

# Restart API deployment after changes
kubectl rollout restart deployment/expenseflow-api -n expenseflow-staging

# Validate deployment
./infrastructure/scripts/validate-deployment.ps1
```

## Code Style

### C# / .NET Backend
- Use file-scoped namespaces
- Follow Microsoft C# naming conventions (PascalCase for public, camelCase for private with `_` prefix)
- Use `async/await` for all I/O operations
- Use FluentValidation for request validation
- Prefer records for DTOs
- Use `[Trait("Category", TestCategories.X)]` for test categorization

### TypeScript / React Frontend
- Use functional components with hooks
- Prefer `const` over `let`
- Use TanStack Query for all API calls (no raw fetch)
- Use shadcn/ui components from `@/components/ui`
- Follow file-based routing with TanStack Router
- Use Zod for runtime validation

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

## Key Architectural Patterns

### Three-Tier Categorization
1. **Vendor Alias** - Exact match lookup in VendorAlias table
2. **Embedding Similarity** - pgvector cosine similarity search
3. **AI Inference** - Semantic Kernel + Azure OpenAI fallback

### Tiered Detection
- **Tier 1**: Rule-based detection (active)
- **Tier 2**: ML-based detection (future)
- All tiers log usage for analytics

### Background Processing
- Hangfire for async jobs (receipt processing, cache warming, alerts)
- PostgreSQL backend for job persistence
- Subscription alerts run as monthly recurring jobs

## Key Decisions

### Infrastructure
- **Supabase self-hosted**: Provides PostgreSQL + pgvector + Studio UI for graphical database exploration
- **Auth/Storage disabled in Supabase**: Using Entra ID for auth, Azure Blob for storage
- **Realtime enabled**: Required `APP_NAME` and `DB_SSL: "false"` env vars (not "disable")
- **Let's Encrypt staging issuer**: Use for testing to avoid rate limits before switching to prod
- **Zero-trust network policies**: Default-deny with explicit allow rules for security

### Backend
- **Semantic Kernel over direct OpenAI SDK**: Better abstraction for AI orchestration
- **pgvector for embeddings**: Native PostgreSQL support, no separate vector DB needed
- **Hangfire over hosted services**: Persistence, UI dashboard, retry policies

### Frontend
- **TanStack Router over React Router**: Type-safe routing, better code splitting
- **TanStack Query over Redux**: Built-in caching, optimistic updates, simpler API
- **shadcn/ui over MUI/Chakra**: Radix primitives, full control, Tailwind integration

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

## CI/CD Deployment (GitOps)

**Deployments are automated via GitHub Actions.** Do NOT build Docker images locally.

### How It Works

```
Push to main → GitHub Actions builds image → Pushes to ACR → Updates manifest → ArgoCD syncs
```

| Workflow | Trigger | What It Does |
|----------|---------|--------------|
| `cd-deploy.yml` | Push to `main` with `backend/**` or `frontend/**` changes | Builds, pushes, updates manifests |
| Manual dispatch | GitHub Actions UI | Deploy specific component to specific environment |

### Deploying Changes

```bash
# Just commit and push - CI/CD handles the rest
git add .
git commit -m "fix(api): Your change description"
git push origin main
```

**Monitor deployment:** https://github.com/iius-rcox/ExpenseTrack/actions

### Manual Deployment (Emergency Only)

If CI/CD is broken and you need to deploy manually:

```bash
# 1. Build with correct platform (AKS requires linux/amd64)
docker buildx build --platform linux/amd64 -t iiusacr.azurecr.io/expenseflow-api:vX.Y.Z --push .

# 2. Update manifest and push
# Edit infrastructure/kubernetes/staging/deployment.yaml
git add . && git commit -m "chore(deploy): Manual deploy vX.Y.Z" && git push origin main
```

**Note:** Manual builds require Docker Desktop. The CI/CD pipeline builds on GitHub's ubuntu runners, eliminating this dependency.

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
