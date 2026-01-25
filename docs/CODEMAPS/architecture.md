# ExpenseTrack Architecture Overview

**Last Updated**: 2026-01-25
**Token Estimate**: ~800 tokens

---

## System Architecture

```mermaid
graph TB
    subgraph Frontend["Frontend (React 18.3)"]
        UI[React Components]
        Router[TanStack Router]
        Query[TanStack Query]
        MSAL[MSAL Auth]
    end

    subgraph Backend["Backend (.NET 8)"]
        API[ASP.NET Core API]
        Services[Business Services]
        Jobs[Hangfire Jobs]
        AI[Semantic Kernel]
    end

    subgraph Data["Data Layer"]
        PG[(PostgreSQL 15)]
        Blob[Azure Blob Storage]
        Cache[Description Cache]
    end

    subgraph External["External Services"]
        EntraID[Azure Entra ID]
        DocInt[Document Intelligence]
        Vista[Viewpoint Vista ERP]
    end

    UI --> Router --> Query --> API
    MSAL --> EntraID
    API --> Services --> PG
    Services --> Blob
    Services --> DocInt
    Jobs --> Vista
    AI --> Services
```

## Layer Architecture

| Layer | Technology | Purpose |
|-------|------------|---------|
| **Presentation** | React + TypeScript | User interface, routing, state |
| **API** | ASP.NET Core 8 | REST endpoints, auth, validation |
| **Business** | C# Services | Domain logic, AI orchestration |
| **Infrastructure** | EF Core + Azure | Data access, blob storage, jobs |
| **Data** | PostgreSQL + pgvector | Persistence, embeddings, search |

## Deployment Topology

```mermaid
graph LR
    subgraph AKS["Azure Kubernetes Service"]
        FE[Frontend Pod]
        BE[Backend Pod]
        HF[Hangfire Worker]
    end

    subgraph Azure["Azure Services"]
        ACR[Container Registry]
        Blob[Blob Storage]
        KV[Key Vault]
    end

    subgraph Supabase["Supabase (Self-Hosted)"]
        PG[(PostgreSQL)]
        Studio[Supabase Studio]
    end

    ArgoCD --> AKS
    ACR --> AKS
    AKS --> Blob
    AKS --> PG
    AKS --> KV
```

## Key Integration Points

| Integration | Protocol | Purpose |
|-------------|----------|---------|
| Azure Entra ID | OAuth 2.0 / OIDC | User authentication |
| Document Intelligence | REST API | OCR receipt extraction |
| Viewpoint Vista | SQL (read-only) | Dept/Project/GL sync |
| Azure Blob Storage | REST + SAS tokens | Receipt file storage |

## AI/ML Pipeline (Tiered)

1. **Tier 1 (Rule-based)**: Pattern matching, vendor aliases, keyword extraction
2. **Tier 2 (Semantic)**: Semantic Kernel + Azure OpenAI for categorization
3. **Vector Search**: pgvector for expense similarity matching

## GitOps Deployment Flow

```
PR Merge → ci-full.yml → cd-deploy.yml → ACR Push → ArgoCD Sync → AKS
```
