# Implementation Plan: Transaction Group Matching

**Branch**: `028-group-matching` | **Date**: 2026-01-07 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/028-group-matching/spec.md`

## Summary

Extend the receipt matching engine to treat transaction groups as first-class match candidates. Currently, `MatchingService.RunAutoMatchAsync()` only evaluates individual transactions. This plan adds group-level matching by:
1. Querying unmatched transaction groups alongside transactions
2. Using `CombinedAmount` and `DisplayDate` for scoring (same algorithm)
3. Excluding grouped transactions from individual matching
4. Supporting manual match/unmatch for groups

## Technical Context

**Language/Version**: .NET 8 with C# 12
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, Npgsql, FluentValidation
**Storage**: PostgreSQL 15+ (Supabase self-hosted)
**Testing**: xUnit, FluentAssertions, Moq, Testcontainers
**Target Platform**: Azure Kubernetes Service (linux/amd64)
**Project Type**: Web application (backend + frontend)
**Performance Goals**: <2 seconds per receipt for auto-match (existing benchmark)
**Constraints**: Must maintain backward compatibility with existing individual transaction matching
**Scale/Scope**: ~1000 transactions per user, ~50 groups per user (typical)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Clean Architecture** | ✅ Pass | Changes follow existing 4-layer pattern: Core (interfaces), Infrastructure (MatchingService), Api (controllers) |
| **II. Test-First Development** | ✅ Pass | Unit tests for scoring logic, integration tests for API endpoints planned |
| **III. ERP Integration** | ✅ N/A | No Vista integration required for matching |
| **IV. API Design** | ✅ Pass | RESTful endpoints, ProblemDetails errors, FluentValidation - extends existing patterns |
| **V. Observability** | ✅ Pass | Serilog structured logging with named parameters for group matching events |
| **VI. Security** | ✅ Pass | User ID scoping inherited from existing services; all queries filter by UserId |

**Gate Result**: PASS - No violations. Proceed to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/028-group-matching/
├── spec.md              # Feature specification (complete)
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (OpenAPI additions)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Api/
│   │   └── Controllers/
│   │       └── MatchingController.cs          # Extend for group matching
│   ├── ExpenseFlow.Core/
│   │   ├── Entities/
│   │   │   ├── TransactionGroup.cs            # Already exists (no changes)
│   │   │   └── ReceiptTransactionMatch.cs     # Already has TransactionGroupId
│   │   └── Interfaces/
│   │       └── IMatchingService.cs            # Add group-aware method signatures
│   ├── ExpenseFlow.Infrastructure/
│   │   └── Services/
│   │       └── MatchingService.cs             # Core implementation changes
│   └── ExpenseFlow.Shared/
│       └── DTOs/
│           └── MatchingDtos.cs                # Add group candidate DTOs
└── tests/
    ├── ExpenseFlow.Unit.Tests/
    │   └── Services/
    │       └── MatchingServiceGroupTests.cs   # New test file
    └── ExpenseFlow.Integration.Tests/
        └── Matching/
            └── GroupMatchingTests.cs          # New integration tests

frontend/
└── src/
    └── features/
        └── matching/
            └── components/
                └── MatchCandidateList.tsx     # Show groups as candidates
```

**Structure Decision**: Web application pattern (backend + frontend). Changes are additive to existing MatchingService - no new projects or layers needed.

## Complexity Tracking

> No constitution violations requiring justification.

| Aspect | Complexity | Rationale |
|--------|------------|-----------|
| New entities | None | TransactionGroup and ReceiptTransactionMatch already exist with required columns |
| New services | None | Extending existing MatchingService |
| API changes | Minimal | Existing endpoints work; add group candidates to response DTOs |
| Database changes | None | Schema already supports TransactionGroupId in match table |
