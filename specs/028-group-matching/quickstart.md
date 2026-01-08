# Quickstart: Transaction Group Matching

**Feature**: 028-group-matching
**Date**: 2026-01-07

## Prerequisites

- .NET 8 SDK installed
- PostgreSQL 15+ running (Supabase)
- Backend solution builds successfully
- Existing transaction groups created in the system

## Development Setup

### 1. Switch to Feature Branch

```bash
git checkout 028-group-matching
```

### 2. Verify Backend Builds

```bash
cd backend
dotnet build
```

### 3. Run Existing Tests

```bash
dotnet test
```

## Implementation Order

### Phase 1: Core Logic (Backend)

1. **Extend IMatchingService interface** (`ExpenseFlow.Core/Interfaces/IMatchingService.cs`)
   - Add `GetCandidatesAsync(Guid receiptId)` method signature

2. **Modify MatchingService.RunAutoMatchAsync** (`ExpenseFlow.Infrastructure/Services/MatchingService.cs`)
   - Add parallel query for unmatched groups
   - Filter grouped transactions from individual pool
   - Create unified candidate scoring loop
   - Support group matches in proposal creation

3. **Add vendor extraction for groups** (`MatchingService.cs`)
   - Add `ExtractVendorFromGroupName()` helper method

4. **Extend manual matching** (`MatchingService.CreateManualMatchAsync`)
   - Accept `transactionGroupId` parameter
   - Update group status on confirmation

### Phase 2: API Layer

5. **Add DTOs** (`ExpenseFlow.Shared/DTOs/MatchingDtos.cs`)
   - Add `MatchCandidateDto` class
   - Extend `MatchProposalDto` with `candidateType`
   - Extend `CreateManualMatchRequest` with `transactionGroupId`

6. **Add endpoint** (`ExpenseFlow.Api/Controllers/MatchingController.cs`)
   - Add `GET /api/matching/candidates` endpoint

### Phase 3: Testing

7. **Unit tests** (`ExpenseFlow.Unit.Tests/Services/MatchingServiceGroupTests.cs`)
   - Test group scoring calculation
   - Test grouped transaction exclusion
   - Test ambiguity detection with groups

8. **Integration tests** (`ExpenseFlow.Integration.Tests/Matching/GroupMatchingTests.cs`)
   - Test auto-match with groups
   - Test manual group matching
   - Test unmatch from group

### Phase 4: Frontend

9. **Update match candidate display** (`frontend/src/features/matching/`)
   - Show groups with badge
   - Display combined amount and transaction count

## Key Files to Modify

| File | Changes |
|------|---------|
| `IMatchingService.cs` | Add `GetCandidatesAsync` signature |
| `MatchingService.cs` | Core group matching logic (~200 lines) |
| `MatchingDtos.cs` | Add/extend 3 DTOs |
| `MatchingController.cs` | Add 1 endpoint, extend 1 endpoint |
| `MatchCandidateList.tsx` | Display groups in UI |

## Testing the Feature

### Manual Testing Flow

1. **Create test data**:
   ```sql
   -- Create a group with 3 transactions totaling $50.00
   -- Upload a receipt for $50.00
   ```

2. **Run auto-match**:
   ```bash
   curl -X POST https://localhost:5001/api/matching/auto \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json"
   ```

3. **Verify proposal**:
   ```bash
   curl https://localhost:5001/api/matching/proposals \
     -H "Authorization: Bearer $TOKEN"
   ```

   Expected: Proposal with `candidateType: "group"` and `transactionGroupId` set

4. **Confirm match**:
   ```bash
   curl -X POST https://localhost:5001/api/matching/{proposalId}/confirm \
     -H "Authorization: Bearer $TOKEN"
   ```

5. **Verify group status updated**:
   ```bash
   curl https://localhost:5001/api/transaction-groups/{groupId} \
     -H "Authorization: Bearer $TOKEN"
   ```

   Expected: `matchStatus: "Matched"` and `matchedReceiptId` set

### Unit Test Commands

```bash
# Run only group matching tests
dotnet test --filter "FullyQualifiedName~GroupMatch"

# Run all matching service tests
dotnet test --filter "FullyQualifiedName~MatchingService"
```

## Common Issues

### Issue: Group not appearing as candidate

**Check**:
- Group `matchStatus` is `Unmatched`
- Group `displayDate` is within ±7 days of receipt date
- Group `combinedAmount` is within ±$1.00 of receipt amount

### Issue: Individual transaction matching a group

**Check**:
- Transaction `groupId` is set (should be excluded)
- Query filters are applied correctly

### Issue: Score calculation differs from expectations

**Debug**:
```csharp
_logger.LogDebug("Group {GroupId} scored: Amount={AmountScore}, Date={DateScore}, Vendor={VendorScore}",
    group.Id, amountScore, dateScore, vendorScore);
```

## Definition of Done

- [X] Auto-match proposes groups as candidates
- [X] Grouped transactions excluded from individual matching
- [X] Manual match to group works
- [X] Unmatch from group works
- [X] Group status updates correctly
- [X] Unit tests pass (MatchingServiceGroupTests.cs)
- [X] Integration tests pending (GroupMatchingTests.cs - test infrastructure required)
- [X] <2 second processing time maintained (GroupMatchingPerformanceTest.cs)
- [X] Frontend displays group candidates
