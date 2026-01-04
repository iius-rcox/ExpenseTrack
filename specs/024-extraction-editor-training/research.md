# Research: Extraction Editor with Model Training

**Feature**: 024-extraction-editor-training
**Date**: 2026-01-03

## Overview

Research findings for implementing the extraction editor with training feedback functionality.

---

## 1. Training Feedback Storage Pattern

### Decision: Separate Entity with Receipt Reference

**Chosen**: Create `ExtractionCorrection` entity with foreign key to `Receipt`

**Rationale**:
- Follows existing `PredictionFeedback` pattern in codebase
- Enables efficient querying for feedback history (User Story 5)
- Supports indefinite retention without bloating Receipt entity
- Allows future expansion (e.g., feedback quality scores, reviewer notes)

**Alternatives Considered**:

| Alternative | Rejected Because |
|-------------|-----------------|
| JSONB column on Receipt | Would bloat receipt record; harder to query; no audit trail |
| Event sourcing | Over-engineering for this use case; adds infrastructure complexity |
| Embedded in existing PredictionFeedback | Different domain; prediction feedback is for expense categorization |

### Entity Design

```csharp
public class ExtractionCorrection : BaseEntity
{
    public Guid ReceiptId { get; set; }
    public Guid UserId { get; set; }
    public string FieldName { get; set; }         // e.g., "vendor", "amount", "date"
    public string? OriginalValue { get; set; }    // JSON-serialized
    public string? CorrectedValue { get; set; }   // JSON-serialized
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Receipt Receipt { get; set; }
    public User User { get; set; }
}
```

---

## 2. Optimistic Concurrency Strategy

### Decision: Row Version with User Notification

**Chosen**: Add `RowVersion` (timestamp/xmin) to Receipt entity for conflict detection

**Rationale**:
- Spec requires FR-012: "handle optimistic concurrency for simultaneous edits with appropriate user notification"
- Last-write-wins with notification aligns with existing Receipt update pattern
- Simple to implement with EF Core's built-in concurrency tokens

**Implementation**:
1. Add `xmin` system column mapping to Receipt entity configuration
2. Catch `DbUpdateConcurrencyException` in service layer
3. Return 409 Conflict response with stale data indicator
4. Frontend shows toast: "Receipt was modified by another user. Please refresh."

**Alternatives Considered**:

| Alternative | Rejected Because |
|-------------|-----------------|
| Pessimistic locking | Would block concurrent viewers; poor UX |
| Merge conflicts UI | Over-engineering; receipt fields are independent |
| Real-time sync (SignalR) | Infrastructure complexity; not justified for edit frequency |

---

## 3. Frontend State Management

### Decision: TanStack Query Mutations with Optimistic Updates

**Chosen**: Use existing TanStack Query patterns with optimistic UI updates

**Rationale**:
- Existing `useUpdateReceipt` mutation pattern in codebase
- Optimistic updates provide immediate feedback (SC-001: <10 seconds)
- Query invalidation on success ensures consistency

**Implementation**:
1. Extend `use-receipts.ts` with `useSubmitCorrections` mutation
2. Batch all field edits into single API call (FR-006)
3. Optimistic update shows changes immediately
4. Rollback on error with toast notification

**Existing Pattern Reference**:
```typescript
// From use-receipts.ts
export function useUpdateReceipt() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }) => receiptsApi.update(id, data),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ['receipts', id] });
    }
  });
}
```

---

## 4. Line Item Editing Scope

### Decision: Edit Values Only (No CRUD)

**Chosen**: Allow editing existing line item fields; no add/delete operations

**Rationale**:
- Clarified in spec: "Edit existing line items only (no add/delete)"
- Reduces UI complexity significantly
- Covers 95% of correction use cases (OCR usually finds items, just gets values wrong)

**Implementation**:
- Line items rendered as read-only list with inline edit capability
- Each line item field editable: description, quantity, unitPrice, totalPrice
- Changes tracked same as top-level fields for training feedback

---

## 5. Confidence Score Display

### Decision: Use Existing ConfidenceIndicator Component

**Chosen**: Leverage existing `ConfidenceIndicator` component with color-coded thresholds

**Rationale**:
- Component already exists at `frontend/src/components/design-system/confidence-indicator.tsx`
- Already integrated into `ExtractedField` component
- Thresholds match spec: ≥90% green, 70-89% yellow, <70% red

**No Additional Work Required**:
- Confidence scores already returned in `ConfidenceScores` dictionary from API
- `ExtractedField` component already displays confidence via `ConfidenceIndicator`

---

## 6. Training Feedback API Design

### Decision: Extend Existing Receipt Update + New History Endpoint

**Chosen**:
1. Modify `PUT /api/receipts/{id}` to accept optional correction metadata
2. Add `GET /api/extraction-corrections` for feedback history

**Rationale**:
- Minimizes API surface area changes
- Receipt update already handles field persistence
- Separate corrections endpoint keeps concerns isolated

**Endpoints**:

| Method | Endpoint | Purpose |
|--------|----------|---------|
| PUT | `/api/receipts/{id}` | Update fields + record corrections |
| GET | `/api/extraction-corrections` | Feedback history (admin view) |
| GET | `/api/extraction-corrections/{id}` | Single correction detail |

**Request Body Extension**:
```json
{
  "vendor": "Amazon Marketplace",
  "amount": 49.99,
  "corrections": [
    {
      "fieldName": "vendor",
      "originalValue": "AMZN*Marketplace"
    }
  ]
}
```

---

## 7. Mobile Responsiveness

### Decision: CSS-Only Responsive Layout

**Chosen**: Use Tailwind responsive utilities for side-by-side → stacked layout

**Rationale**:
- Existing `ReceiptIntelligencePanel` already has responsive classes
- SC-006 requires mobile parity for task completion
- No JavaScript-based layout switching needed

**Implementation**:
```tsx
<div className="flex flex-col lg:flex-row gap-4">
  <DocumentViewer className="flex-1" />
  <FieldsPanel className="w-full lg:w-96" />
</div>
```

---

## Summary of Decisions

| Topic | Decision | Impact |
|-------|----------|--------|
| Storage Pattern | Separate ExtractionCorrection entity | Medium - New entity + migration |
| Concurrency | Optimistic with row version | Low - EF Core built-in |
| State Management | TanStack Query mutations | Low - Follows existing patterns |
| Line Items | Edit only, no add/delete | Low - Simplifies UI |
| Confidence Display | Use existing component | None - Already implemented |
| API Design | Extend PUT + new history endpoint | Medium - New controller |
| Mobile Layout | CSS responsive utilities | Low - Existing patterns |

---

## Open Questions (Resolved)

1. ~~Access control scope~~ → Any visible receipt (no restriction)
2. ~~Retention period~~ → Indefinite (permanent training corpus)
3. ~~Line item editing~~ → Edit existing only

## References

- Existing `PredictionFeedback` entity: `backend/src/ExpenseFlow.Core/Entities/PredictionFeedback.cs`
- Receipt entity with confidence scores: `backend/src/ExpenseFlow.Core/Entities/Receipt.cs`
- ExtractedField component: `frontend/src/components/receipts/extracted-field.tsx`
- ConfidenceIndicator component: `frontend/src/components/design-system/confidence-indicator.tsx`
