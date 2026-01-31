# HANDOFF: Receipt Duplicate Detection Feature

## Summary

Implemented duplicate receipt detection using a hybrid approach:
- **File hash (SHA-256)**: Exact duplicate detection for identical file uploads
- **Content hash (SHA-256)**: Semantic duplicate detection based on extracted data (vendor + date + amount)

This feature prevents users from accidentally uploading the same receipt twice, saving storage costs and avoiding duplicate entries in expense reports.

## Implementation Details

### Database Changes

Added two new columns to the `receipts` table:

| Column | Type | Description |
|--------|------|-------------|
| `file_hash` | VARCHAR(64) | SHA-256 hash of file content (nullable) |
| `content_hash` | VARCHAR(64) | SHA-256 hash of normalized content (nullable) |

Added database indexes for fast lookups:
- `ix_receipts_file_hash` on `(user_id, file_hash)`
- `ix_receipts_content_hash` on `(user_id, content_hash)`

### Code Changes by File

#### 1. `backend/src/ExpenseFlow.Core/Entities/Receipt.cs`
Added properties:
```csharp
public string? FileHash { get; set; }
public string? ContentHash { get; set; }
```

#### 2. `backend/src/ExpenseFlow.Core/Interfaces/IReceiptRepository.cs`
Added methods:
```csharp
Task<Receipt?> FindByFileHashAsync(string fileHash, Guid userId);
Task<Receipt?> FindByContentHashAsync(string contentHash, Guid userId);
```

#### 3. `backend/src/ExpenseFlow.Infrastructure/Repositories/ReceiptRepository.cs`
Implemented repository methods for hash lookups with user isolation (row-level security).

#### 4. `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/ReceiptConfiguration.cs`
Added EF Core configuration for new columns and indexes.

#### 5. `backend/src/ExpenseFlow.Core/Services/ReceiptService.cs`
Added:
- `ComputeFileHashAsync(Stream)` - Static method to compute SHA-256 of file bytes
- `ComputeContentHash(vendor, date, amount)` - Static method to compute SHA-256 of normalized content
- `CheckContentDuplicateAsync(...)` - Method to check for semantic duplicates
- `UploadReceiptAsync(..., allowDuplicates)` - Overload with duplicate detection
- `ReceiptUploadResult` - Result class for upload with duplicate info
- `DuplicateCheckResult` - Result class for content duplicate checks
- `DuplicateType` - Enum (None, ExactFile, SameContent)

Updated:
- Original `UploadReceiptAsync` now computes and stores file hash
- `UpdateReceiptAsync` now computes and updates content hash when extraction data changes

## Tests Added

Created `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/ReceiptDuplicateDetectionTests.cs` with 34 tests covering:

### ComputeFileHash Tests (6 tests)
- `ComputeFileHash_ReturnsConsistentHash_ForSameFile`
- `ComputeFileHash_ReturnsDifferentHash_ForDifferentFiles`
- `ComputeFileHash_ResetsStreamPosition_AfterHashing`
- `ComputeFileHash_ThrowsArgumentNullException_ForNullStream`
- `ComputeFileHash_ReturnsValidHash_ForEmptyStream`
- `ComputeFileHash_ProducesLowercaseHexString`

### ComputeContentHash Tests (14 tests)
- `ComputeContentHash_ReturnsConsistentHash_ForSameContent`
- `ComputeContentHash_ReturnsDifferentHash_ForDifferentVendor`
- `ComputeContentHash_ReturnsDifferentHash_ForDifferentDate`
- `ComputeContentHash_ReturnsDifferentHash_ForDifferentAmount`
- `ComputeContentHash_HandlesNullVendor`
- `ComputeContentHash_HandlesNullDate`
- `ComputeContentHash_HandlesNullAmount`
- `ComputeContentHash_HandlesAllNullValues`
- `ComputeContentHash_NormalizesVendorCasing`
- `ComputeContentHash_NormalizesVendorWhitespace`
- `ComputeContentHash_FormatsAmountWithTwoDecimals`
- `ComputeContentHash_FormatsDateAsIso8601`
- `ComputeContentHash_HandlesSpecialCharactersInVendor`
- `ComputeContentHash_HandlesUnicodeVendorNames`

### Upload Duplicate Detection Tests (5 tests)
- `UploadReceipt_ReturnsConflict_WhenExactDuplicateExists`
- `UploadReceipt_ReturnsConflict_WhenContentDuplicateExists`
- `UploadReceipt_AllowsDuplicate_WhenFlagIsTrue`
- `UploadReceipt_ComputesAndStoresFileHash`
- `UploadReceipt_ChecksDuplicateByDefault`

