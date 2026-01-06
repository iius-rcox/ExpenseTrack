# Data Model: Missing Receipts UI

**Feature**: 026-missing-receipts-ui
**Date**: 2026-01-05

## Entity Changes

### Transaction (Extended)

The existing `Transaction` entity is extended with two new nullable fields:

```csharp
namespace ExpenseFlow.Core.Entities;

public class Transaction : BaseEntity
{
    // ... existing fields ...

    /// <summary>
    /// Optional URL where receipt can be retrieved.
    /// Examples: airline booking confirmation, hotel portal, vendor receipt page.
    /// Stored as plain text without validation (user responsibility).
    /// </summary>
    public string? ReceiptUrl { get; set; }

    /// <summary>
    /// When true, transaction is excluded from the missing receipts list.
    /// Allows users to dismiss transactions incorrectly flagged as reimbursable.
    /// Set to null or false to include in missing receipts list.
    /// </summary>
    public bool? ReceiptDismissed { get; set; }
}
```

### Database Schema Change

```sql
-- Migration: AddMissingReceiptFieldsToTransaction
ALTER TABLE "Transactions"
ADD COLUMN "ReceiptUrl" text NULL;

ALTER TABLE "Transactions"
ADD COLUMN "ReceiptDismissed" boolean NULL;

-- Index for efficient missing receipts query
CREATE INDEX "IX_Transactions_UserId_MissingReceipt"
ON "Transactions" ("UserId", "MatchedReceiptId", "ReceiptDismissed")
WHERE "MatchedReceiptId" IS NULL AND ("ReceiptDismissed" IS NULL OR "ReceiptDismissed" = false);
```

## View Models (DTOs)

### MissingReceiptSummaryDto

Used for list display and widget items:

```csharp
namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Summary of a transaction missing a receipt.
/// </summary>
public class MissingReceiptSummaryDto
{
    /// <summary>Transaction ID.</summary>
    public Guid TransactionId { get; set; }

    /// <summary>Transaction date.</summary>
    public DateOnly TransactionDate { get; set; }

    /// <summary>Vendor/description from transaction.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Transaction amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>Days since transaction date.</summary>
    public int DaysSinceTransaction { get; set; }

    /// <summary>Optional URL where receipt can be retrieved.</summary>
    public string? ReceiptUrl { get; set; }

    /// <summary>Whether transaction has been dismissed from missing receipts list.</summary>
    public bool IsDismissed { get; set; }

    /// <summary>Source of reimbursability determination.</summary>
    public ReimbursabilitySource Source { get; set; }
}

/// <summary>
/// How reimbursability was determined.
/// </summary>
public enum ReimbursabilitySource
{
    /// <summary>User manually marked as reimbursable.</summary>
    UserOverride = 0,

    /// <summary>AI prediction confirmed by user.</summary>
    AIPrediction = 1
}
```

### MissingReceiptsListResponseDto

Paginated response for full list page:

```csharp
/// <summary>
/// Paginated list of missing receipts.
/// </summary>
public class MissingReceiptsListResponseDto
{
    /// <summary>List of missing receipt items.</summary>
    public List<MissingReceiptSummaryDto> Items { get; set; } = new();

    /// <summary>Total count matching filters.</summary>
    public int TotalCount { get; set; }

    /// <summary>Current page (1-based).</summary>
    public int Page { get; set; }

    /// <summary>Page size.</summary>
    public int PageSize { get; set; }

    /// <summary>Total number of pages.</summary>
    public int TotalPages { get; set; }
}
```

### MissingReceiptsWidgetDto

Compact response for dashboard widget:

```csharp
/// <summary>
/// Widget summary for missing receipts dashboard card.
/// </summary>
public class MissingReceiptsWidgetDto
{
    /// <summary>Total count of missing receipts.</summary>
    public int TotalCount { get; set; }

    /// <summary>Top 3 most recent missing receipts for quick action.</summary>
    public List<MissingReceiptSummaryDto> RecentItems { get; set; } = new();
}
```

### UpdateReceiptUrlRequestDto

Request for updating receipt URL:

