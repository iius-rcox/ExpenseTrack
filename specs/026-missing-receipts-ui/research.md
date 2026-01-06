# Research: Missing Receipts UI

**Feature**: 026-missing-receipts-ui
**Date**: 2026-01-05

## Research Tasks

### 1. Missing Receipts Query Pattern

**Question**: How to efficiently compute missing receipts from existing Transaction and TransactionPrediction entities?

**Decision**: Use a LINQ query with left join pattern to compute missing receipts at query time.

**Rationale**:
- The clarified spec explicitly chose "computed view" over materialized table to avoid sync complexity
- User override precedence logic: Check `IsManualOverride=true` with `Status=Confirmed` first, then fall back to AI prediction `Status=Confirmed`
- The query filters: `(reimbursable) AND (MatchedReceiptId IS NULL) AND (ReceiptDismissed = false OR NULL)`

**Query Pattern**:
```csharp
var missingReceipts = await _context.Transactions
    .Where(t => t.UserId == userId)
    .Where(t => t.MatchedReceiptId == null)  // No matched receipt
    .Where(t => t.ReceiptDismissed != true)  // Not dismissed
    .Where(t =>
        // Has a confirmed prediction (manual override takes precedence)
        _context.TransactionPredictions
            .Any(p => p.TransactionId == t.Id &&
                     p.UserId == userId &&
                     p.Status == PredictionStatus.Confirmed))
    .Select(t => new MissingReceiptDto { ... })
    .ToListAsync();
```

**Alternatives Considered**:
- Materialized view: Rejected - adds sync complexity, requires triggers or jobs
- Database view: Rejected - harder to maintain, EF Core mapping complexity
- Background job sync: Rejected - stale data between syncs

**Performance Notes**:
- Index on `Transactions(UserId, MatchedReceiptId, ReceiptDismissed)`
- Index on `TransactionPredictions(TransactionId, UserId, Status)`
- Expected scale: 20-100 items per user, pagination at 25 handles larger sets

---

### 2. Transaction Entity Extension Pattern

**Question**: How to extend the existing Transaction entity with ReceiptUrl and ReceiptDismissed without breaking existing functionality?

**Decision**: Add nullable columns directly to Transaction entity with EF Core migration.

**Rationale**:
- Both fields are optional (nullable) so existing data remains valid
- No foreign key relationships needed (ReceiptUrl is plain text)
- Follows existing pattern - Transaction already has optional fields like `PostDate`, `MatchedReceiptId`

**Implementation**:
```csharp
// In Transaction.cs - add to existing entity
/// <summary>
/// Optional URL where receipt can be retrieved (e.g., airline portal, hotel booking).
/// </summary>
public string? ReceiptUrl { get; set; }

/// <summary>
/// When true, transaction is excluded from missing receipts list.
/// </summary>
public bool? ReceiptDismissed { get; set; }
```

**Migration Strategy**:
```sql
ALTER TABLE "Transactions"
ADD COLUMN "ReceiptUrl" text NULL,
ADD COLUMN "ReceiptDismissed" boolean NULL;
```

**Alternatives Considered**:
- Separate TransactionMetadata table: Rejected - over-engineering for 2 simple fields
- JSON column: Rejected - harder to query and index
- Extended via inheritance: Rejected - EF Core TPH complexity

---

### 3. Receipt Upload Integration

**Question**: How to integrate with existing receipt upload flow from the missing receipts view?

**Decision**: Reuse existing `ReceiptsController.Upload` endpoint with optional `targetTransactionId` parameter.

**Rationale**:
- Receipt upload pipeline already exists and handles: file validation, Azure Blob upload, OCR extraction
- The matching engine already handles linking receipts to transactions
- Frontend can call existing upload endpoint and pass target transaction hint

**Existing Flow** (from `ReceiptsController.cs`):
1. `POST /api/receipts/upload` accepts file + optional metadata
2. Returns ReceiptId for tracking
3. Background job processes OCR and triggers auto-match

**Enhancement**:
- Add optional `targetTransactionId` query param to hint the matching engine
- If provided, matching engine prioritizes this transaction for matching
- Frontend triggers query invalidation after successful upload

**Alternatives Considered**:
- New upload endpoint: Rejected - duplication of upload logic
- Direct match without upload: Rejected - breaks audit trail and OCR pipeline
- Client-side file handling: Rejected - already have server-side pipeline

---

### 4. Widget Component Pattern

**Question**: What's the best pattern for the summary widget on the Matching page?

**Decision**: Self-contained widget component with its own TanStack Query hook, rendered in Matching page layout.

**Rationale**:
- Follows existing patterns in ExpenseFlow (e.g., `MatchStatsSummary` component)
- Independent data fetching means widget loads don't block main page
- Widget can show loading skeleton while data fetches

**Implementation Pattern**:
```tsx
// missing-receipts-widget.tsx
export function MissingReceiptsWidget() {
  const { data, isLoading } = useMissingReceiptsSummary();

  if (isLoading) return <WidgetSkeleton />;
  if (!data || data.totalCount === 0) return null; // Hide when empty

  return (
    <Card>
      <CardHeader>
        <h3>Missing Receipts ({data.totalCount})</h3>
        <Link to="/missing-receipts">View All</Link>
      </CardHeader>
      <CardContent>
        {data.items.slice(0, 3).map(item => (
          <MissingReceiptQuickItem
            key={item.transactionId}
            item={item}
            onUpload={handleQuickUpload}
          />
        ))}
      </CardContent>
    </Card>
  );
}
```

**Alternatives Considered**:
- Prop drilling from parent: Rejected - couples widget to parent's data lifecycle
- Global state: Rejected - over-engineering for single-use component
- Server component: N/A - not using Next.js App Router

---

### 5. Dismiss/Restore Pattern

**Question**: How should the dismiss and restore functionality work?

**Decision**: Soft delete using `ReceiptDismissed` boolean on Transaction, with filter toggle for restore.

**Rationale**:
- Simple boolean flag is sufficient - no need for separate dismissed items table
- Query already filters by `ReceiptDismissed != true`
- Restore is just setting `ReceiptDismissed = null` or `false`

**UI Pattern**:
- Default view: Shows only non-dismissed items
- Toggle/filter: "Show dismissed" reveals dismissed items with restore action
- Dismiss action: Sets `ReceiptDismissed = true` via PATCH endpoint
- Restore action: Sets `ReceiptDismissed = null` (reverts to default)

**Alternatives Considered**:
- Separate dismissed items endpoint: Rejected - filter is simpler
- Permanent delete: Rejected - spec requires restore capability
- Archive table: Rejected - over-engineering

---

## Technology Decisions Summary

| Area | Decision | Rationale |
|------|----------|-----------|
| Query approach | Computed LINQ query | Avoids sync complexity, always fresh |
| Entity extension | Nullable columns on Transaction | Minimal change, existing pattern |
| Upload integration | Reuse existing endpoint + hint param | DRY, existing pipeline |
| Widget pattern | Self-contained with TanStack Query | Follows codebase patterns |
| Dismiss mechanism | Boolean flag on Transaction | Simple, supports restore |

## Open Questions Resolved

All unknowns from Technical Context have been resolved. No NEEDS CLARIFICATION items remain.
