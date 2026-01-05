# Research: Vendor Name Extraction

**Feature**: 025-vendor-extraction
**Date**: 2026-01-05

## Summary

This feature has minimal research requirements because it leverages existing, proven infrastructure. The VendorAlias system is already implemented and tested.

## Existing Infrastructure Analysis

### VendorAliasService (Already Implemented)

**Location**: `backend/src/ExpenseFlow.Infrastructure/Services/VendorAliasService.cs`

**Key Method**: `FindMatchingAliasAsync(string description)`
- Uses PostgreSQL ILIKE for case-insensitive substring matching
- Orders results by Confidence (descending), then MatchCount (descending)
- Returns the best matching VendorAlias or null

**Decision**: Use existing `FindMatchingAliasAsync` method as-is
**Rationale**: Method already implements the matching logic specified in FR-005 and FR-006
**Alternatives Considered**: None - existing implementation meets all requirements

### VendorAlias Entity (Already Implemented)

**Location**: `backend/src/ExpenseFlow.Core/Entities/VendorAlias.cs`

**Relevant Fields**:
| Field | Type | Purpose |
|-------|------|---------|
| CanonicalName | string | Internal identifier for vendor |
| DisplayName | string | Human-readable name (what we extract) |
| AliasPattern | string | Pattern to match in descriptions |
| Confidence | decimal | Match quality score (0-1) |
| MatchCount | int | Usage frequency |
| LastMatchedAt | DateTime? | Last match timestamp |

**Decision**: Use `DisplayName` as the extracted vendor name
**Rationale**: DisplayName is specifically designed for human-readable output
**Alternatives Considered**: CanonicalName (rejected - internal use only, not user-friendly)

### RecordMatchAsync Method (Already Implemented)

**Location**: `VendorAliasService.RecordMatchAsync(Guid aliasId)`

**Behavior**:
- Increments MatchCount
- Updates LastMatchedAt timestamp

**Decision**: Call RecordMatchAsync after successful vendor extraction
**Rationale**: Fulfills FR-008 and FR-009; improves future match prioritization
**Alternatives Considered**: None - this is the designed approach

## Integration Point Analysis

### CategorizationService.GetCategorizationAsync

**Current Implementation** (line 355):
```csharp
Vendor = transaction.Description, // TODO: Extract vendor from description
```

**Required Change**:
1. Call `_vendorAliasService.FindMatchingAliasAsync(transaction.Description)`
2. If match found: use `vendorAlias.DisplayName` and call `RecordMatchAsync`
3. If no match: fall back to `transaction.Description`

**Decision**: Modify single line + add 5-10 lines of vendor extraction logic
**Rationale**: Minimal change with maximum impact; leverages injected dependency
**Alternatives Considered**:
- Create new method for vendor extraction (rejected - over-engineering for 10 lines)
- Extract to separate service (rejected - already have VendorAliasService)

## Performance Considerations

### Current VendorAlias Query Performance

The existing query uses:
```sql
SELECT * FROM vendor_aliases
WHERE description ILIKE '%' || alias_pattern || '%'
ORDER BY confidence DESC, match_count DESC
LIMIT 1
```

**Analysis**:
- Trigram index exists: `CREATE INDEX ix_vendor_aliases_trigram ON vendor_aliases USING gin (alias_pattern gin_trgm_ops)`
- Current VendorAlias count: ~100-500 patterns (seeded data)
- Expected query time: <10ms based on existing usage patterns

**Decision**: No performance optimization needed
**Rationale**: Well under the 100ms budget (SC-003); existing index sufficient
**Alternatives Considered**: In-memory caching (rejected - premature optimization)

## Test Strategy

### Unit Tests Required

| Test Case | Description |
|-----------|-------------|
| Vendor extraction with match | Description matches alias → return DisplayName |
| Vendor extraction no match | Description doesn't match → return original description |
| Match recording | Successful match triggers RecordMatchAsync |
| Null/empty description | Graceful handling of edge cases |

**Decision**: Add unit tests to new `CategorizationServiceTests.cs`
**Rationale**: Test-first development per constitution; mock VendorAliasService
**Alternatives Considered**: Integration tests only (rejected - need isolated unit tests)

## Conclusion

No "NEEDS CLARIFICATION" items remain. The feature is a straightforward integration of existing components:

1. ✅ VendorAliasService already implements pattern matching
2. ✅ VendorAlias entity has DisplayName for human-readable output
3. ✅ RecordMatchAsync tracks match statistics
4. ✅ CategorizationService already has VendorAliasService injected
5. ✅ Trigram index ensures query performance

**Ready for Phase 1: Design & Contracts**
