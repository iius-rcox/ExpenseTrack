# Research: Transaction Group Matching

**Feature**: 028-group-matching
**Date**: 2026-01-07
**Status**: Complete

## Overview

This research documents the technical decisions for extending the matching engine to support transaction groups. Since the infrastructure already exists (database schema, entities, service patterns), the research focuses on implementation patterns and integration points.

---

## Research Area 1: Group Candidate Query Strategy

### Context
The existing `RunAutoMatchAsync()` queries unmatched transactions. We need to also query unmatched groups.

### Decision: Parallel Query with Combined Candidate Pool

**Approach**: Query transactions and groups in parallel, combine into a unified candidate pool for scoring.

**Rationale**:
- Maintains existing transaction query performance
- Groups are a small subset (~50 per user typical) vs transactions (~1000)
- Single scoring loop handles both candidate types
- Matches existing pattern of pre-fetching candidates before scoring

**Implementation**:
```csharp
// Fetch both in parallel
var transactionsTask = GetUnmatchedTransactionsAsync(userId, dateRange);
var groupsTask = GetUnmatchedGroupsAsync(userId, dateRange);

await Task.WhenAll(transactionsTask, groupsTask);

// Combine into unified candidate interface
var candidates = transactionsTask.Result
    .Where(t => t.GroupId == null)  // Exclude grouped transactions
    .Select(t => new MatchCandidate { Type = "Transaction", ... })
    .Concat(groupsTask.Result
        .Select(g => new MatchCandidate { Type = "Group", ... }));
```

**Alternatives Considered**:
1. **UNION query in SQL** - Rejected: Different table shapes make this complex; EF Core handles parallel queries efficiently
2. **Sequential queries** - Rejected: Adds latency unnecessarily
3. **Groups-only pass** - Rejected: Duplicates scoring logic

---

## Research Area 2: Scoring Algorithm for Groups

### Context
Individual transactions use a 3-component scoring algorithm (amount, date, vendor). How should groups be scored?

### Decision: Identical Algorithm with Group-Level Values

**Approach**: Use the same scoring weights and thresholds, substituting group attributes:

| Component | Individual Transaction | Transaction Group |
|-----------|----------------------|-------------------|
| Amount | `transaction.Amount` | `group.CombinedAmount` |
| Date | `transaction.TransactionDate` | `group.DisplayDate` |
| Vendor | `ExtractVendorPattern(description)` | `group.Name` (already normalized) |

**Rationale**:
- Maintains consistency in match quality expectations
- Users understand scores mean the same thing for both types
- Group names already follow "Vendor (N charges)" pattern from creation
- DisplayDate is the most relevant date (max of members or user override)

**Scoring Weights** (unchanged):
- Amount: 40 pts (±$0.10 exact, ±$1.00 near)
- Date: 35 pts (same day) → 10 pts (±7 days)
- Vendor: 25 pts (alias match) → 15 pts (fuzzy 70%+)
- Minimum threshold: 70 pts
- Ambiguous threshold: 5 pts between top candidates

**Alternatives Considered**:
1. **Different weights for groups** - Rejected: Creates user confusion about score meanings
2. **Aggregate scoring across member transactions** - Rejected: Over-complicated; CombinedAmount is the relevant value
3. **Boost scores for groups** - Rejected: Would bias toward groups unfairly

---

## Research Area 3: Vendor Matching for Groups

### Context
Individual transactions use `ExtractVendorPattern()` to normalize vendor from description. Groups have a `Name` property.

### Decision: Use Group Name Directly for Vendor Scoring

**Approach**:
- Group names already follow normalized format (e.g., "TWILIO (3 charges)")
- Extract base vendor by stripping the "(N charges)" suffix
- Use existing alias lookup and fuzzy matching

**Implementation**:
```csharp
private string ExtractVendorFromGroupName(string groupName)
{
    // "TWILIO (3 charges)" → "TWILIO"
    var match = Regex.Match(groupName, @"^(.+?)\s*\(\d+\s*charges?\)$", RegexOptions.IgnoreCase);
    return match.Success ? match.Groups[1].Value.Trim() : groupName;
}
```

**Rationale**:
- Group names are created from primary transaction's normalized description
- Stripping suffix gives the vendor name
- Works with existing alias cache (built from transaction descriptions)

**Alternatives Considered**:
1. **Use first transaction's description** - Rejected: Requires joining to transactions; group name already has this info
2. **Match against all member transactions' vendors** - Rejected: Over-complicated; groups are for same-vendor charges

---

## Research Area 4: Excluding Grouped Transactions

### Context
When transactions are grouped, they should not appear as individual match candidates.

### Decision: Filter by GroupId at Query Time

