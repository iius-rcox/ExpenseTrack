# Data Model: AI Categorization (Tiered)

**Feature**: 006-ai-categorization
**Date**: 2025-12-16

## Entity Overview

```
┌─────────────────────┐     ┌─────────────────────┐
│   DescriptionCache  │     │    VendorAlias      │
│   (Tier 1 - Norm)   │     │  (Tier 1 - Cat)     │
└─────────────────────┘     └─────────────────────┘
          │                           │
          │ hash lookup               │ pattern match
          ▼                           ▼
┌─────────────────────────────────────────────────┐
│                  Transaction                     │
│              (from Sprint 4/5)                   │
└─────────────────────────────────────────────────┘
          │                           │
          │ embedding                 │ categorization
          ▼                           ▼
┌─────────────────────┐     ┌─────────────────────┐
│  ExpenseEmbedding   │     │   TierUsageLog      │
│   (Tier 2 - Sim)    │     │   (Metrics)         │
└─────────────────────┘     └─────────────────────┘
```

## Entities

### DescriptionCache (Existing - Sprint 2)

Stores raw-to-normalized description mappings for Tier 1 normalization.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | UUID | PK, default gen_random_uuid() | Unique identifier |
| RawDescriptionHash | VARCHAR(64) | NOT NULL, UNIQUE INDEX | SHA-256 hash of raw description |
| RawDescription | VARCHAR(500) | NOT NULL | Original bank description |
| NormalizedDescription | VARCHAR(500) | NOT NULL | Human-readable normalized text |
| ExtractedVendor | VARCHAR(255) | NULL | Vendor name extracted during normalization |
| HitCount | INTEGER | DEFAULT 0 | Cache hit counter for analytics |
| CreatedAt | TIMESTAMP | DEFAULT NOW() | Record creation time |
| LastAccessedAt | TIMESTAMP | NULL | Last cache hit time |

**Indexes**:
- `idx_desc_cache_hash` UNIQUE on `RawDescriptionHash` (lookup)
- `idx_desc_cache_vendor` on `ExtractedVendor` (vendor-based queries)

### VendorAlias (Existing - Sprint 2, Extended)

Extended to include default GL code and department for Tier 1 categorization.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | UUID | PK | Unique identifier |
| CanonicalName | VARCHAR(255) | NOT NULL | Normalized vendor name |
| AliasPattern | VARCHAR(500) | NOT NULL, INDEX | Pattern for matching descriptions |
| DisplayName | VARCHAR(255) | NOT NULL | User-friendly display name |
| **DefaultGLCode** | VARCHAR(10) | NULL, FK | Default GL code for this vendor |
| **DefaultDepartment** | VARCHAR(20) | NULL, FK | Default department for this vendor |
| **GLConfirmCount** | INTEGER | DEFAULT 0 | Times user confirmed this GL code |
| **DeptConfirmCount** | INTEGER | DEFAULT 0 | Times user confirmed this department |
| MatchCount | INTEGER | DEFAULT 0 | Total match counter |
| LastMatchedAt | TIMESTAMP | NULL | Last pattern match time |
| Confidence | DECIMAL(3,2) | DEFAULT 1.00 | Pattern confidence score |
| CreatedAt | TIMESTAMP | DEFAULT NOW() | Record creation time |
| UserId | UUID | NOT NULL, FK | Owner user (row-level security) |

**Indexes**:
- `idx_vendor_aliases_pattern_gin` GIN on `AliasPattern` (trigram search)
- `idx_vendor_aliases_confidence_matchcount` on `(Confidence DESC, MatchCount DESC)`
- `idx_vendor_aliases_user_canonical` on `(UserId, CanonicalName)`

**Business Rules**:
- When GLConfirmCount >= 3 with same value, update DefaultGLCode automatically
- When DeptConfirmCount >= 3 with same value, update DefaultDepartment automatically

### ExpenseEmbedding (Existing - Sprint 2, Extended)

Vector embeddings for Tier 2 similarity search.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | UUID | PK | Unique identifier |
| DescriptionText | VARCHAR(500) | NOT NULL | Normalized description text |
| Embedding | VECTOR(1536) | NOT NULL | text-embedding-3-small vector |
| VendorNormalized | VARCHAR(255) | NULL | Extracted vendor name |
| GLCode | VARCHAR(10) | NULL, FK | Associated GL code |
| Department | VARCHAR(20) | NULL, FK | Associated department |
| Verified | BOOLEAN | DEFAULT FALSE | User-confirmed categorization |
| TransactionId | UUID | NULL, FK | Source transaction reference |
| UserId | UUID | NOT NULL, FK | Owner user |
| CreatedAt | TIMESTAMP | DEFAULT NOW() | Record creation time |
| **ExpiresAt** | TIMESTAMP | NULL | Auto-purge date (6 months for unverified) |

**Indexes**:
- `idx_embedding_vector` IVFFlat on `Embedding` with vector_cosine_ops (similarity search)
- `idx_embedding_verified` on `(Verified, UserId)` (filter verified first)
- `idx_embedding_expires` on `ExpiresAt` WHERE `ExpiresAt IS NOT NULL` (cleanup job)

