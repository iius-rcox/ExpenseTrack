# Quickstart: Async Report Generation

**Feature Branch**: `027-async-report-generation`
**Date**: 2026-01-05
**Prerequisites**: .NET 8 SDK, Docker, PostgreSQL (Supabase)

## Overview

This guide walks through the implementation of async report generation with background job processing and real-time progress tracking.

## Step 1: Database Migration

### Create Migration

```bash
cd backend/src/ExpenseFlow.Api
dotnet ef migrations add AddReportGenerationJobs --project ../ExpenseFlow.Infrastructure --context ExpenseFlowDbContext
```

### Apply to Staging

```bash
# Connect to Supabase PostgreSQL
kubectl exec -it $(kubectl get pods -n expenseflow-dev -l app.kubernetes.io/name=supabase-db -o jsonpath='{.items[0].metadata.name}') -n expenseflow-dev -- psql -U postgres -d expenseflow_staging

# Run migration SQL (see data-model.md for full script)
CREATE TABLE report_generation_jobs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id),
    period VARCHAR(7) NOT NULL,
    status INTEGER NOT NULL DEFAULT 0,
    -- ... (full schema in data-model.md)
);

# Record migration
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('YYYYMMDDHHMMSS_AddReportGenerationJobs', '8.0.0');
```

## Step 2: Backend Implementation

### 2.1 Create Entity

```csharp
// backend/src/ExpenseFlow.Core/Entities/ReportGenerationJob.cs
namespace ExpenseFlow.Core.Entities;

public class ReportGenerationJob : BaseEntity
{
    public Guid UserId { get; set; }
    public string Period { get; set; } = string.Empty;
    public ReportJobStatus Status { get; set; } = ReportJobStatus.Pending;
    public int TotalLines { get; set; }
    public int ProcessedLines { get; set; }
    public int FailedLines { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorDetails { get; set; }
    public string? HangfireJobId { get; set; }
    public DateTime? EstimatedCompletionAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? GeneratedReportId { get; set; }

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

### 2.2 Create Repository Interface

```csharp
// backend/src/ExpenseFlow.Core/Interfaces/IReportJobRepository.cs
namespace ExpenseFlow.Core.Interfaces;

public interface IReportJobRepository
{
    Task<ReportGenerationJob?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ReportGenerationJob?> GetActiveByUserAndPeriodAsync(Guid userId, string period, CancellationToken ct = default);
    Task<List<ReportGenerationJob>> GetByUserAsync(Guid userId, ReportJobStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetCountByUserAsync(Guid userId, ReportJobStatus? status, CancellationToken ct = default);
    Task AddAsync(ReportGenerationJob job, CancellationToken ct = default);
    Task UpdateAsync(ReportGenerationJob job, CancellationToken ct = default);
    Task UpdateProgressAsync(Guid jobId, int processedLines, int totalLines, CancellationToken ct = default);
    Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default);
}
```

### 2.3 Create Background Job

```csharp
// backend/src/ExpenseFlow.Infrastructure/Jobs/ReportGenerationBackgroundJob.cs
using Hangfire;

namespace ExpenseFlow.Infrastructure.Jobs;

[AutomaticRetry(Attempts = 0)] // We handle retries per-line, not per-job
[DisableConcurrentExecution(timeoutInSeconds = 600)]
public class ReportGenerationBackgroundJob : JobBase
{
    private readonly IReportJobRepository _jobRepository;
    private readonly IReportService _reportService;
    private readonly ICategorizationService _categorizationService;
    private readonly ILogger<ReportGenerationBackgroundJob> _logger;

    public ReportGenerationBackgroundJob(
        IReportJobRepository jobRepository,
        IReportService reportService,
        ICategorizationService categorizationService,
        ILogger<ReportGenerationBackgroundJob> logger)
        : base(logger)
    {
        _jobRepository = jobRepository;
        _reportService = reportService;
        _categorizationService = categorizationService;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
        {
            _logger.LogError("Job {JobId} not found", jobId);
            return;
        }

        try
        {
            job.Status = ReportJobStatus.Processing;
            job.StartedAt = DateTime.UtcNow;
            await _jobRepository.UpdateAsync(job, cancellationToken);

            // Call the existing GenerateDraftAsync logic
            // (This will be refactored to support progress callbacks)
            var report = await _reportService.GenerateDraftWithProgressAsync(
                job.UserId,
                job.Period,
                async (processed, total) =>
                {
                    await _jobRepository.UpdateProgressAsync(jobId, processed, total, cancellationToken);
                },
                async () => await ShouldCancelAsync(jobId),
                cancellationToken);

            job.Status = ReportJobStatus.Completed;
            job.GeneratedReportId = report.Id;
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (OperationCanceledException)
        {
            job.Status = ReportJobStatus.Cancelled;
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed", jobId);
            job.Status = ReportJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.ErrorDetails = ex.ToString();
            job.CompletedAt = DateTime.UtcNow;
        }

        await _jobRepository.UpdateAsync(job, cancellationToken);
    }

    private async Task<bool> ShouldCancelAsync(Guid jobId)
    {
        var job = await _jobRepository.GetByIdAsync(jobId);
        return job?.Status == ReportJobStatus.CancellationRequested;
    }
}
```

### 2.4 Create Controller

```csharp
// backend/src/ExpenseFlow.Api/Controllers/ReportJobsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/report-jobs")]
public class ReportJobsController : ControllerBase
{
    private readonly IReportJobService _jobService;
    private readonly ILogger<ReportJobsController> _logger;

