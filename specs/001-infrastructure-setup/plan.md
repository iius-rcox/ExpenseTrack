# Implementation Plan: Infrastructure Setup

**Branch**: `001-infrastructure-setup` | **Date**: 2025-12-03 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-infrastructure-setup/spec.md`

## Summary

Deploy the foundational infrastructure for ExpenseFlow in the existing AKS cluster (`dev-aks`). This includes:
- cert-manager for automated Let's Encrypt TLS certificates
- Supabase self-hosted (PostgreSQL 15 + pgvector + Studio UI, Auth/Storage disabled)
- Azure Blob Storage container for receipt/document storage
- Kubernetes namespaces (`expenseflow-dev`, `expenseflow-staging`) with resource quotas and network policies
- Integration with existing Azure Key Vault for secrets management
- Container Insights alerting for infrastructure monitoring

## Technical Context

**Language/Version**: YAML/Helm (Kubernetes manifests), Bash/PowerShell (scripts)
**Primary Dependencies**: cert-manager v1.x, Supabase Helm chart, Azure CLI, kubectl
**Storage**: PostgreSQL 15+ (Supabase), Azure Blob Storage (`ccproctemp2025`), Azure Premium SSD (PVC)
**Testing**: kubectl validation, connectivity tests, cert-manager readiness checks
**Target Platform**: Azure Kubernetes Service (`dev-aks`, Kubernetes 1.33.3)
**Project Type**: Infrastructure-as-Code (Kubernetes manifests + Helm)
**Performance Goals**: Database queries <500ms, TLS handshake <100ms, 99.9% availability
**Constraints**: <$25/month new infrastructure cost, use existing AKS resources where possible
**Scale/Scope**: 10-20 users, single-tenant deployment

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Cost-First AI Architecture** | N/A | No AI operations in this sprint (infrastructure only) |
| **II. Self-Improving System** | N/A | No user interactions in this sprint |
| **III. Receipt Accountability** | N/A | Receipt storage configured but no receipt logic |
| **IV. Infrastructure Optimization** | ✅ PASS | Using NGINX via AKS add-on ($150 saved), Supabase self-hosted ($50 saved), cost target <$25/month |
| **V. Cache-First Design** | N/A | No caching operations in this sprint |

**Technology Constraints Alignment:**
- ✅ PostgreSQL 15+ with pgvector (Supabase self-hosted)
- ✅ Azure Blob Storage for receipts
- ✅ Azure Key Vault for secrets (`iius-akv`)
- ✅ Web App Routing (NGINX) already enabled
- ✅ Container Insights for monitoring

**Gate Result: PASS** - All applicable principles satisfied.

## Project Structure

### Documentation (this feature)

```text
specs/001-infrastructure-setup/
├── plan.md              # This file
├── research.md          # Phase 0: Technology decisions and best practices
├── data-model.md        # Phase 1: Kubernetes resource definitions
├── quickstart.md        # Phase 1: Deployment instructions
├── contracts/           # Phase 1: Infrastructure validation contracts
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

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
│   ├── values.yaml              # Helm values (Auth/Storage disabled)
│   ├── ingress-studio.yaml      # Ingress for Supabase Studio UI
│   ├── backup-pvc.yaml          # PVC for backup storage
│   └── backup-cronjob.yaml      # Daily pg_dump backup job
├── storage/
│   ├── blob-container-setup.ps1
│   └── secrets.yaml
├── monitoring/
│   └── alerts.yaml
└── scripts/
    ├── deploy-all.ps1
    ├── validate-deployment.ps1
    └── test-connectivity.ps1
```

**Structure Decision**: Infrastructure-as-Code with Kubernetes manifests organized by component. Each major component (cert-manager, supabase, namespaces) has its own directory for maintainability.

## Complexity Tracking

No constitution violations requiring justification. All infrastructure decisions align with Principle IV (Infrastructure Optimization) and cost targets.
