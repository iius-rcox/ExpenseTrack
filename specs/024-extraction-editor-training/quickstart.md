# Quickstart: Extraction Editor with Model Training

**Feature**: 024-extraction-editor-training
**Date**: 2026-01-03

## Overview

This feature enables users to edit AI-extracted receipt fields and captures corrections as training feedback for model improvement. It extends the existing `ReceiptIntelligencePanel` with inline editing and creates a new `ExtractionCorrection` entity for storing training data.

---

## Prerequisites

- [ ] Feature branch created: `git checkout -b 024-extraction-editor-training`
- [ ] Read [spec.md](./spec.md) - Feature requirements and user stories
- [ ] Read [research.md](./research.md) - Key decisions and rationale
- [ ] Read [data-model.md](./data-model.md) - Entity schema and migration SQL
- [ ] Read [contracts/extraction-corrections.yaml](./contracts/extraction-corrections.yaml) - API specification

---

## Implementation Phases

### Phase 1: Backend Foundation (Estimated: 4 hours)

#### 1.1 Create ExtractionCorrection Entity

```bash
# Location: backend/src/ExpenseFlow.Core/Entities/
touch ExtractionCorrection.cs
```

Copy entity from [data-model.md](./data-model.md#entity-class).

#### 1.2 Add EF Configuration

```bash
# Location: backend/src/ExpenseFlow.Infrastructure/Data/Configurations/
touch ExtractionCorrectionConfiguration.cs
```

Copy configuration from [data-model.md](./data-model.md#ef-core-configuration).

#### 1.3 Register in DbContext

```csharp
// Add to ExpenseFlowDbContext.cs
public DbSet<ExtractionCorrection> ExtractionCorrections => Set<ExtractionCorrection>();
```

#### 1.4 Add RowVersion to Receipt

```csharp
// Add to Receipt.cs
[Timestamp]
public uint RowVersion { get; set; }

// Add to ReceiptConfiguration.cs
builder.Property(e => e.RowVersion)
    .HasColumnName("xmin")
    .HasColumnType("xid")
    .ValueGeneratedOnAddOrUpdate()
    .IsConcurrencyToken();
```

#### 1.5 Create Migration

```bash
cd backend/src/ExpenseFlow.Infrastructure
dotnet ef migrations add AddExtractionCorrections -s ../ExpenseFlow.Api
```

### Phase 2: Service Layer (Estimated: 3 hours)

#### 2.1 Create Service Interface

```bash
# Location: backend/src/ExpenseFlow.Core/Interfaces/
touch IExtractionCorrectionService.cs
```

```csharp
public interface IExtractionCorrectionService
{
    Task<PagedResult<ExtractionCorrectionDto>> GetCorrectionsAsync(
        ExtractionCorrectionQueryParams queryParams,
        CancellationToken ct = default);

    Task<ExtractionCorrectionDetailDto?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    Task RecordCorrectionsAsync(
        Guid receiptId,
        Guid userId,
        IEnumerable<CorrectionMetadataDto> corrections,
        CancellationToken ct = default);
}
```

#### 2.2 Implement Service

```bash
# Location: backend/src/ExpenseFlow.Infrastructure/Services/
touch ExtractionCorrectionService.cs
```

#### 2.3 Register in DI

```csharp
// Add to DependencyInjection.cs
services.AddScoped<IExtractionCorrectionService, ExtractionCorrectionService>();
```

### Phase 3: API Layer (Estimated: 2 hours)

#### 3.1 Create DTOs

```bash
# Location: backend/src/ExpenseFlow.Shared/DTOs/
touch ExtractionCorrectionDtos.cs
```

#### 3.2 Create Controller

```bash
# Location: backend/src/ExpenseFlow.Api/Controllers/
touch ExtractionCorrectionsController.cs
```

#### 3.3 Extend ReceiptsController

Modify `UpdateAsync` to:
1. Accept optional `corrections` array
2. Call `IExtractionCorrectionService.RecordCorrectionsAsync`
3. Handle `DbUpdateConcurrencyException` with 409 response

### Phase 4: Frontend Integration (Estimated: 4 hours)

#### 4.1 Update Types

```bash
# Location: frontend/src/types/
# Update receipt.ts with correction types
```

#### 4.2 Add Mutation Hook

```typescript
// Add to use-receipts.ts
export function useSubmitCorrections() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data, corrections }) =>
      receiptsApi.updateWithCorrections(id, data, corrections),
    onSuccess: (_, { id }) => {
      queryClient.invalidateQueries({ queryKey: ['receipts', id] });
      toast.success('Changes saved');
    },
    onError: (error) => {
      if (error.status === 409) {
        toast.error('Receipt was modified by another user. Please refresh.');
      }
    }
  });
}
```

#### 4.3 Wire ExtractedField

Connect the existing `ExtractedField` component's `onSave` callback to track corrections:

```typescript
// In receipt-intelligence-panel.tsx
const [pendingCorrections, setPendingCorrections] = useState<CorrectionMetadata[]>([]);

const handleFieldEdit = (fieldName: string, newValue: unknown, originalValue: unknown) => {
  if (newValue !== originalValue) {
    setPendingCorrections(prev => [
      ...prev.filter(c => c.fieldName !== fieldName),
      { fieldName, originalValue: JSON.stringify(originalValue) }
    ]);
  }
};

const handleSaveAll = () => {
  submitCorrections.mutate({
    id: receipt.id,
    data: { vendor, amount, date, tax, currency, lineItems },
    corrections: pendingCorrections
  });
};
```

---

## Testing Checklist

### Unit Tests

- [ ] `ExtractionCorrectionServiceTests.cs`
  - [ ] Records single correction
  - [ ] Records multiple corrections for same receipt
  - [ ] Ignores no-op corrections (same value)
  - [ ] Paginates results correctly
  - [ ] Filters by field name
  - [ ] Filters by date range

### Integration Tests

- [ ] `PUT /api/receipts/{id}` with corrections creates feedback records
- [ ] `PUT /api/receipts/{id}` with stale rowVersion returns 409
- [ ] `GET /api/extraction-corrections` returns paginated results
- [ ] `GET /api/extraction-corrections?fieldName=vendor` filters correctly

### Frontend Tests

- [ ] Edit field shows save button
- [ ] Save All batches all corrections
- [ ] 409 error shows refresh toast
- [ ] Optimistic update shows immediately

---

## Deployment Steps

### Database Migration

The migration must be applied manually per CLAUDE.md. Connect to the Supabase PostgreSQL pod and run the following SQL:

```bash
# 1. Connect to the Supabase PostgreSQL pod
kubectl exec -it $(kubectl get pods -n expenseflow-dev -l app.kubernetes.io/name=supabase-db -o jsonpath='{.items[0].metadata.name}') -n expenseflow-dev -- psql -U postgres -d expenseflow_staging

# 2. Run the migration SQL (see below)
# 3. Record migration in EF history
# 4. Exit and restart API
```

**Migration SQL (20260106000000_AddExtractionCorrections):**

```sql
-- Create extraction_corrections table
CREATE TABLE IF NOT EXISTS extraction_corrections (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    receipt_id uuid NOT NULL,
    user_id uuid NOT NULL,
    field_name character varying(50) NOT NULL,
    original_value text,
    corrected_value text,
    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_extraction_corrections PRIMARY KEY (id),
    CONSTRAINT fk_extraction_corrections_receipt FOREIGN KEY (receipt_id)
        REFERENCES receipts(id) ON DELETE CASCADE,
    CONSTRAINT fk_extraction_corrections_user FOREIGN KEY (user_id)
        REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT ck_extraction_corrections_field_name
        CHECK (field_name IN ('vendor', 'amount', 'date', 'tax', 'currency', 'line_item'))
);

-- Create indexes
CREATE INDEX IF NOT EXISTS ix_extraction_corrections_receipt_id ON extraction_corrections(receipt_id);
CREATE INDEX IF NOT EXISTS ix_extraction_corrections_user_id ON extraction_corrections(user_id);
CREATE INDEX IF NOT EXISTS ix_extraction_corrections_created_at ON extraction_corrections(created_at DESC);
CREATE INDEX IF NOT EXISTS ix_extraction_corrections_field_name ON extraction_corrections(field_name);

-- Add table comment
COMMENT ON TABLE extraction_corrections IS 'Training feedback: user corrections to AI-extracted receipt fields. Retained indefinitely.';

-- Record migration in EF history
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260106000000_AddExtractionCorrections', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
```

**After migration:**

```bash
# Restart API to pick up schema changes
kubectl rollout restart deployment/expenseflow-api -n expenseflow-staging
```

### Verification

#### Editing Workflow

1. Navigate to any receipt detail page
2. Click edit icon on vendor field
3. Change value and click Save
4. Verify:
   - Field shows updated value
   - Visual indicator shows "manually edited" with amber border
   - Training feedback record created (check `/api/extraction-corrections`)

#### Confidence Display (T031)

1. Navigate to a receipt with AI-extracted fields
2. Verify confidence indicators display correctly:
   - **High confidence (≥90%)**: Green dots, 4-5 filled
   - **Medium confidence (70-89%)**: Amber dots, 3-4 filled
   - **Low confidence (<70%)**: Red dots, 0-3 filled
3. Check that percentage labels appear next to indicators (when `showLabel` is enabled)
4. Verify tooltips show "High/Medium/Low confidence: X%"

#### Processing Lock

1. Upload a new receipt
2. While status shows "Processing", verify:
   - Lock icon appears with message "Fields are locked while receipt is being processed"
   - Edit buttons are disabled
   - `readOnly` state prevents input

---

## Key Files Reference

| File | Purpose |
|------|---------|
| `ExtractionCorrection.cs` | Entity definition |
| `ExtractionCorrectionConfiguration.cs` | EF Core configuration |
| `IExtractionCorrectionService.cs` | Service interface |
| `ExtractionCorrectionService.cs` | Service implementation |
| `ExtractionCorrectionsController.cs` | API endpoints |
| `ExtractionCorrectionDtos.cs` | Request/response DTOs |
| `receipt-intelligence-panel.tsx` | Main editing UI |
| `extracted-field.tsx` | Individual field editor |
| `use-receipts.ts` | TanStack Query mutations |

---

## Troubleshooting

### "Receipt was modified by another user"

This 409 error means the `rowVersion` is stale. The frontend should prompt the user to refresh the page to get the latest data.

### Corrections not being recorded

Check that:
1. `corrections` array is included in the PUT request body
2. `originalValue` differs from the new value (no-op corrections are ignored)
3. Service is registered in DI container

### Migration fails

Ensure PostgreSQL 15+ is running with correct permissions. The `extraction_corrections` table uses `gen_random_uuid()` which requires PostgreSQL 13+.

---

## Related Documentation

- [Spec](./spec.md) - Full feature specification
- [Research](./research.md) - Design decisions
- [Data Model](./data-model.md) - Entity schema
- [API Contract](./contracts/extraction-corrections.yaml) - OpenAPI specification
