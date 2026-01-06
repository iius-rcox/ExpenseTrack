# Data Model: Async Report Generation

**Feature Branch**: `027-async-report-generation`
**Date**: 2026-01-05
**Spec**: [spec.md](./spec.md) | **Research**: [research.md](./research.md)

## Overview

This document defines the data model for background report generation jobs. The design follows the established `ImportJob` entity pattern.

## Entity: ReportGenerationJob

### Entity Definition

```csharp
// File: backend/src/ExpenseFlow.Core/Entities/ReportGenerationJob.cs
namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Tracks the status and progress of expense report generation background jobs.
/// </summary>
public class ReportGenerationJob : BaseEntity
{
    /// <summary>User who initiated the job.</summary>
    public Guid UserId { get; set; }

    /// <summary>Billing period in YYYY-MM format.</summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>Current job status.</summary>
    public ReportJobStatus Status { get; set; } = ReportJobStatus.Pending;

    /// <summary>Total expense lines to process.</summary>
    public int TotalLines { get; set; }

    /// <summary>Number of lines processed so far.</summary>
    public int ProcessedLines { get; set; }

    /// <summary>Number of lines that failed categorization after all retries.</summary>
    public int FailedLines { get; set; }

    /// <summary>Total number of retry attempts due to rate limiting.</summary>
    public int RetryCount { get; set; }

    /// <summary>User-friendly error message if job failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Detailed error log for debugging (not shown to user).</summary>
    public string? ErrorDetails { get; set; }

    /// <summary>Hangfire job ID for correlation.</summary>
    public string? HangfireJobId { get; set; }

    /// <summary>Estimated completion time, updated dynamically based on processing rate.</summary>
    public DateTime? EstimatedCompletionAt { get; set; }

    /// <summary>When processing started (null if still queued).</summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>When processing completed (success, failure, or cancellation).</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>ID of the generated report (null until completed).</summary>
    public Guid? GeneratedReportId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ExpenseReport? GeneratedReport { get; set; }
}
```

### Status Enum

```csharp
// File: backend/src/ExpenseFlow.Core/Entities/ReportJobStatus.cs
namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Status of a report generation background job.
/// </summary>
public enum ReportJobStatus
{
    /// <summary>Job is queued and waiting to start.</summary>
    Pending = 0,

    /// <summary>Job is actively processing expense lines.</summary>
    Processing = 1,

    /// <summary>Job completed successfully; report is ready.</summary>
    Completed = 2,

    /// <summary>Job failed due to an unrecoverable error.</summary>
    Failed = 3,

    /// <summary>Job was cancelled by the user.</summary>
    Cancelled = 4,

    /// <summary>User requested cancellation; job will stop at next checkpoint.</summary>
    CancellationRequested = 5
}
```

### EF Core Configuration

```csharp
// File: backend/src/ExpenseFlow.Infrastructure/Data/Configurations/ReportGenerationJobConfiguration.cs
using ExpenseFlow.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseFlow.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for ReportGenerationJob entity.
/// </summary>
public class ReportGenerationJobConfiguration : IEntityTypeConfiguration<ReportGenerationJob>
{
    public void Configure(EntityTypeBuilder<ReportGenerationJob> builder)
    {
        builder.ToTable("report_generation_jobs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.Period)
            .HasColumnName("period")
            .HasMaxLength(7) // YYYY-MM
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(x => x.TotalLines)
            .HasColumnName("total_lines")
            .HasDefaultValue(0);

        builder.Property(x => x.ProcessedLines)
            .HasColumnName("processed_lines")
            .HasDefaultValue(0);

        builder.Property(x => x.FailedLines)
            .HasColumnName("failed_lines")
            .HasDefaultValue(0);

        builder.Property(x => x.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0);

        builder.Property(x => x.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(500);

        builder.Property(x => x.ErrorDetails)
            .HasColumnName("error_details")
            .HasMaxLength(10000);

        builder.Property(x => x.HangfireJobId)
            .HasColumnName("hangfire_job_id")
            .HasMaxLength(50);

        builder.Property(x => x.EstimatedCompletionAt)
            .HasColumnName("estimated_completion_at");

        builder.Property(x => x.StartedAt)
            .HasColumnName("started_at");

        builder.Property(x => x.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(x => x.GeneratedReportId)
            .HasColumnName("generated_report_id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes
        // Primary query: user's active and recent jobs
        builder.HasIndex(x => new { x.UserId, x.Status })
            .HasDatabaseName("ix_report_generation_jobs_user_status");

        // Cleanup query: find old jobs to delete
        builder.HasIndex(x => x.CompletedAt)
            .HasDatabaseName("ix_report_generation_jobs_completed_at")
            .HasFilter("completed_at IS NOT NULL");

        // Duplicate prevention: only one active job per user+period
        builder.HasIndex(x => new { x.UserId, x.Period })
            .HasDatabaseName("ix_report_generation_jobs_user_period_active")
            .IsUnique()
            .HasFilter("status NOT IN (2, 3, 4)"); // Exclude Completed, Failed, Cancelled

        // Relationships
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.GeneratedReport)
            .WithMany()
            .HasForeignKey(x => x.GeneratedReportId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
```

## Database Schema (PostgreSQL)

