# ExpenseFlow External Integrations Documentation

This document provides a comprehensive reference for all ExpenseFlow external service integrations. It is designed to support testing, debugging, and operational monitoring.

**Technology Stack**:
- Azure Blob Storage (receipt images, thumbnails, imports)
- Azure Document Intelligence (OCR)
- Azure OpenAI (embeddings, AI inference)
- Viewpoint Vista ERP (reference data)
- Microsoft Entra ID (authentication)

---

## Table of Contents

1. [Azure Blob Storage](#azure-blob-storage)
2. [Azure Document Intelligence](#azure-document-intelligence)
3. [Azure OpenAI Service](#azure-openai-service)
4. [Viewpoint Vista ERP](#viewpoint-vista-erp)
5. [Microsoft Entra ID](#microsoft-entra-id)
6. [Service Health Monitoring](#service-health-monitoring)

---

## Azure Blob Storage

### Purpose

Stores receipt images, thumbnails, and import files with secure, time-limited access.

### Configuration

| Setting | Description | Default |
|---------|-------------|---------|
| `BlobStorage:ConnectionString` | Storage account connection string | Required |
| `BlobStorage:ReceiptsContainer` | Container for receipt images | `receipts` |
| `BlobStorage:ThumbnailsContainer` | Container for thumbnails | `thumbnails` |

### Container Structure

```
receipts/
├── {userId}/
│   ├── 2025/
│   │   ├── 01/
│   │   │   ├── {uniqueId}_receipt.jpg
│   │   │   └── {uniqueId}_receipt.pdf
│   │   └── 12/
│   │       └── ...
│   └── ...

thumbnails/
├── {userId}/
│   └── 2025/
│       └── 01/
│           └── {receiptId}_thumb.jpg

cache-warming/
├── {userId}/
│   └── {jobId}_import.xlsx
```

### Operations

#### Upload Receipt
```csharp
// Generate path following convention
var path = BlobStorageService.GenerateReceiptPath(userId, originalFilename);
// Returns: receipts/{userId}/{year}/{month}/{uuid}_{sanitized_filename}

// Upload with content type
var blobUrl = await _blobStorageService.UploadAsync(stream, path, "image/jpeg");
```

#### Generate SAS URL
```csharp
// Generate 1-hour download URL
var sasUrl = await _blobStorageService.GenerateSasUrlAsync(
    blobUrl,
    TimeSpan.FromHours(1));
```

**SAS Token Details**:
- Read-only permission
- Starts 5 minutes in the past (clock skew tolerance)
- Expires after specified duration (default: 1 hour)

#### Download Blob
```csharp
using var stream = await _blobStorageService.DownloadAsync(blobUrl);
```

### Error Handling

| Error | Cause | Resolution |
|-------|-------|------------|
| `BlobNotFound` | File deleted or URL malformed | Verify blob exists |
| `AuthorizationFailure` | Connection string invalid | Check Key Vault secret |
| `ContainerNotFound` | Container doesn't exist | Will auto-create |

### Testing Considerations

- Test SAS URL expiration behavior
- Test concurrent upload/download
- Test file size limits (25MB for receipts)
- Test HEIC→JPEG conversion pipeline
- Clean up test blobs after tests

---

## Azure Document Intelligence

### Purpose

Extracts structured data from receipt images using the prebuilt-receipt model.

### Configuration

| Setting | Description |
|---------|-------------|
| `DocumentIntelligence:Endpoint` | Azure AI endpoint URL |
| `DocumentIntelligence:ApiKey` | API key (from Key Vault) |

### Extracted Fields

| Field | Type | Description |
|-------|------|-------------|
| MerchantName | string | Vendor/store name |
| MerchantAddress | string | Vendor address (fallback for name) |
| TransactionDate | DateTime | Transaction date |
| Total | currency | Total amount |
| TotalTax | currency | Tax amount |
| Items | array | Line item details |

### Service Class

```csharp
public interface IDocumentIntelligenceService
{
    Task<ReceiptExtractionResult> AnalyzeReceiptAsync(
        Stream documentStream,
        string contentType);
}
```

### Processing Flow

1. **Submit Document**: Upload receipt image as BinaryData
2. **Analyze**: Use `prebuilt-receipt` model
3. **Extract Fields**: Parse structured response
4. **Fallback Logic**: Extract vendor from address if name missing
5. **Return Result**: Populate ReceiptExtractionResult with confidence scores

### Confidence Scoring

Each extracted field includes a confidence score (0.0 - 1.0):

| Field | Typical Confidence | Threshold |
|-------|-------------------|-----------|
| MerchantName | 0.85 - 0.99 | 0.60 |
| TransactionDate | 0.90 - 0.99 | 0.60 |
| Total | 0.92 - 0.99 | 0.60 |

If overall confidence < 0.60, receipt is marked `ReviewRequired`.

### Bug Fixes Implemented

#### BUG-002: Binary Data Submission
**Problem**: Stream was being consumed before submission
**Fix**: Copy to MemoryStream and use `BinaryData.FromBytes()`

#### BUG-003: Date Resolution
**Problem**: Parking receipts have entry and exit dates; OCR may extract wrong one
**Fix**: Compare OCR date with filename date, prefer later date

#### BUG-004: Missing Vendor Name
**Problem**: Some receipts (parking, airport) have vendor in address only
**Fix**: Extract vendor from MerchantAddress using pattern matching (e.g., airport codes)

### Error Handling

| Error | Status Code | Resolution |
|-------|-------------|------------|
| `RequestFailed` | 400 | Invalid image format |
| `Unauthorized` | 401 | API key invalid |
| `ServiceUnavailable` | 503 | Service temporarily down |

### Testing Considerations

- Test various receipt formats (thermal, printed, handwritten)
- Test multi-page PDFs
- Test different currencies
- Test low-quality images
- Test timeout handling (30-second default)

---

## Azure OpenAI Service

### Purpose

Provides embeddings for expense categorization (Tier 2) and AI inference (Tier 3).

### Configuration

| Setting | Description |
|---------|-------------|
| `AzureOpenAI:Endpoint` | Azure OpenAI endpoint |
| `AzureOpenAI:ApiKey` | API key (from Key Vault) |
| `AzureOpenAI:EmbeddingDeployment` | Embedding model deployment name |
| `AzureOpenAI:ChatDeployment` | Chat model deployment name |

### Models Used

| Model | Deployment | Purpose | Dimensions |
|-------|------------|---------|------------|
| text-embedding-3-small | embeddings | Expense categorization | 1536 |
| gpt-4o | chat | Column mapping inference | N/A |

### Embedding Service

```csharp
public interface IEmbeddingService
{
    Task<Vector> GenerateEmbeddingAsync(string text, CancellationToken ct);

    Task<IReadOnlyList<ExpenseEmbedding>> FindSimilarAsync(
        Vector queryEmbedding,
        Guid userId,
        int limit = 5,
        float threshold = 0.92f,
        CancellationToken ct = default);

    Task<ExpenseEmbedding> CreateVerifiedEmbeddingAsync(
        string descriptionText,
        string glCode,
        string department,
        Guid userId,
        Guid transactionId,
        string? vendorNormalized = null,
        CancellationToken ct = default);
}
```

### Tier 2 Categorization Flow

1. **Generate Query Embedding**: Embed expense description (truncated to 500 chars)
2. **Search Similar**: Use pgvector cosine similarity
3. **Filter by Threshold**: Default 0.92 similarity
4. **Return Suggestions**: Up to 5 similar embeddings with GL/department

### Tier 3 AI Inference

Used for:
- Column mapping inference (statement import)
- GL code suggestions (when Tier 1 & 2 fail)
- Department suggestions (when Tier 1 & 2 fail)

### Cost Management

| Operation | Est. Cost | Mitigation |
|-----------|-----------|------------|
| Embedding generation | $0.02/1M tokens | Cache verified embeddings |
| Chat completion | $0.005/1K tokens | Use Tier 1 & 2 first |

Track costs via `TierUsageLog`:
```
GET /api/categorization/stats?startDate=2025-12-01
```

### Error Handling

| Error | Resolution |
|-------|------------|
| Rate limit exceeded | Retry with exponential backoff (Polly) |
| Context length exceeded | Truncate input to 500 chars |
| Service unavailable | Fallback to Tier 1 or manual |

### Testing Considerations

- Test embedding similarity thresholds
- Test rate limiting behavior
- Test fallback when AI unavailable
- Monitor token usage in tests
- Use mock embedding service for unit tests

---

## Viewpoint Vista ERP

### Purpose

Syncs GL accounts, departments, and projects from Viewpoint Vista SQL Server.

### Configuration

| Setting | Description |
|---------|-------------|
| `Vista:ConnectionString` | SQL Server connection string (Key Vault) |
| `Vista:GLCompany` | Company filter for GL accounts |
| `Vista:PRCompany` | Company filter for departments |
| `Vista:JCCompany` | Company filter for projects |

### Source Tables

| Entity | Source Table | Key Column | Filter |
|--------|--------------|------------|--------|
| GL Accounts | GLAC | GLAcct | Active = 'Y' |
| Departments | PRDP | Department | PRCo = 1, ActiveYN = 'Y' |
| Projects | JCCM | Job | JCCo = 1, JobStatus = 1 |

### Sync Service

```csharp
public interface IReferenceDataService
{
    Task<(int glAccounts, int departments, int projects)> SyncAllAsync();
    Task<int> SyncGLAccountsAsync();
    Task<int> SyncDepartmentsAsync();
    Task<int> SyncProjectsAsync();
}
```

### Sync Behavior

1. **Query Source**: Execute SELECT against Vista tables
2. **Upsert**: Insert new records, update existing
3. **Deactivate**: Mark missing records as inactive
4. **Cascade**: Clear user preferences referencing deactivated records
5. **Log**: Record sync results

### Display Format

Entity names are formatted as:
```
{First 25 chars of name} ({Code})
```

Example: `Office Supplies & Equipment (5100)`

### Scheduled Sync

Runs daily overnight via Hangfire:
```cron
0 2 * * * (2:00 AM daily)
```

### Manual Trigger

```bash
POST /api/reference/sync
Authorization: Bearer {admin_token}
```

Response:
```json
{
  "jobId": "...",
  "status": "enqueued",
  "enqueuedAt": "2025-12-31T12:00:00Z"
}
```

### Error Handling

| Error | Resolution |
|-------|------------|
| Connection timeout | Retry up to 3 times |
| SQL exception | Log and alert ops team |
| No records returned | Log warning, keep stale cache |

### Testing Considerations

- Test with mock SQL connection
- Test deactivation cascade
- Test preference clearing
- Test concurrent sync prevention

---

## Microsoft Entra ID

### Purpose

Authentication and authorization for ExpenseFlow users.

### Configuration

| Setting | Description |
|---------|-------------|
| `AzureAd:Instance` | `https://login.microsoftonline.com/` |
| `AzureAd:TenantId` | Organization tenant ID |
| `AzureAd:ClientId` | Application client ID |
| `AzureAd:Audience` | API audience (same as ClientId) |

### JWT Token Claims

| Claim | Mapped To | Description |
|-------|-----------|-------------|
| `oid` | User.EntraObjectId | Azure AD object ID |
| `preferred_username` | User.Email | User email |
| `name` | User.DisplayName | Display name |
| `roles` | Authorization | App roles |

### Authorization Policies

| Policy | Required Role | Usage |
|--------|---------------|-------|
| Default | (authenticated) | All endpoints |
| AdminOnly | ExpenseFlow.Admin | Reference sync, cache stats |

### User Auto-Provisioning

On first authenticated request:
1. Check if user exists by `EntraObjectId`
2. If not, create user record with JWT claims
3. Update `LastLoginAt` timestamp
4. Return user with preferences

### Frontend Authentication

Uses MSAL.js for SPA authentication:
- Authorization code flow with PKCE
- Token caching in browser
- Automatic token refresh
- Redirect to `/login` on 401

### Testing Considerations

- Mock JWT tokens in integration tests
- Test user provisioning flow
- Test role-based authorization
- Test token expiration handling

---

## Service Health Monitoring

### Health Check Endpoint

```bash
GET /api/health
```

**Response**:
```json
{
  "status": "healthy",
  "timestamp": "2025-12-31T12:00:00Z",
  "version": "1.16.0",
  "checks": {
    "database": "healthy",
    "blobStorage": "healthy",
    "documentIntelligence": "healthy",
    "openAI": "healthy",
    "hangfire": "healthy"
  }
}
```

### Dependency Health Checks

| Service | Check Method | Timeout |
|---------|--------------|---------|
| PostgreSQL | Connection query | 5s |
| Blob Storage | Container list | 5s |
| Document Intelligence | API ping | 10s |
| Azure OpenAI | API ping | 10s |
| Hangfire | Server count | 5s |

### Alerts Configuration

| Metric | Threshold | Alert Level |
|--------|-----------|-------------|
| Health check failure | Any service unhealthy | Critical |
| API response time | P95 > 2s | Warning |
| OCR failure rate | > 5% | Warning |
| AI tier cost | > $10/day | Warning |

### Monitoring Dashboard

Access Hangfire dashboard for job monitoring:
```
GET /hangfire
Authorization: AdminOnly policy
```

### Log Correlation

All service calls include correlation ID in logs:
```
[2025-12-31 12:00:00 INF] Processing receipt {ReceiptId} [CorrelationId: abc123]
```

Query logs by correlation ID for end-to-end tracing.

---

## Environment-Specific Configuration

### Development

```json
{
  "BlobStorage": {
    "ConnectionString": "UseDevelopmentStorage=true"
  },
  "DocumentIntelligence": {
    "Endpoint": "https://dev-di.cognitiveservices.azure.com/"
  }
}
```

### Staging

```json
{
  "BlobStorage": {
    "ConnectionString": "@Microsoft.KeyVault(SecretUri=...)"
  },
  "DocumentIntelligence": {
    "Endpoint": "https://staging-di.cognitiveservices.azure.com/"
  }
}
```

### Production

```json
{
  "BlobStorage": {
    "ConnectionString": "@Microsoft.KeyVault(SecretUri=...)"
  },
  "DocumentIntelligence": {
    "Endpoint": "https://prod-di.cognitiveservices.azure.com/"
  }
}
```

---

## Resilience Patterns

### Retry Policy (Polly)

```csharp
// Azure services use exponential backoff
services.AddHttpClient<IAzureService>()
    .AddPolicyHandler(Policy
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, attempt =>
            TimeSpan.FromSeconds(Math.Pow(2, attempt))));
```

### Circuit Breaker

```csharp
// Break after 5 consecutive failures for 30 seconds
services.AddHttpClient<IAzureService>()
    .AddPolicyHandler(Policy
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```

### Fallback Strategy

| Service | Fallback Behavior |
|---------|-------------------|
| Document Intelligence | Mark receipt as Error, allow retry |
| Azure OpenAI | Use Tier 1 (cache) or manual entry |
| Vista ERP | Keep stale reference data cache |
| Blob Storage | Return placeholder image |

---

## Version History

| Date | Version | Changes |
|------|---------|---------|
| 2025-12-31 | 1.16.0 | Documentation created |
| 2025-12-23 | 1.12.0 | Added cache warming blob support |
| 2025-12-21 | 1.11.0 | Added subscription detection |
| 2025-12-19 | 1.10.0 | Added Tier 2/3 AI categorization |
| 2025-12-17 | 1.09.0 | Added Document Intelligence integration |
