# Research: Receipt Unmatch & Transaction Match Display Fix

**Feature**: 031-receipt-unmatch-fix
**Date**: 2026-01-12

## Research Summary

This feature follows established patterns in the codebase. Minimal research required as we're mirroring existing functionality.

## Existing Pattern Analysis

### 1. Transaction Detail → Receipt Match Display

**Current Implementation** (working reference):
- `TransactionDetailDto` includes `MatchedReceiptInfoDto? MatchedReceipt`
- `MatchedReceiptInfoDto` contains: matchId, id, vendor, date, amount, thumbnailUrl, matchConfidence
- Frontend displays linked receipt in "Linked Receipt" card with View and Unmatch buttons
- `useUnmatch` hook calls `POST /matching/{matchId}/unmatch`

**Decision**: Mirror this pattern for Receipt Detail → Transaction display

### 2. Unmatch API Endpoint

**Current Implementation**:
- Endpoint: `POST /api/matching/{matchId}/unmatch`
- Sets `MatchProposalStatus.Unmatched` (preserves audit trail)
- Invalidates both receipt and transaction caches
- Returns updated `MatchDetail`

**Decision**: Reuse existing endpoint (no backend API changes needed)

### 3. Match Status Display Bug

**Root Cause Analysis**:
```typescript
// TransactionSummary (extended by TransactionDetail)
hasMatchedReceipt: boolean  // From API - can be stale

// TransactionDetail
matchedReceipt: MatchedReceiptInfo | null  // Always current
```

The frontend uses `hasMatchedReceipt` (from summary) for badge display but `matchedReceipt` (from detail) for the linked item section. These can become inconsistent.

**Decision**: Use `!!matchedReceipt` as canonical source of truth for all display logic

## Technology Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Backend DTO pattern | Mirror `MatchedReceiptInfoDto` structure | Consistency, proven pattern |
| API changes | None (extend existing DTO only) | Existing unmatch endpoint is sufficient |
| Frontend state | Reuse `useUnmatch` hook | DRY principle, already invalidates correct caches |
| Match status source | Use object presence, not boolean | Eliminates stale data bug |

## Alternatives Considered

### 1. Separate API for Receipt Match Info
- **Rejected**: Would require additional network call when data can be joined in existing query
- **Rationale**: EF Core can eagerly load match data efficiently

### 2. Remove `hasMatchedReceipt` Boolean
- **Rejected**: Would be breaking change for list views that depend on it
- **Rationale**: Fix the detail page to not rely on it; boolean is still useful for summary displays

### 3. Bidirectional Match Display Component
- **Rejected**: Over-engineering for current scope
- **Rationale**: Transaction and Receipt detail pages have different layouts; shared component would be forced abstraction

## Open Questions

None - all technical decisions resolved.
