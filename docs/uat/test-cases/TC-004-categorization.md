# TC-004: AI Categorization

**Test Case ID**: TC-004
**Feature**: AI Categorization with 3-Tier System
**Sprint**: 6
**Priority**: High
**Status**: Not Started

## Description
Validate the 3-tier AI categorization system that assigns GL codes and departments to expenses using cached lookups (Tier 1), embedding similarity (Tier 2), and LLM inference (Tier 3).

## Preconditions
1. User is authenticated and logged into the application
2. Cache warming completed (>500 descriptions in cache)
3. Vendor aliases populated (>100 aliases)
4. Expense embeddings generated (>500 embeddings)
5. Uncategorized transactions available for testing

## Test Data Required
- Transactions with descriptions matching cached entries (Tier 1 test)
- Transactions with descriptions similar to cached entries (Tier 2 test)
- Transactions with unique descriptions (Tier 3 test)
- Reference data: GL codes, departments, categories

## Test Scenarios

### Scenario 1: Tier 1 - Description Cache Hit
**Steps**:
1. Find transaction with description "SAFEWAY #1234" (known cached description)
2. Request categorization for the transaction
3. Verify category assignment uses cached result
4. Check that no AI API call was made (tier indicator)

**Expected Result**:
- Categorization completes instantly (<100ms)
- Category matches cached entry
- Tier indicator shows "Tier 1 - Cache"
- No billable AI usage for this categorization

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 2: Tier 2 - Embedding Similarity Match
**Steps**:
1. Find transaction with description similar to cached entry
   (e.g., "SAFEWAY GROCERY 5678" when "SAFEWAY #1234" is cached)
2. Request categorization
3. Verify embedding similarity search is used
4. Check category is inferred from similar expense

**Expected Result**:
- Categorization completes quickly (<500ms)
- Category matches similar cached entry
- Tier indicator shows "Tier 2 - Similarity"
- Similarity score displayed (e.g., 0.92)

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 3: Tier 3 - LLM Inference
**Steps**:
1. Find transaction with unique description (no cache/similarity match)
   (e.g., "XYZ UNIQUE VENDOR 12345")
2. Request categorization
3. Verify LLM is called for inference
4. Check category suggestion and confidence

**Expected Result**:
- Categorization takes longer (1-3s)
- Category suggested by LLM
- Tier indicator shows "Tier 3 - AI"
- Result is cached for future lookups

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 4: Vendor Alias Recognition
**Steps**:
1. Create transaction with vendor alias pattern (e.g., "STARBUCKS STORE #456")
2. Verify vendor alias "STARBUCKS" is recognized
3. Check default GL code and department from alias
4. Verify categorization uses alias defaults

**Expected Result**:
- Vendor pattern extracted correctly
- Alias matched to default GL code
- Default department applied
- Category consistent with alias configuration

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 5: Manual Category Override
**Steps**:
1. Find a categorized transaction
2. Change the category manually
3. Save the change
4. Verify the manual override is persisted
5. Trigger re-categorization and verify manual override is respected

**Expected Result**:
- Manual category change allowed
- Change saved successfully
- Re-categorization does not overwrite manual assignments
- Manual override flag/indicator visible

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 6: Batch Categorization
**Steps**:
1. Select multiple uncategorized transactions (5-10)
2. Trigger batch categorization
3. Monitor progress as each transaction is categorized
4. Verify all transactions receive categories

**Expected Result**:
- Batch operation completes for all selected
- Mix of Tier 1/2/3 used as appropriate
- Progress indicator shows completion %
- All transactions categorized upon completion

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

### Scenario 7: Cache Statistics Verification
**Steps**:
1. Navigate to Cache Statistics page
2. Review tier usage statistics
3. Verify counts reflect actual categorization activity
4. Check hit rates and cost savings

**Expected Result**:
- Tier 1/2/3 usage counts displayed
- Hit rate percentage accurate
- Cost savings estimate shown
- Statistics refresh correctly

**Actual Result**: _[To be filled during execution]_

**Pass/Fail**: [ ] Pass [ ] Fail

---

## Summary

| Scenario | Result | Notes |
|----------|--------|-------|
| 1. Tier 1 Cache Hit | - | - |
| 2. Tier 2 Similarity | - | - |
| 3. Tier 3 LLM | - | - |
| 4. Vendor Alias | - | - |
| 5. Manual Override | - | - |
| 6. Batch Categorization | - | - |
| 7. Cache Statistics | - | - |

**Overall Status**: Not Started

**Executed By**: _[Name]_
**Execution Date**: _[Date]_

## Linked Defects
- _None yet_

## Notes
_[Any observations during testing]_
