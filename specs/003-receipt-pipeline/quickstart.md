# Quickstart: Receipt Upload Pipeline

**Feature Branch**: `003-receipt-pipeline`
**Sprint**: 3 (Weeks 5-6)

## Prerequisites

Before starting Sprint 3 implementation, ensure:

- [ ] Sprint 2 (Core Backend & Auth) is deployed and working
- [ ] Azure Document Intelligence resource exists (`iius-doc-intelligence`)
- [ ] Azure Blob Storage account exists (`ccproctemp2025`)
- [ ] Hangfire is configured and running

## Quick Verification

```bash
# Verify Document Intelligence endpoint
az cognitiveservices account show \
  --name iius-doc-intelligence \
  --resource-group rg_prod \
  --query properties.endpoint

# Verify Blob Storage
az storage account show \
  --name ccproctemp2025 \
  --resource-group rg_prod \
  --query primaryEndpoints.blob

# Verify API is running
kubectl get pods -n expenseflow-dev -l app=expenseflow-api
```

## Step 1: Add NuGet Packages

```bash
cd backend/src/ExpenseFlow.Infrastructure

# Document Intelligence SDK
dotnet add package Azure.AI.DocumentIntelligence --version 1.0.0

# Image processing (for HEIC and thumbnails)
dotnet add package Magick.NET-Q16-AnyCPU

# Polly for retry policies
dotnet add package Microsoft.Extensions.Http.Polly
```

## Step 2: Configure App Settings

Add to `appsettings.json`:

```json
{
  "DocumentIntelligence": {
    "Endpoint": "https://iius-doc-intelligence.cognitiveservices.azure.com/",
    "ApiKey": "" // From Key Vault
  },
  "BlobStorage": {
    "ConnectionString": "", // From Key Vault
    "ReceiptsContainer": "receipts",
    "ThumbnailsContainer": "thumbnails"
  },
  "ReceiptProcessing": {
    "MaxFileSizeMB": 25,
    "MaxBatchSize": 20,
    "ConfidenceThreshold": 0.60,
    "MaxRetries": 3,
    "AllowedContentTypes": [
      "image/jpeg",
      "image/png",
      "image/heic",
      "image/heif",
      "application/pdf"
    ]
  }
}
```

## Step 3: Create EF Core Migration

```bash
cd backend/src/ExpenseFlow.Api

dotnet ef migrations add CreateReceiptsTable \
  --project ../ExpenseFlow.Infrastructure \
  --context ExpenseFlowDbContext

# Apply migration
dotnet ef database update --context ExpenseFlowDbContext
```

## Step 4: Configure Blob Storage Lifecycle Policy

```bash
# Create lifecycle policy for 30-day auto-deletion
az storage account management-policy create \
  --account-name ccproctemp2025 \
  --resource-group rg_prod \
  --policy '{
    "rules": [{
      "name": "deleteReceiptsAfter30Days",
      "enabled": true,
      "type": "Lifecycle",
      "definition": {
        "filters": {
          "blobTypes": ["blockBlob"],
          "prefixMatch": ["receipts/"]
        },
        "actions": {
          "baseBlob": {
            "delete": {
              "daysAfterModificationGreaterThan": 30
            }
          }
        }
      }
    }]
  }'
```

## Step 5: Enable Microsoft Defender for Storage

```bash
az security pricing create \
  --name StorageAccounts \
  --tier Standard
```

## Step 6: Update Kubernetes Secrets

```bash
# Get Document Intelligence key
DOC_INTEL_KEY=$(az cognitiveservices account keys list \
  --name iius-doc-intelligence \
  --resource-group rg_prod \
  --query key1 -o tsv)

# Update secret
kubectl create secret generic expenseflow-secrets \
  --from-literal=doc-intelligence-key=$DOC_INTEL_KEY \
  --namespace expenseflow-dev \
  --dry-run=client -o yaml | kubectl apply -f -
```

## Step 7: Build and Deploy

```bash
cd backend

# Build
dotnet build

# Run tests
dotnet test

# Build Docker image
docker build -t iiusacr.azurecr.io/expenseflow-api:v1.1.0 .

# Push to ACR
docker push iiusacr.azurecr.io/expenseflow-api:v1.1.0

# Update deployment
kubectl set image deployment/expenseflow-api \
  api=iiusacr.azurecr.io/expenseflow-api:v1.1.0 \
  -n expenseflow-dev
```

## Validation Tests

### Test 1: Upload Single Receipt

```bash
# Get auth token (replace with your method)
TOKEN="your-jwt-token"

# Upload a receipt
curl -X POST \
  https://your-api-url/api/receipts \
  -H "Authorization: Bearer $TOKEN" \
  -F "files=@sample-receipt.jpg"

# Expected: 201 with receipt ID, status "Uploaded" or "Processing"
```

### Test 2: List Receipts

```bash
curl -X GET \
  "https://your-api-url/api/receipts?page=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"

# Expected: 200 with paginated list
```

### Test 3: Get Receipt Detail

```bash
curl -X GET \
  "https://your-api-url/api/receipts/{receipt-id}" \
  -H "Authorization: Bearer $TOKEN"

# Expected: 200 with full receipt details including extraction results
```

### Test 4: Verify Extraction (wait ~30 seconds)

```bash
# Poll for status change
curl -X GET \
  "https://your-api-url/api/receipts/{receipt-id}" \
  -H "Authorization: Bearer $TOKEN"

# Expected: status = "Ready" with vendorExtracted, dateExtracted, amountExtracted populated
```

### Test 5: Batch Upload

```bash
curl -X POST \
  https://your-api-url/api/receipts \
  -H "Authorization: Bearer $TOKEN" \
  -F "files=@receipt1.jpg" \
  -F "files=@receipt2.pdf" \
  -F "files=@receipt3.png"

# Expected: 201 with array of receipts
```

### Test 6: Unmatched Receipts

```bash
curl -X GET \
  "https://your-api-url/api/receipts/unmatched" \
  -H "Authorization: Bearer $TOKEN"

# Expected: 200 with receipts where status = "Unmatched"
```

### Test 7: Manual Update

```bash
curl -X PUT \
  "https://your-api-url/api/receipts/{receipt-id}" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "vendorExtracted": "Updated Vendor",
    "amountExtracted": 42.50,
    "dateExtracted": "2025-12-05"
  }'

# Expected: 200 with updated receipt
```

### Test 8: Retry Failed Extraction

```bash
# First, find a receipt with Error status, then:
curl -X POST \
  "https://your-api-url/api/receipts/{receipt-id}/retry" \
  -H "Authorization: Bearer $TOKEN"

# Expected: 202 with status changed to "Processing"
```

## Troubleshooting

### Issue: Document Intelligence returns 503

**Cause**: Service unavailable
**Resolution**: Per spec, this should fail immediately. Check Azure Service Health.

### Issue: HEIC files not processed

**Cause**: Missing Magick.NET native libraries
**Resolution**: Ensure Docker image includes ImageMagick dependencies:
```dockerfile
RUN apt-get update && apt-get install -y libmagickwand-dev
```

### Issue: Extraction returns low confidence

**Cause**: Poor image quality or unusual receipt format
**Resolution**: Receipt will have status "ReviewRequired". User can manually enter data.

### Issue: Blob upload fails

**Cause**: Connection string or CORS misconfiguration
**Resolution**: Verify connection string in Key Vault, check CORS settings on storage account.

## Next Steps

After completing Sprint 3:
1. Run `/speckit.tasks` to generate detailed task list
2. Implement backend services following Clean Architecture
3. Create frontend components for upload and list views
4. Run validation tests
5. Deploy to staging for QA