```sql
-- Migration: YYYYMMDDHHMMSS_AddReportGenerationJobs.cs

CREATE TABLE report_generation_jobs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    period VARCHAR(7) NOT NULL,
    status INTEGER NOT NULL DEFAULT 0,
    total_lines INTEGER NOT NULL DEFAULT 0,
    processed_lines INTEGER NOT NULL DEFAULT 0,
    failed_lines INTEGER NOT NULL DEFAULT 0,
    retry_count INTEGER NOT NULL DEFAULT 0,
    error_message VARCHAR(500),
    error_details VARCHAR(10000),
    hangfire_job_id VARCHAR(50),
    estimated_completion_at TIMESTAMP WITH TIME ZONE,
    started_at TIMESTAMP WITH TIME ZONE,
    completed_at TIMESTAMP WITH TIME ZONE,
    generated_report_id UUID,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_report_generation_jobs_user
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE RESTRICT,

    CONSTRAINT fk_report_generation_jobs_report
        FOREIGN KEY (generated_report_id) REFERENCES expense_reports(id) ON DELETE SET NULL
);

-- Indexes
CREATE INDEX ix_report_generation_jobs_user_status
    ON report_generation_jobs (user_id, status);

CREATE INDEX ix_report_generation_jobs_completed_at
    ON report_generation_jobs (completed_at)
    WHERE completed_at IS NOT NULL;

-- Unique constraint: prevent duplicate active jobs for same user+period
CREATE UNIQUE INDEX ix_report_generation_jobs_user_period_active
    ON report_generation_jobs (user_id, period)
    WHERE status NOT IN (2, 3, 4);

-- EF Core migration history
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('YYYYMMDDHHMMSS_AddReportGenerationJobs', '8.0.0');
```

## DTOs

### ReportJobDto (Response)

```csharp
// File: backend/src/ExpenseFlow.Shared/DTOs/ReportJobDto.cs
namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// DTO for report generation job status and progress.
/// </summary>
public record ReportJobDto
{
    public Guid Id { get; init; }
    public string Period { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int TotalLines { get; init; }
    public int ProcessedLines { get; init; }
    public int FailedLines { get; init; }

    /// <summary>Progress percentage (0-100).</summary>
    public int ProgressPercent => TotalLines > 0 ? (int)(ProcessedLines * 100.0 / TotalLines) : 0;

    /// <summary>Human-readable status message.</summary>
    public string StatusMessage { get; init; } = string.Empty;

    public DateTime? EstimatedCompletionAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public Guid? GeneratedReportId { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

### CreateReportJobRequest

```csharp
// File: backend/src/ExpenseFlow.Shared/DTOs/CreateReportJobRequest.cs
namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request to start a new report generation job.
/// </summary>
public record CreateReportJobRequest
{
    /// <summary>Billing period in YYYY-MM format.</summary>
    public string Period { get; init; } = string.Empty;
}
```

### ReportJobListResponse

```csharp
// File: backend/src/ExpenseFlow.Shared/DTOs/ReportJobListResponse.cs
namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Paginated list of report generation jobs.
/// </summary>
public record ReportJobListResponse
{
    public List<ReportJobDto> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}
```

## Entity Relationships

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              User                                       │
│                               ▲                                         │
│                               │ 1                                       │
│                               │                                         │
│              ┌────────────────┴────────────────┐                        │
│              │                                 │                        │
│              ▼ *                               ▼ *                      │
│  ┌───────────────────────┐        ┌───────────────────────────┐        │
│  │ ReportGenerationJob   │        │     ExpenseReport         │        │
│  ├───────────────────────┤        ├───────────────────────────┤        │
│  │ UserId (FK)           │        │ UserId (FK)               │        │
│  │ Period                │        │ Period                    │        │
│  │ Status                │        │ Status                    │        │
│  │ TotalLines            │        │ Lines[]                   │        │
│  │ ProcessedLines        │        └───────────────────────────┘        │
│  │ GeneratedReportId ────┼────────────────────▶ (0..1)                 │
│  └───────────────────────┘                                             │
└─────────────────────────────────────────────────────────────────────────┘
```

## Status Transitions

```
            ┌─────────────┐
            │   Pending   │◀─── Job created, waiting for Hangfire to pick up
            └──────┬──────┘
                   │ Hangfire executes
                   ▼
            ┌─────────────┐
      ┌─────│ Processing  │─────┐
      │     └──────┬──────┘     │
      │            │            │
      │ User       │ Success    │ Error
      │ cancels    ▼            ▼
      │     ┌─────────────┐ ┌─────────┐
      │     │ Completed   │ │ Failed  │
      │     └─────────────┘ └─────────┘
      │
      ▼
┌──────────────────────┐
│ CancellationRequested│───► Cancelled (when job acknowledges)
└──────────────────────┘
```

## Data Retention

Jobs are retained for 30 days after completion (per FR-009). A nightly cleanup job deletes old records:

```sql
-- Cleanup query (run nightly via Hangfire)
DELETE FROM report_generation_jobs
WHERE completed_at IS NOT NULL
  AND completed_at < NOW() - INTERVAL '30 days';
```

## Indexes Rationale

| Index | Purpose | Query Pattern |
|-------|---------|---------------|
| `ix_report_generation_jobs_user_status` | User's job list, filtered by status | `WHERE user_id = ? AND status = ?` |
| `ix_report_generation_jobs_completed_at` | Cleanup old jobs | `WHERE completed_at < ?` |
| `ix_report_generation_jobs_user_period_active` | Prevent duplicates | Unique constraint for active jobs |

## DbContext Registration

```csharp
// Add to ExpenseFlowDbContext.cs
public DbSet<ReportGenerationJob> ReportGenerationJobs => Set<ReportGenerationJob>();

// Add to OnModelCreating (if using Fluent API assembly scanning):
// Already handled via IEntityTypeConfiguration pattern
```
