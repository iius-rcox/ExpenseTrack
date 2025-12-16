# Data Model: Matching Engine

**Feature**: 005-matching-engine
**Date**: 2025-12-15

## Entity Overview

This feature introduces one new entity (`ReceiptTransactionMatch`) and modifies two existing entities (`Receipt`, `Transaction`) with status tracking.

```
┌─────────────┐         ┌──────────────────────────┐         ┌─────────────┐
│   Receipt   │────────►│ ReceiptTransactionMatch  │◄────────│ Transaction │
│             │   1:0..1 │                          │ 1:0..1   │             │
│ Status      │         │ Status (Proposed/        │         │ MatchedRcpt │
│ MatchedTxn? │         │  Confirmed/Rejected)     │         │             │
└─────────────┘         │ ConfidenceScore          │         └─────────────┘
                        │ ScoreBreakdown           │
                        │ RowVersion (concurrency) │
                        └────────────┬─────────────┘
                                     │
                                     ▼ 0..1
                        ┌────────────────────────┐
                        │     VendorAlias        │
                        │ (existing - Sprint 2)  │
                        │                        │
                        │ MatchCount++           │
                        │ LastMatchedAt          │
                        │ Confidence             │
                        └────────────────────────┘
```

---

## New Entity: ReceiptTransactionMatch

### Purpose
Links a receipt to a transaction with match metadata, confidence scoring, and audit trail.

### Schema

| Column | Type | Nullable | Default | Description |
|--------|------|----------|---------|-------------|
| Id | uuid | NO | gen_random_uuid() | Primary key |
| ReceiptId | uuid | NO | - | FK to Receipts |
| TransactionId | uuid | NO | - | FK to Transactions |
| UserId | uuid | NO | - | FK to Users (denormalized for queries) |
| Status | smallint | NO | 0 | 0=Proposed, 1=Confirmed, 2=Rejected |
| ConfidenceScore | decimal(5,2) | NO | 0.00 | 0.00-100.00 |
| AmountScore | decimal(5,2) | NO | 0.00 | 0.00-40.00 |
| DateScore | decimal(5,2) | NO | 0.00 | 0.00-35.00 |
| VendorScore | decimal(5,2) | NO | 0.00 | 0.00-25.00 |
| MatchReason | varchar(500) | YES | null | Human-readable explanation |
| MatchedVendorAliasId | uuid | YES | null | FK to VendorAliases (if alias matched) |
| IsManualMatch | boolean | NO | false | True if user manually matched |
| CreatedAt | timestamptz | NO | now() | When match was proposed |
| ConfirmedAt | timestamptz | YES | null | When user confirmed/rejected |
| ConfirmedByUserId | uuid | YES | null | FK to Users who confirmed |
| xmin | xid | NO | (system) | PostgreSQL row version for concurrency |

### Indexes

```sql
-- Primary key
CREATE INDEX pk_receipt_transaction_matches ON receipt_transaction_matches (id);

-- Unique constraint: one receipt can only be matched to one transaction
CREATE UNIQUE INDEX ix_rtm_receipt_confirmed
    ON receipt_transaction_matches (receipt_id)
    WHERE status = 1; -- Only for Confirmed

-- Unique constraint: one transaction can only be matched to one receipt
CREATE UNIQUE INDEX ix_rtm_transaction_confirmed
    ON receipt_transaction_matches (transaction_id)
    WHERE status = 1; -- Only for Confirmed

-- Query patterns
CREATE INDEX ix_rtm_user_status ON receipt_transaction_matches (user_id, status);
CREATE INDEX ix_rtm_receipt ON receipt_transaction_matches (receipt_id);
CREATE INDEX ix_rtm_transaction ON receipt_transaction_matches (transaction_id);
```

### Constraints

```sql
-- One-to-one matching (FR-010, FR-011) enforced via partial unique indexes above
-- Plus application-level check before insert

-- Confidence must be in valid range
ALTER TABLE receipt_transaction_matches
    ADD CONSTRAINT chk_confidence_range
    CHECK (confidence_score >= 0 AND confidence_score <= 100);

-- Status must be valid enum value
ALTER TABLE receipt_transaction_matches
    ADD CONSTRAINT chk_status_valid
    CHECK (status IN (0, 1, 2));
```

### State Transitions

```
                    ┌─────────────┐
                    │   [Start]   │
                    └──────┬──────┘
                           │
                           ▼
                    ┌─────────────┐
        ┌──────────►│  Proposed   │◄──────────┐
        │           └──────┬──────┘           │
        │                  │                  │
        │    User Confirms │  User Rejects   │
        │                  │                  │
        │                  ▼                  │
        │    ┌─────────────┴─────────────┐   │
        │    │                           │   │
        │    ▼                           ▼   │
        │ ┌─────────────┐         ┌─────────────┐
        │ │  Confirmed  │         │  Rejected   │
        │ └─────────────┘         └──────┬──────┘
        │                                │
        │                                │ Re-run auto-match
        └────────────────────────────────┘ (creates new Proposed)
```

**Note**: Both Confirmed and Rejected records are retained for audit. Rejected matches can be re-proposed in future auto-match runs.

---

## Modified Entity: Receipt

### New/Modified Fields

| Column | Type | Change | Description |
|--------|------|--------|-------------|
| MatchedTransactionId | uuid | **NEW** | FK to Transactions (nullable) |
| MatchStatus | smallint | **NEW** | 0=Unmatched, 1=Proposed, 2=Matched |

