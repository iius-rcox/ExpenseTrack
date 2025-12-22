# ExpenseFlow Development Guidelines

Auto-generated from feature plans. Last updated: 2025-12-17

## Tool Usage
###MCP Servers
-**ref.tools**: Use this to reference best practices when developing any feature
-**shad.cn**: Use this for all frontend components and design choices

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
- 013-frontend-redesign: Added TypeScript 5.7+ with React 18.3+ + TanStack Router, TanStack Query, Tailwind CSS 4.x, shadcn/ui, Framer Motion (new), Recharts
- 012-automated-uat-testing: Added .NET 8 with C# 12 (cleanup endpoint), JSON (expected values file) + ExpenseFlow.Api (existing), test-data folder, staging API
- 011-unified-frontend: Added TypeScript 5.7+ with React 18.3+
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

<!-- MANUAL ADDITIONS END -->
