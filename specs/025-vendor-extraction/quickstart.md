# Quickstart: Vendor Name Extraction

**Feature**: 025-vendor-extraction
**Date**: 2026-01-05

## Overview

This feature extracts clean vendor names from cryptic bank descriptions by integrating the existing VendorAliasService into CategorizationService.

## Implementation Summary

**Files to Modify**: 1 file
**New Files**: 1 test file
**Estimated Effort**: 1-2 hours

## Step-by-Step Implementation

### Step 1: Modify CategorizationService

**File**: `backend/src/ExpenseFlow.Infrastructure/Services/CategorizationService.cs`

**Location**: `GetCategorizationAsync` method, around line 355

**Current Code**:
```csharp
Vendor = transaction.Description, // TODO: Extract vendor from description
```

**New Code**:
```csharp
// Extract vendor name using alias matching
var vendorAlias = await _vendorAliasService.FindMatchingAliasAsync(transaction.Description);
string extractedVendor;

if (vendorAlias != null)
{
    extractedVendor = vendorAlias.DisplayName;
    await _vendorAliasService.RecordMatchAsync(vendorAlias.Id);
    _logger.LogDebug("Extracted vendor {Vendor} from description {Description}",
        extractedVendor, transaction.Description);
}
else
{
    extractedVendor = transaction.Description;
    _logger.LogDebug("No vendor alias match for description {Description}",
        transaction.Description);
}

// Use in DTO construction:
Vendor = extractedVendor,
```

### Step 2: Add Unit Tests

**File**: `backend/tests/ExpenseFlow.UnitTests/Services/CategorizationServiceTests.cs` (new)

**Test Cases**:
1. `GetCategorizationAsync_WhenVendorAliasMatches_ReturnsDisplayName`
2. `GetCategorizationAsync_WhenNoVendorAliasMatch_ReturnsOriginalDescription`
3. `GetCategorizationAsync_WhenVendorAliasMatches_CallsRecordMatch`
4. `GetCategorizationAsync_WhenDescriptionNull_HandlesGracefully`

### Step 3: Build and Test

```bash
# Build
cd backend
dotnet build

# Run unit tests
dotnet test --filter "FullyQualifiedName~CategorizationServiceTests"

# Run all tests
dotnet test
```

### Step 4: Verify Locally

```bash
# Start API
cd backend/src/ExpenseFlow.Api
dotnet run

# Test endpoint (replace with actual transaction ID)
curl -X GET "https://localhost:7001/api/categorization/{transactionId}" \
  -H "Authorization: Bearer {token}"
```

## Verification Checklist

- [x] VendorAliasService.FindMatchingAliasAsync called with transaction description
- [x] DisplayName returned when match found
- [x] Original description returned when no match
- [x] RecordMatchAsync called on successful match
- [x] Logging added for debugging
- [ ] All unit tests pass (requires dotnet CLI)
- [x] No breaking changes to API contract

## Deployment

No special deployment steps required:
- No migrations
- No environment variable changes
- No infrastructure changes

Standard deployment via PR merge → ArgoCD sync.
