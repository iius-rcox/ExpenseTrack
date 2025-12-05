# Validation Tests: Core Backend & Authentication

**Feature**: 002-core-backend-auth
**Date**: 2025-12-04

## Test Categories

### T1: Authentication Tests

#### T1.1: Unauthenticated Request Rejected
```bash
# Test: Request without token returns 401
curl -X GET https://dev.expense.ii-us.com/api/users/me \
  -H "Content-Type: application/json" \
  -w "\n%{http_code}"

# Expected: 401
# Expected Body: ProblemDetails with title "Unauthorized"
```

#### T1.2: Invalid Token Rejected
```bash
# Test: Request with invalid token returns 401
curl -X GET https://dev.expense.ii-us.com/api/users/me \
  -H "Authorization: Bearer invalid.token.here" \
  -H "Content-Type: application/json" \
  -w "\n%{http_code}"

# Expected: 401
```

#### T1.3: Expired Token Rejected
```bash
# Test: Request with expired token returns 401
# (Use a token generated with short expiry, wait for expiry)
# Expected: 401
```

#### T1.4: Valid Token Accepted
```bash
# Test: Request with valid Entra ID token returns 200
TOKEN=$(az account get-access-token --resource api://{client-id} --query accessToken -o tsv)
curl -X GET https://dev.expense.ii-us.com/api/users/me \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -w "\n%{http_code}"

# Expected: 200
# Expected Body: UserResponse with email, displayName
```

#### T1.5: First Login Creates User Profile
```bash
# Test: New user's first authenticated request creates user record
# 1. Get token for user not in database
# 2. Call /api/users/me
# 3. Verify user record created with correct claims

# Expected: 200
# Verify: SELECT * FROM users WHERE email = '{user-email}' returns 1 row
```

### T2: Health Endpoint Tests

#### T2.1: Health Endpoint No Auth Required
```bash
# Test: Health endpoint accessible without authentication
curl -X GET https://dev.expense.ii-us.com/api/health \
  -w "\n%{http_code}"

# Expected: 200
# Expected Body: { "status": "healthy", "timestamp": "...", "version": "..." }
```

#### T2.2: Health Endpoint Response Time
```bash
# Test: Health endpoint responds within 100ms
curl -X GET https://dev.expense.ii-us.com/api/health \
  -w "\nTime: %{time_total}s" \
  -o /dev/null -s

# Expected: Time < 0.1s
```

### T3: Cache Table Tests

#### T3.1: Description Cache Insert and Lookup
```sql
-- Test: Insert into description_cache and verify lookup
INSERT INTO description_cache (raw_description_hash, raw_description, normalized_description)
VALUES (
  'a1b2c3d4e5f6...',
  'DELTA AIR 0062363598531',
  'Delta Airlines Flight'
);

-- Verify lookup
SELECT normalized_description FROM description_cache
WHERE raw_description_hash = 'a1b2c3d4e5f6...';

-- Expected: Returns 'Delta Airlines Flight'
-- Timing: < 50ms
```

#### T3.2: Description Cache Hit Count Increment
```sql
-- Test: Duplicate insert increments hit_count
-- Initial state: hit_count = 0

-- Simulate cache hit (application logic)
UPDATE description_cache SET hit_count = hit_count + 1
WHERE raw_description_hash = 'a1b2c3d4e5f6...';

-- Verify
SELECT hit_count FROM description_cache
WHERE raw_description_hash = 'a1b2c3d4e5f6...';

-- Expected: hit_count = 1
```

#### T3.3: Vendor Alias Pattern Matching
```sql
-- Test: Vendor alias pattern matching
INSERT INTO vendor_aliases (canonical_name, alias_pattern, display_name, default_gl_code)
VALUES ('Delta Air Lines', 'DELTA AIR%', 'Delta Airlines', '66300');

-- Query using pattern
SELECT canonical_name, default_gl_code FROM vendor_aliases
WHERE 'DELTA AIR 0062363598531' LIKE alias_pattern;

-- Expected: Returns 'Delta Air Lines', '66300'
```

#### T3.4: Statement Fingerprint Unique Constraint
```sql
-- Test: Unique constraint on (user_id, header_hash)
INSERT INTO statement_fingerprints (user_id, source_name, header_hash, column_mapping)
VALUES ('{user-id}', 'Chase', 'hash123', '{}');

-- Attempt duplicate
INSERT INTO statement_fingerprints (user_id, source_name, header_hash, column_mapping)
VALUES ('{user-id}', 'Chase Different', 'hash123', '{}');

-- Expected: Unique constraint violation
```

#### T3.5: Expense Embedding Vector Similarity
```sql
-- Test: Vector similarity search returns ordered results
-- Insert test embeddings (1536-dimensional vectors)
INSERT INTO expense_embeddings (description_text, gl_code, embedding, verified)
VALUES
  ('Delta Airlines Flight', '66300', '[0.1, 0.2, ...]'::vector(1536), true),
  ('United Airlines Flight', '66300', '[0.11, 0.21, ...]'::vector(1536), true),
  ('Office Supplies', '63100', '[0.9, 0.1, ...]'::vector(1536), true);

-- Query similar to Delta
SELECT description_text, gl_code, 1 - (embedding <=> '[0.1, 0.2, ...]'::vector(1536)) as similarity
FROM expense_embeddings
ORDER BY embedding <=> '[0.1, 0.2, ...]'::vector(1536)
LIMIT 3;

-- Expected: Delta first, United second (similar), Office third (different)
-- Timing: < 500ms for 10,000 rows
```

