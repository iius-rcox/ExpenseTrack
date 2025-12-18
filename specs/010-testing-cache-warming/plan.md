# Implementation Plan: Testing & Cache Warming

**Branch**: `010-testing-cache-warming` | **Date**: 2025-12-17 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/010-testing-cache-warming/spec.md`

## Summary

This sprint prepares the ExpenseFlow system for production launch through three key activities:

1. **Cache Warming**: Import historical expense report data (6 months) to populate DescriptionCache, VendorAliases, and ExpenseEmbeddings tables, achieving >50% cache hit rate on day one.

2. **User Acceptance Testing**: Execute structured UAT with 3-5 users covering all 7 critical workflows (Receipt Upload, Statement Import, Matching, Categorization, Travel Detection, Report Generation, MoM Comparison).

3. **Performance Validation**: Verify system handles 50 receipts in 5 minutes batch processing and maintains <2 second response times with 20 concurrent users.

## Technical Context

**Language/Version**: .NET 8 with C# 12
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, Npgsql, Hangfire, Semantic Kernel (for embedding generation), NBomber or k6 (for load testing)
**Storage**: PostgreSQL 15+ with pgvector (Supabase self-hosted), Azure Blob Storage (ccproctemp2025)
**Testing**: xUnit, Moq, FluentAssertions (unit tests), NBomber/k6 (load tests)
**Target Platform**: Azure Kubernetes Service (`dev-aks`, Kubernetes 1.33.3)
**Project Type**: Web application (ASP.NET Core backend)
**Performance Goals**: 50 receipts processed in 5 minutes (6 sec/receipt), 95th percentile <2s with 20 concurrent users, all queries <500ms
**Constraints**: Cache hit rate >50% after warming, <1% import error rate, staging fully isolated from production
**Scale/Scope**: 6 months historical data (~500+ unique descriptions, ~100 vendor aliases, ~500 embeddings)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Compliance | Notes |
|-----------|------------|-------|
| **I. Cost-First AI Architecture** | ✅ PASS | Cache warming directly supports this principle by pre-populating Tier 1 (cache) and Tier 2 (embeddings) to minimize Tier 3/4 AI calls at runtime. One-time embedding generation cost ~$10 is justified. |
| **II. Self-Improving System** | ✅ PASS | Historical data import creates verified cache entries that feed the learning loop, jumpstarting the system toward 70%+ hit rate target. |
| **III. Receipt Accountability** | ✅ PASS | UAT test plan includes verification of receipt matching and missing receipt placeholder functionality. |
| **IV. Infrastructure Optimization** | ✅ PASS | Uses existing AKS infrastructure with staging namespace. No new expensive managed services. Load testing validates the cost-optimized setup can handle expected load. |
| **V. Cache-First Design** | ✅ PASS | The entire sprint is focused on cache warming and validating cache-first behavior through UAT. |

**Gate Status**: ✅ PASSED - Proceed to Phase 0

## Project Structure

### Documentation (this feature)

```text
specs/010-testing-cache-warming/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── cache-warming-api.yaml
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Api/
│   │   └── Controllers/
│   │       └── CacheWarmingController.cs     # New: Cache warming endpoints
│   ├── ExpenseFlow.Core/
│   │   ├── Entities/
│   │   │   └── ImportJob.cs                  # New: Track cache warming jobs
│   │   └── Interfaces/
│   │       └── ICacheWarmingService.cs       # New: Cache warming interface
│   ├── ExpenseFlow.Infrastructure/
│   │   ├── Services/
│   │   │   └── CacheWarmingService.cs        # New: Import and cache logic
│   │   └── Jobs/
│   │       └── CacheWarmingJob.cs            # New: Hangfire background job
│   └── ExpenseFlow.Shared/
│       └── DTOs/
│           └── CacheWarmingDtos.cs           # New: Request/response DTOs
└── tests/
    ├── ExpenseFlow.Api.Tests/
    │   └── Controllers/
    │       └── CacheWarmingControllerTests.cs
    ├── ExpenseFlow.Infrastructure.Tests/
    │   └── Services/
    │       └── CacheWarmingServiceTests.cs
    └── ExpenseFlow.LoadTests/                # New: Load test project
        ├── ExpenseFlow.LoadTests.csproj
        └── Scenarios/
            ├── BatchReceiptProcessingTests.cs
            └── ConcurrentUserTests.cs

infrastructure/
├── kubernetes/
│   └── staging/
│       ├── deployment.yaml                   # New: Staging deployment
│       ├── configmap.yaml                    # New: Staging config
│       └── secrets.yaml                      # New: Staging secrets reference
└── namespaces/
    └── expenseflow-staging.yaml              # Existing

docs/
├── uat/
│   ├── test-plan.md                          # New: UAT test plan document
│   ├── test-cases/                           # New: Individual test case files
│   │   ├── TC-001-receipt-upload.md
│   │   ├── TC-002-statement-import.md
│   │   ├── TC-003-matching.md
│   │   ├── TC-004-categorization.md
│   │   ├── TC-005-travel-detection.md
│   │   ├── TC-006-report-generation.md
│   │   └── TC-007-mom-comparison.md
│   └── defects/                              # New: Defect tracking folder
└── performance/
    └── load-test-report-template.md          # New: Load test report template
```

**Structure Decision**: Extends existing Clean Architecture structure with new cache warming service and dedicated load test project. UAT documentation placed in `/docs/uat/` for easy access by test users. Staging Kubernetes manifests added under `/infrastructure/kubernetes/staging/`.

## Complexity Tracking

> No complexity violations detected. All additions follow existing patterns.