**Business Rules**:
- Verified = false: Set ExpiresAt to CreatedAt + 6 months
- Verified = true: Set ExpiresAt to NULL (never expires)
- Similarity threshold: 0.92 (configurable)

### TierUsageLog (New)

Records tier usage for cost monitoring and optimization analytics.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | UUID | PK | Unique identifier |
| UserId | UUID | NOT NULL, FK | User who triggered the operation |
| TransactionId | UUID | NULL, FK | Related transaction (if applicable) |
| OperationType | VARCHAR(50) | NOT NULL | 'normalization', 'gl_suggestion', 'dept_suggestion' |
| TierUsed | INTEGER | NOT NULL | 1, 2, or 3 |
| Confidence | DECIMAL(3,2) | NULL | Result confidence score |
| ResponseTimeMs | INTEGER | NULL | Processing time in milliseconds |
| CacheHit | BOOLEAN | DEFAULT FALSE | Whether result came from cache |
| CreatedAt | TIMESTAMP | DEFAULT NOW() | Operation timestamp |

**Indexes**:
- `idx_tier_usage_user_date` on `(UserId, CreatedAt)` (user analytics)
- `idx_tier_usage_type_tier` on `(OperationType, TierUsed, CreatedAt)` (aggregate reports)

**Partitioning**: Consider monthly partitions if volume exceeds 100K rows/month

### GLAccount (Existing - Sprint 2)

Reference table for valid GL codes, synced from SQL Server.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | UUID | PK | Unique identifier |
| Code | VARCHAR(10) | NOT NULL, UNIQUE | GL account code |
| Name | VARCHAR(255) | NOT NULL | Account name |
| Category | VARCHAR(100) | NULL | Account category (Travel, Supplies, etc.) |
| IsActive | BOOLEAN | DEFAULT TRUE | Whether account is available for selection |
| LastSyncedAt | TIMESTAMP | NULL | Last sync from SQL Server |

### Department (Existing - Sprint 2)

Reference table for valid departments, synced from SQL Server.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | UUID | PK | Unique identifier |
| Code | VARCHAR(20) | NOT NULL, UNIQUE | Department code |
| Name | VARCHAR(255) | NOT NULL | Department name |
| IsActive | BOOLEAN | DEFAULT TRUE | Whether department is available for selection |
| LastSyncedAt | TIMESTAMP | NULL | Last sync from SQL Server |

## State Transitions

### Embedding Lifecycle

```
┌─────────────┐    user confirms    ┌─────────────┐
│  Unverified │ ─────────────────► │  Verified   │
│ (ExpiresAt  │                     │ (ExpiresAt  │
│  = +6mo)    │                     │  = NULL)    │
└─────────────┘                     └─────────────┘
       │
       │ 6 months elapsed
       ▼
┌─────────────┐
│   Purged    │
│  (deleted)  │
└─────────────┘
```

### Vendor Alias GL Code Learning

```
User confirms GL code for vendor
         │
         ▼
┌────────────────────────────┐
│ Increment GLConfirmCount   │
│ Track last confirmed value │
└────────────────────────────┘
         │
         │ GLConfirmCount >= 3 with same value?
         ▼
    ┌────┴────┐
    │  Yes    │  No
    ▼         ▼
┌────────┐  ┌────────┐
│ Update │  │ Keep   │
│Default │  │Existing│
│GLCode  │  │ Value  │
└────────┘  └────────┘
```

## Migration Notes

### New Columns for VendorAlias
```sql
ALTER TABLE vendor_aliases
ADD COLUMN default_gl_code VARCHAR(10) REFERENCES gl_accounts(code),
ADD COLUMN default_department VARCHAR(20) REFERENCES departments(code),
ADD COLUMN gl_confirm_count INTEGER DEFAULT 0,
ADD COLUMN dept_confirm_count INTEGER DEFAULT 0;
```

### New Column for ExpenseEmbedding
```sql
ALTER TABLE expense_embeddings
ADD COLUMN expires_at TIMESTAMP;

-- Set expiry for existing unverified embeddings
UPDATE expense_embeddings
SET expires_at = created_at + INTERVAL '6 months'
WHERE verified = false;

-- Create partial index for cleanup
CREATE INDEX idx_embedding_expires
ON expense_embeddings(expires_at)
WHERE expires_at IS NOT NULL;
```

### New Table TierUsageLog
```sql
CREATE TABLE tier_usage_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id),
    transaction_id UUID REFERENCES transactions(id),
    operation_type VARCHAR(50) NOT NULL,
    tier_used INTEGER NOT NULL CHECK (tier_used BETWEEN 1 AND 4),
    confidence DECIMAL(3,2),
    response_time_ms INTEGER,
    cache_hit BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_tier_usage_user_date ON tier_usage_logs(user_id, created_at);
CREATE INDEX idx_tier_usage_type_tier ON tier_usage_logs(operation_type, tier_used, created_at);
```
