# ExpenseFlow Background Jobs Documentation

This document provides a comprehensive reference for all ExpenseFlow background jobs running via Hangfire. It is designed to support testing, monitoring, and debugging efforts.

**Technology Stack**:
- Hangfire for job scheduling and execution
- PostgreSQL for job persistence
- .NET 8 with C# 12

---

## Table of Contents

1. [Overview](#overview)
2. [Job Types](#job-types)
3. [Receipt Processing Job](#receipt-processing-job)
4. [Subscription Alert Job](#subscription-alert-job)
5. [Cache Warming Job](#cache-warming-job)
6. [Reference Data Sync Job](#reference-data-sync-job)
7. [Embedding Cleanup Job](#embedding-cleanup-job)
8. [Alias Confidence Decay Job](#alias-confidence-decay-job)
9. [Hangfire Dashboard](#hangfire-dashboard)
10. [Monitoring and Alerts](#monitoring-and-alerts)

---

## Overview

ExpenseFlow uses Hangfire for background job processing with the following key features:

- **Job Persistence**: Jobs are stored in PostgreSQL for reliability
- **Automatic Retries**: Failed jobs retry with exponential backoff
- **Recurring Jobs**: Scheduled tasks run on cron-based schedules
- **Job Dashboard**: Web UI for monitoring (admin only)

### Job Execution Modes

| Mode | Description |
|------|-------------|
| Fire-and-forget | Enqueued for immediate execution |
| Delayed | Scheduled for future execution |
| Recurring | Runs on a cron schedule |
| Continuation | Runs after another job completes |

---

## Job Types

| Job | Type | Schedule | Description |
|-----|------|----------|-------------|
| ProcessReceiptJob | Fire-and-forget | On receipt upload | OCR processing |
| SubscriptionAlertJob | Recurring | Monthly (1st) | Subscription detection |
| CacheWarmingJob | Fire-and-forget | On import upload | Historical data import |
| ReferenceDataSyncJob | Recurring | Daily (overnight) | GL/Dept sync from Vista |
| EmbeddingCleanupJob | Recurring | Monthly | Purge stale embeddings |
| AliasConfidenceDecayJob | Recurring | Weekly | Decay unused alias confidence |

---

## Receipt Processing Job

**Class**: `ExpenseFlow.Infrastructure.Jobs.ProcessReceiptJob`
**Feature**: 003-receipt-pipeline

### Purpose

Processes uploaded receipt images using Azure Document Intelligence to extract:
- Vendor name
- Transaction date
- Total amount
- Tax amount
- Currency
- Line items

Also triggers travel period detection for airline/hotel receipts.

### Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `ReceiptProcessing:ConfidenceThreshold` | 0.60 | Minimum confidence for auto-accept |
| `ReceiptProcessing:MaxRetries` | 3 | Maximum retry attempts |

### Retry Policy

```csharp
[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
```

- Attempt 1: Immediate
- Attempt 2: After 1 minute
- Attempt 3: After 5 minutes
- Attempt 4: After 15 minutes

### Processing Flow

1. **Status Update**: Set receipt status to `Processing`
2. **Download**: Fetch image from Azure Blob Storage
3. **OCR**: Call Azure Document Intelligence API
4. **Date Resolution**: Compare OCR date with filename date (BUG-003 fix)
5. **Vendor Extraction**: Extract vendor from OCR or fallback patterns (BUG-004 fix)
6. **Thumbnail**: Generate and upload thumbnail
7. **Travel Detection**: Check for travel-related receipts (Tier 1 rule-based)
8. **Status Finalize**: Set to `Ready` or `ReviewRequired` based on confidence

### Special Features

#### Date Resolution (BUG-003)
For receipts with multiple dates (e.g., parking entry/exit), the job:
- Extracts date from filename patterns (YYYYMMDD_HHMMSS, YYYY-MM-DD, IMG_YYYYMMDD)
- Prefers later date (typically payment date vs entry date)
- Logs discrepancies for debugging

#### Travel Detection
After processing, triggers Tier 1 (rule-based) travel detection:
- Checks vendor against airline/hotel patterns
- Creates or extends travel periods automatically
- Logs detection results with confidence scores

---

## Subscription Alert Job

**Class**: `ExpenseFlow.Infrastructure.Jobs.SubscriptionAlertJob`
**Feature**: 007-advanced-features

### Purpose

Monthly job that checks for missing subscription payments and generates alerts.

### Schedule

Runs on the 1st of each month to check the previous month's transactions.

### Configuration

None (uses default service configuration).

### Retry Policy

```csharp
[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
```

### Processing Flow

1. **User Discovery**: Find all users with active subscriptions
2. **Monthly Check**: For each user, run `RunMonthlyCheckAsync`
3. **Alert Generation**: Create alerts for missing expected payments
4. **Logging**: Report total alerts generated

### Manual Execution

Can be triggered manually for a specific user via:
```csharp
await job.ExecuteForUserAsync(userId, "2025-12", cancellationToken);
```

Or via API:
```
POST /api/subscriptions/check?month=2025-12
```

---

## Cache Warming Job

**Class**: `ExpenseFlow.Infrastructure.Jobs.CacheWarmingJob`
**Feature**: 010-testing-cache-warming

### Purpose

Processes historical expense data imports to pre-populate:
- Description cache (normalized descriptions)
- Vendor aliases (with GL/department defaults)
- Verified embeddings (for AI categorization)

### Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `CacheWarming:BatchSize` | 100 | Records per processing batch |
| `CacheWarming:SimilarityThreshold` | 0.98 | Threshold for embedding deduplication |

### Retry Policy

```csharp
[AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
```

### Expected Excel Columns

| Column | Required | Description |
|--------|----------|-------------|
| Description | Yes | Transaction description |
| Date | No | Transaction date |
| Vendor | No | Vendor name |
| Amount | No | Transaction amount |
| GL Code | No | General ledger code |
| Department | No | Department code |

### Processing Flow

1. **Download**: Fetch Excel file from Azure Blob Storage
2. **Parse**: Read data with ClosedXML
3. **Batch Processing**: Process records in batches of 100
   - Create DescriptionCache entries
   - Create VendorAlias entries
   - Generate verified embeddings
4. **Progress Updates**: Track processed/skipped/cached counts
5. **Error Handling**: Log errors per row, truncate if too many

### Progress Tracking

Job progress is visible via:
```
GET /api/cachewarming/jobs/{jobId}
```

Returns:
- `processedRecords` / `totalRecords`
- `cachedDescriptions`
- `createdAliases`
- `generatedEmbeddings`
- `skippedRecords`

---

## Reference Data Sync Job

**Class**: `ExpenseFlow.Infrastructure.Jobs.ReferenceDataSyncJob`
**Feature**: 002-core-backend-auth

### Purpose

Syncs GL accounts, departments, and projects from Viewpoint Vista ERP.

### Schedule

Daily overnight run (cron schedule configured in Hangfire setup).

### Source

| Entity | Source Table | Filter |
|--------|--------------|--------|
| GL Accounts | GLAC | Active records |
| Departments | PRDP | PRCo = 1, Active |
| Projects | JCCM | JCCo = 1, Active |

### Processing Flow

1. **Connect**: SQL Server connection via Key Vault credentials
2. **Sync**: Upsert records to PostgreSQL cache
3. **Deactivate**: Mark removed records as inactive
4. **Cascade**: Clear user preferences for deactivated records

### Trigger

Manual trigger available via:
```
POST /api/reference/sync
Authorization: AdminOnly policy
```

---

## Embedding Cleanup Job

**Class**: `ExpenseFlow.Infrastructure.Jobs.EmbeddingCleanupJob`
**Feature**: 006-ai-categorization

### Purpose

Purges stale unverified embeddings to manage database size and maintain embedding quality.

### Schedule

Monthly (configured in Hangfire recurring jobs).

### Rules

- **Verified embeddings** (ExpiresAt = null): Never deleted
- **Unverified embeddings**: Deleted if ExpiresAt has passed

### Configuration

| Setting | Value | Description |
|---------|-------|-------------|
| BatchSize | 100 | Records per deletion batch |
| Delay | 100ms | Pause between batches |

### Processing Flow

1. **Query**: Find embeddings where `ExpiresAt < NOW()`
2. **Batch Delete**: Remove in batches of 100
3. **Throttle**: 100ms delay between batches
4. **Log**: Report total deleted

---

## Alias Confidence Decay Job

**Class**: `ExpenseFlow.Infrastructure.Jobs.AliasConfidenceDecayJob`
**Feature**: 005-matching-engine

### Purpose

Reduces confidence scores for vendor aliases that haven't been matched in 6+ months.
Helps prioritize recently-used aliases over stale ones.

### Schedule

Weekly (configured in Hangfire recurring jobs).

### Configuration

| Setting | Value | Description |
|---------|-------|-------------|
| StaleMonths | 6 | Months without match before decay |
| DecayRate | 0.9 | Multiplier (10% reduction per week) |
| MinConfidenceThreshold | 0.5 | Stop decaying below this value |

### Processing Flow

1. **Query**: Find aliases where:
   - `LastMatchedAt < 6 months ago`
   - `Confidence > 0.5`
2. **Decay**: Multiply confidence by 0.9
3. **Save**: Update all affected aliases
4. **Log**: Report count and timing

### Example Decay

| Week | Confidence | Status |
|------|------------|--------|
| 0 | 1.00 | Stale (6+ months) |
| 1 | 0.90 | Decayed |
| 2 | 0.81 | Decayed |
| 3 | 0.73 | Decayed |
| 4 | 0.66 | Decayed |
| 5 | 0.59 | Decayed |
| 6 | 0.53 | Decayed |
| 7 | 0.48 | Below threshold, stops |

---

## Hangfire Dashboard

### Access

**URL**: `/hangfire`
**Authorization**: AdminOnly policy (requires admin role in Entra ID)

### Features

| Tab | Description |
|-----|-------------|
| Dashboard | Real-time job stats, queue status |
| Jobs | Browse succeeded, failed, processing jobs |
| Retries | View failed jobs pending retry |
| Recurring | Manage scheduled recurring jobs |
| Servers | Active Hangfire server instances |

### Common Operations

1. **Requeue Failed Job**: Click job → "Requeue"
2. **Delete Failed Job**: Click job → "Delete"
3. **Trigger Recurring Job**: Recurring tab → Click "Trigger now"
4. **Disable Recurring Job**: Recurring tab → Click "Disable"

---

## Monitoring and Alerts

### Health Checks

The `/api/health` endpoint includes Hangfire status:
- Server connectivity
- Queue depth
- Failed job count

### Metrics to Monitor

| Metric | Alert Threshold | Description |
|--------|----------------|-------------|
| Failed Jobs | > 10 | Jobs that exceeded retry limit |
| Queue Depth | > 100 | Jobs waiting to be processed |
| Processing Time | > 5 min | Individual job duration |
| Server Count | < 1 | No active Hangfire servers |

### Common Issues

#### Jobs Stuck in Processing

**Cause**: Server crashed while processing
**Fix**: Jobs auto-recover after server timeout (default 30 min)

#### High Queue Depth

**Cause**: More jobs enqueued than processed
**Fix**: Scale Hangfire workers or investigate slow jobs

#### Repeated Failures

**Cause**: External service unavailable (Azure AI, Vista)
**Fix**: Check service health, review error logs

### Error Investigation

1. Open Hangfire Dashboard → Failed Jobs
2. Click on failed job
3. Review exception message and stack trace
4. Check "Job ID" to correlate with application logs
5. Optionally requeue or delete

---

## Testing Considerations

### Unit Testing Jobs

Jobs use `JobBase` which provides:
- `LogJobStart(jobName)`
- `LogJobComplete(jobName, duration)`
- `LogJobFailed(jobName, exception)`

Mock dependencies for isolated testing:
```csharp
var mockLogger = new Mock<ILogger<ProcessReceiptJob>>();
var mockRepository = new Mock<IReceiptRepository>();
// ... setup mocks
var job = new ProcessReceiptJob(...);
await job.ProcessAsync(receiptId);
```

### Integration Testing

For E2E testing, use the test cleanup endpoint:
```
POST /api/test/cleanup
{
  "entityTypes": ["receipts", "transactions", "imports"]
}
```

### Simulating Failures

To test retry behavior:
1. Mock the external service to throw
2. Verify job retries with correct delays
3. Confirm final failure after max attempts

---

## Version History

| Date | Version | Changes |
|------|---------|---------|
| 2025-12-31 | 1.16.0 | Documentation created |
| 2025-12-27 | 1.14.0 | Added EmbeddingCleanupJob |
| 2025-12-25 | 1.13.0 | Added AliasConfidenceDecayJob |
| 2025-12-23 | 1.12.0 | Added CacheWarmingJob |
| 2025-12-21 | 1.11.0 | Added SubscriptionAlertJob |
| 2025-12-19 | 1.10.0 | Added ProcessReceiptJob |
