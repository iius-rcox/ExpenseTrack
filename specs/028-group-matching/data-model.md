# Data Model: Transaction Group Matching

**Feature**: 028-group-matching
**Date**: 2026-01-07
**Status**: Complete

## Overview

This feature does **not require database schema changes**. All necessary entities and relationships already exist. This document describes how existing entities are used for group matching.

---

## Existing Entities (No Changes Required)

### TransactionGroup

Already has all required properties for matching:

```
TransactionGroup
├── Id: Guid (PK)
├── UserId: Guid (FK → Users)
├── Name: string (e.g., "TWILIO (3 charges)")
├── DisplayDate: DateOnly (for date scoring)
├── IsDateOverridden: bool
├── CombinedAmount: decimal (for amount scoring)
├── TransactionCount: int
├── MatchedReceiptId: Guid? (FK → Receipts)
├── MatchStatus: MatchStatus enum (Unmatched/Proposed/Matched)
├── CreatedAt: DateTime
└── UpdatedAt: DateTime
```

**Used for matching**:
- `CombinedAmount` → Amount scoring (replaces individual transaction.Amount)
- `DisplayDate` → Date scoring (replaces transaction.TransactionDate)
- `Name` → Vendor scoring (extract vendor from name pattern)
- `MatchStatus` → Filter unmatched groups as candidates
- `MatchedReceiptId` → Link to matched receipt when confirmed

### Transaction

Already has GroupId for exclusion filtering:

```
Transaction
├── Id: Guid (PK)
├── UserId: Guid (FK → Users)
├── ...
├── GroupId: Guid? (FK → TransactionGroups)  ← Key field for exclusion
├── MatchStatus: MatchStatus enum
└── MatchedReceiptId: Guid?
```

**Used for matching**:
- `GroupId` → When not null, exclude from individual candidate pool

### ReceiptTransactionMatch

Already has TransactionGroupId column:

```
ReceiptTransactionMatch
├── Id: Guid (PK)
├── ReceiptId: Guid (FK → Receipts)
├── TransactionId: Guid? (FK → Transactions)     ← Null for group matches
├── TransactionGroupId: Guid? (FK → TransactionGroups)  ← Set for group matches
├── UserId: Guid (FK → Users)
├── Status: MatchProposalStatus enum (Proposed/Confirmed/Rejected)
├── ConfidenceScore: decimal (0-100)
├── AmountScore: decimal (0-40)
├── DateScore: decimal (0-35)
├── VendorScore: decimal (0-25)
├── MatchReason: string
├── MatchedVendorAliasId: Guid? (FK → VendorAliases)
├── IsManualMatch: bool
├── ConfirmedAt: DateTime?
├── ConfirmedByUserId: Guid?
└── RowVersion: uint (PostgreSQL xmin)
```

**Database Constraint** (already exists):
```sql
ALTER TABLE receipt_transaction_matches
ADD CONSTRAINT chk_exactly_one_match_target
CHECK (
    (transaction_id IS NOT NULL AND transaction_group_id IS NULL)
    OR (transaction_id IS NULL AND transaction_group_id IS NOT NULL)
);
```

---

## New DTOs (Application Layer Only)

### MatchCandidate (Internal)

Used within MatchingService to unify transactions and groups:

```csharp
internal class MatchCandidate
{
    public Guid Id { get; set; }
    public MatchCandidateType Type { get; set; }  // Transaction or Group
    public decimal Amount { get; set; }            // Amount or CombinedAmount
    public DateOnly Date { get; set; }             // TransactionDate or DisplayDate
    public string VendorPattern { get; set; }      // Extracted vendor name
    public string DisplayName { get; set; }        // Description or GroupName
    public int? TransactionCount { get; set; }     // Only for groups
}

public enum MatchCandidateType
{
    Transaction,
    Group
}
```

### MatchCandidateDto (API Response)

Extends existing proposal response to indicate candidate type:

```csharp
public class MatchCandidateDto
{
    public Guid Id { get; set; }
    public string CandidateType { get; set; }      // "transaction" or "group"
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string DisplayName { get; set; }
    public int? TransactionCount { get; set; }     // Null for transactions
    public decimal ConfidenceScore { get; set; }
    public decimal AmountScore { get; set; }
    public decimal DateScore { get; set; }
    public decimal VendorScore { get; set; }
}
```

---

## Entity Relationships

```
                    ┌─────────────┐
                    │   Receipt   │
                    └──────┬──────┘
                           │
                           │ 1:N (proposals)
                           ▼
              ┌────────────────────────────┐
              │   ReceiptTransactionMatch  │
              │  (TransactionId XOR        │
              │   TransactionGroupId)      │
              └─────────┬──────────────────┘
                        │
          ┌─────────────┴─────────────┐
          │                           │
          ▼                           ▼
   ┌─────────────┐           ┌──────────────────┐
   │ Transaction │           │ TransactionGroup │
   │ (ungrouped) │           │                  │
   └─────────────┘           └────────┬─────────┘
                                      │
                                      │ 1:N (members)
                                      ▼
                              ┌─────────────┐
                              │ Transaction │
                              │ (grouped)   │
                              └─────────────┘
```

**Key Relationships**:
1. A `ReceiptTransactionMatch` links to exactly one of: Transaction OR TransactionGroup
2. A `TransactionGroup` contains 2+ Transactions (via Transaction.GroupId)
3. Grouped transactions are excluded from individual matching

---

## State Transitions

### TransactionGroup.MatchStatus

```
                     ┌──────────────┐
                     │  Unmatched   │◄─────────────────┐
                     └──────┬───────┘                  │
                            │                          │
                            │ Auto-match proposes      │ Reject / Unmatch
                            ▼                          │
                     ┌──────────────┐                  │
                     │   Proposed   │──────────────────┤
                     └──────┬───────┘                  │
                            │                          │
                            │ User confirms            │
                            ▼                          │
                     ┌──────────────┐                  │
                     │   Matched    │──────────────────┘
                     └──────────────┘
```

### Receipt.MatchStatus (unchanged)

Same state machine applies - receipt can be matched to either a transaction or a group.

---

## Indexes

Existing indexes are sufficient:

| Index | Table | Columns | Purpose |
|-------|-------|---------|---------|
| ix_transactions_user_status | transactions | (user_id, match_status) | Filter unmatched transactions |
| ix_transactions_group_id | transactions | (group_id) | Filter out grouped transactions |
| ix_transaction_groups_user_status | transaction_groups | (user_id, match_status) | Filter unmatched groups |
| ix_receipt_transaction_matches_receipt | receipt_transaction_matches | (receipt_id) | Find matches for a receipt |
| ix_receipt_transaction_matches_group | receipt_transaction_matches | (transaction_group_id) | Find matches for a group |

---

## Validation Rules

### For Group Matching Proposals

| Rule | Validation |
|------|------------|
| Group must be unmatched | `group.MatchStatus == MatchStatus.Unmatched` |
| Group must belong to user | `group.UserId == currentUserId` |
| Receipt must be unmatched | `receipt.MatchStatus == MatchStatus.Unmatched` |
| Receipt must have extracted amount | `receipt.AmountExtracted != null` |
| Receipt must have extracted date | `receipt.ReceiptDate != null` |
| Score must meet threshold | `confidenceScore >= 70` |

### For Confirming Group Match

| Rule | Validation |
|------|------------|
| Proposal must exist | Match record found with status = Proposed |
| Group still unmatched | `group.MatchStatus != MatchStatus.Matched` |
| Receipt still unmatched | `receipt.MatchStatus != MatchStatus.Matched` |

---

## Migration Notes

**No database migration required** - all schema elements already exist.

If this were a new feature, the following would be needed:
1. `TransactionGroupId` column on `receipt_transaction_matches` table
2. Check constraint for XOR (TransactionId, TransactionGroupId)
3. Index on `transaction_group_id` for efficient lookups

All of these are already in place from the original TransactionGroup implementation.