### T4: Background Job Tests

#### T4.1: Job Enqueue Appears in Dashboard
```bash
# Test: Enqueued job visible in Hangfire dashboard
# 1. POST /api/reference/sync (triggers job)
# 2. Check Hangfire dashboard at /hangfire

TOKEN=$(az account get-access-token --resource api://{client-id} --query accessToken -o tsv)
curl -X POST https://dev.expense.ii-us.com/api/reference/sync \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -w "\n%{http_code}"

# Expected: 202 with jobId
# Verify: Job appears in /hangfire within 5 seconds
```

#### T4.2: Failed Job Retry
```sql
-- Test: Failed jobs are retried
-- Trigger job that will fail (e.g., disconnect external SQL)
-- Verify job enters 'Scheduled' state with retry time

-- Check Hangfire tables for retry
SELECT * FROM hangfire.job WHERE state_name = 'Scheduled';

-- Expected: Job has retry scheduled within 5 minutes
```

#### T4.3: Hangfire Dashboard Requires Admin
```bash
# Test: Hangfire dashboard requires admin role
# Non-admin user
curl -X GET https://dev.expense.ii-us.com/hangfire \
  -H "Authorization: Bearer $NON_ADMIN_TOKEN" \
  -w "\n%{http_code}"

# Expected: 403 Forbidden

# Admin user
curl -X GET https://dev.expense.ii-us.com/hangfire \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -w "\n%{http_code}"

# Expected: 200
```

### T5: Reference Data Tests

#### T5.1: GL Accounts List
```bash
# Test: List GL accounts returns data
TOKEN=$(az account get-access-token --resource api://{client-id} --query accessToken -o tsv)
curl -X GET "https://dev.expense.ii-us.com/api/reference/gl-accounts?activeOnly=true" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -w "\n%{http_code}"

# Expected: 200
# Expected: Array of GLAccountResponse objects
```

#### T5.2: Reference Data Sync Populates Tables
```sql
-- Test: After sync job completes, reference tables have data
SELECT COUNT(*) FROM gl_accounts;
SELECT COUNT(*) FROM departments;
SELECT COUNT(*) FROM projects;

-- Expected: gl_accounts > 0 (typically 1000+)
-- Expected: departments > 0 (typically 100+)
-- Expected: projects > 0 (typically 500+)
```

#### T5.3: Sync Failure Preserves Existing Data
```sql
-- Test: If sync fails, existing data remains
-- 1. Record current counts
-- 2. Trigger sync with disconnected SQL Server
-- 3. Verify counts unchanged

SELECT COUNT(*) as before FROM gl_accounts;
-- Trigger failing sync
SELECT COUNT(*) as after FROM gl_accounts;

-- Expected: before = after
```

### T6: Performance Tests

#### T6.1: Authenticated Request Latency
```bash
# Test: Authenticated requests complete within 500ms p95
for i in {1..100}; do
  curl -X GET https://dev.expense.ii-us.com/api/users/me \
    -H "Authorization: Bearer $TOKEN" \
    -o /dev/null -s -w "%{time_total}\n"
done | sort -n | sed -n '95p'

# Expected: p95 < 0.5s
```

#### T6.2: Cache Lookup Latency
```bash
# Test: Cache lookups complete within 50ms
# (Measure database query time for hash lookup)

# Expected: p95 < 50ms
```

#### T6.3: Reference Data Sync Duration
```bash
# Test: Full reference data sync completes within 2 minutes
time curl -X POST https://dev.expense.ii-us.com/api/reference/sync \
  -H "Authorization: Bearer $ADMIN_TOKEN"

# Wait for job completion
# Expected: < 2 minutes for 1000+ GL codes
```

## Test Execution Checklist

| Test ID | Description | Status | Notes |
|---------|-------------|--------|-------|
| T1.1 | Unauthenticated request rejected | | |
| T1.2 | Invalid token rejected | | |
| T1.3 | Expired token rejected | | |
| T1.4 | Valid token accepted | | |
| T1.5 | First login creates user | | |
| T2.1 | Health no auth required | | |
| T2.2 | Health response time | | |
| T3.1 | Description cache insert/lookup | | |
| T3.2 | Cache hit count increment | | |
| T3.3 | Vendor alias pattern match | | |
| T3.4 | Fingerprint unique constraint | | |
| T3.5 | Embedding vector similarity | | |
| T4.1 | Job appears in dashboard | | |
| T4.2 | Failed job retry | | |
| T4.3 | Dashboard requires admin | | |
| T5.1 | GL accounts list | | |
| T5.2 | Sync populates tables | | |
| T5.3 | Sync failure preserves data | | |
| T6.1 | Auth request latency | | |
| T6.2 | Cache lookup latency | | |
| T6.3 | Sync duration | | |
