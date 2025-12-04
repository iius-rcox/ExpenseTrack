<!--
SYNC IMPACT REPORT
==================
Version Change: 1.0.0 → 1.1.0 (MINOR - Infrastructure clarifications and Azure resource specifics)

Modified Principles: None (principles unchanged)

Added Sections:
- Azure Resource Reference (detailed inventory under Technology Constraints)
- Environment-Specific Configuration (connection details, identities)

Removed Sections: None

Updated Content:
- Technology Constraints > Infrastructure: Updated from generic placeholders to actual
  Azure resources discovered via CLI enumeration (dev-aks, iiusacr, iius-akv, etc.)
- Added specific node pool configuration based on actual AKS cluster
- Added Web App Routing note (NGINX already enabled via AKS add-on)
- Added Azure OpenAI resource reference (iius-embedding)
- Added Document Intelligence resource reference (iius-doc-intelligence)

Templates Requiring Updates:
- .specify/templates/plan-template.md ✅ Compatible (Constitution Check section exists)
- .specify/templates/spec-template.md ✅ Compatible (Requirements structure aligns)
- .specify/templates/tasks-template.md ✅ Compatible (Phase structure supports principles)
- .specify/templates/checklist-template.md ✅ Compatible (No principle-specific content)
- .specify/templates/agent-file-template.md ✅ Compatible (No principle-specific content)

Follow-up TODOs: None
-->

# ExpenseFlow Constitution

## Core Principles

### I. Cost-First AI Architecture (NON-NEGOTIABLE)

Every AI-powered operation MUST follow the cost hierarchy in strict order:

1. **Tier 1 - Cache Lookup ($0):** Check exact match in cache tables first
2. **Tier 2 - Embedding Similarity (~$0.00002):** Find similar items via pgvector
3. **Tier 3 - GPT-4o-mini (~$0.0003):** Simple inference for 90% of new items
4. **Tier 4 - GPT-4o/Claude (~$0.01):** Complex reasoning only when cheaper options fail

**Rationale:** The system targets <$20/month AI costs at steady state. Skipping tiers
wastes money and violates the core value proposition. Every AI call MUST log which
tier resolved the request.

### II. Self-Improving System

Every user action MUST improve future system performance:

- Description confirmations → DescriptionCache entries (future = Tier 1)
- Vendor match confirmations → VendorAlias entries (future = Tier 1)
- GL code selections → Verified ExpenseEmbeddings (future = Tier 2)
- Split allocations → SplitPatterns entries (future = Tier 1)
- Statement imports → StatementFingerprints (future = Tier 1)

**Rationale:** Month 1 cache hit rate target is 20%; Month 6+ target is 70%+.
This trajectory is only achievable if every user interaction feeds the learning loop.

### III. Receipt Accountability

All expenses MUST have a receipt or invoice prior to submission. No exceptions.

- Recurring subscriptions require receipts monthly
- Missing receipts require documented justification
- Missing receipt placeholders MUST appear in consolidated PDF
- Justification options: 'Receipt not provided', 'Lost receipt', 'Digital subscription', 'Other'

**Rationale:** This is a critical business rule. The AP department requires complete
documentation. Placeholders ensure visibility into gaps without blocking submission.

### IV. Infrastructure Optimization

Infrastructure decisions MUST minimize monthly costs while maintaining reliability:

- Web App Routing (NGINX) via AKS add-on over Azure Application Gateway ($150/month saved)
- Supabase self-hosted in AKS over Azure PostgreSQL ($50/month saved)
- Hangfire with PostgreSQL over Azure Service Bus ($10/month saved)
- Total new monthly cost target: ~$25 (Blob Storage $5 + AI APIs ~$20)

**Rationale:** This is a 10-20 user application. Enterprise-grade managed services
are overkill. Self-hosted alternatives in existing AKS provide adequate reliability
at fraction of the cost.

### V. Cache-First Design

All operations that can be cached MUST check cache before any computation:

- Statement imports: Check fingerprint cache before AI column mapping
- Description normalization: Check DescriptionCache before GPT-4o-mini
- GL code suggestions: Check vendor cache → embeddings → AI (in order)
- Split suggestions: Check SplitPatterns table (deterministic, no AI)
- Vendor matching: Check VendorAliases table (deterministic, no AI)

