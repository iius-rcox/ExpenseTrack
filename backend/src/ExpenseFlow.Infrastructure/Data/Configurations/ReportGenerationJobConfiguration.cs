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
