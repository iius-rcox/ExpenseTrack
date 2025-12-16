# Implementation Plan: Advanced Features

**Branch**: `007-advanced-features` | **Date**: 2025-12-16 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/007-advanced-features/spec.md`

## Summary

Sprint 7 implements three advanced expense management capabilities: (1) automatic travel period detection from flight/hotel receipts with GL code 66300 suggestions for related expenses, (2) subscription detection through pattern recognition across consecutive months with missing charge alerts, and (3) expense splitting across multiple GL codes/departments with learned pattern suggestions. All features prioritize rule-based detection (Tier 1) over AI inference, aligning with the cost-first architecture.

## Technical Context

**Language/Version**: .NET 8 with C# 12
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, Npgsql, Hangfire, F23.StringSimilarity (existing from Sprint 5)
**Storage**: PostgreSQL 15+ with pgvector (Supabase self-hosted), Azure Blob Storage (ccproctemp2025)
**Testing**: xUnit, FluentAssertions, Moq, Testcontainers (PostgreSQL)
**Target Platform**: Azure Kubernetes Service (dev-aks), Linux containers
**Project Type**: Web application (backend API + future frontend)
**Performance Goals**: Travel period detection <500ms, split pattern suggestions <100ms (Tier 1 cache lookup)
**Constraints**: <200ms p95 for cached operations, calendar month-end subscription alerts
**Scale/Scope**: 50+ travel periods, 20+ subscriptions per user per SC-008

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Compliance Notes |
|-----------|--------|------------------|
| I. Cost-First AI Architecture | PASS | FR-019 mandates Tier 1 rule-based for 95%+ cases; AI only for complex itineraries (FR-006) |
| II. Self-Improving System | PASS | Split patterns saved to SplitPatterns table; subscription confirmations improve detection |
| III. Receipt Accountability | N/A | Feature focuses on categorization, not receipt collection |
| IV. Infrastructure Optimization | PASS | Uses existing Hangfire for background jobs; no new infrastructure |
| V. Cache-First Design | PASS | SplitPatterns = Tier 1 cache; KnownSubscriptionVendor = seed data cache |

**Gate Result**: PASS - All applicable principles satisfied.

## Project Structure

### Documentation (this feature)

```text
specs/007-advanced-features/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── travel-api.yaml
│   ├── subscription-api.yaml
│   └── splitting-api.yaml
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Api/
│   │   └── Controllers/
│   │       ├── TravelPeriodsController.cs
│   │       ├── SubscriptionsController.cs
│   │       └── ExpenseSplittingController.cs
│   ├── ExpenseFlow.Core/
│   │   ├── Entities/
│   │   │   ├── TravelPeriod.cs
│   │   │   ├── DetectedSubscription.cs
│   │   │   ├── SplitPattern.cs
│   │   │   └── KnownSubscriptionVendor.cs
│   │   ├── Interfaces/
│   │   │   ├── ITravelDetectionService.cs
│   │   │   ├── ISubscriptionDetectionService.cs
│   │   │   └── IExpenseSplittingService.cs
│   │   └── DTOs/
│   │       ├── TravelPeriodDto.cs
│   │       ├── SubscriptionDto.cs
│   │       └── SplitAllocationDto.cs
│   └── ExpenseFlow.Infrastructure/
│       ├── Services/
│       │   ├── TravelDetectionService.cs
│       │   ├── SubscriptionDetectionService.cs
│       │   └── ExpenseSplittingService.cs
│       ├── Repositories/
│       │   ├── TravelPeriodRepository.cs
│       │   ├── SubscriptionRepository.cs
│       │   └── SplitPatternRepository.cs
│       └── Jobs/
│           └── SubscriptionAlertJob.cs
└── tests/
    ├── ExpenseFlow.Tests.Unit/
    │   ├── TravelDetectionServiceTests.cs
    │   ├── SubscriptionDetectionServiceTests.cs
    │   └── ExpenseSplittingServiceTests.cs
    └── ExpenseFlow.Tests.Integration/
        ├── TravelPeriodApiTests.cs
        ├── SubscriptionApiTests.cs
        └── SplittingApiTests.cs
```

**Structure Decision**: Extends existing backend/src structure following established patterns from Sprint 5-6. New entities in Core, services in Infrastructure, controllers in Api.

## Complexity Tracking

> No constitution violations requiring justification.