### Repository Method Tests (4 tests)
- `FindByFileHashAsync_ReturnsReceipt_WhenHashExists`
- `FindByFileHashAsync_ReturnsNull_WhenHashNotFound`
- `FindByFileHashAsync_EnforcesUserIsolation`
- `FindByContentHashAsync_ReturnsReceipts_WhenHashExists`

### Edge Case Tests (5 tests)
- `ComputeContentHash_HandlesNegativeAmount`
- `ComputeContentHash_HandlesLargeAmount`
- `ComputeContentHash_HandlesPrecisionDifferences`
- `ComputeFileHash_HandlesLargeFile`
- `UpdateReceipt_UpdatesContentHash_AfterExtraction`

## Edge Cases Handled

1. **Null/Empty Values**: All hash methods gracefully handle null vendor, date, or amount
2. **Vendor Normalization**: Casing and whitespace are normalized before hashing
3. **Amount Precision**: Amounts are formatted to 2 decimal places (12.5 == 12.50)
4. **Date Format**: Dates are formatted as ISO 8601 (yyyy-MM-dd)
5. **Large Files**: File hash computation is streaming-based, handles large files efficiently
6. **User Isolation**: Duplicate checks are scoped to the current user only
7. **Negative Amounts**: Handled correctly (refunds produce different hash)
8. **Unicode Characters**: Special characters in vendor names are preserved

## Content Hash Format

```
SHA256("{normalizedVendor}|{date:yyyy-MM-dd}|{amount:F2}")
```

Example:
- Vendor: "STARBUCKS COFFEE"
- Date: 2024-06-15
- Amount: 12.50

Content string: `"starbucks coffee|2024-06-15|12.50"`

## API Changes

The `UploadReceiptAsync` method now has an overload:
```csharp
Task<ReceiptUploadResult> UploadReceiptAsync(
    Stream stream,
    string filename,
    string contentType,
    Guid userId,
    bool allowDuplicates);
```

Returns `ReceiptUploadResult` with:
- `IsDuplicate`: Whether a duplicate was found
- `DuplicateType`: None, ExactFile, or SameContent
- `ExistingReceiptId`: ID of the duplicate receipt if found
- `Receipt`: The uploaded receipt (null if duplicate blocked)

## Code Review Fixes Applied

After Code Reviewer feedback, the following improvement was made:

**Culture-Invariant Amount Formatting** (lines 417-418):
```csharp
// Before
var formattedAmount = amount?.ToString("F2") ?? string.Empty;

// After
var formattedAmount = amount?.ToString("F2", CultureInfo.InvariantCulture) ?? string.Empty;
```

This ensures consistent hash generation regardless of server culture settings (e.g., German culture uses comma as decimal separator).

## Build/Test Status

```
Build: SUCCESS (0 errors, 3 warnings - pre-existing)
Tests: 34 passed, 0 failed
Coverage: All new code paths covered
```

## Database Migration Required

The following SQL migration needs to be applied:

```sql
-- Add columns
ALTER TABLE receipts ADD COLUMN IF NOT EXISTS file_hash VARCHAR(64);
ALTER TABLE receipts ADD COLUMN IF NOT EXISTS content_hash VARCHAR(64);

-- Add indexes
CREATE INDEX IF NOT EXISTS ix_receipts_file_hash ON receipts(user_id, file_hash);
CREATE INDEX IF NOT EXISTS ix_receipts_content_hash ON receipts(user_id, content_hash);

-- Record migration
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260131000000_AddReceiptDuplicateDetection', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
```

## Frontend Integration Notes

The frontend should:
1. Update the upload hook to handle 409 Conflict responses
2. Display duplicate warning dialog with options:
   - "Upload Anyway" (calls API with `?allowDuplicates=true`)
   - "Cancel" (discard upload)
3. Show info about the existing duplicate receipt (ID, date uploaded)

## Files Changed

| File | Change Type |
|------|-------------|
| `backend/src/ExpenseFlow.Core/Entities/Receipt.cs` | Modified |
| `backend/src/ExpenseFlow.Core/Interfaces/IReceiptRepository.cs` | Modified |
| `backend/src/ExpenseFlow.Core/Services/ReceiptService.cs` | Modified |
| `backend/src/ExpenseFlow.Infrastructure/Repositories/ReceiptRepository.cs` | Modified |
| `backend/src/ExpenseFlow.Infrastructure/Data/Configurations/ReceiptConfiguration.cs` | Modified |
| `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/ReceiptDuplicateDetectionTests.cs` | Created |
