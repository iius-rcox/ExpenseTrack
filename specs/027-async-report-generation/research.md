# Research: Async Report Generation

**Feature Branch**: `027-async-report-generation`
**Date**: 2026-01-05
**Status**: Complete

## Executive Summary

This research validates the technical approach for converting synchronous expense report generation to background job processing. The investigation confirms that:

1. **Hangfire infrastructure is production-ready** with established patterns for job entities, retry logic, and progress tracking
2. **Polly v8 resilience pipeline exists** for AI calls but needs HTTP 429-specific handling for OpenAI rate limiting
3. **ImportJob entity provides a template** for ReportGenerationJob with status, progress, and error tracking
4. **ReportService.GenerateDraftAsync is the target** for extraction into a background job

## Research Questions

### RQ-1: What Hangfire patterns exist in the codebase?

**Finding**: The codebase has a mature Hangfire implementation with:

| Component | Location | Purpose |
|-----------|----------|---------|
| `JobBase.cs` | Infrastructure/Jobs/ | Abstract base class with logging helpers |
| `ProcessReceiptJob.cs` | Infrastructure/Jobs/ | Example with `[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]` |
| `CacheWarmingJob.cs` | Infrastructure/Jobs/ | Batch processing job with progress tracking |
| `BackgroundJobService.cs` | Infrastructure/Services/ | `IBackgroundJobClient` wrapper for enqueue/status |
| `ImportJob.cs` | Core/Entities/ | **Key pattern**: Entity for tracking job state/progress |

**Pattern for ReportGenerationJob**: Follow `ImportJob` entity pattern with:
- Status enum (Pending, Processing, Completed, Failed, Cancelled)
- Progress fields (ProcessedRecords, TotalRecords)
- Error tracking (ErrorLog)
- Timestamps (StartedAt, CompletedAt)

### RQ-2: How should rate limiting (HTTP 429) be handled?

**Current State**: Polly v8 pipeline exists in `ServiceCollectionExtensions.cs:95-111`:
```csharp
services.AddResiliencePipeline("ai-calls", builder =>
{
    builder
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(1),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<HttpRequestException>()
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            BreakDuration = TimeSpan.FromSeconds(60),
            MinimumThroughput = 5
        });
});
```

**Gap**: The current `ShouldHandle` only catches `HttpRequestException` but doesn't specifically handle HTTP 429 responses. Azure OpenAI returns 429 with `Retry-After` header.

**Recommended Enhancement**:
```csharp
ShouldHandle = new PredicateBuilder()
    .Handle<HttpRequestException>()
    .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.TooManyRequests)
```

Also add jitter to prevent thundering herd:
```csharp
UseJitter = true,
Delay = TimeSpan.FromSeconds(2), // Increased base delay for rate limits
MaxRetryAttempts = 5 // More attempts for transient rate limiting
```

### RQ-3: What is the current synchronous flow?

**Analysis of `ReportService.GenerateDraftAsync`** (lines 46-250):

1. **Parse period** → Extract date range
2. **Delete existing draft** → Prevent duplicates
3. **Fetch matches** → `_matchRepository.GetConfirmedByPeriodAsync()`
4. **Fetch unmatched** → `_transactionRepository.GetUnmatchedByPeriodAsync()`
5. **Fetch predictions** → `_predictionService.GetPredictedTransactionsForPeriodAsync()`
6. **Process each match** (BOTTLENECK):
   - `GetCategorizationSafeAsync()` → AI call
   - `NormalizeDescriptionSafeAsync()` → AI call
7. **Process each unmatched** (BOTTLENECK):
   - Same AI calls per transaction
8. **Save report** → `_reportRepository.AddAsync()`

**Bottleneck Analysis**:
- Each transaction requires 2 AI API calls (categorization + normalization)
- 300 lines × 2 calls = 600 AI calls
- With rate limiting (429s), each call can retry up to 3 times
- Worst case: 1,800 API calls with exponential backoff delays

### RQ-4: How should progress be tracked?

**Decision**: Database-backed progress (not in-memory/Redis)

| Approach | Pros | Cons | Verdict |
|----------|------|------|---------|
| In-memory | Fast | Lost on pod restart | ❌ |
| Redis | Fast, persistent | Extra dependency | ❌ |
| Database | Persistent, queryable | Slightly slower | ✅ |

**Rationale**:
- Job runs for 5-10 minutes; database write every ~1 second is negligible
- Aligns with existing `ImportJob` pattern
- No additional infrastructure required
- Query-friendly for history/analytics

**Progress Update Frequency**: Update database every N lines (batch updates):
```csharp
// Update progress every 10 lines or 5 seconds, whichever comes first
if (processedCount % 10 == 0 || (DateTime.UtcNow - lastUpdate) > TimeSpan.FromSeconds(5))
{
    await _jobRepository.UpdateProgressAsync(jobId, processedCount, totalCount);
    lastUpdate = DateTime.UtcNow;
}
```

### RQ-5: How should the frontend poll for progress?

**Decision**: TanStack Query with adaptive polling

```typescript
// Fast polling during processing, slow when idle
const { data: job } = useQuery({
  queryKey: ['reportJob', jobId],
  queryFn: () => fetchJobStatus(jobId),
  refetchInterval: (query) => {
    const status = query.state.data?.status;
    if (status === 'completed' || status === 'failed') return false; // Stop polling
    if (status === 'processing') return 2000; // 2s during active processing
    return 5000; // 5s when queued
  },
});
```

