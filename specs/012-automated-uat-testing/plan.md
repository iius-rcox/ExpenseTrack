# Implementation Plan: Automated UAT Testing for Claude Code

**Branch**: `012-automated-uat-testing` | **Date**: 2025-12-19 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/012-automated-uat-testing/spec.md`

## Summary

This feature implements an automated UAT testing framework executable by Claude Code against the ExpenseFlow staging API. The implementation requires: (1) a cleanup endpoint for test data isolation, (2) an expected values JSON file for golden data assertions, and (3) UAT execution documentation. The test suite validates the complete receipt-to-report pipeline using 19 test receipts and a 486-transaction Chase CSV statement.

## Technical Context

**Language/Version**: .NET 8 with C# 12 (cleanup endpoint), JSON (expected values file)
**Primary Dependencies**: ExpenseFlow.Api (existing), test-data folder, staging API
**Storage**: `test-data/receipts/` (19 images), `test-data/statements/chase.csv`, `test-data/expected-values.json` (new)
**Testing**: Claude Code-driven UAT execution via HTTP API calls with JSON output
**Target Platform**: Claude Code executing against staging API (`https://staging.expense.ii-us.com/api`)
**Project Type**: Test automation (minimal backend addition + test data configuration)
**Performance Goals**: Complete UAT suite in <15 minutes, individual receipt processing <2 minutes
**Constraints**: 5-second polling interval, continue-independent on failures, structured JSON output
**Scale/Scope**: 19 receipts, 486 transactions, 1 known match pair (RDU parking)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Cost-First AI Architecture | ✅ N/A | UAT tests validate existing AI behavior, no new AI operations |
| II. Self-Improving System | ✅ Aligned | Tests validate that user confirmations feed cache tables |
| III. Receipt Accountability | ✅ Aligned | Tests validate receipt processing and matching workflow |
| IV. Infrastructure Optimization | ✅ Aligned | No new infrastructure; uses existing staging deployment |
| V. Cache-First Design | ✅ Aligned | Tests can validate cache hit behavior (fingerprint reuse on re-import) |

**Gate Status**: ✅ PASS - No violations. Proceed to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/012-automated-uat-testing/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (expected values schema)
├── quickstart.md        # Phase 1 output (Claude Code execution guide)
├── contracts/           # Phase 1 output (cleanup endpoint OpenAPI)
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
backend/
├── src/
│   └── ExpenseFlow.Api/
│       └── Controllers/
│           └── TestCleanupController.cs  # NEW: Cleanup endpoint for test isolation
└── tests/
    └── (existing test projects - no changes)

test-data/
├── receipts/                    # 19 receipt images (existing)
│   ├── 20251211_212334.jpg     # RDU parking receipt ($84.00)
│   ├── Receipt 2768.pdf        # PDF receipt test case
│   └── ... (17 more)
├── statements/
│   └── chase.csv               # 486 transactions (existing)
└── expected-values.json        # NEW: Golden data for assertions
```

**Structure Decision**: Minimal footprint - one new controller for cleanup, one new JSON file for expected values. All UAT logic is documentation-driven for Claude Code execution.

## Complexity Tracking

> No constitution violations - section not needed.

