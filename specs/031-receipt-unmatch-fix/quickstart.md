# Quickstart: Receipt Unmatch & Transaction Match Display Fix

**Feature**: 031-receipt-unmatch-fix
**Estimated Effort**: 2-3 hours
**Prerequisites**: Existing codebase with matching functionality working

## Implementation Order

```
1. Backend DTO (30 min)
   └─► 2. Backend Repository (30 min)
       └─► 3. Frontend Types (15 min)
           └─► 4. Frontend Receipt Page (45 min)
               └─► 5. Frontend Transaction Page Fix (15 min)
                   └─► 6. Testing (30 min)
```

## Step 1: Add MatchedTransactionInfoDto (Backend)

**File**: `backend/src/ExpenseFlow.Shared/DTOs/ReceiptDetailDto.cs`

```csharp
// Add new DTO class (can be in same file or separate)
public class MatchedTransactionInfoDto
{
    public Guid MatchId { get; set; }
    public Guid Id { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? MerchantName { get; set; }
    public decimal MatchConfidence { get; set; }
}

// Add to ReceiptDetailDto
public class ReceiptDetailDto : ReceiptSummaryDto
{
    // ... existing properties ...

    public MatchedTransactionInfoDto? MatchedTransaction { get; set; }
}
```

## Step 2: Update Receipt Repository (Backend)

**File**: `backend/src/ExpenseFlow.Infrastructure/Repositories/ReceiptRepository.cs`

In the `GetByIdAsync` method (or equivalent), add Include for match data:

```csharp
var receipt = await _context.Receipts
    .Include(r => r.ReceiptTransactionMatches
        .Where(m => m.Status == MatchProposalStatus.Confirmed))
    .ThenInclude(m => m.Transaction)
    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

// In mapping logic:
MatchedTransaction = receipt.ReceiptTransactionMatches
    .Where(m => m.Status == MatchProposalStatus.Confirmed)
    .Select(m => new MatchedTransactionInfoDto
    {
        MatchId = m.Id,
        Id = m.TransactionId ?? Guid.Empty,
        TransactionDate = m.Transaction?.TransactionDate ?? DateOnly.MinValue,
        Description = m.Transaction?.Description ?? "",
        Amount = m.Transaction?.Amount ?? 0,
        MerchantName = m.Transaction?.MerchantName,
        MatchConfidence = m.ConfidenceScore / 100m
    })
    .FirstOrDefault()
```

## Step 3: Add Frontend Types

**File**: `frontend/src/types/api.ts`

```typescript
export interface MatchedTransactionInfo {
  matchId: string
  id: string
  transactionDate: string
  description: string
  amount: number
  merchantName: string | null
  matchConfidence: number
}

// Update ReceiptDetail interface
export interface ReceiptDetail extends ReceiptSummary {
  // ... existing properties ...
  matchedTransaction: MatchedTransactionInfo | null
}
```

## Step 4: Update Receipt Detail Page

**File**: `frontend/src/routes/_authenticated/receipts/$receiptId.tsx`

Add imports:
```typescript
import { useUnmatch } from '@/hooks/queries/use-matching'
import { Link2Off, CreditCard } from 'lucide-react'
```

Add state and hook:
```typescript
const unmatchMutation = useUnmatch()
const [showUnmatchDialog, setShowUnmatchDialog] = useState(false)
```

Add Linked Transaction section (after File Information card):
```tsx
{/* Linked Transaction */}
<Card>
  <CardHeader>
    <CardTitle className="flex items-center gap-2">
      <CreditCard className="h-5 w-5" />
      Linked Transaction
    </CardTitle>
  </CardHeader>
  <CardContent>
    {receipt.matchedTransaction ? (
      <div className="flex items-center justify-between p-4 bg-muted/50 rounded-lg">
        <div>
          <p className="font-medium">{receipt.matchedTransaction.description}</p>
          <p className="text-sm text-muted-foreground">
            {formatDate(receipt.matchedTransaction.transactionDate)} • {formatCurrency(receipt.matchedTransaction.amount)}
          </p>
          <Badge variant="outline" className="mt-1">
            Confidence: {Math.round(receipt.matchedTransaction.matchConfidence * 100)}%
          </Badge>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" asChild>
            <Link to="/transactions/$transactionId" params={{ transactionId: receipt.matchedTransaction.id }}>
              View Transaction
            </Link>
          </Button>
          <Button variant="outline" onClick={() => setShowUnmatchDialog(true)}>
            <Link2Off className="mr-2 h-4 w-4" />
            Unmatch
          </Button>
        </div>
      </div>
    ) : (
      <div className="text-center py-8 text-muted-foreground">
        <CreditCard className="h-12 w-12 mx-auto mb-2 opacity-50" />
        <p>No transaction matched yet</p>
      </div>
    )}
  </CardContent>
</Card>
```

Add unmatch dialog (copy pattern from transaction page).

## Step 5: Fix Transaction Detail Page

**File**: `frontend/src/routes/_authenticated/transactions/$transactionId.tsx`

Replace all uses of `transaction.hasMatchedReceipt` with `!!transaction.matchedReceipt`:

```typescript
// Before (buggy)
{transaction.hasMatchedReceipt ? (

// After (fixed)
{transaction.matchedReceipt ? (
```

This ensures the match status display uses the actual object presence as the source of truth.

## Step 6: Testing

### Manual Testing Checklist

1. **Receipt with match**: Navigate to matched receipt, verify transaction info displays
2. **Unmatch from receipt**: Click Unmatch, confirm, verify both pages update
3. **Receipt without match**: Verify "No transaction matched" state
4. **Transaction page fix**: Verify matched transactions show correct badge

### Unit Test (Backend)

```csharp
[Fact]
public async Task GetByIdAsync_WithConfirmedMatch_ReturnsMatchedTransactionInfo()
{
    // Arrange: Create receipt with confirmed match
    // Act: Call GetByIdAsync
    // Assert: MatchedTransaction is populated with correct values
}
```

## Verification Commands

```bash
# Backend build
cd backend && dotnet build

# Frontend type check
cd frontend && npm run type-check

# Run tests
cd backend && dotnet test
cd frontend && npm test
```

## Common Issues

1. **Match not showing**: Ensure Include chain includes `.ThenInclude(m => m.Transaction)`
2. **Confidence score wrong**: Remember to divide by 100 (backend stores 0-100, frontend expects 0-1)
3. **Cache not invalidating**: `useUnmatch` already invalidates `['receipts']` - verify this is working