**Approach**: Add `.Where(t => t.GroupId == null)` to the unmatched transactions query.

**Rationale**:
- Most efficient: reduces data transfer from database
- Single point of enforcement
- Already have index on GroupId for group membership queries

**Implementation**:
```csharp
var transactions = await _context.Transactions
    .Where(t => t.UserId == userId)
    .Where(t => t.MatchStatus == MatchStatus.Unmatched)
    .Where(t => t.GroupId == null)  // Exclude grouped transactions
    .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
    .ToListAsync();
```

**Alternatives Considered**:
1. **Filter in-memory after fetch** - Rejected: Wastes bandwidth and memory
2. **Separate "available for matching" flag** - Rejected: Redundant with GroupId null check

---

## Research Area 5: Match Record Structure

### Context
`ReceiptTransactionMatch` has both `TransactionId` and `TransactionGroupId` (mutually exclusive via constraint).

### Decision: Set TransactionGroupId for Group Matches

**Approach**: When matching to a group, set `TransactionGroupId` and leave `TransactionId` null.

**Existing Constraint** (already in database):
```sql
CHECK ((transaction_id IS NOT NULL AND transaction_group_id IS NULL)
    OR (transaction_id IS NULL AND transaction_group_id IS NOT NULL))
```

**MatchReason Format** for groups:
```
"Group 'TWILIO (3 charges)' matches: Amount $45.00 ≈ $45.12 (exact), Date 2026-01-05 = 2026-01-05 (same day), Vendor TWILIO matched alias"
```

**Rationale**:
- Schema already designed for this use case
- Constraint prevents data integrity issues
- Match details in MatchReason provide clarity

---

## Research Area 6: Group Status Updates

### Context
When a match is proposed/confirmed/rejected, group status needs updating.

### Decision: Mirror Transaction Status Update Pattern

**Approach**: Update `TransactionGroup.MatchStatus` and `MatchedReceiptId` the same way we update `Transaction.MatchStatus`.

**State Transitions**:
| Action | Group.MatchStatus | Group.MatchedReceiptId |
|--------|-------------------|------------------------|
| Proposal created | Proposed | null |
| Match confirmed | Matched | ReceiptId |
| Match rejected | Unmatched | null |
| Receipt deleted | Unmatched | null |
| Group deleted | (cascade handled by receipt returning to unmatched) | - |

**Rationale**:
- Consistent with existing transaction status pattern
- Group already has these properties
- Frontend can display status uniformly

---

## Research Area 7: Performance Considerations

### Context
Auto-matching must complete in <2 seconds per receipt.

### Decision: Batch Queries, Leverage Existing Caches

**Optimizations**:
1. **Parallel fetch**: Transactions and groups queried concurrently
2. **Alias cache reuse**: Build once per auto-match run, use for both transaction and group vendor matching
3. **Groups are small**: Typically ~50 groups vs ~1000 transactions; minimal overhead
4. **Early filtering**: GroupId null filter reduces transaction candidate pool

**Benchmarking Plan**:
- Measure before/after for 1000-transaction user with 50 groups
- Target: <10% latency increase

**Alternatives Considered**:
1. **Separate group matching pass** - Rejected: Doubles scoring loop iterations
2. **Materialized view for candidates** - Rejected: Over-engineering for this scale

---

## Research Area 8: Frontend Integration

### Context
The match candidate list needs to display groups alongside transactions.

### Decision: Extend Existing Candidate DTO

**Approach**: Add `CandidateType` field to distinguish transactions from groups in the response.

**DTO Addition**:
```typescript
interface MatchCandidate {
  id: string;
  candidateType: 'transaction' | 'group';  // New field
  amount: number;
  date: string;
  displayName: string;  // Transaction description or group name
  transactionCount?: number;  // Only for groups
  score: number;
}
```

**UI Display**:
- Groups show badge: "3 transactions"
- Groups show combined amount
- Same selection/confirmation flow

**Rationale**:
- Minimal frontend changes
- Single list component handles both types
- User sees unified candidate list

---

## Summary of Decisions

| Area | Decision | Complexity |
|------|----------|------------|
| Query Strategy | Parallel query, combined candidate pool | Low |
| Scoring Algorithm | Identical weights, group-level values | Low |
| Vendor Matching | Extract from group name | Low |
| Grouped TX Exclusion | Filter by GroupId at query time | Low |
| Match Records | Set TransactionGroupId (existing schema) | Low |
| Status Updates | Mirror transaction pattern | Low |
| Performance | Batch queries, cache reuse | Low |
| Frontend | Extend candidate DTO with type | Low |

**Overall Complexity Assessment**: Low - This is a clean extension of existing patterns with no new infrastructure required.