This pattern:
- Provides responsive updates during processing
- Reduces server load when job is queued
- Automatically stops when job completes

### RQ-6: How should cancellation work?

**Pattern**: Cooperative cancellation via CancellationToken

1. User clicks Cancel → API sets job status to `CancellationRequested`
2. Job checks `_cancellationToken.IsCancellationRequested` or queries job status between lines
3. Job completes gracefully, sets status to `Cancelled`

**Implementation**:
```csharp
foreach (var transaction in transactions)
{
    // Check for cancellation before each expensive operation
    if (await ShouldCancelAsync(jobId))
    {
        job.Status = JobStatus.Cancelled;
        job.CompletedAt = DateTime.UtcNow;
        await _jobRepository.UpdateAsync(job);
        return;
    }

    await ProcessLineAsync(transaction);
}
```

### RQ-7: How should duplicate jobs be prevented?

**Decision**: Database constraint + API check

1. Add unique index on `(UserId, Period)` for active jobs (status NOT IN (Completed, Failed, Cancelled))
2. API checks for existing active job before creating new one
3. Return 409 Conflict with job ID if duplicate detected

```sql
CREATE UNIQUE INDEX ix_report_jobs_user_period_active
ON report_generation_jobs (user_id, period)
WHERE status NOT IN ('Completed', 'Failed', 'Cancelled');
```

## Technical Decisions

### TD-1: Entity Design

Follow `ImportJob` pattern:

```csharp
public class ReportGenerationJob : BaseEntity
{
    public Guid UserId { get; set; }
    public string Period { get; set; } = string.Empty;
    public ReportJobStatus Status { get; set; } = ReportJobStatus.Pending;
    public int TotalLines { get; set; }
    public int ProcessedLines { get; set; }
    public int CategorizationRetries { get; set; }
    public string? ErrorMessage { get; set; }
    public string? HangfireJobId { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? GeneratedReportId { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public ExpenseReport? GeneratedReport { get; set; }
}

public enum ReportJobStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    CancellationRequested = 5
}
```

### TD-2: API Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `POST /api/report-jobs` | Create | Start new generation job |
| `GET /api/report-jobs/{id}` | Read | Get job status/progress |
| `GET /api/report-jobs` | List | Get user's job history |
| `DELETE /api/report-jobs/{id}` | Cancel | Request cancellation |

### TD-3: Hangfire Job Configuration

```csharp
[AutomaticRetry(Attempts = 0)] // Disable Hangfire retry - we handle internally
[DisableConcurrentExecution(timeoutInSeconds: 600)]
public class ReportGenerationBackgroundJob : JobBase
{
    public async Task ExecuteAsync(Guid jobId, CancellationToken cancellationToken)
    {
        // Job logic here - uses database job entity for state
    }
}
```

**Rationale for `Attempts = 0`**:
- We track retries per-line, not per-job
- If entire job fails (e.g., DB connection), mark as Failed for user to retry manually
- Prevents zombie jobs from Hangfire's perspective

### TD-4: Rate Limit Resilience Strategy

Layer 1: **Per-line retry with exponential backoff**
- Up to 3 retries per AI call
- Backoff: 2s → 4s → 8s with jitter

Layer 2: **Fallback on exhaustion**
- After max retries, use original description (no normalization)
- Flag line for manual review: `RequiresManualCategorization = true`

Layer 3: **Dynamic throttling**
- If 429 rate > 20% in rolling 30-second window, pause for 30 seconds
- Log: "High rate limiting detected, pausing processing"

### TD-5: 30-Day Retention

Implement via Hangfire recurring job:
```csharp
RecurringJob.AddOrUpdate<ReportJobCleanupJob>(
    "cleanup-old-report-jobs",
    x => x.ExecuteAsync(CancellationToken.None),
    Cron.Daily(3, 0)); // Run at 3 AM

public class ReportJobCleanupJob
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);
        await _repository.DeleteOlderThanAsync(cutoff, ct);
    }
}
```

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Rate limiting causes job to exceed 10-minute goal | Medium | Dynamic throttling, fallback to original descriptions |
| Pod restart during processing | Low | Job state in database; Hangfire re-executes from start but existing partial report is deleted |
| Database contention from progress updates | Low | Batch updates every 10 lines or 5 seconds |
| User confusion about async flow | Medium | Clear UI with progress indicator and estimated completion |

## Out of Scope (Confirmed)

- Real-time push (WebSocket/SignalR) - polling is sufficient for MVP
- Priority queuing - single user per job, no queue contention expected
- Automatic retry of failed jobs - user must manually retry

## Dependencies Confirmed

- Hangfire 1.8+ (existing) ✅
- Polly 8.3.0 (existing) ✅
- Microsoft.Extensions.Resilience 8.0.0 (existing) ✅
- TanStack Query (existing frontend) ✅

## References

- `ImportJob.cs` - Entity pattern template
- `ProcessReceiptJob.cs` - Hangfire job with retry configuration
- `BackgroundJobService.cs` - Job enqueue/status service
- `ServiceCollectionExtensions.cs:95-111` - Polly resilience pipeline
- `ReportService.GenerateDraftAsync` - Code to extract into background job
