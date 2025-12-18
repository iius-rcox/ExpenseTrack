# ExpenseFlow Database Query Performance Analysis Report

**Generated:** 2025-12-18
**Scope:** Repository layer, Service layer, Entity relationships
**Database:** PostgreSQL 15+ with pgvector extension

---

## pg_stat_statements Findings (2025-12-18)

**Result: All queries performing within target (<500ms)**

Actual query performance from staging load tests:

| Query | Calls | Mean (ms) | Max (ms) | Status |
|-------|-------|-----------|----------|--------|
| Transaction duplicate check | 20 | 0.02 | 0.05 | ✅ |
| Transaction INSERT | 13 | 0.31 | 0.52 | ✅ |
| Statement fingerprint lookup | 7 | 0.58 | 1.21 | ✅ |
| Statement import INSERT | 2 | 1.54 | 2.38 | ✅ |
| Fingerprint UPDATE (hit count) | 3 | 0.04 | 0.06 | ✅ |
| Hangfire job operations | 286K+ | 0.01-0.05 | 0.10 | ✅ |

**Conclusion:** Current queries are well-optimized. The recommendations below are **preventive measures** for when data volume increases. No critical performance issues detected during load testing.

---

## Executive Summary

This analysis identifies **15 query optimization opportunities** across the ExpenseFlow codebase, categorized by severity:

| Priority | Count | Description |
|----------|-------|-------------|
| Critical | 3 | N+1 patterns causing multiple DB round-trips per request |
| High | 5 | Missing indexes on frequently queried columns |
| Medium | 4 | Complex queries needing restructuring |
| Low | 3 | Large result sets without pagination guards |

Estimated performance improvement: **40-60% reduction in database load** after implementing all recommendations.

---

## Critical Issues

### CRIT-001: N+1 Query Pattern in ReportService.GenerateDraftAsync

**File:** `backend/src/ExpenseFlow.Infrastructure/Services/ReportService.cs`
**Lines:** 83-182

**Problem:**
The `GenerateDraftAsync` method iterates over confirmed matches and unmatched transactions, calling `GetCategorizationAsync` for each item individually. Each categorization call triggers multiple database queries:

```csharp
// Lines 83-131: N+1 pattern - one query per match
foreach (var match in confirmedMatches)
{
    var categorization = await GetCategorizationSafeAsync(transaction.Id, userId, ct);
    var normalizedDesc = await NormalizeDescriptionSafeAsync(transaction.OriginalDescription, userId, ct);
    // ...
}

// Lines 135-183: Same pattern for unmatched transactions
foreach (var transaction in unmatchedTransactions)
{
    var categorization = await GetCategorizationSafeAsync(transaction.Id, userId, ct);
    var normalizedDesc = await NormalizeDescriptionSafeAsync(transaction.OriginalDescription, userId, ct);
}
```

**Impact:** For a report with 50 transactions, this generates 100+ individual database queries instead of batched operations.

**Recommendation:**
1. Pre-load all vendor aliases for the transaction descriptions in a single query
2. Batch normalize descriptions using a single cached lookup
3. Pre-compute embeddings for all transactions in parallel

```csharp
// Optimized: Batch load all needed data upfront
var transactionIds = confirmedMatches.Select(m => m.TransactionId)
    .Union(unmatchedTransactions.Select(t => t.Id)).ToList();

var descriptions = transactions.Select(t => t.Description).Distinct().ToList();
var aliasCache = await _vendorAliasService.FindMatchingAliasesBatchAsync(descriptions);
var descriptionCache = await _normalizationService.NormalizeBatchAsync(descriptions, userId);
```

---

### CRIT-002: N+1 Query in SubscriptionDetectionService.DetectFromTransactionsAsync

**File:** `backend/src/ExpenseFlow.Infrastructure/Services/SubscriptionDetectionService.cs`
**Lines:** 74-121

**Problem:**
The batch detection method iterates over transactions and calls `DetectFromTransactionAsync` for each one, which queries the database individually:

