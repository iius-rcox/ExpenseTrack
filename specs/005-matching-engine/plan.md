# Implementation Plan: Matching Engine

**Branch**: `005-matching-engine` | **Date**: 2025-12-15 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/005-matching-engine/spec.md`

## Summary

Implement a receipt-to-transaction matching engine that uses vendor aliases, amount/date tolerances, and fuzzy string matching to automatically propose matches with confidence scores. The system learns from user confirmations to improve future accuracy. This is a **Tier 1 only** feature (no AI API calls) per constitution principle I.

Key capabilities:
- Auto-match receipts to transactions with confidence scoring (amount 40pts, date 35pts, vendor 25pts)
- User review and confirmation/rejection workflow
- Vendor alias learning from confirmations
- Background job for alias confidence decay
- Optimistic locking for concurrent conflict resolution

## Technical Context

**Language/Version**: .NET 8 with C# 12
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, Npgsql, Hangfire, F23.StringSimilarity (Levenshtein)
**Storage**: PostgreSQL 15+ (Supabase self-hosted with pgvector), Azure Blob Storage (ccproctemp2025)
**Testing**: xUnit, FluentAssertions, Moq, Testcontainers for PostgreSQL integration tests
**Target Platform**: Azure Kubernetes Service (dev-aks)
**Project Type**: Web application (backend API)
**Performance Goals**: Batch auto-match 50 receipts in <60 seconds, individual match confirmation <500ms
**Constraints**: Tier 1 only (no AI calls), optimistic locking for concurrency, 70% minimum confidence threshold
**Scale/Scope**: 10-20 users, ~500 receipts/month, ~1000 transactions/month per user

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. Cost-First AI Architecture | ✅ PASS | Feature is Tier 1 only - no AI API calls. All matching uses database lookups and string algorithms. |
| II. Self-Improving System | ✅ PASS | User confirmations create/update VendorAlias entries (FR-008). Match_count and last_matched_at tracked (FR-009). |
| III. Receipt Accountability | ✅ PASS | Feature links receipts to transactions, improving documentation completeness. |
| IV. Infrastructure Optimization | ✅ PASS | Uses existing PostgreSQL, Hangfire infrastructure. No new managed services. |
| V. Cache-First Design | ✅ PASS | VendorAliases table is checked first (FR-006) before fuzzy matching fallback (FR-007). |

**Gate Status**: ✅ ALL PRINCIPLES SATISFIED - Proceed to Phase 0

## Project Structure

### Documentation (this feature)

```text
specs/005-matching-engine/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── matching-api.yaml
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Api/
│   │   └── Controllers/
│   │       └── MatchingController.cs       # New: matching endpoints
│   ├── ExpenseFlow.Core/
│   │   ├── Entities/
│   │   │   └── ReceiptTransactionMatch.cs  # New: match entity
│   │   └── Interfaces/
│   │       ├── IMatchingService.cs         # New: matching orchestration
│   │       ├── IMatchRepository.cs         # New: match persistence
│   │       └── IFuzzyMatchingService.cs    # New: string similarity
│   ├── ExpenseFlow.Infrastructure/
│   │   ├── Data/
│   │   │   ├── Configurations/
│   │   │   │   └── ReceiptTransactionMatchConfiguration.cs  # New
│   │   │   └── Migrations/
│   │   │       └── 20251215xxxxxx_AddReceiptTransactionMatch.cs  # New
│   │   ├── Repositories/
│   │   │   └── MatchRepository.cs          # New
│   │   ├── Services/
│   │   │   ├── MatchingService.cs          # New: core algorithm
│   │   │   └── FuzzyMatchingService.cs     # New: Levenshtein wrapper
│   │   └── Jobs/
│   │       └── AliasConfidenceDecayJob.cs  # New: Hangfire job
│   └── ExpenseFlow.Shared/
│       ├── DTOs/
│       │   ├── MatchProposalDto.cs         # New
│       │   ├── MatchConfirmRequestDto.cs   # New
│       │   ├── MatchingStatsDto.cs         # New
│       │   └── UnmatchedItemsDto.cs        # New
│       └── Enums/
│           └── MatchStatus.cs              # New: Proposed/Confirmed/Rejected
└── tests/
    ├── ExpenseFlow.UnitTests/
    │   └── Services/
    │       ├── MatchingServiceTests.cs     # New
    │       └── FuzzyMatchingServiceTests.cs # New
    └── ExpenseFlow.IntegrationTests/
        └── Matching/
            └── MatchingEndpointTests.cs    # New
```

**Structure Decision**: Extends existing Clean Architecture pattern with new entity, service, repository, and controller following established conventions in ExpenseFlow.Core/Infrastructure/Api.

## Complexity Tracking

> No violations - feature aligns with all constitution principles.

| Aspect | Complexity | Justification |
|--------|------------|---------------|
| New Entity | Low | Single new table (ReceiptTransactionMatch) with FK relationships |
| Matching Algorithm | Medium | Scoring algorithm with 3 components, well-defined thresholds |
| Fuzzy Matching | Low | Using proven library (F23.StringSimilarity) |
| Concurrency | Low | Standard EF Core optimistic locking pattern |
| Background Job | Low | Single Hangfire job following existing JobBase pattern |