    public ReportJobsController(
        IReportJobService jobService,
        ILogger<ReportJobsController> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReportJobDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateReportJobRequest request,
        CancellationToken ct)
    {
        var userId = User.GetUserId();

        // Check for existing active job
        var existingJob = await _jobService.GetActiveByUserAndPeriodAsync(userId, request.Period, ct);
        if (existingJob != null)
        {
            return Problem(
                title: "Conflict",
                detail: $"An active report generation job already exists for period {request.Period}",
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["existingJobId"] = existingJob.Id }
            );
        }

        var job = await _jobService.CreateAsync(userId, request.Period, ct);

        return AcceptedAtAction(nameof(Get), new { jobId = job.Id }, job);
    }

    [HttpGet("{jobId:guid}")]
    [ProducesResponseType(typeof(ReportJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid jobId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var job = await _jobService.GetByIdAsync(userId, jobId, ct);

        if (job == null)
        {
            return NotFound();
        }

        return Ok(job);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ReportJobListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] ReportJobStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var userId = User.GetUserId();
        var response = await _jobService.GetListAsync(userId, status, page, pageSize, ct);
        return Ok(response);
    }

    [HttpDelete("{jobId:guid}")]
    [ProducesResponseType(typeof(ReportJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid jobId, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var result = await _jobService.CancelAsync(userId, jobId, ct);

        if (result == null)
        {
            return NotFound();
        }

        if (result.Status is ReportJobStatus.Completed or ReportJobStatus.Failed or ReportJobStatus.Cancelled)
        {
            return Problem(
                title: "Bad Request",
                detail: $"Cannot cancel job with status {result.Status}",
                statusCode: StatusCodes.Status400BadRequest
            );
        }

        return Ok(result);
    }
}
```

## Step 3: Frontend Implementation

### 3.1 Create Query Hook

```typescript
// frontend/src/hooks/queries/use-report-jobs.ts
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { apiFetch } from '@/services/api'
import type { ReportJobDto, CreateReportJobRequest, ReportJobListResponse } from '@/types/api'

export const reportJobKeys = {
  all: ['reportJobs'] as const,
  lists: () => [...reportJobKeys.all, 'list'] as const,
  list: (filters: Record<string, unknown>) => [...reportJobKeys.lists(), filters] as const,
  details: () => [...reportJobKeys.all, 'detail'] as const,
  detail: (id: string) => [...reportJobKeys.details(), id] as const,
}

export function useReportJob(jobId: string | undefined) {
  return useQuery({
    queryKey: reportJobKeys.detail(jobId ?? ''),
    queryFn: () => apiFetch<ReportJobDto>(`/report-jobs/${jobId}`),
    enabled: !!jobId,
    refetchInterval: (query) => {
      const status = query.state.data?.status
      // Stop polling when job is finished
      if (status === 'Completed' || status === 'Failed' || status === 'Cancelled') {
        return false
      }
      // Fast polling during processing
      if (status === 'Processing') {
        return 2000
      }
      // Slower polling when pending
      return 5000
    },
  })
}

export function useCreateReportJob() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (data: CreateReportJobRequest) => {
      return apiFetch<ReportJobDto>('/report-jobs', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      })
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: reportJobKeys.lists() })
    },
  })
}

export function useCancelReportJob() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (jobId: string) => {
      return apiFetch<ReportJobDto>(`/report-jobs/${jobId}`, {
        method: 'DELETE',
      })
    },
    onSuccess: (_data, jobId) => {
      queryClient.invalidateQueries({ queryKey: reportJobKeys.detail(jobId) })
      queryClient.invalidateQueries({ queryKey: reportJobKeys.lists() })
    },
  })
}

export function useReportJobList(params: { status?: string; page?: number; pageSize?: number } = {}) {
  const { page = 1, pageSize = 20, status } = params

  return useQuery({
    queryKey: reportJobKeys.list({ page, pageSize, status }),
    queryFn: async () => {
      const searchParams = new URLSearchParams()
      searchParams.set('page', String(page))
      searchParams.set('pageSize', String(pageSize))
      if (status) searchParams.set('status', status)

      return apiFetch<ReportJobListResponse>(`/report-jobs?${searchParams}`)
    },
  })
}
```

### 3.2 Create Progress Component

```tsx
// frontend/src/components/reports/report-generation-progress.tsx
import { Progress } from '@/components/ui/progress'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { useReportJob, useCancelReportJob } from '@/hooks/queries/use-report-jobs'
import { formatDistanceToNow } from 'date-fns'
import { Loader2, CheckCircle, XCircle, Clock } from 'lucide-react'