```csharp
// Line 87: Calls DetectFromTransactionAsync for EACH transaction
foreach (var transaction in transactions)
{
    var result = await DetectFromTransactionAsync(transaction);
    results.Add(result);
}
```

Each call to `DetectFromTransactionAsync` executes:
- `FindKnownVendorAsync` - queries known vendors table
- `GetByVendorNameAsync` - queries subscriptions table

**Impact:** For 100 transactions, this generates 200+ database queries.

**Recommendation:**
Pre-load known vendors and existing subscriptions in batch before the loop:

```csharp
// Pre-load all known vendors once (already partially done at line 84)
var knownVendors = await _subscriptionRepository.GetKnownVendorsAsync();
var vendorPatternCache = knownVendors.ToDictionary(v => v.VendorPattern.ToUpperInvariant());

// Pre-load user's existing subscriptions
var existingSubscriptions = await _subscriptionRepository.GetAllByUserAsync(userId);
var subscriptionByVendor = existingSubscriptions.ToDictionary(
    s => s.VendorName.ToUpperInvariant());
```

---

### CRIT-003: N+1 Query in TravelDetectionService.GetTimelineAsync

**File:** `backend/src/ExpenseFlow.Infrastructure/Services/TravelDetectionService.cs`
**Lines:** 293-342

**Problem:**
The timeline method loads travel periods, then iterates and calls `BuildTimelineEntryAsync` for each period. Inside that method (lines 481-593), there are multiple queries per period:

```csharp
// Line 317: Iterates over each travel period
foreach (var period in travelPeriods)
{
    var entry = await BuildTimelineEntryAsync(period, includeExpenses);
    // ...
}
```

Each `BuildTimelineEntryAsync` call executes:
- Query for source receipt (line 497)
- Query for receipts within date range (lines 515-520)
- Query for transactions within date range (lines 523-527)

**Impact:** For 10 travel periods, this generates 30+ queries.

**Recommendation:**
Pre-load all receipts and transactions for the entire date range:

```csharp
// Pre-load all needed data in bulk
var minDate = travelPeriods.Min(p => p.StartDate);
var maxDate = travelPeriods.Max(p => p.EndDate);

var allReceipts = await _dbContext.Receipts
    .Where(r => r.UserId == userId &&
                r.DateExtracted >= minDate &&
                r.DateExtracted <= maxDate)
    .ToListAsync();

var allTransactions = await _dbContext.Transactions
    .Where(t => t.UserId == userId &&
                t.TransactionDate >= minDate &&
                t.TransactionDate <= maxDate)
    .ToListAsync();

// Then filter in-memory per period
```

---

## High Priority Issues

### HIGH-001: Missing Index on DetectedSubscription.VendorName

**File:** `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/DetectedSubscriptionConfiguration.cs`
**Table:** `detected_subscriptions`

**Problem:**
The `GetByVendorNameAsync` method uses a case-insensitive search on vendor name:

```csharp
// SubscriptionRepository.cs, line 56-61
return await _context.DetectedSubscriptions
    .Include(s => s.VendorAlias)
    .FirstOrDefaultAsync(s => s.UserId == userId &&
                              s.VendorName.ToUpper() == vendorName.ToUpper());
```

There is no index on `vendor_name` column, and the `ToUpper()` call prevents index usage even if one existed.

**EXPLAIN ANALYZE (estimated):**
```sql
Seq Scan on detected_subscriptions  (cost=0.00..2500.00 rows=1 width=120)
  Filter: ((user_id = $1) AND (upper(vendor_name) = upper($2)))
```

**Recommendation:**
Add a case-insensitive index using PostgreSQL's `citext` extension or a functional index:

```sql
-- Option 1: Functional index on upper(vendor_name)
CREATE INDEX ix_detected_subscriptions_vendor_name_upper
ON detected_subscriptions (user_id, upper(vendor_name));

-- Option 2: Use citext extension for case-insensitive comparisons
ALTER TABLE detected_subscriptions
ALTER COLUMN vendor_name TYPE citext;
```