### Migration

```sql
ALTER TABLE receipts
    ADD COLUMN matched_transaction_id uuid REFERENCES transactions(id),
    ADD COLUMN match_status smallint NOT NULL DEFAULT 0;

CREATE INDEX ix_receipts_match_status ON receipts (user_id, match_status);
```

---

## Modified Entity: Transaction

### Existing Fields (from Sprint 4)

The `Transaction` entity already has `MatchedReceiptId` (nullable FK to Receipts). This sprint activates its usage.

| Column | Type | Change | Description |
|--------|------|--------|-------------|
| MatchedReceiptId | uuid | EXISTS | FK to Receipts (set on confirmation) |
| MatchStatus | smallint | **NEW** | 0=Unmatched, 1=Proposed, 2=Matched |

### Migration

```sql
ALTER TABLE transactions
    ADD COLUMN match_status smallint NOT NULL DEFAULT 0;

CREATE INDEX ix_transactions_match_status ON transactions (user_id, match_status);
```

---

## Modified Entity: VendorAlias

### Fields Used by Matching (existing from Sprint 2)

| Column | Type | Usage |
|--------|------|-------|
| CanonicalName | varchar(255) | Normalized vendor name |
| AliasPattern | varchar(500) | Pattern for LIKE matching |
| DisplayName | varchar(255) | UI display |
| DefaultGLCode | varchar(10) | Pre-populate on match |
| DefaultDepartment | varchar(20) | Pre-populate on match |
| MatchCount | int | Incremented on confirmation |
| LastMatchedAt | timestamptz | Updated on confirmation |
| Confidence | decimal(3,2) | Decayed by background job |

**No schema changes required** - only usage by matching service.

---

## Enum: MatchStatus

```csharp
namespace ExpenseFlow.Shared.Enums;

public enum MatchStatus : short
{
    Unmatched = 0,   // Receipt/Transaction has no match
    Proposed = 1,    // Auto-match proposed, awaiting review
    Matched = 2      // User confirmed the match
}
```

---

## Migration Script

```sql
-- Migration: 20251215_AddReceiptTransactionMatch

-- Create MatchStatus enum tracking on receipts
ALTER TABLE receipts
    ADD COLUMN matched_transaction_id uuid REFERENCES transactions(id),
    ADD COLUMN match_status smallint NOT NULL DEFAULT 0;

-- Create MatchStatus enum tracking on transactions
ALTER TABLE transactions
    ADD COLUMN match_status smallint NOT NULL DEFAULT 0;

-- Create match tracking table
CREATE TABLE receipt_transaction_matches (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    receipt_id uuid NOT NULL REFERENCES receipts(id) ON DELETE CASCADE,
    transaction_id uuid NOT NULL REFERENCES transactions(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES users(id),
    status smallint NOT NULL DEFAULT 0,
    confidence_score decimal(5,2) NOT NULL DEFAULT 0,
    amount_score decimal(5,2) NOT NULL DEFAULT 0,
    date_score decimal(5,2) NOT NULL DEFAULT 0,
    vendor_score decimal(5,2) NOT NULL DEFAULT 0,
    match_reason varchar(500),
    matched_vendor_alias_id uuid REFERENCES vendor_aliases(id),
    is_manual_match boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    confirmed_at timestamptz,
    confirmed_by_user_id uuid REFERENCES users(id),

    CONSTRAINT chk_confidence_range CHECK (confidence_score >= 0 AND confidence_score <= 100),
    CONSTRAINT chk_status_valid CHECK (status IN (0, 1, 2))
);

-- Indexes
CREATE INDEX ix_rtm_user_status ON receipt_transaction_matches (user_id, status);
CREATE INDEX ix_rtm_receipt ON receipt_transaction_matches (receipt_id);
CREATE INDEX ix_rtm_transaction ON receipt_transaction_matches (transaction_id);
CREATE INDEX ix_receipts_match_status ON receipts (user_id, match_status);
CREATE INDEX ix_transactions_match_status ON transactions (user_id, match_status);

-- Partial unique indexes for one-to-one constraint (only confirmed matches)
CREATE UNIQUE INDEX ix_rtm_receipt_confirmed
    ON receipt_transaction_matches (receipt_id)
    WHERE status = 1;

CREATE UNIQUE INDEX ix_rtm_transaction_confirmed
    ON receipt_transaction_matches (transaction_id)
    WHERE status = 1;
```

---

## Entity Relationships Summary

| From | To | Relationship | FK Column |
|------|-----|--------------|-----------|
| ReceiptTransactionMatch | Receipt | Many-to-One | receipt_id |
| ReceiptTransactionMatch | Transaction | Many-to-One | transaction_id |
| ReceiptTransactionMatch | User | Many-to-One | user_id |
| ReceiptTransactionMatch | VendorAlias | Many-to-One (optional) | matched_vendor_alias_id |
| Receipt | Transaction | One-to-One (optional) | matched_transaction_id |
| Transaction | Receipt | One-to-One (optional) | matched_receipt_id |

---

## Data Volume Estimates

Based on 20 users, ~500 receipts/month, ~1000 transactions/month:

| Table | Rows/Month | Rows/Year | Growth Rate |
|-------|------------|-----------|-------------|
| ReceiptTransactionMatches | ~8,000 | ~100,000 | Moderate |
| Receipts (new columns) | - | - | No additional rows |
| Transactions (new columns) | - | - | No additional rows |

**Storage Impact**: Minimal (~10MB/year for match records)