interface Props {
  jobId: string
  onComplete?: (reportId: string) => void
}

export function ReportGenerationProgress({ jobId, onComplete }: Props) {
  const { data: job, isLoading } = useReportJob(jobId)
  const cancelMutation = useCancelReportJob()

  // Notify parent when completed
  if (job?.status === 'Completed' && job.generatedReportId && onComplete) {
    onComplete(job.generatedReportId)
  }

  if (isLoading || !job) {
    return (
      <Card>
        <CardContent className="flex items-center justify-center py-8">
          <Loader2 className="h-6 w-6 animate-spin" />
        </CardContent>
      </Card>
    )
  }

  const statusIcon = {
    Pending: <Clock className="h-5 w-5 text-yellow-500" />,
    Processing: <Loader2 className="h-5 w-5 animate-spin text-blue-500" />,
    Completed: <CheckCircle className="h-5 w-5 text-green-500" />,
    Failed: <XCircle className="h-5 w-5 text-red-500" />,
    Cancelled: <XCircle className="h-5 w-5 text-gray-500" />,
    CancellationRequested: <Loader2 className="h-5 w-5 animate-spin text-orange-500" />,
  }[job.status]

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle className="flex items-center gap-2">
          {statusIcon}
          <span>Report Generation - {job.period}</span>
        </CardTitle>
        {(job.status === 'Pending' || job.status === 'Processing') && (
          <Button
            variant="outline"
            size="sm"
            onClick={() => cancelMutation.mutate(jobId)}
            disabled={cancelMutation.isPending}
          >
            Cancel
          </Button>
        )}
      </CardHeader>
      <CardContent className="space-y-4">
        <Progress value={job.progressPercent} />

        <div className="flex justify-between text-sm text-muted-foreground">
          <span>{job.statusMessage}</span>
          <span>{job.processedLines} / {job.totalLines} lines</span>
        </div>

        {job.estimatedCompletionAt && job.status === 'Processing' && (
          <p className="text-sm text-muted-foreground">
            Estimated completion: {formatDistanceToNow(new Date(job.estimatedCompletionAt), { addSuffix: true })}
          </p>
        )}

        {job.status === 'Failed' && job.errorMessage && (
          <div className="rounded-md bg-red-50 p-3 text-sm text-red-700">
            {job.errorMessage}
          </div>
        )}

        {job.failedLines > 0 && job.status === 'Completed' && (
          <p className="text-sm text-yellow-600">
            {job.failedLines} lines require manual categorization
          </p>
        )}
      </CardContent>
    </Card>
  )
}
```

## Step 4: Test Locally

### Run Backend Tests

```bash
cd backend
dotnet test --filter "FullyQualifiedName~ReportGeneration"
```

### Run Integration Test

```bash
# Start local services
docker-compose -f docker-compose.test.yml up -d

# Run integration tests
dotnet test backend/tests/ExpenseFlow.Api.Tests --filter "Category=Integration"
```

### Manual Test with curl

```bash
# Start job
curl -X POST http://localhost:5000/api/report-jobs \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"period": "2026-01"}'

# Poll status
curl http://localhost:5000/api/report-jobs/{jobId} \
  -H "Authorization: Bearer $TOKEN"

# Cancel job
curl -X DELETE http://localhost:5000/api/report-jobs/{jobId} \
  -H "Authorization: Bearer $TOKEN"
```

## Step 5: Deploy to Staging

```bash
# Build and push backend
cd backend
docker buildx build --platform linux/amd64 -t iiusacr.azurecr.io/expenseflow-api:v1.7.0-xxx --push .

# Update deployment
kubectl set image deployment/expenseflow-api \
  api=iiusacr.azurecr.io/expenseflow-api:v1.7.0-xxx \
  -n expenseflow-staging

# Apply migration (see Step 1)

# Build and push frontend
cd ../frontend
docker buildx build --platform linux/amd64 -t iiusacr.azurecr.io/expenseflow-frontend:v1.4.0-xxx --push .

# Update frontend deployment
kubectl set image deployment/expenseflow-frontend \
  frontend=iiusacr.azurecr.io/expenseflow-frontend:v1.4.0-xxx \
  -n expenseflow-staging
```

## Verification Checklist

- [ ] Database migration applied successfully
- [ ] API returns 202 Accepted when creating job
- [ ] Progress updates visible within 5 seconds
- [ ] Job completes within 10 minutes for 300 lines
- [ ] Cancellation stops job within 10 seconds
- [ ] Duplicate job returns 409 Conflict
- [ ] Failed lines are flagged for manual review
- [ ] Completed job links to generated report