---

### HIGH-002: Missing Index on ExpenseReport (UserId, Status) for GetByUserAsync

**File:** `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/ExpenseReportConfiguration.cs`
**Table:** `expense_reports`

**Problem:**
The `GetByUserAsync` method filters by `UserId`, `Status`, and `IsDeleted`, but the existing indexes don't fully cover this query pattern:

```csharp
// ExpenseReportRepository.cs, lines 48-75
var query = _context.ExpenseReports
    .Where(r => !r.IsDeleted)
    .Where(r => r.UserId == userId);

if (status.HasValue)
{
    query = query.Where(r => r.Status == status.Value);
}
```

Current indexes:
- `ix_expense_reports_user_period` - partial unique index on (UserId, Period)
- `ix_expense_reports_user_created` - index on (UserId, CreatedAt)

**Recommendation:**
Add composite index covering the common filter pattern:

```sql
CREATE INDEX ix_expense_reports_user_status
ON expense_reports (user_id, status, created_at DESC)
WHERE NOT is_deleted;
```

---

### HIGH-003: Missing Index on ExpenseEmbedding.UserId for Similarity Search

**File:** `backend/src/ExpenseFlow.Infrastructure/Repositories/ExpenseEmbeddingRepository.cs`
**Lines:** 23-41

**Problem:**
The vector similarity search filters by `UserId` before performing cosine distance calculation:

```csharp
var results = await _context.ExpenseEmbeddings
    .Where(e => e.UserId == userId)
    .Where(e => e.Embedding.CosineDistance(queryEmbedding) <= maxDistance)
    .OrderBy(e => e.Verified ? 0 : 1)
    .ThenBy(e => e.Embedding.CosineDistance(queryEmbedding))
    .Take(limit)
    .ToListAsync();
```

The current index `ix_expense_embeddings_verified_user` is on `(verified, user_id)`, which is not optimal for filtering by user_id first.

**Recommendation:**
Add a dedicated index for user filtering, and consider partitioning embeddings by user for large datasets:

```sql
-- Index for user-first filtering
CREATE INDEX ix_expense_embeddings_user_id
ON expense_embeddings (user_id);

-- For very large tables, consider partial IVFFlat index per user
-- (requires application-level partitioning strategy)
```

---

### HIGH-004: Missing Index on KnownSubscriptionVendors for Active Filter

**File:** `backend/src/ExpenseFlow.Infrastructure/Repositories/SubscriptionRepository.cs`
**Line:** 102-107

**Problem:**
```csharp
return await _context.KnownSubscriptionVendors
    .Where(v => v.IsActive)
    .OrderBy(v => v.DisplayName)
    .ToListAsync();
```

This query runs frequently during subscription detection but there's no index on `is_active`.

**Recommendation:**
Add partial index for active vendors:

```sql
CREATE INDEX ix_known_subscription_vendors_active
ON known_subscription_vendors (display_name)
WHERE is_active = true;
```

---

### HIGH-005: Missing Index on TierUsageLogs for Transaction Join

**File:** `backend/src/ExpenseFlow.Infrastructure/Repositories/TierUsageRepository.cs`
**Lines:** 56-81

**Problem:**
The `GetVendorTier3UsageAsync` method joins `tier_usage_logs` with `transactions` table:

```csharp
var candidates = await _context.TierUsageLogs
    .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
    .Where(t => t.TierUsed == 3)
    .Where(t => t.TransactionId != null)
    .Join(
        _context.Transactions,
        log => log.TransactionId,
        txn => txn.Id,
        (log, txn) => new { log, txn })
    // ...
```

The current index `ix_tier_usage_logs_type_tier` is on `(operation_type, tier_used, created_at)` which doesn't cover this join pattern.

**Recommendation:**
Add covering index for the join query:

```sql
CREATE INDEX ix_tier_usage_logs_tier3_join
ON tier_usage_logs (created_at, tier_used, transaction_id)
WHERE tier_used = 3 AND transaction_id IS NOT NULL;
```

---

