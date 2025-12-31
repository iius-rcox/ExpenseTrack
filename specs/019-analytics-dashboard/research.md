# Research: Analytics Dashboard API Endpoints

**Feature**: 019-analytics-dashboard
**Date**: 2025-12-31

## Research Topics

### 1. Category Derivation Strategy

**Context**: Transaction entity lacks a Category field. How should category breakdowns work?

**Decision**: Use pattern-based category derivation (same approach as existing `DeriveCategory()` method in AnalyticsController)

**Rationale**:
- Existing codebase already has this pattern working in `AnalyticsController.cs:280-335`
- No database schema changes required
- VendorAlias.Category only has 4 values (Standard, Airline, Hotel, Subscription) - insufficient for detailed analytics
- Pattern matching provides richer categorization (Transportation, Food & Dining, Travel & Lodging, Shopping & Retail, Entertainment, Office & Business, Healthcare, Utilities & Bills, Other)

**Alternatives Considered**:
- Add Category column to Transaction table: Rejected - requires migration, backfill, and ongoing categorization logic
- Use VendorAlias.Category: Rejected - only 4 categories, not suitable for expense analysis
- Use GL codes from Vista: Rejected - not all transactions have GL codes assigned

### 2. Weekly Aggregation with ISO Week Boundaries

**Context**: FR-009 requires ISO week boundaries for weekly granularity.

**Decision**: Use `IsoWeek` calculation: `CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)`

**Rationale**:
- ISO 8601 standard defines week boundaries consistently
- PostgreSQL `EXTRACT(WEEK FROM date)` uses ISO week numbering
- Allows proper aggregation across year boundaries

**Alternatives Considered**:
- Simple 7-day windows from start date: Rejected - inconsistent boundaries between queries
- US calendar weeks (Sunday start): Rejected - doesn't align with ISO standard or PostgreSQL

### 3. Merchant Analytics with Comparison Period

**Context**: FR-011 requires identifying new merchants and calculating percentage changes.

**Decision**: Compute comparison period as same duration, ending one day before start date of current period

**Rationale**:
- Mirrors common "this month vs last month" mental model
- Example: Current = Jan 15-Feb 14 → Comparison = Dec 15-Jan 14
- Allows meaningful trend detection for merchants

**Alternatives Considered**:
- Year-over-year comparison: Rejected - adds complexity, can be added later
- Fixed 30-day comparison: Rejected - doesn't adapt to user's selected range

### 4. Subscription Proxy Pattern

**Context**: Subscription endpoints exist at `/api/subscriptions/*` but frontend expects `/api/analytics/subscriptions/*`

**Decision**: Create thin proxy endpoints in AnalyticsController that delegate to existing ISubscriptionDetectionService

**Rationale**:
- Avoids code duplication
- Frontend expectations met without breaking existing subscriptions API
- Easy to maintain - changes to subscription logic only happen in one place

**Alternatives Considered**:
- URL rewriting middleware: Rejected - adds infrastructure complexity
- Move subscriptions under analytics namespace entirely: Rejected - breaking change for any existing consumers
- Duplicate the subscription logic: Rejected - DRY violation

### 5. Performance Optimization for Date Range Queries

**Context**: SC-002 requires 500ms response for 90-day ranges with 1000 transactions

**Decision**: Use direct EF Core LINQ queries with database-side aggregation

**Rationale**:
- EF Core translates GroupBy to efficient SQL GROUP BY
- Avoids loading all transactions into memory
- PostgreSQL handles aggregation efficiently
- Index on (UserId, TransactionDate) already exists

**Query Pattern**:
```csharp
// Database-side aggregation
var trends = await _dbContext.Transactions
    .Where(t => t.UserId == userId &&
                t.TransactionDate >= startDate &&
                t.TransactionDate <= endDate)
    .GroupBy(t => t.TransactionDate)
    .Select(g => new SpendingTrendItem
    {
        Date = g.Key.ToString("yyyy-MM-dd"),
        Amount = g.Sum(t => t.Amount),
        TransactionCount = g.Count()
    })
    .ToListAsync();
```

**Alternatives Considered**:
- Load all transactions and aggregate in-memory: Rejected - poor performance at scale
- Add caching layer: Deferred - not needed for MVP, can add if performance issues arise
- Materialized views: Deferred - adds database complexity, not needed for expected scale

### 6. Refund Handling in Analytics

**Context**: Clarification confirmed refunds (negative amounts) should be kept as separate line items

**Decision**: Include all transactions regardless of sign; do not net positive and negative amounts

**Rationale**:
- Matches user expectation for detailed spending view
- Allows frontend to choose presentation (net, gross, or both)
- Preserves audit trail clarity
- `Amount > 0` for expenses, `Amount < 0` for refunds - both visible

**Implementation**:
```csharp
// Include both positive and negative in query - no filtering by Amount
.Where(t => t.UserId == userId &&
            t.TransactionDate >= startDate &&
            t.TransactionDate <= endDate)
```

## Resolved Clarifications

| Topic | Resolution |
|-------|------------|
| Category derivation | Pattern-based matching using description text |
| Weekly boundaries | ISO 8601 week numbering |
| Comparison period | Same duration, immediately preceding current period |
| Subscription API | Proxy pattern delegating to existing service |
| Query performance | Database-side aggregation with EF Core |
| Refund handling | Keep as separate line items (no netting) |

## Next Steps

All technical unknowns resolved. Proceed to Phase 1: Design & Contracts.