```csharp
/// <summary>
/// Request to update receipt URL for a transaction.
/// </summary>
public class UpdateReceiptUrlRequestDto
{
    /// <summary>
    /// URL where receipt can be retrieved.
    /// Pass null or empty string to clear.
    /// </summary>
    public string? ReceiptUrl { get; set; }
}
```

### DismissReceiptRequestDto

Request for dismissing/restoring a transaction:

```csharp
/// <summary>
/// Request to dismiss or restore a transaction from missing receipts.
/// </summary>
public class DismissReceiptRequestDto
{
    /// <summary>
    /// True to dismiss from missing receipts list.
    /// False or null to restore to the list.
    /// </summary>
    public bool? Dismiss { get; set; }
}
```

## Relationships

```
┌─────────────────────────────────────────────────────────────┐
│                      Transaction                             │
├─────────────────────────────────────────────────────────────┤
│ Id (PK)                                                      │
│ UserId (FK → Users)                                          │
│ TransactionDate                                              │
│ Description                                                  │
│ Amount                                                       │
│ MatchedReceiptId (FK → Receipts, nullable)                  │
│ ReceiptUrl (NEW - nullable text)                            │
│ ReceiptDismissed (NEW - nullable bool)                      │
└────────────────────────┬────────────────────────────────────┘
                         │
                         │ 1:N
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                  TransactionPrediction                       │
├─────────────────────────────────────────────────────────────┤
│ Id (PK)                                                      │
│ TransactionId (FK → Transactions)                            │
│ UserId (FK → Users)                                          │
│ PatternId (FK → ExpensePatterns, nullable)                  │
│ Status (Pending/Confirmed/Rejected/Ignored)                  │
│ IsManualOverride (bool)                                      │
│ ConfidenceScore                                              │
└─────────────────────────────────────────────────────────────┘
```

## Computed View Logic

**MissingReceiptEntry** is a view model computed at query time, not a persisted entity.

**Query Logic** (pseudocode):
```
SELECT Transactions WHERE:
  - UserId = current user
  - MatchedReceiptId IS NULL (no matched receipt)
  - (ReceiptDismissed IS NULL OR ReceiptDismissed = false)
  - EXISTS TransactionPrediction WHERE:
      - TransactionId = Transaction.Id
      - UserId = current user
      - Status = Confirmed
ORDER BY TransactionDate DESC
```

**Reimbursability Precedence**:
1. If `IsManualOverride = true` AND `Status = Confirmed` → User override (highest priority)
2. If `IsManualOverride = false` AND `Status = Confirmed` → AI prediction confirmed
3. No confirmed prediction → Not reimbursable (excluded from missing receipts)

## Validation Rules

| Field | Rule |
|-------|------|
| ReceiptUrl | Max length: 2048 characters (URL limit) |
| ReceiptUrl | No format validation (per spec clarification) |
| ReceiptDismissed | Boolean or null only |
| Page | Must be ≥ 1 |
| PageSize | Must be 1-100, defaults to 25 |

## State Transitions

### Transaction.ReceiptDismissed

```
null ←────────────────────→ true
 │                            │
 │   Dismiss action           │
 │   ───────────────────────▶ │
 │                            │
 │   Restore action           │
 │   ◀─────────────────────── │
 │                            │
 ▼                            ▼
Appears in          Hidden from
missing receipts    missing receipts
```

### Transaction with Receipt Upload

```
Missing Receipt ──────▶ Receipt Uploaded ──────▶ Auto-Matched
(MatchedReceiptId=null)   (processing)         (MatchedReceiptId set)
                                                     │
                                                     ▼
                                               Removed from
                                               missing receipts
                                               (query excludes)
```

## Indexes

| Index Name | Columns | Condition | Purpose |
|------------|---------|-----------|---------|
| IX_Transactions_UserId_MissingReceipt | UserId, MatchedReceiptId, ReceiptDismissed | WHERE MatchedReceiptId IS NULL | Efficient missing receipt queries |
| IX_TransactionPredictions_Missing | TransactionId, UserId, Status | WHERE Status = 1 | Efficient reimbursability check |