## Medium Priority Issues

### MED-001: Inefficient VendorAlias Category Filtering

**File:** `backend/src/ExpenseFlow.Infrastructure/Services/VendorAliasService.cs`
**Lines:** 44-67

**Problem:**
The `FindMatchingAliasAsync` with category filter loads ALL aliases for given categories into memory, then filters in-memory:

```csharp
var aliases = await _dbContext.VendorAliases
    .AsNoTracking()
    .Where(v => categories.Contains(v.Category))
    .OrderByDescending(v => v.Confidence)
    .ThenByDescending(v => v.MatchCount)
    .ToListAsync();

foreach (var alias in aliases)
{
    if (normalizedDescription.Contains(alias.AliasPattern.ToUpperInvariant()))
    {
        return alias;
    }
}
```

**Impact:** For 500+ vendor aliases, this loads entire table into memory.

**Recommendation:**
Use database-side pattern matching like the other overload:

```csharp
return await _dbContext.VendorAliases
    .AsNoTracking()
    .Where(v => categories.Contains(v.Category))
    .Where(v => EF.Functions.ILike(description, "%" + v.AliasPattern + "%"))
    .OrderByDescending(v => v.Confidence)
    .ThenByDescending(v => v.MatchCount)
    .FirstOrDefaultAsync();
```

---

### MED-002: Duplicate Count Query in SubscriptionDetectionService

**File:** `backend/src/ExpenseFlow.Infrastructure/Services/SubscriptionDetectionService.cs`
**Lines:** 178-197

**Problem:**
The `GetSubscriptionsAsync` method runs the count query twice with similar filters:

```csharp
// First query: Get paged subscriptions
var (subscriptions, totalCount) = await _subscriptionRepository.GetPagedAsync(
    userId, page, pageSize, status);

// Second query: Get ALL subscriptions for count aggregation
var allSubs = await _subscriptionRepository.GetByStatusesAsync(
    userId, SubscriptionStatus.Active, SubscriptionStatus.Missing, SubscriptionStatus.Flagged);

// Then count in memory
return new SubscriptionListResponseDto
{
    // ...
    ActiveCount = allSubs.Count(s => s.Status == SubscriptionStatus.Active),
    MissingCount = allSubs.Count(s => s.Status == SubscriptionStatus.Missing),
    FlaggedCount = allSubs.Count(s => s.Status == SubscriptionStatus.Flagged)
};
```

**Impact:** Two full table scans instead of one with GROUP BY.

**Recommendation:**
Use a single aggregation query:

```sql
SELECT status, COUNT(*) as count
FROM detected_subscriptions
WHERE user_id = @userId AND status IN (0, 1, 2)
GROUP BY status;
```

```csharp
var statusCounts = await _context.DetectedSubscriptions
    .Where(s => s.UserId == userId)
    .GroupBy(s => s.Status)
    .Select(g => new { Status = g.Key, Count = g.Count() })
    .ToDictionaryAsync(x => x.Status, x => x.Count);
```

---

### MED-003: Count Query Without Matching Filter in TransactionRepository

**File:** `backend/src/ExpenseFlow.Infrastructure/Repositories/TransactionRepository.cs`
**Lines:** 62-67

**Problem:**
The `GetPagedAsync` method gets the unmatched count separately from the filtered count:

```csharp
var totalCount = await query.CountAsync();

// Separate query for unmatched count (ignores filters)
var unmatchedCount = await _context.Transactions
    .Where(t => t.UserId == userId && t.MatchedReceiptId == null)
    .CountAsync();
```

**Impact:** Extra database round-trip for every paginated transaction query.

**Recommendation:**
Either cache the unmatched count or compute both counts in a single query using UNION:

```csharp
var counts = await _context.Database.SqlQueryRaw<CountResult>(@"
    SELECT
        COUNT(*) FILTER (WHERE matched_receipt_id IS NULL) as unmatched_count,
        COUNT(*) as total_count
    FROM transactions
    WHERE user_id = @p0 AND transaction_date >= @p1 AND transaction_date <= @p2
", userId, startDate, endDate).FirstOrDefaultAsync();
```

