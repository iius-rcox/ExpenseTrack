# ExpenseFlow Database Entities Documentation

This document provides a comprehensive reference for all ExpenseFlow database entities and their relationships. It is designed to support testing, development, and data modeling efforts.

**Technology Stack**:
- PostgreSQL 15+ (Supabase self-hosted)
- Entity Framework Core 8 with Npgsql
- pgvector extension (for embedding similarity search)

---

## Table of Contents

1. [Entity Relationship Diagram](#entity-relationship-diagram)
2. [Core Entities](#core-entities)
3. [Receipt Pipeline Entities](#receipt-pipeline-entities)
4. [Statement Import Entities](#statement-import-entities)
5. [Matching Entities](#matching-entities)
6. [Categorization Entities](#categorization-entities)
7. [Advanced Features Entities](#advanced-features-entities)
8. [Reporting Entities](#reporting-entities)
9. [Reference Data Entities](#reference-data-entities)
10. [User Preferences](#user-preferences)
11. [Enums](#enums)

---

## Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                           USER DOMAIN                                │
├─────────────────────────────────────────────────────────────────────┤
│  User                                                                │
│    ├── 1:N Receipts                                                  │
│    ├── 1:N Transactions                                              │
│    ├── 1:N ExpenseReports                                            │
│    ├── 1:N TravelPeriods                                             │
│    ├── 1:N DetectedSubscriptions                                     │
│    ├── 1:N StatementFingerprints                                     │
│    ├── 1:N ExpenseEmbeddings                                         │
│    ├── 1:N ImportJobs                                                │
│    └── 1:1 UserPreferences                                           │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                        RECEIPT PIPELINE                              │
├─────────────────────────────────────────────────────────────────────┤
│  Receipt ────────────────────┬──── 1:1 Transaction (matched)        │
│    └── 1:N ReceiptLineItems  │                                       │
│                              │                                       │
│  ReceiptTransactionMatch ────┘                                       │
│    └── N:1 VendorAlias                                               │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                       STATEMENT IMPORT                               │
├─────────────────────────────────────────────────────────────────────┤
│  StatementImport                                                     │
│    ├── 1:N Transactions                                              │
│    └── N:1 StatementFingerprint                                      │
│                                                                      │
│  StatementFingerprint                                                │
│    └── N:1 User                                                      │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                        CATEGORIZATION                                │
├─────────────────────────────────────────────────────────────────────┤
│  VendorAlias                                                         │
│    └── 1:N SplitPatterns                                             │
│                                                                      │
│  ExpenseEmbedding (pgvector)                                         │
│    └── N:1 User                                                      │
│                                                                      │
│  DescriptionCache                                                    │
│                                                                      │
│  TierUsageLog                                                        │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                       REFERENCE DATA                                 │
├─────────────────────────────────────────────────────────────────────┤
│  GLAccount                                                           │
│  Department                                                          │
│  Project                                                             │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Core Entities

### User

Represents an authenticated employee in the system.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| EntraObjectId | varchar | No | Azure AD object ID from JWT 'oid' claim |
| Email | varchar | No | Email from JWT 'preferred_username' |
| DisplayName | varchar | No | Display name from JWT 'name' claim |
| Department | varchar | Yes | Department from JWT or manual entry |
| LastLoginAt | timestamp | No | Most recent authentication timestamp |
| CreatedAt | timestamp | No | Record creation timestamp |

**Relationships**:
- 1:N StatementFingerprints
- 1:1 UserPreferences (optional)

**Unique Constraints**:
- `EntraObjectId` (unique)
- `Email` (unique)

---

## Receipt Pipeline Entities

### Receipt

Represents an uploaded receipt document with OCR extraction results.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| UserId | uuid | No | FK to Users |
| BlobUrl | varchar | No | Full URL to blob in Azure Storage |
| ThumbnailUrl | varchar | Yes | URL to 200x200 thumbnail |
| OriginalFilename | varchar | No | Original uploaded filename |
| ContentType | varchar | No | MIME type |
| FileSize | bigint | No | File size in bytes |
| Status | enum | No | Processing status |
| VendorExtracted | varchar | Yes | OCR-extracted vendor name |
| DateExtracted | date | Yes | OCR-extracted transaction date |
| AmountExtracted | decimal | Yes | OCR-extracted total amount |
| TaxExtracted | decimal | Yes | OCR-extracted tax amount |
| Currency | varchar(3) | No | ISO 4217 currency code (default: USD) |
| LineItems | jsonb | Yes | Array of extracted line items |
| ConfidenceScores | jsonb | Yes | Field-level confidence scores |
| ErrorMessage | varchar | Yes | Error description if extraction failed |
| RetryCount | int | No | Number of extraction retry attempts |
| PageCount | int | No | Number of pages in document |
| CreatedAt | timestamp | No | Upload timestamp |
| ProcessedAt | timestamp | Yes | Extraction completion timestamp |
| MatchedTransactionId | uuid | Yes | FK to Transactions (when matched) |
| MatchStatus | enum | No | Unmatched/Proposed/Matched |

**Indexes**:
- `IX_Receipts_UserId` (user lookup)
- `IX_Receipts_Status` (status filtering)
- `IX_Receipts_MatchStatus` (matching workflow)

### ReceiptLineItem

Represents an individual line item extracted from a receipt.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Description | varchar | Yes | Item description |
| Quantity | decimal | Yes | Item quantity |
| UnitPrice | decimal | Yes | Price per unit |
| TotalPrice | decimal | Yes | Line total |

*Note: Stored as JSONB array within Receipt entity*

---

## Statement Import Entities

### StatementImport

Represents a batch of transactions imported from a bank/credit card statement.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| UserId | uuid | No | FK to Users |
| FingerprintId | uuid | Yes | FK to StatementFingerprints |
| FileName | varchar | No | Original statement filename |
| FileSize | bigint | No | File size in bytes |
| TierUsed | int | No | Inference tier (1=fingerprint, 3=AI) |
| TransactionCount | int | No | Number of transactions imported |
| SkippedCount | int | No | Rows skipped during import |
| DuplicateCount | int | No | Duplicate transactions found |
| CreatedAt | timestamp | No | Import timestamp |

**Relationships**:
- N:1 StatementFingerprint
- 1:N Transactions

### StatementFingerprint

Template for recognizing and mapping statement formats.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| UserId | uuid | Yes | FK to Users (null for system fingerprints) |
| SourceName | varchar | No | User-friendly name (e.g., "Chase Visa") |
| HeaderHash | varchar | No | SHA-256 hash of column headers |
| ColumnMapping | jsonb | No | Column-to-field mapping |
| DateFormat | varchar | No | Date parsing format |
| AmountSign | varchar | No | positive_charges/negative_charges |
| IsSystem | bool | No | True for built-in fingerprints |
| HitCount | int | No | Number of times used |
| LastUsedAt | timestamp | Yes | Most recent use |
| CreatedAt | timestamp | No | Creation timestamp |

**Indexes**:
- `IX_Fingerprints_HeaderHash` (quick lookup)
- `IX_Fingerprints_UserId` (user fingerprints)

### Transaction

Represents an imported credit card transaction.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| UserId | uuid | No | FK to Users |
| ImportId | uuid | No | FK to StatementImports |
| TransactionDate | date | No | Transaction date |
| PostDate | date | Yes | Post date (optional) |
| Description | varchar | No | Normalized description |
| OriginalDescription | varchar | No | Raw description from statement |
| Amount | decimal | No | Amount (positive=expense, negative=credit) |
| DuplicateHash | varchar | No | SHA-256 for duplicate detection |
| MatchedReceiptId | uuid | Yes | FK to Receipts (when matched) |
| MatchStatus | enum | No | Unmatched/Proposed/Matched |
| CreatedAt | timestamp | No | Import timestamp |

**Indexes**:
- `IX_Transactions_UserId_Date` (user timeline)
- `IX_Transactions_DuplicateHash` (duplicate check)
- `IX_Transactions_MatchStatus` (matching workflow)

---

## Matching Entities

### ReceiptTransactionMatch

Tracks proposed and confirmed matches between receipts and transactions.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| ReceiptId | uuid | No | FK to Receipts |
| TransactionId | uuid | No | FK to Transactions |
| ConfidenceScore | decimal | No | Overall match confidence (0-1) |
| AmountScore | decimal | No | Amount match score (0-1) |
| DateScore | decimal | No | Date match score (0-1) |
| VendorScore | decimal | No | Vendor match score (0-1) |
| MatchReason | varchar | No | Human-readable match explanation |
| Status | enum | No | Proposed/Confirmed/Rejected |
| MatchedVendorAliasId | uuid | Yes | FK to VendorAliases |
| IsManualMatch | bool | No | True if user-created match |
| CreatedAt | timestamp | No | Proposal timestamp |
| ConfirmedAt | timestamp | Yes | Confirmation timestamp |

**Indexes**:
- `IX_Matches_ReceiptId` (receipt lookup)
- `IX_Matches_TransactionId` (transaction lookup)
- `IX_Matches_Status` (workflow filtering)

---

## Categorization Entities

### VendorAlias

Maps transaction description patterns to canonical vendor names with default categorization.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| CanonicalName | varchar | No | Standardized vendor name (uppercase) |
| AliasPattern | varchar | No | Pattern to match in descriptions |
| DisplayName | varchar | No | Human-readable vendor name |
| DefaultGLCode | varchar | Yes | Default GL code for this vendor |
| DefaultDepartment | varchar | Yes | Default department |
| GLConfirmCount | int | No | Times user confirmed GL code |
| DeptConfirmCount | int | No | Times user confirmed department |
| MatchCount | int | No | Number of times alias matched |
| LastMatchedAt | timestamp | Yes | Most recent match |
| Confidence | decimal | No | Confidence score (0-1) |
| Category | enum | No | Vendor category (Standard/Airline/Hotel/Subscription) |
| CreatedAt | timestamp | No | Creation timestamp |

**Relationships**:
- 1:N SplitPatterns

**Indexes**:
- `IX_VendorAliases_CanonicalName` (lookup)
- `IX_VendorAliases_AliasPattern` (pattern matching)

### ExpenseEmbedding

Stores vector embeddings for expense descriptions with associated categorization.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| UserId | uuid | No | FK to Users |
| TransactionId | uuid | Yes | FK to Transactions |
| ExpenseLineId | uuid | Yes | FK to ExpenseLines (future) |
| VendorNormalized | varchar | Yes | Normalized vendor name |
| DescriptionText | varchar | No | Description that was embedded |
| GLCode | varchar | Yes | Associated GL code |
| Department | varchar | Yes | Associated department |
| Embedding | vector(1536) | No | text-embedding-3-small vector |
| Verified | bool | No | Whether user verified categorization |
| ExpiresAt | timestamp | Yes | Auto-purge date (null=never) |
| CreatedAt | timestamp | No | Creation timestamp |

**Indexes**:
- `IX_Embeddings_UserId` (user filtering)
- `IX_Embeddings_Verified` (verified lookup)
- HNSW index on `Embedding` for vector similarity

### DescriptionCache

Caches normalized descriptions for Tier 1 lookups.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| RawDescriptionHash | varchar | No | SHA-256 of uppercase description |
| RawDescription | varchar | No | Original description |
| NormalizedDescription | varchar | No | Cleaned/normalized version |
| HitCount | int | No | Cache access count |
| LastAccessedAt | timestamp | No | Most recent access |
| CreatedAt | timestamp | No | Creation timestamp |

**Indexes**:
- `IX_DescriptionCache_Hash` (unique, for lookup)

### TierUsageLog

Tracks AI tier usage for cost monitoring and analytics.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| UserId | uuid | No | FK to Users |
| OperationType | varchar | No | Operation (GL, Dept, Normalization) |
| Tier | int | No | Tier used (1, 2, or 3) |
| Timestamp | timestamp | No | When operation occurred |
| Metadata | jsonb | Yes | Additional context |

**Indexes**:
- `IX_TierUsage_UserId_Timestamp` (user analytics)
- `IX_TierUsage_Tier` (tier breakdown)

---

## Advanced Features Entities

### TravelPeriod

Represents a detected or manually created travel period.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| UserId | uuid | No | FK to Users |
| Description | varchar | Yes | Trip description |
| StartDate | date | No | Trip start date |
| EndDate | date | No | Trip end date |
| Destination | varchar | Yes | Travel destination |
| DetectionSource | enum | No | Manual/Automatic |
| LinkedReceiptIds | jsonb | Yes | Array of related receipt IDs |
| CreatedAt | timestamp | No | Creation timestamp |
| UpdatedAt | timestamp | Yes | Last modification |

### DetectedSubscription

Tracks recurring charges identified through pattern analysis.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| UserId | uuid | No | FK to Users |
| VendorAliasId | uuid | Yes | FK to VendorAliases |
| VendorName | varchar | No | Display name of vendor |
| AverageAmount | decimal | No | Average charge amount |
| OccurrenceMonths | jsonb | No | Array of YYYY-MM strings |
| LastSeenDate | date | No | Most recent charge date |
| ExpectedNextDate | date | Yes | Calculated next expected date |
| Status | enum | No | Active/Missing/Flagged |
| DetectionSource | enum | No | PatternMatch/SeedData |
| UpdatedAt | timestamp | Yes | Last modification |
| CreatedAt | timestamp | No | Creation timestamp |

### KnownSubscriptionVendor

Seed data for subscription detection.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| VendorPattern | varchar | No | Pattern to match in descriptions |
| DisplayName | varchar | No | Human-readable name |
| Category | varchar | Yes | Subscription category |
| TypicalAmount | decimal | Yes | Expected charge amount |
| Frequency | enum | No | Monthly/Weekly/Yearly |

### SplitPattern

Defines expense allocation rules for multi-department expenses.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| UserId | uuid | No | FK to Users |
| VendorAliasId | uuid | Yes | FK to VendorAliases |
| Name | varchar | No | Pattern name |
| Allocations | jsonb | No | Array of allocations |
| CreatedAt | timestamp | No | Creation timestamp |
| UpdatedAt | timestamp | Yes | Last modification |

### ImportJob

Tracks cache warming import jobs.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| UserId | uuid | No | FK to Users |
| BlobUrl | varchar | No | URL to uploaded Excel file |
| SourceFileName | varchar | No | Original filename |
| Status | enum | No | Pending/Processing/Completed/Failed/Cancelled |
| TotalRecords | int | No | Total rows in file |
| ProcessedRecords | int | No | Rows processed |
| CachedDescriptions | int | No | Descriptions added to cache |
| CreatedAliases | int | No | Vendor aliases created |
| GeneratedEmbeddings | int | No | Embeddings generated |
| SkippedRecords | int | No | Rows skipped (errors) |
| ErrorLog | varchar(10000) | Yes | JSON array of errors |
| CompletedAt | timestamp | Yes | Completion timestamp |
| CreatedAt | timestamp | No | Job creation timestamp |

---

## Reporting Entities

### ExpenseReport

Represents a draft expense report for a specific period.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| UserId | uuid | No | FK to Users |
| Period | varchar(7) | No | YYYY-MM format |
| Status | enum | No | Draft/Submitted/Approved/Rejected |
| TotalAmount | decimal | No | Sum of all expense lines |
| LineCount | int | No | Number of expense lines |
| MissingReceiptCount | int | No | Lines without receipts |
| Tier1HitCount | int | No | Suggestions from cache |
| Tier2HitCount | int | No | Suggestions from embeddings |
| Tier3HitCount | int | No | Suggestions from AI |
| IsDeleted | bool | No | Soft delete flag |
| UpdatedAt | timestamp | Yes | Last modification |
| RowVersion | uint | No | PostgreSQL xmin for locking |
| CreatedAt | timestamp | No | Creation timestamp |

**Relationships**:
- 1:N ExpenseLines

### ExpenseLine

Represents a single line item in an expense report.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| ReportId | uuid | No | FK to ExpenseReports |
| TransactionId | uuid | Yes | FK to Transactions |
| ReceiptId | uuid | Yes | FK to Receipts |
| ExpenseDate | date | No | Date of expense |
| Description | varchar | No | Expense description |
| Amount | decimal | No | Expense amount |
| GLCode | varchar | Yes | GL account code |
| DepartmentCode | varchar | Yes | Department code |
| ProjectCode | varchar | Yes | Project code |
| Justification | varchar | Yes | Business justification |
| CategoryConfidence | decimal | Yes | AI suggestion confidence |
| CategorizationTier | int | Yes | Tier used (1/2/3) |
| SequenceNumber | int | No | Order within report |
| CreatedAt | timestamp | No | Creation timestamp |

---

## Reference Data Entities

### GLAccount

General ledger accounts synced from Viewpoint Vista.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| Code | varchar | No | GL account code |
| Name | varchar | No | Account name |
| Description | varchar | Yes | Account description |
| IsActive | bool | No | Active status |
| VistaId | int | No | Source ID from Vista |
| LastSyncedAt | timestamp | No | Last sync timestamp |

### Department

Departments synced from Viewpoint Vista.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| Code | varchar | No | Department code |
| Name | varchar | No | Department name |
| IsActive | bool | No | Active status |
| VistaId | int | No | Source ID from Vista |
| LastSyncedAt | timestamp | No | Last sync timestamp |

### Project

Projects/jobs synced from Viewpoint Vista.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| Code | varchar | No | Project/job code |
| Name | varchar | No | Project name |
| IsActive | bool | No | Active status |
| VistaId | int | No | Source ID from Vista |
| LastSyncedAt | timestamp | No | Last sync timestamp |

---

## User Preferences

### UserPreferences

Stores user application preferences.

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| Id | uuid | No | Primary key |
| UserId | uuid | No | FK to Users (unique) |
| Theme | varchar | No | light/dark/system |
| DefaultDepartmentId | uuid | Yes | FK to Departments |
| DefaultProjectId | uuid | Yes | FK to Projects |
| CreatedAt | timestamp | No | Creation timestamp |
| UpdatedAt | timestamp | Yes | Last modification |

**Constraints**:
- Unique on `UserId`
- FK cascade delete when user is removed

---

## Enums

### ReceiptStatus

| Value | Description |
|-------|-------------|
| Uploaded | Awaiting processing |
| Processing | OCR in progress |
| Ready | Extraction complete, high confidence |
| ReviewRequired | Extraction complete, low confidence |
| Error | Processing failed |

### MatchStatus

| Value | Description |
|-------|-------------|
| Unmatched | No match found/proposed |
| Proposed | Proposed match pending review |
| Matched | Match confirmed |

### ReportStatus

| Value | Description |
|-------|-------------|
| Draft | In progress |
| Submitted | Submitted for approval |
| Approved | Approved by manager |
| Rejected | Rejected by manager |

### SubscriptionStatus

| Value | Description |
|-------|-------------|
| Active | Subscription is active |
| Missing | Expected charge not found |
| Flagged | Requires user attention |
| Cancelled | User cancelled tracking |

### VendorCategory

| Value | Description |
|-------|-------------|
| Standard | Regular vendor |
| Airline | Airline/travel |
| Hotel | Hotel/lodging |
| Subscription | Recurring subscription |

### DetectionSource

| Value | Description |
|-------|-------------|
| Manual | User-created |
| PatternMatch | Detected from transaction patterns |
| SeedData | Matched against known vendors |

### ImportJobStatus

| Value | Description |
|-------|-------------|
| Pending | Queued for processing |
| Processing | Currently being processed |
| Completed | Successfully completed |
| Failed | Processing failed |
| Cancelled | User cancelled |

---

## Testing Considerations

### Test Data Cleanup

Use the test cleanup endpoint to reset user data:
```
POST /api/test/cleanup
{
  "entityTypes": ["receipts", "transactions", "matches", "reports"]
}
```

### Data Isolation

All entities are user-scoped (except reference data). Tests should:
1. Create test user via auth mock
2. Operate on that user's data only
3. Clean up after test completion

### Seeding Reference Data

For integration tests, seed minimal reference data:
```sql
INSERT INTO gl_accounts (id, code, name, is_active) VALUES
  (gen_random_uuid(), '5100', 'Office Supplies', true),
  (gen_random_uuid(), '5200', 'Travel', true);

INSERT INTO departments (id, code, name, is_active) VALUES
  (gen_random_uuid(), '100', 'Engineering', true),
  (gen_random_uuid(), '200', 'Sales', true);
```

---

## Version History

| Date | Version | Changes |
|------|---------|---------|
| 2025-12-31 | 1.16.0 | Documentation created |
| 2025-12-29 | 1.15.0 | Added UserPreferences entity |
| 2025-12-23 | 1.12.0 | Added ImportJob entity |
| 2025-12-21 | 1.11.0 | Added DetectedSubscription, SplitPattern |
