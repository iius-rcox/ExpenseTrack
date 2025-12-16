# Implementation Plan: AI Categorization (Tiered)

**Branch**: `006-ai-categorization` | **Date**: 2025-12-16 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/006-ai-categorization/spec.md`

## Summary

Implement a cost-optimized, three-tier categorization system for expense transactions. The system suggests GL codes and departments by checking: (1) vendor alias cache, (2) embedding similarity search, and (3) AI inference - in strict order. User confirmations feed back into the learning loop, improving cache hit rates over time. Target: 70%+ Tier 1/2 suggestions after 3 months, <$40/month AI costs.

## Technical Context

**Language/Version**: .NET 8 with C# 12, ASP.NET Core Web API
**Primary Dependencies**: Entity Framework Core 8, Npgsql, Semantic Kernel, Azure.AI.OpenAI, Polly
**Storage**: PostgreSQL 15+ with pgvector (Supabase self-hosted), Azure Blob Storage
**Testing**: xUnit, Moq, FluentAssertions
**Target Platform**: Azure Kubernetes Service (dev-aks)
**Project Type**: Web application (backend API + React frontend)
**Performance Goals**: <2s response for Tier 1/2 suggestions, <5s for Tier 3
**Constraints**: <$40/month AI costs, 0.92 embedding similarity threshold
**Scale/Scope**: 10-20 users, ~500 transactions/month per user

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| **I. Cost-First AI Architecture** | ✅ PASS | Strict Tier 1→2→3 hierarchy enforced; tier logging required (FR-021) |
| **II. Self-Improving System** | ✅ PASS | User confirmations create verified embeddings (FR-017) and update vendor aliases (FR-018) |
| **III. Receipt Accountability** | N/A | Not directly applicable to categorization feature |
| **IV. Infrastructure Optimization** | ✅ PASS | Uses existing AKS, Supabase, Azure OpenAI resources |
| **V. Cache-First Design** | ✅ PASS | Tier 1 checks vendor aliases and description cache before any AI calls |

**No violations. Proceeding to Phase 0.**

## Project Structure

### Documentation (this feature)

```text
specs/006-ai-categorization/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── api-contracts.md # REST API specifications
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Api/
│   │   └── Controllers/
│   │       ├── CategorizationController.cs    # NEW: GL/dept suggestion endpoints
│   │       └── DescriptionController.cs       # NEW: Normalization endpoints
│   ├── ExpenseFlow.Core/
│   │   ├── Entities/
│   │   │   ├── DescriptionCache.cs            # Exists (Sprint 2)
│   │   │   ├── ExpenseEmbedding.cs            # Exists (Sprint 2)
│   │   │   ├── VendorAlias.cs                 # Exists (Sprint 2)
│   │   │   └── TierUsageLog.cs                # NEW
│   │   └── Interfaces/
│   │       ├── ICategorizationService.cs      # NEW
│   │       ├── IDescriptionNormalizationService.cs  # NEW
│   │       └── IEmbeddingService.cs           # NEW
│   ├── ExpenseFlow.Infrastructure/
│   │   ├── Services/
│   │   │   ├── CategorizationService.cs       # NEW: Tiered suggestion logic
│   │   │   ├── DescriptionNormalizationService.cs  # NEW
│   │   │   ├── EmbeddingService.cs            # NEW: Vector generation & search
│   │   │   └── TierUsageService.cs            # NEW: Metrics logging
│   │   └── Repositories/
│   │       ├── DescriptionCacheRepository.cs  # NEW
│   │       ├── ExpenseEmbeddingRepository.cs  # NEW
│   │       └── TierUsageRepository.cs         # NEW
│   └── ExpenseFlow.Shared/
│       └── DTOs/
│           ├── CategorizationSuggestionDto.cs # NEW
│           └── TierUsageStatsDto.cs           # NEW
└── tests/
    └── ExpenseFlow.Infrastructure.Tests/
        └── Services/
            ├── CategorizationServiceTests.cs   # NEW
            └── EmbeddingServiceTests.cs        # NEW

frontend/
├── src/
│   ├── components/
│   │   └── categorization/
│   │       ├── CategorizationPanel.tsx        # NEW: Main UI
│   │       ├── SuggestionCard.tsx             # NEW: GL/dept display
│   │       └── TierIndicator.tsx              # NEW: Tier badge
│   ├── pages/
│   │   └── CategorizationPage.tsx             # NEW
│   └── services/
│       └── categorizationService.ts           # NEW: API client
└── tests/
    └── categorization/
        └── CategorizationPanel.test.tsx        # NEW
```

**Structure Decision**: Follows existing Clean Architecture pattern with ExpenseFlow.Core (entities/interfaces), ExpenseFlow.Infrastructure (implementations), and ExpenseFlow.Api (controllers). Frontend follows established React + TypeScript structure.

## Complexity Tracking

> No violations to justify. Design follows constitution principles.

---

## Phase 0: Research Complete

See [research.md](./research.md) for detailed findings.

## Phase 1: Design Complete

See:
- [data-model.md](./data-model.md) - Entity definitions and relationships
- [contracts/api-contracts.md](./contracts/api-contracts.md) - REST API specifications
- [quickstart.md](./quickstart.md) - Developer setup guide
