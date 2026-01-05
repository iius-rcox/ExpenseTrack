# Implementation Plan: Vendor Name Extraction

**Branch**: `025-vendor-extraction` | **Date**: 2026-01-05 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/025-vendor-extraction/spec.md`

## Summary

Extract clean vendor names from cryptic bank transaction descriptions (e.g., "AMZN MKTP US*2K7XY9Z03" → "Amazon") by integrating the existing `VendorAliasService.FindMatchingAliasAsync()` into `CategorizationService.GetCategorizationAsync()`. This is a minimal integration change that leverages the already-implemented VendorAlias pattern matching infrastructure.

## Technical Context

**Language/Version**: .NET 8 with C# 12
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, existing VendorAliasService
**Storage**: PostgreSQL 15+ (Supabase) - existing VendorAlias table
**Testing**: xUnit, Moq for unit tests; integration tests via WebApplicationFactory
**Target Platform**: Azure Kubernetes Service (linux/amd64)
**Project Type**: Web application (backend only for this feature)
**Performance Goals**: <100ms additional processing per transaction (spec SC-003)
**Constraints**: Must use existing VendorAlias infrastructure per FR-004
**Scale/Scope**: Works on existing transaction data; no new tables or migrations required

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture | ✅ Pass | Change is in Infrastructure layer (CategorizationService), uses existing interfaces |
| II. Test-First Development | ✅ Pass | Unit tests required for vendor extraction logic |
| III. ERP Integration | N/A | No Vista integration changes |
| IV. API Design | ✅ Pass | No new endpoints; existing Vendor field in DTO populated |
| V. Observability | ✅ Pass | Use existing Serilog logging in CategorizationService |
| VI. Security | N/A | No auth changes; uses existing authorized endpoints |

**Gate Status**: ✅ PASSED - No violations

## Project Structure

### Documentation (this feature)

```text
specs/025-vendor-extraction/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (minimal - no new entities)
├── quickstart.md        # Phase 1 output
└── contracts/           # Phase 1 output (no new contracts)
```

### Source Code (repository root)

```text
backend/
├── src/
│   └── ExpenseFlow.Infrastructure/
│       └── Services/
│           └── CategorizationService.cs  # Primary change location
└── tests/
    └── ExpenseFlow.UnitTests/
        └── Services/
            └── CategorizationServiceTests.cs  # New test file
```

**Structure Decision**: Backend-only change. No frontend modifications needed - the `Vendor` field already exists in the API response; it just needs to be populated with extracted data instead of the raw description.

## Complexity Tracking

> No violations to justify - feature uses existing infrastructure as designed.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| N/A | N/A | N/A |