**Rationale:** Cache lookups are free and fast. Cache misses trigger learning.
Every cache hit is money saved and latency reduced.

## Technology Constraints

**Backend Stack:**
- .NET 8 with ASP.NET Core Web API
- Entity Framework Core with Npgsql for PostgreSQL
- Semantic Kernel for AI orchestration (tiered model selection)
- Hangfire for background job processing
- Polly for resilience patterns

**Frontend Stack:**
- React 18+ with TypeScript
- PWA-enabled with service worker for offline receipt capture
- Tailwind CSS for responsive design
- MSAL.js for Entra ID authentication

**Data Layer:**
- PostgreSQL 15+ with pgvector extension (Supabase self-hosted)
- Azure Blob Storage for receipts and documents
- Weekly sync from external SQL Server for GL/Dept/Project reference tables

**AI Models (by tier):**
- Tier 2: text-embedding-3-small (Azure OpenAI: iius-embedding) for all embeddings
- Tier 3: gpt-4o-mini (Azure OpenAI: iius-embedding) for normalization, column mapping, GL suggestion
- Tier 4: gpt-4o (Azure OpenAI) or claude-3.5-sonnet (Anthropic) for complex reasoning

**Infrastructure (Azure Resources):**

| Resource | Name/ID | Notes |
|----------|---------|-------|
| AKS Cluster | `dev-aks` (rg_prod) | Kubernetes 1.33.3, private cluster |
| System Node Pool | `systempool` (3x standard_d4lds_v5) | AzureLinux, zones 1,2,3 |
| User Node Pool | `optimized` (1x Standard_B2ms) | Ubuntu, workload pool |
| Container Registry | `iiusacr.azurecr.io` | Premium SKU, zone redundant |
| Key Vault | `iius-akv` | Secrets management |
| Azure OpenAI | `iius-embedding` | Endpoint: https://iius-embedding.openai.azure.com/ |
| Document Intelligence | `iius-doc-intelligence` | Endpoint: https://iius-doc-intelligence.cognitiveservices.azure.com/ |
| Storage Account | `ccproctemp2025` | Use for receipt blob storage |
| VNet | `vnet_prod` (10.0.0.0/16) | AKS integrated |
| Log Analytics | `DefaultWorkspace-...-SCUS` | Container Insights enabled |

**AKS Features Already Enabled:**
- Web App Routing (NGINX ingress) - No separate deployment needed
- Azure Key Vault Secrets Provider (with rotation)
- Workload Identity
- KEDA (event-driven autoscaling)
- Container Insights monitoring

**Required New Deployments:**
- cert-manager for Let's Encrypt TLS certificates
- Supabase self-hosted (PostgreSQL + pgvector)
- Kubernetes namespaces: `expenseflow-dev`, `expenseflow-staging`, `expenseflow-prod`
- Persistent Volume Claims for PostgreSQL data

## Development Workflow

**Feature Implementation:**
1. Verify alignment with constitution principles before design
2. Check cost implications for any AI-powered feature
3. Ensure learning loops are implemented for user interactions
4. Document cache table impacts in design documents

**Code Review Requirements:**
- All AI calls MUST include tier logging
- All user confirmations MUST trigger cache/embedding updates
- All new endpoints MUST handle authentication via Entra ID
- Background jobs MUST use Hangfire, not inline processing

**Testing Requirements:**
- Cache hit/miss scenarios MUST be tested for AI operations
- Tier escalation logic MUST be verified (1→2→3→4 order)
- Receipt accountability rules MUST have acceptance tests

## Governance

This constitution supersedes all other development practices for the ExpenseFlow
project. Amendments require:

1. **Documentation:** Clear rationale for the change
2. **Impact Analysis:** Assessment of affected components and templates
3. **Version Increment:**
   - MAJOR: Backward incompatible governance/principle removals or redefinitions
   - MINOR: New principle/section added or materially expanded guidance
   - PATCH: Clarifications, wording, typo fixes, non-semantic refinements
4. **Migration Plan:** Steps to align existing code with new requirements

**Compliance Review:**
- All PRs MUST reference constitution compliance in description
- Architecture decisions MUST justify any deviation from stated constraints
- Cost projections MUST be validated against Principle I targets

**Version**: 1.1.0 | **Ratified**: 2025-12-03 | **Last Amended**: 2025-12-03
