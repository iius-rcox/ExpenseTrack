# Data Model: Core Backend & Authentication

**Feature**: 002-core-backend-auth
**Date**: 2025-12-04

## Entity Relationship Diagram

```
┌─────────────────┐       ┌─────────────────────┐
│      User       │       │   DescriptionCache  │
├─────────────────┤       ├─────────────────────┤
│ Id (PK)         │       │ Id (PK)             │
│ EntraObjectId   │       │ RawDescriptionHash  │◄─── Unique Index
│ Email           │       │ RawDescription      │
│ DisplayName     │       │ NormalizedDescription│
│ Department      │       │ HitCount            │
│ CreatedAt       │       │ CreatedAt           │
│ LastLoginAt     │       └─────────────────────┘
└─────────────────┘
        │
        │ 1:N
        ▼
┌─────────────────────┐       ┌─────────────────────┐
│ StatementFingerprint│       │    VendorAlias      │
├─────────────────────┤       ├─────────────────────┤
│ Id (PK)             │       │ Id (PK)             │
│ UserId (FK)         │       │ CanonicalName       │
│ SourceName          │       │ AliasPattern        │◄─── Index
│ HeaderHash          │◄─┐    │ DisplayName         │
│ ColumnMapping (JSON)│  │    │ DefaultGLCode       │
│ DateFormat          │  │    │ DefaultDepartment   │
│ AmountSign          │  │    │ MatchCount          │
│ CreatedAt           │  │    │ LastMatchedAt       │
└─────────────────────┘  │    │ Confidence          │
   Unique(UserId,        │    │ CreatedAt           │
   HeaderHash)───────────┘    └─────────────────────┘
                                      │
                                      │ 1:N
                                      ▼
┌─────────────────────┐       ┌─────────────────────┐
│   ExpenseEmbedding  │       │    SplitPattern     │
├─────────────────────┤       ├─────────────────────┤
│ Id (PK)             │       │ Id (PK)             │
│ ExpenseLineId (FK?) │       │ VendorAliasId (FK)  │
│ VendorNormalized    │       │ SplitConfig (JSON)  │
│ DescriptionText     │       │ UsageCount          │
│ GLCode              │       │ LastUsedAt          │
│ Department          │       │ CreatedAt           │
│ Embedding (VECTOR)  │◄───── IVFFlat Index        │
│ Verified            │       └─────────────────────┘
│ CreatedAt           │
└─────────────────────┘

Reference Data (Read-Only Sync):
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│    GLAccount    │  │   Department    │  │     Project     │
├─────────────────┤  ├─────────────────┤  ├─────────────────┤
│ Id (PK)         │  │ Id (PK)         │  │ Id (PK)         │
│ Code            │  │ Code            │  │ Code            │
│ Name            │  │ Name            │  │ Name            │
│ Description     │  │ Description     │  │ Description     │
│ IsActive        │  │ IsActive        │  │ IsActive        │
│ SyncedAt        │  │ SyncedAt        │  │ SyncedAt        │
└─────────────────┘  └─────────────────┘  └─────────────────┘
```

## Entity Definitions

### User

Represents an authenticated employee in the system.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | UUID | PK, auto-generated | Unique identifier |
| EntraObjectId | string(36) | Unique, Not Null | Azure AD object ID from JWT `oid` claim |
| Email | string(255) | Unique, Not Null | Email from JWT `preferred_username` claim |
| DisplayName | string(255) | Not Null | Display name from JWT `name` claim |
| Department | string(100) | Nullable | Department from JWT custom claim or manual entry |
| CreatedAt | timestamp | Not Null, Default NOW() | First authentication timestamp |
| LastLoginAt | timestamp | Not Null | Most recent authentication timestamp |

**Validation Rules**:
- EntraObjectId must be a valid GUID format
- Email must be valid email format
- CreatedAt <= LastLoginAt

### DescriptionCache

Caches raw-to-normalized description mappings for Tier 1 lookups.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | UUID | PK, auto-generated | Unique identifier |
| RawDescriptionHash | string(64) | Unique, Not Null, Index | SHA-256 hash of raw description |
| RawDescription | string(500) | Not Null | Original transaction description |
| NormalizedDescription | string(500) | Not Null | AI-normalized description |
| HitCount | int | Not Null, Default 0 | Number of cache hits for metrics |
| CreatedAt | timestamp | Not Null, Default NOW() | Entry creation timestamp |