---

### MED-004: Inefficient GroupBy for Stats Calculation

**File:** `backend/src/ExpenseFlow.Infrastructure/Repositories/DescriptionCacheRepository.cs`
**Lines:** 40-54

**Problem:**
The `GetStatsAsync` method uses a strange `GroupBy(_ => 1)` pattern:

```csharp
var stats = await _context.DescriptionCaches
    .GroupBy(_ => 1)
    .Select(g => new
    {
        TotalEntries = g.Count(),
        TotalHits = g.Sum(d => (long)d.HitCount)
    })
    .FirstOrDefaultAsync();
```

This creates an inefficient query plan.

**Recommendation:**
Use separate scalar queries or a raw SQL query:

```csharp
var totalEntries = await _context.DescriptionCaches.CountAsync();
var totalHits = await _context.DescriptionCaches.SumAsync(d => (long)d.HitCount);
return (totalEntries, totalHits);

// Or single query:
var stats = await _context.Database.SqlQueryRaw<(int Count, long Sum)>(@"
    SELECT COUNT(*) as Count, COALESCE(SUM(hit_count), 0) as Sum
    FROM description_cache
").FirstOrDefaultAsync();
```

---

## Low Priority Issues

### LOW-001: Missing Pagination Guard in SubscriptionRepository.GetByStatusesAsync

**File:** `backend/src/ExpenseFlow.Infrastructure/Repositories/SubscriptionRepository.cs`
**Lines:** 64-72

**Problem:**
```csharp
public async Task<List<DetectedSubscription>> GetByStatusesAsync(
    Guid userId, params SubscriptionStatus[] statuses)
{
    return await _context.DetectedSubscriptions
        .Include(s => s.VendorAlias)
        .Where(s => s.UserId == userId && statuses.Contains(s.Status))
        .OrderBy(s => s.Status)
        .ThenByDescending(s => s.LastSeenDate)
        .ToListAsync();
}
```

No pagination - could return thousands of records.

**Recommendation:**
Add `Take(1000)` as a safety limit or require pagination parameters.

---

### LOW-002: Missing Pagination Guard in TravelPeriodRepository.GetOverlappingAsync

**File:** `backend/src/ExpenseFlow.Infrastructure/Repositories/TravelPeriodRepository.cs`
**Lines:** 60-68

**Problem:**
```csharp
public async Task<List<TravelPeriod>> GetOverlappingAsync(
    Guid userId, DateOnly startDate, DateOnly endDate)
{
    return await _context.TravelPeriods
        .Where(t => t.UserId == userId &&
                    t.StartDate <= endDate &&
                    t.EndDate >= startDate)
        .OrderBy(t => t.StartDate)
        .ToListAsync();
}
```

No limit on results.

**Recommendation:**
Add reasonable limit: `.Take(100)` - overlapping travel periods should be few.

---

### LOW-003: MatchingService.RunAutoMatchAsync Memory Usage

**File:** `backend/src/ExpenseFlow.Infrastructure/Services/MatchingService.cs`
**Lines:** 58-97

**Problem:**
Both receipts and transactions are loaded entirely into memory:

```csharp
var receipts = await receiptsQuery.ToListAsync();
// ...
var transactions = await _context.Transactions
    .Where(/* filters */)
    .ToListAsync();
```

For users with thousands of receipts/transactions, this could consume significant memory.

**Recommendation:**
Process in batches for large datasets:

```csharp
const int batchSize = 100;
var processedCount = 0;

while (true)
{
    var receiptBatch = await receiptsQuery
        .Skip(processedCount)
        .Take(batchSize)
        .ToListAsync();

    if (!receiptBatch.Any()) break;

    // Process batch...
    processedCount += batchSize;
}
```

---

## Index Recommendations Summary

