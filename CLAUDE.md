# ExpenseFlow Development Guidelines

Auto-generated from feature plans. Last updated: 2025-12-16

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
- 006-ai-categorization: Added Microsoft.SemanticKernel 1.25.0 for embeddings and chat completion, Microsoft.Extensions.Resilience for Polly v8 retry/circuit breaker, tiered categorization system (vendor alias → embedding similarity → AI inference), EmbeddingCleanupJob for monthly stale embedding purge
- 005-matching-engine: Added .NET 8 with C# 12 + ASP.NET Core Web API, Entity Framework Core 8, Npgsql, Hangfire, F23.StringSimilarity (Levenshtein)
- 004-statement-fingerprinting: Added .NET 8 with C# 12 + ASP.NET Core Web API, Entity Framework Core 8, Npgsql, CsvHelper, ClosedXML (Excel), Semantic Kernel, Azure.AI.OpenAI

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
<!-- MANUAL ADDITIONS END -->