**Validation Rules**:
- RawDescriptionHash must be 64 hex characters (SHA-256)
- RawDescription and NormalizedDescription max 500 chars
- HitCount >= 0

**Behavior**:
- On duplicate insert (same hash), increment HitCount instead

### VendorAlias

Maps transaction description patterns to canonical vendor names.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | UUID | PK, auto-generated | Unique identifier |
| CanonicalName | string(255) | Not Null, Index | Standardized vendor name |
| AliasPattern | string(500) | Not Null, Index | Pattern to match in transaction descriptions |
| DisplayName | string(255) | Not Null | Human-readable vendor name for UI |
| DefaultGLCode | string(10) | Nullable | Default GL code for this vendor |
| DefaultDepartment | string(20) | Nullable | Default department for this vendor |
| MatchCount | int | Not Null, Default 0 | Number of times this alias matched |
| LastMatchedAt | timestamp | Nullable | Most recent match timestamp |
| Confidence | decimal(3,2) | Not Null, Default 1.00 | Confidence score (0.00-1.00) |
| CreatedAt | timestamp | Not Null, Default NOW() | Entry creation timestamp |

**Validation Rules**:
- Confidence between 0.00 and 1.00
- MatchCount >= 0
- AliasPattern should be case-insensitive matchable

### StatementFingerprint

Stores user-specific column mappings for recurring statement imports.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | UUID | PK, auto-generated | Unique identifier |
| UserId | UUID | FK → User.Id, Not Null | Owner of this fingerprint |
| SourceName | string(100) | Not Null | Statement source name (e.g., "Chase Business Card") |
| HeaderHash | string(64) | Not Null | SHA-256 hash of header row |
| ColumnMapping | JSONB | Not Null | Column name → field type mapping |
| DateFormat | string(50) | Nullable | Date format pattern (e.g., "MM/DD/YYYY") |
| AmountSign | string(20) | Not Null, Default 'negative_charges' | 'negative_charges' or 'positive_charges' |
| CreatedAt | timestamp | Not Null, Default NOW() | Entry creation timestamp |

**Constraints**:
- Unique(UserId, HeaderHash)

**ColumnMapping JSON Schema**:
```json
{
  "Transaction Date": "date",
  "Post Date": "post_date",
  "Description": "description",
  "Amount": "amount",
  "Category": "category"
}
```

### SplitPattern

Defines expense allocation rules for vendors requiring split accounting.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | UUID | PK, auto-generated | Unique identifier |
| VendorAliasId | UUID | FK → VendorAlias.Id, Nullable | Associated vendor alias |
| SplitConfig | JSONB | Not Null | Split allocation configuration |
| UsageCount | int | Not Null, Default 0 | Number of times this pattern was used |
| LastUsedAt | timestamp | Nullable | Most recent usage timestamp |
| CreatedAt | timestamp | Not Null, Default NOW() | Entry creation timestamp |

**SplitConfig JSON Schema**:
```json
{
  "allocations": [
    { "glCode": "66300", "department": "07", "percentage": 60 },
    { "glCode": "63300", "department": "07", "percentage": 40 }
  ]
}
```

**Validation Rules**:
- Sum of allocation percentages must equal 100
- UsageCount >= 0

### ExpenseEmbedding

Stores vector embeddings for expense descriptions with associated categorization.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | UUID | PK, auto-generated | Unique identifier |
| ExpenseLineId | UUID | Nullable | Reference to source expense line (future) |
| VendorNormalized | string(255) | Nullable | Normalized vendor name |
| DescriptionText | string(500) | Not Null | Description text that was embedded |
| GLCode | string(10) | Nullable | Associated GL code |
| Department | string(20) | Nullable | Associated department |
| Embedding | VECTOR(1536) | Not Null | text-embedding-3-small vector |
| Verified | boolean | Not Null, Default false | Whether user verified this categorization |
| CreatedAt | timestamp | Not Null, Default NOW() | Entry creation timestamp |

**Indexes**:
- IVFFlat index on Embedding column: `USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100)`

**Validation Rules**:
- Embedding dimension must be exactly 1536

### GLAccount (Reference Data)

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | UUID | PK, auto-generated | Unique identifier |
| Code | string(10) | Unique, Not Null | GL account code (e.g., "66300") |
| Name | string(255) | Not Null | Account name |
| Description | string(500) | Nullable | Account description |
| IsActive | boolean | Not Null, Default true | Whether account is currently valid |
| SyncedAt | timestamp | Not Null | Last sync timestamp from source |

