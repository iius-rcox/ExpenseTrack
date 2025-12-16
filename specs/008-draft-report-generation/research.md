# Research: Draft Report Generation

**Feature**: 008-draft-report-generation
**Date**: 2025-12-16

## Research Summary

This feature builds on existing infrastructure from Sprints 5-6 (matching engine, tiered categorization). No new external integrations required. Research focuses on design patterns for aggregating matched data and integrating with existing services.

---

## Topic 1: Report Generation Pattern

### Question
How should draft report generation aggregate data from receipts, transactions, and matches?

### Decision
**Batch Processing with Service Orchestration** - The ReportService will:
1. Query confirmed ReceiptTransactionMatches for the period
2. Query unmatched transactions for the period (missing receipts)
3. For each item, call existing ICategorizationService and IDescriptionNormalizationService
4. Assemble ExpenseReport with ExpenseLines
5. Persist in single transaction

### Rationale
- Reuses existing categorization infrastructure (no duplication)
- Maintains tier logging through existing services
- Simple to test and debug
- Aligns with Clean Architecture - service orchestrates but doesn't duplicate logic

### Alternatives Considered
- **Hangfire Background Job**: Rejected - report generation is fast enough (<30s) for synchronous response; user expects immediate feedback
- **Materialized View**: Rejected - adds PostgreSQL complexity; data changes frequently during editing
- **CQRS with Event Sourcing**: Rejected - over-engineering for 10-20 user scale

---

## Topic 2: Tier Information Propagation

### Question
How should tier information (1/2/3) be captured and displayed for each suggestion?

### Decision
**Store tier metadata on ExpenseLine entity** - Each ExpenseLine stores:
- `GLCodeTier` (1, 2, or 3)
- `DepartmentTier` (1, 2, or 3)
- `GLCodeSource` (string: "VendorAlias", "EmbeddingSimilarity", "AIInference")
- `DepartmentSource` (string: similar values)

### Rationale
- Existing ICategorizationService.GetCategorizationAsync returns TransactionCategorizationDto with tier info
- Persisting allows display without recalculating
- Enables analytics on tier distribution per report

### Alternatives Considered
- **Recalculate on display**: Rejected - expensive; AI costs for Tier 3 items
- **Store only tier number**: Rejected - source name provides better UX labels

---

## Topic 3: Missing Receipt Handling

### Question
How should unmatched transactions (missing receipts) be included in reports?

### Decision
**Include all unmatched transactions with justification requirement**:
1. Query transactions where `MatchStatus = Unmatched` for the period
2. Create ExpenseLine with `HasReceipt = false`
3. Require `MissingReceiptJustification` before report can proceed to Sprint 9 export
4. Store justification as enum with `JustificationNote` for "Other" option

### Rationale
- Constitution Principle III requires receipt accountability
- Missing receipts must appear in consolidated PDF (Sprint 9)
- Justification options align with spec: "Not provided", "Lost", "Digital subscription", "Under threshold", "Other"

### Alternatives Considered
- **Exclude unmatched transactions**: Rejected - violates receipt accountability
- **Auto-generate justification**: Rejected - user must explicitly acknowledge

---

## Topic 4: Learning Loop Integration

### Question
How should user edits trigger cache/embedding updates?

### Decision
**Delegate to existing services via confirmation flow**:
1. When user edits GL/department and saves:
   - Call `ICategorizationService.ConfirmCategorizationAsync()` with selected values
   - This handles VendorAlias updates and ExpenseEmbedding creation
2. When user accepts normalized description:
   - Description is already cached by IDescriptionNormalizationService during generation
   - No additional action needed unless user edits description

### Rationale
- Reuses existing learning infrastructure (Sprint 6)
- No new cache update logic to maintain
- Consistent behavior with direct categorization flow

### Alternatives Considered
- **Custom cache update in ReportService**: Rejected - duplicates logic; harder to maintain
- **Event-driven updates**: Rejected - adds complexity; synchronous is sufficient

---

## Topic 5: Existing Draft Handling

### Question
How should the system handle generating a draft for a period that already has one?

### Decision
**Soft-delete previous draft, create new**:
1. Check for existing draft for user + period
2. If exists: mark as `IsDeleted = true` (soft delete) or prompt user to replace
3. Create new draft
4. User's explicit choice via API parameter: `replaceExisting: true/false`

### Rationale
- Users may want to regenerate after new matches
- Soft delete preserves audit trail if needed
- Simple UX: API accepts flag, frontend shows confirmation dialog

### Alternatives Considered
- **Versioning**: Rejected - over-engineering; users unlikely to need history
- **Merge new items into existing**: Rejected - complex conflict resolution

---

## Topic 6: Performance Optimization

### Question
How to meet <30 second generation target for 50 expenses?

### Decision
**Parallel categorization calls with batching**:
1. Fetch all matches and unmatched transactions in two queries
2. Batch description normalization calls (cache hits are instant)
3. Parallelize categorization calls using `Task.WhenAll`
4. Use `AsNoTracking()` for read queries
5. Single `SaveChangesAsync()` for all new entities

### Rationale
- Tier 1 cache hits: ~1ms each
- Tier 2 embedding lookups: ~10ms each
- Tier 3 AI calls: ~500ms each (but should be rare with warm cache)
- Parallelization reduces wall-clock time for AI calls

### Alternatives Considered
- **Sequential processing**: Rejected - too slow for Tier 3 scenarios
- **Pre-compute categorizations**: Rejected - stale data issues; adds background job complexity

---

## Dependencies Verification

| Dependency | Status | Notes |
|------------|--------|-------|
| ICategorizationService | **Exists** | Sprint 6 - provides tiered GL/dept suggestions |
| IDescriptionNormalizationService | **Exists** | Sprint 6 - provides cache-first normalization |
| ReceiptTransactionMatch | **Exists** | Sprint 5 - links receipts to transactions |
| IMatchRepository | **Exists** | Sprint 5 - queries matches by user/period |
| VendorAlias learning | **Exists** | Sprint 6 - ConfirmCategorizationAsync updates |
| ExpenseEmbedding creation | **Exists** | Sprint 6 - ConfirmCategorizationAsync creates |

---

## Unknowns Resolved

All technical context items are resolved. No NEEDS CLARIFICATION markers remain.