| Table | Index Name | Columns | Filter | Type |
|-------|------------|---------|--------|------|
| detected_subscriptions | ix_detected_subscriptions_vendor_upper | (user_id, upper(vendor_name)) | - | B-tree |
| expense_reports | ix_expense_reports_user_status | (user_id, status, created_at DESC) | NOT is_deleted | Partial B-tree |
| expense_embeddings | ix_expense_embeddings_user_id | (user_id) | - | B-tree |
| known_subscription_vendors | ix_known_subscription_vendors_active | (display_name) | is_active = true | Partial B-tree |
| tier_usage_logs | ix_tier_usage_logs_tier3_join | (created_at, tier_used, transaction_id) | tier_used = 3 | Partial B-tree |

**SQL Script:**

```sql
-- Index migrations for query optimization
-- Generated: 2025-12-18

-- HIGH-001: DetectedSubscription vendor name search
CREATE INDEX CONCURRENTLY ix_detected_subscriptions_vendor_upper
ON detected_subscriptions (user_id, upper(vendor_name));

-- HIGH-002: ExpenseReport status filtering
CREATE INDEX CONCURRENTLY ix_expense_reports_user_status
ON expense_reports (user_id, status, created_at DESC)
WHERE NOT is_deleted;

-- HIGH-003: ExpenseEmbedding user filtering
CREATE INDEX CONCURRENTLY ix_expense_embeddings_user_id
ON expense_embeddings (user_id);

-- HIGH-004: KnownSubscriptionVendors active filter
CREATE INDEX CONCURRENTLY ix_known_subscription_vendors_active
ON known_subscription_vendors (display_name)
WHERE is_active = true;

-- HIGH-005: TierUsageLogs tier 3 join optimization
CREATE INDEX CONCURRENTLY ix_tier_usage_logs_tier3_join
ON tier_usage_logs (created_at, tier_used, transaction_id)
WHERE tier_used = 3 AND transaction_id IS NOT NULL;
```

---

## Query Monitoring Recommendations

Add these PostgreSQL queries to monitor slow queries in production:

```sql
-- Enable query logging for slow queries (>100ms)
ALTER SYSTEM SET log_min_duration_statement = 100;
SELECT pg_reload_conf();

-- Find most expensive queries (requires pg_stat_statements extension)
SELECT
    substring(query, 1, 100) as query_preview,
    calls,
    round(total_exec_time::numeric, 2) as total_time_ms,
    round(mean_exec_time::numeric, 2) as avg_time_ms,
    rows
FROM pg_stat_statements
ORDER BY total_exec_time DESC
LIMIT 20;

-- Find tables needing vacuuming
SELECT
    schemaname || '.' || relname as table_name,
    n_dead_tup as dead_tuples,
    n_live_tup as live_tuples,
    last_vacuum,
    last_autovacuum
FROM pg_stat_user_tables
WHERE n_dead_tup > 1000
ORDER BY n_dead_tup DESC;

-- Check index usage
SELECT
    schemaname || '.' || relname as table_name,
    indexrelname as index_name,
    idx_scan as index_scans,
    pg_size_pretty(pg_relation_size(indexrelid)) as index_size
FROM pg_stat_user_indexes
ORDER BY idx_scan;
```

---

## Implementation Priority

1. **Week 1:** Implement CRIT-001, CRIT-002, CRIT-003 (N+1 patterns)
2. **Week 2:** Apply all HIGH priority index recommendations
3. **Week 3:** Refactor MED priority query optimizations
4. **Week 4:** Add pagination guards and monitoring

---

## Appendix: Testing Recommendations

After implementing optimizations, validate with:

1. **Load test** with NBomber (already configured in Sprint 10)
2. **EXPLAIN ANALYZE** on production queries
3. **Monitor** `pg_stat_statements` for query performance trends
4. **Benchmark** before/after execution times

```csharp
// Add query timing in development
services.AddDbContext<ExpenseFlowDbContext>(options =>
{
    options.UseNpgsql(connectionString);

    #if DEBUG
    options.EnableSensitiveDataLogging();
    options.LogTo(Console.WriteLine, LogLevel.Information);
    #endif
});
```