### Department (Reference Data)

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | UUID | PK, auto-generated | Unique identifier |
| Code | string(20) | Unique, Not Null | Department code (e.g., "07") |
| Name | string(255) | Not Null | Department name |
| Description | string(500) | Nullable | Department description |
| IsActive | boolean | Not Null, Default true | Whether department is currently valid |
| SyncedAt | timestamp | Not Null | Last sync timestamp from source |

### Project (Reference Data)

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | UUID | PK, auto-generated | Unique identifier |
| Code | string(50) | Unique, Not Null | Project code |
| Name | string(255) | Not Null | Project name |
| Description | string(500) | Nullable | Project description |
| IsActive | boolean | Not Null, Default true | Whether project is currently valid |
| SyncedAt | timestamp | Not Null | Last sync timestamp from source |

## Database Migrations

### Migration 001: Create Core Tables

```sql
-- Users table
CREATE TABLE users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entra_object_id VARCHAR(36) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    display_name VARCHAR(255) NOT NULL,
    department VARCHAR(100),
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    last_login_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Description cache
CREATE TABLE description_cache (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    raw_description_hash VARCHAR(64) NOT NULL UNIQUE,
    raw_description VARCHAR(500) NOT NULL,
    normalized_description VARCHAR(500) NOT NULL,
    hit_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_description_cache_hash ON description_cache(raw_description_hash);

-- Vendor aliases
CREATE TABLE vendor_aliases (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    canonical_name VARCHAR(255) NOT NULL,
    alias_pattern VARCHAR(500) NOT NULL,
    display_name VARCHAR(255) NOT NULL,
    default_gl_code VARCHAR(10),
    default_department VARCHAR(20),
    match_count INTEGER NOT NULL DEFAULT 0,
    last_matched_at TIMESTAMP,
    confidence DECIMAL(3,2) NOT NULL DEFAULT 1.00,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_vendor_aliases_pattern ON vendor_aliases(alias_pattern);
CREATE INDEX idx_vendor_aliases_canonical ON vendor_aliases(canonical_name);

-- Statement fingerprints
CREATE TABLE statement_fingerprints (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    source_name VARCHAR(100) NOT NULL,
    header_hash VARCHAR(64) NOT NULL,
    column_mapping JSONB NOT NULL,
    date_format VARCHAR(50),
    amount_sign VARCHAR(20) NOT NULL DEFAULT 'negative_charges',
    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_fingerprint UNIQUE (user_id, header_hash)
);

-- Split patterns
CREATE TABLE split_patterns (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vendor_alias_id UUID REFERENCES vendor_aliases(id) ON DELETE SET NULL,
    split_config JSONB NOT NULL,
    usage_count INTEGER NOT NULL DEFAULT 0,
    last_used_at TIMESTAMP,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Expense embeddings (requires pgvector extension)
CREATE TABLE expense_embeddings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    expense_line_id UUID,
    vendor_normalized VARCHAR(255),
    description_text VARCHAR(500) NOT NULL,
    gl_code VARCHAR(10),
    department VARCHAR(20),
    embedding VECTOR(1536) NOT NULL,
    verified BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_expense_embeddings_vector
    ON expense_embeddings USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
```

### Migration 002: Create Reference Tables

```sql
-- GL Accounts
CREATE TABLE gl_accounts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(10) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description VARCHAR(500),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    synced_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Departments
CREATE TABLE departments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(20) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description VARCHAR(500),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    synced_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Projects
CREATE TABLE projects (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description VARCHAR(500),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    synced_at TIMESTAMP NOT NULL DEFAULT NOW()
);
```

## State Transitions

### User Lifecycle
1. **New** → First authentication creates user record
2. **Active** → LastLoginAt updated on each authentication
3. No explicit delete (soft delete via IsActive flag in future if needed)

### Cache Entry Lifecycle
1. **Created** → AI generates normalized description, entry stored
2. **Active** → HitCount incremented on each cache hit
3. No explicit expiration (cache entries persist indefinitely)

### Reference Data Lifecycle
1. **Synced** → Weekly job populates/updates from source
2. **Active** → IsActive = true, available for selection
3. **Inactive** → IsActive = false after sync finds deletion in source
