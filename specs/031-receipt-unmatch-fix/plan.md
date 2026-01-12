# Implementation Plan: Receipt Unmatch & Transaction Match Display Fix

**Branch**: `031-receipt-unmatch-fix` | **Date**: 2026-01-12 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/031-receipt-unmatch-fix/spec.md`

## Summary

This feature adds unmatch functionality to the receipt detail page (mirroring existing transaction page behavior) and fixes a bug where the transaction detail page incorrectly shows "Unmatched" status due to inconsistent use of `hasMatchedReceipt` boolean vs `matchedReceipt` object. The implementation requires:
1. Backend: Add `MatchedTransactionInfoDto` to `ReceiptDetailDto`
2. Frontend: Add "Linked Transaction" section with unmatch button to receipt detail page
3. Frontend: Fix transaction detail page to use `matchedReceipt` object as source of truth

## Technical Context

**Language/Version**: .NET 8 with C# 12 (backend), TypeScript 5.7+ with React 18.3+ (frontend)
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, TanStack Router, TanStack Query, shadcn/ui
**Storage**: PostgreSQL 15+ (Supabase self-hosted)
**Testing**: xUnit (backend), Vitest (frontend)
**Target Platform**: Azure Kubernetes Service (linux/amd64)
**Project Type**: Web application (backend + frontend)
**Performance Goals**: Unmatch operation completes in <5 seconds (matches existing transaction page)
**Constraints**: Must reuse existing `useUnmatch` hook and `/matching/{matchId}/unmatch` API endpoint
**Scale/Scope**: Single feature, ~3 files backend, ~2 files frontend

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| Clean Architecture | ✅ PASS | Changes follow 4-layer pattern (DTO in Shared, mapping in Infrastructure) |
| Test-First Development | ✅ PASS | Will add unit tests for new DTO mapping |
| ERP Integration | ✅ N/A | No Vista integration required |
| API Design | ✅ PASS | Extends existing REST endpoint, maintains ProblemDetails for errors |
| Observability | ✅ PASS | Existing logging in unmatch endpoint is sufficient |
| Security | ✅ PASS | Uses existing [Authorize] on controllers, no new endpoints |

**Gate Result**: PASS - No violations

## Project Structure

### Documentation (this feature)

```text
specs/031-receipt-unmatch-fix/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 research (minimal - pattern exists)
├── data-model.md        # Phase 1 data model changes
├── quickstart.md        # Phase 1 implementation guide
├── contracts/           # Phase 1 API contract changes
│   └── receipt-detail-response.json
└── tasks.md             # Phase 2 task breakdown
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Shared/DTOs/
│   │   ├── ReceiptDetailDto.cs          # ADD: MatchedTransactionInfoDto
│   │   └── MatchedTransactionInfoDto.cs # NEW: Mirror of MatchedReceiptInfoDto
│   └── ExpenseFlow.Infrastructure/Repositories/
│       └── ReceiptRepository.cs          # MODIFY: Include match data in detail query
└── tests/
    └── ExpenseFlow.Infrastructure.Tests/
        └── Repositories/ReceiptRepositoryTests.cs # ADD: Test for matched transaction mapping

frontend/
├── src/
│   ├── types/
│   │   └── api.ts                        # ADD: MatchedTransactionInfo interface
│   └── routes/_authenticated/
│       ├── receipts/$receiptId.tsx       # MODIFY: Add Linked Transaction section + Unmatch button
│       └── transactions/$transactionId.tsx # MODIFY: Fix match status source of truth
└── tests/
    └── routes/receipts/receiptId.test.tsx # ADD: Tests for unmatch functionality
```

**Structure Decision**: Web application with existing Clean Architecture backend and React frontend. Changes are additive to existing DTOs and components.

## Complexity Tracking

> No violations requiring justification - feature follows existing patterns.
