# Data Model: Receipt Unmatch & Transaction Match Display Fix

**Feature**: 031-receipt-unmatch-fix
**Date**: 2026-01-12

## Overview

This feature adds a new DTO (`MatchedTransactionInfoDto`) and extends `ReceiptDetailDto` to include matched transaction information. No database schema changes required.

## New DTOs

### MatchedTransactionInfoDto

Mirrors the existing `MatchedReceiptInfoDto` pattern for symmetry.

```csharp
namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Information about a matched transaction (displayed on receipt detail page).
/// </summary>
public class MatchedTransactionInfoDto
{
    /// <summary>
    /// The match record ID (needed for unmatch operations).
    /// </summary>
    public Guid MatchId { get; set; }

    /// <summary>
    /// The transaction ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Transaction date.
    /// </summary>
    public DateOnly TransactionDate { get; set; }

    /// <summary>
    /// Transaction description (normalized).
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Transaction amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Merchant name (if available).
    /// </summary>
    public string? MerchantName { get; set; }

    /// <summary>
    /// Match confidence score (0-1).
    /// </summary>
    public decimal MatchConfidence { get; set; }
}
```

## Modified DTOs

### ReceiptDetailDto (Extended)

```csharp
public class ReceiptDetailDto : ReceiptSummaryDto
{
    // ... existing properties ...

    /// <summary>
    /// Matched transaction details (null if not matched).
    /// </summary>
    public MatchedTransactionInfoDto? MatchedTransaction { get; set; }

    /// <summary>
    /// Whether this receipt has a matched transaction.
    /// </summary>
    public bool HasMatchedTransaction => MatchedTransaction != null;
}
```

## Frontend Type Additions

### api.ts

```typescript
export interface MatchedTransactionInfo {
  matchId: string
  id: string
  transactionDate: string // ISO date
  description: string
  amount: number
  merchantName: string | null
  matchConfidence: number // 0-1 scale (normalized from backend 0-100)
}

export interface ReceiptDetail extends ReceiptSummary {
  // ... existing properties ...
  matchedTransaction: MatchedTransactionInfo | null
}
```

## Entity Relationships

```
┌─────────────────────┐         ┌──────────────────────────┐         ┌─────────────────────┐
│      Receipt        │◄────────│  ReceiptTransactionMatch │────────►│    Transaction      │
├─────────────────────┤         ├──────────────────────────┤         ├─────────────────────┤
│ Id (PK)             │         │ Id (PK)                  │         │ Id (PK)             │
│ Vendor              │         │ ReceiptId (FK)           │         │ TransactionDate     │
│ Date                │         │ TransactionId (FK)       │         │ Description         │
│ Amount              │         │ Status                   │         │ Amount              │
│ ...                 │         │ ConfidenceScore          │         │ MerchantName        │
└─────────────────────┘         │ ...                      │         │ ...                 │
                                └──────────────────────────┘         └─────────────────────┘
```

**No schema changes required** - existing `ReceiptTransactionMatch` table already contains all needed data.

## Query Changes

### ReceiptRepository.GetByIdAsync

Current query returns receipt without match data. Needs to be extended:

```csharp
// Add Include for match and related transaction
var receipt = await _context.Receipts
    .Include(r => r.ReceiptTransactionMatches
        .Where(m => m.Status == MatchProposalStatus.Confirmed))
    .ThenInclude(m => m.Transaction)
    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
```

## Mapping Logic

```csharp
// In ReceiptRepository or mapping extension
MatchedTransaction = receipt.ReceiptTransactionMatches
    .Where(m => m.Status == MatchProposalStatus.Confirmed)
    .Select(m => new MatchedTransactionInfoDto
    {
        MatchId = m.Id,
        Id = m.TransactionId ?? m.TransactionGroupId ?? Guid.Empty,
        TransactionDate = m.Transaction?.TransactionDate ?? DateOnly.MinValue,
        Description = m.Transaction?.Description ?? m.TransactionGroup?.Name ?? "",
        Amount = m.Transaction?.Amount ?? m.TransactionGroup?.CombinedAmount ?? 0,
        MerchantName = m.Transaction?.MerchantName,
        MatchConfidence = m.ConfidenceScore / 100m // Normalize to 0-1
    })
    .FirstOrDefault()
```

## Validation Rules

- `MatchedTransaction` is null when receipt has no confirmed match
- `MatchConfidence` is normalized to 0-1 scale for frontend consistency
- If matched to a `TransactionGroup`, use group's combined amount and name
