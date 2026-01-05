# Data Model: Vendor Name Extraction

**Feature**: 025-vendor-extraction
**Date**: 2026-01-05

## Summary

This feature requires **no new entities or schema changes**. It uses existing data structures to extract vendor names from transaction descriptions.

## Existing Entities (No Changes)

### VendorAlias

Already exists with all required fields for vendor extraction:

```
VendorAlias
├── Id: Guid (PK)
├── CanonicalName: string (internal identifier)
├── DisplayName: string (human-readable name - used for extraction output)
├── AliasPattern: string (pattern to match in descriptions)
├── Confidence: decimal (0.00-1.00, match quality)
├── MatchCount: int (incremented on each match)
├── LastMatchedAt: DateTime? (updated on each match)
├── DefaultGLCode: string? (for categorization suggestions)
├── DefaultDepartment: string? (for categorization suggestions)
├── GLConfirmCount: int (learning threshold counter)
├── DeptConfirmCount: int (learning threshold counter)
└── Category: VendorCategory enum (Standard, Airline, Hotel, Subscription)
```

### Transaction

Already contains the description field:

```
Transaction
├── Id: Guid (PK)
├── Description: string (raw bank description - input for extraction)
├── ... (other fields unchanged)
```

### TransactionCategorizationDto

Already contains the Vendor field:

```
TransactionCategorizationDto
├── TransactionId: Guid
├── NormalizedDescription: string
├── Vendor: string (currently set to Description; will be set to DisplayName)
├── GL: GLCategorizationSection
└── Department: DepartmentCategorizationSection
```

## Data Flow

```
[Transaction.Description]
         │
         ▼
┌─────────────────────────────┐
│  VendorAliasService         │
│  FindMatchingAliasAsync()   │
└─────────────────────────────┘
         │
         ▼
    ┌────┴────┐
    │ Match?  │
    └────┬────┘
    Yes  │  No
    ▼    │   ▼
┌────────┐   ┌─────────────────────┐
│ Return │   │ Return original     │
│ Display│   │ transaction.        │
│ Name   │   │ Description         │
└────────┘   └─────────────────────┘
    │
    ▼
[TransactionCategorizationDto.Vendor]
```

## Migrations

**None required** - all tables and columns already exist.

## Indexes

**Existing (no changes)**:
- `ix_vendor_aliases_trigram` - GIN trigram index on `alias_pattern` for fast ILIKE queries

## Seed Data

Vendor aliases are seeded in `20260103000000_SeedVendorAliasesForPredictions.cs` with common vendors:
- Amazon (AMZN, AMAZON.COM, AMZN MKTP)
- Uber (UBER)
- Starbucks (STARBUCKS)
- And approximately 100+ other common vendors

## Validation Rules

All validation already exists in VendorAlias entity:
- `CanonicalName`: Required, non-empty
- `DisplayName`: Required, non-empty
- `AliasPattern`: Required, non-empty
- `Confidence`: Range 0.00-1.00
